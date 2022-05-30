using System;
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
using NBagOfUis;
using Nebulator.Common;
using Nebulator.Script;
using Nebulator.Midi;
using Nebulator.OSC;
using Nebulator.UI;
using NBagOfTricks.ScriptCompiler;


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
        ScriptBase? _script = new();

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
            InitializeComponent();

            // Get settings.
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            UserSettings.TheSettings = (UserSettings)Settings.Load(appDir, typeof(UserSettings));

            #region Init UI from settings
            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = UserSettings.TheSettings.SelectedColor };

            // Main form.
            Location = UserSettings.TheSettings.FormGeometry.Location;
            Size = UserSettings.TheSettings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            BackColor = UserSettings.TheSettings.BackColor;

            // The rest of the controls.
            textViewer.WordWrap = false;
            textViewer.BackColor = UserSettings.TheSettings.BackColor;
            textViewer.Colors.Add(" E ", Color.LightPink);
            textViewer.Colors.Add(" W ", Color.Plum);

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKillComm.Image, UserSettings.TheSettings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap((Bitmap)fileDropDownButton.Image, UserSettings.TheSettings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image, UserSettings.TheSettings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, UserSettings.TheSettings.IconColor);
            btnClear.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnClear.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnWrap.Image, UserSettings.TheSettings.IconColor);
            btnKeyboard.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKeyboard.Image, UserSettings.TheSettings.IconColor);

            btnMonIn.Checked = UserSettings.TheSettings.MonitorInput;
            btnMonOut.Checked = UserSettings.TheSettings.MonitorOutput;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            sldSpeed.DrawColor = UserSettings.TheSettings.IconColor;
            sldSpeed.BackColor = UserSettings.TheSettings.BackColor;
            sldSpeed.Invalidate();

            sldVolume.DrawColor = UserSettings.TheSettings.ControlColor; 
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

            btnWrap.Checked = UserSettings.TheSettings.WordWrap;
            textViewer.WordWrap = btnWrap.Checked;
            btnWrap.Click += (object? _, EventArgs __) => { textViewer.WordWrap = btnWrap.Checked; };

            btnKillComm.Click += (object? _, EventArgs __) => { Kill(); };
            btnClear.Click += (object? _, EventArgs __) => { textViewer.Clear(); };
            #endregion

            //// For testing.
            //lblSolo.Hide();
            //lblMute.Hide();
        }

        /// <summary>
        /// Post create init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
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

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _logger.Info("Shutting down.");

            ProcessPlay(PlayCommand.Stop);

            // Just in case.
            Kill();

            // Save user settings.
            UserSettings.TheSettings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            Keyboard kbd = (Keyboard)_inputDevices[DeviceType.Vkey];
            UserSettings.TheSettings.KeyboardFormGeometry = new()
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
            _nppVals.SetValue("master", "speed", sldSpeed.Value);
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

            if (_scriptFileName == Definitions.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                ProcessPlay(PlayCommand.StopRewind);

                // Clean up any old.
                timeMaster.TimeDefs.Clear();

                // Compile script now.
                Compiler compiler = new() { WorkPath = UserSettings.TheSettings.WorkPath };
                compiler.Execute(_scriptFileName);
                _script = (ScriptBase?)compiler.Script;

                _watcher.WatchedFiles.ForEach(fn => _logger.Debug($"file watcher before {compiler.SourceFiles.Count()}"));

                // Update file watcher.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                _watcher.WatchedFiles.ForEach(fn => _logger.Debug($"file watcher after {compiler.SourceFiles.Count()}"));

                // Process errors. Some may be warnings.
                int errorCount = compiler.Results.Count(w => w.ResultType == CompileResultType.Error);

                if (errorCount == 0 && _script is not null)
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

                        // Init the timeclock.
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
                    LogLevel level = LogLevel.Info;

                    switch(r.ResultType)
                    {
                        case CompileResultType.Error:
                            level = LogLevel.Error;
                            break;
                        case CompileResultType.Warning:
                            level = LogLevel.Warn;
                            break;
                        case CompileResultType.Info:
                            level = LogLevel.Info;
                            break;
                    }

                    string msg = r.Message; // default

                    if (r.SourceFile != "")
                    {
                        msg = r.LineNumber > 0 ? $"{Path.GetFileName(r.SourceFile)}({r.LineNumber}): {r.Message}" : $"{r.SourceFile}: {r.Message}";
                    }

                    _logger.Log(new LogEventInfo()
                    {
                        Level = level,
                        Message = msg
                    });
                });
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
                btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, UserSettings.TheSettings.IconColor);
                _needCompile = false;
            }
            else
            {
                btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, Color.Red);
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
            var kbd = new Keyboard();
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
                    t.Volume = _nppVals.GetDouble(t.ChannelName, "volume", Channel.DEF_VOL_TODO);
                    t.State = (ChannelState)_nppVals.GetInteger(t.ChannelName, "state", (int)ChannelState.Normal);

                    ChannelControl tctl = new()
                    {
                        Location = new Point(x, y),
                        BoundChannel = t,
                        BorderStyle = BorderStyle.FixedSingle
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
        /// Clean up from before.
        /// </summary>
        void DestroyChannelControls()
        {
            List<Control> toRemove = new(Controls.OfType<ChannelControl>());
            toRemove.ForEach(c => { c.Dispose(); Controls.Remove(c); });
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
            BeginInvoke((MethodInvoker)delegate ()
           {
               if (_script is not null)
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

            if (_script is not null && chkPlay.Checked && !_needCompile)
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
               if (_script is not null && sender is not null)
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
            if(_script is not null)
            {
                _script.Playing = chkPlay.Checked;
                _script.StepTime = _stepTime;
                _script.RealTime = (DateTime.Now - _startTime).TotalSeconds;
                _script.Speed = sldSpeed.Value;
                _script.MasterVolume = sldVolume.Value;
            }
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (_script is not null)
            {
                if (Math.Abs(_script.Speed - sldSpeed.Value) > 0.001)
                {
                    sldSpeed.Value = _script.Speed;
                    SetFastTimerPeriod();
                }

                if (Math.Abs(_script.MasterVolume - sldVolume.Value) > 0.001)
                {
                    sldVolume.Value = _script.MasterVolume;
                }
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="ex"></param>
        void ProcessScriptRuntimeError(Exception ex)
        {
            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            // Locate the offending frame.
            string srcFile = "???";
            int srcLine = -1;
            string msg = ex.Message;
            StackTrace st = new(ex, true);
            StackFrame? sf = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame? stf = st.GetFrame(i);
                if (stf is not null)
                {
                    var stfn = stf!.GetFileName();
                    if (stfn is not null)
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
            }
            // else // unknown?

            _logger.Error(srcLine > 0 ? $"{srcFile}({srcLine}): {msg}" : $"{srcFile}: {msg}");
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
            using OpenFileDialog openDlg = new()
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
                        sldSpeed.Value = _nppVals.GetDouble("master", "speed", 100.0);
                        sldVolume.Value = _nppVals.GetDouble("master", "volume", Channel.DEF_VOL_TODO);

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
            e.FileNames.ForEach(fn => _logger.Debug($"Watcher_Changed {fn}"));

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
            MiscUtils.ShowReadme("Nebulator");
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
            BeginInvoke((MethodInvoker)delegate ()
            {
                textViewer.AppendLine(msg);
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
                File.ReadAllLines(logFileName).ForEach(l => tv.AppendText(l));
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
            var changes = UserSettings.TheSettings.Edit("User Settings");

            // Detect changes of interest.
            bool restart = false;

            // Figure out what changed - each handled differently.
            foreach (var (name, cat) in changes)
            {
                restart |= cat == "Cosmetics";
                restart |= cat == "Devices";
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
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
        protected override void OnKeyDown(KeyEventArgs e)
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
                InternalPpq = Time.SubdivsPerBeat,
                Tempo = sldSpeed.Value
            };

            var per = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer(per, MmTimerCallback);
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
            using SaveFileDialog saveDlg = new()
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
            if (_script is not null)
            {
                bool ok = true;
                if (_needCompile)
                {
                    ok = CompileScript();
                }

                if (ok)
                {
                    Dictionary<int, string> channels = new();
                    _channels.ForEach(t => channels.Add(t.ChannelNumber, t.ChannelName));
                    MidiUtils.ExportToMidi(_script.GetAllSteps(), fn, channels, sldSpeed.Value, "Converted from " + _scriptFileName);
                }
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
