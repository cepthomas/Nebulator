using System;
using System.Collections.Generic;
using System.Linq;
using NBagOfTricks.Slog;
using Nebulator.Common;


// The internal script stuff.

namespace Nebulator.Script
{
    public partial class ScriptBase
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        internal readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>The steps being executed.</summary>
        internal StepCollection _scriptSteps = new();

        /// <summary>Script functions may add sequences at runtime.</summary>
        internal StepCollection _transientSteps = new();

        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel> _channelMap = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Set up runtime stuff. Good time to send initial patches.
        /// </summary>
        /// <param name="channels"></param>
        public void Init(List<Channel> channels)
        {
            _channelMap.Clear();
            channels.ForEach(ch =>
            {
                _channelMap[ch.ChannelName] = ch;
                SendPatch(ch.ChannelName, ch.Patch);
            });
        }
        #endregion

        #region Client functions
        /// <summary>
        /// Convert script sequences etc to internal steps.
        /// </summary>
        public void BuildSteps()
        {
            // Build all the steps.
            int sectionBeat = 0;

            foreach (Section section in _sections)
            {
                foreach (SectionElement sectel in section.Elements)
                {
                    if (sectel.Sequences.Length > 0)
                    {
                        // Current index in the sequences list.
                        int seqIndex = 0;

                        // Current beat in the section.
                        int beatInSect = 0;

                        while (beatInSect < section.Beats)
                        {
                            var seq = sectel.Sequences[seqIndex];
                            AddSequence(sectel.Channel, seq, sectionBeat + beatInSect);

                            beatInSect += seq.Beats;
                            if (seqIndex < sectel.Sequences.Length - 1)
                            {
                                seqIndex++;
                            }
                        }
                    }
                }

                // Update accumulated time.
                sectionBeat += section.Beats;
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
        /// <param name="time">Specific time.</param>
        /// <returns>Enumerator for steps at time.</returns>
        public IEnumerable<Step> GetSteps(Time time)
        {
            if(time.Beat == 0 && time.Subdiv == 0)
            {
                // Starting/looping. Clean up transient.
                _transientSteps.Clear();
            }

            // Check both collections.
            var steps = _scriptSteps.GetSteps(time).Concat(_transientSteps.GetSteps(time));
            return steps;
        }

        /// <summary>
        /// All the script steps.
        /// </summary>
        /// <returns></returns>
        public StepCollection GetAllSteps()
        {
            return _scriptSteps;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start at.</param>
        StepCollection ConvertToSteps(string chanName, Sequence seq, int startBeat)
        {
            if (seq is null)
            {
                throw new Exception($"Invalid Sequence");
            }

            StepCollection steps = new();

            var channel = GetChannel(chanName);
            if(channel is not null)
            {
                foreach (SequenceElement seqel in seq.Elements)
                {
                    // Create the note start and stop times.
                    int toffset = 0;
                    //int toffset = startBeat == -1 ? 0 : channel.NextTime();

                    Time startNoteTime = new Time(startBeat, toffset) + seqel.When;
                    Time stopNoteTime = startNoteTime + (seqel.Duration.TotalSubdivs == 0 ? new Time(0.1) : seqel.Duration); // 0.1 is a short hit

                    // Is it a function?
                    if (seqel.ScriptFunction is not null)
                    {
                        StepFunction step = new()
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
                            StepNoteOn step = new()
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
            }

            return steps;
        }

        /// <summary>Send a named script sequence at some beat/time.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        /// <param name="beat">Which beat to send the sequence.</param>
        void AddSequence(string chanName, Sequence seq, int beat)
        {
            if (seq is null)
            {
                throw new Exception($"Invalid Sequence");
            }

            StepCollection scoll = ConvertToSteps(chanName, seq, beat);
            _scriptSteps.Add(scoll);
        }

        /// <summary>
        /// Utility to look up channel.
        /// </summary>
        /// <param name="chanName"></param>
        /// <returns></returns>
        Channel GetChannel(string chanName)
        {
            if (!_channelMap.TryGetValue(chanName, out var channel))
            {
                throw new Exception($"Invalid Channel Name: {chanName}");
            }

            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid device for channel: {chanName}");
            }

            return channel;
        }
        #endregion
    }
}
