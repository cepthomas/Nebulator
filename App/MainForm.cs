﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using NAudio.Wave;
using NLog;
using NBagOfTricks;
using NBagOfTricks.UI;
using Nebulator.Common;
using Nebulator.Script;
using Nebulator.Midi;
using Nebulator.OSC;
using Nebulator.Controls;


namespace Nebulator.App
{
    public partial class MainForm : Form
    {
        #region Enums
        /// <summary>Internal status.</summary>
        enum PlayCommand { Start, Stop, Rewind, StopRewind, UpdateUiTime }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("MainForm");

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Current np file name.</summary>
        string _scriptFileName = Definitions.UNKNOWN_STRING;

        /// <summary>The current script.</summary>
        ScriptBase _script = new();

        /// <summary>The current channels.</summary>
        List<Channel> _channels = new();

        /// <summary>Persisted internal values for current script file.</summary>
        Bag _nppVals = new();

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        Time _stepTime = new();

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for compile products.</summary>
        string _compileTempDir = "";

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>All devices to use for send.</summary>
        readonly Dictionary<DeviceType, IOutputDevice> _outputDevices = new();

        /// <summary>All devices to use for receive.</summary>
        readonly Dictionary<DeviceType, IInputDevice> _inputDevices = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            DirectoryInfo di = new(appDir);
            di.Create();

            var set = UserSettings.Load(appDir);
            if (set is not null)
            {
                UserSettings.TheSettings = set;
            }
            else
            {
                MessageBox.Show($"Wooops - bad user settings - goodbye");
                Environment.Exit(1);
            }

            InitializeComponent();
            toolStrip1.Renderer = new NBagOfTricks.UI.CheckBoxRenderer() { SelectedColor = UserSettings.TheSettings.SelectedColor };
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            #region Init UI from settings
            // Main form.
            Location = new Point(UserSettings.TheSettings.MainFormInfo.X, UserSettings.TheSettings.MainFormInfo.Y);
            Size = new Size(UserSettings.TheSettings.MainFormInfo.Width, UserSettings.TheSettings.MainFormInfo.Height);
            WindowState = FormWindowState.Normal;
            BackColor = UserSettings.TheSettings.BackColor;

