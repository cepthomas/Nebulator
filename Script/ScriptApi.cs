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
    public partial class NebScript
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
        public double Volume { get; set; } = 0;

//TODO1 these?
        /// <summary>Subdivision. Main:W Script:R</summary>
        public int SubdivsPerBeat { get { return Time.SUBDIVS_PER_BEAT; } }

        /// <summary>Current Nebulator Beat. Main:W Script:R</summary>
        public int Beat { get { return StepTime.Beat; } }

        /// <summary>Current Nebulator Subdiv. Main:W Script:R</summary>
        public int Subdiv { get { return StepTime.Subdiv; } }
        #endregion

        #region Functions that can be overridden in the user script
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        // /// <summary>Called if you need to do something with devices after they have been created.</summary>
        // public virtual void Setup2() { }

        /// <summary>Called every mmtimer increment.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputHandler(DeviceType dev, int channel, double value) { } //TODO1 - noteon, noteoff, control, ...  MidiCommandCode?
        #endregion

        #region Script callable functions
        /// <summary>
        /// Create a defined sequence and add to internal collection.
        /// </summary>
        /// <param name="beats">Length in beats.</param>
        /// <param name="elements">.</param>
        protected Sequence CreateSequence(int beats, SequenceElements elements)
        {
            Sequence nseq = new Sequence()
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

            Section nsect = new Section()
            {
                Beats = beats,
                Name = name,
                Elements = elements,
            };
            
            _sections.Add(nsect);
            return nsect;
        }

        /// <summary>Send a note immediately. Lowest level sender. Adds a note off to play after dur time.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(Channel channel, double notenum, double vol, Time dur)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid Channel");
            }

            double vel = channel.NextVol(vol);
            double absnote = MathUtils.Constrain(Math.Abs(notenum), 0, 127);

            // If vol is positive and the note is not negative, it's note on, else note off.
            if (vol > 0 && notenum > 0)
            {
                StepNoteOn step = new StepNoteOn()
                {
                    Device = channel.Device,
                    ChannelNumber = channel.ChannelNumber,
                    NoteNumber = absnote,
                    Velocity = vel,
                    VelocityToPlay = vel,
                    Duration = dur
                };

                step.Adjust(Volume, channel.Volume);
                channel.Device.Send(step);
            }
            else
            {
                StepNoteOff step = new StepNoteOff()
                {
                    Device = channel.Device,
                    ChannelNumber = channel.ChannelNumber,
                    NoteNumber = absnote
                };

                channel.Device.Send(step);
            }
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        protected void SendNote(Channel channel, string notestr, double vol, Time dur)
        {
            SequenceElement note = new SequenceElement(notestr);

            if (note.Notes.Count == 0)
            {
                throw new Exception($"Invalid notestr: {notestr}");
            }
            else
            {
                note.Notes.ForEach(n => SendNote(channel, n, vol, dur));
            }
        }

        /// <summary>Send a note immediately. Lowest level sender. Adds a note off to play after dur time.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        protected void SendNote(Channel channel, double notenum, double vol, double dur = 0.0)
        {
            SendNote(channel, notenum, vol, new Time(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        protected void SendNote(Channel channel, string notestr, double vol, double dur = 0.0)
        {
            SendNote(channel, notestr, vol, new Time(dur));
        }

        /// <summary>Send a note on immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume.</param>
        protected void SendNoteOn(Channel channel, double notenum, double vol)
        {
            SendNote(channel, notenum, vol);
        }

        /// <summary>Send a note off immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        protected void SendNoteOff(Channel channel, double notenum)
        {
            SendNote(channel, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="ctlnum">Controller number.</param>
        /// <param name="val">Controller value.</param>
        protected void SendController(Channel channel, int ctlnum, double val)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid Channel");
            }

            StepControllerChange step = new StepControllerChange()
            {
                Device = channel.Device,
                ChannelNumber = channel.ChannelNumber,
                ControllerId = ctlnum,
                Value = val
            };

            channel.Device.Send(step);
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="channel"></param>
        /// <param name="patch"></param>
        protected void SendPatch(Channel channel, int patch)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid Channel");
            }

            StepPatch step = new StepPatch()
            {
                Device = channel.Device,
                ChannelNumber = channel.ChannelNumber,
                PatchNumber = patch
            };

            channel.Device.Send(step);
        }

        /// <summary>Send a named sequence now. TODO1 not actually now, and becomes a permanent member - what is useful?</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        protected void SendSequence(Channel channel, Sequence seq)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid Channel");
            }

            if (seq is null)
            {
                throw new Exception($"Invalid Sequence");
            }

            StepCollection scoll = ConvertToSteps(channel, seq, StepTime.Beat);
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
            return notes != null ? notes.ToArray() : new double[0];
        }

        /// <summary>Get an array of scale notes.</summary>
        /// <param name="scale">One of the named scales from ScriptDefinitions.md.</param>
        /// <param name="key">Note name and octave.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        protected double[] GetScaleNotes(string scale, string key)
        {
            List<double> notes = NoteUtils.GetScaleNotes(scale, key);
            return notes != null ? notes.ToArray() : new double[0];
        }
        #endregion

        #region Utilities like Processing
        protected double Random(double max) { return _rand.NextDouble() * max; }
        protected double Random(double min, double max) { return min + _rand.NextDouble() * (max - min); }
        protected int Random(int max) { return _rand.Next(max); }
        protected int Random(int min, int max) { return _rand.Next(min, max); }
        protected void Print(params object[] vars) { _logger.Info(string.Join(" | ", vars)); }
        #endregion
    }
}
