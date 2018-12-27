using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PNUT;
using Nebulator.Common;
//using Nebulator.Script;
using Nebulator.Synth;



// TODOX On the performance front, the killer is the .NET garbage collector. You have to hope that it
//doesn't kick in during a callback. With ASIO you often run at super low latencies (<10ms), and
//the garbage collector can cause a callback to be missed, meaning you'd hear a glitch in the audio output.
// NOW (4.5) >>> While the SustainedLowLatency setting is in effect, generation 0, generation 1, and background generation 2 
//collections still occur and do not typically cause noticeable pause times. A blocking generation 2 collection happens
//only if the machine is low in memory or if the app induces a GC by calling GC.Collect(). It is critical that you deploy
//apps that use the SustainedLowLatency setting onto machines that have adequate memory, so they will satisfy the resulting
//growth in the heap while the setting is in effect.




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
            bool ok = sout.Init("SYNTH:Asio4all");

            if(ok)
            {
                sout.SynthUGen = synth;

                sout.Start(); // TODOX separate thread?
                System.Threading.Thread.Sleep(2000);
                sout.Stop();
            }
        }

        //void Stopped(event)
        //{
        //    dac.Dispose();//??
        //    dac = null;//??

        //}

        // The overall synthesizer.
        public class MySynth : UGen
        {
            // components
            Voicer vcr = new Voicer(typeof(MyVoice), 5);
            LPF lpf = new LPF();
            Pan pan = new Pan();

            public override double Sample(double din)
            {
                return pan.Sample(lpf.Sample(vcr.Sample(din)));
            }
        }

        // A single voice in the generator.
        public class MyVoice : UGen
        {
            // 2 oscs slightly detuned and synced
            SinOsc osc1 = new SinOsc();
            SinOsc osc2 = new SinOsc();// { SyncWith = osc1; }
            Mix mix1 = new Mix();
            ADSR adsr = new ADSR();

            public MyVoice()
            {
                osc2.SyncWith = osc1;
            }

            public override double Sample(double din)
            {
                return adsr.Sample(mix1.Sample(osc1.Sample(), osc2.Sample()));
            }
        }

        //public class MyVoice2 : UGen
        //{
        //    // 2 oscs slightly detuned and synced
        //    SinOsc osc1 = new SinOsc();
        //    SinOsc osc2 = new SinOsc();
        //    Mix mix1 = new Mix();
        //    ADSR adsr = new ADSR();

        //    public MyVoice2()
        //    {
        //        osc2.SyncWith = osc1;
        //        mix1.AddInput(osc1);
        //        mix1.AddInput(osc2);
        //        adsr.AddInput(mix1);
        //    }

        //    public override double Sample(double din)
        //    {
        //        return mix1.Sample(din);
        //    }
        //}
    }
}
