﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nebulator.Common;


namespace Nebulator.Comm
{
    /// <summary>
    /// Base class for internal interface representation of a compiled event to be sent or received.
    /// </summary>
    public abstract class Step
    {
        /// <summary>Associated comm device - optional.</summary>
        public NOutput Output { get; set; } = null;

        /// <summary>Channel number.</summary>
        public int ChannelNumber { get; set; } = 1;

        /// <summary>Possibly make adjustments to values.</summary>
        /// <param name="masterVolume"></param>
        /// <param name="channelVolume"></param>
        public virtual void Adjust(int masterVolume, int channelVolume) { }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"ChannelNumber:{ChannelNumber}";
        }
    }

    /// <summary>
    /// One note on.
    /// </summary>
    public class StepNoteOn : Step
    {
        /// <summary>The note to play.</summary>
        public int NoteNumber { get; set; }

        /// <summary>The default volume.</summary>
        public int Velocity { get; set; } = 90;

        /// <summary>The possibly modified Volume.</summary>
        public int VelocityToPlay { get; set; } = 90;

        /// <summary>Time between note on/off. Default of 0 indicates note off generated by owner.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <inheritdoc />
        public override void Adjust(int masterVolume, int channelVolume)
        {
            // Maybe alter note velocity.
            int vel = Velocity * channelVolume * masterVolume / Output.Caps.MaxVolume / Output.Caps.MaxVolume;
            VelocityToPlay = Utils.Constrain(vel, 0, Output.Caps.MaxVolume);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepNoteOn: {base.ToString()} VelocityToPlay:{VelocityToPlay} Duration:{Duration}";
        }
    }

    /// <summary>
    /// One note off.
    /// </summary>
    public class StepNoteOff : Step
    {
        /// <summary>The note to stop.</summary>
        public int NoteNumber { get; set; }

        /// <summary>Velocity.</summary>
        public int Velocity { get; set; } = 64; // seems to be standard default.

        /// <summary>When it's done in tocks - used by stop note chasing.</summary>
        public int Expiry { get; set; } = -1;

        /// <inheritdoc />
        public override void Adjust(int masterVolume, int channelVolume)
        {
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepNoteOff: {base.ToString()} NoteNumber:{NoteNumber}";
        }
    }

    /// <summary>
    /// One control change event. This supports
    ///   - standard CC messages
    ///   - pitch (rather than have a separate type)
    ///   - notes that can be used as control inputs
    /// </summary>
    public class StepControllerChange : Step
    {
        /// <summary>Specific controller. See also specials in ControllerType.</summary>
        public int ControllerId { get; set; } = 0;

        /// <summary>The payload. CC value, pitch value, note number.</summary>
        public int Value { get; set; } = 0;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"StepControllerChange: {base.ToString()}");

            if (ControllerId == ScriptDefinitions.TheDefinitions.NoteControl)
            {
                sb.Append($" Note:{Value}");
            }
            else if (ControllerId == ScriptDefinitions.TheDefinitions.PitchControl)
            {
                sb.Append($" Pitch:{Value}");
            }
            else // CC
            {
                sb.Append($" ControllerId:{ControllerId} Value:{Value}");
            }

            return sb.ToString();
        }
    }

    public class StepPatch : Step
    {
        /// <summary>Specific patch.</summary>
        public int PatchNumber { get; set; } = 0;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepPatch: {base.ToString()} PatchNumber:{PatchNumber}";
        }
    }

    /// <summary>Used for internal things that are not actually comm protocol.</summary>
    public class StepInternal : Step
    {
        /// <summary>A function to call.</summary>
        public Action ScriptFunction { get; set; } = null;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepInternal: {base.ToString()} Function:{ScriptFunction}";
        }
    }
}
