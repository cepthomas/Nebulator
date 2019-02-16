using System;


namespace Nebulator.Synth
{
    public enum SyncType
    {
        Freq = 0,   // sync frequency to input // the input signal will set the frequency of the oscillator
        Phase = 1,  // sync phase to input // the input signal will set the phase of the oscillator
        FM = 2      // the input signal will add to the oscillator's current frequency - TODON1 see fm2.ck - Next()
    }

    /// <summary>
    /// Oscillator base class.
    /// </summary>
    public abstract class Oscillator : UGen
    {
        #region Fields
        protected double _phaseIncr = 0.0; // phase increment was _num
        protected double _freq = 0.0; // 0.0 means not playing
        protected double _mod = 0.0; // modulation value - FM
        protected SyncType _sync = SyncType.Freq;
        protected double _width = 0.5;
        protected double _phase = 0.0;
        #endregion

        #region Properties
        public SyncType Sync { get; set; } //TODON1 rethink this
        // original ?? if (_sync == SyncType.Freq && psync != _sync)
        //set: 
        //{
        //    // if we are switching to internal tick
        //    // we need to pre-advance the phase... 
        //    // this is probably stupid.  -pld
        //    _phase += _phaseIncr;
        //    //bound 0.0 -> 1.0
        //    if (_phase > 1.0)
        //    {
        //        _phase -= 1.0;
        //    }
        //}

        public double Freq
        {
            get { return _freq; }
            set { _freq = value; _phaseIncr = _freq / SynthCommon.SampleRate; }
        }

        public double Phase
        {
            get { return _phase; }
            set { _phase = Wrap(value); }
        }

        public double Mod
        {
            get { return _mod; }
            set { _mod = value; }
        }

        public double Width
        {
            get { return _width; }
            set { _width = Bound(value); }
        }
        #endregion

        #region Public Functions
        /// <inheritdoc />
        public override void NoteOn(double noteNumber, double amplitude)
        {
            Freq = NoteToFreq(noteNumber);
            Volume = amplitude;
            _phase = 0; // TODON1 correct??
        }

        /// <inheritdoc />
        public override void NoteOff(double noteNumber)
        {
            Freq = 0; // TODON1 needs zero-crossing
            Volume = 0;
        }

        /// <inheritdoc />
        public override double Next()
        {
            double dout = 0.0;

            if (_freq > 0.0)
            {
                bool incrementPhase = true;

                switch (_sync)
                {
                    case SyncType.Freq: //Base
                        //_freq = din;
                        _phaseIncr = _freq / SynthCommon.SampleRate;
                        break;

                    case SyncType.Phase: //Base
                        //_phase = din;
                        incrementPhase = false;
                        break;

                    case SyncType.FM: //Base
                        //double freq = _freq + din;
                        double freq = _freq + _mod;
                        _phaseIncr = freq / SynthCommon.SampleRate;
                        break;
                }

                // Shape specific calc.
                dout = ComputeNext();

                if (incrementPhase)
                {
                    _phase = Wrap(_phase + _phaseIncr);
                }
            }

            return dout * Volume;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            // _ampl = 0.0;
            Freq = 0.0;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// The shape specific calculation to be provided by implementation.
        /// </summary>
        /// <returns></returns>
        protected abstract double ComputeNext();
        #endregion
    }

    public class Phasor : Oscillator
    {
        protected override double ComputeNext()
        {
            return _phase;
        }
    }

    public class SinOsc : Oscillator
    {
        protected override double ComputeNext()
        {
            return Math.Sin(_phase * Math.PI * 2);
        }
    }

    public class TriOsc : Oscillator
    {
        protected override double ComputeNext()
        {
            double dout = 0.0;
            double phase = _phase + .25;

            if (phase > 1.0)
            {
                phase -= 1.0;
            }

            if (phase < _width)
            {
                dout = (_width == 0.0) ? 1.0 : -1.0 + 2.0 * phase / _width;
            }
            else
            {
                dout = (_width == 1.0) ? 0 : 1.0 - 2.0 * (phase - _width) / (1.0 - _width);
            }
            
            return dout;
        }
    }
 
