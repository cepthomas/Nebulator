using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Nebulator.Common;
using Nebulator.Scripting;

namespace Nebulator.Test
{
    public class AltScript : Script // Just for dev. TODO2 maybe useful later?
    {

        /*
        ///// Constants /////
        // When to play.
        WHEN1 = constant(0);
        WHEN2 = constant(32);
        WHEN3 = constant(64);
        WHEN4 = constant(96);
        // Total length.
        TLEN = constant(128);
        // Volumes.
        KEYS_DEF_VOL = constant(100);
        DRUM_DEF_VOL = constant(100);

        ///// Variables /////
        COL1 = variable(200); // change color
        MODN = variable(0); // modulate notes
        PITCH = variable(8192); // center is 8192

        ///// Realtime Controllers /////
        MI1 = midiin(1, 4, MODN);
        MO1 = midiout(1, Pitch, PITCH);

        ////// Levers //////
        L1 = lever(0, 255, COL1);
        L2 = lever(0, 16383, PITCH); // max range
        L3 = lever(-10, 10, MODN);


        ///// Tracks and Loops /////
        KEYS = track(1, 5, 0, 0); // wobbles
        BASS = track(2, 2, 0, 0); // wobbles
        DRUMS = track(10, 4, 0, 0); // wobbles
        SYNTH = track(3, 0);


        ////// Drums //////
        // Using patterns. Each hit is 1/16 note - fixed res for now.
        DRUMS_SIMPLE = sequence(8);
        note(x-------x-------x-------x-------, AcousticBassDrum, 90);
        note(----x-------x-x-----x-------x-x-, AcousticSnare, 80);

        ////// Sequenced triggered in script //////
        DSEQ1 = sequence(8);
        note(0.00, G.3, 90, 0.60);
        note(1.00, A.3, 90, 0.60);
        note(2.00, Bb.3, 90, 0.60);
        note(3.00, C.4, 90, 0.60);

        DSEQ2 = sequence(12);
        note(2.00, G#.3, 90, 0.60);
        note(5.00, A#.3, 90, 0.60);
        note(9.00, B.3, 90, 0.60);
        note(11.00, C#.4, 90, 0.60);

        DSEQ3 = sequence(19);
        note(7.00, F.3, 90, 0.60);
        note(12.00, F#.3, 90, 0.60);
        note(14.00, E.3, 90, 0.60);
        note(16.00, Eb.4, 90, 0.60);
        */


        // these:::::
        //MI1 = midiin(1, 4, MODN);
        //MO1 = midiout(1, Pitch, PITCH);
        //L1 = lever(0, 255, COL1);
        //KEYS = track(1, 5, 0, 0);
        //DRUMS_SIMPLE = sequence(8);
        //note(x-------x-------x-------x-------, AcousticBassDrum, 90);
        //note(----x-------x-x-----x-------x-x-, AcousticSnare, 80);



        //// Typical from compiler output.
        /*
        // constant()
        const int WHEN1 = 0;
        const int WHEN2 = 32;
        const int WHEN3 = 64;
        const int WHEN4 = 96;
        const int TLEN = 128;
        const int KEYS_DEF_VOL = 100;
        const int DRUM_DEF_VOL = 100;
        const int AcousticGrandPiano = 0;
        const int AcousticBass = 32;
        const int Pad3Polysynth = 90;

        // variable()
        int COL1 { get { return Dynamic.Vars["COL1"].Value; } set { Dynamic.Vars["COL1"].Value = value; } }
        int MODN { get { return Dynamic.Vars["MODN"].Value; } set { Dynamic.Vars["MODN"].Value = value; } }
        int PITCH { get { return Dynamic.Vars["PITCH"].Value; } set { Dynamic.Vars["PITCH"].Value = value; } }

        // track()
        Track KEYS { get { return Dynamic.Tracks["KEYS"]; } }
        Track BASS { get { return Dynamic.Tracks["BASS"]; } }
        Track DRUMS { get { return Dynamic.Tracks["DRUMS"]; } }
        Track SYNTH { get { return Dynamic.Tracks["SYNTH"]; } }

        // sequence()
        Sequence DRUMS_SIMPLE { get { return Dynamic.Sequences["DRUMS_SIMPLE"]; } }
        Sequence DSEQ1 { get { return Dynamic.Sequences["DSEQ1"]; } }
        Sequence DSEQ2 { get { return Dynamic.Sequences["DSEQ2"]; } }
        Sequence DSEQ3 { get { return Dynamic.Sequences["DSEQ3"]; } }
        */

