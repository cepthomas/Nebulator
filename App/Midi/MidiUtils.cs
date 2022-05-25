using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using NBagOfTricks;
using Nebulator.Common;
using System.ComponentModel;

namespace Nebulator.Midi
{
    public class MidiUtils
    {
        /// <summary>Standard midi.</summary>
        public const int MAX_PITCH = 16383;

        /// <summary>
        /// Convert neb steps to midi file.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="midiFileName"></param>
        /// <param name="channels">Map of channel number to channel name.</param>
        /// <param name="bpm">Beats per minute.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public static void ExportToMidi(StepCollection steps, string midiFileName, Dictionary<int, string> channels, double bpm, string info)
        {
            int exportPpq = 96;

            // Events per track.
            Dictionary<int, IList<MidiEvent>> trackEvents = new();

            ///// Meta file stuff.
            MidiEventCollection events = new(1, exportPpq);

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
            MidiLib.MidiTime mt = new()
            {
                InternalPpq = Time.SubdivsPerBeat,
                MidiPpq = exportPpq,
                Tempo = bpm
            };

            // Run through the main steps and create a midi event per.
            foreach (Time time in steps.Times)
            {
                long mtime = mt.InternalToMidi(time.TotalSubdivs);

                foreach (Step step in steps.GetSteps(time))
                {
                    MidiEvent evt;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            evt = new NoteEvent(mtime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOn,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                                (int)(MathUtils.Constrain(stt.VelocityToPlay, 0, 1.0) * Definitions.MAX_MIDI));
                            trackEvents[step.ChannelNumber].Add(evt);

                            if (stt.Duration.TotalSubdivs > 0) // specific duration
                            {
                                evt = new NoteEvent(mtime + mt.InternalToMidi(stt.Duration.TotalSubdivs),
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOff,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                                    0);
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            break;

                        case StepNoteOff stt:
                            evt = new NoteEvent(mtime,
                                stt.ChannelNumber,
                                MidiCommandCode.NoteOff,
                                (int)MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                                0);
                            trackEvents[step.ChannelNumber].Add(evt);
                            break;

                        case StepControllerChange stt:
                            if (stt.ControllerId == ControllerDef.NoteControl)
                            {
                                // Shouldn't happen, ignore.
                            }
                            else if (stt.ControllerId == ControllerDef.PitchControl)
                            {
                                evt = new PitchWheelChangeEvent(mtime,
                                    stt.ChannelNumber,
                                    (int)MathUtils.Constrain(stt.Value, 0, Definitions.MAX_MIDI));
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            else // CC
                            {
                                evt = new ControlChangeEvent(mtime,
                                    stt.ChannelNumber,
                                    (MidiController)stt.ControllerId,
                                    (int)MathUtils.Constrain(stt.Value, 0, Definitions.MAX_MIDI));
                                trackEvents[step.ChannelNumber].Add(evt);
                            }
                            break;

                        case StepPatch stt:
                            evt = new PatchChangeEvent(mtime,
                                stt.ChannelNumber,
                                (int)stt.Patch);
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
    }
}

