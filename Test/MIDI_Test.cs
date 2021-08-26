using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NBagOfTricks;
using NBagOfTricks.PNUT;
using Nebulator.Common;
using Nebulator.Device;
using Nebulator.Midi;


namespace Nebulator.Test
{
    // Midi utils stuff.
    public class MIDI_Utils : TestSuite
    {
        public override void RunSuite()
        {
            ///// Export midi.
            StepCollection steps = new StepCollection();

            Dictionary<int, string> channels = new Dictionary<int, string>();
            new List<int>() { 1, 2, 3 }.ForEach(i => channels.Add(i, $"CHANNEL_{i}"));

            MidiUtils.ExportMidi(steps, "midiFileName", channels, 1.0, "string info");
        }
    }
}