        // What I need?
        const int WHEN1 = 0;
        const int WHEN2 = 32;
        const int WHEN3 = 64;
        const int WHEN4 = 96;
        const int TLEN = 128;
        const int KEYS_DEF_VOL = 100;
        const int DRUM_DEF_VOL = 100;
        const int AcousticGrandPiano = 0;
        const int AcousticBass = 32;
        const int Pad3Polysynth = 90;

        // variable()
        Variable COL1 { get; set; } = new Variable() { Name = "COL1", Value = 111 };
        Variable MODN { get; set; }
        Variable PITCH { get; set; }
        //int COL1 { get { return Dynamic.Vars["COL1"].Value; } set { Dynamic.Vars["COL1"].Value = value; } }
        //int MODN { get { return Dynamic.Vars["MODN"].Value; } set { Dynamic.Vars["MODN"].Value = value; } }
        //int PITCH { get { return Dynamic.Vars["PITCH"].Value; } set { Dynamic.Vars["PITCH"].Value = value; } }

        // track()
        Track KEYS { get { return Dynamic.Tracks["KEYS"]; } }
        Track BASS { get { return Dynamic.Tracks["BASS"]; } }
        Track DRUMS { get { return Dynamic.Tracks["DRUMS"]; } }
        Track SYNTH { get { return Dynamic.Tracks["SYNTH"]; } }

        // sequence()
        Sequence DRUMS_SIMPLE { get { return Dynamic.Sequences["DRUMS_SIMPLE"]; } }
        Sequence DSEQ1 { get { return Dynamic.Sequences["DSEQ1"]; } }
        Sequence DSEQ2 { get { return Dynamic.Sequences["DSEQ2"]; } }
        Sequence DSEQ3 { get { return Dynamic.Sequences["DSEQ3"]; } }



        public AltScript() : base()
        {
            _scriptFunctions.Add("MODN", On_MODN);
        }


        //////////////////// new way ///////////////////
        class Loop
        {
            public string note;
            public Time duration;
            public Time delay;
            public Time lastStart;
            public Loop(string nt, double dur, double del)
            {
                note = nt;
                duration = new Time(dur);
                delay = new Time(del);
                lastStart = new Time();
            }
        }

        ///// Functions /////

        List<Loop> loops = new List<Loop>();

        public override void setup()
        {
            println("setup()");

            loops.Add(new Loop("F.4", 19.7, 4));
            loops.Add(new Loop("Ab.4", 17.8, 8.1));
            loops.Add(new Loop("C.5", 21.3, 5.6));
            loops.Add(new Loop("Db.5", 18.5, 12.6));
            loops.Add(new Loop("Eb.5", 20.0, 9.2));
            loops.Add(new Loop("F.5", 20.0, 14.1));
            loops.Add(new Loop("Ab.5", 17.7, 3.1));

            // Patches (optional). Only needed if using the Windows GM.
            sendPatch(KEYS, AcousticGrandPiano);
            sendPatch(BASS, AcousticBass);
            sendPatch(SYNTH, Pad3Polysynth);
        }


        public override void step()
        {
            if (tock == 0)
            {
                int notenum = random(40, 70);
                sendMidiNote(SYNTH, notenum, 95, 1.09);
            }

            /*
            function playSample(instrument, note, destination, delaySeconds = 0)
            {
              getSample(instrument, note).then(({audioBuffer, distance}) =>
              {
                let playbackRate = Math.pow(2, distance / 12);
                let bufferSource = audioContext.createBufferSource();

                bufferSource.buffer = audioBuffer;
                bufferSource.playbackRate.value = playbackRate;

                bufferSource.connect(destination);
                bufferSource.start(audioContext.currentTime + delaySeconds);
              });
            }

            // Control variable, set to start time when playing begins
            let playingSince = null;
            */
        }

        public void On_MODN()
        {
            println("MODN changed to", MODN);
            modulate(KEYS, MODN.Value);
        }

