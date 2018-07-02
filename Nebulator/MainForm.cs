using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NLog;
using MoreLinq;
using Newtonsoft.Json;
using Nebulator.Common;
using Nebulator.Controls;
using Nebulator.Script;
using Nebulator.Protocol;
using Nebulator.Server;


// TODO Get rid of the s.XXX rqmt like: ScriptSyntax.md: s.print("DoIt got:", val); use closures? Or make everything static?
// TODO still don't like the ui/sound start/stop controls...
// TODO test all changed midi/controller/pitch/noteonoff stuff.
// IProtocol _device1. TODO support multiple and OSC. Need to change DynamicElements.XXXControls, UserSettings, etc too.


namespace Nebulator
{
    public partial class MainForm : Form
    {
        #region Enums
        /// <summary>Internal status.</summary>
        enum PlayCommand { Start, Stop, Rewind, StopRewind, UpdateUiTime }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Fast timer.</summary>
        NebTimer _nebTimer = new NebTimer();

        /// <summary>Surface child form.</summary>
        Surface _surface = new Surface();

        /// <summary>Piano child form.</summary>
        Piano _piano = new Piano();

        /// <summary>The current script.</summary>
        ScriptCore _script = null;

        /// <summary>Frame rate in fps.</summary>
        int _frameRate = 30;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Current step time clock.</summary>
        Time _stepTime = new Time();

        /// <summary>The compiled event sequence.</summary>
        StepCollection _compiledSteps = new StepCollection();

        /// <summary>Script compile errors and warnings.</summary>
        List<ScriptError> _compileResults = new List<ScriptError>();

        /// <summary>Accumulated control input var changes to be processed at next step.</summary>
        Dictionary<string, NVariable> _ctrlChanges = new Dictionary<string, NVariable>();

        /// <summary>Current neb file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;

        /// <summary>Detect changed composition files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for tracking down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current neb/nebp file.</summary>
        Bag _nebpVals = new Bag();

        /// <summary>Indicates needs user involvement.</summary>
        Color _attentionColor = Color.Red;

        /// <summary>Diagnostics for timing measurement.</summary>
        TimingAnalyzer _tanUi = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Diagnostics for timing measurement.</summary>
        TimingAnalyzer _tanNeb = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Server host.</summary>
        SelfHost _selfHost = null;

