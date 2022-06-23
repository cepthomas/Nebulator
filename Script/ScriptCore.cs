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
    // TODOX home for these classes...
    public class Channel_XXX
    {
        /// <summary>The associated midi channel object.</summary>
        public Channel Channel { get; set; }

        ///// <summary>The device used by this channel.  Used to find and bind the device at runtime.</summary>
        public string DeviceName { get; set; }

        /// <summary>The associated device object.</summary>
        public IMidiOutputDevice? Device { get; set; } = null;
    }




    /// <summary>Custom type to support runtime functions.</summary>
    public class FunctionMidiEvent : MidiEvent
    {
        /// <summary>
        /// Single constructor.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="channel"></param>
        /// <param name="scriptFunc"></param>
        public FunctionMidiEvent(int time, int channel, Action scriptFunc) : base(time, channel, MidiCommandCode.MetaEvent)
        {
            ScriptFunction = scriptFunc;
        }

        /// <summary>The function to call.</summary>
        public Action ScriptFunction { get; init; }

        //MidiEvent(long absoluteTime, int channel, MidiCommandCode commandCode);

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"FunctionMidiEvent: {base.ToString()} function:{ScriptFunction}";
        }
    }




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
        //internal StepCollection _scriptSteps = new();
        internal PatternInfo _scriptEvents = new();

        /// <summary>Script functions may add sequences at runtime.</summary>
        //internal StepCollection _transientSteps = new();
        internal PatternInfo _transientEvents = new();

        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel_XXX> _channelMap_XXX = new(); //TODOX redo this
        #endregion

        #region Lifecycle
        /// <summary>
        /// Set up runtime stuff. Good time to send initial patches.
        /// </summary>
        /// <param name="channels">All output channels.</param>
        public void Init(List<Channel_XXX> channels)
        {
            _channelMap_XXX.Clear();
            channels.ForEach(ch =>
            {
                _channelMap_XXX[ch.Channel.ChannelName] = ch;
                SendPatch(ch.Channel.ChannelName, ch.Channel.Patch);
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
                        FunctionMidiEvent evt = new(startNoteTime.TotalSubdivs, channel.Channel.ChannelNumber, seqel.ScriptFunction);
                        events.Add(new(evt));
                    }
                    else // plain ordinary
                    {
                        // Process all note numbers.
                        foreach (int noteNum in seqel.Notes)
                        {
                            ///// Note on.
                            double vel = channel.Channel.NextVol(seqel.Volume) * MasterVolume;
                            int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                            velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                            NoteOnEvent evt = new(startNoteTime.TotalSubdivs, channel.Channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubdivs);
                            events.Add(new(evt));

                            //// Maybe add a deferred stop note. TODOX old/unused
                            //if (stopNoteTime != startNoteTime)
                            //{
                            //    events.AddStep(stopNoteTime, new StepNoteOff(event));
                            //}
                            //// else client is taking care of it.
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
                throw new Exception($"Invalid Sequence");
            }

            var ecoll = ConvertToEvents(chanName, seq, beat);
            _scriptEvents.Events.AddRange(ecoll);
        }

        /// <summary>
        /// Utility to look up channel.
        /// </summary>
        /// <param name="chanName"></param>
        /// <returns></returns>
        Channel_XXX GetChannel(string chanName)
        {
            if (!_channelMap_XXX.TryGetValue(chanName, out var channel))
            {
                throw new Exception($"Invalid Channel Name: {chanName}");
            }

            if (channel is null)// || channel.Device is null)
            {
                throw new Exception($"Invalid device for channel: {chanName}");
            }

            return channel;
        }
        #endregion
    }
}
