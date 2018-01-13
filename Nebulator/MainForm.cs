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
using Nebulator.Scripting;
using Nebulator.UI;
using Nebulator.FastTimer;
using Nebulator.Midi;

/*

TODO1 all this stuff:

Turn off diagnostic tools in debug options?

Suppress errors in project properties >> build


------------------------ nameof --------------------------------------
 (IsNullOrWhiteSpace(lastName))
    throw new ArgumentException(message: "Cannot be blank", paramName: nameof(lastName));

    

------------------------ Expression-bodied function members --------------------------------------
The body of a lot of members that we write consist of only one statement that can be represented as an expression. You can reduce that syntax by writing an expression-bodied member instead. It works for methods and read-only properties." For example, an override of ToString() is often a great candidate:
public override string ToString() => $"{LastName}, {FirstName}";
You can also use expression-bodied members in read only properties as well:
public string FullName => $"{FirstName} {LastName}";

--------------------------------------
My kbd: Pad1=44, pad8=51, knob1=ctlr1, knob8=ctlr8



--------------------------------------
drones:
 - Since you have Ableton, try this: Take any sound, drench it in reverb and print it to an audio file. Now take that
   audio file, use the warp and change the tempo to a number of your choosing (let's say half the tempo) and drench
   that in reverb. Rinse and repeat. Do this enough times and you'll get some ridiculous, stretched out pads that sound
   like they're hanging out next to a black hole.
 - The key to ambient pads and drones is movement, which can be accomplished in various ways.
   A wavetable sweep is a tried and trusted classic. For that you need a wavetable synthesizer, obviously. I don't think
   the KingKorg does that but there should be free softsynths out there that does wavetables. If you can use two different
   wavetables for two oscillators it will have a better effect. Basically, just set up some LFO's in random mode with a
   slow rate modulating the wavetables, and run that through chorus, delay and reverb.
   You can do a similar thing with a subtractive synth if it has waveshaping options (Most synths have square wave PWM,
   some have wavefolding, some Oberheims and Oberheim clones allow you to sweep between the waveforms).
 - reverb is key, i'd say filtered saw waves, but whatever works. I tend to use the Octatrack for that. noise, reverb,
   filter, resampling, som other stuff, resampling, repitching ... I have fun using the OT for that, so I do it this way
 - On the first bus, first put in a compressor with low ratio and treshold (optional), followed by a reverb with 6 seconds
   or more of reverb time. On the second bus, first put in a delay with a relatively short delay time (optional, can ofc
   be adjusted to taste) followed by a pitch shifter set to an octave above.
   Then send your synth into the first bus, and send the first bus into the second bus, and then send the second bus back
   into the first bus at a lower level to get your feedback. Done.
 - Two other ways: Mangle VST (or any granular synthesis VST) with any sample loaded and drenched in FX. Another is 
   through extreme time stretching. Load up a piano sample and stretch it out like 400-4000% percent. Will yield some 
   wild results.
 - +1 on granular. Listen to some Solar Fields for ideas. He is a drone master.
 - multiple detuned oscillators help.



--------------------------------------
Mouse events occur in the following order:
MouseEnter
MouseMove
MouseHover / MouseDown / MouseWheel
MouseUp
MouseLeave


--------------------------------------
You may want to look into the EventLoopScheduler It's way more memory efficient since it only maintains a single thread, 
and execution is guaranteed to be in order. However if you have a task that hangs or runs long you'll end up with 
a lot of traffic.
Or if you want you can implement your own FIFO task scheduler. It'd be a good exercise. It would look a lot like the 
EventLoopScheduler. You have one thread that runs until you tell it to stop and a queue of tasks. If there's no 
currently running task the scheduler polls the queue and takes off the first task and executes it. If you add tasks 
you queue them at the end of the queue. If the queue is empty the scheduler just hangs around waiting for tasks to 
be queued. It would run off the main thread so won't block your application.
https://social.msdn.microsoft.com/Forums/en-US/4aeda2a4-74cb-43ba-ac94-ce82c26671f5/best-way-to-preschedule-events-with-rx?forum=rx
http://stackoverflow.com/questions/37038442/using-custom-scheduler-for-audio-stream-time


If you're programming against .NET 4.5 or later, you can make your life much easier by converting your code that 
explicitly uses SynchronizationContext, ThreadPool.QueueUserWorkItem, control.BeginInvoke, etc. over to the 
new async / await keywords and the Task Parallel Library (TPL), i.e. the API surrounding the Task and Task<TResult> classes. 
These will, to a very high degree, take care of capturing the UI thread's synchronization context, starting an 
asynchronous operation, then getting back onto the UI thread so you can process the operation's result.


https://msdn.microsoft.com/en-us/library/dd537609.aspx

Parallel.Invoke(() => DoSomeWork(), () => DoSomeOtherWork());

Thread.CurrentThread.Name = "Main";

// Create a task and supply a user delegate by using a lambda expression. 
Task taskA = new Task( () => Console.WriteLine("Hello from taskA."));
// Start the task.
taskA.Start();

// Output a message from the calling thread.
Console.WriteLine("Hello from thread '{0}'.", Thread.CurrentThread.Name);
taskA.Wait();


--------------------------------------
https://at.or.at/hans/solitude/

Solitude.png

In the score, time flows from left to right. Each color represents a sample. Each sample controller has 
two arrays: the brighter, bigger one on top controls sample playback; and a smaller darker one at the 
bottom controls amp and pan. The lowest point of the sample array is the beginning of the sample, the 
highest is the end, and the height of the array is how much and what part of the sample to play starting 
at that point in time. There are between 50 and 100 voice polyphony for the samples. The height of the 
amp/pan array is the amp, and the y location is the pan.

*/



