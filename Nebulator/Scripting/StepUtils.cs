using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Scripting
{
    public class StepUtils
    {
        ///// <summary>
        ///// Turn collected stuff into midi event sequence.
        ///// </summary>
        ///// <param name="tracks">The Tracks.</param>
        ///// <param name="sequences">The Sequences.</param>
        ///// <returns></returns>
        public static StepCollection ConvertToSteps(Dynamic dynamic)
        {
            StepCollection steps = new StepCollection();

            // Iterate through the sections.
            foreach (Section sect in dynamic.Sections.Values)
            {
                // Iterate through the sections tracks.
                foreach (SectionTrack strack in sect.SectionTracks)
                {
                    // Get the pertinent Track object.
                    Track track = dynamic.Tracks[strack.TrackName];

                    // For processing current Sequence.
                    int seqOffset = sect.Start;

                    // Gen steps for each sequence.
                    foreach (string sseq in strack.SequenceNames)
                    {
                        Sequence seq = dynamic.Sequences[sseq];
                        StepCollection stepsToAdd = ConvertToSteps(track, seq, seqOffset);
                        steps.Add(stepsToAdd);
                        seqOffset += seq.Length;
                    }
                }
            }

            return steps;
        }

        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="tick">Which tick to start at. If -1 use current Tick.</param>
        public static StepCollection ConvertToSteps(Track track, Sequence seq, int tick = -1)
        {
            StepCollection steps = new StepCollection();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = tick == -1 ? 0 : track.NextTime();

                Time startNoteTime = new Time(tick == -1 ? Globals.StepTime.Tick : tick, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + seqel.Duration;

                if(seqel.Function != "")
                {
                    StepSpecial step = new StepSpecial()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        Function = seqel.Function
                    };
                    steps.AddStep(startNoteTime, step);
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        int vel = track.NextVol(seqel.Volume);
                        StepNoteOn step = new StepNoteOn()
                        {
                            TrackName = track.Name,
                            Channel = track.Channel,
                            NoteNumber = noteNum,
                            NoteNumberToPlay = noteNum,
                            Velocity = vel,
                            VelocityToPlay = vel,
                            Duration = seqel.Duration
                        };
                        steps.AddStep(startNoteTime, step);

                        // Maybe add a deferred stop note.
                        if (stopNoteTime != startNoteTime)
                        {
                            steps.AddStep(stopNoteTime, new StepNoteOff(step));
                        }
                        // else client is taking care of it.
                    }
                }
            }

            return steps;
        }
    }
}
