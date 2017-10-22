using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using NLog;
using Nebulator.Common;


// FUTURE record midi directly to neb format?

namespace Nebulator.Midi
{
    public class MidiUtils
    {
        /// <summary>
        /// Convert neb steps to midi file.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="midiFileName"></param>
        /// <param name="tracks">Map of channel number to track name.</param>
        /// <param name="secPerTick">Seconds per Tick (aka qtr note).</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public static void ExportMidi(StepCollection steps, string midiFileName, Dictionary<int, string> tracks, double secPerTick, string info)
        {
            Dictionary<int, IList<MidiEvent>> trackEvents = new Dictionary<int, IList<MidiEvent>>();

            ///// Calc some times.
            int deltaTicksPerQuarterNote = 96; // fixed output value
            //double tocksPerQuarterNote = Globals.TOCKS_PER_TICK / 4;
            //double deltaTicksPerTock = deltaTicksPerQuarterNote / tocksPerQuarterNote;
            // double ticksPerClick = deltaTicksPerQuarterNote * 4 * speed;
            //long usecPerQuarterNote = (long)(1000000.0 * secPerQuarterNote);

            ///// Meta file stuff.
            MidiEventCollection events = new MidiEventCollection(1, deltaTicksPerQuarterNote);

            ///// Add Header chunk stuff.
            IList<MidiEvent> lhdr = events.AddTrack();
            //lhdr.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, 8));
            //TimeSignatureEvent me = new TimeSignatureEvent(long absoluteTime, int numerator, int denominator, int ticksInMetronomeClick, int no32ndNotesInQuarterNote);
            //  - numerator of the time signature (as notated).
            //  - denominator of the time signature as a negative power of 2 (ie 2 represents a quarter-note, 3 represents an eighth-note, etc).
            //  - number of MIDI clocks between metronome clicks.
            //  - number of notated 32nd-notes in a MIDI quarter-note (24 MIDI Clocks). The usual value for this parameter is 8.

            //lhdr.Add(new KeySignatureEvent(0, 0, 0));
            //  - number of flats (-ve) or sharps (+ve) that identifies the key signature (-7 = 7 flats, -1 = 1 //flat, 0 = key of C, 1 = 1 sharp, etc).
            //  - major (0) or minor (1) key.
            //  - abs time.

            lhdr.Add(new TempoEvent((int)(1000000.0 * secPerTick), 0));

            lhdr.Add(new TextEvent("Midi file created by Nebulator.", MetaEventType.TextEvent, 0));
            lhdr.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            lhdr.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

            ///// Make one midi event collection per track.
            foreach(int channel in tracks.Keys)
            {
                IList<MidiEvent> le = events.AddTrack();
                trackEvents.Add(channel, le);
                le.Add(new TextEvent(tracks[channel], MetaEventType.SequenceTrackName, 0));
                // >> 0 SequenceTrackName G.MIDI Acou Bass
            }

            // Run through the main steps and create a midi event per.
            foreach (Time time in steps.Times)
            {
                foreach (Step step in steps.GetSteps(time))
                {
                    MidiEvent evt = null;

                    double d = (time.Tick + time.Tock / Globals.TOCKS_PER_TICK) * secPerTick * deltaTicksPerQuarterNote;
                    long midiTime = (long)d;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            evt = new NoteEvent(midiTime, stt.Channel, MidiCommandCode.NoteOn, stt.NoteNumber, stt.Velocity);
                            break;

                        case StepNoteOff stt:
                            evt = new NoteEvent(midiTime, stt.Channel, MidiCommandCode.NoteOff, stt.NoteNumber, stt.Velocity);
                            break;

                        case StepControllerChange stt:
                            if (stt.MidiController == MidiInterface.CTRL_PITCH) // hacked in pitch support
                            {
                                evt = new PitchWheelChangeEvent(midiTime, stt.Channel, stt.ControllerValue);
                            }
                            else // normal controller
                            {
                                evt = new ControlChangeEvent(midiTime, stt.Channel, (MidiController)stt.MidiController, stt.ControllerValue);
                            }
                            break;

                        case StepPatch stt:
                            evt = new PatchChangeEvent(midiTime, stt.Channel, stt.PatchNumber);
                            break;

                        default:
                            break;
                    }

                    if (evt != null)
                    {
                        trackEvents[step.Channel].Add(evt);
                    }
                }
            }

