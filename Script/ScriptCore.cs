using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using NBagOfTricks.Slog;
using MidiLib;
using NBagOfTricks;


// The internal script stuff.

namespace Nebulator.Script
{
    public partial class ScriptBase
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        internal readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>All the defined script events.</summary>
        internal List<MidiEventDesc> _scriptEvents = new();

        /// <summary>Script functions may add sequences at runtime. TODO0 need to handle these</summary>
        internal List<MidiEventDesc> _dynamicEvents = new();

        /// <summary>All the channels - key is user assigned name.</summary>
        Dictionary<string, Channel> _channels = new();

        /// <summary>Script randomizer.</summary>
        static readonly Random _rand = new();

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Set up runtime stuff.
        /// </summary>
        /// <param name="channels">All output channels.</param>
        public void Init(Dictionary<string, Channel> channels)
        {
            _channels = channels;
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
        /// Get all events.
        /// </summary>
        /// <returns>Enumerator for all events.</returns>
        public IEnumerable<MidiEventDesc> GetEvents()
        {
            return _scriptEvents;
        }
        #endregion

        #region Private utilities
        /// <summary>
        /// Generate events from sequence notes.
        /// </summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start sequence at.</param>
        List<MidiEventDesc> ConvertToEvents(string chanName, Sequence seq, int startBeat)
        {
            if (seq is null)
            {
                throw new ArgumentException($"Invalid sequence");
            }

            List<MidiEventDesc> events = new();

            var channel = _channels[chanName];

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                //int toffset = 0;
                // int toffset = startBeat == -1 ? 0 : channel.NextTime();

                BarTime startNoteTime = new BarTime(startBeat * MidiSettings.LibSettings.SubdivsPerBeat) + seqel.When;
                BarTime stopNoteTime = startNoteTime + (seqel.Duration.TotalSubdivs == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    FunctionMidiEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, seqel.ScriptFunction);
                    events.Add(new(evt, chanName));
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

                        NoteOnEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, noteNum, velPlay, 0);// seqel.Duration.TotalSubdivs);
                        events.Add(new(evt, chanName));

                        // Add explicit note off. TODOX1 this adds events after the last real one. Kind of ugly looking but...
                        NoteEvent off = new((startNoteTime + seqel.Duration).TotalSubdivs, channel.ChannelNumber, MidiCommandCode.NoteOff, noteNum, 64);
                        events.Add(new(off, chanName));
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
                throw new ArgumentException($"Invalid sequence");
            }

            var ecoll = ConvertToEvents(chanName, seq, beat);
            _scriptEvents.AddRange(ecoll);
        }
        #endregion
    }
}
