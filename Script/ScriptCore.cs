using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using NLog;
using Nebulator.Common;
using Nebulator.Device;


// The internal script stuff.

namespace Nebulator.Script
{
    public partial class NebScript : IDisposable
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;

        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();
        #endregion

        #region Properties - dynamic things shared between host and script at runtime
        /// <summary>Steps added by script functions at runtime e.g. sendSequence(). Script -> Main</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();
        #endregion

        #region Properties - things defined in the script that MainForm needs
        /// <summary>All vars.</summary>
        public List<NVariable> Variables { get; set; } = new List<NVariable>();

        /// <summary>Control inputs.</summary>
        public List<NController> Controllers { get; set; } = new List<NController>();

        /// <summary>Levers.</summary>
        public List<NController> Levers { get; set; } = new List<NController>();

        /// <summary>All displays.</summary>
        public List<NDisplay> Displays { get; set; } = new List<NDisplay>();

        /// <summary>All channels.</summary>
        public List<NChannel> Channels { get; set; } = new List<NChannel>();

        /// <summary>All sequences.</summary>
        public List<NSequence> Sequences { get; set; } = new List<NSequence>();
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

        #region Utilities
        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startTick">Which tick toB start at.</param>
        public static StepCollection ConvertToSteps(NChannel channel, NSequence seq, int startTick)
        {
            StepCollection steps = new StepCollection();

            foreach (NSequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = startTick == -1 ? 0 : channel.NextTime();

                Time startNoteTime = new Time(startTick, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + seqel.Duration;

                // Is it a function?
                if (seqel.ScriptFunction != null)
                {
                    StepInternal step = new StepInternal()
                    {
                        Device = channel.Device,
                        ChannelNumber = channel.ChannelNumber,
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
                        double vel = channel.NextVol(seqel.Volume);
                        StepNoteOn step = new StepNoteOn()
                        {
                            Device = channel.Device,
                            ChannelNumber = channel.ChannelNumber,
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
    }
}
