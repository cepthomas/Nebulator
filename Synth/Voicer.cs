
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

    public class Voicer
    {
        List<Voice> _voices;

        int _muteTime;
        //StkFrames _lastFrame;

        Voicer(double decayTime = 0.0)
        {
            _muteTime = decayTime <= 0.0 ? 0 : ((int)decayTime * SynthCommon.SAMPLE_RATE);
            //_lastFrame.resize(1, 2); // Fixed at one stereo frame
        }

        void Clear()
        {
            //foreach(Voice v in _voices)
            //{
            //    delete v.ugen;
            //}
            _voices.Clear();
        }

        void AddUGen(UGen ugen)
        {
            Voice voice = new Voice();
            voice.ugen = ugen;
            voice.birth = SynthCommon.NextId();
            voice.noteNumber = -1;
            _voices.Add(voice);
        }

        void NoteOn(double noteNumber, double amplitude)
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
            _voices[which].ugen.NoteOn(frequency, amplitude * SynthCommon.ONE_OVER_128);
            _voices[which].sounding = 1;
        }

        void NoteOff(double noteNumber, double amplitude)
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

        void SetFrequency(double noteNumber)
        {
            double frequency = SynthCommon.NoteToFreq(noteNumber);

            foreach (Voice v in _voices)
            {
                v.noteNumber = noteNumber;
                v.frequency = frequency;
 //TODOX ????               v.ugen.SetFrequency(frequency);
            }
        }

        void ControlChange(int number, double value)
        {
            foreach (Voice v in _voices)
            {
                v.ugen.ControlChange(number, value);
            }
        }

        void Silence()
        {
            foreach (Voice v in _voices)
            {
                if (v.sounding > 0)
                {
                    v.ugen.NoteOff(0.2);
                }
            }
        }

        int Read(double[] buffer, int count)
        {


            return count;
        }

        //void tick()
        //{
        //    // Clear buffer.
        //    //_lastFrame[0] = 0.0;
        //    //_lastFrame[1] = 0.0;

        //    foreach (Voice v in _voices)
        //    {
        //        if (v.sounding != 0)
        //        {
        //            // Gen next sample(s).
        //            v.ugen.tick();

        //            // Update the output buffer.
        //            for (uint j = 0; j < v.ugen.channelsOut(); j++)
        //            {
        //                _lastFrame[j] += v.ugen.lastOut(j);
        //            }
        //        }

        //        // Test for tail sound.
        //        if (v.sounding < 0)
        //        {
        //            v.sounding++;
        //        }

        //        // Test for done.
        //        if (v.sounding == 0)
        //        {
        //            v.noteNumber = -1;
        //        }
        //    }
        //}

        //void tick(StkFrames& frames)
        //{
        //     uint nChannels = _lastFrame.channels();
        //     double* samples = &frames[channel];
        //     uint hop = frames.channels() - nChannels;

        //     for(const Voice& v: _voices)
        //     {

        //     }


        //     for (uint i = 0; i < frames.frames(); i++, samples += hop)
        //     {
        //         tick();

        //         for (uint j = 0; j < nChannels; j++)
        //         {
        //             *samples++ = _lastFrame[j];
        //         }
        //     }
        //}

    }

    
} // namespace
