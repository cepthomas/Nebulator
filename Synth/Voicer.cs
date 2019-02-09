
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
        public UGen ugen = null; // contained ugen
        public int birth = 0; // used for tracking oldest voice
        public double noteNumber = -1.0; // current sounding
        //public double frequency = 0.0; // current
        public int sounding = 0; // 0=no 1=yes <0=tail ticks
    }

    public class Voicer : UGen
    {
        #region Fields
        /// <summary>Constituents</summary>
        List<Voice> _voices = new List<Voice>();

        /// <summary>In sample clock ticks.</summary>
        int _muteTime;
        #endregion

        #region Properties
        public double DecayTime { set { _muteTime = value <= 0.0 ? 0 : ((int)value * SynthCommon.SampleRate); } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="t">Type of UGen to create.</param>
        /// <param name="num">How many voices.</param>
        public Voicer(Type t, int num)
        {
            bool ok = true;

            if (t.BaseType == typeof(UGen))
            {
                for (int i = 0; i < num; i++)
                {
                    Voice voice = new Voice
                    {
                        ugen = Activator.CreateInstance(t) as UGen,
                        birth = SynthCommon.NextId(),
                        noteNumber = -1.0
                    };
                    _voices.Add(voice);
                }
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

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            _voices.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Reset()
        {
            foreach (Voice v in _voices)
            {
                if (v.sounding > 0)
                {
                    v.ugen.Note(0.2, 0.0);
                    v.ugen.Reset();
                }
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override double Next(double _)
        {
            double dout = 0;

            foreach(Voice v in _voices)
            {
                if (v.sounding != 0)
                {
                    // Gen next sample(s).
                    double ds = v.ugen.Next(0);
                    dout += ds;
                }

                // Test for tail sound.
                if (v.sounding < 0)
                {
                    v.sounding++;
                }
 
                // Test for done.
                if (v.sounding == 0)
                {
                    v.noteNumber = -1.0;
                }
            }

            return dout * Volume;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <param name="amplitude"></param>
        public override void Note(double noteNumber, double amplitude)
        {
            if(amplitude != 0.0)
            {
                int oldest = -1;
                int index = -1;

                // Find a place to put this.
                for(int i = 0; i < _voices.Count && index == -1; i++)
                {
                    if (_voices[i].noteNumber < 0) // available slot
                    {
                        index = i;
                    }
                    else // keep track of oldest sounding
                    {
                        if (oldest == -1 || _voices[oldest].birth > _voices[i].birth)
                        {
                            oldest = i;
                        }
                    }
                }

                // Update the slot.
                int which = index != -1 ? index : oldest;

                // Voice management fields.
                _voices[which].birth = SynthCommon.NextId();
                _voices[which].noteNumber = noteNumber;
                _voices[which].sounding = 1;
                // make noise
                _voices[which].ugen.Note(noteNumber, amplitude);
            }
            else // stop
            {
                foreach (Voice v in _voices)
                {
                    if(v.noteNumber.IsClose(noteNumber))
                    {
                        v.ugen.Note(noteNumber, 0.0);
                        v.sounding = -_muteTime;
                    }
                }
            }
        }
        #endregion
    }
}
