﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBagOfTricks;


namespace Nebulator.Common
{
    /// <summary>
    /// Base class for representation of a received event or a compiled event to be sent.
    /// </summary>
    public abstract class Step_XXX_
    {
        ///// <summary>Associated comm device to use.</summary>
        //public IDevice? Device { get; set; } = null;

        /// <summary>Channel number 1-16.</summary>
        public int ChannelNumber { get; set; } = 1;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"channel:{ChannelNumber}";
        }
    }

    /// <summary>
    /// One note on.
    /// </summary>
    public class StepNoteOn_XXX_ : Step_XXX_
    {
        /// <summary>The note to play.</summary>
        public double NoteNumber { get; set; }

        /// <summary>The volume 0 -> 1.</summary>
        public double Velocity { get; set; } = 0.5;

        /// <summary>The possibly modified Volume.</summary>
        public double VelocityToPlay { get; set; } = 0.5;

        /// <summary>Time between note on/off. Default of 0 indicates note off generated by owner.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>Possibly make adjustments to values.</summary>
        /// <param name="masterVolume"></param>
        /// <param name="channelVolume"></param>
        public void Adjust(double masterVolume, double channelVolume)
        {
            // Maybe alter note velocity.
            //if (Device is IOutputDevice)
            //{
                double vel = Velocity * channelVolume * masterVolume;
                VelocityToPlay = MathUtils.Constrain(vel, 0, 1.0);
            //}
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepNoteOn: {base.ToString()} note:{NoteNumber:F2} vel:{VelocityToPlay:F2} dur:{Duration}";
        }
    }

    /// <summary>
    /// One note off.
    /// </summary>
    public class StepNoteOff_XXX_ : Step_XXX_
    {
        /// <summary>The note to stop.</summary>
        public double NoteNumber { get; set; }

        /// <summary>Velocity.</summary>
        public double Velocity { get; set; } = 64; // seems to be standard default.

        /// <summary>When it's done in subdivs - used by stop note chasing.</summary>
        public int Expiry { get; set; } = -1;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepNoteOff: {base.ToString()} note:{NoteNumber:F2}";
        }
    }

    /// <summary>
    /// One controller change event. This supports
    ///   - standard CC messages
    ///   - pitch (rather than have a separate type)
    ///   - notes that can be used as controller inputs
    /// </summary>
    public class StepControllerChange_XXX_ : Step_XXX_
    {
        /// <summary>Specific controller. See also specials in ControllerType.</summary>
        public int ControllerId { get; set; } = -1;

        /// <summary>The payload. CC value, midi pitch value, note number.</summary>
        public double Value { get; set; } = 0;

        ///// <summary>For viewing pleasure.</summary>
        //public override string ToString()
        //{
        //    StringBuilder sb = new($"StepControllerChange: {base.ToString()}");

        //    if (ControllerId == ControllerDef.NoteControl)
        //    {
        //        sb.Append($" Note:{Value:F2}");
        //    }
        //    else if (ControllerId == ControllerDef.PitchControl)
        //    {
        //        sb.Append($" Pitch:{Value:F2}");
        //    }
        //    else // CC
        //    {
        //        sb.Append($" ControllerId:{ControllerId} value:{Value:F2}");
        //    }

        //    return sb.ToString();
        //}
    }

    /// <summary>Used for patches.</summary>
    public class StepPatch_XXX_ : Step_XXX_
    {
        /// <summary>Specific patch.</summary>
        public int Patch { get; set; } = -1;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepPatch: {base.ToString()} patch:{Patch}";
        }
    }

    /// <summary>Step that calls a function.</summary>
    public class StepFunction_XXX_ : Step_XXX_
    {
        /// <summary>A function to call.</summary>
        public Action? ScriptFunction { get; set; }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"StepFunction: {base.ToString()} function:{ScriptFunction}";
        }
    }
}
