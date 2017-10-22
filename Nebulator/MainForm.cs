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
using Nebulator.Model;
using Nebulator.Engine;
using Nebulator.UI;
using Nebulator.FastTimer;
using Nebulator.Midi;

// TODO space bar to start stop.


namespace Nebulator
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Fast timer.</summary>
        IFastTimer _timer = null;

        /// <summary>Piano child form.</summary>
        Piano _piano = new Piano();

        /// <summary>The current script.</summary>
        Script _script = null;

        /// <summary>The compiled midi event sequence.</summary>
        StepCollection _steps = new StepCollection();

        /// <summary>Accumulated control input var changes to be processed at next step.</summary>
        LazyCollection<Variable> _ctrlChanges = new LazyCollection<Variable>() { AllowOverwrite = true };

        /// <summary>Diagnostics for timing measurement.</summary>
        TimingAnalyzer _tan = new TimingAnalyzer();

        /// <summary>Current neb file name.</summary>
        string _fn = Globals.UNKNOWN_STRING;

        /// <summary>Detect changed composition files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally, will require a reload.</summary>
        bool _dirtyFiles = false;

        /// <summary>Indicates needs compilation.</summary>
        Color _needCompile = Color.Red;
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
            Globals.UserSettings = UserSettings.Load(appDir);
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
            Globals.MidiInterface.NebMidiInputEvent += Midi_NebMidiInputEvent;
            Globals.MidiInterface.NebMidiLogEvent += Midi_NebMidiLogEvent;

            Globals.MidiInterface.Init();

            // Midi output timer.
            _timer = new NebTimer()
            {
                NebPeriod = 10,
                UiPeriod = 30
            };
            _timer.TickEvent += FastTimer_TickEvent;
            _timer.Start();
            #endregion

            #region Piano
            pianoToolStripMenuItem.Checked = Globals.UserSettings.PianoFormInfo.Visible;
            _piano.Visible = Globals.UserSettings.PianoFormInfo.Visible;
            _piano.PianoKeyDown += Piano_KeyDown;
            _piano.PianoKeyUp += Piano_KeyUp;
            #endregion

            InitControls();

            _watcher.FileChangeEvent += Watcher_Changed;

            levers.LeverChangeEvent += Levers_Changed;

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                OpenFile(args[1]);
            }

            UpdateMenu();


#if DEBUG
            OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\declarative.neb");
            //OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\algorithmic.neb");
            //MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
