using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


// Nebulator script API stuff.

namespace Nebulator.Script
{
    public partial class NebScript
    {
        #region Properties that can be referenced in the script
        /// <summary>Current Nebulator step time. Main -> Script</summary>
        public Time StepTime { get; set; } = new Time();

        /// <summary>Sound is playing. Main -> Script</summary>
        public bool Playing { get; set; } = false;

        /// <summary>Subdivision.</summary>
        public int SubdivsPerBeat { get { return Time.SUBDIVS_PER_BEAT; } }

        /// <summary>Current Nebulator Beat.</summary>
        public int Beat { get { return StepTime.Beat; } }

        /// <summary>Current Nebulator Subdiv.</summary>
        public int Subdiv { get { return StepTime.Subdiv; } }

        /// <summary>Actual time since start pressed. Main -> Script</summary>
        public double RealTime { get; set; } = 0.0;

        /// <summary>Nebulator Speed in bpm. Main -> Script ( -> Main)</summary>
        public double Speed { get; set; } = 0.0;

        /// <summary>Nebulator master Volume. Main -> Script ( -> Main)</summary>
        public double Volume { get; set; } = 0;
        #endregion

        #region Functions that can be overridden in the user script
        /// <summary>Called to initialize Nebulator stuff.</summary>
        public virtual void Setup() { }

        /// <summary>Called if you need to do something with devices after they have been created.</summary>
        public virtual void Setup2() { } //TODO probably don't need

        /// <summary>Called every Nebulator Incr.</summary>
        public virtual void Step() { }

        /// <summary>Called when input arrives.</summary>
        public virtual void InputHandler(DeviceType dev, int channel, double value) { } //TODO - noteon, noteoff, control, ...  MidiCommandCode?
        #endregion

        #region Script callable functions
        // /// <summary>
        // /// Normal factory.
        // /// </summary>
        // /// <param name="name">UI name</param>
        // /// <param name="val">Initial value</param>
        // /// <param name="min"></param>
        // /// <param name="max"></param>
        // /// <param name="handler">Optional callback function.</param>
        // protected NVariable CreateVariable(string name, double val, double min, double max, Action handler = null)
        // {
        //     NVariable nv = new NVariable() { Name = name, Value = val, Min = min, Max = max, Changed = handler };
        //     Variables.Add(nv);
        //     return nv;
        // }


        // /// <summary>
        // /// Create a UI lever.
        // /// </summary>
        // /// <param name="bound"></param>
        // protected void CreateLever(NVariable bound)
        // {
        //     if (bound is null)
        //     {
        //         throw new Exception($"Invalid NVariable for lever");
        //     }

        //     NController ctlr = new NController() { BoundVar = bound };
        //     Levers.Add(ctlr);
        // }

        // /// <summary>
        // /// Create a UI meter.
        // /// </summary>
        // /// <param name="bound"></param>
        // /// <param name="type"></param>
        // protected void CreateDisplay(NVariable bound, int type)
        // {
        //     if (bound is null)
        //     {
        //         throw new Exception($"Invalid NVariable for meter");
        //     }

        //     NDisplay disp = new NDisplay()
        //     {
        //         BoundVar = bound,
        //         DisplayType = (DisplayType)type,
        //     };

        //     Displays.Add(disp);
        // }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="beats">Length in beats.</param>
        /// <param name="elements">.</param>
        protected NSequence CreateSequence(int beats, NSequenceElements elements)
        {
            NSequence nseq = new NSequence()
            {
                Beats = beats,
                Elements = elements
            };

            Sequences.Add(nseq);
            return nseq;
        }

        /// <summary>
        /// Create a defined section.
        /// </summary>
        /// <param name="beats">How long in beats.</param>
        /// <param name="name">For UI display.</param>
        /// <param name="elements">Section info to add.</param>
        protected NSection CreateSection(int beats, string name, NSectionElements elements)
        {
            // Sanity check elements.
            foreach (var el in elements)
            {
                if (el.Channel is null)
                {
                    throw new Exception($"Invalid NChannel at index {elements.IndexOf(el)}");
                }
            }

            NSection nsect = new NSection()
            {
                Beats = beats,
                Name = name,
                Elements = elements,
            };
            
            Sections.Add(nsect);
            return nsect;
        }

        /// <summary>Send a note immediately. Lowest level sender. Adds a note off to play after dur time.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated so user has to turn it off explicitly.</param>
        public void SendNote(NChannel channel, double notenum, double vol, Time dur)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid NChannel");
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
        public void SendNote(NChannel channel, string notestr, double vol, Time dur)
        {
            NSequenceElement note = new NSequenceElement(notestr);

            if (note.Notes.Count == 0)
            {
                _logger.Warn($"Invalid notestr: {notestr}");
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
        public void SendNote(NChannel channel, double notenum, double vol, double dur = 0.0)
        {
            SendNote(channel, notenum, vol, new Time(dur));
        }

        /// <summary>Send one or more named notes immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notestr">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        public void SendNote(NChannel channel, string notestr, double vol, double dur = 0.0)
        {
            SendNote(channel, notestr, vol, new Time(dur));
        }

        /// <summary>Send a note on immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        /// <param name="vol">Note volume.</param>
        public void SendNoteOn(NChannel channel, double notenum, double vol)
        {
            SendNote(channel, notenum, vol);
        }

        /// <summary>Send a note off immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="notenum">Note number.</param>
        public void SendNoteOff(NChannel channel, double notenum)
        {
            SendNote(channel, notenum, 0);
        }

        /// <summary>Send a controller immediately.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="ctlnum">Controller number.</param>
        /// <param name="val">Controller value.</param>
        public void SendController(NChannel channel, int ctlnum, double val)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid NChannel");
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
        public void SendPatch(NChannel channel, int patch)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid NChannel");
            }

            StepPatch step = new StepPatch()
            {
                Device = channel.Device,
                ChannelNumber = channel.ChannelNumber,
                PatchNumber = patch
            };

            channel.Device.Send(step);
        }

        /// <summary>Send a named sequence now.</summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        public void SendSequence(NChannel channel, NSequence seq)
        {
            if (channel is null || channel.Device is null)
            {
                throw new Exception($"Invalid NChannel");
            }

            if (seq is null)
            {
                throw new Exception($"Invalid NSequence");
            }

            StepCollection scoll = ConvertToSteps(channel, seq, StepTime.Beat);
            Steps.Add(scoll);
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
        public double[] GetChordNotes(string note)
        {
            List<double> notes = NoteUtils.ParseNoteString(note);
            return notes != null ? notes.ToArray() : new double[0];
        }

        /// <summary>Get an array of scale notes.</summary>
        /// <param name="scale">One of the named scales from ScriptDefinitions.md.</param>
        /// <param name="key">Note name and octave.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        public double[] GetScaleNotes(string scale, string key)
        {
            List<double> notes = NoteUtils.GetScaleNotes(scale, key);
            return notes != null ? notes.ToArray() : new double[0];
        }
        #endregion

        #region Utilities like Processing
        public double Random(double max) { return _rand.NextDouble() * max; }
        public double Random(double min, double max) { return min + _rand.NextDouble() * (max - min); }
        public int Random(int max) { return _rand.Next(max); }
        public int Random(int min, int max) { return _rand.Next(min, max); }
        public void Print(params object[] vars) { _logger.Info(string.Join(" ", vars)); }
        #endregion
    }
}
