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
using Nebulator.Comm;
using Nebulator.Server;
using NAudio.Midi;
using Nebulator.Midi;


// TODO Factor out the processing and non-processing stuff would be nice but much breakage.
//   - Remove Comm dependency in Script project
//   - Breaks the ScriptCore partial class model.
//   - Many tendrils involving ScriptDefinitions, NoteUtils, etc.



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
        MmTimerEx _timer = new MmTimerEx();

        /// <summary>Surface form.</summary>
        Surface _surface = new Surface();

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

        /// <summary>Current np file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;

        /// <summary>Detect changed composition files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for channeling down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current np/npp file.</summary>
        Bag _nppVals = new Bag();

        /// <summary>Diagnostics for timing measurement.</summary>
        TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };

        /// <summary>Server host.</summary>
        SelfHost _selfHost = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = Utils.GetAppDataDir();
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);
            NebSettings.Load(appDir);
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object sender, EventArgs e)
        {
            #region Init UI from settings
            Location = new Point(UserSettings.TheSettings.MainFormInfo.X, UserSettings.TheSettings.MainFormInfo.Y);
            Size = new Size(UserSettings.TheSettings.MainFormInfo.Width, UserSettings.TheSettings.MainFormInfo.Height);
            WindowState = FormWindowState.Normal;

            _surface.Visible = true;
            _surface.Location = new Point(Right, Top);
            _surface.TopMost = UserSettings.TheSettings.LockUi;
            #endregion

            InitLogging();

            PopulateRecentMenu();

            ScriptDefinitions.TheDefinitions.Init();

            // Fast timer.
            _timer = new MmTimerEx();
            SetSpeedTimerPeriod();
            SetUiTimerPeriod();
            _timer.TimerElapsedEvent += TimerElapsedEvent;
            _timer.Start();

            KeyPreview = true; // for routing kbd strokes properly

            InitControls();

            _watcher.FileChangeEvent += Watcher_Changed;

            levers.LeverChangeEvent += Levers_Changed;

            // Catches runtime errors during drawing.
            _surface.RuntimeErrorEvent += (object _, Surface.RuntimeErrorEventArgs eargs) => { ScriptRuntimeError(eargs); };

            // Init server.
            _selfHost = new SelfHost();
            SelfHost.RequestEvent += SelfHost_RequestEvent;
            Task.Run(() => { _selfHost.Run(); });

            #region Open file
            string sopen = "";
            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                sopen = OpenFile(args[1]);
            }
            #endregion

            #region System info
            Text = $"Nebulator {Utils.GetVersionString()} - No file loaded";

            string sins = "Inputs: \"Virtual Keyboard\"";
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                sins += $" \"{MidiIn.DeviceInfo(device).ProductName}\"";
            }
            _logger.Info(sins);

            string souts = "Outputs:";
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                souts += $" \"{MidiOut.DeviceInfo(device).ProductName}\"";
            }
            _logger.Info(souts);
            #endregion

#if _DEV // Debug stuff
            if (args.Count() <= 1)
            {
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\example.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\airport.np");
                sopen = OpenFile(@"C:\Dev\Nebulator\Examples\dev.np");


                //these in np:
                //sopen = OpenFile(@"C:\Dev\Nebulator\Dev\nptest.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\lsys.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\gol.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\flocking.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\generative1.np");
                //sopen = OpenFile(@"C:\Dev\Nebulator\Examples\generative2.np");



                //// Server debug stuff
                //TestClient client = new TestClient();
                //Task.Run(async () => { await client.Run(); });


                //ExportMidi("test.mid");

                //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\npulator\midi\styles-jazzy\Mambo.sty");
                //var v = MidiUtils.ImportStyle(@"C:\Users\cet\OneDrive\OneDrive Documents\npulator\midi\styles-jazzy\Funk.sty");
                //Clipboard.SetText(string.Join(Environment.NewLine, v));
            }
