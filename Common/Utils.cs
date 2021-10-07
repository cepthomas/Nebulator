using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace Nebulator.Common
{
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal, Mute, Solo }

    /// <summary>Trace log markers.</summary>
    public enum TraceCat { SND, RCV }

    public static class Definitions
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>General UI.</summary>
        public const int BORDER_WIDTH = 1;

        /// <summary>Standard midi.</summary>
        public const int MAX_MIDI = 127;
        #endregion
    }

    /// <summary>
    /// Static utility functions.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i">Number to fix.</param>
        /// <returns>Fixed number.</returns>
        public static UInt32 FixEndian(UInt32 i)
        {
            return BitConverter.IsLittleEndian ?
                ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24) :
                i;
        }

        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i">Number to fix.</param>
        /// <returns>Fixed number.</returns>
        public static UInt16 FixEndian(UInt16 i)
        {
            return BitConverter.IsLittleEndian ?
                (UInt16)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8)) :
                i;
        }
    }
}
