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

//TODO0 Config editor.

namespace Nebulator.UI
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
        MmTimerEx _mmTimer = new MmTimerEx();

        /// <summary>The current script.</summary>
        NebScript _script = null;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        Time _stepTime = new Time();

        /// <summary>Script compile errors and warnings.</summary>
        List<ScriptError> _compileResults = new List<ScriptError>();

        /// <summary>Current np file name.</summary>
        string _fn = Definitions.UNKNOWN_STRING;

        /// <summary>Detect changed script files. TODO0 also config files.</summary>
        readonly MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for compile products.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current script file.</summary>
        Config _config = null;

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Devices to use for send.</summary>
        readonly Dictionary<DeviceType, IOutputDevice> _outputDevices = new Dictionary<DeviceType, IOutputDevice>();//TODO1 consolidate?

        /// <summary>Devices to use for recv.</summary>
        readonly Dictionary<DeviceType, IInputDevice> _inputDevices = new Dictionary<DeviceType, IInputDevice>();//TODO1 consolidate?
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.TheSettings = UserSettings.Load(appDir);
            InitializeComponent();
            toolStrip1.Renderer = new NBagOfTricks.UI.CheckBoxRenderer() { SelectedColor = UserSettings.TheSettings.SelectedColor };
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object sender, EventArgs e)
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
            textViewer.Colors.Add("ERROR:", Color.LightPink);
            textViewer.Colors.Add("_WARN:", Color.Plum);

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap(btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap(btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap(btnKillComm.Image, UserSettings.TheSettings.IconColor);
            btnSettings.Image = GraphicsUtils.ColorizeBitmap(btnSettings.Image, UserSettings.TheSettings.IconColor);
            btnAbout.Image = GraphicsUtils.ColorizeBitmap(btnAbout.Image, UserSettings.TheSettings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap(fileDropDownButton.Image, UserSettings.TheSettings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap(btnRewind.Image, UserSettings.TheSettings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);
            btnClear.Image = GraphicsUtils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Image = GraphicsUtils.ColorizeBitmap(btnWrap.Image, UserSettings.TheSettings.IconColor);

            btnMonIn.Checked = UserSettings.TheSettings.MonitorInput;
            btnMonOut.Checked = UserSettings.TheSettings.MonitorOutput;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap(chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            potSpeed.DrawColor = UserSettings.TheSettings.IconColor;
            potSpeed.BackColor = UserSettings.TheSettings.BackColor;
            //potSpeed.Font = UserSettings.TheSettings.ControlFont;
            potSpeed.Invalidate();

            sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
            //sldVolume.Font = UserSettings.TheSettings.ControlFont;
            sldVolume.DecPlaces = 2;
            sldVolume.Invalidate();

            timeMaster.ControlColor = UserSettings.TheSettings.ControlColor;
            timeMaster.Invalidate();

            if (UserSettings.TheSettings.CpuMeter)
            {
                CpuMeter cpuMeter = new CpuMeter()
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

            lblSolo.Hide();
            lblMute.Hide();

            btnClear.Click += (object _, EventArgs __) => { textViewer.Clear(); };
            btnWrap.Click += (object _, EventArgs __) => { textViewer.WordWrap = btnWrap.Checked; };
            #endregion

            InitLogging();

            _logger.Info("Starting up.");

            PopulateRecentMenu();

            ScriptDefinitions.TheDefinitions.Init();

            // Fast mm timer.
            SetSpeedTimerPeriod();
            _mmTimer.Start();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChangeEvent += Watcher_Changed;

            Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";

            #region Open file
            string sopen = "";
            
            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                sopen = OpenFile(args[1]);
            }

            if (sopen == "")
            {
                ProcessPlay(PlayCommand.Stop);
            }
            else
            {
                _logger.Error(sopen);

            }
            #endregion
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _logger.Info("Shutting down.");

            ProcessPlay(PlayCommand.Stop);

            // Just in case.
            _outputDevices.ForEach(o => o.Value?.Kill());

            DestroyDevices();

            if (_script != null)
            {
                // Save the project.
                DestroyChannelControls();
                _script.Dispose();
            }

            // Save user settings.
            SaveSettings();
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmTimer?.Stop();
                _mmTimer?.Dispose();
                _mmTimer = null;

                DestroyDevices();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Manage controls
        /// <summary>
        /// Create the channel controls.
        /// </summary>
        void CreateChannelControls()
        {
            const int CONTROL_SPACING = 10;
            int x = btnRewind.Left; // timeMaster.Right + CONTROL_SPACING;
            int y = timeMaster.Bottom + CONTROL_SPACING;

            // Create new channel controls.
            foreach (Channel t in _config.Channels)
            {
                // Init from persistence.
                t.Volume = t.Volume;

                t.State = t.State;

                ChannelControl tctl = new ChannelControl()
                {
                    Location = new Point(x, y),
                    // BoundChannel = t,
                    State = t.State,
                    Volume = t.Volume,
                     
                };

                tctl.ChannelChangeEvent += ChannelChange_Event;
                Controls.Add(tctl);
                x += tctl.Width + CONTROL_SPACING;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DestroyChannelControls()
        {
            // Save values first. //TODO0
            //_nppVals.Clear();
            //_nppVals.SetValue("master", "volume", sldVolume.Value);
            //_nppVals.SetValue("master", "speed", potSpeed.Value);
            //_script?.Channels?.ForEach(c =>
            //{
            //    _nppVals.SetValue(c.Name, "volume", c.Volume);
            //    _nppVals.SetValue(c.Name, "state", c.State);
            //});
            //_nppVals.Save();
            _config.Save();

            // Remove current controls.
            foreach (Control ctl in Controls)
            {
                if (ctl is ChannelControl)
                {
                    ChannelControl tctl = ctl as ChannelControl;
                    tctl.ChannelChangeEvent -= ChannelChange_Event;
                    tctl.Dispose();
                    Controls.Remove(tctl);
                }
            }
        }

        /// <summary>
        /// Create all devices from user script. NController, Keyboard
        /// </summary>
        void CreateDevices()
        {
            // Clean up for company.
            DestroyDevices();

            // Get requested inputs.
            Keyboard vkey = null; // If used, requires special handling.

            foreach(Controller con in _config.Controllers)
            {
                // Have we seen it yet?
                if (_inputDevices.ContainsKey(con.Device.DeviceType))
                {
                    con.Device = _inputDevices[con.Device.DeviceType];
                }
                else // nope
                {
                    IInputDevice nin = con.Device;
                    switch (nin.DeviceType)
                    {
                        case DeviceType.MidiIn:
                            nin = new MidiInput();
                            break;

                        case DeviceType.OscIn:
                            nin = new OscInput();
                            break;

                        case DeviceType.VkeyIn:
                            vkey = new Keyboard();
                            nin = vkey;
                            break;
                    }

                    // Finish it up.
                    if (nin != null)
                    {
                        if (nin.Init())
                        {
                            nin.DeviceInputEvent += Device_InputEvent;
                            _inputDevices.Add(nin.DeviceType, nin);
                        }
                        else
                        {
                            _logger.Error($"Failed to init input device:");
                        }
                    }
                    else
                    {
                        _logger.Error($"Invalid input device");
                    }
                }
            }

            // Get requested outputs.
            foreach (Channel chan in _config.Channels)
            {
                // Have we seen it yet?
                if (_outputDevices.ContainsKey(chan.Device.DeviceType))
                {
                    chan.Device = _outputDevices[chan.Device.DeviceType];
                }
                else // nope
                {
                    IOutputDevice nout = null;

                    switch (chan.Device.DeviceType)
                    {
                        case DeviceType.MidiOut:
                            nout = new MidiOutput();
                            break;

                        case DeviceType.OscOut:
                            nout = new OscOutput();
                            break;
                    }

                    // Finish it up.
                    if (nout != null)
                    {
                        if (nout.Init())
                        {
                            chan.Device = nout;
                            _outputDevices.Add(chan.Device.DeviceType, nout);
                        }
                        else
                        {
                            _logger.Error($"Failed to init channel: {chan.Device.DeviceType}");
                        }
                    }
                    else
                    {
                        _logger.Error($"Invalid channel: {chan.Device.DeviceType}");
                    }
                }
            }

            if (vkey != null)
            {
                vkey.StartPosition = FormStartPosition.Manual;
                vkey.Size = new Size(UserSettings.TheSettings.VirtualKeyboardInfo.Width, UserSettings.TheSettings.VirtualKeyboardInfo.Height);
                vkey.TopMost = false;
                vkey.Location = new Point(UserSettings.TheSettings.VirtualKeyboardInfo.X, UserSettings.TheSettings.VirtualKeyboardInfo.Y);
                vkey.Show();
            }
        }

        /// <summary>
        /// Dispose of all current devices.
        /// </summary>
        void DestroyDevices()
        {
            // Save the vkbd position.
            _inputDevices.Values.Where(v => v.GetType() == typeof(Keyboard)).ForEach(k =>
                UserSettings.TheSettings.VirtualKeyboardInfo = FromForm(k as Keyboard));

            _inputDevices.ForEach(i => 
            {
                i.Value.DeviceInputEvent -= Device_InputEvent;
                i.Value?.Stop();
                i.Value?.Dispose();
            });
            _inputDevices.Clear();

            _outputDevices.ForEach(o =>
            {
                o.Value?.Stop();
                o.Value?.Dispose();
            });
            _outputDevices.Clear();
        }
        #endregion

        #region Compile
        /// <summary>
        /// Master compiler function.
        /// </summary>
        bool Compile()
        {
            bool ok = true;

            if (_fn == Definitions.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                Compiler compiler = new() { Min = false };

                // Clean up any old.
                _script?.Dispose();

                // Compile script now.
                _script = compiler.Execute(_fn);

                // Update file watcher - keeps an eye on any included files too.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                // Time points.
                timeMaster.TimeDefs.Clear();

                // Process errors. Some may be warnings.
                _compileResults = compiler.Errors;
                int errorCount = _compileResults.Count(w => w.ErrorType == ScriptErrorType.Error);

                if (errorCount == 0 && _script != null)
                {
                    SetCompileStatus(true);
                    _compileTempDir = compiler.TempDir;

                    // Note: Need exception handling here to protect from user script errors.
                    try
                    {
                        // Devices have been specified in project config - create now.
                        CreateDevices();

                        // Surface area.
                        InitRuntime();

                        // Setup scrript.
                        _script.Setup();

                        // Script may have altered shared values.
                        ProcessRuntime();

                        // Build all the steps.
                        _script.BuildSteps();

                        ///// Init the timeclock.
                        timeMaster.TimeDefs = _script.GetSectionMarkers();
                        timeMaster.MaxBeat = timeMaster.TimeDefs.Keys.Max();

                        ///// Init other controls.
                        potSpeed.Value = Convert.ToInt32(_config.MasterSpeed);
                        double mv = Convert.ToDouble(_config.MasterVolume);
                        sldVolume.Value = mv == 0 ? 90 : mv; // in case it's new

                        SetSpeedTimerPeriod();
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

                _compileResults.ForEach(r =>
                {
                    if (r.ErrorType == ScriptErrorType.Warning)
                    {
                        _logger.Warn(r.ToString());
                    }
                    else
                    {
                        _logger.Error(r.ToString());
                    }
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

        string GetConfigFileName(string scriptFileName)
        {
            string cfn = null;

            foreach (string s in File.ReadAllLines(scriptFileName))
            {
                if (s.Trim().StartsWith("Config"))
                {
                    List<string> parts = s.SplitByTokens("\"");
                    if (parts.Count == 3)
                    {
                        cfn = parts[1];
                        break;
                    }
                }
            }

            return cfn;
        }



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
            BeginInvoke((MethodInvoker) delegate()
            {
                if (_script != null)
                {
                    NextStep();
                }
            });
        }

        /// <summary>
        /// Output next time/step.
        /// </summary>
        void NextStep()
        {
            ////// Neb steps /////
            InitRuntime();

            if (chkPlay.Checked && !_needCompile)
            {
                //_tan.Arm();

                // Update section - only on beats.
                if (timeMaster.TimeDefs.Count > 0 && _stepTime.Subdiv == 0)
                {
                    if(timeMaster.TimeDefs.ContainsKey(_stepTime.Beat))
                    {
                        // currentSection = _stepTime.Beat;
                    }
                }

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
                bool anySolo = _config.Channels.Where(t => t.State == ChannelState.Solo).Count() > 0;
                bool anyMute = _config.Channels.Where(t => t.State == ChannelState.Mute).Count() > 0;
                lblSolo.BackColor = anySolo ? Color.Pink : SystemColors.Control;
                lblMute.BackColor = anyMute ? Color.Pink : SystemColors.Control;

                var steps = _script.GetSteps(_stepTime);

                foreach(var step in steps)
                {
                    Channel channel = _config.Channels.Where(t => t.ChannelNumber == step.ChannelNumber).First();

                    // Is it ok to play now?
                    bool play = channel != null && (channel.State == ChannelState.Solo || (channel.State == ChannelState.Normal && !anySolo));

                    if (play)
                    {
                        switch(step)
                        {
                            case StepFunction sf:
                                // Note: Need exception handling here to protect from user script errors.
                                try
                                {
                                    sf.ScriptFunction();
                                }
                                catch (Exception ex)
                                {
                                    ProcessScriptRuntimeError(ex);
                                }
                                break;

                            default:
                                if (step.Device is IOutputDevice)
                                {
                                    // Maybe tweak values.
                                    if (step is StepNoteOn)
                                    {
                                        (step as StepNoteOn).Adjust(sldVolume.Value, channel.Volume);
                                    }
                                    (step.Device as IOutputDevice).Send(step);
                                }
                                break;
                        }
                    }
                }

                ///// Bump time.
                _stepTime.Advance();

                // Check for end of play. If no steps or not selected, free running mode so always keep going.
                if(timeMaster.TimeDefs.Count() > 1)
                {
                   // Check for end.
                   if (_stepTime.Beat > timeMaster.TimeDefs.Last().Key)
                   {
                       ProcessPlay(PlayCommand.StopRewind);
                        _outputDevices.ForEach(o => o.Value?.Kill()); // just in case
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
        void Device_InputEvent(object sender, DeviceInputEventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate()
            {
                if (_script != null && e.Step != null)
                {
                    if (e.Step is StepNoteOn || e.Step is StepNoteOff)
                    {
                        // Dig out the note number. !!! Note sign specifies note on/off.
                        double value = (e.Step is StepNoteOn) ? (e.Step as StepNoteOn).NoteNumber : -(e.Step as StepNoteOff).NoteNumber;
                        _script.InputHandler((sender as IInputDevice).DeviceType, e.Step.ChannelNumber, value);
                    }
                    else if (e.Step is StepControllerChange)
                    {
                        // Control change.
                        StepControllerChange scc = e.Step as StepControllerChange;
                        _script.InputHandler((sender as IInputDevice).DeviceType, e.Step.ChannelNumber, scc.ControllerId); // also needs value TODO1
                    }
                }
            });
        }

        /// <summary>
        /// User has changed a channel value. Interested in solo/mute and volume.
        /// </summary>
        void ChannelChange_Event(object sender, EventArgs e) //TODO1 the rest...
        {
            //ChannelControl cc = sender as ChannelControl;
            
            // Channel ch = cc.BoundChannel;  eliminate?

            

            // Save values.
            // _nppVals.SetValue(ch.BoundChannel.Name, "volume", ch.BoundChannel.Volume);

            // Kill any not solo. ???
            //_script.Channels.Where(c => c.State != ChannelState.Solo).ForEach(c => c.Device?.Kill(c.ChannelNumber));
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
            _script.Volume = sldVolume.Value;
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (_script.Speed != potSpeed.Value)
            {
                potSpeed.Value = _script.Speed;
                SetSpeedTimerPeriod();
            }

            if (_script.Volume != sldVolume.Value)
            {
                sldVolume.Value = _script.Volume;
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="ex"></param>
        ScriptError ProcessScriptRuntimeError(Exception ex)
        {
            ScriptError err;

            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            // Locate the offending frame.
            string srcFile;
            int srcLine = -1;
            StackTrace st = new StackTrace(ex, true);
            StackFrame sf = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame stf = st.GetFrame(i);
                if (stf.GetFileName() != null && stf.GetFileName().ToUpper().Contains(_compileTempDir.ToUpper()))
                {
                    sf = stf;
                    break;
                }
            }

            if (sf != null)
            {
                // Dig out generated file parts.
                string genFile = sf.GetFileName();
                int genLine = sf.GetFileLineNumber() - 1;

                // Open the generated file and dig out the source file and line.
                string[] genLines = File.ReadAllLines(genFile);

                srcFile = genLines[0].Trim().Replace("//", "");

                int ind = genLines[genLine].LastIndexOf("//");
                if (ind != -1)
                {
                    string sl = genLines[genLine].Substring(ind + 2);
                    int.TryParse(sl, out srcLine);
                }

                err = new ScriptError()
                {
                    ErrorType = ScriptErrorType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = ex.Message
                };
            }
            else // unknown?
            {
                err = new ScriptError()
                {
                    ErrorType = ScriptErrorType.Runtime,
                    SourceFile = "",
                    LineNumber = -1,
                    Message = ex.Message
                };
            }

            if (err != null)
            {
                _logger.Error(err.ToString());
            }

            return err;
        }
        #endregion

        #region File handling
        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object sender, EventArgs e)
        {
            string fn = sender.ToString();
            string sopen = OpenFile(fn);
            if (sopen != "")
            {
                _logger.Error(sopen);
            }
        }

        /// <summary>
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "Nebulator files | *.neb",
                Title = "Select a Nebulator file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                string sopen = OpenFile(openDlg.FileName);
                if (sopen != "")
                {
                    _logger.Error(sopen);
                }
            }
        }

        /// <summary>
        /// Common np file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        public string OpenFile(string fn)
        {
            string ret = "";

            using (new WaitCursor())
            {
                try
                {
                    if (File.Exists(fn))
                    {
                        _logger.Info($"Opening {fn}");
                        _fn = fn;

                        // Get the config and set things up.
                        DestroyChannelControls();
                        DestroyDevices();

                        _config = null;

                        string cfigfn = GetConfigFileName(fn);

                        if(cfigfn is not null)
                        {
                            // Check absolute.
                            if(File.Exists(cfigfn))
                            {
                                _config = Config.Load(cfigfn);
                            }
                            else // Check local.
                            {
                                string local = Path.GetFullPath(fn);
                                local = Path.Combine(local, cfigfn);

                                if(File.Exists(local))
                                {
                                    _config = Config.Load(local);
                                }
                            }
                        }

                        if(_config is null)
                        {
                            _logger.Error("Couldn't load config file: {cfigfn}");
                            SetCompileStatus(false);
                            Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";
                        }
                        else
                        {
                            CreateDevices();
                            CreateChannelControls();

                            AddToRecentDefs(fn);
                            bool ok = Compile();
                            SetCompileStatus(ok);

                            Text = $"Nebulator {MiscUtils.GetVersionString()} - {fn}";
                        }
                    }
                    else
                    {
                        ret = $"Invalid script file: {fn}";
                        SetCompileStatus(false);
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
                ToolStripMenuItem menuItem = new ToolStripMenuItem(f, null, new EventHandler(Recent_Click));
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
        void Watcher_Changed(object sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker) delegate()
            {
                if(UserSettings.TheSettings.AutoCompile)
                {
                    Compile();
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
        void Play_Click(object sender, EventArgs e)
        {
            ProcessPlay(chkPlay.Checked ? PlayCommand.Start : PlayCommand.Stop);
        }

        /// <summary>
        /// Update multimedia timer period.
        /// </summary>
        void Speed_ValueChanged(object sender, EventArgs e)
        {
            _config.MasterSpeed = potSpeed.Value;
            SetSpeedTimerPeriod();
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        void Rewind_Click(object sender, EventArgs e)
        {
            ProcessPlay(PlayCommand.Rewind);
        }

        /// <summary>
        /// User updated volume.
        /// </summary>
        void Volume_ValueChanged(object sender, EventArgs e)
        {
            _config.MasterVolume = sldVolume.Value;
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        void Compile_Click(object sender, EventArgs e)
        {
            Compile();
            ProcessPlay(PlayCommand.StopRewind);
        }

        /// <summary>
        /// User updated the time.
        /// </summary>
        void Time_ValueChanged(object sender, EventArgs e)
        {
            _stepTime = timeMaster.CurrentTime;
            ProcessPlay(PlayCommand.UpdateUiTime);
        }

        /// <summary>
        /// Monitor comm messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        void Mon_Click(object sender, EventArgs e)
        {
            UserSettings.TheSettings.MonitorInput = btnMonIn.Checked;
            UserSettings.TheSettings.MonitorOutput = btnMonOut.Checked;
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object sender, EventArgs e)
        {
            // Make some markdown.
            List<string> mdText = new List<string>
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
            if (AsioOut.GetDriverNames().Count() > 0)
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

            FileInfo fi = new FileInfo(Path.Combine(appDir, "log.txt"));
            if(fi.Exists && fi.Length > 100000)
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
            BeginInvoke((MethodInvoker) delegate()
            {
                textViewer.AddLine(msg);
            });
        }

        /// <summary>
        /// Show the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogShow_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "Log Viewer",
                Size = new Size(900, 600),
                BackColor = UserSettings.TheSettings.BackColor,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                TextViewer tv = new TextViewer()
                {
                    Dock = DockStyle.Fill,
                    WordWrap = true
                };

                f.Controls.Add(tv);
                tv.Colors.Add(" ERROR ", Color.Plum);
                tv.Colors.Add(" _WARN ", Color.LightPink);
                //tv.Colors.Add(" SND???:", Color.LightGreen);

                string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
                string logFileName = Path.Combine(appDir, "log.txt");
                using (new WaitCursor())
                {
                    File.ReadAllLines(logFileName).ForEach(l => tv.AddLine(l));
                }

                f.ShowDialog();
            }
        }
        #endregion

        #region User settings
        /// <summary>
        /// Save user settings that aren't automatic.
        /// </summary>
        void SaveSettings()
        {
            UserSettings.TheSettings.MainFormInfo = FromForm(this);
            UserSettings.TheSettings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void UserSettings_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "User Settings",
                Size = new Size(350, 400),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                PropertyGrid pg = new PropertyGrid()
                {
                    Dock = DockStyle.Fill,
                    PropertySort = PropertySort.NoSort,
                    SelectedObject = UserSettings.TheSettings
                };

                // Detect changes of interest.
                bool ctrls = false;
                pg.PropertyValueChanged += (sdr, args) =>
                {
                    string p = args.ChangedItem.PropertyDescriptor.Name;
                    ctrls |= (p.Contains("Font") | p.Contains("Color")); //TODO0 fix
                };

                f.Controls.Add(pg);
                f.ShowDialog();

                // Figure out what changed - each handled differently.
                if (ctrls)
                {
                    MessageBox.Show("UI changes require a restart to take effect.");
                }

                SaveSettings();
            }
        }

        /// <summary>
        /// Helper for serialization.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public FormInfo FromForm(Form f)
        {
            return new()
            {
                X = f.Location.X,
                Y = f.Location.Y,
                Width = f.Width,
                Height = f.Height
            };
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
                    bool ok = !_needCompile || Compile();
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
                    _outputDevices.ForEach(o => o.Value?.Kill());
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
        void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
        }
        #endregion

        #region Timers
        /// <summary>
        /// Common func.
        /// </summary>
        void SetSpeedTimerPeriod()
        {
            // Make a transformer.
            MidiTime mt = new MidiTime()
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
        void ExportMidi_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                Filter = "Midi files (*.mid)|*.mid",
                Title = "Export to midi file",
                FileName = Path.GetFileName(_fn.Replace(".neb", ".mid"))
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

            if(_script != null)
            {
                if(_needCompile)
                {
                    ok = Compile();
                }
            }
            else
            {
                ok = false;
            }

            if(ok)
            {
                Dictionary<int, string> channels = new Dictionary<int, string>();
                _config.Channels.ForEach(t => channels.Add(t.ChannelNumber, t.ChannelName));
                MidiUtils.ExportToMidi(_script.GetAllSteps(), fn, channels, potSpeed.Value, "Converted from " + _fn);
            }
        }

        /// <summary>
        /// Kill em all.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Kill_Click(object sender, EventArgs e)
        {
            _outputDevices.ForEach(o => o.Value?.Kill());
        }
        #endregion
    }
}