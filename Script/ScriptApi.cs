using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;


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

        #region Functions that can be overridden in the user script
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called every mmtimer increment.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputNote(DeviceType dev, int channel, double note) { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputControl(DeviceType dev, int channel, int ctlid, double value) { }
        #endregion

        #region Script callable functions
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

            _sequences.Add(nseq);
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

        /// <summary>Send a note immediately. Lowest level sender. Adds a note off to play after dur time.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, double notenum, double vol, Time dur)
        {
            Channel channel = GetChannel(chanName);

            double vel = channel.NextVol(vol);
            double absnote = MathUtils.Constrain(Math.Abs(notenum), 0, 127);

            // If vol is positive and the note is not negative, it's note on, else note off.
            if (vol > 0 && notenum > 0)
            {
                StepNoteOn step = new()
                {
                    Device = channel.Device,
                    ChannelNumber = channel.ChannelNumber,
                    NoteNumber = absnote,
                    Velocity = vel,
                    VelocityToPlay = vel,
                    Duration = dur
                };

                step.Adjust(MasterVolume, channel.Volume);
                channel.Device.Send(step);
            }
            else
            {
                StepNoteOff step = new()
                {
                    Device = channel.Device,
                    ChannelNumber = channel.ChannelNumber,
                    NoteNumber = absnote
                };

                channel.Device.Send(step);
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

        /// <summary>Send a note immediately. Lowest level sender. Adds a note off to play after dur time.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(string chanName, double notenum, double vol, double dur = 0.0)
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
        protected void SendNoteOn(string chanName, double notenum, double vol)
        {
            SendNote(chanName, notenum, vol);
        }

        /// <summary>Send a note off immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        protected void SendNoteOff(string chanName, double notenum)
        {
            SendNote(chanName, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="ctlnum">Controller number.</param>
        /// <param name="val">Controller value.</param>
        protected void SendController(string chanName, int ctlnum, double val)
        {
            Channel channel = GetChannel(chanName);

            StepControllerChange step = new()
            {
                Device = channel.Device,
                ChannelNumber = channel.ChannelNumber,
                ControllerId = ctlnum,
                Value = val
            };

            channel.Device.Send(step);
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="chanName"></param>
        /// <param name="patch"></param>
        protected void SendPatch(string chanName, int patch)
        {
            Channel channel = GetChannel(chanName);

            StepPatch step = new()
            {
                Device = channel.Device,
                ChannelNumber = channel.ChannelNumber,
                PatchNumber = patch
            };

            channel.Device.Send(step);
        }

        /// <summary>Send a named sequence now. TODO1 not actually now, and becomes a permanent member - what is useful?</summary>
        /// <param name="chanName">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        protected void SendSequence(string chanName, Sequence seq)
        {
            StepCollection scoll = ConvertToSteps(chanName, seq, StepTime.Beat);
            _steps.Add(scoll);
        }

        /// <summary>
        /// Add a chord or scale definition.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">"1 4 6 b13"</param>
        protected void CreateNotes(string name, string parts)
        {
            NoteUtils.AddScriptNoteDef(name, parts);
        }

        /// <summary>Convert the argument into numbered notes.</summary>
        /// <param name="note">Note string using any form allowed in the script.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        protected double[] GetChordNotes(string note)
        {
            List<double> notes = NoteUtils.ParseNoteString(note);
            return notes != null ? notes.ToArray() : Array.Empty<double>();
        }

        /// <summary>Get an array of scale notes.</summary>
        /// <param name="scale">One of the named scales from ScriptDefinitions.md.</param>
        /// <param name="key">Note name and octave.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        protected double[] GetScaleNotes(string scale, string key)
        {
            List<double> notes = NoteUtils.GetScaleNotes(scale, key);
            return notes != null ? notes.ToArray() : Array.Empty<double>();
        }
        #endregion

        #region Script utilities
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
            _logger.Info(string.Join(" | ", vars));
        }
        #endregion
    }
}