        public override void draw()
        {
            background(COL1.Value, 100, 200);

            if (mousePressedP)
            {
                //println("mouse is pressed");
                fill(random(255), random(255), random(255));
                strokeWeight(2);
                stroke(0, 100);
                ellipse(mouseX, mouseY, 80, 80);
            }

            /* JS version
            function render()
            {
                const LANE_COLOR = 'rgba(220, 220, 220, 0.3)';
                const SOUND_COLOR = '#ED146F';


                context.clearRect(0, 0, 1000, 1000);

                context.strokeStyle = '#888';
                context.lineWidth = 1;
                context.moveTo(325, 325);
                context.lineTo(650, 325);
                context.stroke();

                context.lineWidth = 30;
                context.lineCap = 'round';
                let radius = 280;
                for (const { duration, delay}
                of LOOPS)
              {
                    const size = Math.PI * 2 / duration;
                    const offset = playingSince ? audioContext.currentTime - playingSince : 0;
                    const startAt = (delay - offset) * size;
                    const endAt = (delay + 0.01 - offset) * size;

                    context.strokeStyle = LANE_COLOR;
                    context.beginPath();
                    context.arc(325, 325, radius, 0, 2 * Math.PI);
                    context.stroke();

                    context.strokeStyle = SOUND_COLOR;
                    context.beginPath();
                    context.arc(325, 325, radius, startAt, endAt);
                    context.stroke();

                    radius -= 35;
                }

                if (playingSince)
                {
                    requestAnimationFrame(render);
                }
                else
                {
                    context.fillStyle = 'rgba(0, 0, 0, 0.3)';
                    context.strokeStyle = 'rgba(0, 0, 0, 0)';
                    context.beginPath();
                    context.moveTo(235, 170);
                    context.lineTo(485, 325);
                    context.lineTo(235, 455);
                    context.lineTo(235, 170);
                    context.fill();
                }
            }
            */

        }



        public override void mouseClicked()
        {
            // Note selected based on mouse position.
            //    int sn = (int)map(mouseX, 0, width, scaleNotes[0], scaleNotes[scaleNotes.Length - 1]);
            //    sendMidiNote(SYNTH, sn, 90, 0.48);
        }













        ///////////////////// other way? ///////////////////////////
        public const int EVERY = -1;

        void Exec(int tick, Action act)
        {
            Exec(new Time(tick, 0), act);
        }

        void Exec(int[] ticks, Action act)
        {
            foreach (int tick in ticks) { Exec(new Time(tick, 0), act); }
        }

        void Exec(int tick, int tock, Action act)
        {
            Exec(new Time(tick, tock), act);
        }

        void Exec(Time time, Action act)
        {
            if (!ScriptActions.ContainsKey(time))
            {
                ScriptActions.Add(time, new List<Action>());
            }
            ScriptActions[time].Add(act);
        }

        ///<summary>The main collection of actions. The key is the time to send the list.</summary>
        public Dictionary<Time, List<Action>> ScriptActions { get; set; } = new Dictionary<Time, List<Action>>();

        void SomeAction()
        {
            //int dodo = 99;
        }


        // From script source.
        ///// Play a sequence periodically.
        public void go2()
        {
            // Define script actions like this:
            Exec(1, () =>
            {
                //int todo = 99;
            });

            // or like this:
            Exec(new[] { 2, 3, 8 }, () =>
            {
                //int todo = 99;
            });

            // or like this:
            Exec(2, SomeAction);
            Exec(3, SomeAction);
            Exec(8, SomeAction);

            ///// New logic:
            Exec(new[] { 0, 16 }, () =>
            {
                // playSequence(SYNTH, DYNAMIC_SEQ);
            });
            Exec(8, () =>
            {
                sendMidiNote(SYNTH, "D.4", 95, 0.00); // named note on, no chase
            });
            Exec(12, () =>
            {
                sendMidiNote(SYNTH, 62, 0, 0.00); // numbered note off
            });
            Exec(new[] { 24, 25, 26, 27 }, () =>
            {
                int notenum = random(40, 70);
                sendMidiNote(SYNTH, notenum, 95, 1.09);
            });
        }
    }
}
