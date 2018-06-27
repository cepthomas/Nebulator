using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using Nebulator.Common;
using Nebulator.Protocol;

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
            // Events per track.
            Dictionary<int, IList<MidiEvent>> trackEvents = new Dictionary<int, IList<MidiEvent>>();
            int deltaTicksPerQuarterNote = 96; // fixed output value

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

                    double d = (time.Tick + time.Tock / Time.TOCKS_PER_TICK) * secPerTick * deltaTicksPerQuarterNote;
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
                            switch (stt.ControllerType)
                            {
                                case ControllerTypes.Normal:
                                    evt = new ControlChangeEvent(midiTime, stt.Channel, (MidiController)stt.ControllerId, stt.Value);
                                    break;

                                case ControllerTypes.Pitch:
                                    evt = new PitchWheelChangeEvent(midiTime, stt.Channel, stt.Value);
                                    break;

                                case ControllerTypes.Note:
                                    break;
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

            // Finish up tracks with end marker.
            foreach (IList<MidiEvent> let in trackEvents.Values)
            {
                long ltime = let.Last().AbsoluteTime;
                let.Add(new MetaEvent(MetaEventType.EndTrack, 0, ltime));
            }

            MidiFile.Export(midiFileName, events);
        }

        /// <summary>
        /// Read a style file into text that can be placed in a neb file.
        /// It attempts to clean up any issues in the midi event data e.g. note on/off mismatches.
        /// Returns the list of strings and also places them in the clipboard.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Collection of strings for pasting into a file.</returns>
        public static List<string> ImportStyle(string fileName)
        {
            List<string> constants = new List<string>() { "///// Constants /////" };
            List<string> tracks = new List<string>() { "///// Tracks and Loops /////" };
            List<string> sequences = new List<string>() { "///// Sequences and Notes /////" };
            List<string> leftovers = new List<string> { "///// Leftovers /////" };

            StyleParser sty = new StyleParser();
            sty.ProcessFile(fileName);

            // Process collected events into strings digestible by neb.
            List<string> parts = sty.Parts;
            List<int> channels = sty.Channels;

            // Collect sequence info.
            foreach(var part in parts)
            {
                foreach (int channel in channels)
                {
                    var events = sty.GetEvents(part, channel);

                    if(events != null)
                    {
                        // Current note on events that are waiting for corresponding note offs.
                        LinkedList<NoteOnEvent> ons = new LinkedList<NoteOnEvent>();

                        // Collected and processed events.
                        List<NoteOnEvent> validEvents = new List<NoteOnEvent>();

                        // Run through each group of events
                        foreach (var evt in events)
                        {
                            switch (evt)
                            {
                                case NoteOnEvent onevt:
                                    {
                                        if (onevt.OffEvent != null)
                                        {
                                            // Self contained - just save it.
                                            validEvents.Add(onevt);
                                            // Reset it.
                                            ons.AddLast(onevt);
                                        }
                                        else if (onevt.Velocity == 0)
                                        {
                                            // It's actually a note off - handle as such. Locate the initiating note on.

                                            var on = ons.First(o => o.NoteNumber == onevt.NoteNumber);
                                            if (on != null)
                                            {
                                                // Found it.
                                                on.OffEvent = new NoteEvent(onevt.AbsoluteTime, onevt.Channel, MidiCommandCode.NoteOff, onevt.NoteNumber, 0);
                                                validEvents.Add(on);
                                                ons.Remove(on); // reset
                                            }
                                            else
                                            {
                                                // hmmm...
                                                leftovers.Add($"NoteOff: NoteOnEvent with vel=0 in part {part}:{onevt}");
                                            }
                                        }
                                        else
                                        {
                                            // True note on - save it until note off shows up.
                                            ons.AddLast(onevt);
                                        }
                                    }
                                    break;

                                case NoteEvent nevt:
                                    {
                                        if (nevt.CommandCode == MidiCommandCode.NoteOff || nevt.Velocity == 0)
                                        {
                                            // It's actually a note off - handle as such. Locate the initiating note on.
                                            var on = ons.First(o => o.NoteNumber == nevt.NoteNumber);
                                            if (on != null)
                                            {
                                                // Found it.
                                                on.OffEvent = new NoteEvent(nevt.AbsoluteTime, nevt.Channel, MidiCommandCode.NoteOff, nevt.NoteNumber, 0);
                                                validEvents.Add(on);
                                                ons.Remove(on); // reset
                                            }
                                            else
                                            {
                                                // hmmm... see below
                                                leftovers.Add($"NoteOff: NoteEvent in part {part}:{nevt}");
                                            }
                                        }
                                        // else ignore.
                                    }
                                    break;
                            }
                        }

                        // Check for note tracking leftovers. Error?
                        foreach (NoteOnEvent on in ons)
                        {
                            if(on != null)
                            {
                                leftovers.Add($"Leftover NoteOnEvent in part {part}:{on}");
                            }
                        }

                        Time MidiTimeToInternal(long mtime, int tpqn)
                        {
                            //return new Time(mtime / tpqn);
                            return new Time(mtime * Time.TOCKS_PER_TICK / tpqn);
                        }

                        // Process the collected valid events.
                        if (validEvents.Count > 0)
                        {
                            ///// Sequences and Notes /////
                            //seq(SEQ1, 8);
                            //note(0.00, F.4.m7, 90, 0.08);
                            //note(1.21, C.4, 90, 0.08);
                            // ....

                            validEvents.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
                            long duration = validEvents.Last().AbsoluteTime - validEvents.First().AbsoluteTime;
                            Time tdur = MidiTimeToInternal(duration, sty.DeltaTicksPerQuarterNote);
                            tdur.RoundUp();
                            sequences.Add($"seq({part.Replace(" ", "_")}_{channel}, {tdur.Tick});");

                            // Process each set of notes at each discrete play time.
                            foreach (IEnumerable<NoteOnEvent> nevts in validEvents.GroupBy(e => e.AbsoluteTime))
                            {
                                List<int> notes = new List<int>(nevts.Select(n => n.NoteNumber));
                                //notes.Sort();

                                NoteOnEvent noevt = nevts.ElementAt(0);
                                Time when = MidiTimeToInternal(noevt.AbsoluteTime, sty.DeltaTicksPerQuarterNote);
                                Time dur = MidiTimeToInternal(noevt.NoteLength, sty.DeltaTicksPerQuarterNote);

                                if (channel == 10)
                                {
                                    // Drums - one line per hit.
                                    foreach(int d in notes)
                                    {
                                        string sdrum = NoteUtils.FormatDrum(d);
                                        sequences.Add($"note({when}, {sdrum}, {noevt.Velocity}, {dur});");
                                    }
                                }
                                else
                                {
                                    // Instrument - note(s) or chord.
                                    foreach(string sn in NoteUtils.FormatNotes(notes))
                                    {
                                        sequences.Add($"note({when}, {sn}, {noevt.Velocity}, {dur});");
                                    }
                                }
                            }
                            sequences.Add(""); // some space
                        }
                    }
                    // else not a valid combination - ignore
                }
            }

            // Process track info.
            channels.ForEach(c => tracks.Add($"track(TRACK_{c}, {c}, 0, 0, 0);"));

            // Global stuff.
            constants.Add($"const(TLEN, 888);");

            List<string> all = new List<string>() { "///// Imported Style /////" };
            all.AddRange(constants);
            all.Add(""); // space
            all.AddRange(tracks);
            all.Add(""); // space
            all.AddRange(sequences);

            System.Windows.Forms.Clipboard.SetText(string.Join(Environment.NewLine, all));

            return all;
        }
    }
}