            // Finish up track collections.
            foreach (IList<MidiEvent> let in trackEvents.Values)
            {
                long ltime = let.Last().AbsoluteTime;
                let.Add(new MetaEvent(MetaEventType.EndTrack, 0, ltime));
            }

            MidiFile.Export(midiFileName, events);
        }


        /// <summary>
        /// Read a style file into text that can be placed in a neb file. TODO
        /// </summary>
        /// <param name="fileName"></param>
        public static void ImportStyle(string fileName)
        {
            StyleParser sty = new StyleParser();

            sty.ProcessFile(fileName);


            // Process collected events into strings. 

            //Output should be strings like this:
            ///// Constants /////
            // When to play.
            //const(START, 0);
            //const(PART1, 99);
            // Total length.
            //const(TLEN, 333);

            ///// Tracks and Loops /////
            //track(TRACK1, 1, 0);
            //loop(START, PART1, SEQ1);
            //loop(PART1, TLEN, SEQ2);

            //track(TRACK2, 2, 0);
            //loop(START, TLEN, SEQ2);

            //track(DRUMS, 10, 0);
            //loop(START, TLEN, SEQ3);

            ///// Sequences and Notes /////
            //seq(SEQ1, 8);
            //note(0.00, F.4, 90, 0.08);
            //note(0.1/2, D#.4, 111, 0.08);
            //note(1.21, C.4, 90, 0.08);
            // ....

            //seq(SEQ2, TLEN);
            // ....


            List<string> ls1 = new List<string>
            {
                "///// Constants /////",
                "const (START, 0);"
            };

            List<string> ls2 = new List<string>
            {
                "///// Tracks and Loops /////"
            };

            List<string> ls3 = new List<string>
            {
                "///// Sequences and Notes /////"
            };

            // Process each defined part in the midi data. TODO
            HashSet<int> channels = new HashSet<int>();
            HashSet<int> whens = new HashSet<int>();

            //List<string> parts = events.Keys.ToList();
            //foreach (string part in parts)
            //{
            //    //ls2.Add($"track(TRACK1, 1, 0);");

            //    // part <> 
            //    MidiEventCollection ec = events[part];

            //    int tpqn = ec.DeltaTicksPerQuarterNote;

            //    foreach (MidiEvent me in ec[0])
            //    {
            //        channels.Add(me.Channel);
            //    }

            //    // We have a match. Diff the absolute time and convert to Time type. TODO
            //    // note(4.00, F.3, 90, 0.08);
            //}


            // a delta time of 960 when the resolution is 1920 ticks per quarter note is after a 1/8 note rest


            // Time is measured in “delta time” which is defined as the number of ticks (the resolution of which is
            // defined in the header) before the midi event is to be executed. I.e., a delta time of 0 =
            // immediately; a delta time of 960 when the resolution is 1920 ticks per quarter note is after a
            // 1/8 note rest. Delta time is a variable length format using 7 of the 8 available bits; the
            // maximum time value of any time byte is 127 (7FH). The first or 8th bit is used to identify the
            // last of the delta time bytes; the least significant byte is indicated by a leading bit=0, all other
            // bytes have a leading bit=1.

            // Track chunks(identifier = MTrk) contain a sequence of time - ordered events(MIDI and / or sequencer - specific data), 
            // each of which has a delta time value associated with it - ie the amount of time(specified in tickdiv units) since the 
            // previous event.


        }
    }
}
