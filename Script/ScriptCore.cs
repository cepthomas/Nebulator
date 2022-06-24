using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using NBagOfTricks.Slog;
using MidiLib;
using Nebulator.Common;
using NBagOfTricks;


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

        /// <summary>The events being executed.</summary>
        internal PatternInfo _scriptEvents = new();

        /// <summary>Script functions may add sequences at runtime.</summary>
        internal PatternInfo _transientEvents = new();

        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel> _channels = new();

        /// <summary>Script randomizer.</summary>
        static readonly Random _rand = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Set up runtime stuff.
        /// </summary>
        /// <param name="channels">All output channels.</param>
        public void Init(List<Channel> channels)
        {
            _channels.Clear();
            channels.ForEach(ch =>
            {
                _channels[ch.ChannelName] = ch;
                // Good time to send initial patches.
                SendPatch(ch.ChannelName, ch.Patch);
            });
        }
        #endregion

        #region Client functions
        /// <summary>
        /// Convert script sequences etc to internal events.
        /// </summary>
        public void BuildSteps()
        {
            // Build all the events.
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
        /// Get events at specific time.
        /// </summary>
        /// <param name="time">Specific time.</param>
        /// <returns>Enumerator for events at time.</returns>
        public IEnumerable<MidiEventDesc> GetEvents(Time time)
        {
            if(time.Beat == 0 && time.Subdiv == 0)
            {
                // Starting/looping. Clean up transient.
                _transientEvents = new();
            }

            // Check both collections.
            var events = _scriptEvents.GetEvents(time.TotalSubdivs).Concat(_transientEvents.GetEvents(time.TotalSubdivs));
            return events;
        }

        ///// <summary>
        ///// All the script events.
        ///// </summary>
        ///// <returns></returns>
        //public StepCollection GetAllSteps()
        //{
        //    return _scriptSteps;
        //}
        #endregion

        #region Utilities
        /// <summary>
        /// Generate events from sequence notes.
        /// </summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start at.</param>
        List<MidiEventDesc> ConvertToEvents(string chanName, Sequence seq, int startBeat)
        {
            if (seq is null)
            {
                throw new ArgumentException($"Invalid Sequence");
            }

            List<MidiEventDesc> events = new();

            var channel = GetChannel(chanName);
            if (channel is not null)
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
                        FunctionMidiEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, seqel.ScriptFunction);
                        events.Add(new(evt));
                    }
                    else // plain ordinary
                    {
                        // Process all note numbers.
                        foreach (int noteNum in seqel.Notes)
                        {
                            ///// Note on.
                            double vel = channel.NextVol(seqel.Volume) * MasterVolume;
                            int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                            velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                            NoteOnEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubdivs);
                            events.Add(new(evt));
                        }
                    }
                }
            }

            return events;
        }

        /// <summary>Send a named script sequence at some beat/time.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        /// <param name="beat">Which beat to send the sequence.</param>
        void AddSequence(string chanName, Sequence seq, int beat)
        {
            if (seq is null)
            {
                throw new ArgumentException($"Invalid Sequence");
            }

            var ecoll = ConvertToEvents(chanName, seq, beat);
            _scriptEvents.Events.AddRange(ecoll);
        }

        /// <summary>
        /// Utility to look up channel.
        /// </summary>
        /// <param name="chanName"></param>
        /// <returns></returns>
        Channel GetChannel(string chanName)
        {
            if (!_channels.TryGetValue(chanName, out var ch))
            {
                throw new ArgumentException($"Invalid Channel Name: {chanName}");
            }

            if (ch is null || ch.Tag is null)
            {
                throw new ArgumentException($"Invalid device for channel: {chanName}");
            }

            return ch;
        }
        #endregion
    }
}
