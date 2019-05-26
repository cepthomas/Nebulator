using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;

namespace Nebulator.Midi
{
    public class MidiUtils
    {
        public const int MAX_MIDI = 127;
        public const int MAX_CHANNELS = 16;
        public const int MAX_PITCH = 16383;

        /// <summary>
        /// Convert neb steps to midi file. TODO timing is still a bit wonky.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="midiFileName"></param>
        /// <param name="channels">Map of channel number to channel name.</param>
        /// <param name="secPerTick">Seconds per Tick (aka qtr note).</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public static void ExportMidi(StepCollection steps, string midiFileName, Dictionary<int, string> channels, double secPerTick, string info)
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
            foreach(int channel in channels.Keys)
            {
                IList<MidiEvent> le = events.AddTrack();
                trackEvents.Add(channel, le);
                le.Add(new TextEvent(channels[channel], MetaEventType.SequenceTrackName, 0));
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
                            evt = new NoteEvent(midiTime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOn,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                (int)(MathUtils.Constrain(stt.VelocityToPlay, 0, 1.0) * MidiUtils.MAX_MIDI));
                            break;

                        case StepNoteOff stt:
                            evt = new NoteEvent(midiTime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOff,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                (int)(MathUtils.Constrain(stt.Velocity, 0, 1.0) * MidiUtils.MAX_MIDI));
                            break;

                        case StepControllerChange stt:
                            if (stt.ControllerId == ScriptDefinitions.TheDefinitions.NoteControl)
                            {
                                // Shouldn't happen, ignore.
                            }
                            else if (stt.ControllerId == ScriptDefinitions.TheDefinitions.PitchControl)
                            {
                                evt = new PitchWheelChangeEvent(midiTime,
                                    stt.ChannelNumber,
                                    (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_MIDI));
                            }
                            else // CC
                            {
                                evt = new ControlChangeEvent(midiTime,
                                    stt.ChannelNumber,
                                    (MidiController)stt.ControllerId,
                                    (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_MIDI));
                            }
                            break;

                        case StepPatch stt:
                            evt = new PatchChangeEvent(midiTime,
                                stt.ChannelNumber,
                                stt.PatchNumber);
                            break;

                        default:
                            break;
                    }

                    if (evt != null)
                    {
                        trackEvents[step.ChannelNumber].Add(evt);
                    }
                }
            }

            // Finish up channels with end marker.
            foreach (IList<MidiEvent> let in trackEvents.Values)
            {
                long ltime = let.Last().AbsoluteTime;
                let.Add(new MetaEvent(MetaEventType.EndTrack, 0, ltime));
            }

            MidiFile.Export(midiFileName, events);
        }

        /// <summary>
        /// Read a midi or style file into text that can be placed in a neb file.
        /// It attempts to clean up any issues in the midi event data e.g. note on/off mismatches.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Collection of strings for pasting into a neb file.</returns>
        public static List<string> ImportFile(string fileName)
        {
            List<string> defs = new List<string>();
            List<string> sequences = new List<string>() { "///// Sequences /////" };
            List<string> leftovers = new List<string>();

            FileParser fpars = new FileParser();
            fpars.ProcessFile(fileName);

            defs.Add("///// Channel definitions /////");
            fpars.Channels.ForEach(ch => defs.Add($"NChannel {MakeChanName(ch)};"));
            defs.Add("");

            defs.Add("///// Sequence definitions /////");
            fpars.Parts.ForEach(pt =>
            {
                fpars.Channels.ForEach(ch => defs.Add($"NSequence {MakeSeqName(pt, ch)};"));
            });
            defs.Add("");

            #region Local functions
            Time MidiTimeToInternal(long mtime, int tpqn)
            {
                //return new Time(mtime / tpqn);
                return new Time(mtime * Time.TOCKS_PER_TICK / tpqn);
            }

            string MakeSeqName(string part, int channel)
            {
                return $"{part.Replace(" ", "_")}_CH{channel}";
            }

            string MakeChanName(int channel)
            {
                return $"CH{channel}";
            }
            #endregion

            // Collect sequence info.
            foreach (var part in fpars.Parts)
            {
                foreach (int channel in fpars.Channels)
                {
                    var events = fpars.GetEvents(part, channel);

                    if (events != null)
                    {
                        string seqName = MakeSeqName(part, channel);

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

                        // Process the collected valid events.
                        if (validEvents.Count > 0)
                        {
                            validEvents.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
                            long duration = validEvents.Last().AbsoluteTime; // - validEvents.First().AbsoluteTime;
                            Time tdur = MidiTimeToInternal(duration, fpars.DeltaTicksPerQuarterNote);
                            tdur.RoundUp();
                            sequences.Add($"{seqName} = CreateSequence({tdur.Tick}); // !!! fix this number !!!");

                            // Process each set of notes at each discrete play time.
                            foreach (IEnumerable<NoteOnEvent> nevts in validEvents.GroupBy(e => e.AbsoluteTime))
                            {
                                List<int> notes = new List<int>(nevts.Select(n => n.NoteNumber));
                                //notes.Sort();

                                NoteOnEvent noevt = nevts.ElementAt(0);
                                Time when = MidiTimeToInternal(noevt.AbsoluteTime, fpars.DeltaTicksPerQuarterNote);
                                Time dur = MidiTimeToInternal(noevt.NoteLength, fpars.DeltaTicksPerQuarterNote);

                                if (channel == 10)
                                {
                                    // Drums - one line per hit.
                                    foreach(int d in notes)
                                    {
                                        string sdrum = NoteUtils.FormatDrum(d);
                                        sequences.Add($"{seqName}.Add({when}, {sdrum}, {noevt.Velocity});");
                                    }
                                }
                                else
                                {
                                    // Instrument - note(s) or chord.
                                    foreach(string sn in NoteUtils.FormatNotes(notes))
                                    {
                                        sequences.Add($"{seqName}.Add({when}, {sn}, {noevt.Velocity}, {dur});");
                                    }
                                }
                            }
                            sequences.Add(""); // some space
                        }
                    }
                    // else not a valid combination - ignore
                }
            }

            List<string> all = new List<string>() { "///// Imported Style /////" };
            all.Add("");

            all.AddRange(defs);
            all.AddRange(sequences);

            return all;
        }
    }
}

