using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using NAudio.Wave;
using NBagOfTricks;
using NBagOfTricks.ScriptCompiler;
using NBagOfTricks.Slog;
using NBagOfUis;
using MidiLib;
using Nebulator.Script;


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
        readonly Logger _logger = LogManager.CreateLogger("Main");

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Current np file name.</summary>
        string _scriptFileName = "";

        /// <summary>The current script.</summary>
        ScriptBase? _script = new();

        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel> _channels = new();

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Longest length of channels in subdivs.</summary>
        int _totalSubdivs = 0;

        /// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        readonly Dictionary<string, IOutputDevice> _outputDevices = new();

        /// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        readonly Dictionary<string, IInputDevice> _inputDevices = new();

        /// <summary>Persisted internal values for current script file.</summary>
        Bag _nppVals = new();

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        BarTime _stepTime = new();

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for compile products.</summary>
        string _compileTempDir = "";

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            UserSettings.TheSettings = (UserSettings)Settings.Load(appDir, typeof(UserSettings));
            MidiSettings.LibSettings = UserSettings.TheSettings.MidiSettings;
            // Force the resolution for this application.
            MidiSettings.LibSettings.InternalPPQ = BarTime.LOW_RES_PPQ;

            InitializeComponent();

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = UserSettings.TheSettings.FileLogLevel;
            LogManager.MinLevelNotif = UserSettings.TheSettings.NotifLogLevel;
            LogManager.LogEvent += LogManager_LogEvent;
            LogManager.Run(logFileName, 100000);

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
            textViewer.MatchColors.Add("ERR", Color.LightPink);
            textViewer.MatchColors.Add("WRN", Color.Plum);
            textViewer.Prompt = "> ";

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKillComm.Image, UserSettings.TheSettings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap((Bitmap)fileDropDownButton.Image, UserSettings.TheSettings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image, UserSettings.TheSettings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, UserSettings.TheSettings.IconColor);
            btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image, UserSettings.TheSettings.IconColor);
            btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image, UserSettings.TheSettings.IconColor);

            btnMonIn.Checked = UserSettings.TheSettings.MonitorInput;
            btnMonOut.Checked = UserSettings.TheSettings.MonitorOutput;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            sldTempo.DrawColor = UserSettings.TheSettings.ControlColor;
            sldTempo.Invalidate();
            sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
            sldVolume.Invalidate();

            // Time controller.
            barBar.ProgressColor = UserSettings.TheSettings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;
            barBar.Invalidate();

            textViewer.WordWrap = UserSettings.TheSettings.WordWrap;

            btnKillComm.Click += (object? _, EventArgs __) => { KillAll(); };
            #endregion
        }

        /// <summary>
        /// Post create init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _logger.Info("============================ Starting up ===========================");

            PopulateRecentMenu();

            bool ok = CreateDevices();

            if(ok)
            {
                // Fast mm timer.
                SetFastTimerPeriod();
                _mmTimer.Start();

                KeyPreview = true; // for routing kbd strokes properly

                _watcher.FileChangeEvent += Watcher_Changed;

                Text = $"Nebulator {MiscUtils.GetVersionString()} - No file loaded";

                // Look for filename passed in.
                string sopen = "";
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
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            ProcessPlay(PlayCommand.Stop);

            // Just in case.
            KillAll();

            // Save user settings.
            UserSettings.TheSettings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            UserSettings.TheSettings.WordWrap = textViewer.WordWrap;

            UserSettings.TheSettings.Save();

            SaveProjectValues();

            base.OnFormClosing(e);
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

                // Wait a bit in case there are some lingering events.
                System.Threading.Thread.Sleep(100);

                DestroyDevices();

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
            _nppVals.SetValue("master", "speed", sldTempo.Value);
            _nppVals.SetValue("master", "volume", sldVolume.Value);

            _channels.Values.ForEach(ch =>
            {
                if(ch.NumEvents > 0)
                {
                    _nppVals.SetValue(ch.ChannelName, "volume", ch.Volume);
                    _nppVals.SetValue(ch.ChannelName, "state", ch.State);
                }
            });

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

            if (_scriptFileName == "")
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                // Clean up old script stuff.
                ProcessPlay(PlayCommand.StopRewind);
                // Clean out our current elements.
                _channelControls.ForEach(c =>
                {
                    Controls.Remove(c);
                    c.Dispose();
                });
                _channelControls.Clear();
                _channels.Clear();
                _watcher.Clear();
                _totalSubdivs = 0;
                barBar.Reset();

                // Compile script.
                Compiler compiler = new();
                compiler.CompileScript(_scriptFileName);
                _script = compiler.Script as ScriptBase;

                // Process errors. Some may only be warnings.
                ok = !compiler.Results.Any(w => w.ResultType == CompileResultType.Error) && _script is not null;

                if (ok)
                {
                    SetCompileStatus(true);
                    _compileTempDir = compiler.TempDir;

                    // Need exception handling here to protect from user script errors.
                    try
                    {
                        // Init shared vars.
                        InitRuntime();

                        // Setup script. This builds the sequences and sections.
                        _script!.Setup();

                        // Script may have altered shared values.
                        ProcessRuntime();
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(ex);
                        ok = false;
                    }
                }

                ///// Script is sane - build the channels and UI.
                if (ok)
                {
                    // Create channels and controls from event sets.
                    const int CONTROL_SPACING = 10;
                    int x = btnRewind.Left;
                    int y = barBar.Bottom + CONTROL_SPACING;

                    foreach (var chspec in compiler.ChannelSpecs)
                    {
                        // Make new channel.
                        Channel channel = new()
                        {
                            ChannelName = chspec.ChannelName,
                            ChannelNumber = chspec.ChannelNumber,
                            DeviceId = chspec.DeviceId,
                            Volume = _nppVals.GetDouble(chspec.ChannelName, "volume", MidiLibDefs.VOLUME_DEFAULT),
                            State = (ChannelState)_nppVals.GetInteger(chspec.ChannelName, "state", (int)ChannelState.Normal),
                            Patch = chspec.Patch,
                            IsDrums = chspec.IsDrums,
                            Selected = false,
                            Device = _outputDevices[chspec.DeviceId],
                            AddNoteOff = true
                        };
                        _channels.Add(chspec.ChannelName, channel);

                        // Make new control and bind to channel.
                        ChannelControl control = new()
                        {
                            Location = new(x, y),
                            BorderStyle = BorderStyle.FixedSingle,
                            BoundChannel = channel
                        };
                        control.ChannelChangeEvent += ChannelControl_ChannelChangeEvent;
                        Controls.Add(control);
                        _channelControls.Add(control);

                        // Good time to send initial patch.
                        channel.SendPatch();

                        // Adjust positioning for next iteration.
                        y += control.Height + 5;
                    }
                }

                ///// Script is sane - build the events.
                if (ok)
                {
                    _script!.Init(_channels);
                    _script.BuildSteps();

                    // Store the steps in the channel objects.
                    MidiTimeConverter _mt = new(BarTime.LOW_RES_PPQ, UserSettings.TheSettings.MidiSettings.DefaultTempo);
                    foreach (var channel in _channels.Values)
                    {
                        var chEvents = _script.GetEvents().Where(e => e.ChannelName == channel.ChannelName &&
                            (e.RawEvent is NoteEvent || e.RawEvent is NoteOnEvent));

                        // Scale time and give to channel.
                        chEvents.ForEach(e => e.ScaledTime = _mt!.MidiToInternal(e.AbsoluteTime));
                        channel.SetEvents(chEvents);

                        // Round total up to next beat.
                        BarTime bs = new();
                        bs.SetRounded(channel.MaxSubdiv, SnapType.Beat, true);
                        _totalSubdivs = Math.Max(_totalSubdivs, bs.TotalSubdivs);
                    }
                }

                ///// Everything is sane - prepare to run.
                if (ok)
                {
                    ///// Init the timeclock.
                    if (_totalSubdivs > 0) // sequences
                    {
                        barBar.TimeDefs = _script!.GetSectionMarkers();
                        barBar.Length = new(_totalSubdivs);
                        barBar.Start = new(0);
                        barBar.End = new(_totalSubdivs - 1);
                        barBar.Current = new(0);
                    }
                    else // free form
                    {
                        barBar.Length = new(0);
                        barBar.Start = new(0);
                        barBar.End = new(0);
                        barBar.Current = new(0);
                    }

                    // Start the clock.
                    SetFastTimerPeriod();
                }

                // Update file watcher.
                compiler.SourceFiles.ForEach(f => { _watcher.Add(f); });

                SetCompileStatus(ok);

                if(!ok)
                {
                    _logger.Error("Compile failed.");
                }

                // Log compiler results.
                compiler.Results.ForEach(r =>
                {
                    string msg = r.SourceFile != "" ? $"{Path.GetFileName(r.SourceFile)}({r.LineNumber}): {r.Message}" : r.Message;
                    switch (r.ResultType)
                    {
                        case CompileResultType.Error: _logger.Error(msg); break;
                        case CompileResultType.Warning: _logger.Warn(msg); break;
                        default: _logger.Info(msg); break;
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
            btnCompile.BackColor = compileStatus ? UserSettings.TheSettings.BackColor : Color.Red;
            _needCompile = !compileStatus;
        }
        #endregion

        #region Devices
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices()
        {
            bool ok = true;

            // First...
            DestroyDevices();

            foreach(var dev in UserSettings.TheSettings.MidiSettings.InputDevices)
            {
                switch (dev.DeviceName)
                {
                    case nameof(VirtualKeyboard):
                        {
                            VirtualKeyboard vkey = new()
                            {
                                Dock = DockStyle.Fill,
                            };
                            vkey.InputEvent += Device_InputEvent;
                            _inputDevices.Add(dev.DeviceId, vkey);

                            Form f = new()
                            {
                                Text = "Virtual Keyboard",
                                ClientSize = vkey.Size,
                                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                            };
                            f.Controls.Add(vkey);
                            f.Show();
                        }
                        break;

                    case nameof(BingBong):
                        {
                            BingBong bb = new()
                            {
                                Dock = DockStyle.Fill,
                            };
                            bb.InputEvent += Device_InputEvent;
                            _inputDevices.Add(dev.DeviceId, bb);

                            Form f = new()
                            {
                                Text = "Bing Bong",
                                ClientSize = bb.Size,
                                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                            };
                            f.Controls.Add(bb);
                            f.Show();
                        }
                        break;

                    case "":
                        // None specified.
                        break;

                    default:
                        // Try midi.
                        var min = new MidiInput(dev.DeviceName);
                        if (min.Valid)
                        {
                            min.InputEvent += Device_InputEvent;
                            _inputDevices.Add(dev.DeviceId, min);
                        }
                        else
                        {
                            // Try osc.
                            try
                            {
                                var mosc = new OscInput(dev.DeviceName);
                                mosc.InputEvent += Device_InputEvent;
                                _inputDevices.Add(dev.DeviceId, mosc);
                            }
                            catch
                            {
                                _logger.Error($"Something wrong with your input device:{dev.DeviceName} id:{dev.DeviceId}");
                                ok = false;
                            }
                        }
                        break;
                }
            }

            foreach (var dev in UserSettings.TheSettings.MidiSettings.OutputDevices)
            {
                switch (dev.DeviceName)
                {
                    default:
                        // Try midi.
                        var mout = new MidiOutput(dev.DeviceName);
                        if (mout.Valid)
                        {
                            _outputDevices.Add(dev.DeviceId, mout);
                        }
                        else
                        {
                            // Try osc.
                            try
                            {
                                var mosc = new OscOutput(dev.DeviceName);
                                _outputDevices.Add(dev.DeviceId, mosc);
                            }
                            catch
                            {
                                _logger.Error($"Something wrong with your output device:{dev.DeviceName} id:{dev.DeviceId}");
                                ok = false;
                            }
                        }
                        break;
                }
            }

            _outputDevices.Values.ForEach(d => d.LogEnable = UserSettings.TheSettings.MonitorOutput);

            return ok;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyDevices()
        {
            _inputDevices.Values.ForEach(d => d.Dispose());
            _inputDevices.Clear();
            _outputDevices.Values.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion

        #region Channel controls
        /// <summary>
        /// UI changed something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChangeEvent(object? sender, ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        _channels.Values.ForEach(ch =>
                        {
                            if (ch.ChannelName != chc.BoundChannel.ChannelName && chc.State != ChannelState.Solo)
                            {
                                chc.BoundChannel.Kill();
                            }
                        });
                        break;

                    case ChannelState.Mute:
                        chc.BoundChannel.Kill();
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                chc.BoundChannel.SendPatch();
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
            this.InvokeIfRequired(_ =>
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
            if (_script is not null && chkPlay.Checked && !_needCompile)
            {
                //_tan.Arm();

                InitRuntime();

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

                bool anySolo = _channels.AnySolo();

                // Process any sequence steps.
                foreach (var ch in _channels.Values)
                {
                    // Is it ok to play now?
                    bool play = ch.State == ChannelState.Solo || (ch.State == ChannelState.Normal && !anySolo);

                    if (play)
                    {
                        // Need exception handling here to protect from user script errors.
                        try
                        {
                            ch.DoStep(_stepTime.TotalSubdivs);
                        }
                        catch (Exception ex)
                        {
                            ProcessScriptRuntimeError(ex);
                        }
                    }
                }

                ///// Bump time.
                _stepTime.Increment(1);
                bool done = barBar.IncrementCurrent(1);
                // Check for end of play. If no steps or not selected, free running mode so always keep going.
                if (barBar.TimeDefs.Count > 1)
                {
                    // Check for end.
                    if (done)
                    {
                        _channels.Values.ForEach(ch => ch.Flush(_stepTime.TotalSubdivs));
                        ProcessPlay(PlayCommand.StopRewind);
                        KillAll(); // just in case
                    }
                }
                // else keep going

                ProcessPlay(PlayCommand.UpdateUiTime);

                // Process whatever the script did.
                ProcessRuntime();
            }
        }

        /// <summary>
        /// Process input event.
        /// </summary>
        void Device_InputEvent(object? sender, InputEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if (_script is not null && sender is not null)
                {
                    var dev = (IInputDevice)sender;

                    // Hand over to the script.
                    if (e.Note != -1)
                    {
                        _script.InputNote(dev.DeviceName, e.Channel, e.Note, e.Value);
                    }
                    else if (e.Controller != -1)
                    {
                        _script.InputControl(dev.DeviceName, e.Channel, e.Controller, e.Value);
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
            if (_script is not null)
            {
                _script.Playing = chkPlay.Checked;
                _script.StepTime = _stepTime;
                _script.RealTime = (DateTime.Now - _startTime).TotalSeconds;
                _script.Tempo = (int)sldTempo.Value;
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
                if (_script.Tempo != (int)sldTempo.Value)
                {
                    sldTempo.Value = _script.Tempo;
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

            // Locate the offending frame by finding the file in the temp dir.
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
            string dir = ""; 
            if (UserSettings.TheSettings.ScriptPath != "")
            {
                if(Directory.Exists(UserSettings.TheSettings.ScriptPath))
                {
                    dir = UserSettings.TheSettings.ScriptPath;
                }
                else
                {
                    _logger.Warn("Your script path is invalid, edit your settings");
                }
            }

            using OpenFileDialog openDlg = new()
            {
                Filter = "Nebulator files | *.neb",
                Title = "Select a Nebulator file",
                InitialDirectory = dir,
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

            try
            {
                // Clean up the old.
                SaveProjectValues();
                barBar.TimeDefs.Clear();

                if (File.Exists(fn))
                {
                    _logger.Info($"Opening {fn}");
                    _scriptFileName = fn;

                    // Get the persisted properties.
                    _nppVals = Bag.Load(fn.Replace(".neb", ".nebp"));
                    sldTempo.Value = _nppVals.GetDouble("master", "speed", 100.0);
                    sldVolume.Value = _nppVals.GetDouble("master", "volume", MidiLibDefs.VOLUME_DEFAULT);

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

            // Update bar.
            barBar.Start = new();
            barBar.Current = new();

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
            this.InvokeIfRequired(_ =>
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
        /// Monitor comm messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        void Monitor_Click(object? sender, EventArgs e)
        {
            UserSettings.TheSettings.MonitorInput = btnMonIn.Checked;
            UserSettings.TheSettings.MonitorOutput = btnMonOut.Checked;

            _outputDevices.Values.ForEach(d => d.LogEnable = UserSettings.TheSettings.MonitorOutput);
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
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogEvent(object? sender, LogEventArgs e)
        {
            // Usually come from a different thread.
            if (IsHandleCreated)
            {
                this.InvokeIfRequired(_ => { textViewer.AppendLine($"{e.Message}"); });
            }
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            List<(string name, string cat)> changes = UserSettings.TheSettings.Edit("User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "MidiInDevice":
                    case "MidiOutDevice":
                    case "InternalPPQ":
                    case "ControlColor":
                    case "SelectedColor":
                    case "BackColor":
                        restart = true;
                        break;
                }
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
                    KillAll();
                    break;

                case PlayCommand.Rewind:
                    _stepTime.Reset();
                    barBar.Current = new();
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
            barBar.Current = new(_stepTime.TotalSubdivs);

            return ret;
        }

        /// <summary>
        /// User has changed the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            _stepTime = new(barBar.Current.TotalSubdivs);
            ProcessPlay(PlayCommand.UpdateUiTime);
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
            MidiTimeConverter mt = new(UserSettings.TheSettings.MidiSettings.SubdivsPerBeat, sldTempo.Value);
            var per = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer(per, MmTimerCallback);
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export to a midi file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportMidi_Click(object? sender, EventArgs e)
        {
            if(_script is not null)
            {
                using SaveFileDialog saveDlg = new()
                {
                    Filter = "Midi files (*.mid)|*.mid",
                    Title = "Export to midi file",
                    FileName = Path.GetFileName(_scriptFileName.Replace(".neb", ".mid"))
                };

                if (saveDlg.ShowDialog() == DialogResult.OK)
                {
                    // Make a Pattern object and call the formatter.
                    IEnumerable<Channel> channels = _channels.Values.Where(ch => ch.NumEvents > 0);

                    PatternInfo pattern = new("export", UserSettings.TheSettings.MidiSettings.SubdivsPerBeat,
                        _script.GetEvents(), channels, _script.Tempo);

                    Dictionary<string, int> meta = new()
                    {
                        { "MidiFileType", 0 },
                        { "DeltaTicksPerQuarterNote", UserSettings.TheSettings.MidiSettings.SubdivsPerBeat },
                        { "NumTracks", 1 }
                    };

                    MidiExport.ExportMidi(saveDlg.FileName, pattern, channels, meta);
                }
            }
        }

        /// <summary>
        /// Dump human readable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportCsv_Click(object sender, EventArgs e)
        {
            if (_script is not null)
            {
                // Make a Pattern object and call the formatter.
                IEnumerable<Channel> channels = _channels.Values.Where(ch => ch.NumEvents > 0);

                var fn = Path.GetFileName(_scriptFileName.Replace(".neb", ".csv"));

                PatternInfo pattern = new("export", UserSettings.TheSettings.MidiSettings.SubdivsPerBeat, _script.GetEvents(), channels, _script.Tempo);

                Dictionary<string, int> meta = new()
                {
                    { "MidiFileType", 0 },
                    { "DeltaTicksPerQuarterNote", UserSettings.TheSettings.MidiSettings.SubdivsPerBeat },
                    { "NumTracks", 1 }
                };

                MidiExport.ExportCsv(fn, new List<PatternInfo>() { pattern }, channels, meta);
                _logger.Info($"Exported to {fn}");
            }
        }

        /// <summary>
        /// Kill em all.
        /// </summary>
        void KillAll()
        {
            chkPlay.Checked = false;
            _channels.Values.ForEach(ch => ch.Kill());
        }

        /// <summary>
        /// Show the builtin definitions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowDefinitions_Click(object sender, EventArgs e)
        {
            var docs = MidiDefs.FormatDoc();
            docs.AddRange(MusicDefinitions.FormatDoc());
            Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("arial", 16), true);
        }
        #endregion
    }
}
