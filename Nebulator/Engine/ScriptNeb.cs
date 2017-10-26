using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using NLog;
using Nebulator.Common;
using Nebulator.Model;
using Nebulator.Midi;


namespace Nebulator.Engine
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

        /// <summary>Current Nebulator Speed in seconds per tick. Can be used to calculate real times.</summary>
        public float speed { get { return (float)Globals.CurrentPersisted.Speed; } }
        #endregion

        #region Script functions
        /// <summary>
        /// Send a midi note immediately. Respects solo/mute.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="snote">Note string using any form allowed in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation.</param>
        public void sendMidiNote(Track track, string snote, int vol, double dur)
        {
            Note note = new Note(snote);
            note.NoteNumbers.ForEach(n => sendMidiNote(track, n, vol, dur));
        }

        /// <summary>
        /// Send a midi note immediately. Respects solo/mute.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="inote">Note number.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Tick.Tock representation.</param>
        public void sendMidiNote(Track track, int inote, int vol, double dur)
        {
            bool _anySolo = Dynamic.Tracks.Values.Where(t => t.State == TrackState.Solo).Count() > 0;

            bool play = track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo);
            if (play)
            {
                StepNoteOn step = new StepNoteOn()
                {
                    Tag = track,
                    Channel = track.Channel,
                    NoteNumber = Utils.Constrain(inote, 0, MidiInterface.MAX_MIDI_NOTE),
                    NoteNumberToPlay = Utils.Constrain(inote, 0, MidiInterface.MAX_MIDI_NOTE),
                    Velocity = vol,
                    VelocityToPlay = vol,
                    Duration = new Time(dur)
                };

                step.Adjust(track.Volume, track.Modulate);
                Globals.MidiInterface.Send(step, true);
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
                Tag = track,
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
                Tag = track,
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
        #endregion
    }
}
