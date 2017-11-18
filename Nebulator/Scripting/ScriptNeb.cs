using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using NLog;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Scripting
{
    /// <summary>
    /// Nebulator specific script stuff.
    /// </summary>
    public partial class Script
    {
        #region Fields


        #endregion

        #region Functions that can be overridden in the user script
        /// <summary>Called every Nebulator Tock.</summary>
        public virtual void step() { }
        #endregion

        #region User script properties
        /// <summary>Current Nebulator Tick.</summary>
        public int tick { get { return Globals.CurrentStepTime.Tick; } }

        /// <summary>Current Nebulator Tock.</summary>
        public int tock { get { return Globals.CurrentStepTime.Tock; } }

        /// <summary>Neb step clock is running.</summary>
        public bool playing { get { return Globals.Playing; } }

        /// <summary>Tock subdivision.</summary>
        public int tocksPerTick { get { return Globals.TOCKS_PER_TICK; } }

        /// <summary>Nebulator Speed in Ticks per minute (aka bpm).</summary>
        public float speed
        {
            get
            {
                ScriptEventArgs args = new ScriptEventArgs();
                ScriptEvent?.Invoke(this, args);
                return (float)args.Speed;
            }
            set
            {
                ScriptEvent?.Invoke(this, new ScriptEventArgs() { Speed = value });
            }
        }

        /// <summary>Nebulator master Volume.</summary>
        public int volume
        {
            get
            {
                ScriptEventArgs args = new ScriptEventArgs();
                ScriptEvent?.Invoke(this, args);
                return (int)args.Volume;
            }
            set
            {
                ScriptEvent?.Invoke(this, new ScriptEventArgs() { Volume = value });
            }
        }
        #endregion

        #region Script functions
        /// <summary>
        /// Send a midi note immediately. Respects solo/mute.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="snote">Note string using any form allowed in the script. TODO2 Requires double quotes in the script, would be nice to not.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation.</param>
        public void sendMidiNote(Track track, string snote, int vol, double dur)
        {
            Note note = new Note(snote);
            note.ChordNotes.ForEach(n => sendMidiNote(track, n, vol, dur));
        }

        /// <summary>
        /// Send a midi note immediately. Respects solo/mute. Adds a note off to play aftr dur time.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="inote">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation. If 0.0 doesn't add a subsequent NoteOff so the script can control it directly.</param>
        public void sendMidiNote(Track track, int inote, int vol, double dur)
        {
            bool _anySolo = Dynamic.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;

            bool play = track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo);

            if (play)
            {
                int vel = track.NextVol(vol);
                int notenum = Utils.Constrain(inote, 0, MidiInterface.MAX_MIDI_NOTE);

                if(vol > 0)
                {
                    StepNoteOn step = new StepNoteOn()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        NoteNumber = notenum,
                        NoteNumberToPlay = notenum,
                        Velocity = vel,
                        VelocityToPlay = vel,
                        Duration = new Time(dur)
                    };

                    step.Adjust(volume, track.Volume, track.Modulate);
                    Globals.MidiInterface.Send(step, dur > 0.0);
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

                    Globals.MidiInterface.Send(step);
                }
            }
        }

        /// <summary>
        /// Send a midi controller immediately.
        /// </summary>
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

            Globals.MidiInterface.Send(step);
        }

        /// <summary>
        /// Send a midi patch immediately.
        /// </summary>
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

            Globals.MidiInterface.Send(step);
        }

        /// <summary>
        /// Modulate all notes on the track by number of notes. Can be changed on the fly altering all subsequent notes.
        /// </summary>
        /// <param name="track">Track to alter notes on.</param>
        /// <param name="val">Number of notes, +-.</param>
        public void modulate(Track track, int val)
        {
            track.Modulate = val;
        }

        /// <summary>
        /// Send a sequence.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        /// <param name="tick">Which tick to start at. If -1 use current Tick.</param>
        public void playSequence(Track track, Sequence seq, int tick = -1)
        {
            ScriptSteps.Add(StepUtils.ConvertTrackToSteps(track, seq, tick));
        }
        #endregion
    }
}