            // The rest of the controls.
            textViewer.WordWrap = false;
            textViewer.BackColor = UserSettings.TheSettings.BackColor;
            textViewer.Colors.Add(" E ", Color.LightPink);
            textViewer.Colors.Add(" W ", Color.Plum);

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap(btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap(btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap(btnKillComm.Image, UserSettings.TheSettings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap(fileDropDownButton.Image, UserSettings.TheSettings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap(btnRewind.Image, UserSettings.TheSettings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);
            btnClear.Image = GraphicsUtils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Image = GraphicsUtils.ColorizeBitmap(btnWrap.Image, UserSettings.TheSettings.IconColor);
            btnKeyboard.Image = GraphicsUtils.ColorizeBitmap(btnKeyboard.Image, UserSettings.TheSettings.IconColor);

            btnMonIn.Checked = UserSettings.TheSettings.MonitorInput;
            btnMonOut.Checked = UserSettings.TheSettings.MonitorOutput;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap(chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            potSpeed.DrawColor = UserSettings.TheSettings.IconColor;
            potSpeed.BackColor = UserSettings.TheSettings.BackColor;
            potSpeed.Invalidate();

            sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
            sldVolume.DecPlaces = 2;
            sldVolume.Invalidate();

            timeMaster.ControlColor = UserSettings.TheSettings.ControlColor;
            timeMaster.Invalidate();

            if (UserSettings.TheSettings.CpuMeter)
            {
                CpuMeter cpuMeter = new()
                {
                    Width = 50,
                    Height = toolStrip1.Height,
                    DrawColor = Color.Red
                };
                // This took way too long to find out:
                //https://stackoverflow.com/questions/12823400/statusstrip-hosting-a-usercontrol-fails-to-call-usercontrols-onpaint-event
                cpuMeter.MinimumSize = cpuMeter.Size;
                cpuMeter.Enable = true;
                toolStrip1.Items.Add(new ToolStripControlHost(cpuMeter));
            }

            // For testing.
            lblSolo.Hide();
            lblMute.Hide();

            btnWrap.Checked = UserSettings.TheSettings.WordWrap;
            textViewer.WordWrap = btnWrap.Checked;
            btnWrap.Click += (object? _, EventArgs __) => { textViewer.WordWrap = btnWrap.Checked; };

            btnKillComm.Click += (object? _, EventArgs __) => { Kill(); };
            btnClear.Click += (object? _, EventArgs __) => { textViewer.Clear(); };
            #endregion

            InitLogging();
            _logger.Info("============================ Starting up ===========================");

            PopulateRecentMenu();

            MusicDefinitions.Init();

            CreateDevices();

            // Fast mm timer.
            SetFastTimerPeriod();
            _mmTimer.Start();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChangeEvent += Watcher_Changed;

            Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";

            #region Command line args
            string sopen = "";

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                sopen = OpenScriptFile(args[1]);
            }

            if (sopen == "")
            {
                ProcessPlay(PlayCommand.Stop);
            }
            else
            {
                _logger.Error($"Couldn't open script file: {sopen}");
            }
            #endregion
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _logger.Info("Shutting down.");

            ProcessPlay(PlayCommand.Stop);

            // Just in case.
            Kill();

            // Save user settings.
            UserSettings.TheSettings.MainFormInfo = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            Keyboard kbd = (Keyboard)_inputDevices[DeviceType.Vkey];
            UserSettings.TheSettings.KeyboardInfo = new()
            {
                X = kbd.Location.X,
                Y = kbd.Location.Y,
                Width = kbd.Width,
                Height = kbd.Height
            };

            UserSettings.TheSettings.Keyboard = btnKeyboard.Checked;
            UserSettings.TheSettings.WordWrap = btnWrap.Checked;

            UserSettings.TheSettings.Save();

            SaveProjectValues();

            DestroyDevices();

            _script.Dispose();
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmTimer.Stop();
                _mmTimer.Dispose();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Project persistence
        /// <summary>
        /// Save current values.
        /// </summary>
        void SaveProjectValues()
        {
            _nppVals.Clear();
            _nppVals.SetValue("master", "speed", potSpeed.Value);
            _nppVals.SetValue("master", "volume", sldVolume.Value);

            foreach (var ch in _channels)
            {
                _nppVals.SetValue(ch.ChannelName, "volume", ch.Volume);
                _nppVals.SetValue(ch.ChannelName, "state", ch.State);
            }

            _nppVals.Save();
        }
        #endregion

        #region Compile
        /// <summary>
        /// Compile the neb script itself.
        /// </summary>
        bool CompileScript()
        {
            bool ok = true;

            using (new WaitCursor())
            {
                if (_scriptFileName == Definitions.UNKNOWN_STRING)
                {
                    _logger.Warn("No script file loaded.");
                    ok = false;
                }
                else
                {
                    // Clean up any old.
                    _script.Dispose();
                    _watcher.Clear();
                    timeMaster.TimeDefs.Clear();

                    // Compile script now.
                    Compiler compiler = new();
                    compiler.Execute(_scriptFileName);
                    _script = compiler.Script;

                    // Process results.
                    if (_script.Valid)
                    {
                        // Check for changes to channels.
                        if (compiler.Channels.Count > 0)
                        {
                            // Update.
                            DestroyChannelControls();
                            _channels = compiler.Channels;
                            CreateChannelControls();
                        }

                        _script.Init(compiler.Channels);

                        SetCompileStatus(true);
                        compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });
                        _compileTempDir = compiler.TempDir;

                        // Need exception handling here to protect from user script errors.
                        try
                        {
                            // Init shared vars.
                            InitRuntime();

                            // Setup script. This builds the sequences and sections.
                            _script.Setup();

                            // Script may have altered shared values.
                            ProcessRuntime();

                            // Build all the steps.
                            _script.BuildSteps();

                            ///// Init the timeclock.
                            timeMaster.TimeDefs = _script.GetSectionMarkers();
                            timeMaster.MaxBeat = timeMaster.TimeDefs.Keys.Max();

                            SetFastTimerPeriod();
                        }
                        catch (Exception ex)
                        {
                            ProcessScriptRuntimeError(ex);
                            ok = false;
                        }

                        SetCompileStatus(ok);
                    }
                    else
                    {
                        _logger.Error("Compile failed.");
                        ok = false;
                        ProcessPlay(PlayCommand.StopRewind);
                        SetCompileStatus(false);
                    }

                    // Log/show results.
                    compiler.Results.ForEach(r =>
                    {
                        switch (r.ResultType)
                        {
                            case CompileResultType.Fatal:
                            case CompileResultType.Error:
                            case CompileResultType.Runtime:
                                _logger.Error(r.ToString());
                                break;

                            case CompileResultType.Warning:
                                _logger.Warn(r.ToString());
                                break;

                            case CompileResultType.Info:
                                _logger.Info(r.ToString());
                                break;
                        }
                    });
                }
            }

            return ok;
        }

        /// <summary>
        /// Update system statuses.
        /// </summary>
        /// <param name="compileStatus">True if compile is clean.</param>
        void SetCompileStatus(bool compileStatus)
        {
            if (compileStatus)
            {
                btnCompile.Image = GraphicsUtils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);
                _needCompile = false;
            }
            else
            {
                btnCompile.Image = GraphicsUtils.ColorizeBitmap(btnCompile.Image, Color.Red);
                _needCompile = true;
            }
        }
        #endregion

        #region Devices
        /// <summary>
        /// Create all devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices()
        {
            bool ok = true;

            // Keyboard.
            var kbd = new Keyboard(); //TODO2 useful? and/or replace with something else? x/y surface
            RegDevice(kbd);
            kbd.Visible = UserSettings.TheSettings.Keyboard;
            btnKeyboard.Checked = UserSettings.TheSettings.Keyboard;
            btnKeyboard.Click += (object? _, EventArgs __) => { kbd.Visible = btnKeyboard.Checked; };

            if (UserSettings.TheSettings.MidiIn != "")
            {
                RegDevice(new MidiInput());
            }

            if (UserSettings.TheSettings.MidiOut != "")
            {
                RegDevice(new MidiOutput());
            }

            if (UserSettings.TheSettings.OscIn != "")
            {
                RegDevice(new OscInput());
            }

            if (UserSettings.TheSettings.OscOut != "")
            {
                RegDevice(new OscOutput());
            }

            // Local function.
            void RegDevice(IDevice dt)
            {
                if (dt.Init())
                {
                    if (dt is IInputDevice)
                    {
                        IInputDevice? nin = dt as IInputDevice;
                        nin!.DeviceInputEvent += Device_InputEvent;
                        _inputDevices.Add(nin.DeviceType, nin);
                    }
                    else
                    {
                        IOutputDevice? nout = dt as IOutputDevice;
                        _outputDevices.Add(nout!.DeviceType, nout);
                    }
                }
                else
                {
                    _logger.Error($"Failed to init device:{dt.DeviceType}");
                    ok = false;
                }
            }

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        void DestroyDevices()
        {
            // Destroy devices.
            _inputDevices.ForEach(i =>
            {
                i.Value.DeviceInputEvent -= Device_InputEvent;
                i.Value.Stop();
                i.Value.Dispose();
            });
            _inputDevices.Clear();

            _outputDevices.ForEach(o =>
            {
                o.Value.Stop();
                o.Value.Dispose();
            });
            _outputDevices.Clear();
        }
        #endregion

        #region Channel controls
        /// <summary>
        /// Create the channel controls from the user script.
        /// </summary>
        bool CreateChannelControls()
        {
            bool ok = true;

            const int CONTROL_SPACING = 10;
            int x = btnRewind.Left; // timeMaster.Right + CONTROL_SPACING;
            int y = timeMaster.Bottom + CONTROL_SPACING;

            // Create new channel controls.
            foreach (Channel t in _channels)
            {
                if (_outputDevices.TryGetValue(t.DeviceType, out IOutputDevice? dev))
                {
                    t.Device = dev;
                    t.Volume = _nppVals.GetDouble(t.ChannelName, "volume", 0.8);
                    t.State = (ChannelState)_nppVals.GetInteger(t.ChannelName, "state", (int)ChannelState.Normal);

                    ChannelControl tctl = new()
                    {
                        Location = new Point(x, y),
                        BoundChannel = t,
                    };
                    Controls.Add(tctl);

                    x += tctl.Width + CONTROL_SPACING;
                }

                if (dev is null)
                {
                    _logger.Error($"Invalid device: {t.DeviceType} for channel: {t.ChannelName}");
                    ok = false;
                    break;
                }
            }

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        void DestroyChannelControls()
        {
            foreach (Control ctl in Controls)
            {
                if (ctl is ChannelControl)
                {
                    ChannelControl? tctl = ctl as ChannelControl;
                    tctl?.Dispose();
                    Controls.Remove(tctl);
                }
            }
        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // Do some stats gathering for measuring jitter.
            //if (_tan.Grab())
            //{
            //    _logger.Info($"Midi timing: {_tan.Mean}");
            //}

            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker) delegate ()
           {
               if (_script.Valid)
               {
                   NextStep();
               }
           });
        }

        /// <summary>
        /// Output steps for next time increment.
        /// </summary>
        void NextStep()
        {
            ////// Neb steps /////
            InitRuntime();

            if (chkPlay.Checked && !_needCompile)
            {
                //_tan.Arm();

                // Kick the script. Note: Need exception handling here to protect from user script errors.
                try
                {
                    _script.Step();
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(ex);
                }

                //if (_tan.Grab())
                //{
                //    _logger.Info($"NEB tan: {_tan.Mean}");
                //}

                // Process any sequence steps.
                bool anySolo = _channels.Where(t => t.State == ChannelState.Solo).Any();
                bool anyMute = _channels.Where(t => t.State == ChannelState.Mute).Any();
                lblSolo.BackColor = anySolo ? Color.Pink : SystemColors.Control;
                lblMute.BackColor = anyMute ? Color.Pink : SystemColors.Control;

                var steps = _script.GetSteps(_stepTime);

                foreach (var step in steps)
                {
                    Channel channel = _channels.Where(t => t.ChannelNumber == step.ChannelNumber).First();

                    // Is it ok to play now?
                    bool play = channel is not null && (channel.State == ChannelState.Solo || (channel.State == ChannelState.Normal && !anySolo));

                    if (play)
                    {
                        switch (step)
                        {
                            case StepFunction sf:
                                // Need exception handling here to protect from user script errors.
                                try
                                {
                                    sf.ScriptFunction?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    ProcessScriptRuntimeError(ex);
                                }
                                break;

                            default:
                                if (step.Device is IOutputDevice dev)
                                {
                                    // Maybe tweak values.
                                    if (step is StepNoteOn on && channel is not null)
                                    {
                                        on.Adjust(sldVolume.Value, channel.Volume);
                                    }
                                    dev.Send(step);
                                }
                                break;
                        }
                    }
                }

                ///// Bump time.
                _stepTime.Advance();

                // Check for end of play. If no steps or not selected, free running mode so always keep going.
                if (timeMaster.TimeDefs.Count > 1)
                {
                    // Check for end.
                    if (_stepTime.Beat > timeMaster.TimeDefs.Last().Key)
                    {
                        ProcessPlay(PlayCommand.StopRewind);
                        Kill(); // just in case
                    }
                }
                // else keep going

                ProcessPlay(PlayCommand.UpdateUiTime);
            }

            // Process whatever the script did.
            ProcessRuntime();

            // Process any lingering noteoffs etc.
            _outputDevices.ForEach(o => o.Value?.Housekeep());
            _inputDevices.ForEach(i => i.Value?.Housekeep());
        }

        /// <summary>
        /// Process input event.
        /// </summary>
        void Device_InputEvent(object? sender, DeviceInputEventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate ()
            {
               if (_script.Valid && sender is not null)
               {
                   var dev = (IInputDevice)sender;

                   switch (e.Step)
                   {
                       case StepNoteOn ston:
                           _script.InputNote(dev.DeviceType, ston.ChannelNumber, ston.NoteNumber);
                           break;

                       case StepNoteOff stoff:
                           _script.InputNote(dev.DeviceType, stoff.ChannelNumber, -stoff.NoteNumber);
                           break;

                       case StepControllerChange stctl:
                            _script.InputControl(dev.DeviceType, stctl.ChannelNumber, stctl.ControllerId, stctl.Value);
                           break;
                   }
               }
           });
        }
        #endregion