namespace Nebulator
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Fast timer.</summary>
        IFastTimer _nebTimer = null;

        /// <summary>Piano child form.</summary>
        Piano _piano = new Piano();

        /// <summary>The current script.</summary>
        Script _script = null;

        /// <summary>The compiled midi event sequence.</summary>
        StepCollection _compiledSteps = new StepCollection();

        /// <summary>Accumulated control input var changes to be processed at next step.</summary>
        LazyCollection<Variable> _ctrlChanges = new LazyCollection<Variable>() { AllowOverwrite = true };

        /// <summary>Diagnostics for nebulator clock timing measurement.</summary>
        TimingAnalyzer _tanNeb = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Current neb file name.</summary>
        string _fn = Globals.UNKNOWN_STRING;

        /// <summary>Detect changed composition files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally, will require a recompile.</summary>
        bool _dirtyFiles = false;

        /// <summary>The temp dir for tracking down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current neb file.</summary>
        Bag _nebpVals = new Bag();

        /// <summary>UI update rate.</summary>
        int _fps = 50;

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
            _nebTimer = new NebTimer();
            SetSpeedTimerPeriod();
            SetUiTimerPeriod();
            _nebTimer.TimerElapsedEvent += FastTimer_TimerElapsedEvent;
            _nebTimer.Start();
            #endregion

            #region Piano
            pianoToolStripMenuItem.Checked = Globals.UserSettings.PianoFormInfo.Visible;
            _piano.Visible = Globals.UserSettings.PianoFormInfo.Visible;
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

            #region ////////////////////// debug stuff ///////////////////////

            Editor.ScriptEditor sed = new Editor.ScriptEditor();
            sed.Show();
            sed.TestGrid();
            Visible = false;

            //OpenFile(@"C:\Dev\Nebulator\Examples\airport.neb"); // airport  dev  example  lsys

            //ExportMidi("test.mid");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\OneDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
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
                Globals.MidiInterface.KillAll();

                if(_script != null)
                {
                    // Save the project.
                    _nebpVals.Clear();
                    _nebpVals.SetValue("master", "volume", sldVolume.Value);
                    _nebpVals.SetValue("master", "speed", potSpeed.Value);
                    _nebpVals.SetValue("master", "loop", chkLoop.Checked);
                    _nebpVals.SetValue("master", "sequence", chkSequence.Checked);

                    _script.Dynamic.Tracks.Values.ForEach(c => _nebpVals.SetValue(c.Name, "volume", c.Volume));
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

            if (_fn == Globals.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                Compiler compiler = new Compiler();

                // Save internal vals now as they will be reloaded during compile.
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
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Globals.UserSettings.IconColor);
                    _dirtyFiles = false;

                    _compileTempDir = compiler.TempDir;

                    _script.Dynamic.Sections.Values.ForEach(s => timeMaster.TimeDefs.Add(new Time(s.Start, 0), s.Name));

                    _compiledSteps = StepUtils.ConvertToSteps(_script.Dynamic);

                    _script.ScriptEvent += Script_ScriptEvent;

                    InitUi();
                }
                else
                {
                    _logger.Warn("Compile failed.");
                    ok = false;
                    SetPlayStatus(PlayCommand.StopRewind);
                    compiler.Errors.ForEach(e => _logger.Warn(e.ToString()));
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Globals.ATTENTION_COLOR);
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

            foreach (Track t in _script.Dynamic.Tracks.Values)
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
            levers.Init(_script.Dynamic.Levers.Values);

            // Surface area.
            foreach (Control c in splitContainerInput.Panel2.Controls)
            {
                c.Dispose();
            }
            splitContainerInput.Panel2.Controls.Clear();

            UserControl surface = _script.CreateSurface();
            surface.Dock = DockStyle.Fill;
            splitContainerInput.Panel2.Controls.Add(surface);
        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void FastTimer_TimerElapsedEvent(object sender, FastTimerEventArgs e)
        {
            if (Globals.UserSettings.TimerStats && e.ElapsedTimers.Contains("NEB"))
            {
                // Do some stats gathering for measuring jitter.
                TimingAnalyzer.Stats stats = _tanNeb.Grab();
                if (stats != null)
                {
                    _logger.Info($"Midi timiing: {stats}");
                }
            }

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
        void NextStep(FastTimerEventArgs e)
        {
            try
            {
                ////// Process changed vars //////
                foreach (Variable var in _ctrlChanges.Values)
                {
                    // Execute any script handlers.
                    _script.ExecScriptFunction(var.Name);

                    // Output any midictlout controllers.
                    IEnumerable<MidiControlPoint> ctlpts = _script.Dynamic.OutputMidis.Values.Where(c => c.RefVar.Name == var.Name);

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

                ////// Neb steps /////
                if (Globals.Playing && e.ElapsedTimers.Contains("NEB"))
                {
                    if(_script != null)
                    {
                        // Do any script execute stuff. This is done now as the script may manipulate things.
                        _script.step();

                        // Do runtime steps.
                        _script.RuntimeSteps.GetSteps(Globals.StepTime).ForEach(s => PlayStep(s));
                        _script.RuntimeSteps.DeleteSteps(Globals.StepTime);
                    }

                    // Do the compiled steps.
                    if(chkSequence.Checked)
                    {
                        _compiledSteps.GetSteps(Globals.StepTime).ForEach(s => PlayStep(s));
                    }

                    // Local common function
                    void PlayStep(Step step)
                    {
                        Track track = _script.Dynamic.Tracks[step.TrackName];

                        // Is it ok to play now?
                        bool _anySolo = _script.Dynamic.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;
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
                                Globals.MidiInterface.Send(step);
                            }
                        }
                    }

                    ///// Bump time.
                    Globals.StepTime.Advance();

                    ////// Check for end of play.
                    // If no steps or not selected, free running mode so always keep going.
                    if(_compiledSteps.Times.Count() != 0 && chkSequence.Checked)
                    {
                        // Check for end and loop condition.
                        if (Globals.StepTime.Tick >= _compiledSteps.MaxTick)
                        {
                            if (chkLoop.Checked) // keep going
                            {
                                SetPlayStatus(PlayCommand.Rewind);
                            }
                            else // stop now
                            {
                                SetPlayStatus(PlayCommand.StopRewind);
                                Globals.MidiInterface.KillAll(); // just in case
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
                    _script.UpdateSurface();
                }

                // In case there are lingering noteoffs that need to be processed.
                Globals.MidiInterface.Housekeep();
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
                        IEnumerable<MidiControlPoint> ctlpts = _script.Dynamic.InputMidis.Values.Where((c, m) => (
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
                infoDisplay.AddMidiMessage(e.Message);
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
                bool _anySolo = _script.Dynamic.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;

                if (_anySolo)
                {
                    // Kill any not solo.
                    _script.Dynamic.Tracks.Values.ForEach(t => { if (t.State != TrackState.Solo) Globals.MidiInterface.Kill(t.Channel); });
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

            string srcFile = Globals.UNKNOWN_STRING;
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
                    ErrorType = ScriptErrorType.Runtime,
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
                    ErrorType = ScriptErrorType.Runtime,
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
                    btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Globals.ATTENTION_COLOR);
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
                btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Globals.ATTENTION_COLOR);
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
            Globals.StepTime = timeMaster.CurrentTime;
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
        /// A message from the script to display to the user.
        /// Request for information.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Script_ScriptEvent(object sender, Script.ScriptEventArgs e)
        {
            if(e.Message != null)
            {
                BeginInvoke((MethodInvoker)delegate ()
                {
                    infoDisplay.AddInfo(e.Message);
                });
            }

            if (e.Volume != null)
            {
                sldVolume.Value = (int)e.Volume;
            }

            if (e.Speed != null)
            {
                potSpeed.Value = (int)e.Speed;
                SetSpeedTimerPeriod();
            }

            if (e.FrameRate != null)
            {
                _fps = (int)e.FrameRate;
                SetUiTimerPeriod();
            }

            // Return all current.
            e.Volume = sldVolume.Value;
            e.Speed = potSpeed.Value;
            e.FrameRate = _fps;
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
                    SelectedObject = Globals.UserSettings
                };

                // Detect changes of interest.
                List<string> propsChanged = new List<string>();
                pg.PropertyValueChanged += (sdr, args) => { propsChanged.Add(args.ChangedItem.PropertyDescriptor.Name); };

                f.Controls.Add(pg);
                f.ShowDialog();

                if(propsChanged.Count > 0)
                {
                    bool midi = false;
                    bool notes = false;
                    bool ctrls = false;

                    propsChanged.ForEach(p =>
                    {
                        midi |= p.Contains("Midi");
                        notes |= (p.Contains("Notes") | p.Contains("Scales"));
                        ctrls |= (p.Contains("Font") | p.Contains("Color"));
                    });

                    if (midi)
                    {
                        Globals.MidiInterface.Init();
                    }

                    if (notes)
                    {
                        NoteUtils.Init();
                    }

                    if (ctrls)
                    {
                        MessageBox.Show("UI changes require a restart to take effect."); // TODO2 Update controls without restarting.
                    }

                    SaveSettings();
                }
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
                Globals.MidiInterface.Send(step);
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
                Globals.MidiInterface.Send(step);
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
            else if (Globals.Playing)
            {
                ///// Stop!
                SetPlayStatus(PlayCommand.Stop);

                // Send midi stop all notes just in case.
                Globals.MidiInterface.KillAll();
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
                    Globals.Playing = true;
                    break;

                case PlayCommand.Stop:
                    chkPlay.Checked = false;
                    Globals.Playing = false;
                    break;

                case PlayCommand.Rewind:
                    Globals.StepTime.Reset();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    Globals.Playing = false;
                    Globals.StepTime.Reset();
                    break;

                case PlayCommand.UpdateUiTime:
                    break;
            }

            timeMaster.CurrentTime = Globals.StepTime;
        }
        #endregion

        #region Keyboard handling
        // How windows handles key presses, i.e Shift+A, you'll get:
        // - KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
        // - KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
        // - KeyPress: KeyChar='A'
        // - KeyUp: KeyCode=Keys.A
        // - KeyUp: KeyCode=Keys.ShiftKey
        // Also note that Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.

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
            settingsToolStripMenuItem.Enabled = !Globals.Playing;
        }

        /// <summary>
        /// Common func.
        /// </summary>
        void SetSpeedTimerPeriod()
        {
            // Convert speed/bpm to msec per tock.
            double ticksPerMinute = potSpeed.Value; // sec/tick, bpm
            double tocksPerMinute = ticksPerMinute * Globals.TOCKS_PER_TICK;
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
            double framesPerMsec = (double)_fps / 1000;
            double msecPerFrame = 1 / framesPerMsec;
            _nebTimer.SetTimer("UI", (int)msecPerFrame);
        }

        /// <summary>
        /// Colorize by theme.
        /// </summary>
        void InitControls()
        {
            BackColor = Globals.UserSettings.BackColor;

            // Stash the original image in the tag field.
            btnRewind.Image = Utils.ColorizeBitmap(btnRewind.Image, Globals.UserSettings.IconColor);

            btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Globals.UserSettings.IconColor);

            chkLoop.Image = Utils.ColorizeBitmap(chkLoop.Image, Globals.UserSettings.IconColor);
            chkLoop.BackColor = Globals.UserSettings.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;

            chkPlay.Image = Utils.ColorizeBitmap(chkPlay.Image, Globals.UserSettings.IconColor);
            chkPlay.BackColor = Globals.UserSettings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;

            chkSequence.Image = Utils.ColorizeBitmap(chkSequence.Image, Globals.UserSettings.IconColor);
            chkSequence.BackColor = Globals.UserSettings.BackColor;
            chkSequence.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;

            potSpeed.ControlColor = Globals.UserSettings.IconColor;
            potSpeed.Font = Globals.UserSettings.ControlFont;
            potSpeed.Invalidate();

            sldVolume.ControlColor = Globals.UserSettings.ControlColor;
            sldVolume.Font = Globals.UserSettings.ControlFont;
            sldVolume.Invalidate();

            timeMaster.ControlColor = Globals.UserSettings.ControlColor;
            timeMaster.Invalidate();

            infoDisplay.BackColor = Globals.UserSettings.BackColor;
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
            _script.Dynamic.Tracks.Values.ForEach(t => tracks.Add(t.Channel, t.Name));

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
            Globals.MidiInterface.KillAll();
        }
        #endregion
    }
}
