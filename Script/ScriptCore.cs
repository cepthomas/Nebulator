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
        public virtual void ReceiveNote(string dev, int channel, int note, int vel) { }

        /// <summary>Called when input arrives.</summary>
        public virtual void ReceiveController(string dev, int channel, int controller, int value) { }
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
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, int notenum, double vol, MusicTime dur)
        {
            var ch = MidiManager.Instance.GetOutputChannel(chanName) ?? throw new ArgumentException($"Invalid channel: {chanName}");
            notenum = MathUtils.Constrain(Math.Abs(notenum), 0, MidiDefs.MAX_MIDI);

            // If vol is positive it's note on else note off.
            if (vol > 0)
            {
                vol *= MasterVolume;
                int velPlay = (int)(vol * MidiDefs.MAX_MIDI);
                velPlay = MathUtils.Constrain(velPlay, 0, MidiDefs.MAX_MIDI);
                NoteOn non = new(ch.ChannelNumber, notenum, velPlay, StepTime);
                ch.Send(non);

                // Add a transient note off for later.
                NoteOff noff = new(ch.ChannelNumber, notenum, StepTime + dur) {  Transient = true };
                ch.Events.Add(noff);
            }
            else // note off
            {
                NoteOff noff = new(ch.ChannelNumber, notenum, StepTime);
                ch.Send(noff);
            }
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation.</param>
        protected void SendNote(string chanName, string notestr, double vol, MusicTime dur)
        {
            SequenceElement note = new(notestr);
            note.Notes.ForEach(n => SendNote(chanName, n, vol, dur));
        }

        /// <summary>Send a note immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in MusicTime representation. Default is for things like drum hits.</param>
        protected void SendNote(string chanName, int notenum, double vol, double dur = 0.1)
        {
            SendNote(chanName, notenum, vol, new MusicTime(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in MusicTime representation. Default is for things like drum hits.</param>
        protected void SendNote(string chanName, string notestr, double vol, double dur = 0.1)
        {
            SendNote(chanName, notestr, vol, new MusicTime(dur));
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
            var ch = MidiManager.Instance.GetOutputChannel(chanName) ?? throw new ArgumentException($"Invalid channel: {chanName}");
            int ctlid = MidiDefs.Controllers.GetId(controller);
            if (ctlid < 0) throw new ArgumentException($"Invalid controller: {controller}");

            Controller ctlr = new(ch.ChannelNumber, ctlid, val, StepTime);
            ch.Send(ctlr);
        }

        /// <summary>
        /// Creates a midi input channel.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        protected void OpenInputChannel(string device, int channelNumber, string channelName)
        {
            MidiManager.Instance.OpenInputChannel(device, channelNumber, channelName);
        }

        /// <summary>
        /// Creates a midi output channel.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patch"></param>
        /// <exception cref="ArgumentException"></exception>
        protected void OpenOutputChannel(string device, int channelNumber, string channelName, string patch)
        {
            var chan = MidiManager.Instance.OpenOutputChannel(device, channelNumber, channelName, patch);
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
        /// Synchronously outputs the next sequence midi events.
        /// <param name="sounding">Which channel to send.</param>
        /// </summary>
        public void DoNextStep(HashSet<int> sounding)
        {
            foreach (var ch in MidiManager.Instance.OutputChannels)
            {
                if (sounding.Contains(ch.ChannelNumber))
                {
                    foreach (var mevt in ch.Events.Get(StepTime))
                    {
                        if (mevt is NoteOn evt)
                        {
                            // Adjust volume.
                            evt.Velocity = MathUtils.Constrain((int)(evt.Velocity * ch.Volume), 0, MidiDefs.MAX_MIDI);
                            ch.Send(evt);
                        }
                        else
                        {
                            // Everything else as is.
                            ch.Send(mevt);
                        }
                    }

                    // Clean up.
                    ch.Events.RemoveTransients(StepTime);
                }
            }
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
                            //was AddSequence(sectel.Channel, seq, sectionBeat + beatInSect);

                            var ch = MidiManager.Instance.GetOutputChannel(sectel.ChannelName) ?? throw new ArgumentException($"Invalid channel: {sectel.ChannelName}");
                            int beat = sectionBeat + beatInSect;
                            var ecoll = ConvertToEvents(ch, seq, beat);
                            ch.Events.AddRange(ecoll);

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
        List<BaseEvent> ConvertToEvents(OutputChannel channel, Sequence seq, int startBeat)
        {
            List<BaseEvent> events = [];

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                MusicTime startNoteTime = new MusicTime(startBeat * MusicTime.TicksPerBeat) + seqel.When;
                MusicTime stopNoteTime = startNoteTime + (seqel.Duration.Tick == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    Function evt = new(channel.ChannelNumber, seqel.ScriptFunction, startNoteTime);
                    //events.Add(new(evt, channel.ChannelName)); ??
                    events.Add(evt);
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = channel.Volume * MasterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, 0, MidiDefs.MAX_MIDI);

                        NoteOn non = new(channel.ChannelNumber, noteNum, velPlay, startNoteTime);
                        events.Add(non);

                        // Add note off.
                        NoteOff noff = new(channel.ChannelNumber, noteNum, stopNoteTime);
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
        public Dictionary<int, string> GetSectionInfo()
        {
            Dictionary<int, string> info = [];
            int when = 0;

            foreach (Section sect in _sections)
            {
                info.Add(when, sect.Name);
                when += sect.Beats;
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

    public class Utils
    {
        /// <summary>
        /// Gets note number for music or drum names. TODO1 put in MidiLibEx or? Resist urge to put in MusicLib...
        /// </summary>
        /// <param name="snotes"></param>
        /// <returns></returns>
        public static List<int> ParseNotes(string snotes)
        {
            List<int> notes = MusicDefs.GetNotesFromString(snotes);
            if (!notes.Any())
            {
                // It might be a drum.
                int idrum = MidiDefs.Drums.GetId(snotes);
                if (idrum >= 0)
                {
                    notes.Add(idrum);
                }
                else
                {
                    // Not a drum either - error!
                    throw new InvalidOperationException($"Invalid notes [{snotes}]");
                }
            }

            return notes;
        }
    }
}
