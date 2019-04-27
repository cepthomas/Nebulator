using System;
using Nebulator.Device;


namespace Nebulator.OSC
{
    /// <summary>
    /// Misc utilities.
    /// </summary>
    public static class OscCommon
    {
        public const int MAX_NOTE = 127;

        public static DeviceLogCategory TranslateLogCategory(NebOsc.LogCategory cat)
        {
            DeviceLogCategory dlog = DeviceLogCategory.Error;

            switch (cat)
            {
                case NebOsc.LogCategory.Error: dlog = DeviceLogCategory.Error; break;
                case NebOsc.LogCategory.Info: dlog = DeviceLogCategory.Info; break;
                case NebOsc.LogCategory.Recv: dlog = DeviceLogCategory.Recv; break;
                case NebOsc.LogCategory.Send: dlog = DeviceLogCategory.Send; break;
            }

            return dlog;
        }
    }
}