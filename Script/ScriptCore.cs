using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using NLog;
using Nebulator.Common;
using Nebulator.Protocol;


namespace Nebulator.Script
{
    public partial class ScriptCore : IDisposable
    {
        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties - general
        /// <summary>Protocol to use.</summary>
        public IProtocol Protocol { get; set; } = null;
        #endregion

        #region Properties - shared between host and script at runtime
        /// <summary>Main -> Script</summary>
        public Time StepTime { get; set; } = new Time();

        /// <summary>Main -> Script</summary>
        public bool Playing { get; set; } = false;

        /// <summary>Main -> Script</summary>
        public float RealTime { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public float Speed { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public int Volume { get; set; } = 0;

        /// <summary>Main -> Script -> Main</summary>
        public int FrameRate { get; set; } = 0;

        /// <summary>Steps added by script functions at runtime e.g. playSequence(). Script -> Main</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();
        #endregion

        #region Properties - things defined in the script that MainForm needs
        /// <summary>Control inputs.</summary>
        public List<NControlPoint> InputControls { get; set; } = new List<NControlPoint>();

        /// <summary>Control outputs.</summary>
        public List<NControlPoint> OutputControls { get; set; } = new List<NControlPoint>();

        /// <summary>Levers.</summary>
        public List<NControlPoint> Levers { get; set; } = new List<NControlPoint>();

        /// <summary>Levers.</summary>
        public List<NVariable> Variables { get; set; } = new List<NVariable>();

        /// <summary>All sequences.</summary>
        public List<NSequence> Sequences { get; set; } = new List<NSequence>();

        /// <summary>All sections.</summary>
        public List<NSection> Sections { get; set; } = new List<NSection>();

        /// <summary>All tracks.</summary>
        public List<NTrack> Tracks { get; set; } = new List<NTrack>();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startTick">Which tick to start at.</param>
        public static StepCollection ConvertToSteps(NTrack track, NSequence seq, int startTick)
        {
            StepCollection steps = new StepCollection();

            foreach (NSequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = startTick == -1 ? 0 : track.NextTime();

                Time startNoteTime = new Time(startTick, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + seqel.Duration;

                // Is it a function?
                if (seqel.ScriptFunction != null)
                {
                    StepInternal step = new StepInternal()
                    {
                        Channel = track.Channel,
                        ScriptFunction = seqel.ScriptFunction
                    };
                    steps.AddStep(startNoteTime, step);
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        int vel = track.NextVol(seqel.Volume);
                        StepNoteOn step = new StepNoteOn()
                        {
                            Channel = track.Channel,
                            NoteNumber = noteNum,
                            Velocity = vel,
                            VelocityToPlay = vel,
                            Duration = seqel.Duration
                        };
                        steps.AddStep(startNoteTime, step);

                        //// Maybe add a deferred stop note.
                        //if (stopNoteTime != startNoteTime)
                        //{
                        //    steps.AddStep(stopNoteTime, new StepNoteOff(step));
                        //}
                        //// else client is taking care of it.
                    }
                }
            }

            return steps;
        }
        #endregion

        #region Private functions
        /// <summary>Handle unimplemented script elements that we can safely ignore. But do tell the user.</summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        void NotImpl(string name, string desc = "")
        {
            _logger.Warn($"{name} not implemented. {desc}");
        }

        /// <summary>Bounds check a color definition./// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        SKColor SafeColor(float r, float g, float b, float a)
        {
            r = constrain(r, 0, 255);
            g = constrain(g, 0, 255);
            b = constrain(b, 0, 255);
            a = constrain(a, 0, 255);
            return new SKColor((byte)r, (byte)g, (byte)b, (byte)a);
        }
        #endregion
    }
}