        /// <summary>The in/out device.</summary>
        IProtocol _device1 = new Midi.MidiInterface();
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
        void MainForm_Load(object sender, EventArgs e)
        {
            #region Init main UI from settings
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

            _surface.Size = new Size(UserSettings.TheSettings.SurfaceFormInfo.Width, UserSettings.TheSettings.SurfaceFormInfo.Height);
            _surface.Visible = true;
            _surface.TopMost = UserSettings.TheSettings.LockUi;
            _surface.Location = new Point(UserSettings.TheSettings.SurfaceFormInfo.X, UserSettings.TheSettings.SurfaceFormInfo.Y);

            _piano.Size = new Size(UserSettings.TheSettings.PianoFormInfo.Width, UserSettings.TheSettings.PianoFormInfo.Height);
            _piano.Visible = UserSettings.TheSettings.PianoFormInfo.Visible;
            _piano.TopMost = false;
            _piano.Location = new Point(UserSettings.TheSettings.PianoFormInfo.X, UserSettings.TheSettings.PianoFormInfo.Y);
            pianoToolStripMenuItem.Checked = UserSettings.TheSettings.PianoFormInfo.Visible;
            _piano.PianoKeyEvent += Piano_PianoKeyEvent;
            #endregion

            #region App innards
            InitLogging();

            PopulateRecentMenu();
            #endregion

            #region Set up devices
            // Input events.
            _device1.ProtocolInputEvent += Midi_InputEvent;
            _device1.ProtocolLogEvent += Midi_LogEvent;
            _device1.Init();

            // Midi timer.
            _nebTimer = new NebTimer();
            SetSpeedTimerPeriod();
            SetUiTimerPeriod();
            _nebTimer.TimerElapsedEvent += TimerElapsedEvent;
            _nebTimer.Start();
            #endregion

            #region Misc setups
            InitControls();

            _watcher.FileChangeEvent += Watcher_Changed;

            levers.LeverChangeEvent += Levers_Changed;

            NoteUtils.Init();

            Text = $"Nebulator {Utils.GetVersionString()} - No file loaded";

            // Catches runtime errors during drawing.
            _surface.RuntimeErrorEvent += (object esender, Surface.RuntimeErrorEventArgs eargs) => { ProcessScriptRuntimeError(eargs); };

            // Init server.
            _selfHost = new SelfHost();
            SelfHost.RequestEvent += SelfHost_RequestEvent;
            Task.Run(() => { _selfHost.Run(); });
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
            //OpenFile(@"C:\Dev\Nebulator\Examples\example.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\airport.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\lsys.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\gol.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\boids.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\generative1.neb");
            //OpenFile(@"C:\Dev\Nebulator\Examples\generative2.neb");
            OpenFile(@"C:\Dev\Nebulator\Dev\dev.neb");
            //OpenFile(@"C:\Dev\Nebulator\Dev\nptest.neb");


            //// Server debug stuff
            //TestClient client = new TestClient();
            //Task.Run(async () => { await client.Run(); });


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
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ProcessPlay(PlayCommand.Stop, false);

                // Just in case.
                _device1.Kill();

                if(_script != null)
                {
                    // Save the project.
                    _nebpVals.Clear();
                    _nebpVals.SetValue("master", "volume", sldVolume.Value);
                    _nebpVals.SetValue("master", "speed", potSpeed.Value);
                    _nebpVals.SetValue("master", "sequence", chkSeq.Checked);
                    _nebpVals.SetValue("master", "ui", chkUi.Checked);
                    _nebpVals.SetValue("master", "seq", chkSeq.Checked);
                    DynamicElements.Tracks.ForEach(c => _nebpVals.SetValue(c.Name, "volume", c.Volume));
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

                _selfHost?.Dispose();

                _device1?.Stop();
                _device1?.Dispose();
                _device1 = null;

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Server processing
        /// <summary>
        /// Process a request from the web api. Set the e.Result to a json string. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SelfHost_RequestEvent(object sender, SelfHost.RequestEventArgs e)
        {
            Console.WriteLine($"MainForm Web request:{e.Request}");

            // What is wanted?
            List<string> parts = e.Request.SplitByToken("/");
            string[] validCmds = { "open", "compile", "start", "stop", "rewind" };

            switch (parts[0])
            {
                case "open":
                    string fn = string.Join("/", parts.GetRange(1, parts.Count - 1));
                    string res = OpenFile(fn);
                    e.Result = JsonConvert.SerializeObject(res == "" ? SelfHost.OK_NO_DATA : res, Formatting.Indented);
                    break;

                case "compile":
                    bool compok = Compile();
                    e.Result = JsonConvert.SerializeObject(_compileResults, Formatting.Indented);
                    break;

                case "start":
                    bool playok = false;
                    playok = ProcessPlay(PlayCommand.Start, false);
                    e.Result = JsonConvert.SerializeObject(playok ? SelfHost.OK_NO_DATA : SelfHost.FAIL, Formatting.Indented);
                    break;

                case "stop":
                    ProcessPlay(PlayCommand.Stop, false);
                    e.Result = JsonConvert.SerializeObject(SelfHost.OK_NO_DATA, Formatting.Indented);
                    break;

                case "rewind":
                    ProcessPlay(PlayCommand.Rewind, false);
                    e.Result = JsonConvert.SerializeObject(SelfHost.OK_NO_DATA, Formatting.Indented);
                    break;

                default:
                    // Invalid.
                    e.Result = JsonConvert.SerializeObject(null, Formatting.Indented);
                    break;
            }

            Console.WriteLine($"MainForm Result:{e.Result}");
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

                // Process errors. Some may be warnings.
                _compileResults = compiler.Errors;
                int errorCount = _compileResults.Count(w => w.ErrorType == ScriptError.ScriptErrorType.Error);

                if (errorCount == 0 && _script != null)
                {
                    SetCompileStatus(true);
                    _compileTempDir = compiler.TempDir;
                    _script.Protocol = _device1;

                    try
                    {
                        // Surface area.
                        InitRuntime();

                        _script.setupNeb();

                        _surface.InitSurface(_script);

                        ProcessRuntime();

                        ConvertToSteps();

                        // Show everything.
                        InitScriptUi();
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                        ok = false;
                    }

                    SetCompileStatus(ok);
                }
                else
                {
                    _logger.Warn("Compile failed.");
                    ok = false;
                    ProcessPlay(PlayCommand.StopRewind, false);
                    SetCompileStatus(false);
                }

                _compileResults.ForEach(r =>
                {
                    if (r.ErrorType == ScriptError.ScriptErrorType.Warning)
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
        /// Convert the script output into steps for feeding devices.
        /// </summary>
        void ConvertToSteps()
        {
            // Convert compiled stuff to step collection.
            _compiledSteps.Clear();

            foreach (NSection sect in DynamicElements.Sections)
            {
                // Collect important times.
                timeMaster.TimeDefs.Add(new Time(sect.Start, 0), sect.Name);

                // Iterate through the sections tracks.
                foreach (NSectionTrack strack in sect.SectionTracks)
                {
                    // For processing current Sequence.
                    int seqOffset = sect.Start;

                    // Gen steps for each sequence.
                    foreach (NSequence seq in strack.Sequences)
                    {
                        try
                        {
                            StepCollection stepsToAdd = ScriptCore.ConvertToSteps(strack.ParentTrack, seq, seqOffset);
                            _compiledSteps.Add(stepsToAdd);
                            seqOffset += seq.Length;

                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error in the sequences for NTrack {strack.ParentTrack.Name} : {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create the main UI parts from the script.
        /// </summary>
        void InitScriptUi()
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

            foreach (NTrack t in DynamicElements.Tracks)
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
            chkSeq.Checked = Convert.ToBoolean(_nebpVals.GetValue("master", "sequence"));
            chkUi.Checked = Convert.ToBoolean(_nebpVals.GetValue("master", "ui"));

            sldVolume.Value = mv == 0 ? 90 : mv; // in case it's new
            timeMaster.MaxTick = _compiledSteps.MaxTick;
            //ProcessPlay(PlayCommand.StopRewind, false);

            ///// Init the user input area.
            // Levers.
            levers.Init(DynamicElements.Levers);
        }

        /// <summary>
        /// Update system statuses.
        /// </summary>
        /// <param name="compileStatus"></param>
        void SetCompileStatus(bool compileStatus)
        {
            if (compileStatus)
            {
                btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);
                _needCompile = false;
            }
            else
            {
                btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, _attentionColor);
                _needCompile = true;
            }

        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void TimerElapsedEvent(object sender, NebTimer.TimerEventArgs e)
        {
            //// Do some stats gathering for measuring jitter.
            //if ( _tanTimer.Grab())
            //{
            //    _logger.Info($"Midi timing: {stats}");
            //}

            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                if (_script != null)
                {
                    NextStep(e);
                }
            });
        }

        /// <summary>
        /// Output next time/step.
        /// </summary>
        /// <param name="e">Information about updates required.</param>
        void NextStep(NebTimer.TimerEventArgs e)
        {
            ////// Process changed vars regardless of any other status //////
            foreach (NVariable var in _ctrlChanges.Values)
            {
                // Output any out controllers.
                foreach(NControlPoint c in DynamicElements.OutputControls)
                {
                    if(c.BoundVar.Name == var.Name)
                    {
                        StepControllerChange step = new StepControllerChange()
                        {
                            Channel = c.Track.Channel,
                            ControllerId = c.ControllerId,
                            Value = c.BoundVar.Value
                        };
                        _device1.Send(step);
                    }
                }
            }

            // Reset controllers for next go around.
            _ctrlChanges.Clear();

            InitRuntime();

            ////// Neb steps /////
            if (chkPlay.Checked && e.ElapsedTimers.Contains("NEB") && !_needCompile)
            {
                //_tanNeb.Arm();

                // Kick it.
                try
                {
                    _script.step();
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                }

                //if (_tanNeb.Grab())
                //{
                //    _logger.Info("NEB tan: " + _tanNeb.ToString());
                //}

                // Process any sequence steps the script added.
                RuntimeContext.RuntimeSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));
                RuntimeContext.RuntimeSteps.DeleteSteps(_stepTime);

                // Now do the compiled steps.
                if (chkSeq.Checked)
                {
                    _compiledSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));
                }
                timeMaster.ShowProgress = chkSeq.Checked;

                ///// Bump time.
                _stepTime.Advance();

                ////// Check for end of play.
                // If no steps or not selected, free running mode so always keep going.
                if(_compiledSteps.Times.Count() != 0 && chkSeq.Checked)
                {
                    // Check for end and loop condition.
                    if (_stepTime.Tick >= _compiledSteps.MaxTick)
                    {
                        ProcessPlay(PlayCommand.StopRewind, false);
                        _device1.Kill(); // just in case
                    }
                }
                // else keep going

                ProcessPlay(PlayCommand.UpdateUiTime, false);
            }

            ///// UI updates /////
            if (e.ElapsedTimers.Contains("UI") && chkUi.Checked && !_needCompile)
            {
                //_tanUi.Arm();

                try
                {
                    _surface.UpdateSurface();
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                }

                //if (_tanUi.Grab())
                //{
                //    _logger.Info("UI tan: " + _tanUi.ToString());
                //}
            }

            ProcessRuntime();

            ///// Process any lingering noteoffs. /////
            _device1.Housekeep();

            ///// Local common function /////
            void PlayStep(Step step)
            {
                if(DynamicElements.Tracks.Count > 0)
                {
                    NTrack track = DynamicElements.Tracks.Where(t => t.Channel == step.Channel).First();

                    // Is it ok to play now?
                    bool _anySolo = DynamicElements.Tracks.Where(t => t.State == TrackState.Solo).Count() > 0;
                    bool play = track != null && (track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo));

                    if (play)
                    {
                        if (step is StepInternal)
                        {
                            try
                            {
                                (step as StepInternal).ScriptFunction();
                            }
                            catch (Exception ex)
                            {
                                ProcessScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                            }
                        }
                        else
                        {
                            // Maybe tweak values.
                            step.Adjust(_device1.Caps, sldVolume.Value, track.Volume);
                            _device1.Send(step);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process input midi event.
        /// Is it a midi controller? Look it up in the inputs.
        /// If not a ctlr or not found, send to the midi output, otherwise trigger listeners.
        /// </summary>
        void Midi_InputEvent(object sender, ProtocolInputEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                e.Handled = false; // default

                if (_script != null && e.Step != null)
                {
                    if(e.Step is StepNoteOn || e.Step is StepNoteOff)
                    {
                        int channel = (e.Step as Step).Channel;
                        // Dig out the note number. Note sign change for note off.
                        int value = (e.Step is StepNoteOn) ? (e.Step as StepNoteOn).NoteNumber : - (e.Step as StepNoteOff).NoteNumber;

                        // Process through our list of inputs of interest.
                        foreach (NControlPoint ctlpt in DynamicElements.InputControls)
                        {
                            if (ctlpt.ControllerId == ControllerType.NOTE && ctlpt.Track.Channel == channel)
                            {
                                // Add to our list for processing at the next tock.
                                ctlpt.BoundVar.Value = value;
                                _ctrlChanges[ctlpt.BoundVar.Name] = ctlpt.BoundVar;
                                e.Handled = true;
                            }
                        }
                    }
                    else if(e.Step is StepControllerChange)
                    {
                        // Control change
                        StepControllerChange scc = e.Step as StepControllerChange;

                        // Process through our list of inputs of interest.
                        foreach (NControlPoint ctlpt in DynamicElements.InputControls)
                        {
                            if (ctlpt.ControllerId == scc.ControllerId && ctlpt.Track.Channel == scc.Channel)
                            {
                                // Add to our list for processing at the next tock.
                                ctlpt.BoundVar.Value = scc.Value;
                                _ctrlChanges[ctlpt.BoundVar.Name] = ctlpt.BoundVar;
                                e.Handled = true;
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Process midi log event.
        /// </summary>
        void Midi_LogEvent(object sender, ProtocolLogEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                // Route all midi events through log.
                string s = $"Midi{e.Category} {_stepTime} {e.Message}";
                _logger.Info(s);
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
                bool _anySolo = DynamicElements.Tracks.Where(t => t.State == TrackState.Solo).Count() > 0;

                if (_anySolo)
                {
                    // Kill any not solo.
                    DynamicElements.Tracks.ForEach(t => { if (t.State != TrackState.Solo) _device1.Kill(t.Channel); });
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
            _ctrlChanges.Add(e.BoundVar.Name, e.BoundVar);
        }

        /// <summary>
        /// Package up the runtime stuff the script may need. Call this before any script updates.
        /// </summary>
        void InitRuntime()
        {
            RuntimeContext.Playing = chkPlay.Checked;
            RuntimeContext.StepTime = _stepTime;
            RuntimeContext.RealTime = (float)(DateTime.Now - _startTime).TotalSeconds;
            RuntimeContext.Speed = (float)potSpeed.Value;
            RuntimeContext.Volume = sldVolume.Value;
            RuntimeContext.FrameRate = _frameRate;
            RuntimeContext.RuntimeSteps.Clear();
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (RuntimeContext.Speed != potSpeed.Value)
            {
                potSpeed.Value = RuntimeContext.Speed;
                SetSpeedTimerPeriod();
            }

            if (RuntimeContext.Volume != sldVolume.Value)
            {
                sldVolume.Value = RuntimeContext.Volume;
            }

            if (RuntimeContext.FrameRate != _frameRate)
            {
                _frameRate = RuntimeContext.FrameRate;
                SetUiTimerPeriod();
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="args"></param>
        void ProcessScriptRuntimeError(Surface.RuntimeErrorEventArgs args)
        {
            ProcessPlay(PlayCommand.Stop, false);
            SetCompileStatus(false);

            // Locate the offending frame.
            string srcFile = Utils.UNKNOWN_STRING;
            int srcLine = -1; //XXX
            StackTrace st = new StackTrace(args.Exception, true);
            StackFrame sf = null;

            for (int i = 0; i < st.FrameCount;i++)
            {
                StackFrame stf = st.GetFrame(i);
                if(stf.GetFileName() != null && stf.GetFileName().ToUpper().Contains(_compileTempDir.ToUpper()))
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

                ScriptError err = new ScriptError()
                {
                    ErrorType = ScriptError.ScriptErrorType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = args.Exception.Message
                };

                _logger.Error(err.ToString());
            }
            else // unknown?
            {
                ScriptError err = new ScriptError()
                {
                    ErrorType = ScriptError.ScriptErrorType.Runtime,
                    SourceFile = "",
                    LineNumber = -1, //XXX
                    Message = args.Exception.Message
                };

                _logger.Error(err.ToString());
            }
        }
        #endregion

        #region File handling
        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string fn = sender.ToString();
            OpenFile(fn);
        }

        /// <summary>
        /// Allows the user to select a neb file from file system.
        /// </summary>
        void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "Nebulator files (*.neb)|*.neb",
                Title = "Select a Nebulator file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// Common neb file opener.
        /// </summary>
        /// <param name="fn">The neb file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        public string OpenFile(string fn)
        {
            string ret = "";

            using (new WaitCursor())
            {
                try
                {
                    if(File.Exists(fn))
                    {
                        _logger.Info($"Reading neb file: {fn}");
                        _nebpVals = Bag.Load(fn.Replace(".neb", ".nebp"));
                        _fn = fn;

                        // This may be coming from the web service...
                        if(InvokeRequired)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                // Running on the UI thread
                                SetCompileStatus(true);
                                AddToRecentDefs(fn);
                                Text = $"Nebulator {Utils.GetVersionString()} - {fn}";

                                Compile();
                            });
                        }
                    }
                    else
                    {
                        ret = $"Invalid file: {fn}";
                    }
                }
                catch (Exception ex)
                {
                    ret = $"Couldn't open the neb file: {fn} because: {ex.Message}";
                    _logger.Error(ret);
                }

                SetCompileStatus(false);
                InitScriptUi();
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
        /// One or more neb files have changed so reload/compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                SetCompileStatus(false);
            });
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go or stop button.
        /// </summary>
        void Play_Click(object sender, EventArgs e)
        {
            bool ok = _needCompile ? Compile() : true;

            if(ok)
            {
                ProcessPlay(chkPlay.Checked ? PlayCommand.Start : PlayCommand.Stop, true);
            }
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
            ProcessPlay(PlayCommand.Rewind, true);
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
        void Compile_Click(object sender, EventArgs e)
        {
            Compile();
            ProcessPlay(PlayCommand.StopRewind, true);
        }

        /// <summary>
        /// User updated the time.
        /// </summary>
        void Time_ValueChanged(object sender, EventArgs e)
        {
            _stepTime = timeMaster.CurrentTime;
            ProcessPlay(PlayCommand.UpdateUiTime, true);
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
            LogClientNotificationTarget.ClientNotification += Log_ClientNotification;
        }

        /// <summary>
        /// A message from the logger to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        void Log_ClientNotification(string msg)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                string s = $"{msg}{Environment.NewLine}"; //.Replace(ScriptCore.SCRIPT_PRINT_PREFIX, "");
                infoDisplay.AddInfo(s);
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
        /// Save user settings that aren't automatic.
        /// </summary>
        void SaveSettings()
        {
            UserSettings.TheSettings.SurfaceFormInfo.FromForm(_surface);
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

            UserSettings.TheSettings.Save();
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e)
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
                ListSelector.Options.Add("MidiIn", _device1.ProtocolInputs);
                ListSelector.Options.Add("MidiOut", _device1.ProtocolOutputs);

                // Detect changes of interest.
                bool midi = false;
                bool ctrls = false;
                pg.PropertyValueChanged += (sdr, args) =>
                {
                    string p = args.ChangedItem.PropertyDescriptor.Name;
                    midi |= p.Contains("Midi");
                    ctrls |= (p.Contains("Font") | p.Contains("Color"));
                };

                f.Controls.Add(pg);
                f.ShowDialog();

                // Figure out what changed - each handled differently.
                if (midi)
                {
                    _device1.Init();
                }

                if (ctrls)
                {
                    MessageBox.Show("UI changes require a restart to take effect.");
                }

                // Always safe to update these.
                SetUiTimerPeriod();
                _surface.TopMost = UserSettings.TheSettings.LockUi;

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
        void Piano_PianoKeyEvent(object sender, Piano.PianoKeyEventArgs e)
        {
            if(e.Down)
            {
                StepNoteOn step = new StepNoteOn()
                {
                    Channel = 2,
                    NoteNumber = Utils.Constrain(e.NoteId, _device1.Caps.MinNote, _device1.Caps.MaxNote),
                    Velocity = 90,
                    VelocityToPlay = 90,
                    Duration = new Time(0)
                };
                _device1.Send(step);
            }
            else
            {
                StepNoteOff step = new StepNoteOff()
                {
                    Channel = 2,
                    NoteNumber = Utils.Constrain(e.NoteId, _device1.Caps.MinNote, _device1.Caps.MaxNote),
                    Velocity = 64
                };
                _device1.Send(step);
            }
        }

        /// <summary>
        /// Turn piano on/off.
        /// </summary>
        void Piano_Click(object sender, EventArgs e)
        {
            _piano.Visible = pianoToolStripMenuItem.Checked;
        }
        #endregion

        #region Play control
        /// <summary>
        /// Update everything per param.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="userAction">Something the user did.</param>
        /// <returns>Indication of success.</returns>
        bool ProcessPlay(PlayCommand cmd, bool userAction)
        {
            bool ret = true;

            switch (cmd)
            {
                case PlayCommand.Start:
                    if(userAction)
                    {
                        bool ok = _needCompile ? Compile() : true;
                        if (ok)
                        {
                            _startTime = DateTime.Now;
                            SetSpeedTimerPeriod();
                        }
                        else
                        {
                            chkPlay.Checked = false;
                            ret = false;
                        }
                    }
                    else
                    {
                        if (_needCompile)
                        {
                            ret = false; // not yet
                        }
                        else
                        {
                            chkPlay.Checked = true;
                            _startTime = DateTime.Now;
                            SetSpeedTimerPeriod();
                        }
                    }
                    break;

                case PlayCommand.Stop:
                    if (!userAction)
                    {
                        chkPlay.Checked = false;
                    }
                    
                    // Send midi stop all notes just in case.
                    _device1.Kill();
                    break;

                case PlayCommand.Rewind:
                    _stepTime.Reset();
                    break;

                case PlayCommand.StopRewind:
                    if (!userAction)
                    {
                        chkPlay.Checked = false;
                    }

                    _stepTime.Reset();
                    break;

                case PlayCommand.UpdateUiTime:
                    // See below.
                    break;
            }

            // Always do this.
            timeMaster.CurrentTime = _stepTime;

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
                ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start, true);
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
            double framesPerMsec = (double)_frameRate / 1000;
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
        /// Colorize by theme.
        /// </summary>
        void InitControls()
        {
            BackColor = UserSettings.TheSettings.BackColor;

            btnRewind.Image = Utils.ColorizeBitmap(btnRewind.Image, UserSettings.TheSettings.IconColor);

            btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, UserSettings.TheSettings.IconColor);

            chkPlay.Image = Utils.ColorizeBitmap(chkPlay.Image, UserSettings.TheSettings.IconColor);
            chkPlay.BackColor = UserSettings.TheSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            chkSeq.BackColor = UserSettings.TheSettings.BackColor;
            chkSeq.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

            chkUi.BackColor = UserSettings.TheSettings.BackColor;
            chkUi.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_Resize(object sender, EventArgs e)
        {
            if (_script != null)
            {
                InitRuntime();
                _surface.InitSurface(_script);
                ProcessRuntime();
            }
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
            DynamicElements.Tracks.ForEach(t => tracks.Add(t.Channel, t.Name));

            // Convert speed/bpm to sec per tick.
            double ticksPerMinute = potSpeed.Value; // bpm
            double ticksPerSec = ticksPerMinute / 60;
            double secPerTick = 1 / ticksPerSec;

            Midi.MidiUtils.ExportMidi(_compiledSteps, fn, tracks, secPerTick, "Converted from " + _fn);
        }

        /// <summary>
        /// Import a style file as neb file lines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ImportStyle_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "Style files (*.sty)|*.sty",
                Title = "Import from style file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                var v = Midi.MidiUtils.ImportStyle(openDlg.FileName);
                Clipboard.SetText(string.Join(Environment.NewLine, v));
                MessageBox.Show("Style file content is in the clipboard");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Kill_Click(object sender, EventArgs e)
        {
            _device1.Kill();
        }
        #endregion
    }
}