        #region Runtime interop
        /// <summary>
        /// Package up the shared runtime stuff the script may need. Call this before any script updates.
        /// </summary>
        void InitRuntime()
        {
            _script.Playing = chkPlay.Checked;
            _script.StepTime = _stepTime;
            _script.RealTime = (DateTime.Now - _startTime).TotalSeconds;
            _script.Speed = potSpeed.Value;
            _script.MasterVolume = sldVolume.Value;
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (Math.Abs(_script.Speed - potSpeed.Value) > 0.001)
            {
                potSpeed.Value = _script.Speed;
                SetFastTimerPeriod();
            }

            if (Math.Abs(_script.MasterVolume - sldVolume.Value) > 0.001)
            {
                sldVolume.Value = _script.MasterVolume;
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="ex"></param>
        void ProcessScriptRuntimeError(Exception ex)
        {
            CompileResult? err;

            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            // Locate the offending frame.
            string srcFile;
            int srcLine = -1;
            StackTrace st = new(ex, true);
            StackFrame? sf = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame? stf = st.GetFrame(i);
                if(stf is not null)
                {
                    var stfn = stf!.GetFileName();
                    if(stfn is not null)
                    {
                        if (stfn.ToUpper().Contains(_compileTempDir.ToUpper()))
                        {
                            sf = stf;
                            break;
                        }
                    }
                }
            }

            if (sf is not null)
            {
                // Dig out generated file parts.
                string? genFile = sf!.GetFileName();
                int genLine = sf.GetFileLineNumber() - 1;

                // Open the generated file and dig out the source file and line.
                string[] genLines = File.ReadAllLines(genFile!);

                srcFile = genLines[0].Trim().Replace("//", "");

                int ind = genLines[genLine].LastIndexOf("//");
                if (ind != -1)
                {
                    string sl = genLines[genLine][(ind + 2)..];
                    srcLine = int.Parse(sl);
                }

                err = new CompileResult()
                {
                    ResultType = CompileResultType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = ex.Message
                };
            }
            else // unknown?
            {
                err = new CompileResult()
                {
                    ResultType = CompileResultType.Runtime,
                    SourceFile = "",
                    LineNumber = -1,
                    Message = ex.Message
                };
            }

            if (err is not null)
            {
                _logger.Error(err.ToString());
            }
        }
        #endregion

        #region File handling
        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object? sender, EventArgs e)
        {
            string? fn = sender!.ToString();
            string sopen = OpenScriptFile(fn!);
            if (sopen != "")
            {
                _logger.Error(sopen);
            }
        }

