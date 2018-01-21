using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// All the script stuff we might want at runtime.
    /// </summary>
    public class ScriptEntities
    {
        /// <summary>Declared variables.</summary>
        public static LazyCollection<Variable> Vars { get; set; } = new LazyCollection<Variable>();

        /// <summary>Midi inputs.</summary>
        public static LazyCollection<MidiControlPoint> InputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Midi outputs.</summary>
        public static LazyCollection<MidiControlPoint> OutputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Levers.</summary>
        public static LazyCollection<LeverControlPoint> Levers { get; set; } = new LazyCollection<LeverControlPoint>();

        /// <summary>All sections.</summary>
        public static LazyCollection<Section> Sections { get; set; } = new LazyCollection<Section>();

        /// <summary>All tracks.</summary>
        public static LazyCollection<Track> Tracks { get; set; } = new LazyCollection<Track>();

        /// <summary>All sequences.</summary>
        public static LazyCollection<Sequence> Sequences { get; set; } = new LazyCollection<Sequence>();

        /// <summary>Don't even try to do this.</summary>
        ScriptEntities() { }

        /// <summary>Reset everything.</summary>
        public static void Clear()
        {
            Vars.Clear();
            InputMidis.Clear();
            OutputMidis.Clear();
            Levers.Clear();
            Sections.Clear();
            Tracks.Clear();
            Sequences.Clear();
        }
        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startTick">Which tick to start at.</param>
        public static StepCollection ConvertToSteps(Track track, Sequence seq, int startTick) // TODO2 Needs better home than here.
        {
            StepCollection steps = new StepCollection();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = startTick == -1 ? 0 : track.NextTime();

                //Time startNoteTime = new Time(tick == -1 ? Script.StepTime.Tick : tick, toffset) + seqel.When;
                Time startNoteTime = new Time(startTick, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + seqel.Duration;

                if (seqel.Function != "")
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
