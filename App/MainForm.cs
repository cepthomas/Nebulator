using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;
using Ephemera.MusicLib;
using Ephemera.NScript;
using Nebulator.Script;

//>>> TODO-M goes 8x too fast.


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

        /// <summary>Midi boss.</summary>
        readonly ChannelManager CM;

        /// <summary>Midi boss.</summary>
        readonly InputManager IM;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Our compiler.</summary>
        readonly Compiler _compiler;

        /// <summary>Current neb script file name.</summary>
        string? _scriptFileName;

        /// <summary>The current script.</summary>
        ScriptCore? _script;

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

            CM = new(Controls);
            IM = new();

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
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
            tvInfo.WordWrap = false;
            tvInfo.Prompt = "> ";
            tvInfo.Matchers =
            [
                new("ERR", Color.Red),
                new("WRN", Color.Green),
            ];

            GraphicsUtils.ColorizeControl(btnMonIn, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnMonOut, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnKillComm, _settings.IconColor);
            GraphicsUtils.ColorizeControl(fileDropDownButton, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnAbout, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnSettings, _settings.IconColor);
            // ColorizeControl() not here...

            btnMonIn.Checked = _settings.MonitorInput;
            btnMonOut.Checked = _settings.MonitorOutput;
    
            sldTempo.DrawColor = _settings.DrawColor;
            sldTempo.Minimum = 50;
            sldTempo.Maximum = 200;
            sldTempo.Resolution = 5;
            sldTempo.Value = 100;

            sldVolume.DrawColor = _settings.DrawColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = MidiLibDefs.MAX_VOLUME;
            sldVolume.Resolution = MidiLibDefs.RES_VOLUME;
            sldVolume.Value = MidiLibDefs.DEFAULT_VOLUME;

            // Time controller.
            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.StateChange += TimeBar_StateChange; // += TimeBar_CurrentTimeChanged;
            timeBar.Invalidate();

            tvInfo.WordWrap = _settings.WordWrap;

            btnMonIn.Click += Monitor_Click;
            btnMonOut.Click += Monitor_Click;
            btnAbout.Click += About_Click;
            btnSettings.Click += Settings_Click;
            btnKillComm.Click += (_, __) => CM.KillAll();

            IM.MessageReceived += Manager_MessageReceived;
            //CM.MessageSent += Manager_MessageSent;
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

            // TODO after update to .NET10 this needs to be done here instead of in constructor.
            GraphicsUtils.ColorizeControl(chkPlay, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnCompile, _settings.IconColor);
            GraphicsUtils.ColorizeControl(btnRewind, _settings.IconColor);
            chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

            // Show them what they have.
            MidiDefs.GenUserDeviceInfo().ForEach(l => tvInfo.Append(l));

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();
            ProcessPlay(PlayCommand.Stop);
            CM.KillAll();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            _settings.WordWrap = tvInfo.WordWrap;
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
            CM.Dispose();
            IM.Dispose();

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
            if (_scriptFileName is null)
            {
                _logger.Warn("No script file loaded.");
                return false;
            }

            // Clean up old script stuff.
            ProcessPlay(PlayCommand.StopRewind);

            CM.Reset();
            IM.Reset();
            _watcher.Clear();
            timeBar.Reset();

            // Run the compiler.
            _compiler.CompileScript(_scriptFileName);

            // What happened?
            if (_compiler.CompiledScript is null)
            {
                _logger.Error($"Compile failed:");
                LogCompilerResults();
                return false;
            }

            _script = _compiler.CompiledScript as ScriptCore;

            if (_script is not null)
            {
                ScriptCore.CM = CM;
                ScriptCore.IM = IM;

                SetCompileStatus(true);

                // Need exception handling here to protect from user script errors.
                try
                {
                    // Init shared vars.
                    InitRuntime();

                    // Setup script. Defines channels and devices, builds the sequences and sections.
                    _script.Setup();

                    // Script may have altered shared vars.
                    ProcessRuntime();
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(ex);
                    return false;
                }
            }

            // Script is sane - build the controls.
            const int CONTROL_SPACING = 10;
            int x = btnRewind.Left;
            int y = timeBar.Bottom + CONTROL_SPACING;

            // Get all script-defined channels and make corresponding UI element.
            var channels = CM.GetChannels().Select(ch => ch.chan).ToList();

            channels.ForEach(chan =>
            {
                var ctrl = new ChannelControl()
                {
                    //Name = chan.ChannelName,
                    Info1 = $"M{chan.ChannelNumber} {chan.ChannelName}",
                    Info2 = $"{chan.PatchName} ({chan.Patch}){Environment.NewLine}{chan}",
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = _settings.DrawColor,
                    SelectedColor = _settings.SelectedColor,
                    Volume = MidiLibDefs.DEFAULT_VOLUME // should persist + channel vals etc
                };

                //ctrl.InfoClick += ChannelControl_InfoClick;
                CM.Bind(chan, ctrl);

                // Adjust positioning.
                x += ctrl.Width + 5;
            });

            // Script is sane - build the events.
            _script!.BuildSteps();

            // Init the timebar.
            Dictionary<MusicTime, string> sectInfo = [];
            _script!.GetSectionInfo().ForEach(si => sectInfo.Add(new(si.Key), si.Value));
            timeBar.InitSectionInfo(sectInfo);

            // Start the clock.
            SetFastTimerPeriod();

            // Update file watcher.
            _compiler.SourceFiles.ForEach(f => { _watcher.Add(f); });

            SetCompileStatus(true);

            // Log compiler results.
            LogCompilerResults();

            return true;
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

                    // Execute the script step. Note: Need exception handling here to protect from user script errors.
                    try
                    {
                        // Execute user step function.
                        _script.Step();

                        // Process each channel.
                        foreach (var (chan, ctrl) in CM.GetChannels())
                        {
                            // Process any sequence steps.
                            var playEvents = chan.Events.Get(timeBar.Current);

                            foreach (var mevt in playEvents)
                            {
                                switch (mevt)
                                {
                                    case NoteOn evt:
                                        // Adjust volume.
                                        evt.Velocity = MathUtils.Constrain((int)(evt.Velocity * ctrl!.Volume * sldVolume.Value), 0, MidiDefs.MAX_MIDI);
                                        chan.Send(evt);
                                        break;

                                    case NoteOff evt:
                                        chan.Send(evt);
                                        break;

                                    default:
                                        // Everything else as is.
                                        chan.Send(mevt);
                                        break;
                                }
                            }
                        }

                        // Bump time. Check for end of play.
                        bool donex = !timeBar.Increment();
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
                        CM.KillAll(); // just in case
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
        void Manager_MessageReceived(object? sender, BaseEvent e)
        {
            //?? this.InvokeIfRequired(_ =>
            if (_script is not null && sender is not null)
            {
                var dev = (IInputDevice)sender;

                switch (e)
                {
                    case NoteOn evt:
                        _script.ReceiveNote(dev.DeviceName, evt.ChannelNumber, evt.Note, evt.Velocity);
                        break;

                    case NoteOff evt:
                        _script.ReceiveNote(dev.DeviceName, evt.ChannelNumber, evt.Note, 0);
                        break;

                    case Controller evt:
                        _script.ReceiveController(dev.DeviceName, evt.ChannelNumber, evt.Id, evt.Value);
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Process output event.
        /// </summary>
        void Manager_MessageSent(object? sender, BaseEvent e)
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
        /// Allows the user to select a neb file from file system. Should remember last location or default to guessed.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            string dir = "";
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
                this.InvokeIfRequired(_ => { tvInfo.Append($"{e.Message}"); });
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
                    CM.KillAll();
                    break;

                case PlayCommand.Rewind:
                    timeBar.Rewind();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    timeBar.Rewind();
                    CM.KillAll();
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
        /// Set the multimedia timer period.
        /// </summary>
        void SetFastTimerPeriod()
        {
            double secPerBeat = 60.0 / sldTempo.Value;
            double msecPerT = 1000 * secPerBeat / MusicTime.SubbeatsPerBeat;
            _mmTimer.SetTimer(msecPerT > 1.0 ? (int)Math.Round(msecPerT) : 1, MmTimerCallback);
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export to a midi file. TODO needs some work/debug.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportMidi_Click(object? sender, EventArgs e)
        {
            if (_scriptFileName is not null && _script is not null)
            {
                using SaveFileDialog saveDlg = new()
                {
                    Filter = "Midi files (*.mid)|*.mid",
                    Title = "Export to midi file",
                    FileName = Path.GetFileName(_scriptFileName.Replace(".neb", ".mid"))
                };

                if (saveDlg.ShowDialog() == DialogResult.OK)
                {
                    // Init output file contents.
                    int ppq = MusicTime.SubbeatsPerBeat;
                    MidiEventCollection outColl = new(1, ppq);

                    var channels = CM.GetChannels().Where(ch => ch.chan.Events.Count() > 0);

                    // Build the event collection.
                    foreach (var (chan, _) in channels)
                    {
                        IList<MidiEvent> outEvents = outColl.AddTrack();
                        for (int i = 0; i < chan.Events.Count(); i++)
                        {
                            //>>> outEvents.Add(chan.Events[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            Tools.ShowReadme("Nebulator");

            // Show them what they have.
            MidiDefs.GenUserDeviceInfo().ForEach(l => tvInfo.Append(l));
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
