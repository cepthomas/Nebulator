using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using Nebulator.Common;
using Nebulator.Model;


// Generally 96 PPQ is sufficient to capture enough temporal variation. However, some musicians like to work with PPQs around 960 or more.

// FUTURE record midi in to neb format?

namespace Nebulator.Engine
{
    public class MidiUtils
    {
        /// <summary>
        /// Convert neb steps to midi file.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="midiFileName"></param>
        /// <param name="midiFileType">0 = single track, 1 = multi-track synchronous, 2 = multi-track asynchronous</param>
        /// <param name="deltaTicksPerQuarterNote">Ticks per qtr note aka tickdiv or ppqn.</param>
        /// <param name="speed">Seconds per Tick.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public static void ExportMidi(StepCollection steps, string midiFileName, int midiFileType, int deltaTicksPerQuarterNote, double speed, string info)
        {
            ///// Calc some times.
            double tocksPerQuarterNote = Globals.TocksPerTick / 4;
            double deltaTicksPerTock = deltaTicksPerQuarterNote / tocksPerQuarterNote;
            // double ticksPerClick = deltaTicksPerQuarterNote * 4 * speed;
            // double secPerQuarterNote = speed / 1000.0 / 4.0;
            // long microsecondsPerQuarterNote = (long)(1000000.0 * secPerQuarterNote);

            Dictionary<int, IList<MidiEvent>> trackEvents = new Dictionary<int, IList<MidiEvent>>();

            ///// Meta file stuff.
            MidiEventCollection events = new MidiEventCollection(midiFileType, deltaTicksPerQuarterNote);
            // >>> Format 1, Tracks 30, Delta Ticks Per Quarter Note 240

            ///// Add Header chunk stuff.
            IList<MidiEvent> lhdr = events.AddTrack();
            //lhdr.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, 8));
            //TimeSignatureEvent me = new TimeSignatureEvent(long absoluteTime, int numerator, int denominator, int ticksInMetronomeClick, int no32ndNotesInQuarterNote);
            // >>> 0 TimeSignature 4/4 TicksInClick:24 32ndsInQuarterNote:8
            //  - numerator of the time signature (as notated).
            //  - denominator of the time signature as a negative power of 2 (ie 2 represents a quarter-note, 3 represents an eighth-note, etc).
            //  - number of MIDI clocks between metronome clicks.
            //  - number of notated 32nd-notes in a MIDI quarter-note (24 MIDI Clocks). The usual value for this parameter is 8.

            //lhdr.Add(new KeySignatureEvent(0, 0, 0));
            //  - number of flats (-ve) or sharps (+ve) that identifies the key signature (-7 = 7 flats, -1 = 1 //flat, 0 = key of C, 1 = 1 sharp, etc).
            //  - major (0) or minor (1) key.
            //  - abs time.

            //lhdr.Add(new TempoEvent((int)microsecondsPerQuarterNote, 0));
            //TempoEvent te = new TempoEvent(int microsecondsPerQuarterNote, long absoluteTime);
            // >>> 0 SetTempo 120bpm (500000)

            lhdr.Add(new TextEvent("Midi file created by Nebulator.", MetaEventType.TextEvent, 0));
            lhdr.Add(new TextEvent(info, MetaEventType.TextEvent, 0));
            // >>> 0 Marker Killer Joe, Lorelei & other songs

            lhdr.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

            ///// Make one midi event collection per track.
            foreach (Track t in Globals.Dynamic.Tracks.Values)
            {
                IList<MidiEvent> le = events.AddTrack();
                trackEvents.Add(t.Channel, le);
                le.Add(new TextEvent(t.Name, MetaEventType.SequenceTrackName, 0));
                // >> 0 SequenceTrackName G.MIDI Acou Bass
            }

            // Run through the main steps and create a midi event per.
            foreach (int time in steps.Times)
            {
                foreach (Step step in steps.GetSteps(time))
                {
                    MidiEvent evt = null;
                    long midiTime = (long)(deltaTicksPerTock * time);

                    switch (step)
                    {
                        case StepNoteOn stt:
                            evt = new NoteEvent(midiTime, stt.Channel, MidiCommandCode.NoteOn, stt.NoteNumber, stt.Velocity);
                            break;

                        case StepNoteOff stt:
                            evt = new NoteEvent(midiTime, stt.Channel, MidiCommandCode.NoteOff, stt.NoteNumber, stt.Velocity);
                            break;

                        case StepControllerChange stt:
                            if (stt.MidiController == Midi.CTRL_PITCH) // hacked in pitch support
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
        /// Read a style file into text that can be placed in a neb file.
        /// For style parsing, only a minimal set is included. You can add the rest - see stylefiles_v101.pdf.
        /// </summary>
        /// <param name="fileName"></param>
        public static void ImportStyle(string fileName)
        {
            // Collected midi events.
            Dictionary<string, MidiEventCollection[]> events = null;

            using (var br = new BinaryReader(File.OpenRead(fileName)))
            {
                bool done = false;
                uint chunkSize = 0;

                while (!done)
                {
                    string chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                    //Console.WriteLine("--------" + chunkHeader);

                    switch(chunkHeader)
                    {
                        case "MThd":
                            // Midi part
                            events = ReadStyleMidiSection(br);
                            break;

                        case "CASM":
                            // The information in the CASM section is necessary if the midi section does not follow the rules
                            // for “simple” style files, which do not necessarily need a CASM section (see chapter 5.2.1 for
                            // the rules). The CASM section gives instructions to the instrument on how to deal with the midi data.
                            // This includes:
                            // • Assigning the sixteen possible midi channels to 8 accompaniment channels which are
                            // available to a style in the instrument (9 = sub rhythm, 10 = rhythm, 11 = bass, 12 = chord
                            // 1, 13 = chord 2, 14 = pad, 15 = phrase 1, 16 = phrase 2). More than one midi channel
                            // may be assigned to an accompaniment channel.
                            // • Allowing the PSR to edit the source channel in StyleCreator. This setting is overridden by
                            // the instrument if the style has > 1 midi source channel assigned to an accompaniment
                            // channel. In this case the source channels are not editable.
                            // • Muting/enabling specific notes or chords to trigger the accompaniment. In practice, only
                            // chord choices are used.
                            // • The key that is used in the midi channel. Styles often use different keys for the midi data.
                            // Styles without a CASM must be in the key of CMaj7.
                            // • How the chords and notes are transposed as chords are changed and how notes held
                            // through chord changes are reproduced.
                            // • The range of notes generated by the style.

                            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
                            break;

                        case "CSEG":
                            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
                            break;

                        case "Sdec":
                            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
                            // swallow for now
                            br.ReadBytes((int)chunkSize);
                            break;

                        case "Ctab":
                            // Has some key and chord info.
                            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
                            // swallow for now
                            br.ReadBytes((int)chunkSize);
                            break;

                        case "Cntt":
                            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
                            // swallow for now
                            br.ReadBytes((int)chunkSize);
                            break;

                        default:
                            // ignore the rest.
                            done = true;
                            break;
                    }
                }
            }

            // Process collected events into strings.
            List<string> ls = new List<string>();

            foreach(string part in events.Keys)
            {
                MidiEventCollection[] ec = events[part];

                int tpqn = ec[0].DeltaTicksPerQuarterNote;

                //long t1 = 

                // a delta time of 960 when the resolution is 1920 ticks per quarter note is after a 1/8 note rest




            }



            // Time is measured in “delta time” which is defined as the number of ticks (the resolution of which is
            // defined in the header) before the midi event is to be executed. I.e., a delta time of 0 =
            // immediately; a delta time of 960 when the resolution is 1920 ticks per quarter note is after a
            // 1/8 note rest. Delta time is a variable length format using 7 of the 8 available bits; the
            // maximum time value of any time byte is 127 (7FH). The first or 8th bit is used to identify the
            // last of the delta time bytes; the least significant byte is indicated by a leading bit=0, all other
            // bytes have a leading bit=1.

            //Track chunks(identifier = MTrk) contain a sequence of time - ordered events(MIDI and / or sequencer - specific data), 
            //each of which has a delta time value associated with it - ie the amount of time(specified in tickdiv units) since the 
            //previous event.

            //Output should be strings like this:
            ///// Constants /////
            // When to play.
            //const (START, 0);
            // Total length.
            //const (TLEN, XXXX);

            ///// Tracks and Loops /////
            //track(TRACK1, 1, 0);
            //loop(START, TLEN, SEQ1);

            //track(TRACK2, 2, 0);
            //loop(START, TLEN, SEQ2);

            //track(DRUMS, 10, 0);
            //loop(START, TLEN, SEQD);

            ///// Sequences and Notes /////
            //seq(SEQ1, TLEN);
            //note(0.00, F.4, 90, 0.08);
            //note(0.08, D#.4, 111, 0.08);
            //note(1.00, C.4, 90, 0.08);
            //note(1.08, B.4.m7, 90, 0.08);
            //note(2.00, F.5, 90, 0.08);
            //note(2.08, D#.5, 111, 0.08);
            //note(3.00, C.5, 90, 0.08);
            //note(3.08, B.5.m7, 90, 0.08);
            //note(4.00, F.3, 90, 0.08);
            //note(4.08, D#.3, 111, 0.08);
            //note(5.00, C.3, 90, 0.08);
            //note(5.08, B.3.m7, 90, 0.08);
            //note(6.00, F.2, 90, 0.08);
            //note(6.08, D#.2, 111, 0.08);
            //note(7.00, C.2, 90, 0.08);
            //note(7.08, B.2.m7, 90, 0.08);

            //seq(SEQ2, TLEN);
            // ....


        }

        /// <summary>
        /// Read the midi section of a style file.
        /// </summary>
        /// <param name="br"></param>
        static Dictionary<string, MidiEventCollection[]> ReadStyleMidiSection(BinaryReader br)
        {
            // Key is section name, value is one set of midi events per channel.
            Dictionary<string, MidiEventCollection[]> events = new Dictionary<string, MidiEventCollection[]>();
            MidiEventCollection[] currentEvents = null;
            List<string> leftovers = new List<string>();
            double tempo = 0.0;
            string timesig = "";
            string keysig = "";
            long absoluteTime = 0;
            long startPos = br.BaseStream.Position;

            // Current note on events, looking for corresponding note offs.
            NoteOnEvent[,] ons = new NoteOnEvent[Midi.NUM_MIDI_CHANNELS, Midi.MAX_MIDI_NOTE];

            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            int fileFormat = Utils.SwapUInt16(br.ReadUInt16());
            int tracks = Utils.SwapUInt16(br.ReadUInt16());
            int deltaTicksPerQuarterNote = Utils.SwapUInt16(br.ReadUInt16());

            // Style midi section is always type 0 - only one track.
            if (fileFormat != 0 || tracks != 1)
            {
                throw new FormatException("Invalid file format for style");
            }

            string chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
            if (chunkHeader != "MTrk")
            {
                throw new FormatException("Invalid chunk header");
            }

            chunkSize = Utils.SwapUInt32(br.ReadUInt32());

            // Read all midi events.
            MidiEvent me = null; // current

            while (br.BaseStream.Position < startPos + chunkSize)
            {
                me = MidiEvent.ReadNextEvent(br, me);
                absoluteTime += me.DeltaTime;
                me.AbsoluteTime = absoluteTime;

                switch (me.CommandCode)
                {
                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.Marker:
                        {
                            // Indicates start of a new midi part. Bin per channel.
                            string name = (me as TextEvent).Text;

                            currentEvents = new MidiEventCollection[Midi.NUM_MIDI_CHANNELS];
                            events[name] = currentEvents;
                            for (int i = 0; i < Midi.NUM_MIDI_CHANNELS; i++)
                            {
                                currentEvents[i] = new MidiEventCollection(0, deltaTicksPerQuarterNote);
                                currentEvents[i].AddTrack();
                            }

                            // Clean up the note tracking. TODO handle any ons without offs?
                            ons = new NoteOnEvent[Midi.NUM_MIDI_CHANNELS, Midi.MAX_MIDI_NOTE];

                            absoluteTime = 0;
                        }
                        break;

                    case MidiCommandCode.NoteOn:
                        {
                            // Save it while waiting for note off.
                            NoteOnEvent evt = me as NoteOnEvent;

                            if (evt.Velocity > 0)
                            {
                                ons[evt.Channel, evt.NoteNumber] = evt;
                                currentEvents?[evt.Channel][0].Add(evt);
                            }
                            else
                            {
                                // don't remove the note offs, even though they are annoying
                                // events[track].Remove(me);
                                ons[evt.Channel, evt.NoteNumber] = null;
                            }
                        }
                        break;

                    case MidiCommandCode.NoteOff:
                        {
                            NoteEvent evt = me as NoteEvent;
                            NoteOnEvent on = ons[evt.Channel, evt.NoteNumber];
                            if (on != null)
                            {
                                // We have a match. Diff the absolute time and convert to Time type. TODO
                                // note(4.00, F.3, 90, 0.08);

                                currentEvents?[evt.Channel][0].Add(evt);

                                // Reset it.
                                ons[evt.Channel, evt.NoteNumber] = null;
                            }
                        }
                        break;

                    case MidiCommandCode.ControlChange:
                        {
                            ControlChangeEvent evt = me as ControlChangeEvent;
                            currentEvents?[evt.Channel][0].Add(evt);
                        }
                        break;

                    case MidiCommandCode.PitchWheelChange:
                        {
                            PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                            currentEvents?[evt.Channel][0].Add(evt);
                        }
                        break;

                    case MidiCommandCode.PatchChange:
                        {
                            PatchChangeEvent evt = me as PatchChangeEvent;
                            leftovers.Add(evt.ToString());
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.SequenceTrackName:
                        {
                            // Indicates start of a new midi track.
                            leftovers.Add(me.ToString());
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.EndTrack:
                        {
                            // Indicates end of current midi track.
                            leftovers.Add(me.ToString());
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.SetTempo:
                        {
                            TempoEvent evt = me as TempoEvent;
                            tempo = evt.Tempo;
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.TimeSignature:
                        {
                            TimeSignatureEvent evt = me as TimeSignatureEvent;
                            timesig = evt.TimeSignature;
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.KeySignature:
                        {
                            KeySignatureEvent evt = me as KeySignatureEvent;
                            keysig = evt.ToString();
                        }
                        break;

                    default:
                        leftovers.Add(me.ToString());
                        break;
                }
            }

            if (br.BaseStream.Position != startPos + chunkSize)
            {
                throw new FormatException(String.Format("Read too far {0}+{1}!={2}", chunkSize, startPos, br.BaseStream.Position));
            }

            return events;




        }
    }
}
