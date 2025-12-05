using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Ephemera.MidiLib;
using Ephemera.NBagOfTricks;


namespace Nebulator.Script
{
    public partial class ScriptCore
    {
        #region Fields
        /// <summary>My logger.</summary>
        internal readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>All sections.</summary>
        internal List<Section> _sections = [];

        /// <summary>All the events defined in the script.</summary>
        internal List<MidiEventDesc> _scriptEvents = [];

        /// <summary>All the channels - key is user assigned name.</summary>
        Dictionary<string, Channel> _channels = [];

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        #region Properties - accessible by host and script
        /// <summary>Sound is playing. Main:W Script:R</summary>
        public bool Playing { get; set; } = false;

        /// <summary>Current Nebulator step time. Main:W Script:R</summary>
        public BarTime StepTime { get; set; } = new BarTime(0);

        /// <summary>Actual time since start pressed. Main:W Script:R</summary>
        public double RealTime { get; set; } = 0.0;

        /// <summary>Nebulator Speed in bpm. Main:RW Script:RW</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Nebulator master Volume. Main:RW Script:RW</summary>
        public double MasterVolume { get; set; } = 0;
        #endregion

        #region Public functions to override - called by host
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called every mmtimer increment.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputNote(string dev, int channel, int note, int vel) { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputControl(string dev, int channel, int controller, int value) { }
        #endregion

        #region Internal functions - called by script
        /// <summary>
        /// Standard print.
        /// </summary>
        /// <param name="vars"></param>
        protected void Print(params object[] vars)
        {
            _logger.Info(string.Join(", ", vars));
        }

        /// <summary>
        /// Create a defined sequence and add to internal collection.
        /// </summary>
        /// <param name="beats">Length in beats.</param>
        /// <param name="elements">.</param>
        protected Sequence CreateSequence(int beats, SequenceElements elements)
        {
            Sequence nseq = new()
            {
                Beats = beats,
                Elements = elements
            };
            return nseq;
        }

        /// <summary>
        /// Create a defined section and add to internal collection.
        /// </summary>
        /// <param name="beats">How long in beats.</param>
        /// <param name="name">For UI display.</param>
        /// <param name="elements">Section info to add.</param>
        protected Section CreateSection(int beats, string name, SectionElements elements)
        {
            // Sanity check elements.
            foreach (var el in elements)
            {
                if (el.ChannelName is null)
                {
                    throw new InvalidOperationException($"Invalid Channel at index {elements.IndexOf(el)}");
                }
            }

            Section nsect = new()
            {
                Beats = beats,
                Name = name,
                Elements = elements
            };
            
            _sections.Add(nsect);
            return nsect;
        }

        /// <summary>
        /// Add a named chord or scale definition.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">Like "1 4 6 b13"</param>
        protected void CreateNotes(string name, string parts)
        {
            MusicDefinitions.AddChordScale(name, parts);
        }

        /// <summary>Send a note immediately. Lowest level sender.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, int notenum, double vol, BarTime dur)
        {
            if (!_channels.ContainsKey(chanName))
            {
                throw new ArgumentException($"Invalid channel: {chanName}");
            }

            var ch = _channels[chanName];
            int absnote = MathUtils.Constrain(Math.Abs(notenum), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            // If vol is positive it's note on else note off.
            if (vol > 0)
            {
                vol *= MasterVolume;
                int velPlay = (int)(vol * MidiDefs.MAX_MIDI);
                velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                NoteOnEvent evt = new(StepTime.TotalSubs, ch.ChannelNumber, absnote, velPlay, dur.TotalSubs);
                ch.SendEvent(evt);
            }
            else
            {
                NoteEvent evt = new(StepTime.TotalSubs, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
                ch.SendEvent(evt);
            }
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation.</param>
        protected void SendNote(string chanName, string notestr, double vol, BarTime dur)
        {
            SequenceElement note = new(notestr);
            note.Notes.ForEach(n => SendNote(chanName, n, vol, dur));
        }

        /// <summary>Send a note immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in BarTime representation. Default is for things like drum hits.</param>
        protected void SendNote(string chanName, int notenum, double vol, double dur = 0.1)
        {
            SendNote(chanName, notenum, vol, new BarTime(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in BarTime representation. Default is for things like drum hits.</param>
        protected void SendNote(string chanName, string notestr, double vol, double dur = 0.1)
        {
            SendNote(chanName, notestr, vol, new BarTime(dur));
        }

        /// <summary>Send an explicit note on immediately. Caller is responsible for sending note off later.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume.</param>
        protected void SendNoteOn(string chanName, int notenum, double vol)
        {
            SendNote(chanName, notenum, vol);
        }

        /// <summary>Send an explicit note off immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        protected void SendNoteOff(string chanName, int notenum)
        {
            SendNote(chanName, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="controller">Controller.</param>
        /// <param name="val">Controller value.</param>
        protected void SendController(string chanName, string controller, int val)
        {
            if (!_channels.ContainsKey(chanName))
            {
                throw new ArgumentException($"Invalid channel: {chanName}");
            }

            var ch = _channels[chanName];
            int ctlrid = MidiDefs.GetControllerNumber(controller);
            if (ctlrid >= 0)
            {
                ch.SendController((MidiController)ctlrid, val);
            }
            else
            {
                throw new ArgumentException($"Invalid controller: {controller}");
            }
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="chanName"></param>
        /// <param name="patch"></param>
        protected void SendPatch(string chanName, int patch)
        {
            if (!_channels.ContainsKey(chanName))
            {
                throw new ArgumentException($"Invalid channel: {chanName}");
            }

            var ch = _channels[chanName];
            ch.Patch = patch;
            ch.SendPatch();
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="chanName"></param>
        /// <param name="patch"></param>
        protected void SendPatch(string chanName, string patch)
        {
            if (!_channels.ContainsKey(chanName))
            {
                throw new ArgumentException($"Invalid channel: {chanName}");
            }

            int patchid = MidiDefs.GetInstrumentNumber(patch);
            if (patchid >= 0)
            {
                SendPatch(chanName, patchid);
            }
            else
            {
                throw new ArgumentException($"Invalid patch: {patch}");
            }
        }
        #endregion

        #region Host functions for internal use
        /// <summary>
        /// Set up runtime stuff.
        /// </summary>
        /// <param name="channels">All output channels.</param>
        public void Init(Dictionary<string, Channel> channels)
        {
            _channels = channels;
        }

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
                            //was AddSequence(sectel.Channel, seq, sectionBeat + beatInSect);
                            var ch = _channels[sectel.ChannelName];
                            int beat = sectionBeat + beatInSect;
                            var ecoll = ConvertToEvents(ch, seq, beat);
                            _scriptEvents.AddRange(ecoll);

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
        /// Generate events from sequence notes.
        /// </summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start sequence at.</param>
        List<MidiEventDesc> ConvertToEvents(Channel channel, Sequence seq, int startBeat)
        {
            List<MidiEventDesc> events = [];

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                BarTime startNoteTime = new BarTime(startBeat * MidiSettings.LibSettings.SubsPerBeat) + seqel.When;
                BarTime stopNoteTime = startNoteTime + (seqel.Duration.TotalSubs == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    FunctionMidiEvent evt = new(startNoteTime.TotalSubs, channel.ChannelNumber, seqel.ScriptFunction);
                    events.Add(new(evt, channel.ChannelName));
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = channel.Volume * MasterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                        NoteOnEvent evt = new(startNoteTime.TotalSubs, channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubs);
                        events.Add(new(evt, channel.ChannelName));
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Get all section names and when they start. The end marker is also added.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetSectionMarkers()
        {
            Dictionary<int, string> info = [];
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
    }
}
