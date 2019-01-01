using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PNUT;
using Nebulator.Common;
//using Nebulator.Script;
using Nebulator.Synth;



//------- hook modulators in!!!!
////// Sanford does this:
// public class LiteWaveVoice : Voice
//
// // For use as a portamento source.
// private SlewLimiter slewLimiter;

// // Two oscillators; typical of most subtractive synths.
// private Oscillator osc1;
// private Oscillator osc2;

// // A couple of LFO's to use as modulation sources.
// private Lfo lfo1;
// private Lfo lfo2;

// // A couple of ADSR envelopes to use as modulation source. The
// // first envelope is hardwired to modulate the overall amplitude.
// private AdsrEnvelope envelope1;
// private AdsrEnvelope envelope2;

// // For filtering the sound.
// private StateVariableFilter filter;

// // Convert the mono output of the filter into stereo.
// private MonoToStereoConverter converter;

// // For use when no modulation source is set.
// private EmptyMonoComponent emptyFMModulator;

// // For choosing which waveform the oscillators use.
// private OscWaveProgrammer oscWaveProgrammer1;
// private OscWaveProgrammer oscWaveProgrammer2;

// // For choosing the oscillators' FM source.
// private OscFMProgrammer oscFMProgrammer1;
// private OscFMProgrammer oscFMProgrammer2;


namespace Nebulator.Test
{
    // Midi utils stuff.
    public class SYNTH_Basic : TestSuite
    {
        public override void RunSuite()
        {
            MySynth synth = new MySynth();

            SynthOutput sout = new SynthOutput();
            bool ok = sout.Init("SYNTH:ASIO4ALL v2");

            UT_TRUE(ok);

            if (ok)
            {
                sout.SynthUGen = synth;
                sout.Start();

                double val = 0.3;
                while (val < 0.9)
                {
                    synth.lpf.Freq = val;
                    System.Threading.Thread.Sleep(200);
                    val += 0.1; 
                }

                sout.Stop();
            }
        }

        // The overall synthesizer.
        public class MySynth : UGen
        {
            // components
            public Voicer vcr = new Voicer(typeof(MyVoice), 5, 0.2);
            public LPF lpf = new LPF();
            public Pan pan = new Pan();

            public override Sample Next2(double din)
            {
                return pan.Next2(lpf.Next(vcr.Next(din)));  // TODOX something like: din -> vcr -> lpf -> pan;
            }

            // public virtual void NoteOn(double noteNumber, double amplitude)
            // public virtual void NoteOff(double noteNumber, double amplitude = 0.0)

        }

        // A single voice in the generator.
        public class MyVoice : UGen
        {
            // 2 oscs slightly detuned and synced
            SinOsc osc1 = new SinOsc();
            SinOsc osc2 = new SinOsc();
            Mix mix1 = new Mix();
            ADSR adsr = new ADSR();

            public MyVoice()
            {
                osc2.SyncWith = osc1;
            }

            public override double Next(double din)
            {
                return adsr.Next(mix1.Next(osc1.Next(0), osc2.Next(0)));
            }
        }
    }
}
