using System;
using System.Collections.Generic;
using NLog;
using Nebulator.Common;


// The internal script stuff. 

namespace Nebulator.Script
{
    public partial class NebScript : IDisposable
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        internal readonly Logger _logger = LogManager.GetLogger("NebScript");

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;

        /// <summary>Script randomizer.</summary>
        internal Random _rand = new Random();

        /// <summary>All sequences.</summary>
        internal List<Sequence> _sequences = new List<Sequence>();

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new List<Section>();

        /// <summary>The steps being executed. Script functions may add to it at runtime.</summary>
        internal StepCollection _steps = new StepCollection();
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

        #region Client functions
        /// <summary>
        /// Convert script sequences etc to internal steps.
        /// </summary>
        public void BuildSteps()
        {
            // Build all the steps.
            int sectionTime = 0;

            foreach (Section section in _sections)
            {
                foreach ((Channel ch, Sequence seq, int beat) v in section)
                {
                    AddSequence(v.ch, v.seq, sectionTime + v.beat);
                }

                // Update accumulated time.
                sectionTime += section.Beats;
            }
        }

        /// <summary>
        /// Get all section names and when they start. The end marker is also added.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetSectionMarkers()
        {
            Dictionary<int, string> info = new();
            int when = 0;

            foreach (Section sect in _sections)
            {
                info.Add(when, sect.Name);
                when += sect.Beats;
            }

            // Add the dummy end marker.
            info.Add(when, "");

            return info;
        }

        /// <summary>
        /// Get steps at specific time.
        /// </summary>
        /// <param name="time">Specific time or null if all.</param>
        /// <returns></returns>
        public IEnumerable<Step> GetSteps(Time time)
        {
            return _steps.GetSteps(time);
        }

        /// <summary>
        /// The whole enchilada.
        /// </summary>
        /// <returns></returns>
        public StepCollection GetAllSteps()
        {
            return _steps;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start at.</param>
        StepCollection ConvertToSteps(Channel channel, Sequence seq, int startBeat)
        {
            StepCollection steps = new StepCollection();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = 0;
                //int toffset = startBeat == -1 ? 0 : channel.NextTime();

                Time startNoteTime = new Time(startBeat, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + (seqel.Duration.TotalSubdivs == 0 ? new Time(0.1) : seqel.Duration); // 0.1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction != null)
                {
                    StepFunction step = new StepFunction()
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
        void AddSequence(Channel channel, Sequence seq, int beat)
        {
            if (channel is null)
            {
                throw new Exception($"Invalid Channel");
            }

            if (seq is null)
            {
                throw new Exception($"Invalid Sequence");
            }

            StepCollection scoll = ConvertToSteps(channel, seq, beat);
            _steps.Add(scoll);
        }
        #endregion
    }
}
