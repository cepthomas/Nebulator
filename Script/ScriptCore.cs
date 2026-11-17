using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.MidiLib;
using Ephemera.NBagOfTricks;
using Ephemera.MusicLib;


namespace Nebulator.Script
{
    public partial class ScriptCore
    {
        #region Fields
        /// <summary>My logger.</summary>
        internal readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>All sections. Static so it is common to all script classes derived from ScriptCore.</summary>
        internal static List<Section> _sections = [];

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        #region Kind of klunky hook TODO
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        /// <summary>Midi boss.</summary>
        public static ChannelManager CM;
#pragma warning restore CS8618
        #endregion

        #region API - Properties shared by host and script
        /// <summary>Sound is playing. Main:W Script:R</summary>
        public bool Playing { get; set; } = false;

        /// <summary>Current Nebulator step time. Main:W Script:R</summary>
        public MusicTime StepTime { get; set; } = new MusicTime(0);

        /// <summary>Actual time since start pressed. Main:W Script:R</summary>
        public double RealTime { get; set; } = 0.0;

        /// <summary>Nebulator Speed in bpm. Main:RW Script:RW</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Nebulator master Volume. Main:RW Script:RW</summary>
        public double MasterVolume { get; set; } = 0;
        #endregion

        #region API - Script implemented functions called by host
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called every tick.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void ReceiveNote(string dev, int midiChannel, int note, int vel) { }

        /// <summary>Called when input arrives.</summary>
        public virtual void ReceiveController(string dev, int midiChannel, int controller, int value) { }
        #endregion

        #region API - Functions callable by script
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
                if (el.Name is null) { throw new InvalidOperationException($"Invalid Channel at index {elements.IndexOf(el)}"); }
            }

            Section nsect = new()
            {
                Beats = beats,
                Name = name,
                Elements = elements
            };
            
            _sections.Add(nsect);
            _logger.Info($"_sections:{_sections.Count}");
            return nsect;
        }

        /// <summary>
        /// Add a named chord or scale definition.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">Like "1 4 6 b13"</param>
        protected void CreateNotes(string name, string parts)
        {
            MusicDefs.AddCompound(name, parts);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="snotes"></param>
        /// <returns></returns>
        public List<int> ParseNotes(string snotes)
        {
            return Utils.ParseNotes(snotes);
        }

        /// <summary>
        /// Send a note immediately. Lowest level sender.
        /// </summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(MidiOutput mout, int notenum, double vol, MusicTime dur)
        {
            notenum = MathUtils.Constrain(Math.Abs(notenum), 0, MidiDefs.MAX_MIDI);

            // If vol is positive it's note on else note off.
            if (vol > 0)
            {
                vol *= MasterVolume;
                int velPlay = (int)(vol * MidiDefs.MAX_MIDI);
                velPlay = MathUtils.Constrain(velPlay, 0, MidiDefs.MAX_MIDI);
                NoteOn non = new(mout.MidiChannel, notenum, velPlay, StepTime);
                mout.Send(non);

                // Add a transient note off for later.
                NoteOff noff = new(mout.MidiChannel, notenum, StepTime + dur) {  Transient = true };
                mout.Events.Add(noff);
            }
            else // note off
            {
                NoteOff noff = new(mout.MidiChannel, notenum, StepTime);
                mout.Send(noff);
            }
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation.</param>
        protected void SendNote(MidiOutput mout, string notestr, double vol, MusicTime dur)
        {
            SequenceElement note = new(notestr);
            note.Notes.ForEach(n => SendNote(mout, n, vol, dur));
        }

        /// <summary>Send a note immediately.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in MusicTime representation. Default is for things like drum hits.</param>
        protected void SendNote(MidiOutput mout, int notenum, double vol, double dur = 0.1)
        {
            SendNote(mout, notenum, vol, new MusicTime(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in MusicTime representation. Default is for things like drum hits.</param>
        protected void SendNote(MidiOutput mout, string notestr, double vol, double dur = 0.1)
        {
            SendNote(mout, notestr, vol, new MusicTime(dur));
        }

        /// <summary>Send an explicit note on immediately. Caller is responsible for sending note off later.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume.</param>
        protected void SendNoteOn(MidiOutput mout, int notenum, double vol)
        {
            SendNote(mout, notenum, vol);
        }

        /// <summary>Send an explicit note off immediately.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="notenum">Note number.</param>
        protected void SendNoteOff(MidiOutput mout, int notenum)
        {
            SendNote(mout, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="mout">Which output to send it on.</param>
        /// <param name="ctrlId">Controller.</param>
        /// <param name="val">Controller value.</param>
        protected void SendController(MidiOutput mout, Enum ctrlId, int val)
        {
            Controller ctlr = new(mout.MidiChannel, ctrlId, val, StepTime);
            mout.Send(ctlr);
        }

        /// <summary>
        /// Creates a midi input.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        protected void OpenMidiInput(string device, int midiChannel, string name)
        {
            CM.OpenMidiInput(device, midiChannel, name);
        }

        /// <summary>
        /// Creates a midi output channel.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <exception cref="ArgumentException"></exception>
        protected MidiOutput OpenMidiOutput(string device, int midiChannel, string name, Enum patch)
        {
            var mout = CM.OpenMidiOutput(device, midiChannel, name);
            CM.SetPatch(mout, patch);
            return mout;
        }

        /// <summary>
        /// Standard print.
        /// </summary>
        /// <param name="vars"></param>
        protected void Print(params object[] vars)
        {
            _logger.Info(string.Join(", ", vars));
        }
        #endregion

        #region Host functions for internal use
        /// <summary>
        /// Clean up.
        /// </summary>
        public void Reset()
        {
            _sections.Clear();
        }

        /// <summary>
        /// Convert script sequences to internal events.
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

                            var mouts = CM.GetOutputs().Where(ch => ch.mout.Name == sectel.Name);
                            if (mouts.Any())
                            {
                                var mout = mouts.First().mout;
                                int beat = sectionBeat + beatInSect;
                                var ecoll = ConvertToEvents(mout.MidiChannel, seq, beat);
                                mout.Events.AddRange(ecoll);
                            }

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
        /// <param name="chanNum"></param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start sequence at.</param>
        List<BaseEvent> ConvertToEvents(int chanNum, Sequence seq, int startBeat)
        {
            List<BaseEvent> events = [];

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                MusicTime startNoteTime = new MusicTime(startBeat * MusicTime.SubbeatsPerBeat) + seqel.When;
                MusicTime stopNoteTime = startNoteTime + (seqel.Duration.Tick == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    Function evt = new(chanNum, seqel.ScriptFunction, startNoteTime);
                    events.Add(evt);
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = seqel.Volume; // TODO maybe also ChannelControl.Volume * MasterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, 0, MidiDefs.MAX_MIDI);

                        NoteOn non = new(chanNum, noteNum, velPlay, startNoteTime);
                        events.Add(non);

                        // Add note off.
                        NoteOff noff = new(chanNum, noteNum, stopNoteTime);
                        events.Add(noff);
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Get all section names and when they start in beats. The end marker is also added.
        /// Will be empty if free-running.
        /// </summary>
        /// <returns></returns>
        public Dictionary<MusicTime, string> GetSectionInfo()
        {
            Dictionary<MusicTime, string> info = [];
            MusicTime when = new();
            foreach (Section sect in _sections)
            {
                info.Add(when, sect.Name);
                when += new MusicTime(sect.Beats * MusicTime.SubbeatsPerBeat);
            }

            // Add the dummy end marker.
            if (info.Any())
            {
                info.Add(when, "END");
            }

            return info;
        }
        #endregion
    }
}
