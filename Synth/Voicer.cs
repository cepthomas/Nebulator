
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
        /// <summary>Contained ugen.</summary>
        public UGen ugen = null;

        /// <summary>Used for tracking oldest voice.</summary>
        public int birth = 0;

        /// <summary>Current sounding note.</summary>
        public double noteNumber = -1.0;

        /// <summary>Making noise.</summary>
        public bool sounding = false;

        /// <summary>After note off.</summary>
        public int release = 0;
    }

    public class Voicer : UGen
    {
        #region Fields
        /// <summary>Constituents.</summary>
        List<Voice> _voices = new List<Voice>();

        /// <summary>In sample clock ticks.</summary>
        int _releaseTime;
        #endregion

        #region Properties
        /// <summary></summary>
        public double DecayTime { set { _releaseTime = value <= 0.0 ? 0 : (int)(value * (double)SynthCommon.SampleRate); } }
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
                if (v.sounding)
                {
                    //v.ugen.NoteOff(0.0, 0.0);
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
                if (v.sounding)
                {
                    // Gen next sample.
                    double ds = v.ugen.Next(0);
                    dout += ds;
                }

                // Test for tail sound.
                if (v.release > 0)
                {
                    v.release--;
                }

                // Test for done.
                if (v.release <= 0)
                {
                    v.sounding = false;
                    v.release = 0;
                }
            }

            return dout * Volume;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <param name="amplitude"></param>
        public override void NoteOn(double noteNumber, double amplitude)
        {
            // Find a place to put this.
            int oldest = -1;
            int index = -1;

            for (int i = 0; i < _voices.Count && index == -1; i++)
            {
                if (!_voices[i].sounding) // available slot
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
            _voices[which].sounding = true;
            // make noise
            _voices[which].ugen.NoteOn(noteNumber, amplitude);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="noteNumber"></param>
        public override void NoteOff(double noteNumber)
        {
            foreach (Voice v in _voices)
            {
                if (/*v.sounding &&*/ v.noteNumber.IsClose(Math.Abs(noteNumber)))
                {
                    v.ugen.NoteOff(noteNumber);
                    v.noteNumber = -1;
                    v.release = _releaseTime;
                }
            }
        }
        #endregion
    }
}
