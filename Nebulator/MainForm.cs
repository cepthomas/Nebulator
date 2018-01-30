using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NLog;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Controls;
using Nebulator.Script;
using Nebulator.UI;
using Nebulator.Midi;
using Nebulator.Dynamic;
using NProcessing;


namespace Nebulator
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Fast timer.</summary>
        NebTimer _nebTimer = new NebTimer();

        /// <summary>Piano child form.</summary>
        Piano _piano = new Piano();

        /// <summary>The current script.</summary>
        NebScript _script = null;

        /// <summary>Playing the part.</summary>
        bool _playing = false;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        Time _stepTime = new Time();

        /// <summary>The compiled midi event sequence.</summary>
        StepCollection _compiledSteps = new StepCollection();

        /// <summary>Accumulated control input var changes to be processed at next step.</summary>
        LazyCollection<Variable> _ctrlChanges = new LazyCollection<Variable>() { AllowOverwrite = true };

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tanTimer = new TimingAnalyzer() { SampleSize = 100 };
        //TimingAnalyzer _tanUi = new TimingAnalyzer() { SampleSize = 50 };

        /// <summary>Current neb file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;

        /// <summary>Detect changed composition files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally, will require a recompile.</summary>
        bool _dirtyFiles = false;

        /// <summary>The temp dir for tracking down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current neb file.</summary>
        Bag _nebpVals = new Bag();

        /// <summary>Indicates needs user involvement.</summary>
        Color _attentionColor = Color.Red;

        /// <summary>Internal status.</summary>
        enum PlayCommand { Start, Stop, Rewind, StopRewind, UpdateUiTime }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = Utils.GetAppDir();
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            ///// App innards.
            InitSettings();

            InitLogging();

            PopulateRecentMenu();

            #region Set up midi
            // Input midi events.
            MidiInterface.TheInterface.NebMidiInputEvent += Midi_NebMidiInputEvent;
            MidiInterface.TheInterface.NebMidiLogEvent += Midi_NebMidiLogEvent;

            MidiInterface.TheInterface.Init();

            // Midi timer.
            _nebTimer = new NebTimer();
            SetSpeedTimerPeriod();
            SetUiTimerPeriod();
            _nebTimer.TimerElapsedEvent += TimerElapsedEvent;
            _nebTimer.Start();
            #endregion

            #region Piano
            pianoToolStripMenuItem.Checked = UserSettings.TheSettings.PianoFormInfo.Visible;
            _piano.Visible = UserSettings.TheSettings.PianoFormInfo.Visible;
            _piano.PianoKeyEvent += Piano_PianoKeyEvent;
            #endregion

            #region Misc setups
            InitControls();

            _watcher.FileChangeEvent += Watcher_Changed;

            levers.LeverChangeEvent += Levers_Changed;

            UpdateMenu();

            NoteUtils.Init();

            Text = $"Nebulator {Utils.GetVersionString()} - No file loaded";

            // Intercept all keyboard events.
            KeyPreview = true;
            #endregion

            #region Command line
            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                OpenFile(args[1]);
            }
            #endregion

            #region Debug stuff
#if _DEV
            OpenFile(@"C:\Dev\Nebulator\Examples\dev.neb"); // airport  dev  example  lsys

            //ExportMidi("test.mid");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\OneDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