    public class PulseOsc : Oscillator
    {
        protected override double ComputeNext()
        {
            return (_phase < _width) ? 1.0 : -1.0; ;
        }
    }


#if TODON2_BLIT_OSC

/***************************************************/
/*! \class Blit
    \brief STK band-limited impulse train class.

    This class generates a band-limited impulse train using a
    closed-form algorithm reported by Stilson and Smith in "Alias-Free
    Digital Synthesis of Classic Analog Waveforms", 1996.  The user
    can specify both the fundamental frequency of the impulse train
    and the number of harmonics contained in the resulting signal.

    The signal is normalized so that the peak value is +/-1.0.

    If nHarmonics is 0, then the signal will contain all harmonics up
    to half the sample rate.  Note, however, that this setting may
    produce aliasing in the signal when the frequency is changing (no
    automatic modification of the number of harmonics is performed by
    the setFrequency() function).

    Original code by Robin Davies, 2005.
    Revisions by Gary Scavone for STK, 2005.
*/
/***************************************************/

Blit:: Blit( double frequency )   : public BLT
{
    nHarmonics_ = 0;
    this->setFrequency( frequency );
    this->reset();
}

Blit :: ~Blit()
{
}

void Blit :: reset()
{
    phase_ = 0.0;
    // lastOutput_ = 0;
}

void Blit :: setFrequency( double frequency )
{
    p_ = SynthCommon.SAMPLE_RATE / frequency;
    rate_ = ONE_PI / p_;
    this->updateHarmonics();
}

void Blit :: setHarmonics( unsigned int nHarmonics )
{
    nHarmonics_ = nHarmonics;
    this->updateHarmonics();
}

void Blit :: updateHarmonics( void )
{
    if ( nHarmonics_ <= 0 )
    {
        unsigned int maxHarmonics = (unsigned int) floor( 0.5 * p_ );
        m_ = 2 * maxHarmonics + 1;
    }
    else
        m_ = 2 * nHarmonics_ + 1;
}

double Blit :: tick( void )
{
    // The code below implements the SincM algorithm of Stilson and
    // Smith with an additional scale factor of P / M applied to
    // normalize the output.

    // A fully optimized version of this code would replace the two sin
    // calls with a pair of fast sin oscillators, for which stable fast
    // two-multiply algorithms are well known. In the spirit of STK,
    // which favors clarity over performance, the optimization has not
    // been made here.

    double output;

    // Avoid a divide by zero at the sinc peak, which has a limiting
    // value of 1.0.
    double denominator = sin( phase_ );
    if ( denominator <= std::numeric_limits<double>::epsilon() )
    {
        output = 1.0;
    }
    else
    {
        output =  sin( m_ * phase_ );
        output /= m_ * denominator;
    }

    phase_ += rate_;
    if ( phase_ >= ONE_PI ) phase_ -= ONE_PI;

    return output;
}


/***************************************************/
/*! \class BlitSaw
    \brief STK band-limited sawtooth wave class.

    This class generates a band-limited sawtooth waveform using a
    closed-form algorithm reported by Stilson and Smith in "Alias-Free
    Digital Synthesis of Classic Analog Waveforms", 1996.  The user
    can specify both the fundamental frequency of the sawtooth and the
    number of harmonics contained in the resulting signal.

    If nHarmonics is 0, then the signal will contain all harmonics up
    to half the sample rate.  Note, however, that this setting may
    produce aliasing in the signal when the frequency is changing (no
    automatic modification of the number of harmonics is performed by
    the setFrequency() function).

    Based on initial code of Robin Davies, 2005.
    Modified algorithm code by Gary Scavone, 2005.
*/
/***************************************************/

BlitSaw:: BlitSaw( double frequency )   : public BLT
{
    nHarmonics_ = 0;
    this->reset();
    this->setFrequency( frequency );
}

BlitSaw :: ~BlitSaw()
{
}

void BlitSaw :: reset()
{
    phase_ = 0.0f;
    state_ = 0.0;
    // lastOutput_ = 0;
}

void BlitSaw :: setFrequency( double frequency )
{
    p_ = SynthCommon.SAMPLE_RATE / frequency;
    C2_ = 1 / p_;
    rate_ = ONE_PI * C2_;
    this->updateHarmonics();
}

void BlitSaw :: setHarmonics( unsigned int nHarmonics )
{
    nHarmonics_ = nHarmonics;
    this->updateHarmonics();

    // I found that the initial DC offset could be minimized with an
    // initial state setting as given below.  This initialization should
    // only happen before starting the oscillator for the first time
    // (but after setting the frequency and number of harmonics).  I
    // struggled a bit to decide where best to put this and finally
    // settled on here.  In general, the user shouldn't be messing with
    // the number of harmonics once the oscillator is running because
    // this is automatically taken care of in the setFrequency()
    // function.  (GPS - 1 October 2005)
    state_ = -0.5 * a_;
}

void BlitSaw :: updateHarmonics( void )
{
    if ( nHarmonics_ <= 0 )
    {
        unsigned int maxHarmonics = (unsigned int) floor( 0.5 * p_ );
        m_ = 2 * maxHarmonics + 1;
    }
    else
        m_ = 2 * nHarmonics_ + 1;

    a_ = m_ / p_;
}

double BlitSaw :: tick( void )
{
    // The code below implements the BLIT algorithm of Stilson and
    // Smith, followed by a summation and filtering operation to produce
    // a sawtooth waveform.  After experimenting with various approaches
    // to calculate the average value of the BLIT over one period, I
    // found that an estimate of C2_ = 1.0 / period (in samples) worked
    // most consistently.  A "leaky integrator" is then applied to the
    // difference of the BLIT output and C2_. (GPS - 1 October 2005)

    // A fully  optimized version of this code would replace the two sin
    // calls with a pair of fast sin oscillators, for which stable fast
    // two-multiply algorithms are well known. In the spirit of STK,
    // which favors clarity over performance, the optimization has
    // not been made here.

    double output;

    // Avoid a divide by zero, or use of a denormalized divisor
    // at the sinc peak, which has a limiting value of m_ / p_.
    double denominator = sin( phase_ );
    if ( fabs(denominator) <= std::numeric_limits<double>::epsilon() )
        output = a_;
    else
    {
        output =  sin( m_ * phase_ );
        output /= p_ * denominator;
    }

    output += state_ - C2_;
    state_ = output * 0.995;

    phase_ += rate_;
    if ( phase_ >= ONE_PI ) phase_ -= ONE_PI;

    return output;
}


/***************************************************/
/*! \class BlitSquare
    \brief STK band-limited square wave class.

    This class generates a band-limited square wave signal.  It is
    derived in part from the approach reported by Stilson and Smith in
    "Alias-Free Digital Synthesis of Classic Analog Waveforms", 1996.
    The algorithm implemented in this class uses a SincM function with
    an even M value to achieve a bipolar bandlimited impulse train.
    This signal is then integrated to achieve a square waveform.  The
    integration process has an associated DC offset but that is
    subtracted off the output signal.

    The user can specify both the fundamental frequency of the
    waveform and the number of harmonics contained in the resulting
    signal.

    If nHarmonics is 0, then the signal will contain all harmonics up
    to half the sample rate.  Note, however, that this setting may
    produce aliasing in the signal when the frequency is changing (no
    automatic modification of the number of harmonics is performed by
    the setFrequency() function).

    Based on initial code of Robin Davies, 2005.
    Modified algorithm code by Gary Scavone, 2005.
*/
/***************************************************/

BlitSquare:: BlitSquare( double frequency )   : public BLT
{
    nHarmonics_ = 0;
    this->setFrequency( frequency );
    this->reset();
}

BlitSquare :: ~BlitSquare()
{
}

void BlitSquare :: reset()
{
    phase_ = 0.0;
    m_output = 0;
    dcbState_ = 0.0;
    m_boutput = 0.0;
}

void BlitSquare :: setFrequency( double frequency )
{
    // By using an even value of the parameter M, we get a bipolar blit
    // waveform at half the blit frequency.  Thus, we need to scale the
    // frequency value here by 0.5. (GPS, 2006).
    p_ = 0.5 * SynthCommon.SAMPLE_RATE / frequency;
    rate_ = ONE_PI / p_;
    this->updateHarmonics();
}

void BlitSquare :: setHarmonics( unsigned int nHarmonics )
{
    nHarmonics_ = nHarmonics;
    this->updateHarmonics();
}

void BlitSquare :: updateHarmonics( void )
{
    // Make sure we end up with an even value of the parameter M here.
    if ( nHarmonics_ <= 0 )
    {
        unsigned int maxHarmonics = (unsigned int) floor( 0.5 * p_ );
        m_ = 2 * ( maxHarmonics );
    }
    else
        m_ = 2 * ( nHarmonics_ );

    // This offset value was derived empirically. (GPS, 2005)
    // offset_ = 1.0 - 0.5 * m_ / p_;

    a_ = m_ / p_;
}

double BlitSquare :: tick( void )
{
    double temp = m_boutput;

    // A fully  optimized version of this would replace the two sin calls
    // with a pair of fast sin oscillators, for which stable fast
    // two-multiply algorithms are well known. In the spirit of STK,
    // which favors clarity over performance, the optimization has
    // not been made here.

    // Avoid a divide by zero, or use of a denomralized divisor
    // at the sinc peak, which has a limiting value of 1.0.
    double denominator = sin( phase_ );
    if ( fabs( denominator )  < std::numeric_limits<double>::epsilon() )
    {
        // Inexact comparison safely distinguishes betwen *close to zero*, and *close to PI*.
        if ( phase_ < 0.1f || phase_ > TWO_PI - 0.1f )
            m_boutput = a_;
        else
            m_boutput = -a_;
    }
    else
    {
        m_boutput =  sin( m_ * phase_ );
        m_boutput /= p_ * denominator;
    }

    m_boutput += temp;

    // Now apply DC blocker.
    m_output = m_boutput - dcbState_ + 0.999 * m_output;
    dcbState_ = m_boutput;

    phase_ += rate_;
    if ( phase_ >= TWO_PI ) phase_ -= TWO_PI;

    return m_output;
}


/***************************************************/
/*! \class Noise
    \brief STK noise generator.

    Generic random number generation using the
    C rand() function.  The quality of the rand()
    function varies from one OS to another.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

Noise :: Noise() : Stk()
{
    // Seed the random number generator with system time.
    this->setSeed( 0 );
    lastOutput = (double) 0.0;
}

Noise :: Noise( unsigned int seed ) : Stk()
{
    // Seed the random number generator
    this->setSeed( seed );
    lastOutput = (double) 0.0;
}

Noise :: ~Noise()
{
}

void Noise :: setSeed( unsigned int seed )
{
    if ( seed == 0 )
        srand( (unsigned int) time(NULL) );
    else
        srand( seed );
}

double Noise :: tick()
{
    lastOutput = (double) (2.0 * rand() / (RAND_MAX + 1.0) );
    lastOutput -= 1.0;
    return lastOutput;
}

double *Noise :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick();

    return vec;
}

double Noise :: lastOut() const
{
    return lastOutput;
}


/***************************************************/
/*! \class SubNoise
    \brief STK sub-sampled noise generator.

    Generates a new random number every "rate" ticks
    using the C rand() function.  The quality of the
    rand() function varies from one OS to another.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


SubNoise :: SubNoise(int subRate) : Noise()
{
    rate = subRate;
    counter = rate;
}
SubNoise :: ~SubNoise()
{
}

int SubNoise :: subRate(void) const
{
    return rate;
}

void SubNoise :: setRate(int subRate)
{
    if (subRate > 0)
        rate = subRate;
}

double SubNoise :: tick()
{
    if ( ++counter > rate )
    {
        Noise::tick();
        counter = 1;
    }

    return lastOutput;
}

#endif

}
