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
        /// Convert neb steps to midi file.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="midiFileName"></param>
        /// <param name="channels">Map of channel number to channel name.</param>
        /// <param name="bpm">Beats per minute.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public static void ExportMidi(StepCollection steps, string midiFileName, Dictionary<int, string> channels, double bpm, string info)
        {
            int exportPpq = 96;

            // Events per track.
            Dictionary<int, IList<MidiEvent>> trackEvents = new Dictionary<int, IList<MidiEvent>>();

            ///// Meta file stuff.
            MidiEventCollection events = new MidiEventCollection(1, exportPpq);

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

            // Tempo.
            lhdr.Add(new TempoEvent(0, 0) { Tempo = bpm });

            // General info.
            lhdr.Add(new TextEvent("Midi file created by Nebulator.", MetaEventType.TextEvent, 0));
            lhdr.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            lhdr.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

            ///// Make one midi event collection per track.
            foreach (int channel in channels.Keys)
            {
                IList<MidiEvent> le = events.AddTrack();
                trackEvents.Add(channel, le);
                le.Add(new TextEvent(channels[channel], MetaEventType.SequenceTrackName, 0));
                // >> 0 SequenceTrackName G.MIDI Acou Bass
            }

            // Make a transformer.
            MidiTime mt = new MidiTime()
            {
                InternalPpq = Time.SUBDIVS_PER_BEAT,
                MidiPpq = exportPpq,
                Tempo = bpm
            };

            // Run through the main steps and create a midi event per.
            foreach (Time time in steps.Times)
            {
                long mtime = mt.InternalToMidi(time.TotalSubdivs);

                foreach (Step step in steps.GetSteps(time))
                {
                    MidiEvent evt = null;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            evt = new NoteEvent(mtime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOn,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                (int)(MathUtils.Constrain(stt.VelocityToPlay, 0, 1.0) * MidiUtils.MAX_MIDI));
                            trackEvents[step.ChannelNumber].Add(evt);

                            if (stt.Duration.TotalSubdivs > 0) // specific duration
                            {
                                evt = new NoteEvent(mtime + mt.InternalToMidi(stt.Duration.TotalSubdivs),
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOff,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                    0);
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            break;

                        case StepNoteOff stt:
                            evt = new NoteEvent(mtime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOff,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                0);
                            trackEvents[step.ChannelNumber].Add(evt);
                            break;

                        case StepControllerChange stt:
                            if (stt.ControllerId == ScriptDefinitions.TheDefinitions.NoteControl)
                            {
                                // Shouldn't happen, ignore.
                            }
                            else if (stt.ControllerId == ScriptDefinitions.TheDefinitions.PitchControl)
                            {
                                evt = new PitchWheelChangeEvent(mtime,
                                    stt.ChannelNumber,
                                    (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_MIDI));
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            else // CC
                            {
                                evt = new ControlChangeEvent(mtime,
                                    stt.ChannelNumber,
                                    (MidiController)stt.ControllerId,
                                    (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_MIDI));
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            break;

                        case StepPatch stt:
                            evt = new PatchChangeEvent(mtime,
                                stt.ChannelNumber,
                                stt.PatchNumber);
                            trackEvents[step.ChannelNumber].Add(evt);
                            break;

                        default:
                            break;
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
        /// Read a midi file into text that can be placed in a neb file.
        /// It attempts to clean up any issues in the midi event data e.g. note on/off mismatches.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Collection of strings for pasting into a neb file.</returns>
        public static List<string> ImportFile(string fileName)
        {
            FileParser fpars = new FileParser();
            fpars.ProcessFile(fileName);

            List<string> defs = new List<string>
            {
                $"Tempo:{fpars.Tempo}",
                $"TimeSig:{fpars.TimeSig}",
                $"DeltaTicksPerQuarterNote:{fpars.DeltaTicksPerQuarterNote}",
                $"KeySig:{fpars.KeySig}"
            };

            foreach (KeyValuePair<int, string> kv in fpars.Channels)
            {
                int chnum = kv.Key;
                string chname = kv.Value;

                defs.Add("");
                defs.Add($"================================================================================");
                defs.Add($"====== Channel {chnum} {chname} ");
                defs.Add($"================================================================================");

                // Current note on events that are waiting for corresponding note offs.
                LinkedList<NoteOnEvent> ons = new LinkedList<NoteOnEvent>();

                // Collected and processed events.
                List<NoteOnEvent> validEvents = new List<NoteOnEvent>();

                // Make a transformer.
                MidiTime mt = new MidiTime()
                {
                    InternalPpq = Time.SUBDIVS_PER_BEAT,
                    MidiPpq = fpars.DeltaTicksPerQuarterNote,
                    Tempo = fpars.Tempo
                };

                foreach (MidiEvent evt in fpars.GetEvents(chnum))
                {
                    // Run through each group of events
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
                                        fpars.Leftovers.Add($"NoteOff: NoteOnEvent with vel=0: {onevt}");
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
                                        fpars.Leftovers.Add($"NoteOff: NoteEvent in part {nevt}");
                                    }
                                }
                                // else ignore.
                            }
                            break;
                    }

                    // Check for note tracking leftovers.
                    foreach (NoteOnEvent on in ons)
                    {
                        if (on != null)
                        {
                            Time when = new Time(mt.MidiToInternal(on.AbsoluteTime));
                            // ? fpars.Leftovers.Add($"Orphan NoteOn: {when} {on.Channel} {on.NoteNumber}");
                        }
                    }

                    // Process the collected valid events.
                    if (validEvents.Count > 0)
                    {
                        validEvents.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
                        long duration = validEvents.Last().AbsoluteTime; // - validEvents.First().AbsoluteTime;
                        Time tdur = new Time(mt.MidiToInternal(duration));
                        tdur.RoundUp();

                        // Process each set of notes at each discrete play time.
                        foreach (IEnumerable<NoteOnEvent> nevts in validEvents.GroupBy(e => e.AbsoluteTime))
                        {
                            List<int> notes = new List<int>(nevts.Select(n => n.NoteNumber));
                            //notes.Sort();

                            NoteOnEvent noevt = nevts.ElementAt(0);
                            Time when = new Time(mt.MidiToInternal(noevt.AbsoluteTime));
                            Time dur = new Time(mt.MidiToInternal(noevt.NoteLength));
                            double vel = (double)noevt.Velocity / MAX_MIDI;

                            if (chnum == 10)
                            {
                                // Drums - one line per hit.
                                foreach (int d in notes)
                                {
                                    string sdrum = NoteUtils.FormatDrum(d);
                                    defs.Add($"{{ {when}, {sdrum}, {vel:0.00} }},");
                                }
                            }
                            else
                            {
                                // Instrument - note(s) or chord.
                                foreach (string sn in NoteUtils.FormatNotes(notes))
                                {
                                    defs.Add($"{{ {when}, {sn}, {vel:0.00}, {dur} }},");
                                }
                            }
                        }

                        validEvents.Clear();
                    }
                }
            }

            defs.Add("");

            List<string> all = new List<string>();

            all.AddRange(defs);

            if(fpars.Leftovers.Count > 0)
            {
                all.Add($"");
                all.Add($"================================================================================");
                all.Add($"====== Leftovers ");
                all.Add($"================================================================================");

                all.AddRange(fpars.Leftovers);
            }
            return all;
        }
    }
}

