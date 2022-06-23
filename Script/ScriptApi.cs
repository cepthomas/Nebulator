﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using MidiLib;
using Nebulator.Common;
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
        public Time StepTime { get; set; } = new Time();

        /// <summary>Actual time since start pressed. Main:W Script:R</summary>
        public double RealTime { get; set; } = 0.0;

        /// <summary>Nebulator Speed in bpm. Main:RW Script:RW</summary>
        public double Speed { get; set; } = 0.0;

        /// <summary>Nebulator master Volume. Main:RW Script:RW</summary>
        public double MasterVolume { get; set; } = 0;
        #endregion

        #region Script functions that can be overridden
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called every mmtimer increment.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputNote(string dev, int channel, double note) { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputControl(string dev, int channel, int ctlid, double value) { } //string ctlid
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
                if (el.Channel is null)
                {
                    throw new Exception($"Invalid Channel at index {elements.IndexOf(el)}");
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
        #endregion

        #region Script callable functions - send immediately
        /// <summary>Send a note immediately. Lowest level sender.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, int notenum, double vol, Time dur)
        {
            var channel = GetChannel(chanName);

            int absnote = MathUtils.Constrain(Math.Abs(notenum), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            if (channel is not null)
            {
                // If vol is positive and the note is not negative, it's note on, else note off.
                if (vol > 0 && notenum > 0)
                {
                    double vel = channel.Channel.NextVol(vol) * MasterVolume;
                    int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                    velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                    NoteOnEvent evt = new(0, channel.Channel.ChannelNumber, absnote, velPlay, dur.TotalSubdivs);

                    //if (dur.TotalSubdivs > 0) // specific duration TODO2 needed?
                    //{
                    //    // Remove any lingering note offs and add a fresh one.
                    //    _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber && s.ChannelNumber == stt.ChannelNumber);

                    //    _stops.Add(new()
                    //    {
                    //        Device = stt.Device,
                    //        ChannelNumber = stt.ChannelNumber,
                    //        NoteNumber = MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                    //        Expiry = stt.Duration.TotalSubdivs
                    //    });
                    //}
                    ///// <summary>Notes to stop later.</summary>
                    //readonly List<StepNoteOff_XXX> _stops = new();
                    // public void Housekeep()
                    // {
                    //     // Send any stops due.
                    //     _stops.ForEach(s => { s.Expiry--; if (s.Expiry < 0) Send(s); });

                    //     // Reset.
                    //     _stops.RemoveAll(s => s.Expiry < 0);
                    // }

                    channel.Device?.SendEvent(evt);
                }
                else
                {
                    NoteEvent evt = new(0, channel.Channel.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
                    channel.Device?.SendEvent(evt);
                }
            }
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        protected void SendNote(string chanName, string notestr, double vol, Time dur)
        {
            SequenceElement note = new(notestr);
            note.Notes.ForEach(n => SendNote(chanName, n, vol, dur));
        }

        /// <summary>Send a note immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, int notenum, double vol, double dur = 0.0)
        {
            SendNote(chanName, notenum, vol, new Time(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        protected void SendNote(string chanName, string notestr, double vol, double dur = 0.0)
        {
            SendNote(chanName, notestr, vol, new Time(dur));
        }

        /// <summary>Send a note on immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume.</param>
        protected void SendNoteOn(string chanName, int notenum, double vol)
        {
            SendNote(chanName, notenum, vol);
        }

        /// <summary>Send a note off immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        protected void SendNoteOff(string chanName, int notenum)
        {
            SendNote(chanName, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="ctlid">Controller.</param>
        /// <param name="val">Controller value.</param>
        protected void SendController(string chanName, int ctlid, int val)
        {
            var channel = GetChannel(chanName);
            if (channel is not null)
            {
                ControlChangeEvent evt = new(0, channel.Channel.ChannelNumber, (MidiController)ctlid, val);
                channel.Device?.SendEvent(evt);
            }
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="chanName"></param>
        /// <param name="patch"></param>
        protected void SendPatch(string chanName, int patch)
        {
            var channel = GetChannel(chanName);
            if (channel is not null)
            {
                PatchChangeEvent evt = new(0, channel.Channel.ChannelNumber, patch);
                channel.Device?.SendEvent(evt);
            }
        }
        #endregion

        ///// <summary>Send a named sequence at some point.</summary>
        ///// <param name="chanName">Which channel to send it on.</param>
        ///// <param name="seq">Which sequence to send.</param>
        ///// <param name="beat">When to send the sequence. Must be in the future.</param>
        //protected void SendSequence(string chanName, Sequence seq, int beat) //TODO1
        //{
        //    if (seq is null)
        //    {
        //        throw new Exception($"Invalid Sequence");
        //    }

        //    StepCollection scoll = ConvertToSteps(chanName, seq, beat);
        //    _transientSteps.Add(scoll);
        //}
    }
}
