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
        /// <summary>
        /// Turn collected stuff into midi event sequence.
        /// </summary>
        /// <param name="tracks">The Tracks.</param>
        /// <param name="sequences">The Sequences.</param>
        /// <returns></returns>
        public static StepCollection ConvertTracksToSteps(IEnumerable<Track> tracks, IEnumerable<Sequence> sequences)
        {
            StepCollection steps = new StepCollection();

            // Gather the sequence definitions.
            Dictionary<string, Sequence> seqDefs= sequences.Distinct().ToDictionary(i => i.Name, i => i);

            // Process the composition values.
            foreach (Track track in tracks)
            {
                foreach (Loop loop in track.Loops)
                {
                    // Get the loop sequence info.
                    Sequence seq = seqDefs[loop.SequenceName];

                    for (int loopTick = loop.StartTick; loopTick < loop.EndTick; loopTick += seq.Length)
                    {
                        StepCollection stepsToAdd = ConvertTrackToSteps(track, seq, loopTick);
                        steps.Add(stepsToAdd);
                    }
                }
            }

            return steps;
        }

        /// <summary>
        /// Generate a sequence.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        /// <param name="tick">Which tick to start at. If -1 use current Tick.</param>
        public static StepCollection ConvertTrackToSteps(Track track, Sequence seq, int tick = -1)
        {
            StepCollection steps = new StepCollection();

            foreach (Note note in seq.Notes)
            {
                // Create the note start and stop times.
                int toffset = tick == 0 ? 0 : track.NextTime();

                Time startNoteTime = new Time(tick == -1 ? Globals.CurrentStepTime.Tick : tick, toffset) + note.When;
                Time stopNoteTime = startNoteTime + note.Duration;

                // Process all note numbers.
                foreach (int noteNum in note.ChordNotes)
                {
                    ///// Note on.
                    int vel = track.NextVol(note.Volume);
                    StepNoteOn step = new StepNoteOn()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        NoteNumber = noteNum,
                        NoteNumberToPlay = noteNum,
                        Velocity = vel,
                        VelocityToPlay = vel,
                        Duration = note.Duration
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
            return steps;
        }
    }
}
