using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.Script
{
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal, Mute, Solo }

    /// <summary>Display types.</summary>
    public enum DisplayType { LinearMeter, LogMeter, Chart }

    /// <summary>
    /// One bound variable.
    /// </summary>
    public class NVariable
    {
        #region Properties
        /// <summary>Variable name - as shown in ui.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Value as double. It is initialized from the script supplied value.</summary>
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                if(value != _value)
                {
                    _value = value;
                    Changed?.Invoke();
                    ValueChangeEvent?.Invoke(this, null);
                }
            }
        }
        double _value;

        /// <summary>Min value - optional.</summary>
        public double Min { get; set; } = 0;

        /// <summary>Max value - optional.</summary>
        public double Max { get; set; } = 100;

        /// <summary>For extra info. Makes me feel dirty.</summary>
        public object Tag { get; set; } = null;
        #endregion

        #region Events
        /// <summary>Notify with new value. This represents a callback defined in a script.</summary>
        public Action Changed;

        /// <summary>Reporting a change to internal listeners.</summary>
        public event EventHandler ValueChangeEvent;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NVariable: Name:{Name} Value:{Value} Min:{Min} Max:{Max}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Defines a controller input.
    /// </summary>
    public class NController
    {
        #region Properties
        /// <summary>The associated comm device name.</summary>
        public string DeviceName { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated comm device.</summary>
        public NInput Device { get; set; } = null;

        /// <summary>The associated numerical (midi) channel to use.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>The numerical controller type. Usually the same as midi.</summary>
        public int ControllerId { get; set; } = 0;

        /// <summary>The bound var.</summary>
        public NVariable BoundVar { get; set; } = null;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NController: ControllerId:{ControllerId} BoundVar:{BoundVar.Name} ChannelNumber:{ChannelNumber}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// One channel output, usually an instrument.
    /// </summary>
    public class NChannel
    {
        #region Properties
        /// <summary>The associated device name.</summary>
        public string DeviceName { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated device.</summary>
        public NOutput Device { get; set; } = null;

        /// <summary>The UI name for this channel.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>Helper to add short default duration to each hit without needing to explicitly put in script.</summary>
        public bool IsDrums { get; set; } = false;

        /// <summary>Current volume.</summary>
        public double Volume { get; set; } = 90;

        /// <summary>Wobbler for time.</summary>
        public Wobbler TimeWobbler { get; set; } = new Wobbler();

        /// <summary>Wobbler for volume.</summary>
        public Wobbler VolWobbler { get; set; } = new Wobbler();

        /// <summary>Current state for this channel.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;
        #endregion

        /// <summary>
        /// Get the next time.
        /// </summary>
        /// <returns></returns>
        public int NextTime()
        {
            return (int)TimeWobbler.Next(0);
        }

        /// <summary>
        /// Get the next volume.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            return VolWobbler.Next(def);
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NChannel: Name:{Name} ChannelNumber:{ChannelNumber}";
        }
    }

    /// <summary>
    /// One display output.
    /// </summary>
    public class NDisplay
    {
        #region Properties
        /// <summary>The type of display.</summary>
        public DisplayType DisplayType { get; set; } = DisplayType.LinearMeter;

        /// <summary>The bound var.</summary>
        public NVariable BoundVar { get; set; } = null;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NDisplay: DisplayType:{DisplayType} BoundVar:{BoundVar.Name}");
            return sb.ToString();
        }
    }

}