#endif
            if(sopen != "")
            {
                _logger.Error(sopen);
            }
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
                _script?.Outputs.ForEach(o => o?.Kill());

                if (_script != null)
                {
                    // Save the project.
                    _nppVals.Clear();
                    _nppVals.SetValue("master", "volume", sldVolume.Value);
                    _nppVals.SetValue("master", "speed", potSpeed.Value);
                    _script.Channels.ForEach(c => _nppVals.SetValue(c.Name, "volume", c.Volume));
                    _nppVals.Save();

                    _script.Dispose();
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
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;

                _selfHost?.Dispose();

                _script?.Outputs.ForEach(o => { o?.Stop(); o?.Dispose(); });
                _script?.Inputs.ForEach(i => { i?.Stop(); i?.Dispose(); });

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

                // Save internal npp file vals now as they will be reloaded during compile.
                _nppVals.Save();

                // Clean up any old.
                _script?.Dispose();

                // Compile now.
                _script = compiler.Execute(_fn);

                // Update file watcher - keeps an eye in any included files too.
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

                    // Hook up comms for runtime.
                    for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                    {
                        NInput input = new MidiInput() { CommName = MidiIn.DeviceInfo(device).ProductName };
                        input.CommInputEvent += Comm_InputEvent;
                        input.CommLogEvent += Comm_LogEvent;
                        _script.Inputs.Add(input);
                    }

                    // Add the virtual keyboard as an input.
                    {
                        VirtualKeyboard.VKeyboard vk = new VirtualKeyboard.VKeyboard();
                        vk.StartPosition = FormStartPosition.Manual;
                        vk.Size = new Size(NebSettings.TheSettings.VirtualKeyboardInfo.Width, NebSettings.TheSettings.VirtualKeyboardInfo.Height);
                        vk.Visible = NebSettings.TheSettings.VirtualKeyboardInfo.Visible;
                        vk.TopMost = false;
                        vk.Location = new Point(NebSettings.TheSettings.VirtualKeyboardInfo.X, NebSettings.TheSettings.VirtualKeyboardInfo.Y);
                        vk.CommInputEvent += Comm_InputEvent;
                        vk.CommLogEvent += Comm_LogEvent;
                        _script.Inputs.Add(vk);
                    }

                    for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                    {
                        NOutput output = new MidiOutput() { CommName = MidiOut.DeviceInfo(device).ProductName };
                        output.CommLogEvent += Comm_LogEvent;
                        _script.Outputs.Add(output);
                    }

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
                        ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
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
        /// Convert the script output into steps for feeding output devices.
        /// </summary>
        void ConvertToSteps()
        {
            // Convert compiled stuff to step collection.
            _compiledSteps.Clear();

            foreach (NSection sect in _script.Sections)
            {
                // Collect important times.
                timeMaster.TimeDefs.Add(new Time(sect.Start, 0), sect.Name);

                // Iterate through the sections channels.
                foreach (NSectionChannel schannel in sect.SectionChannels)
                {
                    // For processing current Sequence.
                    int seqOffset = sect.Start;

                    // Gen steps for each sequence.
                    foreach (NSequence seq in schannel.Sequences)
                    {
                        try
                        {
                            StepCollection stepsToAdd = ScriptUtils.ConvertToSteps(schannel.ParentChannel, seq, seqOffset);
                            _compiledSteps.Add(stepsToAdd);
                            seqOffset += seq.Length;

                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error in the sequences for NChannel {schannel.ParentChannel.Name} : {ex.Message}");
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

            ///// The channel controls.
            
            // Clean up current controls.
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

            if(_script != null)
            {
                foreach (NChannel t in _script.Channels)
                {
                    // Init from persistence.
                    int vt = Convert.ToInt32(_nppVals.GetValue(t.Name, "volume"));
                    t.Volume = vt == 0 ? 90 : vt; // in case it's new

                    ChannelControl tctl = new ChannelControl()
                    {
                        Location = new Point(x, timeMaster.Top),
                        BoundChannel = t
                    };
                    tctl.ChannelChangeEvent += ChannelChange_Event;
                    Controls.Add(tctl);
                    x += tctl.Width + CONTROL_SPACING;
                }

                // Levers.
                levers.Init(_script.Levers);
            }

            ///// Init other controls.
            potSpeed.Value = Convert.ToInt32(_nppVals.GetValue("master", "speed"));
            int mv = Convert.ToInt32(_nppVals.GetValue("master", "volume"));

            sldVolume.Value = mv == 0 ? 90 : mv; // in case it's new
            timeMaster.MaxTick = _compiledSteps.MaxTick;
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
                btnCompile.Image = Utils.ColorizeBitmap(btnCompile.Image, Color.Red);
                _needCompile = true;
            }

        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void TimerElapsedEvent(object sender, MmTimerEx.TimerEventArgs e)
        {
            //// Do some stats gathering for measuring jitter.
            //if ( _tan.Grab())
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
        void NextStep(MmTimerEx.TimerEventArgs e)
        {
            ////// Neb steps /////
            InitRuntime();

            if (chkPlay.Checked && e.ElapsedTimers.Contains("NEB") && !_needCompile)
            {
                //_tan.Arm();

                // Kick it.
                try
                {
                    _script.step();
                }
                catch (Exception ex)
                {
                    ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                }

                //if (_tan.Grab())
                //{
                //    _logger.Info("NEB tan: " + _tan.ToString());
                //}

                // Process any sequence steps the script added.
                _script.RuntimeSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));
                _script.RuntimeSteps.DeleteSteps(_stepTime);

                // Now do the compiled steps.
                _compiledSteps.GetSteps(_stepTime).ForEach(s => PlayStep(s));

                ///// Bump time.
                _stepTime.Advance();

                ////// Check for end of play.
                // If no steps or not selected, free running mode so always keep going.
                if(_compiledSteps.Times.Count() != 0)
                {
                    // Check for end and loop condition.
                    if (_stepTime.Tick >= _compiledSteps.MaxTick)
                    {
                        ProcessPlay(PlayCommand.StopRewind, false);
                        _script?.Outputs.ForEach(o => o?.Kill()); // just in case
                    }
                }
                // else keep going

                ProcessPlay(PlayCommand.UpdateUiTime, false);
            }

            ///// UI updates /////
            if (e.ElapsedTimers.Contains("UI") && chkPlay.Checked && !_needCompile)
            {
                //_tan.Arm();

                try
                {
                    _surface.UpdateSurface();
                }
                catch (Exception ex)
                {
                    ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                }

                //if (_tan.Grab())
                //{
                //    _logger.Info("UI tan: " + _tan.ToString());
                //}
            }

            // Process whatever the script did.
            ProcessRuntime();

            // Process any lingering noteoffs etc.
            _script?.Outputs.ForEach(o => o?.Housekeep());
            _script?.Inputs.ForEach(i => i?.Housekeep());

            ///// Local common function /////
            void PlayStep(Step step)
            {
                if(_script.Channels.Count > 0)
                {
                    NChannel channel = _script.Channels.Where(t => t.ChannelNumber == step.ChannelNumber).First();

                    // Is it ok to play now?
                    bool _anySolo = _script.Channels.Where(t => t.State == ChannelState.Solo).Count() > 0;
                    bool play = channel != null && (channel.State == ChannelState.Solo || (channel.State == ChannelState.Normal && !_anySolo));

                    if (play)
                    {
                        switch (step)
                        {

                            case StepInternal stin:

                                break;



                        }


                        if (step is StepInternal)
                        {
                            try
                            {
                                (step as StepInternal).ScriptFunction();
                            }
                            catch (Exception ex)
                            {
                                ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
                            }
                        }
                        else
                        {
                            if (step.Comm is NOutput)
                            {
                                // Maybe tweak values.
                                if (step is StepNoteOn)
                                {
                                    (step as StepNoteOn).Adjust(sldVolume.Value, channel.Volume);
                                }

                                (step.Comm as NOutput).Send(step);
                            }
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
        void Comm_InputEvent(object sender, CommInputEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                if (_script != null && e.Step != null)
                {
                    bool handled = false; // default

                    if (e.Step is StepNoteOn || e.Step is StepNoteOff)
                    {
                        int chanNum = (e.Step as Step).ChannelNumber;
                        // Dig out the note number. Note sign change for note off.
                        int value = (e.Step is StepNoteOn) ? (e.Step as StepNoteOn).NoteNumber : - (e.Step as StepNoteOff).NoteNumber;
                        handled = ProcessInput(sender as NInput, ScriptDefinitions.TheDefinitions.NoteControl, chanNum, value);
                    }
                    else if(e.Step is StepControllerChange)
                    {
                        // Control change
                        StepControllerChange scc = e.Step as StepControllerChange;
                        handled = ProcessInput(sender as NInput, scc.ControllerId, scc.ChannelNumber, scc.Value);
                    }

                    ///// Local common function /////
                    bool ProcessInput(NInput input, int ctrlId, int channelNum, int value)
                    {
                        bool ret = false;

                        // Run through our list of inputs of interest.
                        foreach (NController ctlpt in _script.InputControllers)
                        {
                            if (ctlpt.Input == input && ctlpt.ControllerId == ctrlId && ctlpt.ChannelNumber == channelNum)
                            {
                                // Assign new value which triggers script callback.
                                ctlpt.BoundVar.Value = value;
                                ret = true;
                            }
                        }

                        return ret;
                    }

                    if (!handled)
                    {
                        // Pass through. Not.... let the script handle it.
                        //e.Step.Comm.Send(e.Step);
                    }
                }
            });
        }

        /// <summary>
        /// Process midi log event.
        /// </summary>
        void Comm_LogEvent(object sender, CommLogEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                // Route all events through log.
                string s = $"{e.Category} {_stepTime} {e.Message}";
                _logger.Info(s);
            });
        }

        /// <summary>
        /// User has changed a channel value. Interested in solo/mute.
        /// </summary>
        void ChannelChange_Event(object sender, ChannelControl.ChannelChangeEventArgs e)
        {
            if (sender is ChannelControl)
            {
                // Check for solos.
                bool _anySolo = _script.Channels.Where(c => c.State == ChannelState.Solo).Count() > 0;

                if (_anySolo)
                {
                    // Kill any not solo.
                    _script.Channels.ForEach(c =>
                    {
                        if (c.State != ChannelState.Solo && c.Output != null)
                        {
                            c.Output.Kill(c.ChannelNumber);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// UI change event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Levers_Changed(object sender, Levers.LeverChangeEventArgs e)
        {

        }

        /// <summary>
        /// Package up the runtime stuff the script may need. Call this before any script updates.
        /// </summary>
        void InitRuntime()
        {
            _script.Playing = chkPlay.Checked;
            _script.StepTime = _stepTime;
            _script.RealTime = (float)(DateTime.Now - _startTime).TotalSeconds;
            _script.Speed = (float)potSpeed.Value;
            _script.Volume = sldVolume.Value;
            _script.FrameRate = _frameRate;
            _script.RuntimeSteps.Clear();
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

            if (_script.FrameRate != _frameRate)
            {
                _frameRate = _script.FrameRate;
                SetUiTimerPeriod();
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="args"></param>
        void ScriptRuntimeError(Surface.RuntimeErrorEventArgs args)
        {
            ProcessPlay(PlayCommand.Stop, false);
            SetCompileStatus(false);

            ScriptError err = ScriptUtils.ProcessScriptRuntimeError(args, _compileTempDir);

            if(err != null)
            {
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
                Filter = "Nebulator files (*.np)|*.np",
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
                    if(File.Exists(fn))
                    {
                        _logger.Info($"Opening {fn}");
                        _nppVals = Bag.Load(fn.Replace(".np", ".npp"));
                        _fn = fn;

                        // This may be coming from the web service...
                        if(InvokeRequired)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                // Running on the UI thread
                                SetCompileStatus(true);
                                AddToRecentDefs(fn);
                                Compile();
                            });
                        }

                        Text = $"Nebulator {Utils.GetVersionString()} - {fn}";
                    }
                    else
                    {
                        ret = $"Invalid file: {fn}";
                    }
                }
                catch (Exception ex)
                {
                    ret = $"Couldn't open the np file: {fn} because: {ex.Message}";
                    _logger.Error(ret);
                }
                finally
                {
                    SetCompileStatus(false);
                    InitScriptUi();
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
            ProcessPlay(chkPlay.Checked ? PlayCommand.Start : PlayCommand.Stop, true);
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
            string appDir = Utils.GetAppDataDir();

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
                string s = $"{msg}{Environment.NewLine}";
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
                tv.Colors.Add(" ERROR ", Color.Plum);
                tv.Colors.Add(" _WARN ", Color.LightPink);
                tv.Colors.Add(" _INFO ", Color.LightGreen);

                string appDir = Utils.GetAppDataDir();
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
            UserSettings.TheSettings.MainFormInfo.FromForm(this);
            UserSettings.TheSettings.Save();

            // Get the vkbd position.
            _script?.Inputs.ForEach(i => 
            {
                if(i is VirtualKeyboard.VKeyboard)
                {
                    NebSettings.TheSettings.VirtualKeyboardInfo.FromForm(i as VirtualKeyboard.VKeyboard);
                }
            });

            NebSettings.TheSettings.Save();
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
                PropertyGridEx pg = new PropertyGridEx()
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
                    ctrls |= (p.Contains("Font") | p.Contains("Color"));
                };

                f.Controls.Add(pg);
                f.ShowDialog();

                // Figure out what changed - each handled differently.
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
                            chkPlay.Checked = true;
                        }
                        else
                        {
                            chkPlay.Checked = false;
                            ret = false;
                        }
                    }
                    else // from the server
                    {
                        if (_needCompile)
                        {
                            ret = false; // not yet
                        }
                        else
                        {
                            _startTime = DateTime.Now;
                            SetSpeedTimerPeriod();
                            chkPlay.Checked = true;
                        }
                    }
                    break;

                case PlayCommand.Stop:
                    chkPlay.Checked = false;

                    // Send midi stop all notes just in case.
                    _script?.Outputs.ForEach(o => o?.Kill());
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
            _timer.SetTimer("NEB", (int)msecPerTock);
        }

        /// <summary>
        /// Common func.
        /// </summary>
        void SetUiTimerPeriod()
        {
            // Convert fps to msec per frame.
            double framesPerMsec = (double)_frameRate / 1000;
            double msecPerFrame = 1 / framesPerMsec;
            _timer.SetTimer("UI", (int)msecPerFrame);
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
        void ExportMidi_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                Filter = "Midi files (*.mid)|*.mid",
                Title = "Export to midi file",
                FileName = _fn.Replace(".np", ".mid")
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
            Dictionary<int, string> channels = new Dictionary<int, string>();
            _script.Channels.ForEach(t => channels.Add(t.ChannelNumber, t.Name));

            // Convert speed/bpm to sec per tick.
            double ticksPerMinute = potSpeed.Value; // bpm
            double ticksPerSec = ticksPerMinute / 60;
            double secPerTick = 1 / ticksPerSec;

            Midi.MidiUtils.ExportMidi(_compiledSteps, fn, channels, secPerTick, "Converted from " + _fn);
        }

        /// <summary>
        /// Import a style file as np file lines.
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
            _script?.Outputs.ForEach(o => o?.Kill());
        }
        #endregion
    }
}