#endif
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Just in case.
                Globals.MidiInterface.KillAll();

                // Save the project.
                Globals.CurrentPersisted.Values.Clear();
                GetTrackControls().ForEach(c => Globals.CurrentPersisted.SetValue(c.TrackInfo.Name, "volume", c.TrackInfo.Volume));
                Globals.CurrentPersisted.Save();

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
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;

                Globals.MidiInterface?.Stop();
                Globals.MidiInterface?.Dispose();
                Globals.MidiInterface = null;

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
            Compiler compiler = new Compiler();
            _script = compiler.Execute(_fn);

            // Update file watcher just in case.
            _watcher.Clear();
            compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

            timeMaster.TimeDefs = compiler.TimeDefs;

            if (compiler.Errors.Count == 0 && _script != null)
            {
                btnCompile.BackColor = Globals.UserSettings.BackColor;
                _dirtyFiles = false;

                _steps = compiler.ConvertToSteps();

                Globals.Dynamic = _script.Dynamic;
                _script.ScriptEvent += Script_ScriptEvent;
                InitMainUi();
                levers.Init(_script.Surface);
                // Init the script.
                _script.setup();
            }
            else
            {
                _logger.Warn("Compile failed.");
                ok = false;
                SetPlayStatus(false);
                compiler.Errors.ForEach(e => _logger.Warn(e.ToString()));

                btnCompile.BackColor = _needCompile;
                _dirtyFiles = true;
            }

            return ok;
        }

        /// <summary>
        /// Create the main UI parts from the composition.
        /// </summary>
        void InitMainUi()
        {
            ///// Clean up current.
            GetTrackControls().ForEach(c => splitContainerMain.Panel1.Controls.Remove(c));

            ///// Set up UI.
            const int CONTROL_SPACING = 10;
            int x = timeMaster.Right + CONTROL_SPACING;

            ///// The track controls.
            foreach (Track t in Globals.Dynamic.Tracks.Values)
            {
                // Init from persistence.
                t.Volume = Globals.CurrentPersisted.GetValue(t.Name, "volume");

                TrackControl trk = new TrackControl()
                {
                    Location = new Point(x, 0), // txtTime.Top),
                    TrackInfo = t
                };
                trk.TrackChangeEvent += TrackChange_Event;
                splitContainerMain.Panel1.Controls.Add(trk);
                x += trk.Width + CONTROL_SPACING;
            }

            ///// Misc controls.
            potSpeed.Value = Globals.CurrentPersisted.Speed;
            sldVolume.Value = Globals.CurrentPersisted.Volume;
            timeMaster.MaxMajor = _steps.MaxTick;

            UpdateTime(true);
            UpdateMenu();
        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void FastTimer_TickEvent(object sender, FastTimerEventArgs e)
        {
            if (Globals.UserSettings.TimerStats && e.NebEvent)
            {
                // Do some stats gathering for measuring jitter.
                _tan.Grab();
                if (_tan.Count >= 50)
                {
                    _tan.Stop();
                    _logger.Info($"#### {_tan}");
                    _tan.Clear();
                }
            }

            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                DoNext(e);
            });
        }

        /// <summary>
        /// Output next time/step.
        /// </summary>
        /// <param name="e">Information about updates required.</param>
        void DoNext(FastTimerEventArgs e)
        {
            try
            {
                ////// Neb steps /////
                if (e.NebEvent && Globals.Playing)
                {
                    // Go through changed vars list.
                    foreach (Variable var in _ctrlChanges.Values)
                    {
                        // Execute any script handlers.
                        _script.ExecScriptFunction(var.Name);

                        // Output any midiout controllers.
                        IEnumerable<MidiControlPoint> ctlpts = Globals.Dynamic.OutputMidis.Values.Where(c => c.RefVar.Name == var.Name);

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

                                Globals.MidiInterface.Send(step);
                            });
                        }
                    }

                    // Reset controllers for next go around.
                    _ctrlChanges.Clear();

                    // Do the steps.
                    foreach (Step step in _steps.GetSteps(Globals.CurrentStepTime))
                    {
                        Track track = step.Tag as Track;
                        bool _anySolo = Globals.Dynamic.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;
                        bool play = track != null && (track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo));
                        if (play)
                        {
                            // Maybe tweak values.
                            step.Adjust(track.Volume, track.Modulate);
                            Globals.MidiInterface.Send(step);
                        }
                    }

                    // Bump.
                    Globals.CurrentStepTime.Advance();

                    bool keepGoing = true;

                    // If no steps, free running mode so always keep going.
                    if(_steps.Count != 0)
                    {
                        // Check for end and loop condition.
                        if (Globals.CurrentStepTime.Tick >= _steps.MaxTick)
                        {
                            UpdateTime(true); // reset to beginning.
                            keepGoing = chkLoop.Checked;
                            if (!keepGoing)
                            {
                                Globals.MidiInterface.KillAll(); // just in case
                            }
                        }
                    }

                    // Do any script execute stuff.
                    _script?.step();

                    SetPlayStatus(keepGoing);
                    UpdateTime(false);

                    // In case there are noteoff that need to be processed.
                    Globals.MidiInterface.Housekeep();
                }

                ///// UI updates /////
                if (e.UiEvent)
                {
                    _script?.Render();
                }
            }
            catch (Exception ex)
            {
                // Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
                string srcFile = Globals.UNKNOWN_STRING;
                int srcLine = -1;

                // FUTURE Could use StackTrace(ex) instead, maybe.
                foreach (string s in ex.StackTrace.SplitByTokens(Environment.NewLine))
                {
                    if (s.Contains("Nebulator.UserScript"))
                    {
                        // The line we are interested in.
                        List<string> parts = s.SplitByToken(" in ");
                        parts = parts.Last().SplitByToken(":line ");
                        // Dig out generated file parts.
                        string genFile = parts.First();
                        int genLine = int.Parse(parts.Last()) - 1;

                        // Open the generated file and dig out the source file and line.
                        string[] genLines = File.ReadAllLines(genFile);

                        srcFile = genLines[0].Trim().Replace("//", "");

                        int ind = genLines[genLine].LastIndexOf("//");
                        if (ind != -1)
                        {
                            string sl = genLines[genLine].Substring(ind + 2);
                            int.TryParse(sl, out srcLine);
                        }

                        break;
                    }
                }

                ScriptError err = new ScriptError()
                {
                    ErrorType = ScriptErrorType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = ex.Message
                };

                _logger.Error(err.ToString());
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
                    // Process through our list.
                    IEnumerable<MidiControlPoint> ctlpts = Globals.Dynamic.InputMidis.Values.Where((c, m) => (
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
                    }
                    else
                    {
                        // Not one we are interested in so pass through.
                        Globals.MidiInterface.Send(e.Step);
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
                midiMonitor.AddMidiMessage(e.Message);
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
                IEnumerable<TrackControl> ctls = GetTrackControls();
                bool _anySolo = ctls.Where(t => t.State == TrackState.Solo).Count() > 0;

                // Kill any not solo.
                if (_anySolo)
                {
                    ctls.ForEach(c => { if (c.TrackInfo.State != TrackState.Solo) Globals.MidiInterface.Kill(c.TrackInfo.Channel); });
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
        private void OpenFile(string fn)
        {
            using (new WaitCursor())
            {
                try
                {
                    _logger.Info($"Reading neb file: {fn}");
                    Globals.CurrentPersisted = Persisted.Load(fn.Replace(".neb", ".nebp"));
                    _fn = fn;
                    _dirtyFiles = true;
                    btnCompile.BackColor = _needCompile;
                    AddToRecentDefs(fn);
                    Text = $"Nebulator {Utils.GetVersionString()} - {fn}";
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

            foreach (string s in Globals.UserSettings.RecentFiles)
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
                Globals.UserSettings.RecentFiles.UpdateMru(fn);
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
                btnCompile.BackColor = _needCompile;
                UpdateMenu();
            });
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go back.
        /// </summary>
        private void Rewind_Click(object sender, EventArgs e)
        {
            UpdateTime(true);
        }

        /// <summary>
        /// Go or stop.
        /// </summary>
        private void Play_Click(object sender, EventArgs e)
        {
            if(chkPlay.Checked)
            {
                SetPlayStatus(false);

                bool ok = true;

                if(_dirtyFiles)
                {
                    ok = Compile();
                    UpdateTime(true);
                }

                if(ok)
                {
                    SetTimerPeriod();
                    SetPlayStatus(true);
                }
            }
            else
            {
                SetPlayStatus(false);
                // Send midi stop all notes, stop sequencer.
                Globals.MidiInterface.KillAll();
            }
        }

        /// <summary>
        /// Update multimedia timer period.
        /// </summary>
        void Speed_ValueChanged(object sender, EventArgs e)
        {
            Globals.CurrentPersisted.Speed = (int)(potSpeed.Value);
            SetTimerPeriod();
        }

        /// <summary>
        /// Common func.
        /// </summary>
        void SetTimerPeriod()
        {
            _timer.NebPeriod = (int)(1000 * Globals.CurrentPersisted.Speed / Globals.TOCKS_PER_TICK);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Volume_ValueChanged(object sender, EventArgs e)
        {
            Globals.CurrentPersisted.Volume = sldVolume.Value;
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Compile_Click(object sender, EventArgs e)
        {
            Compile();
            UpdateMenu();
            UpdateTime(true);
        }

        /// <summary>
        /// User updated the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Time_ValueChanged(object sender, EventArgs e)
        {
            Globals.CurrentStepTime.Tick = timeMaster.Major;
            Globals.CurrentStepTime.Tock = timeMaster.Minor;
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
                infoDisplay.AddMessage(msg + Environment.NewLine);
            });
        }

        /// <summary>
        /// A message from the script to display to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Script_ScriptEvent(object sender, Script.ScriptEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                infoDisplay.AddMessage(e.Message);
            });
        }

        /// <summary>
        /// Show the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Log_Click(object sender, EventArgs e)
        {
            string appDir = Utils.GetAppDir();
            string logFilename = Path.Combine(appDir, "log.txt");
            List<string> lines = File.ReadAllLines(logFilename).ToList();
            TextViewer lv = new TextViewer() { Lines = lines, Text = "Log Viewer" };
            lv.ShowDialog();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Get the user settings.
        /// </summary>
        void InitSettings()
        {
            if (Globals.UserSettings.MainFormInfo.Width == 0)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                Location = new Point(Globals.UserSettings.MainFormInfo.X, Globals.UserSettings.MainFormInfo.Y);
                Size = new Size(Globals.UserSettings.MainFormInfo.Width, Globals.UserSettings.MainFormInfo.Height);
                WindowState = FormWindowState.Normal;
            }

            bool top = false;

            _piano.Size = new Size(Globals.UserSettings.PianoFormInfo.Width, Globals.UserSettings.PianoFormInfo.Height);
            _piano.Visible = Globals.UserSettings.PianoFormInfo.Visible;
            _piano.TopMost = top;

            // Now we can set the locations.
            _piano.Location = new Point(Globals.UserSettings.PianoFormInfo.X, Globals.UserSettings.PianoFormInfo.Y);

            splitContainerControl.SplitterDistance = Globals.UserSettings.ControlSplitterPos;
        }

        /// <summary>
        /// Save the user settings.
        /// </summary>
        void SaveSettings()
        {
            Globals.UserSettings.PianoFormInfo.FromForm(_piano);

            if (WindowState == FormWindowState.Maximized)
            {
                Globals.UserSettings.MainFormInfo.Width = 0; // indicates maximized
                Globals.UserSettings.MainFormInfo.Height = 0;
            }
            else
            {
                Globals.UserSettings.MainFormInfo.FromForm(this);
            }

            Globals.UserSettings.ControlSplitterPos = splitContainerControl.SplitterDistance;

            Globals.UserSettings.Save();
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        private void Settings_Click(object sender, EventArgs e)
        {
            using (PropertyEditor f = new PropertyEditor() { EditObject = Globals.UserSettings })
            {
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(MousePosition.X + 20, MousePosition.Y + 20);
                DialogResult dr = f.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    Globals.MidiInterface.Init();
                    SaveSettings();
                    if(f.Dirty)
                    {
                        MessageBox.Show("Changes require a restart to take effect."); // FUTURE Update without restarting.
                    }
                }
            }
        }
        #endregion

        #region Piano
        /// <summary>
        /// Handle piano key down event.
        /// </summary>
        private void Piano_KeyDown(object sender, PianoKeyEventArgs e)
        {
            StepNoteOn step = new StepNoteOn()
            {
                Channel = 2,
                NoteNumberToPlay = Utils.Constrain(e.NoteID, 0, MidiInterface.MAX_MIDI_NOTE),
                VelocityToPlay = 90,
                Duration = 0
            };
            Globals.MidiInterface.Send(step);
        }

        /// <summary>
        /// Handle piano key up event.
        /// </summary>
        private void Piano_KeyUp(object sender, PianoKeyEventArgs e)
        {
            StepNoteOff step = new StepNoteOff()
            {
                Channel = 2,
                NoteNumber = Utils.Constrain(e.NoteID, 0, MidiInterface.MAX_MIDI_NOTE),
                NoteNumberToPlay = Utils.Constrain(e.NoteID, 0, MidiInterface.MAX_MIDI_NOTE),
                Velocity = 64
            };
            Globals.MidiInterface.Send(step);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Piano_Click(object sender, EventArgs e)
        {
            _piano.Visible = pianoToolStripMenuItem.Checked;
        }
        #endregion

        #region Internal stuff
        /// <summary>
        /// Common play function.
        /// </summary>
        /// <param name="play"></param>
        void SetPlayStatus(bool play)
        {
            chkPlay.Checked = play;
            Globals.Playing = play;
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            //chkPlay.Enabled = btnCompile.BackColor != _needCompile;
            settingsToolStripMenuItem.Enabled = !Globals.Playing;
        }

        /// <summary>
        /// Update the global time and the UI.
        /// </summary>
        void UpdateTime(bool reset)
        {
            if (reset)
            {
                Globals.CurrentStepTime.Reset();
            }

            timeMaster.Major = Globals.CurrentStepTime.Tick;
            timeMaster.Minor = Globals.CurrentStepTime.Tock;
        }

        /// <summary>
        /// Colorize by theme.
        /// </summary>
        void InitControls()
        {
            BackColor = Globals.UserSettings.BackColor;

            btnRewind.Image = Utils.ColorizeBitmap(btnRewind.Image);

            btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image);

            chkLoop.Image = Utils.ColorizeBitmap(chkLoop.Image);
            chkLoop.BackColor = Globals.UserSettings.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;

            chkPlay.Image = Utils.ColorizeBitmap(chkPlay.Image);
            chkPlay.BackColor = Globals.UserSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;

            potSpeed.ControlColor = Globals.UserSettings.IconColor;
            potSpeed.Font = Globals.UserSettings.ControlFont;
            potSpeed.Invalidate();

            sldVolume.ControlColor = Globals.UserSettings.ControlColor;
            sldVolume.Font = Globals.UserSettings.ControlFont;
            sldVolume.Invalidate();

            timeMaster.ControlColor = Globals.UserSettings.ControlColor;
            timeMaster.Invalidate();
        }

        /// <summary>
        /// Helper.
        /// </summary>
        /// <returns></returns>
        IEnumerable<TrackControl> GetTrackControls()
        {
            List<TrackControl> ctls = new List<TrackControl>();
            foreach (Control ctl in splitContainerMain.Panel1.Controls)
            {
                if (ctl is TrackControl)
                {
                    ctls.Add(ctl as TrackControl);
                }
            }
            return ctls;
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
                Dictionary<int, string> tracks = new Dictionary<int, string>();
                Globals.Dynamic.Tracks.Values.ForEach(t => tracks.Add(t.Channel, t.Name));
                MidiUtils.ExportMidi(_steps, saveDlg.FileName, tracks, Globals.CurrentPersisted.Speed, "Converted from " + _fn);
            }
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
                MidiUtils.ImportStyle(openDlg.FileName);
            }
        }
        #endregion
    }
}