        /// <summary>
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            OpenFileDialog openDlg = new()
            {
                Filter = "Nebulator files | *.neb",
                Title = "Select a Nebulator file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                string sopen = OpenScriptFile(openDlg.FileName);
                if (sopen != "")
                {
                    _logger.Error(sopen);
                }
            }
        }

        /// <summary>
        /// Common script file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        string OpenScriptFile(string fn)
        {
            string ret = "";

            using (new WaitCursor())
            {
                try
                {
                    // Clean up the old.
                    SaveProjectValues();

                    if (File.Exists(fn))
                    {
                        _logger.Info($"Opening {fn}");
                        _scriptFileName = fn;

                        // Get the persisted properties.
                        _nppVals = Bag.Load(fn.Replace(".neb", ".nebp"));
                        potSpeed.Value = _nppVals.GetDouble("master", "speed", 100.0);
                        sldVolume.Value = _nppVals.GetDouble("master", "volume", 0.8);

                        SetCompileStatus(true);
                        AddToRecentDefs(fn);
                        bool ok = CompileScript();
                        SetCompileStatus(ok);

                        Text = $"Nebulator {MiscUtils.GetVersionString()} - {fn}";
                    }
                    else
                    {
                        ret = $"Invalid file: {fn}";
                    }
                }
                catch (Exception ex)
                {
                    ret = $"Couldn't open the script file: {fn} because: {ex.Message}";
                    _logger.Error(ret);
                    SetCompileStatus(false);
                }
            }

            return ret;
        }

        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateRecentMenu()
        {
            ToolStripItemCollection menuItems = recentToolStripMenuItem.DropDownItems;
            menuItems.Clear();

            UserSettings.TheSettings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f, null, new EventHandler(Recent_Click));
                menuItems.Add(menuItem);
            });
        }

        /// <summary>
        /// Update the mru with the user selection.
        /// </summary>
        /// <param name="fn">The selected file.</param>
        void AddToRecentDefs(string fn)
        {
            if (File.Exists(fn))
            {
                UserSettings.TheSettings.RecentFiles.UpdateMru(fn);
                PopulateRecentMenu();
            }
        }

        /// <summary>
        /// One or more np files have changed so reload/compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object? sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker) delegate ()
           {
               if (UserSettings.TheSettings.AutoCompile)
               {
                   CompileScript();
               }
               else
               {
                   SetCompileStatus(false);
               }
           });
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go or stop button.
        /// </summary>
        void Play_Click(object? sender, EventArgs e)
        {
            ProcessPlay(chkPlay.Checked ? PlayCommand.Start : PlayCommand.Stop);
        }

        /// <summary>
        /// Update multimedia timer period.
        /// </summary>
        void Speed_ValueChanged(object? sender, EventArgs e)
        {
            SetFastTimerPeriod();
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        void Rewind_Click(object? sender, EventArgs e)
        {
            ProcessPlay(PlayCommand.Rewind);
        }

        /// <summary>
        /// User updated volume.
        /// </summary>
        void Volume_ValueChanged(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        void Compile_Click(object? sender, EventArgs e)
        {
            CompileScript();
            ProcessPlay(PlayCommand.StopRewind);
        }

        /// <summary>
        /// User updated the time.
        /// </summary>
        void Time_ValueChanged(object? sender, EventArgs e)
        {
            _stepTime = timeMaster.CurrentTime;
            ProcessPlay(PlayCommand.UpdateUiTime);
        }

        /// <summary>
        /// Monitor comm messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        void Mon_Click(object? sender, EventArgs e)
        {
            UserSettings.TheSettings.MonitorInput = btnMonIn.Checked;
            UserSettings.TheSettings.MonitorOutput = btnMonOut.Checked;
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            // Make some markdown.
            List<string> mdText = new()
            {
                // Device info.
                "# Your Devices",
                "## Midi Input"
            };

            if (MidiIn.NumberOfDevices > 0)
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    mdText.Add($"- {MidiIn.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                mdText.Add($"- None");
            }

            mdText.Add("## Midi Output");
            if (MidiOut.NumberOfDevices > 0)
            {
                for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                {
                    mdText.Add($"- {MidiOut.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                mdText.Add($"- None");
            }

            mdText.Add("## Asio");
            if (AsioOut.GetDriverNames().Length > 0)
            {
                foreach (string sdev in AsioOut.GetDriverNames())
                {
                    mdText.Add($"- {sdev}");
                }
            }
            else
            {
                mdText.Add($"- None");
            }

            // Main help file.
            mdText.AddRange(File.ReadAllLines(@"Resources\README.md"));

            Tools.MarkdownToHtml(mdText, "lightcyan", "helvetica", true);
        }
        #endregion

        #region Messages and logging
        /// <summary>
        /// Init all logging functions.
        /// </summary>
        void InitLogging()
        {
            // Do log maintenance.
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");

            FileInfo fi = new(Path.Combine(appDir, "log.txt"));
            if (fi.Exists && fi.Length > 100000)
            {
                File.Copy(fi.FullName, fi.FullName.Replace("log.", "log2."), true);
                File.Delete(fi.FullName);
            }

            // Hook to client window.
            LogClientNotificationTarget.ClientNotification += Log_ClientNotification;
        }

        /// <summary>
        /// A message from the logger to display to the user.
        /// </summary>
        /// <param name="level">Level.</param>
        /// <param name="msg">The message.</param>
        void Log_ClientNotification(LogLevel level, string msg)
        {
            BeginInvoke((MethodInvoker) delegate ()
           {
               textViewer.AddLine(msg);
           });
        }

        /// <summary>
        /// Show the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogShow_Click(object? sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "Log Viewer",
                Size = new Size(900, 600),
                BackColor = UserSettings.TheSettings.BackColor,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            TextViewer tv = new()
            {
                Dock = DockStyle.Fill,
                WordWrap = true,
                MaxText = 50000
            };

            tv.Colors.Add(" E ", Color.LightPink);
            tv.Colors.Add(" W ", Color.Plum);
            //tv.Colors.Add(" SND???:", Color.LightGreen);
            f.Controls.Add(tv);

            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            string logFileName = Path.Combine(appDir, "log.txt");
            using (new WaitCursor())
            {
                File.ReadAllLines(logFileName).ForEach(l => tv.AddLine(l)); //TODO2 still a little broken.
            }

            f.ShowDialog();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void UserSettings_Click(object? sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "User Settings",
                Size = new Size(350, 500),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            PropertyGrid pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = UserSettings.TheSettings
            };

            // Detect changes of interest.
            bool changed = false;
            pg.PropertyValueChanged += (sdr, args) => { changed = true; };

            f.Controls.Add(pg);
            f.ShowDialog();

            if (changed)
            {
                UserSettings.TheSettings.Save();
                MessageBox.Show("Settings changes require a restart to take effect.");
            }
        }
        #endregion

        #region Play control
        /// <summary>
        /// Update UI state per param.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>Indication of success.</returns>
        bool ProcessPlay(PlayCommand cmd)
        {
            bool ret = true;

            switch (cmd)
            {
                case PlayCommand.Start:
                    bool ok = !_needCompile || CompileScript();
                    if (ok)
                    {
                        _startTime = DateTime.Now;
                        chkPlay.Checked = true;
                        _mmTimer.Start();
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        ret = false;
                    }
                    break;

                case PlayCommand.Stop:
                    chkPlay.Checked = false;
                    _mmTimer.Stop();

                    // Send midi stop all notes just in case.
                    Kill();
                    break;

                case PlayCommand.Rewind:
                    _stepTime.Reset();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    _stepTime.Reset();
                    break;

                case PlayCommand.UpdateUiTime:
                    // See below.
                    break;
            }

            // Always do this.
            timeMaster.CurrentTime = _stepTime;

            _outputDevices.Values.ForEach(o => { if (chkPlay.Checked) o.Start(); else o.Stop(); });

            return ret;
        }
        #endregion

        #region Keyboard handling
        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
        }
        #endregion

        #region Timer
        /// <summary>
        /// Common func.
        /// </summary>
        void SetFastTimerPeriod()
        {
            // Make a transformer.
            MidiTime mt = new()
            {
                InternalPpq = Time.SUBDIVS_PER_BEAT,
                Tempo = potSpeed.Value
            };
            _mmTimer.SetTimer(mt.RoundedInternalPeriod(), MmTimerCallback);
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export steps to a midi file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportMidi_Click(object? sender, EventArgs e)
        {
            SaveFileDialog saveDlg = new()
            {
                Filter = "Midi files (*.mid)|*.mid",
                Title = "Export to midi file",
                FileName = Path.GetFileName(_scriptFileName.Replace(".neb", ".mid"))
            };

            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                ExportMidi(saveDlg.FileName);
            }
        }

        /// <summary>
        /// Export a midi file.
        /// </summary>
        /// <param name="fn">Output filename.</param>
        void ExportMidi(string fn)
        {
            bool ok = true;

            if (_script.Valid)
            {
                if (_needCompile)
                {
                    ok = CompileScript();
                }
            }
            else
            {
                ok = false;
            }

            if (ok)
            {
                Dictionary<int, string> channels = new();
                _channels.ForEach(t => channels.Add(t.ChannelNumber, t.ChannelName));
                MidiUtils.ExportToMidi(_script.GetAllSteps(), fn, channels, potSpeed.Value, "Converted from " + _scriptFileName);
            }
        }

        /// <summary>
        /// Kill em all.
        /// </summary>
        void Kill()
        {
            _outputDevices.ForEach(o => o.Value?.Kill());
        }
        #endregion
    }
}