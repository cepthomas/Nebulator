
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;


namespace Nebulator.Synth
{
    /// Represents one sounding voice.
    class Voice
    {
        public UGen ugen = null;
        public int birth = 0; // used for tracking oldest voice
        public double noteNumber = -1; // current
        public double frequency = 0.0; // current
        public int sounding = 0; // 0=no 1=yes <0=tail ticks
    }

    public class Voicer : UGen
    {
        #region Fields
        List<Voice> _voices = new List<Voice>();
        int _muteTime;
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public Voicer(Type t, int num, double decayTime = 0.0)
        {
            bool ok = true;

            if (t.BaseType == typeof(UGen))
            {
                for (int i = 0; i < num; i++)
                {
                    Voice voice = new Voice();
                    voice.ugen = Activator.CreateInstance(t) as UGen;
                    voice.birth = SynthCommon.NextId();
                    voice.noteNumber = -1;
                    _voices.Add(voice);
                }

                _muteTime = decayTime <= 0.0 ? 0 : ((int)decayTime * SynthCommon.SAMPLE_RATE);
            }
            else
            {
                ok = false;
            }

            if (!ok)
            {
                throw new Exception("Bad Voicer");
            }
        }

        public override double Sample(double din)
        {
            double dout = 0;

            foreach(Voice v in _voices)
            {
                if (v.sounding != 0)
                {
                    // Gen next sample(s).
                    dout += v.ugen.Sample();
 
                    // // Update the output buffer.
                    // for (uint j = 0; j < v.ugen.channelsOut(); j++)
                    // {
                    //     _lastFrame[j] += v.ugen.lastOut(j);
                    // }
                }
 
                // Test for tail sound.
                if (v.sounding < 0)
                {
                    v.sounding++;
                }
 
                // Test for done.
                if (v.sounding == 0)
                {
                    v.noteNumber = -1;
                }
            }

            return dout;
        }

        public void Clear()
        {
            _voices.Clear();
        }

        public override void Reset()
        {
            foreach(Voice v in _voices)
            {
                if (v.sounding > 0)
                {
                    v.ugen.NoteOff(0.2);
                    v.ugen.Reset();
                }
            }
        }

        public override void NoteOn(double noteNumber, double amplitude)
        {
            double frequency = SynthCommon.NoteToFreq(noteNumber);
            int found = -1;
            int oldest = -1;
            int index = 0;

            // Find a place to put this.
            foreach(Voice v in _voices)
            {
                if (v.noteNumber < 0) // available slot
                {
                    found = index;
                }
                else // keep track of oldest sounding
                {
                    if(oldest == -1 || _voices[oldest].birth > v.birth)
                    {
                        oldest = index;
                    }
                }
                index++;
            }

            // Update the slot.
            int which = found != -1 ? found : oldest;
            _voices[which].birth = SynthCommon.NextId();
            _voices[which].noteNumber = noteNumber;
            _voices[which].frequency = frequency;
            _voices[which].ugen.NoteOn(noteNumber, amplitude * SynthCommon.ONE_OVER_128);
            _voices[which].sounding = 1;
        }

        public override void NoteOff(double noteNumber, double amplitude)
        {
            foreach (Voice v in _voices)
            {
                if (SynthCommon.Close(v.noteNumber, noteNumber))
                {
                    v.ugen.NoteOff(amplitude * SynthCommon.ONE_OVER_128);
                    v.sounding = -_muteTime;
                }
            }
        }

        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        //void SetFrequency(double noteNumber)
        //{
        //    double frequency = SynthCommon.NoteToFreq(noteNumber);
        //    foreach (Voice v in _voices)
        //    {
        //        v.noteNumber = noteNumber;
        //        v.frequency = frequency;
        //        v.ugen.SetFrequency(frequency);
        //    }
        //}
    }
}
