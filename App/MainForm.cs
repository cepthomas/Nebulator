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
        MmTimerEx _mmTimer = new();

        /// <summary>The current script.</summary>
        ScriptBase? _script = null;

        /// <summary>Persisted internal values for current script file.</summary>
        Config? _config = null;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        Time _stepTime = new();

        /// <summary>Current np file name.</summary>
        string _scriptFileName = Definitions.UNKNOWN_STRING;

        /// <summary>Detect changed script files. TODO1 also config files.</summary>
        readonly MultiFileWatcher _watcher = new();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for compile products.</summary>
        string _compileTempDir = "";

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Devices to use for send.</summary>
        readonly Dictionary<DeviceType, IOutputDevice> _outputDevices = new();//TODO2 remove/consolidate?

        /// <summary>Devices to use for receive.</summary>
        readonly Dictionary<DeviceType, IInputDevice> _inputDevices = new();//TODO2 remove/consolidate?
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

            // Keyboard.
            var kbd = new Keyboard(); //TODO2 useful? and/or replace with something else? x/y surface
            kbd.DeviceInputEvent += Device_InputEvent;
            _inputDevices.Add(kbd.DeviceType, kbd);
            kbd.Visible = UserSettings.TheSettings.Keyboard;
            btnKeyboard.Click += (object? _, EventArgs __) => { kbd.Visible = btnKeyboard.Checked; UserSettings.TheSettings.Keyboard = btnKeyboard.Checked; };

            // For testing.
            lblSolo.Hide();
            lblMute.Hide();

            btnWrap.Checked = UserSettings.TheSettings.WordWrap;
            textViewer.WordWrap = btnWrap.Checked;
            btnWrap.Click += (object? _, EventArgs __) => { textViewer.WordWrap = btnWrap.Checked; UserSettings.TheSettings.WordWrap = btnWrap.Checked; };

            btnKillComm.Click += (object? _, EventArgs __) => { Kill(); };
            btnClear.Click += (object? _, EventArgs __) => { textViewer.Clear(); };
            #endregion

            InitLogging();

            _logger.Info("============================ Starting up ===========================");

            PopulateRecentMenu();

            ScriptDefinitions.TheDefinitions.Init();//TODO1

            // Fast mm timer.
            SetFastTimerPeriod();
            _mmTimer.Start();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChangeEvent += Watcher_Changed;

            Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";

            //Config.MakeFake(@"..\..\..\fake.nebcfig");

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
            UserSettings.TheSettings.Save();

            UnloadConfig();

            // DestroyDevices()

            // Destroy devices.
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


            _script?.Dispose();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_Resize(object? sender, EventArgs e)
        {
            UserSettings.TheSettings.MainFormInfo =
                new()
                {
                    X = Location.X,
                    Y = Location.Y,
                    Width = Width,
                    Height = Height
                };
        }

        #endregion

        #region Compile
        /// <summary>
        /// Compile the neb script itself. Config is handled separately.
        /// </summary>
        bool CompileScript()
        {
            bool ok = true;

            if (_scriptFileName == Definitions.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else if (_config is null || !_config.Valid)
            {
                _logger.Warn("Invalid config.");
                ok = false;
            }
            else
            {
                // Clean up any old.
                _script?.Dispose();

                // Compile script now.
                Compiler compiler = new();
                _script = compiler.Execute(_scriptFileName, _config);

                // Update file watcher - keeps an eye on any included files too.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                // Time points.
                timeMaster.TimeDefs.Clear();

                // Process errors. Some may be warnings.
                if (compiler.ErrorCount == 0 && _script is not null)
                {
                    _script.Init(_config.Channels);

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
                        case ScriptResultType.Fatal:
                        case ScriptResultType.Error:
                        case ScriptResultType.Runtime:
                            _logger.Error(r.ToString());
                            break;

                        case ScriptResultType.Warning:
                            _logger.Warn(r.ToString());
                            break;

                        case ScriptResultType.None:
                            _logger.Info(r.ToString());
                            break;
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

        #region Config management
        /// <summary>
        /// Load the runtime stuff from the config file.
        /// </summary>
        /// <param name="cfigfn"></param>
        void LoadConfig(string cfigfn)
        {
            if (_config is not null)
            {
                UnloadConfig();
            }

            _config = Config.Load(cfigfn);

            if (_config is not null)
            {
                // Create devices.
                _config.Valid = CreateDevices();

                if (_config.Valid)
                {
                    // Create controls.
                    _config.Valid = CreateChannels();

                    // Init other controls.
                    potSpeed.Value = Convert.ToInt32(_config.MasterSpeed);
                    double mv = Convert.ToDouble(_config.MasterVolume);
                    sldVolume.Value = mv == 0 ? 90 : mv; // in case it's new
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void UnloadConfig()
        {
            ///// Destroy controls.
            foreach (Control ctl in Controls)
            {
                if (ctl is ChannelControl)
                {
                    ChannelControl? tctl = ctl as ChannelControl;
                    tctl?.Dispose();
                    Controls.Remove(tctl);
                }
            }

            _config?.Save();
            _config = null;
        }

        /// <summary>
        /// Create the channel controls from the user script config.
        /// </summary>
        bool CreateChannels()
        {
            bool ok = true;

            if (_config is not null)
            {
                const int CONTROL_SPACING = 10;
                int x = btnRewind.Left; // timeMaster.Right + CONTROL_SPACING;
                int y = timeMaster.Bottom + CONTROL_SPACING;

                // Create new channel controls.
                foreach (Channel t in _config.Channels)
                {


                    if (_outputDevices.TryGetValue(t.DeviceType, out IOutputDevice? dev))
                    {
                        t.Device = dev;

                        ChannelControl tctl = new()
                        {
                            //Name = t.DeviceType.ToString(),
                            Location = new Point(x, y),
                            BoundChannel = t,
                        };
                        Controls.Add(tctl);

                        x += tctl.Width + CONTROL_SPACING;
                    }
                    //else

                    if (dev is not null)
                    {
                        _logger.Error($"Invalid device {t.DeviceType} for channel {t.ChannelName}");
                        ok = false;
                        break;
                    }



                }
            }

            return ok;
        }

        /// <summary>
        /// Create all devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices()
        {
            bool ok = true;

            // Virtual keyboards has already been added.

            // TODO1 clean these up.
            if (UserSettings.TheSettings.MidiIn != "None" && UserSettings.TheSettings.MidiIn != "")
            {
                var nin = new MidiInput();
                if (nin.Init())
                {
                    nin.DeviceInputEvent += Device_InputEvent;
                    _inputDevices.Add(nin.DeviceType, nin);
                }
                else
                {
                    _logger.Error($"Failed to init input device:{nin.DeviceType}");
                    ok = false;
                }
            }

            if (UserSettings.TheSettings.MidiOut != "None" && UserSettings.TheSettings.MidiOut != "")
            {
                var nin = new MidiOutput();
                if (nin.Init())
                {
                    //nin.DeviceInputEvent += Device_InputEvent;
                    _outputDevices.Add(nin.DeviceType, nin);
                }
                else
                {
                    _logger.Error($"Failed to init input device:{nin.DeviceType}");
                    ok = false;
                }

            }

            if (UserSettings.TheSettings.OscIn != "None" && UserSettings.TheSettings.OscIn != "")
            {
                var nin = new OscInput();
                if (nin.Init())
                {
                    nin.DeviceInputEvent += Device_InputEvent;
                    _inputDevices.Add(nin.DeviceType, nin);
                }
                else
                {
                    _logger.Error($"Failed to init input device:{nin.DeviceType}");
                    ok = false;
                }

            }

            if (UserSettings.TheSettings.OscOut != "None" && UserSettings.TheSettings.OscOut != "")
            {
                var nin = new OscOutput();
                if (nin.Init())
                {
                    //nin.DeviceInputEvent += Device_InputEvent;
                    _outputDevices.Add(nin.DeviceType, nin);
                }
                else
                {
                    _logger.Error($"Failed to init input device:{nin.DeviceType}");
                    ok = false;
                }

            }

            //bool local(IDevice nin)
            //{
            //    bool ok = true;

            //    if (nin.Init())
            //    {
            //        nin.DeviceInputEvent += Device_InputEvent;
            //        _inputDevices.Add(nin.DeviceType, nin);
            //    }
            //    else
            //    {
            //        _logger.Error($"Failed to init input device:{nin.DeviceType}");
            //        ok = false;
            //    }
            //    return ok;
            //}

            return ok;
        }

        /*
                    // Get requested inputs. Hook to the device.
                    foreach (Controller con in _config.Controllers)
                    {
                        // Have we seen it yet?
                        if (_inputDevices.ContainsKey(con.DeviceType))
                        {
                            con.Device = _inputDevices[con.DeviceType];
                        }
                        else // nope
                        {
                            IInputDevice nin = con.Device;
                            switch (con.DeviceType)
                            {
                                case DeviceType.MidiIn:
                                    nin = new MidiInput();
                                    break;

                                case DeviceType.OscIn:
                                    nin = new OscInput();
                                    break;

                                case DeviceType.Vkey:
                                    // var kbd = new Keyboard //TODO2 useful? or replace with something else? x/y surface
                                    // {
                                    //     StartPosition = FormStartPosition.Manual,
                                    //     Size = new Size(UserSettings.TheSettings.KeyboardInfo.Width, UserSettings.TheSettings.KeyboardInfo.Height),
                                    //     TopMost = false,
                                    //     Location = new Point(UserSettings.TheSettings.KeyboardInfo.X, UserSettings.TheSettings.KeyboardInfo.Y)
                                    // };
                                    // kbd.Show();
                                    // nin = kbd;
                                    nin = null;
                                    break;
                            }

                            // Finish it up.
                            if (nin is not null)
                            {
                                if (nin.Init())
                                {
                                    nin.DeviceInputEvent += Device_InputEvent;
                                    _inputDevices.Add(nin.DeviceType, nin);
                                }
                                else
                                {
                                    _logger.Error($"Failed to init input device:{con.ControllerName}");
                                    ok = false;
                                }
                            }
                            else
                            {
                                _logger.Error($"Invalid input device for {con.ControllerName}");
                                ok = false;
                            }
                        }
                    }

                    // Get requested outputs. Hook to the device.
                    foreach (Channel chan in _config.Channels)
                    {
                        // Have we seen it yet?
                        if (_outputDevices.ContainsKey(chan.DeviceType))
                        {
                            chan.Device = _outputDevices[chan.DeviceType];
                        }
                        else // nope
                        {
                            IOutputDevice nout = null;

                            switch (chan.DeviceType)
                            {
                                case DeviceType.MidiOut:
                                    nout = new MidiOutput();
                                    break;

                                case DeviceType.OscOut:
                                    nout = new OscOutput();
                                    break;
                            }

                            // Finish it up.
                            if (nout is not null)
                            {
                                if (nout.Init())
                                {
                                    chan.Device = nout;
                                    _outputDevices.Add(chan.DeviceType, nout);
                                }
                                else
                                {
                                    _logger.Error($"Failed to init channel: {chan.ChannelName}");
                                    ok = false;
                                }
                            }
                            else
                            {
                                _logger.Error($"Invalid output device for {chan.ChannelName}");
                                ok = false;
                            }
                        }
                    }

                    return ok;
                }
        */

        /// <summary>
        /// Get the config file name from the script and validate.
        /// </summary>
        /// <param name="scriptFileName"></param>
        /// <returns></returns>
        string? GetConfigFileName(string scriptFileName)
        {
            string? cfigfn = null;

            foreach (string s in File.ReadAllLines(scriptFileName))
            {
                if (s.Trim().StartsWith("Config"))
                {
                    List<string> parts = s.SplitByTokens("\"");
                    if (parts.Count == 3)
                    {
                        cfigfn = parts[1];
                        break;
                    }
                }
            }

            //if (cfigfn is not null)
            //{
            //    // Check absolute.
            //    if (File.Exists(cfigfn))
            //    {
            //        // ok
            //    }
            //    else // Check local.
            //    {
            //        string local = Path.GetDirectoryName(scriptFileName);
            //        local = Path.Combine(local, cfigfn);

            //        if (File.Exists(local))
            //        {
            //            cfigfn = local;
            //        }
            //        else
            //        {
            //            cfigfn = null;
            //        }
            //    }
            //}

            return cfigfn;
        }

        /// <summary>
        /// Edit the script config in a property grid.
        /// </summary>
        void Config_Click(object sender, EventArgs e)
        {
            if (_config is not null)
            {
                using Form f = new()
                {
                    Text = "Config File",
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
                    SelectedObject = _config
                };

                // Detect changes of interest.
                bool changed = false;
                pg.PropertyValueChanged += (sdr, args) => { changed = true; };

                f.Controls.Add(pg);
                f.ShowDialog();

                if (changed)
                {
                    string cfn = _config.FileName;
                    _config.Save();
                    UnloadConfig();
                    LoadConfig(cfn);
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

            if (chkPlay.Checked && !_needCompile)
            {
                //_tan.Arm();

                // Update section - only on beats.
                if (timeMaster.TimeDefs.Count > 0 && _stepTime.Subdiv == 0)
                {
                    if (timeMaster.TimeDefs.ContainsKey(_stepTime.Beat))
                    {
                        // TODO1? currentSection = _stepTime.Beat;
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
                bool anySolo = _config.Channels.Where(t => t.State == ChannelState.Solo).Any();
                bool anyMute = _config.Channels.Where(t => t.State == ChannelState.Mute).Any();
                lblSolo.BackColor = anySolo ? Color.Pink : SystemColors.Control;
                lblMute.BackColor = anyMute ? Color.Pink : SystemColors.Control;

                var steps = _script.GetSteps(_stepTime);

                foreach (var step in steps)
                {
                    Channel channel = _config.Channels.Where(t => t.ChannelNumber == step.ChannelNumber).First();

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
        void Device_InputEvent(object sender, DeviceInputEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
           {
               if (_script is not null && e.Step is not null)
               {
                   var dev = sender as IInputDevice;
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
        ScriptResult ProcessScriptRuntimeError(Exception ex)
        {
            ScriptResult err;

            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            // Locate the offending frame.
            string srcFile;
            int srcLine = -1;
            StackTrace st = new(ex, true);
            StackFrame sf = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame stf = st.GetFrame(i);
                if (stf.GetFileName() is not null && stf.GetFileName().ToUpper().Contains(_compileTempDir.ToUpper()))
                {
                    sf = stf;
                    break;
                }
            }

            if (sf is not null)
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
                    string sl = genLines[genLine][(ind + 2)..];
                    srcLine = int.Parse(sl);
                }

                err = new ScriptResult()
                {
                    ResultType = ScriptResultType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = ex.Message
                };
            }
            else // unknown?
            {
                err = new ScriptResult()
                {
                    ResultType = ScriptResultType.Runtime,
                    SourceFile = "",
                    LineNumber = -1,
                    Message = ex.Message
                };
            }

            if (err is not null)
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
            string sopen = OpenScriptFile(fn);
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
                    UnloadConfig();

                    if (File.Exists(fn))
                    {
                        _logger.Info($"Opening {fn}");
                        _scriptFileName = fn;

                        // Get the config and set things up.
                        string cfigfn = GetConfigFileName(fn);
                        if (cfigfn is not null)
                        {
                            cfigfn = Path.Combine(UserSettings.TheSettings.WorkPath, cfigfn);

                            LoadConfig(cfigfn);

                            if (_config is not null && _config.Valid)
                            {
                                AddToRecentDefs(fn);
                                bool ok = CompileScript();
                                SetCompileStatus(ok);

                                Text = $"Nebulator {MiscUtils.GetVersionString()} - {fn}";
                            }
                            else
                            {
                                _logger.Error($"Couldn't load config file: {cfigfn}");
                                SetCompileStatus(false);
                                Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";
                            }
                        }
                        else
                        {
                            _logger.Error($"Invalid config file in script");
                            SetCompileStatus(false);
                            Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";
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
        void Watcher_Changed(object sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
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
            SetFastTimerPeriod();
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
            CompileScript();
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
            BeginInvoke((MethodInvoker)delegate ()
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
                WordWrap = true
            };

            f.Controls.Add(tv);
            tv.Colors.Add(" E ", Color.LightPink);
            tv.Colors.Add(" W ", Color.Plum);
            //tv.Colors.Add(" SND???:", Color.LightGreen);

            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            string logFileName = Path.Combine(appDir, "log.txt");
            using (new WaitCursor())
            {
                File.ReadAllLines(logFileName).ForEach(l => tv.AddLine(l));//TODO2 doesn't work.
            }

            f.ShowDialog();
        }
        #endregion

        #region User settings
        ///// <summary>
        ///// Save user settings that aren't automatic.
        ///// </summary>
        //void SaveSettings()
        //{
        //    UserSettings.TheSettings.MainFormInfo = FromForm(this);

        //    UserSettings.TheSettings.Save();
        //}

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void UserSettings_Click(object sender, EventArgs e)
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
                MessageBox.Show("Settings changes require a restart to take effect.");
                SaveSettings();
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
        void MainForm_KeyDown(object sender, KeyEventArgs e)
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
        void ExportMidi_Click(object sender, EventArgs e)
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

            if (_script is not null)
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
                _config.Channels.ForEach(t => channels.Add(t.ChannelNumber, t.ChannelName));
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
