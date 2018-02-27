using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.Dynamic;

// Nebulator API stuff.

namespace Nebulator.Script
{
    public partial class ScriptCore
    {
        #region Functions that can be overridden in the user script
        /// <summary>Called every Nebulator Tock.</summary>
        public virtual void step() { }
        #endregion

        #region User script properties
        /// <summary>Sound is playing.</summary>
        public bool playing { get { return Context.Playing; } }

        /// <summary>Current Nebulator step time.</summary>
        public Time stepTime { get { return Context.StepTime; } }

        /// <summary>Current Nebulator Tick.</summary>
        public int tick { get { return Context.StepTime.Tick; } }

        /// <summary>Current Nebulator Tock.</summary>
        public int tock { get { return Context.StepTime.Tock; } }

        /// <summary>Actual time since start pressed.</summary>
        public float now { get { return Context.RealTime; } }

        /// <summary>Tock subdivision.</summary>
        public int tocksPerTick { get { return Time.TOCKS_PER_TICK; } }

        /// <summary>Nebulator Speed in Ticks per minute (aka bpm).</summary>
        public float speed { get { return Context.Speed; } set { Context.Speed = value; } }

        /// <summary>Nebulator master Volume.</summary>
        public int volume { get { return Context.Volume; } set { Context.Volume = value; } }

        /// <summary>Indicates using internal synth.</summary>
        public bool winGm { get { return UserSettings.TheSettings.MidiOut == "Microsoft GS Wavetable Synth"; } }
        #endregion

        #region Script callable functions
        /// <summary>Send a midi note immediately. Respects solo/mute. Adds a note off to play after dur time.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="inote">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated. User has to turn it off explicitly.</param>
        public void sendMidiNote(Track track, int inote, int vol, Time dur)
        {
            bool _anySolo = ScriptEntities.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;

            bool play = track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo);

            if (play)
            {
                int vel = track.NextVol(vol);
                int notenum = Utils.Constrain(inote, 0, MidiInterface.MAX_MIDI_NOTE);

                if (vol > 0)
                {
                    StepNoteOn step = new StepNoteOn()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        NoteNumber = notenum,
                        NoteNumberToPlay = notenum,
                        Velocity = vel,
                        VelocityToPlay = vel,
                        Duration = dur
                    };

                    step.Adjust(volume, track.Volume, track.Modulate);
                    MidiInterface.TheInterface.Send(step);//, dur.TotalTocks > 0);
                }
                else
                {
                    StepNoteOff step = new StepNoteOff()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        NoteNumber = notenum,
                        NoteNumberToPlay = notenum
                    };

                    MidiInterface.TheInterface.Send(step);
                }
            }
        }

        /// <summary>Send a midi note immediately. Respects solo/mute.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="snote">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        public void sendMidiNote(Track track, string snote, int vol, Time dur)
        {
            SequenceElement note = new SequenceElement(snote);

            if (note.Notes.Count == 0)
            {
                _logger.Warn($"Invalid note:{snote}");
            }
            else
            {
                note.Notes.ForEach(n => sendMidiNote(track, n, vol, dur));
            }
        }

        /// <summary>Send a midi note immediately. Respects solo/mute.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="snote">Note string using any form allowed in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation. 0 means no note off generated.</param>
        public void sendMidiNote(Track track, string snote, int vol, double dur)
        {
            SequenceElement note = new SequenceElement(snote);

            if (note.Notes.Count == 0)
            {
                _logger.Warn($"Invalid note:{snote}");
            }
            else
            {
                note.Notes.ForEach(n => sendMidiNote(track, n, vol, new Time(dur)));
            }
        }

        /// <summary>Send a midi note immediately. Respects solo/mute. Adds a note off to play after dur time.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="inote">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation. 0 means no note off generated.</param>
        public void sendMidiNote(Track track, int inote, int vol, double dur)
        {
            sendMidiNote(track, inote, vol, new Time(dur));
        }

        /// <summary>Send a midi controller immediately.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="ctlnum">Controller number.</param>
        /// <param name="val">Controller value.</param>
        public void sendMidiController(Track track, int ctlnum, int val)
        {
            StepControllerChange step = new StepControllerChange()
            {
                TrackName = track.Name,
                Channel = track.Channel,
                MidiController = ctlnum,
                ControllerValue = val
            };

            MidiInterface.TheInterface.Send(step);
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="track"></param>
        /// <param name="patch"></param>
        public void sendPatch(Track track, int patch)
        {
            StepPatch step = new StepPatch()
            {
                TrackName = track.Name,
                Channel = track.Channel,
                PatchNumber = patch
            };

            MidiInterface.TheInterface.Send(step);
        }

        /// <summary>Modulate all notes on the track by number of notes. Can be changed on the fly altering all subsequent notes.</summary>
        /// <param name="track">Track to alter notes on.</param>
        /// <param name="val">Number of notes, +-.</param>
        public void modulate(Track track, int val)
        {
            track.Modulate = val;
        }

        /// <summary>Send a named sequence.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        public void playSequence(Track track, Sequence seq)
        {
            StepCollection scoll = ConvertToSteps(track, seq, Context.StepTime.Tick);
            Context.RuntimeSteps.Add(scoll);
        }

        /// <summary>Convert the argument into numbered notes.</summary>
        /// <param name="note">Note string using any form allowed in the script.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        public int[] getNotes(string note)
        {
            List<int> notes = NoteUtils.ParseNoteString(note, ScriptEntities.NoteDefs);
            return notes != null ? notes.ToArray() : new int[0];
        }

        /// <summary>Get an array of scale notes.</summary>
        /// <param name="scale">One of the named scales from ScriptDefinitions.md.</param>
        /// <param name="key">Note name and octave.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        public int[] getScaleNotes(string scale, string key)
        {
            List<int> notes = NoteUtils.GetScaleNotes(scale, key, ScriptEntities.NoteDefs);
            return notes != null ? notes.ToArray() : new int[0];
        }
        #endregion
    }
}
