using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using MidiLib;
using NAudio.Midi;


// Nebulator script API stuff.

namespace Nebulator.Script
{
    public partial class ScriptBase
    {
        #region Properties that can be referenced in the user script
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

        #region Script functions that can be overridden
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called every mmtimer increment.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives. TODOX named dev like outputs.</summary>
        public virtual void InputNote(string dev, int channel, int note) { }

        /// <summary>Called when input arrives. TODOX named dev/controller like outputs.</summary>
        public virtual void InputControl(string dev, int channel, int controller, int value) { }
        #endregion

        #region Script callable functions - composition
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
        public void CreateNotes(string name, string parts)
        {
            MusicDefinitions.AddChordScale(name, parts);
        }
        #endregion

        #region Script callable functions - send immediately
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

            // If vol is positive and the note is not negative, it's note on, else note off.
            if (vol > 0)
            {
                double vel = ch.NextVol(vol) * MasterVolume;
                int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                NoteOnEvent evt = new(StepTime.TotalSubdivs, ch.ChannelNumber, absnote, velPlay, dur.TotalSubdivs);
                ch.SendEvent(evt);
            }
            else
            {
                NoteEvent evt = new(StepTime.TotalSubdivs, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
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

        #region General utilities
        protected double Random(double max)
        {
            return _rand.NextDouble() * max;
        }

        protected double Random(double min, double max)
        {
            return min + _rand.NextDouble() * (max - min);
        }

        protected int Random(int max)
        {
            return _rand.Next(max);
        }

        protected int Random(int min, int max)
        {
            return _rand.Next(min, max);
        }

        protected void Print(params object[] vars)
        {
            _logger.Info(string.Join(", ", vars));
        }
        #endregion
    }
}