#endif
            #endregion
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Just in case.
                MidiInterface.TheInterface.KillAll();

                if(_script != null)
                {
                    // Save the project.
                    _nebpVals.Clear();
                    _nebpVals.SetValue("master", "volume", sldVolume.Value);
                    _nebpVals.SetValue("master", "speed", potSpeed.Value);
                    _nebpVals.SetValue("master", "loop", chkLoop.Checked);
                    _nebpVals.SetValue("master", "sequence", chkSequence.Checked);

                    ScriptEntities.Tracks.Values.ForEach(c => _nebpVals.SetValue(c.Name, "volume", c.Volume));
                    _nebpVals.Save();
                }

                // Save user settings.
                SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Couldn't save the file: {ex.Message}.");
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nebTimer?.Stop();
                _nebTimer?.Dispose();
                _nebTimer = null;

                MidiInterface.TheInterface?.Stop();
                MidiInterface.TheInterface?.Dispose();
                MidiInterface.TheInterface = null;

                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Adjust controls.
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
        }
        #endregion

        #region Compile
        /// <summary>
        /// Master compiler function.
        /// </summary>
        bool Compile()
        {
            bool ok = true;

            if (_fn == Utils.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                NebCompiler compiler = new NebCompiler();

                // Save internal nebp file vals now as they will be reloaded during compile.
                _nebpVals.Save();

                // Compile now.
                _script = compiler.Execute(_fn);

                // Update file watcher just in case.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                // Time points.
                timeMaster.TimeDefs.Clear();

                if (compiler.Errors.Count == 0 && _script != null)
                {
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);
                    _dirtyFiles = false;

                    _compileTempDir = compiler.TempDir;

                    // Collect important times.
                    ScriptEntities.Sections.Values.ForEach(s => timeMaster.TimeDefs.Add(new Time(s.Start, 0), s.Name));

                    // Convert compiled stuff to step collection.
                    _compiledSteps.Clear();
                    foreach (Section sect in ScriptEntities.Sections.Values)
                    {
                        // Iterate through the sections tracks.
                        foreach (SectionTrack strack in sect.SectionTracks)
                        {
                            // Get the pertinent Track object.
                            Track track = ScriptEntities.Tracks[strack.TrackName];

                            // For processing current Sequence.
                            int seqOffset = sect.Start;

                            // Gen steps for each sequence.
                            foreach (string sseq in strack.SequenceNames)
                            {
                                Sequence seq = ScriptEntities.Sequences[sseq];
                                StepCollection stepsToAdd = NebScript.ConvertToSteps(track, seq, seqOffset);
                                _compiledSteps.Add(stepsToAdd);
                                seqOffset += seq.Length;
                            }
                        }
                    }

                    // Show everything.
                    InitUi();
                }
                else
                {
                    _logger.Warn("Compile failed.");
                    ok = false;
                    SetPlayStatus(PlayCommand.StopRewind);
                    compiler.Errors.ForEach(e => _logger.Warn(e.ToString()));
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, _attentionColor);
                    _dirtyFiles = true;
                }
            }

            return ok;
        }

        /// <summary>
        /// Create the main UI parts from the composition.
        /// </summary>
        void InitUi()
        {
            ///// Set up UI.
            const int CONTROL_SPACING = 10;
            int x = timeMaster.Right + CONTROL_SPACING;

            ///// The track controls.
            
            // Clean up current controls.
            foreach (Control ctl in splitContainerMain.Panel1.Controls)
            {
                if (ctl is TrackControl)
                {
                    TrackControl tctl = ctl as TrackControl;
                    tctl.Dispose();
                    splitContainerMain.Panel1.Controls.Remove(tctl);
                }
            }

            foreach (Track t in ScriptEntities.Tracks.Values)
            {
                // Init from persistence.
                int vt = Convert.ToInt32(_nebpVals.GetValue(t.Name, "volume"));
                t.Volume = vt == 0 ? 90 : vt; // in case it's new

                TrackControl trk = new TrackControl()
                {
                    Location = new Point(x, 0), // txtTime.Top),
                    BoundTrack = t
                };
                trk.TrackChangeEvent += TrackChange_Event;
                splitContainerMain.Panel1.Controls.Add(trk);
                x += trk.Width + CONTROL_SPACING;
            }

            ///// Init other controls.
            potSpeed.Value = Convert.ToInt32(_nebpVals.GetValue("master", "speed"));
            int mv = Convert.ToInt32(_nebpVals.GetValue("master", "volume"));
            chkLoop.Checked = Convert.ToBoolean(_nebpVals.GetValue("master", "loop"));
            chkSequence.Checked = Convert.ToBoolean(_nebpVals.GetValue("master", "sequence"));

            sldVolume.Value = mv == 0 ? 90 : mv; // in case it's new
            timeMaster.MaxTick = _compiledSteps.MaxTick;
            SetPlayStatus(PlayCommand.StopRewind);
            UpdateMenu();

            ///// Init the user input area.
            // Levers.
            levers.Init(ScriptEntities.Levers.Values);

            // Surface area.
            scriptSurface.InitScript(_script);
        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void TimerElapsedEvent(object sender, NebTimerEventArgs e)
        {
            //// Do some stats gathering for measuring jitter.
            //TimingAnalyzer.Stats stats = _tanTimer.Grab();
            //if (stats != null)
            //{
            //    _logger.Info($"Midi timiing: {stats}");
            //}

            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                NextStep(e);
            });
        }

        /// <summary>
        /// Output next time/step.
        /// </summary>
        /// <param name="e">Information about updates required.</param>
        void NextStep(NebTimerEventArgs e)
        {
            try
            {
                ////// Process changed vars //////
                foreach (Variable var in _ctrlChanges.Values)
                {
                    // Execute any script handlers.
                    _script.ExecScriptFunction(var.Name);

                    // Output any midictlout controllers.
                    IEnumerable<MidiControlPoint> ctlpts = ScriptEntities.OutputMidis.Values.Where(c => c.RefVar.Name == var.Name);

                    if (ctlpts != null && ctlpts.Count() > 0)
                    {
                        ctlpts.ForEach(c =>
                        {
                            StepControllerChange step = new StepControllerChange()
                            {
                                Channel = c.Channel,
                                MidiController = c.MidiController,
                                ControllerValue = c.RefVar.Value
                            };
                            MidiInterface.TheInterface.Send(step);
                        });
                    }
                }

                // Reset controllers for next go around.
                _ctrlChanges.Clear();

                ////// Neb steps /////
                if (_playing && e.ElapsedTimers.Contains("NEB"))
                {
                    if(_script != null)
                    {
                        // Package up the runtime stuff the script may need.
                        _script.RtVals.Playing = _playing;
                        _script.RtVals.StepTime = _stepTime;
                        _script.RtVals.RealTime = (float)(DateTime.Now - _startTime).TotalSeconds;
                        _script.RtVals.Speed = (float)potSpeed.Value;
                        _script.RtVals.Volume = sldVolume.Value;

                        _script.step();

                        // Process whatever the script may have done.
                        if (_script.RtVals.Speed != potSpeed.Value)
                        {
                            potSpeed.Value = _script.RtVals.Speed;
                            SetSpeedTimerPeriod();
                        }

                        if (_script.RtVals.Volume != sldVolume.Value)
                        {
                            sldVolume.Value = _script.RtVals.Volume;
                        }

                        _script.RtVals.RuntimeSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));
                        _script.RtVals.RuntimeSteps.DeleteSteps(_stepTime);

                        _script.PrintLines.ForEach(l => BeginInvoke((MethodInvoker)delegate () { infoDisplay.AddInfo(l); }));
                        _script.PrintLines.Clear();
                    }

                    // Do the compiled steps.
                    if (chkSequence.Checked)
                    {
                        _compiledSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));
                    }
                    timeMaster.ShowProgress = chkSequence.Checked;

                    // Local common function
                    void PlayStep(Step step)
                    {
                        Track track = ScriptEntities.Tracks[step.TrackName];

                        // Is it ok to play now?
                        bool _anySolo = ScriptEntities.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;
                        bool play = track != null && (track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo));

                        if (play)
                        {
                            if(step is StepSpecial)
                            {
                                _script.ExecScriptFunction((step as StepSpecial).Function);
                            }
                            else
                            {
                                // Maybe tweak values.
                                step.Adjust(sldVolume.Value, track.Volume, track.Modulate);
                                MidiInterface.TheInterface.Send(step);
                            }
                        }
                    }

                    ///// Bump time.
                    _stepTime.Advance();

                    ////// Check for end of play.
                    // If no steps or not selected, free running mode so always keep going.
                    if(_compiledSteps.Times.Count() != 0 && chkSequence.Checked)
                    {
                        // Check for end and loop condition.
                        if (_stepTime.Tick >= _compiledSteps.MaxTick)
                        {
                            if (chkLoop.Checked) // keep going
                            {
                                SetPlayStatus(PlayCommand.Rewind);
                            }
                            else // stop now
                            {
                                SetPlayStatus(PlayCommand.StopRewind);
                                MidiInterface.TheInterface.KillAll(); // just in case
                            }
                        }
                    }
                    // else keep going
                    SetPlayStatus(PlayCommand.UpdateUiTime);
                }

                ///// UI updates /////
                if (_script != null && e.ElapsedTimers.Contains("UI"))
                {
                    // Measure and alert if too slow, or throttle.
                    //_tanUi.Arm();

                    scriptSurface.UpdateSurface();
                }

                // In case there are lingering noteoffs that need to be processed.
                MidiInterface.TheInterface.Housekeep();
            }
            catch (Exception ex)
            {
                ProcessRunTimeError(ex);
            }
        }

        /// <summary>
        /// Process input midi event.
        /// Is it a midi controller? Look it up in the inputs.
        /// If not a ctlr or not found, send to the midi output, otherwise trigger listeners.
        /// </summary>
        void Midi_NebMidiInputEvent(object sender, MidiInterface.NebMidiInputEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                if (e.Step != null && e.Step is StepControllerChange)
                {
                    /////// Control change
                    StepControllerChange scc = e.Step as StepControllerChange;

                    bool handled = false;

                    // Process through our list.
                    if(_script != null)
                    {
                        IEnumerable<MidiControlPoint> ctlpts = ScriptEntities.InputMidis.Values.Where((c, m) => (
                            c.MidiController == scc.MidiController &&
                            c.Channel == scc.Channel));

                        if (ctlpts != null && ctlpts.Count() > 0)
                        {
                            ctlpts.ForEach(c =>
                            {
                                // Add to our list for processing at the next tock.
                                c.RefVar.Value = scc.ControllerValue;
                                _ctrlChanges.Add(c.RefVar.Name, c.RefVar);
                            });

                            handled = true;
                        }
                    }

                    if(!handled)
                    {
                        // Not one we are interested in so pass through.
                        MidiInterface.TheInterface.Send(e.Step);
                    }
                }
            });
        }

        /// <summary>
        /// Process midi log event.
        /// </summary>
        private void Midi_NebMidiLogEvent(object sender, MidiInterface.NebMidiLogEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                infoDisplay.AddMidiMessage($"{_stepTime} {e.Message}");
                if (e.Message.StartsWith("ERR"))
                {
                    _logger.Error(e.Message);
                }
            });
        }

        /// <summary>
        /// User has changed a track value. Interested in solo/mute.
        /// </summary>
        void TrackChange_Event(object sender, TrackControl.TrackChangeEventArgs e)
        {
            if (sender is TrackControl)
            {
                // Check for solos.
                bool _anySolo = ScriptEntities.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;

                if (_anySolo)
                {
                    // Kill any not solo.
                    ScriptEntities.Tracks.Values.ForEach(t => { if (t.State != TrackState.Solo) MidiInterface.TheInterface.Kill(t.Channel); });
                }
            }
        }

        /// <summary>
        /// UI change event. Add to our list for processing at the next tock.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Levers_Changed(object sender, Levers.LeverChangeEventArgs e)
        {
            _ctrlChanges.Add(e.RefVar.Name, e.RefVar);
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="ex"></param>
        void ProcessRunTimeError(Exception ex)
        {
            SetPlayStatus(PlayCommand.Stop);

            string srcFile = Utils.UNKNOWN_STRING;
            int srcLine = -1;

            // Locate the offending frame.
            StackTrace st = new StackTrace(ex, true);
            StackFrame sf = null;
            for (int i = 0; i < st.FrameCount;i++)
            {
                StackFrame stf = st.GetFrame(i);
                if(stf.GetFileName().ToUpper().Contains(_compileTempDir.ToUpper()))
                {
                    sf = stf;
                    break;
                }
            }

            if(sf != null)
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

                ScriptError err = new ScriptError()
                {
                    ErrorType = ScriptError.ScriptErrorType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = ex.Message
                };

                _logger.Error(err.ToString());
            }
            else // unknown?
            {
                ScriptError err = new ScriptError()
                {
                    ErrorType = ScriptError.ScriptErrorType.Runtime,
                    SourceFile = "",
                    LineNumber = -1,
                    Message = ex.Message
                };

                _logger.Error(err.ToString());
            }
        }
        #endregion

        #region File handling
        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        private void Recent_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string fn = sender.ToString();
            OpenFile(fn);
            UpdateMenu();
        }

        /// <summary>
        /// Allows the user to select a neb file from file system.
        /// </summary>
        private void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "Nebulator files (*.neb)|*.neb",
                Title = "Select a Nebulator file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openDlg.FileName);
                UpdateMenu();
            }
        }

        /// <summary>
        /// Common neb file opener.
        /// </summary>
        /// <param name="fn">The neb file to open.</param>
        public void OpenFile(string fn)
        {
            using (new WaitCursor())
            {
                try
                {
                    _logger.Info($"Reading neb file: {fn}");
                    _nebpVals = Bag.Load(fn.Replace(".neb", ".nebp"));
                    _fn = fn;
                    _dirtyFiles = true;
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, _attentionColor);
                    AddToRecentDefs(fn);
                    Text = $"Nebulator {Utils.GetVersionString()} - {fn}";

                    Compile();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Couldn't open the neb file: {fn} because: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        private void PopulateRecentMenu()
        {
            ToolStripItemCollection menuItems = recentToolStripMenuItem.DropDownItems;
            menuItems.Clear();

            foreach (string s in UserSettings.TheSettings.RecentFiles)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(s, null, new EventHandler(Recent_Click));
                menuItems.Add(menuItem);
            }
        }

        /// <summary>
        /// Update the mru with the user selection.
        /// </summary>
        /// <param name="fn">The selected file.</param>
        private void AddToRecentDefs(string fn)
        {
            if (File.Exists(fn))
            {
                UserSettings.TheSettings.RecentFiles.UpdateMru(fn);
                PopulateRecentMenu();
            }
        }

        /// <summary>
        /// One or more neb files have changed so reload/compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                //_logger.Info("Watcher_Changed");
                _dirtyFiles = true;
                btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, _attentionColor);
                UpdateMenu();
            });
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go or stop button.
        /// </summary>
        private void Play_Click(object sender, EventArgs e)
        {
            Play();
        }

        /// <summary>
        /// Update multimedia timer period.
        /// </summary>
        void Speed_ValueChanged(object sender, EventArgs e)
        {
            SetSpeedTimerPeriod();
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        void Rewind_Click(object sender, EventArgs e)
        {
            SetPlayStatus(PlayCommand.Rewind);
        }

        /// <summary>
        /// User updated volume.
        /// </summary>
        void Volume_ValueChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        private void Compile_Click(object sender, EventArgs e)
        {
            Compile();
            SetPlayStatus(PlayCommand.StopRewind);
            UpdateMenu();
        }

        /// <summary>
        /// User updated the time.
        /// </summary>
        void Time_ValueChanged(object sender, EventArgs e)
        {
            _stepTime = timeMaster.CurrentTime;
            SetPlayStatus(PlayCommand.UpdateUiTime);
        }
        #endregion

        #region Messages and logging
        /// <summary>
        /// Init all logging functions.
        /// </summary>
        void InitLogging()
        {
            string appDir = Utils.GetAppDir();

            FileInfo fi = new FileInfo(Path.Combine(appDir, "log.txt"));
            if(fi.Exists && fi.Length > 100000)
            {
                File.Copy(fi.FullName, fi.FullName.Replace("log.", "log2."), true);
                File.Delete(fi.FullName);
            }

            // Hook to client window.
            LogClientNotificationTarget.ClientNotification += Syslog_ClientNotification;
        }

        /// <summary>
        /// A message from the logger to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        void Syslog_ClientNotification(string msg)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                infoDisplay.AddInfoLine(msg);
            });
        }

        /// <summary>
        /// Show the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogShow_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "Log Viewer",
                Size = new Size(900, 600),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                TextViewer tv = new TextViewer() { Dock = DockStyle.Fill };
                f.Controls.Add(tv);
                tv.Colors.Add("|ERROR|", Color.Pink);
                tv.Colors.Add("|_WARN|", Color.Plum);
                tv.Colors.Add("|_INFO|", Color.LightGreen);

                string appDir = Utils.GetAppDir();
                string logFilename = Path.Combine(appDir, "log.txt");
                using (new WaitCursor())
                {
                    File.ReadAllLines(logFilename).ForEach(l => tv.AddLine(l, false));
                }

                f.ShowDialog();
            }
        }
        #endregion

        #region User settings
        /// <summary>
        /// Init the user settings.
        /// </summary>
        void InitSettings()
        {
            if (UserSettings.TheSettings.MainFormInfo.Width == 0)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                Location = new Point(UserSettings.TheSettings.MainFormInfo.X, UserSettings.TheSettings.MainFormInfo.Y);
                Size = new Size(UserSettings.TheSettings.MainFormInfo.Width, UserSettings.TheSettings.MainFormInfo.Height);
                WindowState = FormWindowState.Normal;
            }

            bool top = false;

            _piano.Size = new Size(UserSettings.TheSettings.PianoFormInfo.Width, UserSettings.TheSettings.PianoFormInfo.Height);
            _piano.Visible = UserSettings.TheSettings.PianoFormInfo.Visible;
            _piano.TopMost = top;

            // Now we can set the locations.
            _piano.Location = new Point(UserSettings.TheSettings.PianoFormInfo.X, UserSettings.TheSettings.PianoFormInfo.Y);

            splitContainerControl.SplitterDistance = UserSettings.TheSettings.ControlSplitterPos;
        }

        /// <summary>
        /// Save the user settings.
        /// </summary>
        void SaveSettings()
        {
            UserSettings.TheSettings.PianoFormInfo.FromForm(_piano);

            if (WindowState == FormWindowState.Maximized)
            {
                UserSettings.TheSettings.MainFormInfo.Width = 0; // indicates maximized
                UserSettings.TheSettings.MainFormInfo.Height = 0;
            }
            else
            {
                UserSettings.TheSettings.MainFormInfo.FromForm(this);
            }

            UserSettings.TheSettings.ControlSplitterPos = splitContainerControl.SplitterDistance;

            UserSettings.TheSettings.Save();
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        private void Settings_Click(object sender, EventArgs e)
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
                PropertyGridEx pg = new PropertyGridEx()
                {
                    Dock = DockStyle.Fill,
                    PropertySort = PropertySort.NoSort,
                    SelectedObject = UserSettings.TheSettings
                };

                // Supply the midi options. There should be a cleaner way than this but the ComponentModel is a hard wrestle.
                ListSelector.Options.Add("MidiIn", MidiInterface.TheInterface.MidiInputs);
                ListSelector.Options.Add("MidiOut", MidiInterface.TheInterface.MidiOutputs);

                // Detect changes of interest.
                List<string> propsChanged = new List<string>();
                pg.PropertyValueChanged += (sdr, args) => { propsChanged.Add(args.ChangedItem.PropertyDescriptor.Name); };

                f.Controls.Add(pg);
                f.ShowDialog();

                // Figure out what changed.
                bool midi = false;
                bool defs = false;
                bool ctrls = false;

                propsChanged.ForEach(p =>
                {
                    midi |= p.Contains("Midi");
                    defs |= (p.Contains("Notes") | p.Contains("Scales"));
                    ctrls |= (p.Contains("Font") | p.Contains("Color"));
                });

                if (midi)
                {
                    MidiInterface.TheInterface.Init();
                }

                if (defs)
                {
                    NoteUtils.Init();
                }

                if (ctrls)
                {
                    MessageBox.Show("UI changes require a restart to take effect.");
                }

                // Always safe to do this.
                SetUiTimerPeriod();
                SaveSettings();
            }
        }
        #endregion

        #region Piano
        /// <summary>
        /// Handle piano key event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_PianoKeyEvent(object sender, Piano.PianoKeyEventArgs e)
        {
            if(e.Down)
            {
                StepNoteOn step = new StepNoteOn()
                {
                    Channel = 2,
                    NoteNumberToPlay = Utils.Constrain(e.NoteId, 0, MidiInterface.MAX_MIDI_NOTE),
                    Velocity = 90,
                    VelocityToPlay = 90,
                    Duration = new Time(0)
                };
                MidiInterface.TheInterface.Send(step);
            }
            else
            {
                StepNoteOff step = new StepNoteOff()
                {
                    Channel = 2,
                    NoteNumber = Utils.Constrain(e.NoteId, 0, MidiInterface.MAX_MIDI_NOTE),
                    NoteNumberToPlay = Utils.Constrain(e.NoteId, 0, MidiInterface.MAX_MIDI_NOTE),
                    Velocity = 64
                };
                MidiInterface.TheInterface.Send(step);
            }
        }

        /// <summary>
        /// Turn piano on/off.
        /// </summary>
        private void Piano_Click(object sender, EventArgs e)
        {
            _piano.Visible = pianoToolStripMenuItem.Checked;
        }
        #endregion

        #region Play control
        /// <summary>
        /// Start or stop depending on current status.
        /// </summary>
        void Play()
        {
            if(_script == null)
            {
                _logger.Warn("No script file loaded");
                SetPlayStatus(PlayCommand.Stop);
            }
            else if (_playing)
            {
                ///// Stop!
                SetPlayStatus(PlayCommand.Stop);

                // Send midi stop all notes just in case.
                MidiInterface.TheInterface.KillAll();
            }
            else
            {
                ///// Start!
                bool ok = _dirtyFiles ? Compile() : true;

                if (ok)
                {
                    SetSpeedTimerPeriod();
                    // Init the script.
                    try
                    {
                        _script.setup();
                    }
                    catch (Exception ex)
                    {
                        ProcessRunTimeError(ex);
                    }
                }

                SetPlayStatus(ok ? PlayCommand.Start : PlayCommand.Stop);
            }
        }

        /// <summary>
        /// Update everything per param.
        /// </summary>
        /// <param name="cmd"></param>
        void SetPlayStatus(PlayCommand cmd)
        {
            switch (cmd)
            {
                case PlayCommand.Start:
                    chkPlay.Checked = true;
                    _playing = true;
                    _startTime = DateTime.Now;
                    break;

                case PlayCommand.Stop:
                    chkPlay.Checked = false;
                    _playing = false;
                    break;

                case PlayCommand.Rewind:
                    _stepTime.Reset();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    _playing = false;
                    _stepTime.Reset();
                    break;

                case PlayCommand.UpdateUiTime:
                    break;
            }

            timeMaster.CurrentTime = _stepTime;
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
                chkPlay.Checked = !chkPlay.Checked;
                Play();
                e.Handled = true;
            }
            else
            {
                // Pass along.
                e.Handled = false;
            }
        }

        /// <summary>
        /// Do some global key handling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Space)
            //{
            //    e.Handled = true;
            //}
            //else
            //{
            //    // Pass along.
            //    e.Handled = false;
            //}
        }

        /// <summary>
        /// Do some global key handling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            //// Watch for space bar which starts/stops play.
            //if(e.KeyChar == ' ')
            //{
            //    e.Handled = true;
            //}
            //else
            //{
            //    // Pass along - we don't care.
            //    e.Handled = false;
            //}
        }
        #endregion

        #region Timers
        /// <summary>
        /// Common func.
        /// </summary>
        void SetSpeedTimerPeriod()
        {
            // Convert speed/bpm to msec per tock.
            double ticksPerMinute = potSpeed.Value; // sec/tick, aka bpm
            double tocksPerMinute = ticksPerMinute * Time.TOCKS_PER_TICK;
            double tocksPerSec = tocksPerMinute / 60;
            double tocksPerMsec = tocksPerSec / 1000;
            double msecPerTock = 1 / tocksPerMsec;
            _nebTimer.SetTimer("NEB", (int)msecPerTock);
        }

        /// <summary>
        /// Common func.
        /// </summary>
        void SetUiTimerPeriod()
        {
            // Convert fps to msec per frame.
            double framesPerMsec = (double)UserSettings.TheSettings.FrameRate / 1000;
            double msecPerFrame = 1 / framesPerMsec;
            _nebTimer.SetTimer("UI", (int)msecPerFrame);
        }
        #endregion

        #region Internal stuff
        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object sender, EventArgs e)
        {
            About dlg = new About();
            dlg.ShowDialog();
        }

        /// <summary>
        /// Set menu/toolbar item enables according to system states.
        /// </summary>
        void UpdateMenu()
        {
            settingsToolStripMenuItem.Enabled = !_playing;
        }

        /// <summary>
        /// Colorize by theme.
        /// </summary>
        void InitControls()
        {
            BackColor = UserSettings.TheSettings.BackColor;

            // Stash the original image in the tag field.
            btnRewind.Image = Utils.ColorizeBitmap(btnRewind.Image, UserSettings.TheSettings.IconColor);

            btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);

            chkLoop.Image = Utils.ColorizeBitmap(chkLoop.Image, UserSettings.TheSettings.IconColor);
            chkLoop.BackColor = UserSettings.TheSettings.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            chkPlay.Image = Utils.ColorizeBitmap(chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            chkSequence.Image = Utils.ColorizeBitmap(chkSequence.Image, UserSettings.TheSettings.IconColor);
            chkSequence.BackColor = UserSettings.TheSettings.BackColor;
            chkSequence.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            potSpeed.ControlColor = UserSettings.TheSettings.IconColor;
            potSpeed.Font = UserSettings.TheSettings.ControlFont;
            potSpeed.Invalidate();

            sldVolume.ControlColor = UserSettings.TheSettings.ControlColor;
            sldVolume.Font = UserSettings.TheSettings.ControlFont;
            sldVolume.Invalidate();

            timeMaster.ControlColor = UserSettings.TheSettings.ControlColor;
            timeMaster.Invalidate();

            infoDisplay.BackColor = UserSettings.TheSettings.BackColor;
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export steps to a midi file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportMidi_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                Filter = "Midi files (*.mid)|*.mid",
                Title = "Export to midi file",
                FileName = _fn.Replace(".neb", ".mid")
            };

            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                ExportMidi(saveDlg.FileName);
            }
        }

        /// <summary>
        /// Output filename.
        /// </summary>
        /// <param name="fn"></param>
        void ExportMidi(string fn)
        {
            Dictionary<int, string> tracks = new Dictionary<int, string>();
            ScriptEntities.Tracks.Values.ForEach(t => tracks.Add(t.Channel, t.Name));

            // Convert speed/bpm to sec per tick.
            double ticksPerMinute = potSpeed.Value; // bpm
            double ticksPerSec = ticksPerMinute / 60;
            double secPerTick = 1 / ticksPerSec;

            MidiUtils.ExportMidi(_compiledSteps, fn, tracks, secPerTick, "Converted from " + _fn);
        }

        /// <summary>
        /// Import a style file as neb file lines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportStyle_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "Style files (*.sty)|*.sty",
                Title = "Import from style file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                var v = MidiUtils.ImportStyle(openDlg.FileName);
                Clipboard.SetText(string.Join(Environment.NewLine, v));
                MessageBox.Show("Style file content is in the clipboard");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KillMidi_Click(object sender, EventArgs e)
        {
            MidiInterface.TheInterface.KillAll();
        }
        #endregion
    }
}
