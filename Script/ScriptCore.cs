using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
//using NLog;
using Nebulator.Common;
using Nebulator.Device;


// The internal script stuff.

namespace Nebulator.Script
{
    public partial class NebScript : IDisposable
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        readonly Logger _logger = new Logger("Script");

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;

        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();
        #endregion

        #region Properties
        /// <summary>Short duration for drum hits.</summary>
        public static double DrumDur { get; } = 0.1;
        #endregion

        #region Elements defined in the script that MainForm needs
        ///// <summary>All vars.</summary>
        //public List<NVariable> Variables { get; set; } = new List<NVariable>();

        ///// <summary>Control inputs.</summary>
        //public List<NController> Controllers { get; set; } = new List<NController>();

        // /// <summary>Levers.</summary>
        // public List<NController> Levers { get; set; } = new List<NController>();

        // /// <summary>All displays.</summary>
        // public List<NDisplay> Displays { get; set; } = new List<NDisplay>();

        ///// <summary>All channels.</summary>
        //public List<NChannel> Channels { get; set; } = new List<NChannel>();

        /// <summary>All sequences.</summary>
        public List<NSequence> Sequences { get; set; } = new List<NSequence>();

        /// <summary>All sections.</summary>
        public List<NSection> Sections { get; set; } = new List<NSection>();

        /// <summary>The steps being executed. Script functions may add to it at runtime using e.g. SendSequence(). Script -> Main</summary>
        public StepCollection Steps { get; private set; } = new StepCollection();
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
        /// <param name="startBeat">Which beat to start at.</param>
        public static StepCollection ConvertToSteps(Channel channel, NSequence seq, int startBeat)
        {
            StepCollection steps = new StepCollection();

            foreach (NSequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = 0;
                //int toffset = startBeat == -1 ? 0 : channel.NextTime();

                Time startNoteTime = new Time(startBeat, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + (seqel.Duration.TotalSubdivs == 0 ? new Time(DrumDur) : seqel.Duration);

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

        /// <summary>Send a named sequence at some future time.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        /// <param name="beat">When to send the sequence.</param>
        public void AddSequence(Channel channel, NSequence seq, int beat)
        {
            if (channel is null)
            {
                throw new Exception($"Invalid NChannel");
            }

            if (seq is null)
            {
                throw new Exception($"Invalid NSequence");
            }

            StepCollection scoll = ConvertToSteps(channel, seq, beat);
            Steps.Add(scoll);
        }
        #endregion
    }
}
