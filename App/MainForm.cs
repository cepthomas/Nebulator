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

// TODO1 get rid of parens in .neb files? gen enums?

// TODO1 fix midilib project reference.

// TODO1 show docs: mididefs, musicdefs. gen? these from internal defs:
// - "F4.o7" - Named chord from [Chords](#musicdefinitions/chords) in the key of middle F.
// - "F4.Aeolian" - Named scale from [Scales](#musicdefinitions/scales).
// - SideStick - Drum name from [Drums](#musicdefinitions/generalmididrums).


// TODOX - Support multiple/generic ins/outs:
// - inputs by name in the script.
// - incl OSC, virtuals, ... channel access/id will require number + device.
// - affects all user settings change detection.
// - ChannelManager: readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS]; // breaks for multi-devices.
// - put OSC back.

// TODO Add audio lib?


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

        ///// <summary>The current channels.</summary>
        //readonly Dictionary<int, Channel> _channels = new();
        /// <summary>The internal channel objects.</summary>
        readonly ChannelManager _channelManager = new();

        /// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        readonly Dictionary<string, IMidiOutputDevice> _outputDevices = new();

        /// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        readonly Dictionary<string, IMidiInputDevice> _inputDevices = new();

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

            InitializeComponent();

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Info;
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
            textViewer.Colors.Add("ERR", Color.LightPink);
            textViewer.Colors.Add("WRN", Color.Plum);

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKillComm.Image, UserSettings.TheSettings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap((Bitmap)fileDropDownButton.Image, UserSettings.TheSettings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image, UserSettings.TheSettings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, UserSettings.TheSettings.IconColor);
            btnClear.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnClear.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnWrap.Image, UserSettings.TheSettings.IconColor);

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
            _logger.Info("============================ Starting up ===========================");

            PopulateRecentMenu();

            CreateDevices();

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
            Kill();

            // Save user settings.
            UserSettings.TheSettings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            UserSettings.TheSettings.WordWrap = btnWrap.Checked;

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

            _channelManager.ForEach(ch =>
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
                //List<Control> controlsToRemove = new(Controls.OfType<ChannelControl>());
                //controlsToRemove.ForEach(c => { c.Dispose(); Controls.Remove(c); });
                (Controls.OfType<ChannelControl>()).ForEach(c => { c.Dispose(); Controls.Remove(c); });
                _channelManager.Reset();
                _watcher.Clear();
                barBar.Reset();

                // Compile script.
                Compiler compiler = new();
                compiler.Execute(_scriptFileName);
                _script = compiler.Script as ScriptBase;

                // Process errors. Some may only be warnings.
                ok = !compiler.Results.Any(w => w.ResultType == CompileResultType.Error) && _script is not null;

                if (ok)
                {
                    //compiler.Channels.ForEach(ch => _channels.Add(ch.ChannelNumber, ch));

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


                        // Build all the events from the sequences and sections. TODO0 needs valid channels
 //>>>                       _script.BuildSteps();
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(ex);
                        ok = false;
                    }
                }

                ///// Script is sane - build the UI.
                if (ok)
                {
                    // Create channels and controls from event sets.
                    const int CONTROL_SPACING = 10;
                    int x = btnRewind.Left;
                    int y = barBar.Bottom + CONTROL_SPACING;

                    foreach (var chspec in compiler.ChannelSpecs)
                    {
                        // Make new control.
                        PlayerControl control = new()
                        {
                            Location = new(x, y),
                            //Name = $"{chspec.ChannelName}",
                            // Name = $"channel{chspec.ChannelNumber}",
                            BorderStyle = BorderStyle.FixedSingle
                        };
                        control.ChannelChangeEvent += ChannelControl_ChannelChangeEvent;
                        Controls.Add(control);

                        // Bind to internal channel object then init dynamic properties. TODOX this is clumsy, create control first then bind.
                        _channelManager.Bind(chspec.ChannelNumber, control);
                        control.Name = $"{chspec.ChannelName}";
                        control.Volume = _nppVals.GetDouble(chspec.ChannelName, "volume", VolumeDefs.DEFAULT);
                        control.State = (ChannelState)_nppVals.GetInteger(chspec.ChannelName, "state", (int)ChannelState.Normal);
                        control.Patch = chspec.Patch;
                        control.IsDrums = chspec.IsDrums;

                        // Adjust positioning for next iteration.
                        y += control.Height + 5;
                    }
                }


                ///// Script is sane - build the events.
                if (ok)
                {
                    _script.BuildSteps();

                    foreach (var chspec in compiler.ChannelSpecs)
                    {
                        var chEvents = _script.GetEvents().Where(e => e.ChannelNumber == chspec.ChannelNumber &&
                            (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent));

                        _channelManager.SetEvents(chspec.ChannelNumber, chEvents);
                    }
                }


                ///// Everything is sane - prepare to run.
                if (ok)
                {
                    _script.Init(_channelManager);

                    ///// Init the timeclock.
                    barBar.TimeDefs = _script.GetSectionMarkers();
                    barBar.Length = new(_channelManager.TotalSubdivs);
                    //barBar.Length = new(barBar.TimeDefs.Keys.Max());
                    barBar.Start = new(0);
                    barBar.End = new(_channelManager.TotalSubdivs - 1);
                    barBar.Current = new(0);

                    // Start the clock.
                    SetFastTimerPeriod();
                }

                // Update file watcher. TODO1 working?
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
            btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, compileStatus ? UserSettings.TheSettings.IconColor : Color.Red);
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

            if (UserSettings.TheSettings.MidiSettings.MidiInDevice != "")
            {
                var ml = new MidiListener(UserSettings.TheSettings.MidiSettings.MidiInDevice);//, "MidiInDevice");
                if (ml.Valid)
                {
                    ml.InputEvent += Device_InputEvent;
                    _inputDevices.Add("MidiInDevice", ml);
                }
                else
                {
                    _logger.Error($"Something wrong with your device: {UserSettings.TheSettings.MidiSettings.MidiInDevice}");
                }
            }

            if (UserSettings.TheSettings.MidiSettings.MidiOutDevice != "")
            {
                var ml = new MidiSender(UserSettings.TheSettings.MidiSettings.MidiOutDevice);//, "MidiOutDevice");
                if (ml.Valid)
                {
                    _outputDevices.Add("MidiOutDevice", ml);
                }
                else
                {
                    _logger.Error($"Something wrong with your device: {UserSettings.TheSettings.MidiSettings.MidiOutDevice}");
                }
            }

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
        ///// <summary>
        ///// Create the channel controls from the user script.
        ///// </summary>
        //bool CreateChannelControls()
        //{
        //    bool ok = true;
        //    const int CONTROL_SPACING = 10;
        //    int x = btnRewind.Left;
        //    int y = barBar.Bottom + CONTROL_SPACING;

        //    // Create new channel controls.
        //    foreach (Channel ch in _channels.Values.OrderBy(c => c.ChannelNumber))
        //    {
        //        // Locate the output device for this channel.
        //        if (_outputDevices.ContainsKey(ch.DeviceId))
        //        {
        //            var outDev = _outputDevices[ch.DeviceId];
        //            ch.Device = outDev;
        //            ch.Volume = _nppVals.GetDouble(ch.ChannelName, "volume", VolumeDefs.DEFAULT);
        //            ch.State = (ChannelState)_nppVals.GetInteger(ch.ChannelName, "state", (int)ChannelState.Normal);

        //            PlayerControl tctl = new()
        //            {
        //                Location = new Point(x, y),
        //                BorderStyle = BorderStyle.FixedSingle,
        //                BoundChannel = ch
        //            };
        //            tctl.ChannelChangeEvent += ChannelControl_ChannelChangeEvent;
        //            Controls.Add(tctl);

        //            y += tctl.Height + CONTROL_SPACING;
        //        }
        //        else
        //        {
        //            _logger.Error($"Invalid device: {ch.DeviceId} for channel: {ch.ChannelName}");
        //            ok = false;
        //            break;
        //        }
        //    }

        //    return ok;
        //}

        /// <summary>
        /// UI changed something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChangeEvent(object? sender, ChannelChangeEventArgs e)
        {
            PlayerControl chc = (PlayerControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                        {
                            int chnum = i + 1;
                            if (chnum != chc.ChannelNumber && chc.State != ChannelState.Solo)
                            {
                                _outputDevices.Values.ForEach(d => d.Kill(chnum));
                            }
                        }
                        break;

                    case ChannelState.Mute:
                        _outputDevices.Values.ForEach(d => d.Kill(chc.ChannelNumber));
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                _outputDevices.Values.ForEach(d => d.SendPatch(chc.ChannelNumber, chc.Patch));
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

                // Check for inter-channel states.
                bool anySolo = _channelManager.AnySolo;
                bool anyMute = _channelManager.AnyMute;
                lblSolo.BackColor = anySolo ? Color.Pink : SystemColors.Control;
                lblMute.BackColor = anyMute ? Color.Pink : SystemColors.Control;

                // Process any sequence steps.

                foreach(var ch in _channelManager)
                {

                    // Is it ok to play now?
                    bool play = ch.State == ChannelState.Solo || (ch.State == ChannelState.Normal && !anySolo);

                    if (play)
                    {
                        foreach(var evt in ch.GetEvents(_stepTime.TotalSubdivs))
                        {
                            switch (evt)
                            {
                                case FunctionMidiEvent fe:
                                    // Need exception handling here to protect from user script errors.
                                    try
                                    {
                                        fe.ScriptFunction?.Invoke();
                                    }
                                    catch (Exception ex)
                                    {
                                        ProcessScriptRuntimeError(ex);
                                    }
                                    break;

                                default:
                                    ch.Device.SendEvent(evt);
                                    break;
                            }
                        }
                    }
                }

                ///// Bump time.
                _stepTime.Increment(1);
                barBar.IncrementCurrent(1);
                // Check for end of play. If no steps or not selected, free running mode so always keep going.
                if (barBar.TimeDefs.Count > 1)
                {
                    // Check for end.
                    if (_stepTime.Beat > barBar.Current.Beat)
                    {
                        ProcessPlay(PlayCommand.StopRewind);
                        Kill(); // just in case
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
                    var dev = (IMidiInputDevice)sender;

                    if (e.Note != -1)
                    {
                        // Send note on or off.
                        _script.InputNote(dev.DeviceName, e.Channel, e.Value != -1 ? e.Note : -e.Note);
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
                    sldVolume.Value = _nppVals.GetDouble("master", "volume", VolumeDefs.DEFAULT);

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
                    Kill();
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

            //TODO0 ??? _outputDevices.Values.ForEach(o => { if (chkPlay.Checked) o.Start(); else o.Stop(); });

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
            //MidiTime mt = new()
            //{
            //    InternalPpq = Time.SubdivsPerBeat,
            //    Tempo = sldTempo.Value
            //};

            var per = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer(per, MmTimerCallback);
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export to a text or midi file.
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
                    IEnumerable<Channel> channels = _channelManager.Where(ch => ch.NumEvents > 0);

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
        /// Kill em all.
        /// </summary>
        void Kill()
        {
            _outputDevices.Values.ForEach(o => o.KillAll());
        }
        #endregion
    }
}
