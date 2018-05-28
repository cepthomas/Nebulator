using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Midi;

// Nebulator API stuff.

namespace Nebulator.Script
{
    public partial class ScriptCore
    {
        #region User script properties
        /// <summary>Sound is playing.</summary>
        public bool playing { get { return RuntimeContext.Playing; } }

        /// <summary>Current Nebulator step time.</summary>
        public Time stepTime { get { return RuntimeContext.StepTime; } }

        /// <summary>Current Nebulator Tick.</summary>
        public int tick { get { return RuntimeContext.StepTime.Tick; } }

        /// <summary>Current Nebulator Tock.</summary>
        public int tock { get { return RuntimeContext.StepTime.Tock; } }

        /// <summary>Actual time since start pressed.</summary>
        public float now { get { return RuntimeContext.RealTime; } }

        /// <summary>Tock subdivision.</summary>
        public int tocksPerTick { get { return Time.TOCKS_PER_TICK; } }

        /// <summary>Nebulator Speed in Ticks per minute (aka bpm).</summary>
        public float speed { get { return RuntimeContext.Speed; } set { RuntimeContext.Speed = value; } }

        /// <summary>Nebulator master Volume.</summary>
        public int volume { get { return RuntimeContext.Volume; } set { RuntimeContext.Volume = value; } }

        /// <summary>Indicates using internal synth.</summary>
        public bool winGm { get { return UserSettings.TheSettings.MidiOut == "Microsoft GS Wavetable Synth"; } }
        #endregion

        #region Functions that can be overridden in the user script
        /// <summary>Called to iniialize Nebulator stuff.</summary>
        public virtual void setupNeb() { }

        /// <summary>Called every Nebulator Tock.</summary>
        public virtual void step() { }
        #endregion

        #region Script callable functions
        /// <summary>
        /// Create a midi input.
        /// </summary>
        /// <param name="channel">1</param>
        /// <param name="controller">4</param>
        /// <param name="bound">COL1</param>
        protected void midiIn(int channel, int controller, NVariable bound)
        {
            NMidiControlPoint mp = new NMidiControlPoint() { Channel = channel, MidiController = controller, BoundVar = bound };
            DynamicElements.InputMidis.Add(mp);
        }

        /// <summary>
        /// Create a midi output.
        /// </summary>
        /// <param name="channel">1</param>
        /// <param name="controller">4</param>
        /// <param name="bound">COL1</param>
        protected void midiOut(int channel, int controller, NVariable bound)
        {
            NMidiControlPoint mp = new NMidiControlPoint() { Channel = channel, MidiController = controller, BoundVar = bound };
            DynamicElements.OutputMidis.Add(mp);
        }

        /// <summary>
        /// Create a UI leveer.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="bound"></param>
        protected void lever(int min, int max, NVariable bound)
        {
            NLeverControlPoint lp = new NLeverControlPoint() { Min = min, Max = max, BoundVar = bound };
            DynamicElements.Levers.Add(lp);
        }

