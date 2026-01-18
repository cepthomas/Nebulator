using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;
using Ephemera.MidiLibEx;
using Ephemera.MusicLib;
using Ephemera.NScript;
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

        /// <summary>App settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Our compiler.</summary>
        readonly Compiler _compiler;

        /// <summary>Current neb script file name.</summary>
        string? _scriptFileName;

        /// <summary>The current script.</summary>
        ScriptCore? _script;

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("Nebulator", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.SourceInfo = false;
            //LogManager.Timestamp = false;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);

            // Create compiler.
            _compiler = new() { IgnoreWarnings = true };

            #region Init UI from settings
            toolStrip1.Renderer = new ToolStripCheckBoxRenderer() { SelectedColor = _settings.SelectedColor };

            // Main form.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;

            // The rest of the controls.
            textViewer.WordWrap = false;
            textViewer.MatchText.Add("ERR", Color.LightPink);
            textViewer.MatchText.Add("WRN", Color.Plum);
            textViewer.Prompt = "> ";

            GraphicsUtils.ColorizeControl(btnMonIn, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnMonOut, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnKillComm, _settings.IconColor);
            GraphicsUtils.ColorizeControl(fileDropDownButton, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnRewind, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnCompile, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnAbout, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnSettings, _settings.IconColor);
            GraphicsUtils.ColorizeControl(chkPlay, _settings.IconColor);

            btnMonIn.Checked = _settings.MonitorInput;
            btnMonOut.Checked = _settings.MonitorOutput;

            chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

            sldTempo.DrawColor = _settings.DrawColor;
            sldTempo.Invalidate();
            sldVolume.DrawColor = _settings.DrawColor;
            sldVolume.Invalidate();

            // Time controller.
            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.StateChange += TimeBar_StateChange; // += TimeBar_CurrentTimeChanged;
            timeBar.Invalidate();

            textViewer.WordWrap = _settings.WordWrap;

            btnMonIn.Click += Monitor_Click;
            btnMonOut.Click += Monitor_Click;
            btnAbout.Click += About_Click;
            btnSettings.Click += Settings_Click;
            btnKillComm.Click += (_, __) => MidiManager.Instance.Kill();

            MidiManager.Instance.MessageReceive += Manager_MessageReceive;
            MidiManager.Instance.MessageSend += Manager_MessageSend;
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

            // Fast mm timer.
            SetFastTimerPeriod();
            _mmTimer.Start();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChange += Watcher_Changed;

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
            MidiManager.Instance.Kill();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            _settings.WordWrap = textViewer.WordWrap;
            _settings.Save();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _mmTimer.Stop();
            _mmTimer.Dispose();
            DestroyControls();
            MidiManager.Instance.DestroyDevices();

            // Wait a bit in case there are some lingering events.
            System.Threading.Thread.Sleep(100);

            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Compile
        /// <summary>
        /// Compile the neb script.
        /// </summary>
        bool CompileScript()
        {
            bool ok = true;

            if (_scriptFileName is null)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                // Clean up old script stuff.
                ProcessPlay(PlayCommand.StopRewind);

                DestroyControls();

                MidiManager.Instance.DestroyChannels();
                _watcher.Clear();
                timeBar.Reset();

                // Run the compiler.
                _compiler.CompileScript(_scriptFileName);

                // What happened?
                if (_compiler.CompiledScript is null)
                {
                    _logger.Error($"Compile failed:");

                    // Log compiler results.
                    LogCompilerResults();

                    return false;
                }

                _script = _compiler.CompiledScript as ScriptCore;

                if (ok)
                {
                    SetCompileStatus(true);

                    // Need exception handling here to protect from user script errors.
                    try
                    {
                        // Init shared vars.
                        InitRuntime();

                        // Setup script. Defines channels and devices, builds the sequences and sections.
                        _script!.Setup();

                        // Script may have altered shared vars.
                        ProcessRuntime();
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(ex);
                        ok = false;
                    }
                }

                // Script is sane - build the controls.
                if (ok)
                {
                    const int CONTROL_SPACING = 10;
                    int x = btnRewind.Left;
                    int y = timeBar.Bottom + CONTROL_SPACING;

                    foreach (var channel in MidiManager.Instance.OutputChannels)
                    {
                        // Make new control and bind to defined channel.
                        ChannelControl control = new()
                        {
                            Location = new(x, y),
                            BorderStyle = BorderStyle.FixedSingle,
                            BoundChannel = channel,
                            DrawColor = _settings.DrawColor,
                            SelectedColor = _settings.SelectedColor,
                            Options = DisplayOptions.SoloMute
                        };
                        control.ChannelChange += Control_ChannelChange;
                        Controls.Add(control);
                        _channelControls.Add(control);

                        // Adjust positioning.
                        y += control.Height + 5;
                    }
                }

                // Script is sane - build the events.
                if (ok)
                {
                    _script!.BuildSteps();

                    // Init the timebar.
                    var sinfo = _script!.GetSectionInfo();
                    timeBar.InitSectionInfo(sinfo);

                    // Start the clock.
                    SetFastTimerPeriod();
                }

                // Update file watcher.
                _compiler.SourceFiles.ForEach(f => { _watcher.Add(f); });

                SetCompileStatus(ok);

                if(!ok)
                {
                    _logger.Error("Compile failed.");
                }

                // Log compiler results.
                LogCompilerResults();
            }

            return ok;
        }

        /// <summary>
        /// Update system statuses.
        /// </summary>
        /// <param name="compileStatus">True if compile is clean.</param>
        void SetCompileStatus(bool compileStatus)
        {
            btnCompile.BackColor = compileStatus ? BackColor : Color.Red;
            _needCompile = !compileStatus;
        }

        /// <summary>Log helper.</summary>
        void LogCompilerResults()
        {
            _compiler.Reports.ForEach(r =>
            {
                var msg = r.SourceFileName is not null ?
                    $"{r.ReportType}: {r.SourceFileName}({r.SourceLineNumber}) [{r.Message}]" :
                    $"{r.ReportType}: [{r.Message}]";

                switch (r.Level)
                {
                    case ReportLevel.Error: _logger.Error(msg); break;
                    case ReportLevel.Warning: _logger.Warn(msg); break;
                    case ReportLevel.Info: _logger.Info(msg); break;
                    default: break;
                }
            });
        }
        #endregion

        #region Channel controls
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            MidiManager.Instance.Kill();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                c.ChannelChange -= Control_ChannelChange;
                //c.SendMidi -= ChannelControl_SendMidi;
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }

        /// <summary>
        /// UI changed something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.State)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        MidiManager.Instance.OutputChannels
                            .Where(ch => ch.ChannelName != chc.BoundChannel.ChannelName && chc.State != ChannelState.Solo)
                            .ForEach(ch => MidiManager.Instance.Kill(chc.BoundChannel));
                        break;

                    case ChannelState.Mute:
                        MidiManager.Instance.Kill(chc.BoundChannel);
                        break;
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
            try
            {
                if (_script is null || !chkPlay.Checked || _needCompile) return;

                // Kick over to main UI thread.
                this.InvokeIfRequired(_ =>
                {
                    InitRuntime();

                    // Determine what should sound.
                    HashSet<int> sounding = [];
                    bool anySolo = _channelControls.Where(c => c.State == ChannelState.Solo).Any();
                    _channelControls.ForEach(cc => sounding.Add(cc.BoundChannel.ChannelNumber));

                    // Execute the script step. Note: Need exception handling here to protect from user script errors.
                    try
                    {
                        // Execute user step function.
                        _script.Step();

                        // Send any predefined sequence steps.
                        _script.DoNextStep(sounding);
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(ex);
                    }

                    ///// Bump time.
                    bool done = !timeBar.Increment();

                    // Check for end of play. If no steps or not selected, free running mode so always keep going.
                    if (!timeBar.FreeRunning && done)
                    {
                        ProcessPlay(PlayCommand.StopRewind);
                        MidiManager.Instance.Kill(); // just in case
                    }
                    // else keep going

                    ProcessPlay(PlayCommand.UpdateUiTime);

                    // Process whatever the script did.
                    ProcessRuntime();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                _script.StepTime.Set(timeBar.Current);
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
        /// Runtime error.
        /// </summary>
        /// <param name="ex"></param>
        void ProcessScriptRuntimeError(Exception ex)
        {
            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            _compiler.ProcessRuntimeException(ex);
            LogCompilerResults();
        }
        #endregion

        #region Comms
        /// <summary>
        /// Process input event.
        /// </summary>
        void Manager_MessageReceive(object? sender, BaseEvent e)
        {
            //?? this.InvokeIfRequired(_ =>
            if (_script is not null && sender is not null)
            {
                var dev = (IInputDevice)sender;

                switch (e)
                {
                    case NoteOn evt:
                        _script.InputNote(dev.DeviceName, evt.ChannelNumber, evt.Note, evt.Velocity);
                        break;

                    case NoteOff evt:
                        _script.InputNote(dev.DeviceName, evt.ChannelNumber, evt.Note, 0);
                        break;

                    case Controller evt:
                        _script.InputControl(dev.DeviceName, evt.ChannelNumber, evt.ControllerId, evt.Value);
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Process output event.
        /// </summary>
        void Manager_MessageSend(object? sender, BaseEvent e)
        {
            //Anything useful?
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
        /// Allows the user to select a neb file from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            string dir = ""; // TODO remember last location or default to guessed
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
                timeBar.Reset();

                if (File.Exists(fn))
                {
                    _logger.Info($"Opening {fn}");

                    _scriptFileName = fn;
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
            }

            if (ret != "")
            {
                _logger.Error(ret);
                SetCompileStatus(false);
                _scriptFileName = null;
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

            _settings.RecentFiles.ForEach(f =>
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
                _settings.UpdateMru(fn);
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
                if (_settings.AutoCompile)
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
            _settings.MonitorInput = btnMonIn.Checked;
            _settings.MonitorOutput = btnMonOut.Checked;
        }
        #endregion

        #region Messages and logging
        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
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
            // GenericListTypeEditor.SetOptions("OutputDevice", MidiOutputDevice.GetAvailableDevices());

            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    // case "InputDevice":
                    // case "OutputDevice":
                    // case "InternalPPQ":
                    case "DrawColor":
                    case "SelectedColor":
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
                    bool ok = _script is not null && (!_needCompile || CompileScript());
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
                    MidiManager.Instance.Kill();
                    break;

                case PlayCommand.Rewind:
                    timeBar.Rewind();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    timeBar.Rewind();
                    break;

                case PlayCommand.UpdateUiTime:
                    break;
            }

            return ret;
        }

        /// <summary>
        /// User has changed the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        void TimeBar_StateChange(object? sender, TimeBar.StateChangeEventArgs e)
        {
            if (e.CurrentTimeChange)
            {
                //_stepTime.Set(timeBar.Current);
                //ProcessPlay(PlayCommand.UpdateUiTime);
            }
        }
        #endregion

        #region Keyboard handling
        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:  // Handle start/stop toggle.
                    ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                    e.Handled = true;
                    break;

                case Keys.Escape:  // Handle time bar mouse ops.
                    var tbr = new Rectangle(timeBar.Location, timeBar.Size);
                    var mp = PointToClient(MousePosition);
                    if (tbr.Contains(mp))
                    {
                        timeBar.ResetSelection();
                        e.Handled = true;
                    }
                    break;

                default:
                    break;
            }

            base.OnKeyDown(e);
        }
        #endregion

        #region Timer
        /// <summary>
        /// Common func.
        /// </summary>
        void SetFastTimerPeriod()
        {
            // Make a transformer.
            MidiTimeConverter mt = new(MusicTime.TicksPerBeat, sldTempo.Value);
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
            if(_scriptFileName is not null && _script is not null)
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
                    IEnumerable<OutputChannel> channels = MidiManager.Instance.OutputChannels.Where(ch => ch.Events.Count() > 0);

                    PatternInfo pattern = new("export", MusicTime.TicksPerBeat);

                    Dictionary<string, int> meta = new()
                    {
                        { "MidiFileType", 0 },
                        { "DeltaTicksPerQuarterNote", MusicTime.TicksPerBeat },
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
            if (_scriptFileName is not null && _script is not null)
            {
                // Make a Pattern object and call the formatter.
                IEnumerable<OutputChannel> channels = MidiManager.Instance.OutputChannels.Where(ch => ch.Events.Count() > 0);

                //List<int> channelNumbers = [.. channels.Select(cc => cc.ChannelNumber)];
                //List<int> drumNumbers = [.. channels.Where(cc => cc.IsDrums).Select(cc => cc.ChannelNumber)];

                var fn = Path.GetFileName(_scriptFileName.Replace(".neb", ".csv"));

                PatternInfo pattern = new("export", MusicTime.TicksPerBeat);

                Dictionary<string, int> meta = new()
                {
                    { "MidiFileType", 0 },
                    { "DeltaTicksPerQuarterNote", MusicTime.TicksPerBeat },
                    { "NumTracks", 1 }
                };

                //MidiExport.ExportCsv(fn, [pattern], channelNumbers, drumNumbers, meta);
                MidiExport.ExportCsv(fn, [pattern], channels, meta);
                _logger.Info($"Exported to {fn}");
            }
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            Tools.ShowReadme("Nebulator");
        }

        /// <summary>
        /// Show the builtin definitions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowDefinitions_Click(object sender, EventArgs e)
        {
            var docs = MidiDefs.GenUserDeviceInfo();
            docs.AddRange(MidiDefs.GenMarkdown());
            docs.AddRange(MusicDefs.GenMarkdown());
            Tools.MarkdownToHtml(docs, Tools.MarkdownMode.DarkApi, true);
        }
        #endregion
    }
}