        /// <summary>
        /// Normal factory.
        /// </summary>
        /// <param name="name">UI name</param>
        /// <param name="val">Initial value</param>
        protected NVariable variable(string name, int val)
        {
            NVariable nv = new NVariable() { Name = name, Value = val };
            DynamicElements.Variables.Add(nv);
            return nv;
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="length"></param>
        protected NSequence sequence(int length)
        {
            NSequence nseq = new NSequence() { Length = length };
            DynamicElements.Sequences.Add(nseq);
            return nseq;
        }

        /// <summary>
        /// Normal factory.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        protected NSection section(string name, int start, int length)
        {
            NSection nsec = new NSection() { Name = name, Start = start, Length = length };
            DynamicElements.Sections.Add(nsec);
            return nsec;
        }

        /// <summary>
        /// Normal factory.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        /// <param name="wobvol"></param>
        /// <param name="wobbefore"></param>
        /// <param name="wobafter"></param>
        protected NTrack track(string name, int channel, int wobvol = 0, int wobbefore = 0, int wobafter = 0)
        {
            NTrack nt = new NTrack() { Name = name, Channel = channel, WobbleVolume = wobvol, WobbleTimeBefore = wobbefore, WobbleTimeAfter = wobafter };
            DynamicElements.Tracks.Add(nt);
            return nt;
        }

        /// <summary>Send a midi note immediately. Respects solo/mute. Adds a note off to play after dur time.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="inote">Note number.</param>
        /// <param name="vol">Note volume. If 0, sends NoteOff instead.</param>
        /// <param name="dur">How long it lasts in Time. 0 means no note off generated. User has to turn it off explicitly.</param>
        public void sendMidiNote(NTrack track, int inote, int vol, double dur)
        {
            bool _anySolo = DynamicElements.Tracks.Where(t => t.State == TrackState.Solo).Count() > 0;

            bool play = track.State == TrackState.Solo || (track.State == TrackState.Normal && !_anySolo);

            if (play)
            {
                int vel = track.NextVol(vol);
                int notenum = Utils.Constrain(inote, 0, MidiInterface.MAX_MIDI_NOTE);

                if (vol > 0)
                {
                    StepNoteOn step = new StepNoteOn()
                    {
                        Channel = track.Channel,
                        NoteNumber = notenum,
                        NoteNumberToPlay = notenum,
                        Velocity = vel,
                        VelocityToPlay = vel,
                        Duration = new Time(dur)
                    };

                    step.Adjust(volume, track.Volume, track.Modulate);
                    MidiInterface.TheInterface.Send(step);
                }
                else
                {
                    StepNoteOff step = new StepNoteOff()
                    {
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
        public void sendMidiNote(NTrack track, string snote, int vol, double dur)
        {
            NSequenceElement note = new NSequenceElement(snote);

            if (note.Notes.Count == 0)
            {
                _logger.Warn($"Invalid note: {snote}");
            }
            else
            {
                note.Notes.ForEach(n => sendMidiNote(track, n, vol, dur));
            }
        }

        /// <summary>Send a midi note immediately. Respects solo/mute.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="snote">Note string using any form allowed in the script. Requires double quotes in the script.</param>
        /// <param name="vol">Note volume.</param>
        /// <param name="dur">How long it lasts in Time representation. 0 means no note off generated.</param>
        public void sendMidiNote(NTrack track, string snote, int vol, Time dur)
        {
            sendMidiNote(track, snote, vol, dur.AsDouble);
        }

        /// <summary>Send a midi controller immediately.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="ctlnum">Controller number.</param>
        /// <param name="val">Controller value.</param>
        public void sendMidiController(NTrack track, int ctlnum, int val)
        {
            StepControllerChange step = new StepControllerChange()
            {
                Channel = track.Channel,
                MidiController = ctlnum,
                ControllerValue = val
            };

            MidiInterface.TheInterface.Send(step);
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// <param name="track"></param>
        /// <param name="patch"></param>
        public void sendPatch(NTrack track, int patch)
        {
            StepPatch step = new StepPatch()
            {
                Channel = track.Channel,
                PatchNumber = patch
            };

            MidiInterface.TheInterface.Send(step);
        }

        /// <summary>Modulate all notes on the track by number of notes. Can be changed on the fly altering all subsequent notes.</summary>
        /// <param name="track">Track to alter notes on.</param>
        /// <param name="val">Number of notes, +-.</param>
        public void modulate(NTrack track, int val)
        {
            track.Modulate = val;
        }

        /// <summary>Send a named sequence.</summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which sequence to send.</param>
        public void playSequence(NTrack track, NSequence seq)
        {
            StepCollection scoll = ConvertToSteps(track, seq, RuntimeContext.StepTime.Tick);
            RuntimeContext.RuntimeSteps.Add(scoll);
        }

        /// <summary>
        /// Add a chord or scale definition.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">"1 4 6 b13"</param>
        protected void notes(string name, string parts)
        {
            NoteUtils.ScriptNoteDefs.Add(name, parts.SplitByToken(" "));
        }

        /// <summary>Convert the argument into numbered notes.</summary>
        /// <param name="note">Note string using any form allowed in the script.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        public int[] getNotes(string note)
        {
            List<int> notes = NoteUtils.ParseNoteString(note);
            return notes != null ? notes.ToArray() : new int[0];
        }

        /// <summary>Get an array of scale notes.</summary>
        /// <param name="scale">One of the named scales from ScriptDefinitions.md.</param>
        /// <param name="key">Note name and octave.</param>
        /// <returns>Array of notes or empty if invalid.</returns>
        public int[] getScaleNotes(string scale, string key)
        {
            List<int> notes = NoteUtils.GetScaleNotes(scale, key);
            return notes != null ? notes.ToArray() : new int[0];
        }
        #endregion
    }
}
