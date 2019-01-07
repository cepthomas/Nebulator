
using System;

namespace Nebulator.Synth
{
    // This Envelope subclass implements a
    // traditional ADSR (Attack, Decay,
    // Sustain, Release) envelope.  It
    // responds to simple keyOn and keyOff
    // messages, keeping track of its state.
    // The \e state = ADSR::DONE after the
    // envelope value reaches 0.0 in the
    // ADSR::RELEASE state.
    public class ADSR_o : UGen// Envelope
    {
        public enum ADSRState { IDLE, ATTACK, DECAY, SUSTAIN, RELEASE }

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        double _rate = 0.0;
        double _target = 0.0;
        double _value = 0.0;
        double _attackRate = 0.001;
        double _decayRate = 0.001;
        double _sustainLevel = 0.5;
        double _releaseRate = 0.01;
        ADSRState _state = ADSRState.IDLE;

        double m_decayTime = -1.0; // not used (chuck?) ????????????
        double m_releaseTime = -1.0; // not used (chuck?) ????????????

        public double AttackTime
        {
            get { return 1.0 / (_attackRate * SynthCommon.SAMPLE_RATE); }
            set { setAttackTime(value); }
        }

        public double DecayTime
        {
            get { return (1.0 - _sustainLevel) / (_decayRate * SynthCommon.SAMPLE_RATE); }
            set { setDecayTime(value); }
        }

        public double SustainLevel
        {
            get { return _sustainLevel; }
            set { setSustainLevel(value); }
        }

        public double ReleaseTime
        {
            get { return _sustainLevel / (_releaseRate * SynthCommon.SAMPLE_RATE); }
            set { setReleaseTime(value); }
        }

        public override double Next(double _)
        {
            switch(_state)
            {
                case ADSRState.ATTACK:
                    _value += _rate;
                    if (_value >= _target)
                    {
                        _value = _target;
                        _rate = _decayRate;
                        _target = _sustainLevel;
                        _state = ADSRState.DECAY;

                        // TODO: check this
                        if( _decayRate >= double.MaxValue ) // big number
                        {
                            // go directly to sustain.
                            _state = ADSRState.SUSTAIN;
                            _value = _sustainLevel;
                            _rate = 0.0;
                        }
                    }
                    break;

                case ADSRState.DECAY:
                    _value -= _decayRate;
                    if (_value <= _sustainLevel)
                    {
                        _value = _sustainLevel;
                        _rate = 0.0;
                        _state = ADSRState.SUSTAIN;
                    }
                    break;

                case ADSRState.RELEASE:
                    // WAS:
                    // value -= releaseRate;

                    // chuck
                    _value -= _rate;

                    if (_value <= 0.0)
                    {
                        _value = 0.0;
                        _state = ADSRState.IDLE;
                    }
                    break;
            }

            return _value;
        }

        public void KeyOn()
        {
            _target = 1.0;
            _rate = _attackRate;
            _state = ADSRState.ATTACK;
        }

        public void KeyOff()
        {
            // chuck
            if( m_releaseTime > 0 )
            {
                // in case release triggered before sustain
                _rate = _value / (m_releaseTime * SynthCommon.SAMPLE_RATE);
            }
            else
            {
                // rate was set
                _rate = _releaseRate;
            }

            _target = 0.0;
            _state = ADSRState.RELEASE;
        }

        ////////TODOX1 these - also check for valid values.
        void setSustainLevel(double aLevel)
        {
            _sustainLevel = aLevel;

            // chuck: need to recompute decay and release rates
            if( m_decayTime > 0.0 )
            {
                setDecayTime( m_decayTime );
            }
            if( m_releaseTime > 0.0 )
            {
                setReleaseTime( m_releaseTime );
            }
        }

        void setReleaseRate(double aRate)
        {
            _releaseRate = aRate;

            // chuck
            m_releaseTime = -1.0;
        }

        void setAttackTime(double aTime)
        {
            _attackRate = 1.0 / ( aTime * SynthCommon.SAMPLE_RATE );
        }

        void setDecayTime(double aTime)
        {
            if (aTime < 0.0)
            {
                _decayRate = (1.0 - _sustainLevel) / ( -aTime * SynthCommon.SAMPLE_RATE );
            }
            else if( aTime == 0.0 )
            {
                _decayRate = double.MaxValue; // a big number
            }
            else
            {
                _decayRate = (1.0 - _sustainLevel) / ( aTime * SynthCommon.SAMPLE_RATE );
            }

            // chuck
            m_decayTime = aTime;
        }

        void setReleaseTime(double aTime)
        {
            _releaseRate = _sustainLevel / ( aTime * SynthCommon.SAMPLE_RATE );

            // chuck
            m_releaseTime = aTime;
        }

        void setTarget(double aTarget)
        {
            _target = aTarget;

            if (_value < _target)
            {
                _state = ADSRState.ATTACK;
                setSustainLevel(_target);
                _rate = _attackRate;
            }

            if (_value > _target)
            {
                setSustainLevel(_target);
                _state = ADSRState.DECAY;
                _rate = _decayRate;
            }
        }

        void setValue(double aValue)
        {
            _state = ADSRState.SUSTAIN;
            _target = aValue;
            _value = aValue;
            setSustainLevel(aValue);
            _rate = 0.0;
        }
    }

    /***************************************************/
    /*! \class Envelope
        \brief STK envelope base class.

        This class implements a simple envelope
        generator which is capable of ramping to
        a target value by a specified \e rate.
        It also responds to simple \e keyOn and
        \e keyOff messages, ramping to 1.0 on
        keyOn and to 0.0 on keyOff.

        by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
    */
    /***************************************************/
    public class Envelope : UGen
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion


        protected double _value = 0.0;
        protected double _target = 0.0;
        protected double _rate = 0.001;
        int _state = 0; // is 0 or 1

        protected double m_target = 1.0; // chuck ????????????
        protected double m_time; // chuck ????????????

        public Envelope()
        {
            m_time = m_target / (_rate * SynthCommon.SAMPLE_RATE);
        }

        public void KeyOn()
        {
            _target = m_target;
            if (_value != _target)
                _state = 1;
            setTime( m_time );
        }

        public void KeyOff()
        {
            _target = 0.0;
            if (_value != _target)
                _state = 1;
            setTime( m_time );
        }

        public override double Next(double _)
        {
            if (_state > 0)
            {
                if (_target > _value)
                {
                    _value += _rate;
                    if (_value >= _target)
                    {
                        _value = _target;
                        _state = 0;
                    }
                }
                else
                {
                    _value -= _rate;
                    if (_value <= _target)
                    {
                        _value = _target;
                        _state = 0;
                    }
                }
            }
            return _value;
        }

        // double *tick(double *vec, unsigned int vectorSize)
        // {
        //     for (unsigned int i=0; i<vectorSize; i++)
        //         vec[i] = tick();

        //     return vec;
        // }

        ////////TODOX2 these - also check for valid values.

        void setRate(double aRate)
        {
            _rate = aRate;

            m_time = (_target - _value) / (_rate * SynthCommon.SAMPLE_RATE);

            if( m_time < 0.0 )
                m_time = -m_time;
        }

        void setTime(double aTime)
        {
            if( aTime == 0.0 )
                _rate = double.MaxValue;
            else
                _rate = (_target - _value) / (aTime * SynthCommon.SAMPLE_RATE);

            // rate
            if( _rate < 0 )
                _rate = -_rate;

            // should >= 0
            m_time = aTime;
        }

        void setTarget(double aTarget)
        {
            _target = aTarget;
            m_target = aTarget;

            if (_value != _target)
                _state = 1;

            // set time
            setTime( m_time );
        }

        //void setValue(double aValue)
        //{
        //    _state = 0;
        //    _target = aValue;
        //    _value = aValue;
        //}

        //int getState()
        //{
        //    return _state;
        //}

        //double lastOut()
        //{
        //    return _value;
        //}
    }
}


#if TODOX2_PORT_ALL_THIS

////////////////////////////////////////////////////////
// Source //


/***************************************************/
/*! \class WvIn
    \brief STK audio data input base class.

    This class provides input support for various
    audio file formats.  It also serves as a base
    class for "realtime" streaming subclasses.

    WvIn loads the contents of an audio file for
    subsequent output.  Linear interpolation is
    used for fractional "read rates".

    WvIn supports multi-channel data in interleaved
    format.  It is important to distinguish the
    tick() methods, which return samples produced
    by averaging across sample frames, from the
    tickFrame() methods, which return pointers to
    multi-channel sample frames.  For single-channel
    data, these methods return equivalent values.

    Small files are completely read into local memory
    during instantiation.  Large files are read
    incrementally from disk.  The file size threshold
    and the increment size values are defined in
    WvIn.h.

    WvIn currently supports WAV, AIFF, SND (AU),
    MAT-file (Matlab), and STK RAW file formats.
    Signed integer (8-, 16-, and 32-bit) and floating-
    point (32- and 64-bit) data types are supported.
    Uncompressed data types are not supported.  If
    using MAT-files, data should be saved in an array
    with each data channel filling a matrix row.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


// Files larger than CHUNK_THRESHOLD will be copied into memory
// in CHUNK_SIZE increments, rather than completely loaded into
// a buffer at once.

//#define CHUNK_THRESHOLD 5000000  // 5 Mb
//#define CHUNK_SIZE 1024          // sample frames


class WvIn : public Stk
{
    public:
    //! Default constructor.
    WvIn();

    //! Overloaded constructor for file input.
    /*!
      An StkError will be thrown if the file is not found, its format is
      unknown, or a read error occurs.
    */
    WvIn( const char *fileName, bool raw = FALSE, bool doNormalize = TRUE, bool generate=true );

    //! Class destructor.
    virtual ~WvIn();

    //! Open the specified file and load its data.
    /*!
      An StkError will be thrown if the file is not found, its format is
      unknown, or a read error occurs.
    */
    virtual void openFile( const char *fileName, bool raw = FALSE, bool doNormalize = TRUE, bool generate = true );

    //! If a file is open, close it.
    void closeFile(void);

    //! Clear outputs and reset time (file pointer) to zero.
    void reset(void);

    //! Normalize data to a maximum of +-1.0.
    /*!
      For large, incrementally loaded files with integer data types,
      normalization is computed relative to the data type maximum.
      No normalization is performed for incrementally loaded files
      with floating-point data types.
    */
    void normalize(void);

    //! Normalize data to a maximum of \e +-peak.
    /*!
      For large, incrementally loaded files with integer data types,
      normalization is computed relative to the data type maximum
      (\e peak/maximum).  For incrementally loaded files with floating-
      point data types, direct scaling by \e peak is performed.
    */
    void normalize(MY_FLOAT peak);

    //! Return the file size in sample frames.
    unsigned long getSize(void) const;

    //! Return the number of audio channels in the file.
    unsigned int getChannels(void) const;

    //! Return the input file sample rate in Hz (not the data read rate).
    /*!
      WAV, SND, and AIF formatted files specify a sample rate in
      their headers.  STK RAW files have a sample rate of 22050 Hz
      by definition.  MAT-files are assumed to have a rate of 44100 Hz.
    */
    MY_FLOAT getFileRate(void) const;

    //! Query whether reading is complete.
    bool isFinished(void) const;

    //! Set the data read rate in samples.  The rate can be negative.
    /*!
      If the rate value is negative, the data is read in reverse order.
    */
    void setRate(MY_FLOAT aRate);

    //! Increment the read pointer by \e aTime samples.
    virtual void addTime(MY_FLOAT aTime);

    //! Turn linear interpolation on/off.
    /*!
      Interpolation is automatically off when the read rate is
      an integer value.  If interpolation is turned off for a
      fractional rate, the time index is truncated to an integer
      value.
    */
    void setInterpolate(bool doInterpolate);

    //! Return the average across the last output sample frame.
    virtual MY_FLOAT lastOut(void) const;

    //! Read out the average across one sample frame of data.
    /*!
      An StkError will be thrown if a file is read incrementally and a read error occurs.
    */
    virtual MY_FLOAT tick(void);

    //! Read out vectorSize averaged sample frames of data in \e vector.
    /*!
      An StkError will be thrown if a file is read incrementally and a read error occurs.
    */
    virtual MY_FLOAT *tick(MY_FLOAT *vector, unsigned int vectorSize);

    //! Return a pointer to the last output sample frame.
    virtual const MY_FLOAT *lastFrame(void) const;

    //! Return a pointer to the next sample frame of data.
    /*!
      An StkError will be thrown if a file is read incrementally and a read error occurs.
    */
    virtual const MY_FLOAT *tickFrame(void);

    //! Read out sample \e frames of data to \e frameVector.
    /*!
      An StkError will be thrown if a file is read incrementally and a read error occurs.
    */
    virtual MY_FLOAT *tickFrame(MY_FLOAT *frameVector, unsigned int frames);

    public: // SWAP formerly protected

    // Initialize class variables.
    void init( void );

    // Read file data.
    virtual void readData(unsigned long index);

    // Get STK RAW file information.
    bool getRawInfo( const char *fileName );

    // Get WAV file header information.
    bool getWavInfo( const char *fileName );

    // Get SND (AU) file header information.
    bool getSndInfo( const char *fileName );

    // Get AIFF file header information.
    bool getAifInfo( const char *fileName );

    // Get MAT-file header information.
    bool getMatInfo( const char *fileName );

    char msg[256];
    // char m_filename[256]; // chuck data
    Chuck_String str_filename; // chuck data
    FILE *fd;
    MY_FLOAT *data;
    MY_FLOAT *lastOutput;
    bool chunking;
    bool finished;
    bool interpolate;
    bool byteswap;
    unsigned long fileSize;
    unsigned long bufferSize;
    unsigned long dataOffset;
    unsigned int channels;
    long chunkPointer;
    STK_FORMAT dataType;
    MY_FLOAT fileRate;
    MY_FLOAT gain;
    MY_FLOAT time;
    MY_FLOAT rate;
    public:
    bool m_loaded;
};



/***************************************************/
/*! \class WaveLoop
    \brief STK waveform oscillator class.

    This class inherits from WvIn and provides
    audio file looping functionality.

    WaveLoop supports multi-channel data in
    interleaved format.  It is important to
    distinguish the tick() methods, which return
    samples produced by averaging across sample
    frames, from the tickFrame() methods, which
    return pointers to multi-channel sample frames.
    For single-channel data, these methods return
    equivalent values.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class WaveLoop : public WvIn
{
    public:
    WaveLoop( );
    //! Class constructor.
    WaveLoop( const char *fileName, bool raw = FALSE, bool generate = true );

    virtual void openFile( const char * fileName, bool raw = FALSE, bool n = TRUE );

    //! Class destructor.
    virtual ~WaveLoop();

    //! Set the data interpolation rate based on a looping frequency.
    /*!
      This function determines the interpolation rate based on the file
      size and the current Stk::sampleRate.  The \e aFrequency value
      corresponds to file cycles per second.  The frequency can be
      negative, in which case the loop is read in reverse order.
     */
    void setFrequency(MY_FLOAT aFrequency);

    //! Increment the read pointer by \e aTime samples, modulo file size.
    void addTime(MY_FLOAT aTime);

    //! Increment current read pointer by \e anAngle, relative to a looping frequency.
    /*!
      This function increments the read pointer based on the file
      size and the current Stk::sampleRate.  The \e anAngle value
      is a multiple of file size.
     */
    void addPhase(MY_FLOAT anAngle);

    //! Add a phase offset to the current read pointer.
    /*!
      This function determines a time offset based on the file
      size and the current Stk::sampleRate.  The \e anAngle value
      is a multiple of file size.
     */
    void addPhaseOffset(MY_FLOAT anAngle);

    //! Return a pointer to the next sample frame of data.
    const MY_FLOAT *tickFrame(void);

    public:

    // Read file data.
    void readData(unsigned long index);
    MY_FLOAT phaseOffset;
    MY_FLOAT m_freq; // chuck data;
};





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

Blit:: Blit( double frequency )
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

BlitSaw:: BlitSaw( double frequency )
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

BlitSquare:: BlitSquare( double frequency )
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
/*! \class Chorus
    \brief STK chorus effect class.

    This class implements a chorus effect.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

Chorus :: Chorus(double baseDelay)
{
    delayLine[0] = delayLine[1] = NULL;
    mods[0] = new WaveLoop( "special:sinewave", TRUE );
    mods[1] = NULL;
    set(baseDelay, 4);
    setDelay( baseDelay );
    setModDepth( .5 );
    setModFrequency( .25 );

    // Concatenate the STK rawwave path to the rawwave file
    // mods[0] = new WaveLoop( "special:sinewave", TRUE );
    // mods[1] = new WaveLoop( "special:sinewave", TRUE );
    // mods[0]->setFrequency(0.2);
    // mods[1]->setFrequency(0.222222);
    effectMix = 0.5;
}

Chorus :: ~Chorus()
{
    SAFE_DELETE( delayLine[0] );
    SAFE_DELETE( delayLine[1] );
    SAFE_DELETE( mods[0] );
    SAFE_DELETE( mods[1] );
}

// chuck
void Chorus :: set(double baseDelay, double depth)
{
    SAFE_DELETE( delayLine[0] );
    SAFE_DELETE( delayLine[1] );

    delayLine[0] = new DelayL((long) baseDelay, (long) (baseDelay + baseDelay * depth) + 2);
    // delayLine[0] = new DelayL((long) baseDelay, (long) (baseDelay + baseDelay * 1.414 * depth) + 2);
    // delayLine[1] = new DelayL((long) baseDelay, (long) (baseDelay + baseDelay * depth) + 2);

    this->clear();
}

void Chorus :: clear()
{
    delayLine[0]->clear();
    // delayLine[1]->clear();
    lastOutput[0] = 0.0;
    lastOutput[1] = 0.0;
}

void Chorus :: setEffectMix(double mix)
{
    effectMix = mix;
    if ( mix < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Chorus: setEffectMix parameter is less than zero!" << CK_STDENDL;
        effectMix = 0.0;
    }
    else if ( mix > 1.0 )
    {
        CK_STDCERR << "[chuck](via STK): Chorus: setEffectMix parameter is greater than 1.0!" << CK_STDENDL;
        effectMix = 1.0;
    }
}

void Chorus :: setModDepth(double depth)
{
    modDepth = depth;
}

void Chorus :: setDelay(double baseDelay)
{
    baseLength = baseDelay;
}

void Chorus :: setModFrequency(double frequency)
{
    mods[0]->setFrequency(frequency);
    // mods[1]->setFrequency(frequency * 1.1111);
}

double Chorus :: lastOut() const
{
//  return (lastOutput[0] + lastOutput[1]) * (double) 0.5;
    return lastOutput[0];
}

double Chorus :: lastOutLeft() const
{
    return lastOutput[0];
}

double Chorus :: lastOutRight() const
{
    return lastOutput[1];
}

double Chorus :: tick(double input)
{
    delayLine[0]->setDelay(baseLength * modDepth * .5 * (1.0 + mods[0]->tick()));
    // delayLine[0]->setDelay(baseLength * 0.707 * modDepth * (1.0 + mods[0]->tick()));
    // delayLine[1]->setDelay(baseLength  * 0.5 * modDepth * (1.0 + mods[1]->tick()));
    lastOutput[0] = input * (1.0 - effectMix);
    lastOutput[0] += effectMix * delayLine[0]->tick(input);
    // lastOutput[1] = input * (1.0 - effectMix);
    // lastOutput[1] += effectMix * delayLine[1]->tick(input);
    // return (lastOutput[0] + lastOutput[1]) * (double) 0.5;
    return lastOutput[0];
}

double *Chorus :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class Delay
    \brief STK non-interpolating delay line class.

    This protected Filter subclass implements
    a non-interpolating digital delay-line.
    A fixed maximum length of 4095 and a delay
    of zero is set using the default constructor.
    Alternatively, the delay and maximum length
    can be set during instantiation with an
    overloaded constructor.

    A non-interpolating delay line is typically
    used in fixed delay-length applications, such
    as for reverberation.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

Delay :: Delay()
{
    this->set( 0, 4096 );
}

Delay :: Delay(long theDelay, long maxDelay)
{
    this->set( theDelay, maxDelay );
}

void Delay :: set( long delay, long max )
{
    // Writing before reading allows delays from 0 to length-1.
    // If we want to allow a delay of maxDelay, we need a
    // delay-line of length = maxDelay+1.
    length = max+1;

    // We need to delete the previously allocated inputs.
    if( inputs ) delete [] inputs;
    inputs = new double[length];
    this->clear();

    inPoint = 0;
    this->setDelay(delay);
}

Delay :: ~Delay()
{
}

void Delay :: clear(void)
{
    long i;
    for (i=0; i<length; i++) inputs[i] = 0.0;
    outputs[0] = 0.0;
}

void Delay :: setDelay(long theDelay)
{
    if (theDelay > length-1)   // The value is too big.
    {
        CK_STDCERR << "[chuck](via STK): Delay: setDelay(" << theDelay << ") too big!" << CK_STDENDL;
        // Force delay to maxLength.
        outPoint = inPoint + 1;
        delay = length - 1;
    }
    else if (theDelay < 0 )
    {
        CK_STDCERR << "[chuck](via STK): Delay: setDelay(" << theDelay << ") less than zero!" << CK_STDENDL;
        outPoint = inPoint;
        delay = 0;
    }
    else
    {
        outPoint = inPoint - (long) theDelay;  // read chases write
        delay = theDelay;
    }

    while (outPoint < 0) outPoint += length;  // modulo maximum length
}

double Delay :: getDelay(void) const
{
    return delay;
}

double Delay :: energy(void) const
{
    int i;
    register double e = 0;
    if (inPoint >= outPoint)
    {
        for (i=outPoint; i<inPoint; i++)
        {
            register double t = inputs[i];
            e += t*t;
        }
    }
    else
    {
        for (i=outPoint; i<length; i++)
        {
            register double t = inputs[i];
            e += t*t;
        }
        for (i=0; i<inPoint; i++)
        {
            register double t = inputs[i];
            e += t*t;
        }
    }
    return e;
}

double Delay :: contentsAt(unsigned long tapDelay) const
{
    long i = tapDelay;
    if (i < 1)
    {
        CK_STDCERR << "[chuck](via STK): Delay: contentsAt(" << tapDelay << ") too small!" << CK_STDENDL;
        i = 1;
    }
    else if (i > delay)
    {
        CK_STDCERR << "[chuck](via STK): Delay: contentsAt(" << tapDelay << ") too big!" << CK_STDENDL;
        i = (long) delay;
    }

    long tap = inPoint - i;
    if (tap < 0) // Check for wraparound.
        tap += length;

    return inputs[tap];
}

double Delay :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double Delay :: nextOut(void) const
{
    return inputs[outPoint];
}

double Delay :: tick(double sample)
{
    inputs[inPoint++] = sample;

    // Check for end condition
    if (inPoint == length)
        inPoint -= length;

    // Read out next value
    outputs[0] = inputs[outPoint++];

    if (outPoint>=length)
        outPoint -= length;

    return outputs[0];
}

double *Delay :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class DelayA
    \brief STK allpass interpolating delay line class.

    This Delay subclass implements a fractional-
    length digital delay-line using a first-order
    allpass filter.  A fixed maximum length
    of 4095 and a delay of 0.5 is set using the
    default constructor.  Alternatively, the
    delay and maximum length can be set during
    instantiation with an overloaded constructor.

    An allpass filter has unity magnitude gain but
    variable phase delay properties, making it useful
    in achieving fractional delays without affecting
    a signal's frequency magnitude response.  In
    order to achieve a maximally flat phase delay
    response, the minimum delay possible in this
    implementation is limited to a value of 0.5.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

DelayA :: DelayA()
{
    this->setDelay( 0.5 );
    apInput = 0.0;
    doNextOut = true;
}

DelayA :: DelayA(double theDelay, long maxDelay)
{
    // Writing before reading allows delays from 0 to length-1.
    length = maxDelay+1;

    if ( length > 4096 )
    {
        // We need to delete the previously allocated inputs.
        delete [] inputs;
        inputs = new double[length];
        this->clear();
    }

    inPoint = 0;
    this->setDelay(theDelay);
    apInput = 0.0;
    doNextOut = true;
}

void DelayA :: set( double delay, long max )
{
    // Writing before reading allows delays from 0 to length-1.
    // If we want to allow a delay of maxDelay, we need a
    // delay-line of length = maxDelay+1.
    length = max+1;

    // We need to delete the previously allocated inputs.
    if( inputs ) delete [] inputs;
    inputs = new double[length];
    this->clear();

    inPoint = 0;
    this->setDelay(delay);
    apInput = 0.0;
    doNextOut = true;
}

DelayA :: ~DelayA()
{
}

void DelayA :: clear()
{
    Delay::clear();
    apInput = 0.0;
}

void DelayA :: setDelay(double theDelay)
{
    double outPointer;

    if (theDelay > length-1)
    {
        CK_STDCERR << "[chuck](via STK): DelayA: setDelay(" << theDelay << ") too big!" << CK_STDENDL;
        // Force delay to maxLength
        outPointer = inPoint + 1.0;
        delay = length - 1;
    }
    else if (theDelay < 0.5)
    {
        CK_STDCERR << "[chuck](via STK): DelayA: setDelay(" << theDelay << ") less than 0.5 not possible!" << CK_STDENDL;
        outPointer = inPoint + 0.4999999999;
        delay = 0.5;
    }
    else
    {
        outPointer = inPoint - theDelay + 1.0;     // outPoint chases inpoint
        delay = theDelay;
    }

    if (outPointer < 0)
        outPointer += length;  // modulo maximum length

    outPoint = (long) outPointer;        // integer part
    alpha = 1.0 + outPoint - outPointer; // fractional part

    if (alpha < 0.5)
    {
        // The optimal range for alpha is about 0.5 - 1.5 in order to
        // achieve the flattest phase delay response.
        outPoint += 1;
        if (outPoint >= length) outPoint -= length;
        alpha += (double) 1.0;
    }

    coeff = ((double) 1.0 - alpha) /
            ((double) 1.0 + alpha);         // coefficient for all pass
}

double DelayA :: nextOut(void)
{
    if ( doNextOut )
    {
        // Do allpass interpolation delay.
        nextOutput = -coeff * outputs[0];
        nextOutput += apInput + (coeff * inputs[outPoint]);
        doNextOut = false;
    }

    return nextOutput;
}

double DelayA :: tick(double sample)
{
    inputs[inPoint++] = sample;

    // Increment input pointer modulo length.
    if (inPoint == length)
        inPoint -= length;

    outputs[0] = nextOut();
    doNextOut = true;

    // Save the allpass input and increment modulo length.
    apInput = inputs[outPoint++];
    if (outPoint == length)
        outPoint -= length;

    return outputs[0];
}


/***************************************************/
/*! \class DelayL
    \brief STK linear interpolating delay line class.

    This Delay subclass implements a fractional-
    length digital delay-line using first-order
    linear interpolation.  A fixed maximum length
    of 4095 and a delay of zero is set using the
    default constructor.  Alternatively, the
    delay and maximum length can be set during
    instantiation with an overloaded constructor.

    Linear interpolation is an efficient technique
    for achieving fractional delay lengths, though
    it does introduce high-frequency signal
    attenuation to varying degrees depending on the
    fractional delay setting.  The use of higher
    order Lagrange interpolators can typically
    improve (minimize) this attenuation characteristic.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

DelayL :: DelayL()
{
    doNextOut = true;
}

DelayL :: DelayL(double theDelay, long maxDelay)
{
    // Writing before reading allows delays from 0 to length-1.
    length = maxDelay+1;

    if ( length > 4096 )
    {
        // We need to delete the previously allocated inputs.
        delete [] inputs;
        inputs = new double[length];
        this->clear();
    }

    inPoint = 0;
    this->setDelay(theDelay);
    doNextOut = true;
}

DelayL :: ~DelayL()
{
}

void DelayL :: set( double delay, long max )
{
    // Writing before reading allows delays from 0 to length-1.
    // If we want to allow a delay of maxDelay, we need a
    // delay-line of length = maxDelay+1.
    length = max+1;

    // We need to delete the previously allocated inputs.
    if( inputs ) delete [] inputs;
    inputs = new double[length];
    this->clear();

    inPoint = 0;
    this->setDelay(delay);
    doNextOut = true;
}

void DelayL :: setDelay(double theDelay)
{
    double outPointer;

    if (theDelay > length-1)
    {
        CK_STDCERR << "[chuck](via STK): DelayL: setDelay(" << theDelay << ") too big!" << CK_STDENDL;
        // Force delay to maxLength
        outPointer = inPoint + 1.0;
        delay = length - 1;
    }
    else if (theDelay < 0 )
    {
        CK_STDCERR << "[chuck](via STK): DelayL: setDelay(" << theDelay << ") less than zero!" << CK_STDENDL;
        outPointer = inPoint;
        delay = 0;
    }
    else
    {
        outPointer = inPoint - theDelay;  // read chases write
        delay = theDelay;
    }

    while (outPointer < 0)
        outPointer += length; // modulo maximum length

    outPoint = (long) outPointer;  // integer part
    alpha = outPointer - outPoint; // fractional part
    omAlpha = (double) 1.0 - alpha;
}

double DelayL :: nextOut(void)
{
    if ( doNextOut )
    {
        // First 1/2 of interpolation
        nextOutput = inputs[outPoint] * omAlpha;
        // Second 1/2 of interpolation
        if (outPoint+1 < length)
            nextOutput += inputs[outPoint+1] * alpha;
        else
            nextOutput += inputs[0] * alpha;
        doNextOut = false;
    }

    return nextOutput;
}

double DelayL :: tick(double sample)
{
    inputs[inPoint++] = sample;

    // Increment input pointer modulo length.
    if (inPoint == length)
        inPoint -= length;

    outputs[0] = nextOut();
    doNextOut = true;

    // Increment output pointer modulo length.
    if (++outPoint >= length)
        outPoint -= length;

    return outputs[0];
}


/***************************************************/
/*! \class Drummer
    \brief STK drum sample player class.

    This class implements a drum sampling
    synthesizer using WvIn objects and one-pole
    filters.  The drum rawwave files are sampled
    at 22050 Hz, but will be appropriately
    interpolated for other sample rates.  You can
    specify the maximum polyphony (maximum number
    of simultaneous voices) via a #define in the
    Drummer.h.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

// Not really General MIDI yet.  Coming soon.
unsigned char genMIDIMap[128] =
{
    0,0,0,0,0,0,0,0,        // 0-7
    0,0,0,0,0,0,0,0,        // 8-15
    0,0,0,0,0,0,0,0,        // 16-23
    0,0,0,0,0,0,0,0,        // 24-31
    0,0,0,0,1,0,2,0,        // 32-39
    2,3,6,3,6,4,7,4,        // 40-47
    5,8,5,0,0,0,10,0,       // 48-55
    9,0,0,0,0,0,0,0,        // 56-63
    0,0,0,0,0,0,0,0,        // 64-71
    0,0,0,0,0,0,0,0,        // 72-79
    0,0,0,0,0,0,0,0,        // 80-87
    0,0,0,0,0,0,0,0,        // 88-95
    0,0,0,0,0,0,0,0,        // 96-103
    0,0,0,0,0,0,0,0,        // 104-111
    0,0,0,0,0,0,0,0,        // 112-119
    0,0,0,0,0,0,0,0     // 120-127
};

//XXX changed this from 16 to 32 for the 'special' convention..also, we do not have these linked
//in the headers
char waveNames[DRUM_NUMWAVES][32] =
{
    "special:dope",
    "special:bassdrum",
    "special:snardrum",
    "special:tomlowdr",
    "special:tommiddr",
    "special:tomhidrm",
    "special:hihatcym",
    "special:ridecymb",
    "special:crashcym",
    "special:cowbell1",
    "special:tambourn"
};

Drummer :: Drummer() : Instrmnt()
{
    for (int i=0; i<DRUM_POLYPHONY; i++)
    {
        filters[i] = new OnePole;
        sounding[i] = -1;
    }

    // This counts the number of sounding voices.
    nSounding = 0;
}

Drummer :: ~Drummer()
{
    int i;
    for ( i=0; i<nSounding-1; i++ ) delete waves[i];
    for ( i=0; i<DRUM_POLYPHONY; i++ ) delete filters[i];
}

void Drummer :: noteOn(double instrument, double amplitude)
{
    double gain = amplitude;
    if ( amplitude > 1.0 )
    {
        CK_STDCERR << "[chuck](via STK): Drummer: noteOn amplitude parameter is greater than 1.0!" << CK_STDENDL;
        gain = 1.0;
    }
    else if ( amplitude < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Drummer: noteOn amplitude parameter is less than 0.0!" << CK_STDENDL;
        return;
    }

    // Yes, this is tres kludgey.
    int noteNum = (int) ((12*log(instrument/220.0)/log(2.0)) + 57.01);

    // Check first to see if there's already one like this sounding.
    int i, waveIndex = -1;
    for (i=0; i<DRUM_POLYPHONY; i++)
    {
        if (sounding[i] == noteNum) waveIndex = i;
    }

    if ( waveIndex >= 0 )
    {
        // Reset this sound.
        waves[waveIndex]->reset();
        filters[waveIndex]->setPole((double) 0.999 - (gain * 0.6));
        filters[waveIndex]->setGain(gain);
    }
    else
    {
        if (nSounding == DRUM_POLYPHONY)
        {
            // If we're already at maximum polyphony, then preempt the oldest voice.
            delete waves[0];
            filters[0]->clear();
            WvIn *tempWv = waves[0];
            OnePole *tempFilt = filters[0];
            // Re-order the list.
            for (i=0; i<DRUM_POLYPHONY-1; i++)
            {
                waves[i] = waves[i+1];
                filters[i] = filters[i+1];
            }
            waves[DRUM_POLYPHONY-1] = tempWv;
            filters[DRUM_POLYPHONY-1] = tempFilt;
        }
        else
            nSounding += 1;

        sounding[nSounding-1] = noteNum;
        // Concatenate the STK rawwave path to the rawwave file
        waves[nSounding-1] = new WvIn( (Stk::rawwavePath() + waveNames[genMIDIMap[noteNum]]).c_str(), TRUE );
        if (SynthCommon.SAMPLE_RATE != 22050.0)
            waves[nSounding-1]->setRate( 22050.0 / SynthCommon.SAMPLE_RATE );
        filters[nSounding-1]->setPole((double) 0.999 - (gain * 0.6) );
        filters[nSounding-1]->setGain( gain );
    }
}

void Drummer :: noteOff(double amplitude)
{
    // Set all sounding wave filter gains low.
    int i = 0;
    while(i < nSounding)
    {
        filters[i++]->setGain( amplitude * 0.01 );
    }
}

double Drummer :: tick()
{
    double output = 0.0;
    OnePole *tempFilt;

    int j, i = 0;
    while (i < nSounding)
    {
        if ( waves[i]->isFinished() )
        {
            delete waves[i];
            tempFilt = filters[i];
            // Re-order the list.
            for (j=i; j<nSounding-1; j++)
            {
                sounding[j] = sounding[j+1];
                waves[j] = waves[j+1];
                filters[j] = filters[j+1];
            }
            filters[j] = tempFilt;
            filters[j]->clear();
            sounding[j] = -1;
            nSounding -= 1;
            i -= 1;
        }
        else
            output += filters[i]->tick( waves[i]->tick() );
        i++;
    }

    return output;
}


/***************************************************/
/*! \class Echo
    \brief STK echo effect class.

    This class implements a echo effect.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Echo :: Echo(double longestDelay)
{
    delayLine = NULL;
    this->set( longestDelay );
    effectMix = 0.5;
}

Echo :: ~Echo()
{
    delete delayLine;
}

void Echo :: set( double max )
{
    length = (long)max + 2;
    double delay = delayLine ? delayLine->getDelay() : length>>1;
    if( delayLine ) delete delayLine;
    if( delay >= max ) delay = max;
    delayLine = new Delay(length>>1, length);
    this->clear();
    this->setDelay(delay+.5);
}

double Echo :: getDelay()
{
    return delayLine->getDelay();
}

void Echo :: clear()
{
    delayLine->clear();
    lastOutput = 0.0;
}

void Echo :: setDelay(double delay)
{
    double size = delay;
    if ( delay < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Echo: setDelay parameter is less than zero!" << CK_STDENDL;
        size = 0.0;
    }
    else if ( delay > length )
    {
        CK_STDCERR << "[chuck](via STK): Echo: setDelay parameter is greater than delay length!" << CK_STDENDL;
        size = length;
    }

    delayLine->setDelay((long)size);
}

void Echo :: setEffectMix(double mix)
{
    effectMix = mix;
    if ( mix < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Echo: setEffectMix parameter is less than zero!" << CK_STDENDL;
        effectMix = 0.0;
    }
    else if ( mix > 1.0 )
    {
        CK_STDCERR << "[chuck](via STK): Echo: setEffectMix parameter is greater than 1.0!" << CK_STDENDL;
        effectMix = 1.0;
    }
}

double Echo :: lastOut() const
{
    return lastOutput;
}

double Echo :: tick(double input)
{
    lastOutput = effectMix * delayLine->tick(input);
    lastOutput += input * (1.0 - effectMix);
    return lastOutput;
}

double *Echo :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}




/***************************************************/
/*! \class Filter
    \brief STK filter class.

    This class implements a generic structure which
    can be used to create a wide range of filters.
    It can function independently or be subclassed
    to provide more specific controls based on a
    particular filter type.

    In particular, this class implements the standard
    difference equation:

    a[0]*y[n] = b[0]*x[n] + ... + b[nb]*x[n-nb] -
                a[1]*y[n-1] - ... - a[na]*y[n-na]

    If a[0] is not equal to 1, the filter coeffcients
    are normalized by a[0].

    The \e gain parameter is applied at the filter
    input and does not affect the coefficient values.
    The default gain value is 1.0.  This structure
    results in one extra multiply per computed sample,
    but allows easy control of the overall filter gain.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

FilterStk :: FilterStk()
{
    // The default constructor should setup for pass-through.
    gain = 1.0;
    nB = 1;
    nA = 1;
    b = new double[nB];
    b[0] = 1.0;
    a = new double[nA];
    a[0] = 1.0;

    inputs = new double[nB];
    outputs = new double[nA];
    this->clear();
}

FilterStk :: FilterStk(int nb, double *bCoefficients, int na, double *aCoefficients)
{
    char message[256];

    // Check the arguments.
    if ( nb < 1 || na < 1 )
    {
        sprintf(message, "[chuck](via Filter): nb (%d) and na (%d) must be >= 1!", nb, na);
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if ( aCoefficients[0] == 0.0 )
    {
        sprintf(message, "[chuck](via Filter): a[0] coefficient cannot == 0!");
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    gain = 1.0;
    nB = nb;
    nA = na;
    b = new double[nB];
    a = new double[nA];

    inputs = new double[nB];
    outputs = new double[nA];
    this->clear();

    this->setCoefficients(nB, bCoefficients, nA, aCoefficients);
}

FilterStk :: ~FilterStk()
{
    delete [] b;
    delete [] a;
    delete [] inputs;
    delete [] outputs;
}

void FilterStk :: clear(void)
{
    int i;
    for (i=0; i<nB; i++)
        inputs[i] = 0.0;
    for (i=0; i<nA; i++)
        outputs[i] = 0.0;
}

void FilterStk :: setCoefficients(int nb, double *bCoefficients, int na, double *aCoefficients)
{
    int i;
    char message[256];

    // Check the arguments.
    if ( nb < 1 || na < 1 )
    {
        sprintf(message, "[chuck](via Filter): nb (%d) and na (%d) must be >= 1!", nb, na);
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if ( aCoefficients[0] == 0.0 )
    {
        sprintf(message, "[chuck](via Filter): a[0] coefficient cannot == 0!");
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if (nb != nB)
    {
        delete [] b;
        delete [] inputs;
        nB = nb;
        b = new double[nB];
        inputs = new double[nB];
        for (i=0; i<nB; i++) inputs[i] = 0.0;
    }

    if (na != nA)
    {
        delete [] a;
        delete [] outputs;
        nA = na;
        a = new double[nA];
        outputs = new double[nA];
        for (i=0; i<nA; i++) outputs[i] = 0.0;
    }

    for (i=0; i<nB; i++)
        b[i] = bCoefficients[i];
    for (i=0; i<nA; i++)
        a[i] = aCoefficients[i];

    // scale coefficients by a[0] if necessary
    if (a[0] != 1.0)
    {
        for (i=0; i<nB; i++)
            b[i] /= a[0];
        for (i=0; i<nA; i++)
            a[i] /= a[0];
    }
}

void FilterStk :: setNumerator(int nb, double *bCoefficients)
{
    int i;
    char message[256];

    // Check the arguments.
    if ( nb < 1 )
    {
        sprintf(message, "[chuck](via Filter): nb (%d) must be >= 1!", nb);
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if (nb != nB)
    {
        delete [] b;
        delete [] inputs;
        nB = nb;
        b = new double[nB];
        inputs = new double[nB];
        for (i=0; i<nB; i++) inputs[i] = 0.0;
    }

    for (i=0; i<nB; i++)
        b[i] = bCoefficients[i];
}

void FilterStk :: setDenominator(int na, double *aCoefficients)
{
    int i;
    char message[256];

    // Check the arguments.
    if ( na < 1 )
    {
        sprintf(message, "[chuck](via Filter): na (%d) must be >= 1!", na);
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if ( aCoefficients[0] == 0.0 )
    {
        sprintf(message, "[chuck](via Filter): a[0] coefficient cannot == 0!");
        handleError( message, StkError::FUNCTION_ARGUMENT );
    }

    if (na != nA)
    {
        delete [] a;
        delete [] outputs;
        nA = na;
        a = new double[nA];
        outputs = new double[nA];
        for (i=0; i<nA; i++) outputs[i] = 0.0;
    }

    for (i=0; i<nA; i++)
        a[i] = aCoefficients[i];

    // scale coefficients by a[0] if necessary

    if (a[0] != 1.0)
    {
        for (i=0; i<nB; i++)
            b[i] /= a[0];
        for (i=0; i<nA; i++)
            a[i] /= a[0];
    }
}

void FilterStk :: setGain(double theGain)
{
    gain = theGain;
}

double FilterStk :: getGain(void) const
{
    return gain;
}

double FilterStk :: lastOut(void) const
{
    return outputs[0];
}

double FilterStk :: tick(double sample)
{
    int i;

    outputs[0] = 0.0;
    inputs[0] = gain * sample;
    for (i=nB-1; i>0; i--)
    {
        outputs[0] += b[i] * inputs[i];
        inputs[i] = inputs[i-1];
    }
    outputs[0] += b[0] * inputs[0];

    for (i=nA-1; i>0; i--)
    {
        outputs[0] += -a[i] * outputs[i];
        outputs[i] = outputs[i-1];
    }

    return outputs[0];
}

double *FilterStk :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}



/***************************************************/
/*! \class FormSwep
    \brief STK sweepable formant filter class.

    This public BiQuad filter subclass implements
    a formant (resonance) which can be "swept"
    over time from one frequency setting to another.
    It provides methods for controlling the sweep
    rate and target frequency.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


FormSwep :: FormSwep() : BiQuad()
{
    frequency = (double) 0.0;
    radius = (double) 0.0;
    targetGain = (double) 1.0;
    targetFrequency = (double) 0.0;
    targetRadius = (double) 0.0;
    deltaGain = (double) 0.0;
    deltaFrequency = (double) 0.0;
    deltaRadius = (double) 0.0;
    sweepState = (double)  0.0;
    sweepRate = (double) 0.002;
    dirty = false;
    this->clear();
}

FormSwep :: ~FormSwep()
{
}

void FormSwep :: setResonance(double aFrequency, double aRadius)
{
    dirty = false;
    radius = aRadius;
    frequency = aFrequency;

    BiQuad::setResonance( frequency, radius, true );
}

void FormSwep :: setStates(double aFrequency, double aRadius, double aGain)
{
    dirty = false;

    if ( frequency != aFrequency || radius != aRadius )
        BiQuad::setResonance( aFrequency, aRadius, true );

    frequency = aFrequency;
    radius = aRadius;
    gain = aGain;
    targetFrequency = aFrequency;
    targetRadius = aRadius;
    targetGain = aGain;
}

void FormSwep :: setTargets(double aFrequency, double aRadius, double aGain)
{
    dirty = true;
    startFrequency = frequency;
    startRadius = radius;
    startGain = gain;
    targetFrequency = aFrequency;
    targetRadius = aRadius;
    targetGain = aGain;
    deltaFrequency = aFrequency - frequency;
    deltaRadius = aRadius - radius;
    deltaGain = aGain - gain;
    sweepState = (double) 0.0;
}

void FormSwep :: setSweepRate(double aRate)
{
    sweepRate = aRate;
    if ( sweepRate > 1.0 ) sweepRate = 1.0;
    if ( sweepRate < 0.0 ) sweepRate = 0.0;
}

void FormSwep :: setSweepTime(double aTime)
{
    sweepRate = 1.0 / ( aTime * SynthCommon.SAMPLE_RATE );
    if ( sweepRate > 1.0 ) sweepRate = 1.0;
    if ( sweepRate < 0.0 ) sweepRate = 0.0;
}

double FormSwep :: tick(double sample)
{
    if (dirty)
    {
        sweepState += sweepRate;
        if ( sweepState >= 1.0 )
        {
            sweepState = (double) 1.0;
            dirty = false;
            radius = targetRadius;
            frequency = targetFrequency;
            gain = targetGain;
        }
        else
        {
            radius = startRadius + (deltaRadius * sweepState);
            frequency = startFrequency + (deltaFrequency * sweepState);
            gain = startGain + (deltaGain * sweepState);
        }
        BiQuad::setResonance( frequency, radius, true );
    }

    return BiQuad::tick( sample );
}

double *FormSwep :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}



/***************************************************/
/*! \class Instrmnt
    \brief STK instrument abstract base class.

    This class provides a common interface for
    all STK instruments.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Instrmnt :: Instrmnt()
{
    m_frequency = 0;
}

Instrmnt :: ~Instrmnt()
{
}

void Instrmnt :: setFrequency(double frequency)
{
    CK_STDCERR << "[chuck](via STK): Instrmnt: virtual setFrequency function call!" << CK_STDENDL;
    // m_frequency = frequency;
}

double Instrmnt :: lastOut() const
{
    return lastOutput;
}

// Support for stereo output:
double Instrmnt :: lastOutLeft(void) const
{
    return 0.5 * lastOutput;
}

double Instrmnt :: lastOutRight(void) const
{
    return 0.5 * lastOutput;
}

double *Instrmnt :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick();

    return vec;
}

/*
TODO :  let's add this as two function in Chuck.
     :  one version is a ( int , float ) for the midi messages
     :  the second is a ( string, float ) that does a quick binary search
     :  into the skini table for the __SK_ value, and dispatches the proper function
     :  hoohoo!

     - and then everyone can inherit from Instrmnt like the good Lord intended.
     - pld
*/

void Instrmnt :: controlChange(int number, double value)
{
}

/***************************************************/
/*! \class JCRev
    \brief John Chowning's reverberator class.

    This class is derived from the CLM JCRev
    function, which is based on the use of
    networks of simple allpass and comb delay
    filters.  This class implements three series
    allpass units, followed by four parallel comb
    filters, and two decorrelation delay lines in
    parallel at the output.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


JCRev :: JCRev(double T60)
{
    // Delay lengths for 44100 Hz sample rate.
    int lengths[9] = {1777, 1847, 1993, 2137, 389, 127, 43, 211, 179};
    double scaler = SynthCommon.SAMPLE_RATE / 44100.0;

    int delay, i;
    if ( scaler != 1.0 )
    {
        for (i=0; i<9; i++)
        {
            delay = (int) floor(scaler * lengths[i]);
            if ( (delay & 1) == 0) delay++;
            while ( !this->isPrime(delay) ) delay += 2;
            lengths[i] = delay;
        }
    }

    for (i=0; i<3; i++)
        allpassDelays[i] = new Delay(lengths[i+4], lengths[i+4]);

    for (i=0; i<4; i++)
    {
        combDelays[i] = new Delay(lengths[i], lengths[i]);
        combCoefficient[i] = pow(10.0,(-3 * lengths[i] / (T60 * SynthCommon.SAMPLE_RATE)));
    }

    outLeftDelay = new Delay(lengths[7], lengths[7]);
    outRightDelay = new Delay(lengths[8], lengths[8]);
    allpassCoefficient = 0.7;
    effectMix = 0.3;
    this->clear();
}

JCRev :: ~JCRev()
{
    delete allpassDelays[0];
    delete allpassDelays[1];
    delete allpassDelays[2];
    delete combDelays[0];
    delete combDelays[1];
    delete combDelays[2];
    delete combDelays[3];
    delete outLeftDelay;
    delete outRightDelay;
}

void JCRev :: clear()
{
    allpassDelays[0]->clear();
    allpassDelays[1]->clear();
    allpassDelays[2]->clear();
    combDelays[0]->clear();
    combDelays[1]->clear();
    combDelays[2]->clear();
    combDelays[3]->clear();
    outRightDelay->clear();
    outLeftDelay->clear();
    lastOutput[0] = 0.0;
    lastOutput[1] = 0.0;
}

double JCRev :: tick(double input)
{
    double temp, temp0, temp1, temp2, temp3, temp4, temp5, temp6;
    double filtout;

    // gewang: dedenormal
    CK_STK_DDN(input);

    temp = allpassDelays[0]->lastOut();
    temp0 = allpassCoefficient * temp;
    temp0 += input;
    // gewang: dedenormal
    CK_STK_DDN(temp0);
    allpassDelays[0]->tick(temp0);
    temp0 = -(allpassCoefficient * temp0) + temp;

    temp = allpassDelays[1]->lastOut();
    temp1 = allpassCoefficient * temp;
    temp1 += temp0;
    // gewang: dedenormal
    CK_STK_DDN(temp1);
    allpassDelays[1]->tick(temp1);
    temp1 = -(allpassCoefficient * temp1) + temp;

    temp = allpassDelays[2]->lastOut();
    temp2 = allpassCoefficient * temp;
    temp2 += temp1;
    // gewang: dedenormal
    CK_STK_DDN(temp2);
    allpassDelays[2]->tick(temp2);
    temp2 = -(allpassCoefficient * temp2) + temp;

    temp3 = temp2 + (combCoefficient[0] * combDelays[0]->lastOut());
    temp4 = temp2 + (combCoefficient[1] * combDelays[1]->lastOut());
    temp5 = temp2 + (combCoefficient[2] * combDelays[2]->lastOut());
    temp6 = temp2 + (combCoefficient[3] * combDelays[3]->lastOut());

    // gewang: dedenormal
    CK_STK_DDN(temp3);
    CK_STK_DDN(temp4);
    CK_STK_DDN(temp5);
    CK_STK_DDN(temp6);

    combDelays[0]->tick(temp3);
    combDelays[1]->tick(temp4);
    combDelays[2]->tick(temp5);
    combDelays[3]->tick(temp6);

    filtout = temp3 + temp4 + temp5 + temp6;

    // gewang: dedenormal
    CK_STK_DDN(filtout);

    lastOutput[0] = effectMix * (outLeftDelay->tick(filtout));
    lastOutput[1] = effectMix * (outRightDelay->tick(filtout));
    temp = (1.0 - effectMix) * input;
    lastOutput[0] += temp;
    lastOutput[1] += temp;

    return (lastOutput[0] + lastOutput[1]) * 0.5;
}


/***************************************************/
/*! \class Modulate
    \brief STK periodic/random modulator.

    This class combines random and periodic
    modulations to give a nice, natural human
    modulation function.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Modulate :: Modulate()
{
    // Concatenate the STK rawwave path to the rawwave file
    vibrato = new WaveLoop( "special:sinewave", TRUE );
    vibrato->setFrequency( 6.0 );
    vibratoGain = 0.04;

    noise = new SubNoise(330);
    randomGain = 0.05;

    filter = new OnePole( 0.999 );
    filter->setGain( randomGain );
}

Modulate :: ~Modulate()
{
    delete vibrato;
    delete noise;
    delete filter;
}

void Modulate :: reset()
{
    lastOutput = (double)  0.0;
}

void Modulate :: setVibratoRate(double aRate)
{
    vibrato->setFrequency( aRate );
}

void Modulate :: setVibratoGain(double aGain)
{
    vibratoGain = aGain;
}

void Modulate :: setRandomGain(double aGain)
{
    randomGain = aGain;
    filter->setGain( randomGain );
}

double Modulate :: tick()
{
    // Compute periodic and random modulations.
    lastOutput = vibratoGain * vibrato->tick();
    lastOutput += filter->tick( noise->tick() );
    return lastOutput;
}

double *Modulate :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick();

    return vec;
}

double Modulate :: lastOut() const
{
    return lastOutput;
}


/***************************************************/
/*! \class Moog
    \brief STK moog-like swept filter sampling synthesis class.

    This instrument uses one attack wave, one
    looped wave, and an ADSR envelope (inherited
    from the Sampler class) and adds two sweepable
    formant (FormSwep) filters.

    Control Change Numbers:
       - Filter Q = 2
       - Filter Sweep Rate = 4
       - Vibrato Frequency = 11
       - Vibrato Gain = 1
       - Gain = 128

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Moog :: Moog()
{
    // Concatenate the STK rawwave path to the rawwave file
    attacks[0] = new WvIn( "special:mandpluk", TRUE );
    loops[0] = new WaveLoop( "special:impuls20", TRUE );
    loops[1] = new WaveLoop( "special:sinewave", TRUE ); // vibrato
    loops[1]->setFrequency((double) 6.122);

    filters[0] = new FormSwep();
    filters[0]->setTargets( 0.0, 0.7 );

    filters[1] = new FormSwep();
    filters[1]->setTargets( 0.0, 0.7 );

    adsr->setAllTimes((double) 0.001,(double) 1.5,(double) 0.6,(double) 0.250);
    filterQ = (double) 0.85;
    filterRate = (double) 0.0001;
    modDepth = (double) 0.0;

    // chuck
    //reverse: loops[1]->setFrequency(mSpeed);
    m_vibratoFreq = loops[1]->m_freq;
    //reverse: modDepth = mDepth * (double) 0.5;
    m_vibratoGain = modDepth / 0.5;
    //reverse: nothing
    m_volume = 1.0;
}

Moog :: ~Moog()
{
    delete attacks[0];
    delete loops[0];
    delete loops[1];
    delete filters[0];
    delete filters[1];
}

void Moog :: setFrequency(double frequency)
{
    baseFrequency = frequency;
    if ( frequency <= 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Moog: setFrequency parameter is less than or equal to zero!" << CK_STDENDL;
        baseFrequency = 220.0;
    }

    double rate = attacks[0]->getSize() * 0.01 * baseFrequency / sampleRate();
    attacks[0]->setRate( rate );
    loops[0]->setFrequency(baseFrequency);

    // chuck
    m_frequency = baseFrequency;
}

//CHUCK wrapper
void Moog :: noteOn(double amplitude )
{
    noteOn ( baseFrequency, amplitude );
}

void Moog :: noteOn(double frequency, double amplitude)
{
    double temp;

    this->setFrequency( frequency );
    this->keyOn();
    attackGain = amplitude * (double) 0.5;
    loopGain = amplitude;

    temp = filterQ + (double) 0.05;
    filters[0]->setStates( 2000.0, temp );
    filters[1]->setStates( 2000.0, temp );

    temp = filterQ + (double) 0.099;
    filters[0]->setTargets( frequency, temp );
    filters[1]->setTargets( frequency, temp );

    filters[0]->setSweepRate( filterRate * 22050.0 / SynthCommon.SAMPLE_RATE );
    filters[1]->setSweepRate( filterRate * 22050.0 / SynthCommon.SAMPLE_RATE );
}

void Moog :: setModulationSpeed(double mSpeed)
{
    loops[1]->setFrequency(mSpeed);
    m_vibratoFreq = loops[1]->m_freq;
}

void Moog :: setModulationDepth(double mDepth)
{
    modDepth = mDepth * (double) 0.5;
    m_vibratoGain = mDepth;
}

double Moog :: tick()
{
    double temp;

    if ( modDepth != 0.0 )
    {
        temp = loops[1]->tick() * modDepth;
        loops[0]->setFrequency( baseFrequency * (1.0 + temp) );
    }

    temp = Sampler::tick();
    temp = filters[0]->tick( temp );
    lastOutput = filters[1]->tick( temp );
    return lastOutput * 3.0;
}

void Moog :: controlChange(int number, double value)
{
    double norm = value * ONE_OVER_128;
    if ( norm < 0 )
    {
        norm = 0.0;
        CK_STDCERR << "[chuck](via STK): Moog: Control value less than zero!" << CK_STDENDL;
    }
    else if ( norm > 1.0 )
    {
        norm = 1.0;
        CK_STDCERR << "[chuck](via STK): Moog: Control value exceeds nominal range!" << CK_STDENDL;
    }

    if (number == __SK_FilterQ_) // 2
        filterQ = 0.80 + ( 0.1 * norm );
    else if (number == __SK_FilterSweepRate_) // 4
        filterRate = norm * 0.0002;
    else if (number == __SK_ModFrequency_)   // 11
    {
        this->setModulationSpeed( norm * 12.0 );
    }
    else if (number == __SK_ModWheel_)   // 1
    {
        this->setModulationDepth( norm );
    }
    else if (number == __SK_AfterTouch_Cont_)   // 128
    {
        adsr->setTarget( norm );
        m_volume = norm;
    }
    else
        CK_STDCERR << "[chuck](via STK): Moog: Undefined Control Number (" << number << ")!!" << CK_STDENDL;
}



/***************************************************/
/*! \class NRev
    \brief CCRMA's NRev reverberator class.

    This class is derived from the CLM NRev
    function, which is based on the use of
    networks of simple allpass and comb delay
    filters.  This particular arrangement consists
    of 6 comb filters in parallel, followed by 3
    allpass filters, a lowpass filter, and another
    allpass in series, followed by two allpass
    filters in parallel with corresponding right
    and left outputs.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


NRev :: NRev(double T60)
{
    int lengths[15] = {1433, 1601, 1867, 2053, 2251, 2399, 347, 113, 37, 59, 53, 43, 37, 29, 19};
    double scaler = SynthCommon.SAMPLE_RATE / 25641.0;

    int delay, i;
    for (i=0; i<15; i++)
    {
        delay = (int) floor(scaler * lengths[i]);
        if ( (delay & 1) == 0) delay++;
        while ( !this->isPrime(delay) ) delay += 2;
        lengths[i] = delay;
    }

    for (i=0; i<6; i++)
    {
        combDelays[i] = new Delay( lengths[i], lengths[i]);
        combCoefficient[i] = pow(10.0, (-3 * lengths[i] / (T60 * SynthCommon.SAMPLE_RATE)));
    }

    for (i=0; i<8; i++)
        allpassDelays[i] = new Delay(lengths[i+6], lengths[i+6]);

    allpassCoefficient = 0.7;
    effectMix = 0.3;
    this->clear();
}

NRev :: ~NRev()
{
    int i;
    for (i=0; i<6; i++)  delete combDelays[i];
    for (i=0; i<8; i++)  delete allpassDelays[i];
}

void NRev :: clear()
{
    int i;
    for (i=0; i<6; i++) combDelays[i]->clear();
    for (i=0; i<8; i++) allpassDelays[i]->clear();
    lastOutput[0] = 0.0;
    lastOutput[1] = 0.0;
    lowpassState = 0.0;
}

double NRev :: tick(double input)
{
    double temp, temp0, temp1, temp2, temp3;
    int i;

    // gewang: dedenormal
    CK_STK_DDN(input);

    temp0 = 0.0;
    for (i=0; i<6; i++)
    {
        temp = input + (combCoefficient[i] * combDelays[i]->lastOut());
        // gewang: dedenormal
        CK_STK_DDN(temp);
        temp0 += combDelays[i]->tick(temp);
    }

    for (i=0; i<3; i++)
    {
        temp = allpassDelays[i]->lastOut();
        temp1 = allpassCoefficient * temp;
        temp1 += temp0;
        // gewang: dedenormal
        CK_STK_DDN(temp1);
        allpassDelays[i]->tick(temp1);
        temp0 = -(allpassCoefficient * temp1) + temp;
    }

    // One-pole lowpass filter.
    lowpassState = 0.7*lowpassState + 0.3*temp0;
    // gewang: dedenormal
    CK_STK_DDN(lowpassState);
    temp = allpassDelays[3]->lastOut();
    temp1 = allpassCoefficient * temp;
    temp1 += lowpassState;
    // gewang: dedenormal
    CK_STK_DDN(temp1);
    allpassDelays[3]->tick(temp1);
    temp1 = -(allpassCoefficient * temp1) + temp;

    temp = allpassDelays[4]->lastOut();
    temp2 = allpassCoefficient * temp;
    temp2 += temp1;
    // gewang: dedenormal
    CK_STK_DDN(temp2);
    allpassDelays[4]->tick(temp2);
    lastOutput[0] = effectMix*(-(allpassCoefficient * temp2) + temp);

    temp = allpassDelays[5]->lastOut();
    temp3 = allpassCoefficient * temp;
    temp3 += temp1;
    // gewang: dedenormal
    CK_STK_DDN(temp3);
    allpassDelays[5]->tick(temp3);
    lastOutput[1] = effectMix*(-(allpassCoefficient * temp3) + temp);

    temp = (1.0 - effectMix) * input;
    lastOutput[0] += temp;
    lastOutput[1] += temp;

    return (lastOutput[0] + lastOutput[1]) * 0.5;
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
/*! \class OnePole
    \brief STK one-pole filter class.

    This protected Filter subclass implements
    a one-pole digital filter.  A method is
    provided for setting the pole position along
    the real axis of the z-plane while maintaining
    a constant peak filter gain.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


OnePole :: OnePole() : FilterStk()
{
    double B = 0.1;
    double A[2] = {1.0, -0.9};
    FilterStk::setCoefficients( 1, &B, 2, A );
}

OnePole :: OnePole(double thePole) : FilterStk()
{
    double B;
    double A[2] = {1.0, -0.9};

    // Normalize coefficients for peak unity gain.
    if (thePole > 0.0)
        B = (double) (1.0 - thePole);
    else
        B = (double) (1.0 + thePole);

    A[1] = -thePole;
    FilterStk::setCoefficients( 1, &B, 2,  A );
}

OnePole :: ~OnePole()
{
}

void OnePole :: clear(void)
{
    FilterStk::clear();
}

void OnePole :: setB0(double b0)
{
    b[0] = b0;
}

void OnePole :: setA1(double a1)
{
    a[1] = a1;
}

void OnePole :: setPole(double thePole)
{
    // Normalize coefficients for peak unity gain.
    if (thePole > 0.0)
        b[0] = (double) (1.0 - thePole);
    else
        b[0] = (double) (1.0 + thePole);

    a[1] = -thePole;
}

void OnePole :: setGain(double theGain)
{
    FilterStk::setGain(theGain);
}

double OnePole :: getGain(void) const
{
    return FilterStk::getGain();
}

double OnePole :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double OnePole :: tick(double sample)
{
    inputs[0] = gain * sample;
    outputs[0] = b[0] * inputs[0] - a[1] * outputs[1];
    outputs[1] = outputs[0];

    // gewang: dedenormal
    CK_STK_DDN(outputs[1]);

    return outputs[0];
}

double *OnePole :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class OneZero
    \brief STK one-zero filter class.

    This protected Filter subclass implements
    a one-zero digital filter.  A method is
    provided for setting the zero position
    along the real axis of the z-plane while
    maintaining a constant filter gain.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


OneZero :: OneZero() : FilterStk()
{
    double B[2] = {0.5, 0.5};
    double A = 1.0;
    FilterStk::setCoefficients( 2, B, 1, &A );
}

OneZero :: OneZero(double theZero) : FilterStk()
{
    double B[2];
    double A = 1.0;

    // Normalize coefficients for unity gain.
    if (theZero > 0.0)
        B[0] = 1.0 / ((double) 1.0 + theZero);
    else
        B[0] = 1.0 / ((double) 1.0 - theZero);

    B[1] = -theZero * B[0];
    FilterStk::setCoefficients( 2, B, 1,  &A );
}

OneZero :: ~OneZero(void)
{
}

void OneZero :: clear(void)
{
    FilterStk::clear();
}

void OneZero :: setB0(double b0)
{
    b[0] = b0;
}

void OneZero :: setB1(double b1)
{
    b[1] = b1;
}

void OneZero :: setZero(double theZero)
{
    // Normalize coefficients for unity gain.
    if (theZero > 0.0)
        b[0] = 1.0 / ((double) 1.0 + theZero);
    else
        b[0] = 1.0 / ((double) 1.0 - theZero);

    b[1] = -theZero * b[0];
}

void OneZero :: setGain(double theGain)
{
    FilterStk::setGain(theGain);
}

double OneZero :: getGain(void) const
{
    return FilterStk::getGain();
}

double OneZero :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double OneZero :: tick(double sample)
{
    inputs[0] = gain * sample;
    outputs[0] = b[1] * inputs[1] + b[0] * inputs[0];
    inputs[1] = inputs[0];

    return outputs[0];
}

double *OneZero :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class PRCRev
    \brief Perry's simple reverberator class.

    This class is based on some of the famous
    Stanford/CCRMA reverbs (NRev, KipRev), which
    were based on the Chowning/Moorer/Schroeder
    reverberators using networks of simple allpass
    and comb delay filters.  This class implements
    two series allpass units and two parallel comb
    filters.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


PRCRev :: PRCRev(double T60)
{
    // Delay lengths for 44100 Hz sample rate.
    int lengths[4]= {353, 1097, 1777, 2137};
    double scaler = SynthCommon.SAMPLE_RATE / 44100.0;

    // Scale the delay lengths if necessary.
    int delay, i;
    if ( scaler != 1.0 )
    {
        for (i=0; i<4; i++)
        {
            delay = (int) floor(scaler * lengths[i]);
            if ( (delay & 1) == 0) delay++;
            while ( !this->isPrime(delay) ) delay += 2;
            lengths[i] = delay;
        }
    }

    for (i=0; i<2; i++)
    {
        allpassDelays[i] = new Delay( lengths[i], lengths[i] );
        combDelays[i] = new Delay( lengths[i+2], lengths[i+2] );
        combCoefficient[i] = pow(10.0,(-3 * lengths[i+2] / (T60 * SynthCommon.SAMPLE_RATE)));
    }

    allpassCoefficient = 0.7;
    effectMix = 0.5;
    this->clear();
}

PRCRev :: ~PRCRev()
{
    delete allpassDelays[0];
    delete allpassDelays[1];
    delete combDelays[0];
    delete combDelays[1];
}

void PRCRev :: clear()
{
    allpassDelays[0]->clear();
    allpassDelays[1]->clear();
    combDelays[0]->clear();
    combDelays[1]->clear();
    lastOutput[0] = 0.0;
    lastOutput[1] = 0.0;
}

double PRCRev :: tick(double input)
{
    double temp, temp0, temp1, temp2, temp3;

    // gewang: dedenormal
    CK_STK_DDN(input);

    temp = allpassDelays[0]->lastOut();
    temp0 = allpassCoefficient * temp;
    temp0 += input;
    // gewang: dedenormal
    CK_STK_DDN(temp0);
    allpassDelays[0]->tick(temp0);
    temp0 = -(allpassCoefficient * temp0) + temp;

    temp = allpassDelays[1]->lastOut();
    temp1 = allpassCoefficient * temp;
    temp1 += temp0;
    // gewang: dedenormal
    CK_STK_DDN(temp1);
    allpassDelays[1]->tick(temp1);
    temp1 = -(allpassCoefficient * temp1) + temp;

    temp2 = temp1 + (combCoefficient[0] * combDelays[0]->lastOut());
    temp3 = temp1 + (combCoefficient[1] * combDelays[1]->lastOut());

    // gewang: dedenormal
    CK_STK_DDN(temp2);
    CK_STK_DDN(temp3);

    lastOutput[0] = effectMix * (combDelays[0]->tick(temp2));
    lastOutput[1] = effectMix * (combDelays[1]->tick(temp3));
    temp = (double) (1.0 - effectMix) * input;
    lastOutput[0] += temp;
    lastOutput[1] += temp;

    return (lastOutput[0] + lastOutput[1]) * (double) 0.5;
}


/***************************************************/
/*! \class PitShift
    \brief STK simple pitch shifter effect class.

    This class implements a simple pitch shifter
    using delay lines.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


PitShift :: PitShift()
{
    delay[0] = 12;
    delay[1] = 512;
    delayLine[0] = new DelayL(delay[0], (long) 1024);
    delayLine[1] = new DelayL(delay[1], (long) 1024);
    effectMix = (double) 0.5;
    rate = 1.0;
}

PitShift :: ~PitShift()
{
    delete delayLine[0];
    delete delayLine[1];
}

void PitShift :: clear()
{
    delayLine[0]->clear();
    delayLine[1]->clear();
    lastOutput = 0.0;
}

void PitShift :: setEffectMix(double mix)
{
    effectMix = mix;
    if ( mix < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): PitShift: setEffectMix parameter is less than zero!" << CK_STDENDL;
        effectMix = 0.0;
    }
    else if ( mix > 1.0 )
    {
        CK_STDCERR << "[chuck](via STK): PitShift: setEffectMix parameter is greater than 1.0!" << CK_STDENDL;
        effectMix = 1.0;
    }
}

void PitShift :: setShift(double shift)
{
    if (shift < 1.0)
    {
        rate = 1.0 - shift;
    }
    else if (shift > 1.0)
    {
        rate = 1.0 - shift;
    }
    else
    {
        rate = 0.0;
        delay[0] = 512;
    }
}

double PitShift :: lastOut() const
{
    return lastOutput;
}

double PitShift :: tick(double input)
{
    delay[0] = delay[0] + rate;
    while (delay[0] > 1012) delay[0] -= 1000;
    while (delay[0] < 12) delay[0] += 1000;
    delay[1] = delay[0] + 500;
    while (delay[1] > 1012) delay[1] -= 1000;
    while (delay[1] < 12) delay[1] += 1000;
    delayLine[0]->setDelay((long)delay[0]);
    delayLine[1]->setDelay((long)delay[1]);
    env[1] = fabs(delay[0] - 512) * 0.002;
    env[0] = 1.0 - env[1];
    lastOutput =  env[0] * delayLine[0]->tick(input);
    lastOutput += env[1] * delayLine[1]->tick(input);
    lastOutput *= effectMix;
    lastOutput += (1.0 - effectMix) * input;
    return lastOutput;
}

double *PitShift :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}

/***************************************************/
/*! \class PoleZero
    \brief STK one-pole, one-zero filter class.

    This protected Filter subclass implements
    a one-pole, one-zero digital filter.  A
    method is provided for creating an allpass
    filter with a given coefficient.  Another
    method is provided to create a DC blocking filter.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


PoleZero :: PoleZero() : FilterStk()
{
    // Default setting for pass-through.
    double B[2] = {1.0, 0.0};
    double A[2] = {1.0, 0.0};
    FilterStk::setCoefficients( 2, B, 2, A );
}

PoleZero :: ~PoleZero()
{
}

void PoleZero :: clear(void)
{
    FilterStk::clear();
}

void PoleZero :: setB0(double b0)
{
    b[0] = b0;
}

void PoleZero :: setB1(double b1)
{
    b[1] = b1;
}

void PoleZero :: setA1(double a1)
{
    a[1] = a1;
}

void PoleZero :: setAllpass(double coefficient)
{
    b[0] = coefficient;
    b[1] = 1.0;
    a[0] = 1.0; // just in case
    a[1] = coefficient;
}

void PoleZero :: setBlockZero(double thePole)
{
    b[0] = 1.0;
    b[1] = -1.0;
    a[0] = 1.0; // just in case
    a[1] = -thePole;
}

void PoleZero :: setGain(double theGain)
{
    FilterStk::setGain(theGain);
}

double PoleZero :: getGain(void) const
{
    return FilterStk::getGain();
}

double PoleZero :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double PoleZero :: tick(double sample)
{
    inputs[0] = gain * sample;
    outputs[0] = b[0] * inputs[0] + b[1] * inputs[1] - a[1] * outputs[1];
    inputs[1] = inputs[0];
    outputs[1] = outputs[0];

    // gewang: dedenormal
    CK_STK_DDN(outputs[1]);

    return outputs[0];
}

double *PoleZero :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class Resonate
    \brief STK noise driven formant filter.

    This instrument contains a noise source, which
    excites a biquad resonance filter, with volume
    controlled by an ADSR.

    Control Change Numbers:
       - Resonance Frequency (0-Nyquist) = 2
       - Pole Radii = 4
       - Notch Frequency (0-Nyquist) = 11
       - Zero Radii = 1
       - Envelope Gain = 128

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Resonate :: Resonate()
{
    adsr = new ADSR;
    noise = new Noise;

    filter = new BiQuad;
    poleFrequency = 4000.0;
    poleRadius = 0.95;
    // Set the filter parameters.
    filter->setResonance( poleFrequency, poleRadius, TRUE );
    zeroFrequency = 0.0;
    zeroRadius = 0.0;
}

Resonate :: ~Resonate()
{
    delete adsr;
    delete filter;
    delete noise;
}

void Resonate :: keyOn()
{
    adsr->keyOn();
}

void Resonate :: keyOff()
{
    adsr->keyOff();
}

void Resonate :: noteOn(double frequency, double amplitude)
{
    adsr->setTarget( amplitude );
    this->keyOn();
    this->setResonance(frequency, poleRadius);
}
void Resonate :: noteOff(double amplitude)
{
    this->keyOff();
}

void Resonate :: setResonance(double frequency, double radius)
{
    poleFrequency = frequency;
    if ( frequency < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Resonate: setResonance frequency parameter is less than zero!" << CK_STDENDL;
        poleFrequency = 0.0;
    }

    poleRadius = radius;
    if ( radius < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Resonate: setResonance radius parameter is less than 0.0!" << CK_STDENDL;
        poleRadius = 0.0;
    }
    else if ( radius >= 1.0 )
    {
        CK_STDCERR << "[chuck](via STK): Resonate: setResonance radius parameter is greater than or equal to 1.0, which is unstable!" << CK_STDENDL;
        poleRadius = 0.9999;
    }
    filter->setResonance( poleFrequency, poleRadius, TRUE );
}

void Resonate :: setNotch(double frequency, double radius)
{
    zeroFrequency = frequency;
    if ( frequency < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Resonate: setNotch frequency parameter is less than zero!" << CK_STDENDL;
        zeroFrequency = 0.0;
    }

    zeroRadius = radius;
    if ( radius < 0.0 )
    {
        CK_STDCERR << "[chuck](via STK): Resonate: setNotch radius parameter is less than 0.0!" << CK_STDENDL;
        zeroRadius = 0.0;
    }

    filter->setNotch( zeroFrequency, zeroRadius );
}

void Resonate :: setEqualGainZeroes()
{
    filter->setEqualGainZeroes();
}

double Resonate :: tick()
{
    lastOutput = filter->tick(noise->tick());
    lastOutput *= adsr->tick();
    return lastOutput;
}

void Resonate :: controlChange(int number, double value)
{
    double norm = value * ONE_OVER_128;
    if ( norm < 0 )
    {
        norm = 0.0;
        CK_STDCERR << "[chuck](via STK): Resonate: Control value less than zero!" << CK_STDENDL;
    }
    else if ( norm > 1.0 )
    {
        norm = 1.0;
        CK_STDCERR << "[chuck](via STK): Resonate: Control value exceeds nominal range!" << CK_STDENDL;
    }

    if (number == 2) // 2
        setResonance( norm * SynthCommon.SAMPLE_RATE * 0.5, poleRadius );
    else if (number == 4) // 4
        setResonance( poleFrequency, norm*0.9999 );
    else if (number == 11) // 11
        this->setNotch( norm * SynthCommon.SAMPLE_RATE * 0.5, zeroRadius );
    else if (number == 1)
        this->setNotch( zeroFrequency, norm );
    else if (number == __SK_AfterTouch_Cont_) // 128
        adsr->setTarget( norm );
    else
        CK_STDCERR << "[chuck](via STK): Resonate: Undefined Control Number (" << number << ")!!" << CK_STDENDL;
}


/***************************************************/
/*! \class Reverb
    \brief STK abstract reverberator parent class.

    This class provides common functionality for
    STK reverberator subclasses.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Reverb :: Reverb()
{
}

Reverb :: ~Reverb()
{
}

void Reverb :: setEffectMix(double mix)
{
    effectMix = mix;
}

double Reverb :: lastOut() const
{
    return (lastOutput[0] + lastOutput[1]) * 0.5;
}

double Reverb :: lastOutLeft() const
{
    return lastOutput[0];
}

double Reverb :: lastOutRight() const
{
    return lastOutput[1];
}

double *Reverb :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}

bool Reverb :: isPrime(int number)
{
    if (number == 2) return true;
    if (number & 1)
    {
        for (int i=3; i<(int)sqrt((double)number)+1; i+=2)
            if ( (number % i) == 0) return false;
        return true; /* prime */
    }
    else return false; /* even */
}



/***************************************************/
/*! \class Sampler
    \brief STK sampling synthesis abstract base class.

    This instrument contains up to 5 attack waves,
    5 looped waves, and an ADSR envelope.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Sampler :: Sampler()
{
    // We don't make the waves here yet, because
    // we don't know what they will be.
    adsr = new ADSR;
    baseFrequency = 440.0;
    filter = new OnePole;
    attackGain = 0.25;
    loopGain = 0.25;
    whichOne = 0;

    // chuck
    m_frequency = baseFrequency;
}

Sampler :: ~Sampler()
{
    delete adsr;
    delete filter;
}

void Sampler :: keyOn()
{
    adsr->keyOn();
    attacks[0]->reset();
}

void Sampler :: keyOff()
{
    adsr->keyOff();
}

void Sampler :: noteOff(double amplitude)
{
    this->keyOff();
}

double Sampler :: tick()
{
    lastOutput = attackGain * attacks[whichOne]->tick();
    lastOutput += loopGain * loops[whichOne]->tick();
    lastOutput = filter->tick(lastOutput);
    lastOutput *= adsr->tick();
    return lastOutput;
}


/***************************************************/
/*! \class Simple
    \brief STK wavetable/noise instrument.

    This class combines a looped wave, a
    noise source, a biquad resonance filter,
    a one-pole filter, and an ADSR envelope
    to create some interesting sounds.

    Control Change Numbers:
       - Filter Pole Position = 2
       - Noise/Pitched Cross-Fade = 4
       - Envelope Rate = 11
       - Gain = 128

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


Simple :: Simple()
{
    adsr = new ADSR;
    baseFrequency = (double) 440.0;

    // Concatenate the STK rawwave path to the rawwave file
    loop = new WaveLoop( "special:impuls10", TRUE );

    filter = new OnePole(0.5);
    noise = new Noise;
    biquad = new BiQuad();

    setFrequency(baseFrequency);
    loopGain = 0.5;
}

Simple :: ~Simple()
{
    delete adsr;
    delete loop;
    delete filter;
    delete biquad;
}

void Simple :: keyOn()
{
    adsr->keyOn();
}

void Simple :: keyOff()
{
    adsr->keyOff();
}

void Simple :: noteOn(double frequency, double amplitude)
{
    keyOn();
    setFrequency(frequency);
    filter->setGain(amplitude);
}
void Simple :: noteOff(double amplitude)
{
    keyOff();
}

void Simple :: setFrequency(double frequency)
{
    biquad->setResonance( frequency, 0.98, true );
    loop->setFrequency(frequency);

    // chuck
    m_frequency = frequency;
}

double Simple :: tick()
{
    lastOutput = loopGain * loop->tick();
    biquad->tick( noise->tick() );
    lastOutput += (1.0 - loopGain) * biquad->lastOut();
    lastOutput = filter->tick( lastOutput );
    lastOutput *= adsr->tick();
    return lastOutput;
}

void Simple :: controlChange(int number, double value)
{
    double norm = value * ONE_OVER_128;
    if ( norm < 0 )
    {
        norm = 0.0;
        CK_STDCERR << "[chuck](via STK): Clarinet: Control value less than zero!" << CK_STDENDL;
    }
    else if ( norm > 1.0 )
    {
        norm = 1.0;
        CK_STDCERR << "[chuck](via STK): Clarinet: Control value exceeds nominal range!" << CK_STDENDL;
    }

    if (number == __SK_Breath_) // 2
        filter->setPole( 0.99 * (1.0 - (norm * 2.0)) );
    else if (number == __SK_NoiseLevel_) // 4
        loopGain = norm;
    else if (number == __SK_ModFrequency_)   // 11
    {
        norm /= 0.2 * SynthCommon.SAMPLE_RATE;
        adsr->setAttackRate( norm );
        adsr->setDecayRate( norm );
        adsr->setReleaseRate( norm );
    }
    else if (number == __SK_AfterTouch_Cont_) // 128
        adsr->setTarget( norm );
    else
        CK_STDCERR << "[chuck](via STK): Simple: Undefined Control Number (" << number << ")!!" << CK_STDENDL;
}


/***************************************************/
/*! \class SingWave
    \brief STK "singing" looped soundfile class.

    This class contains all that is needed to make
    a pitched musical sound, like a simple voice
    or violin.  In general, it will not be used
    alone because of munchkinification effects
    from pitch shifting.  It will be used as an
    excitation source for other instruments.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


SingWave :: SingWave(const char *fileName, bool raw)
{
    // An exception could be thrown here.
    wave = new WaveLoop( fileName, raw );

    rate = 1.0;
    sweepRate = 0.001;
    modulator = new Modulate();
    modulator->setVibratoRate( 6.0 );
    modulator->setVibratoGain( 0.04 );
    modulator->setRandomGain( 0.005 );
    envelope = new Envelope;
    pitchEnvelope = new Envelope;
    setFrequency( 75.0 );
    pitchEnvelope->setRate( 1.0 );
    this->tick();
    this->tick();
    pitchEnvelope->setRate( sweepRate * rate );
}

SingWave :: ~SingWave()
{
    delete wave;
    delete modulator;
    delete envelope;
    delete pitchEnvelope;
}

void SingWave :: reset()
{
    wave->reset();
    lastOutput = 0.0;
}

void SingWave :: normalize()
{
    wave->normalize();
}

void SingWave :: normalize(double newPeak)
{
    wave->normalize( newPeak );
}

void SingWave :: setFrequency(double frequency)
{
    m_freq = frequency;
    double temp = rate;
    rate = wave->getSize() * frequency / SynthCommon.SAMPLE_RATE;
    temp -= rate;
    if ( temp < 0) temp = -temp;
    pitchEnvelope->setTarget( rate );
    pitchEnvelope->setRate( sweepRate * temp );
}

void SingWave :: setVibratoRate(double aRate)
{
    modulator->setVibratoRate( aRate );
}

void SingWave :: setVibratoGain(double gain)
{
    modulator->setVibratoGain(gain);
}

void SingWave :: setRandomGain(double gain)
{
    modulator->setRandomGain(gain);
}

void SingWave :: setSweepRate(double aRate)
{
    sweepRate = aRate;
}

void SingWave :: setGainRate(double aRate)
{
    envelope->setRate(aRate);
}

void SingWave :: setGainTarget(double target)
{
    envelope->setTarget(target);
}

void SingWave :: noteOn()
{
    envelope->keyOn();
}

void SingWave :: noteOff()
{
    envelope->keyOff();
}

double SingWave :: tick()
{
    // Set the wave rate.
    double newRate = pitchEnvelope->tick();
    newRate += newRate * modulator->tick();
    wave->setRate( newRate );

    lastOutput = wave->tick();
    lastOutput *= envelope->tick();

    return lastOutput;
}

double SingWave :: lastOut()
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


/***************************************************/
/*! \class TwoPole
    \brief STK two-pole filter class.

    This protected Filter subclass implements
    a two-pole digital filter.  A method is
    provided for creating a resonance in the
    frequency response while maintaining a nearly
    constant filter gain.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


TwoPole :: TwoPole() : FilterStk()
{
    double B = 1.0;
    double A[3] = {1.0, 0.0, 0.0};
    m_resFreq = 440.0;
    m_resRad = 0.0;
    m_resNorm = false;
    FilterStk::setCoefficients( 1, &B, 3, A );
}

TwoPole :: ~TwoPole()
{
}

void TwoPole :: clear(void)
{
    FilterStk::clear();
}

void TwoPole :: setB0(double b0)
{
    b[0] = b0;
}

void TwoPole :: setA1(double a1)
{
    a[1] = a1;
}

void TwoPole :: setA2(double a2)
{
    a[2] = a2;
}

void TwoPole :: setResonance(double frequency, double radius, bool normalize)
{
    a[2] = radius * radius;
    a[1] = (double) -2.0 * radius * cos(TWO_PI * frequency / SynthCommon.SAMPLE_RATE);

    if ( normalize )
    {
        // Normalize the filter gain ... not terribly efficient.
        double real = 1 - radius + (a[2] - radius) * cos(TWO_PI * 2 * frequency / SynthCommon.SAMPLE_RATE);
        double imag = (a[2] - radius) * sin(TWO_PI * 2 * frequency / SynthCommon.SAMPLE_RATE);
        b[0] = sqrt( pow(real, 2) + pow(imag, 2) );
    }
}

void TwoPole :: setGain(double theGain)
{
    FilterStk::setGain(theGain);
}

double TwoPole :: getGain(void) const
{
    return FilterStk::getGain();
}

double TwoPole :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double TwoPole :: tick(double sample)
{
    inputs[0] = gain * sample;
    outputs[0] = b[0] * inputs[0] - a[2] * outputs[2] - a[1] * outputs[1];
    outputs[2] = outputs[1];
    outputs[1] = outputs[0];

    // gewang: dedenormal
    CK_STK_DDN(outputs[1]);
    CK_STK_DDN(outputs[2]);

    return outputs[0];
}

double *TwoPole :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}


/***************************************************/
/*! \class TwoZero
    \brief STK two-zero filter class.

    This protected Filter subclass implements
    a two-zero digital filter.  A method is
    provided for creating a "notch" in the
    frequency response while maintaining a
    constant filter gain.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


TwoZero :: TwoZero() : FilterStk()
{
    double B[3] = {1.0, 0.0, 0.0};
    double A = 1.0;
    m_notchFreq = 440.0;
    m_notchRad = 0.0;
    FilterStk::setCoefficients( 3, B, 1, &A );
}

TwoZero :: ~TwoZero()
{
}

void TwoZero :: clear(void)
{
    FilterStk::clear();
}

void TwoZero :: setB0(double b0)
{
    b[0] = b0;
}

void TwoZero :: setB1(double b1)
{
    b[1] = b1;
}

void TwoZero :: setB2(double b2)
{
    b[2] = b2;
}

void TwoZero :: setNotch(double frequency, double radius)
{
    b[2] = radius * radius;
    b[1] = (double) -2.0 * radius * cos(TWO_PI * (double) frequency / SynthCommon.SAMPLE_RATE);

    // Normalize the filter gain.
    if (b[1] > 0.0) // Maximum at z = 0.
        b[0] = 1.0 / (1.0+b[1]+b[2]);
    else            // Maximum at z = -1.
        b[0] = 1.0 / (1.0-b[1]+b[2]);
    b[1] *= b[0];
    b[2] *= b[0];
}

void TwoZero :: setGain(double theGain)
{
    FilterStk::setGain(theGain);
}

double TwoZero :: getGain(void) const
{
    return FilterStk::getGain();
}

double TwoZero :: lastOut(void) const
{
    return FilterStk::lastOut();
}

double TwoZero :: tick(double sample)
{
    inputs[0] = gain * sample;
    outputs[0] = b[2] * inputs[2] + b[1] * inputs[1] + b[0] * inputs[0];
    inputs[2] = inputs[1];
    inputs[1] = inputs[0];

    return outputs[0];
}

double *TwoZero :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}




//-----------------------------------------------------------------------------
// name: Instrmnt_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Instrmnt_tick )
{
    Instrmnt * i = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    if( !out ) CK_FPRINTF_STDERR( "[chuck](via STK): we warned you...\n");
    *out = i->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: BlowBotl_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Instrmnt_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Instrmnt_ctrl_noteOn()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Instrmnt_ctrl_noteOn )
{
    Instrmnt * i = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    i->noteOn( i->m_frequency, f );
}


//-----------------------------------------------------------------------------
// name: Instrmnt_ctrl_noteOff()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Instrmnt_ctrl_noteOff )
{
    Instrmnt * i = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    i->noteOff( f );
}


//-----------------------------------------------------------------------------
// name: Instrmnt_ctrl_freq()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Instrmnt_ctrl_freq )
{
    Instrmnt * i = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    i->setFrequency( f );
    RETURN->v_float = (t_CKFLOAT)i->m_frequency;
}


//-----------------------------------------------------------------------------
// name: Instrmnt_cget_freq()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Instrmnt_cget_freq )
{
    Instrmnt * i = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
//    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    RETURN->v_float = (t_CKFLOAT)i->m_frequency;
}


//-----------------------------------------------------------------------------
// name: Instrmnt_ctrl_controlChange()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL(Instrmnt_ctrl_controlChange )
{
    Instrmnt * ii = (Instrmnt *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKINT i = GET_NEXT_INT(ARGS);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    ii->controlChange( i, f );
}



// Chorus
//-----------------------------------------------------------------------------
// name: Chorus_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Chorus_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, Chorus_offset_data) = (t_CKUINT) new Chorus( 3000 );
}


//-----------------------------------------------------------------------------
// name: Chorus_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Chorus_dtor )
{
    delete (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    OBJ_MEMBER_UINT(SELF, Chorus_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Chorus_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Chorus_tick )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    *out = p->tick(in);
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Chorus_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Chorus_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Chorus_ctrl_mix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Chorus_ctrl_mix )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    p->setEffectMix( f );
    RETURN->v_float = (t_CKFLOAT) p->effectMix;
}


//-----------------------------------------------------------------------------
// name: Chorus_ctrl_modDepth()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Chorus_ctrl_modDepth )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    p->setModDepth( f );
    RETURN->v_float = (t_CKFLOAT) p->modDepth;
}


//-----------------------------------------------------------------------------
// name: Chorus_ctrl_modFreq()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Chorus_ctrl_modFreq )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    p->setModFrequency( f );
    RETURN->v_float = (t_CKFLOAT) p->mods[0]->m_freq;
}


//-----------------------------------------------------------------------------
// name: Chorus_ctrl_baseDelay()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Chorus_ctrl_baseDelay )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    t_CKDUR f = GET_NEXT_DUR(ARGS);
    p->setDelay( f );
    RETURN->v_float = (t_CKFLOAT) p->baseLength;
}


//-----------------------------------------------------------------------------
// name: Chorus_cget_mix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Chorus_cget_mix )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    RETURN->v_float = (t_CKFLOAT) p->effectMix;
}


//-----------------------------------------------------------------------------
// name: Chorus_cget_baseDelay()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Chorus_cget_baseDelay )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    RETURN->v_dur = (t_CKFLOAT)p->baseLength;
}


//-----------------------------------------------------------------------------
// name: Chorus_cget_modDepth()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Chorus_cget_modDepth )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    RETURN->v_float = (t_CKFLOAT) p->modDepth;
}


//-----------------------------------------------------------------------------
// name: Chorus_cget_modFreq()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Chorus_cget_modFreq )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    RETURN->v_float = (t_CKFLOAT) p->mods[0]->m_freq;
}


//-----------------------------------------------------------------------------
// name: Chorus_ctrl_set()
// desc: set ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Chorus_ctrl_set )
{
    Chorus * p = (Chorus *)OBJ_MEMBER_UINT(SELF, Chorus_offset_data);
    t_CKDUR d = GET_NEXT_DUR(ARGS);
    t_CKFLOAT dd = GET_NEXT_FLOAT(ARGS);
    p->set( d, dd );
}




// Delay
//-----------------------------------------------------------------------------
// name: Delay_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Delay_ctor )
{
    OBJ_MEMBER_UINT(SELF, Delay_offset_data) = (t_CKUINT)new Delay;
}


//-----------------------------------------------------------------------------
// name: Delay_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Delay_dtor )
{
    delete (Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data);
    OBJ_MEMBER_UINT(SELF, Delay_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Delay_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Delay_tick )
{
    *out = (SAMPLE)((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Delay_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Delay_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: Delay_ctrl_delay()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Delay_ctrl_delay )
{
    ((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->setDelay( (long)(GET_NEXT_DUR(ARGS)+.5) );
    RETURN->v_dur = (t_CKDUR)((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: Delay_cget_delay()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Delay_cget_delay )
{
    RETURN->v_dur = (t_CKDUR)((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: Delay_ctrl_max()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Delay_ctrl_max )
{
    Delay * delay = (Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data);
    t_CKFLOAT val = (t_CKFLOAT)delay->getDelay();
    t_CKDUR max = GET_NEXT_DUR(ARGS);
    delay->set( (long)(val+.5), (long)(max+1.5) );
    RETURN->v_dur = (t_CKDUR)((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->length-1.0;
}


//-----------------------------------------------------------------------------
// name: Delay_cget_max()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Delay_cget_max )
{
    RETURN->v_dur = (t_CKDUR)((Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data))->length-1.0;
}


//-----------------------------------------------------------------------------
// name: Delay_clear()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Delay_clear )
{
    Delay * delay = (Delay *)OBJ_MEMBER_UINT(SELF, Delay_offset_data);
    delay->clear();
}




// DelayA
//-----------------------------------------------------------------------------
// name: DelayA_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( DelayA_ctor )
{
    OBJ_MEMBER_UINT(SELF, DelayA_offset_data) = (t_CKUINT)new DelayA;
}


//-----------------------------------------------------------------------------
// name: DelayA_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( DelayA_dtor )
{
    delete (DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data);
    OBJ_MEMBER_UINT(SELF, DelayA_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: DelayA_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( DelayA_tick )
{
    *out = (SAMPLE)((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: DelayA_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( DelayA_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: DelayA_ctrl_delay()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( DelayA_ctrl_delay )
{
    ((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->setDelay( GET_NEXT_DUR(ARGS) );
    RETURN->v_dur = (t_CKDUR)((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: DelayA_cget_delay()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayA_cget_delay )
{
    RETURN->v_dur = (t_CKDUR)((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: DelayA_ctrl_max()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( DelayA_ctrl_max )
{
    DelayA * delay = (DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data);
    t_CKDUR val = (t_CKDUR)delay->getDelay();
    t_CKDUR max = GET_NEXT_DUR(ARGS);
    delay->set( val, (long)(max+1.5) );
    RETURN->v_dur = (t_CKDUR)((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->length-1.0;
}


//-----------------------------------------------------------------------------
// name: DelayA_cget_max()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayA_cget_max )
{
    RETURN->v_dur = (t_CKDUR)((DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data))->length-1.0;
}


//-----------------------------------------------------------------------------
// name: DelayA_clear()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayA_clear )
{
    DelayA * delay = (DelayA *)OBJ_MEMBER_UINT(SELF, DelayA_offset_data);
    delay->clear();
}




// DelayL
//-----------------------------------------------------------------------------
// name: DelayL_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( DelayL_ctor )
{
    OBJ_MEMBER_UINT(SELF, DelayL_offset_data) = (t_CKUINT)new DelayL;
}


//-----------------------------------------------------------------------------
// name: DelayL_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( DelayL_dtor )
{
    delete (DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data);
    OBJ_MEMBER_UINT(SELF, DelayL_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: DelayL_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( DelayL_tick )
{
    *out = (SAMPLE)((DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data))->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: DelayL_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( DelayL_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: DelayL_ctrl_delay()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( DelayL_ctrl_delay )
{
    ((DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data))->setDelay( GET_NEXT_DUR(ARGS) );
    RETURN->v_dur = (t_CKDUR)((DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: DelayL_cget_delay()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayL_cget_delay )
{
    RETURN->v_dur = (t_CKDUR)((DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: DelayL_ctrl_max()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( DelayL_ctrl_max )
{
    DelayL * delay = (DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data);
    t_CKDUR val = (t_CKDUR)delay->getDelay();
    t_CKDUR max = GET_NEXT_DUR(ARGS);
    delay->set( val, (long)(max+1.5) );
    RETURN->v_dur = (t_CKDUR)delay->length-1.0;
}


//-----------------------------------------------------------------------------
// name: DelayL_cget_max()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayL_cget_max )
{
    RETURN->v_dur = (t_CKDUR)((DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data))->length-1.0;
}


//-----------------------------------------------------------------------------
// name: DelayL_clear()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( DelayL_clear )
{
    DelayL * delay = (DelayL *)OBJ_MEMBER_UINT(SELF, DelayL_offset_data);
    delay->clear();
}




// Echo
//-----------------------------------------------------------------------------
// name: Echo_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Echo_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, Echo_offset_data) = (t_CKUINT)new Echo( SynthCommon.SAMPLE_RATE / 2.0 );
}


//-----------------------------------------------------------------------------
// name: Echo_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Echo_dtor )
{
    delete (Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data);
    OBJ_MEMBER_UINT(SELF, Echo_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Echo_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Echo_tick )
{
    *out = (SAMPLE)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Echo_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Echo_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: Echo_ctrl_delay()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Echo_ctrl_delay )
{
    ((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->setDelay( GET_NEXT_DUR(ARGS) );
    RETURN->v_dur = (t_CKDUR)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: Echo_cget_delay()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Echo_cget_delay )
{
    RETURN->v_dur = (t_CKDUR)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->getDelay();
}


//-----------------------------------------------------------------------------
// name: Echo_ctrl_max()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Echo_ctrl_max )
{
    ((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->set( GET_NEXT_DUR(ARGS) );
    RETURN->v_dur = (t_CKDUR)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->length;
}


//-----------------------------------------------------------------------------
// name: Echo_cget_max()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Echo_cget_max )
{
    RETURN->v_dur = (t_CKDUR)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->length;
}


//-----------------------------------------------------------------------------
// name: Echo_ctrl_mix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Echo_ctrl_mix )
{
    ((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->setEffectMix( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->effectMix;
}


//-----------------------------------------------------------------------------
// name: Echo_cget_mix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Echo_cget_mix )
{
    RETURN->v_float = (t_CKFLOAT)((Echo *)OBJ_MEMBER_UINT(SELF, Echo_offset_data))->effectMix;
}


//-----------------------------------------------------------------------------
// name: Envelope - import
// desc: ..
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// name: Envelope_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Envelope_ctor )
{
    OBJ_MEMBER_UINT(SELF, Envelope_offset_data) = (t_CKUINT)new Envelope;
}


//-----------------------------------------------------------------------------
// name: Envelope_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Envelope_dtor )
{
    delete (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    OBJ_MEMBER_UINT(SELF, Envelope_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Envelope_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Envelope_tick )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    *out = in * d->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Envelope_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Envelope_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_time()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_time )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setTime( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = d->m_time;
}


//-----------------------------------------------------------------------------
// name: Envelope_cget_time()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Envelope_cget_time )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = d->m_time;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_duration()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_duration )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setTime( GET_NEXT_FLOAT(ARGS) / SynthCommon.SAMPLE_RATE );
    RETURN->v_float = d->m_time * SynthCommon.SAMPLE_RATE;
}


//-----------------------------------------------------------------------------
// name: Envelope_cget_duration()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Envelope_cget_duration )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = d->m_time * SynthCommon.SAMPLE_RATE;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_rate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_rate )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setRate( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->rate;
}


//-----------------------------------------------------------------------------
// name: Envelope_cget_rate()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Envelope_cget_rate )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->rate;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_target()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_target )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setTarget( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->target;
}


//-----------------------------------------------------------------------------
// name: Envelope_cget_target()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Envelope_cget_target )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->target;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_value()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_value )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setValue( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->value;
}


//-----------------------------------------------------------------------------
// name: Envelope_cget_value()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Envelope_cget_value )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->value;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_keyOn0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_keyOn0 )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->keyOn();
    RETURN->v_int = 1;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_keyOn()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_keyOn )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    if( GET_NEXT_INT(ARGS) )
    {
        d->keyOn();
        RETURN->v_int = 1;
    }
    else
    {
        d->keyOff();
        RETURN->v_int = 0;
    }
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_keyOff0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_keyOff0 )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->keyOff();
    RETURN->v_int = 1;
}


//-----------------------------------------------------------------------------
// name: Envelope_ctrl_keyOff()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Envelope_ctrl_keyOff )
{
    Envelope * d = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    if( !GET_NEXT_INT(ARGS) )
    {
        d->keyOn();
        RETURN->v_int = 0;
    }
    else
    {
        d->keyOff();
        RETURN->v_int = 1;
    }
}


//-----------------------------------------------------------------------------
// name: ADSR - import
// desc: ..
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// name: ADSR_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( ADSR_ctor )
{
    // TODO: fix this horrid thing
    Envelope * e = (Envelope *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    SAFE_DELETE(e);

    OBJ_MEMBER_UINT(SELF, Envelope_offset_data) = (t_CKUINT)new ADSR;
}


//-----------------------------------------------------------------------------
// name: ADSR_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( ADSR_dtor )
{
    delete (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    OBJ_MEMBER_UINT(SELF, Envelope_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: ADSR_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( ADSR_tick )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    *out = in * d->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: ADSR_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( ADSR_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_attackTime()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_attackTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    t_CKDUR t = GET_NEXT_DUR(ARGS);
    d->setAttackTime( t / SynthCommon.SAMPLE_RATE );
    RETURN->v_dur = t;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_attackTime()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_attackTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_dur = d->getAttackTime() * SynthCommon.SAMPLE_RATE;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_attackRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_attackRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setAttackRate( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->attackRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_attackRate()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_attackRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->attackRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_decayTime()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_decayTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    t_CKDUR t = GET_NEXT_DUR(ARGS);
    d->setDecayTime( t / SynthCommon.SAMPLE_RATE );
    RETURN->v_dur = t;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_decayTime()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_decayTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_dur = d->getDecayTime() * SynthCommon.SAMPLE_RATE;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_decayRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_decayRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setDecayRate( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->decayRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_decayRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_decayRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->decayRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_sustainLevel()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_sustainLevel )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setSustainLevel( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->sustainLevel;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_sustainLevel()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_sustainLevel )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->sustainLevel;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_releaseTime()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_releaseTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    t_CKDUR t = GET_NEXT_DUR(ARGS);
    d->setReleaseTime( t / SynthCommon.SAMPLE_RATE );
    RETURN->v_dur = t;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_releaseTime()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_releaseTime )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_dur = d->getReleaseTime() * SynthCommon.SAMPLE_RATE;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_releaseRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_releaseRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    d->setReleaseRate( GET_NEXT_FLOAT(ARGS) );
    RETURN->v_float = (t_CKFLOAT)d->releaseRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_releaseRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_releaseRate )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->releaseRate;
}


//-----------------------------------------------------------------------------
// name: ADSR_cget_state()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( ADSR_cget_state )
{
    ADSR * d = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    RETURN->v_int = (t_CKINT) d->state;
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_set()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_set )
{
    ADSR * e = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    t_CKFLOAT a = GET_NEXT_FLOAT(ARGS);
    t_CKFLOAT d = GET_NEXT_FLOAT(ARGS);
    t_CKFLOAT s = GET_NEXT_FLOAT(ARGS);
    t_CKFLOAT r = GET_NEXT_FLOAT(ARGS);
    e->setAttackTime( a );
    e->setDecayTime( d );
    e->setSustainLevel( s );
    e->setReleaseTime( r );
}


//-----------------------------------------------------------------------------
// name: ADSR_ctrl_set2()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( ADSR_ctrl_set2 )
{
    ADSR * e = (ADSR *)OBJ_MEMBER_UINT(SELF, Envelope_offset_data);
    t_CKDUR a = GET_NEXT_DUR(ARGS);
    t_CKDUR d = GET_NEXT_DUR(ARGS);
    t_CKFLOAT s = GET_NEXT_FLOAT(ARGS);
    t_CKDUR r = GET_NEXT_DUR(ARGS);
    e->setAttackTime( a / SynthCommon.SAMPLE_RATE );
    e->setDecayTime( d / SynthCommon.SAMPLE_RATE );
    e->setSustainLevel( s );
    e->setReleaseTime( r / SynthCommon.SAMPLE_RATE );
}




// FilterStk
//-----------------------------------------------------------------------------
// name: FilterStk_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( FilterStk_ctor )
{
    OBJ_MEMBER_UINT(SELF, FilterStk_offset_data) = (t_CKUINT)new FilterStk;
}


//-----------------------------------------------------------------------------
// name: FilterStk_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( FilterStk_dtor )
{
    delete (FilterStk *)OBJ_MEMBER_UINT(SELF, FilterStk_offset_data);
    OBJ_MEMBER_UINT(SELF, FilterStk_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: FilterStk_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( FilterStk_tick )
{
    FilterStk * d = (FilterStk *)OBJ_MEMBER_UINT(SELF, FilterStk_offset_data);
    *out = d->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: FilterStk_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( FilterStk_pmsg )
{
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: FilterStk_ctrl_coefs()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( FilterStk_ctrl_coefs )
{
    // FilterStk * d = (FilterStk *)OBJ_MEMBER_UINT(SELF, FilterStk_offset_data);
    CK_FPRINTF_STDERR( "FilterStk.coefs :: not implemented\n" );
}


//-----------------------------------------------------------------------------
// name: OnePole_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( OnePole_ctor  )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, OnePole_offset_data) = (t_CKUINT)new OnePole();
}


//-----------------------------------------------------------------------------
// name: OnePole_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( OnePole_dtor  )
{
    delete (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    OBJ_MEMBER_UINT(SELF, OnePole_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: OnePole_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( OnePole_tick )
{
    OnePole * m = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    *out = m->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: OnePole_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( OnePole_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: OnePole_ctrl_a1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OnePole_ctrl_a1 )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setA1( f );
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: OnePole_cget_a1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OnePole_cget_a1 )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: OnePole_ctrl_b0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OnePole_ctrl_b0 )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB0( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: OnePole_cget_b0()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OnePole_cget_b0 )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: OnePole_ctrl_pole()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OnePole_ctrl_pole )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setPole( f );
    RETURN->v_float = (t_CKFLOAT) -filter->a[1];
}


//-----------------------------------------------------------------------------
// name: OnePole_cget_pole()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OnePole_cget_pole )
{
    OnePole * filter = (OnePole *)OBJ_MEMBER_UINT(SELF, OnePole_offset_data);
    RETURN->v_float = (t_CKFLOAT) -filter->a[1];
}



//TwoPole functions

//-----------------------------------------------------------------------------
// name: TwoPole_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( TwoPole_ctor  )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, TwoPole_offset_data) = (t_CKUINT)new TwoPole();
}


//-----------------------------------------------------------------------------
// name: TwoPole_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( TwoPole_dtor  )
{
    delete (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    OBJ_MEMBER_UINT(SELF, TwoPole_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: TwoPole_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( TwoPole_tick )
{
    TwoPole * m = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    *out = m->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: TwoPole_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( TwoPole_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_a1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_a1 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setA1( f );
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_a1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_a1 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_a2()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_a2 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setA2( f );
    RETURN->v_float = (t_CKFLOAT) filter->a[2];
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_a2()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_a2 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->a[2];
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_b0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_b0 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB0( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_b0()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_b0 )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_freq()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_freq )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->ck_setResFreq( f );
    RETURN->v_float = (t_CKFLOAT) filter->m_resFreq;
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_freq()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_freq )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->m_resFreq;
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_radius()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_radius )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->ck_setResRad( f );
    RETURN->v_float = (t_CKFLOAT) filter->m_resRad;
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_radius()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_radius )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->m_resRad;
}


//-----------------------------------------------------------------------------
// name: TwoPole_ctrl_norm()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoPole_ctrl_norm )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    bool b = ( GET_CK_INT(ARGS) != 0 );
    filter->ck_setResNorm( b );
    RETURN->v_int = (t_CKINT) filter->m_resNorm;
}


//-----------------------------------------------------------------------------
// name: TwoPole_cget_norm()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoPole_cget_norm )
{
    TwoPole * filter = (TwoPole *)OBJ_MEMBER_UINT(SELF, TwoPole_offset_data);
    RETURN->v_int = (t_CKINT) filter->m_resNorm;
}




//OneZero functions

//-----------------------------------------------------------------------------
// name: OneZero_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( OneZero_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, OneZero_offset_data) = (t_CKUINT) new OneZero();
}


//-----------------------------------------------------------------------------
// name: OneZero_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( OneZero_dtor )
{
    delete (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    OBJ_MEMBER_UINT(SELF, OneZero_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: OneZero_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( OneZero_tick )
{
    OneZero * m = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    *out = m->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: OneZero_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( OneZero_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: OneZero_ctrl_zero()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OneZero_ctrl_zero )
{
    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setZero( f );
    double zeeroo = ( filter->b[0] == 0 ) ? 0 : -filter->b[1] / filter->b[0];
    RETURN->v_float = (t_CKFLOAT) zeeroo;
}


//-----------------------------------------------------------------------------
// name: OneZero_cget_zero()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OneZero_cget_zero )
{
    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    double zeeroo = ( filter->b[0] == 0 ) ? 0 : -filter->b[1] / filter->b[0];
    RETURN->v_float = (t_CKFLOAT)zeeroo;
}


//-----------------------------------------------------------------------------
// name: OneZero_ctrl_b0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OneZero_ctrl_b0 )
{
    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB0( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: OneZero_cget_b0()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OneZero_cget_b0 )
{
    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: OneZero_ctrl_b1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( OneZero_ctrl_b1 )
{
    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB1( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[1];
}


//-----------------------------------------------------------------------------
// name: OneZero_cget_b1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( OneZero_cget_b1 )
{

    OneZero * filter = (OneZero *)OBJ_MEMBER_UINT(SELF, OneZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[1];
}


//TwoZero functions

//-----------------------------------------------------------------------------
// name: TwoZero_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( TwoZero_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, TwoZero_offset_data) = (t_CKUINT)new TwoZero();
}


//-----------------------------------------------------------------------------
// name: TwoZero_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( TwoZero_dtor )
{
    delete (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    OBJ_MEMBER_UINT(SELF, TwoZero_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: TwoZero_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( TwoZero_tick )
{
    TwoZero * m = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    *out = m->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: TwoZero_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( TwoZero_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: TwoZero_ctrl_b0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoZero_ctrl_b0 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB0( f );
}


//-----------------------------------------------------------------------------
// name: TwoZero_cget_b0()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoZero_cget_b0 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: TwoZero_ctrl_b1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoZero_ctrl_b1 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB1( f );
}

//-----------------------------------------------------------------------------
// name: TwoZero_cget_b1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoZero_cget_b1 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[1];
}

//-----------------------------------------------------------------------------
// name: TwoZero_ctrl_b2()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoZero_ctrl_b2 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB2( f );
}


//-----------------------------------------------------------------------------
// name: TwoZero_cget_b2()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoZero_cget_b2 )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[2];
}


//-----------------------------------------------------------------------------
// name: TwoZero_ctrl_freq()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoZero_ctrl_freq )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->ck_setNotchFreq( f );
}


//-----------------------------------------------------------------------------
// name: TwoZero_cget_freq()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoZero_cget_freq )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->m_notchFreq;
}


//-----------------------------------------------------------------------------
// name: TwoZero_ctrl_radius()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( TwoZero_ctrl_radius )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->ck_setNotchRad( f );
}

//-----------------------------------------------------------------------------
// name: TwoZero_cget_radius()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( TwoZero_cget_radius )
{
    TwoZero * filter = (TwoZero *)OBJ_MEMBER_UINT(SELF, TwoZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->m_notchRad;
}



//PoleZero functions

//-----------------------------------------------------------------------------
// name: PoleZero_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( PoleZero_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, PoleZero_offset_data) = (t_CKUINT) new PoleZero();
}


//-----------------------------------------------------------------------------
// name: PoleZero_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( PoleZero_dtor )
{
    delete (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    OBJ_MEMBER_UINT(SELF, PoleZero_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: PoleZero_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( PoleZero_tick )
{
    PoleZero * m = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    *out = m->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PoleZero_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( PoleZero_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PoleZero_ctrl_a1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PoleZero_ctrl_a1 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setA1( f );
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: PoleZero_cget_a1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PoleZero_cget_a1 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->a[1];
}


//-----------------------------------------------------------------------------
// name: PoleZero_ctrl_b0()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PoleZero_ctrl_b0 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB0( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: PoleZero_cget_b0()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PoleZero_cget_b0 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}



//-----------------------------------------------------------------------------
// name: PoleZero_ctrl_b1()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PoleZero_ctrl_b1 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setB1( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[1];
}


//-----------------------------------------------------------------------------
// name: PoleZero_cget_b1()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PoleZero_cget_b1 )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[1];
}


//-----------------------------------------------------------------------------
// name: PoleZero_ctrl_allpass()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PoleZero_ctrl_allpass )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setAllpass( f );
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: PoleZero_cget_allpass()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PoleZero_cget_allpass )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) filter->b[0];
}


//-----------------------------------------------------------------------------
// name: PoleZero_ctrl_blockZero()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PoleZero_ctrl_blockZero )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    filter->setBlockZero( f );
    RETURN->v_float = (t_CKFLOAT) -filter->a[1];
}


//-----------------------------------------------------------------------------
// name: PoleZero_cget_blockZero()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PoleZero_cget_blockZero )
{
    PoleZero * filter = (PoleZero *)OBJ_MEMBER_UINT(SELF, PoleZero_offset_data);
    RETURN->v_float = (t_CKFLOAT) -filter->a[1];
}








//-----------------------------------------------------------------------------
// name: FormSwep_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( FormSwep_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, FormSwep_offset_data) = (t_CKUINT)new FormSwep();
}


//-----------------------------------------------------------------------------
// name: FormSwep_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( FormSwep_dtor )
{
    delete (FormSwep *)OBJ_MEMBER_UINT(SELF, FormSwep_offset_data);
    OBJ_MEMBER_UINT(SELF, FormSwep_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: FormSwep_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( FormSwep_tick )
{
    FormSwep * m = (FormSwep *)OBJ_MEMBER_UINT(SELF, FormSwep_offset_data);
    *out = m->tick(in);
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: FormSwep_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( FormSwep_pmsg )
{
    return TRUE;
}

//FormSwep requires multiple arguments
//to most of its parameters.



//-----------------------------------------------------------------------------
// name: JCRev_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( JCRev_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, JCRev_offset_data) = (t_CKUINT)new JCRev( 4.0f );
}


//-----------------------------------------------------------------------------
// name: JCRev_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( JCRev_dtor )
{
    delete (JCRev *)OBJ_MEMBER_UINT(SELF, JCRev_offset_data);
    OBJ_MEMBER_UINT(SELF, JCRev_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: JCRev_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( JCRev_tick )
{
    JCRev * j = (JCRev *)OBJ_MEMBER_UINT(SELF, JCRev_offset_data);
    *out = j->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: JCRev_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( JCRev_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: JCRev_ctrl_mix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( JCRev_ctrl_mix )
{
    JCRev * j = (JCRev *)OBJ_MEMBER_UINT(SELF, JCRev_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setEffectMix( f );
    RETURN->v_float = (t_CKFLOAT) j->effectMix;
}


//-----------------------------------------------------------------------------
// name: JCRev_cget_mix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( JCRev_cget_mix )
{
    JCRev * j = (JCRev *)OBJ_MEMBER_UINT(SELF, JCRev_offset_data);
    RETURN->v_float = (t_CKFLOAT) j->effectMix;
}



// Modulate
//-----------------------------------------------------------------------------
// name: Modulate_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Modulate_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, Modulate_offset_data) = (t_CKUINT) new Modulate( );
}


//-----------------------------------------------------------------------------
// name: Modulate_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Modulate_dtor )
{
    delete (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    OBJ_MEMBER_UINT(SELF, Modulate_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Modulate_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Modulate_tick )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    *out = j->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Modulate_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Modulate_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Modulate_ctrl_vibratoRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Modulate_ctrl_vibratoRate )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setVibratoRate( f );
    RETURN->v_float = (t_CKFLOAT) j->vibrato->m_freq;
}


//-----------------------------------------------------------------------------
// name: Modulate_cget_vibratoRate()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Modulate_cget_vibratoRate )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    RETURN->v_float = (t_CKFLOAT) j->vibrato->m_freq;
}


//-----------------------------------------------------------------------------
// name: Modulate_ctrl_vibratoGain()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Modulate_ctrl_vibratoGain )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setVibratoGain( f );
    RETURN->v_float = (t_CKFLOAT) j->vibratoGain;
}


//-----------------------------------------------------------------------------
// name: Modulate_cget_vibratoGain()
// desc: CGET function ...
//-----------------------------------------------------------------------------

CK_DLL_CGET( Modulate_cget_vibratoGain )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    RETURN->v_float = (t_CKFLOAT) j->vibratoGain;
}


//-----------------------------------------------------------------------------
// name: Modulate_ctrl_randomGain()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Modulate_ctrl_randomGain )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setRandomGain(f );
    RETURN->v_float = (t_CKFLOAT) j->randomGain;
}


//-----------------------------------------------------------------------------
// name: Modulate_cget_randomGain()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Modulate_cget_randomGain )
{
    Modulate * j = (Modulate *)OBJ_MEMBER_UINT(SELF, Modulate_offset_data);
    RETURN->v_float = (t_CKFLOAT) j->randomGain;
}


//-----------------------------------------------------------------------------
// name: Moog_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Moog_ctor  )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data) = (t_CKUINT)new Moog();
}


//-----------------------------------------------------------------------------
// name: Moog_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( Moog_dtor  )
{
    delete (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: Moog_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( Moog_tick )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    *out = m->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Moog_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( Moog_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_modSpeed()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_modSpeed )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->setModulationSpeed(f);
    RETURN->v_float = (t_CKFLOAT) m->loops[1]->m_freq;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_modSpeed()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_modSpeed )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT) m->loops[1]->m_freq;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_modDepth()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_modDepth )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->setModulationDepth(f);
    RETURN->v_float = (t_CKFLOAT) m->modDepth * 2.0;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_modDepth()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_modDepth )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT) m->modDepth * 2.0;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_filterQ()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_filterQ )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->controlChange( __SK_FilterQ_, f * 128.0 );
    RETURN->v_float = (t_CKFLOAT)  10.0 * ( m->filterQ - 0.80 );
}


//-----------------------------------------------------------------------------
// name: Moog_cget_filterQ()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_filterQ )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT)  10.0 * ( m->filterQ - 0.80 );
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_filterSweepRate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_filterSweepRate )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->controlChange( __SK_FilterSweepRate_, f * 128.0 );
    RETURN->v_float = (t_CKFLOAT)  m->filterRate * 5000;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_filterSweepRate()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_filterSweepRate )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT)  m->filterRate * 5000;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_afterTouch()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_afterTouch )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->controlChange( __SK_AfterTouch_Cont_, f * 128.0 );
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_vibratoFreq()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_vibratoFreq )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->setModulationSpeed(f);
    RETURN->v_float = (t_CKFLOAT)m->m_vibratoFreq;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_vibratoFreq()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_vibratoFreq )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT)m->m_vibratoFreq;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_vibratoGain()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_vibratoGain )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->controlChange( __SK_ModWheel_, f * 128.0 );
    RETURN->v_float = (t_CKFLOAT)  m->m_vibratoGain;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_vibratoGain()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_vibratoGain )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT)  m->m_vibratoGain;
}


//-----------------------------------------------------------------------------
// name: Moog_ctrl_volume()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( Moog_ctrl_volume )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    m->controlChange( __SK_AfterTouch_Cont_, f * 128.0 );
    RETURN->v_float = (t_CKFLOAT)  m->m_volume;
}


//-----------------------------------------------------------------------------
// name: Moog_cget_volume()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( Moog_cget_volume )
{
    Moog * m = (Moog *)OBJ_MEMBER_UINT(SELF, Instrmnt_offset_data);
    RETURN->v_float = (t_CKFLOAT)  m->m_volume;
}




// NRev
//-----------------------------------------------------------------------------
// name: NRev_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( NRev_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, NRev_offset_data) = (t_CKUINT)new NRev( 4.0f );
}


//-----------------------------------------------------------------------------
// name: NRev_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( NRev_dtor )
{
    delete (NRev *)OBJ_MEMBER_UINT(SELF, NRev_offset_data);
    OBJ_MEMBER_UINT(SELF, NRev_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: NRev_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( NRev_tick )
{
    NRev * j = (NRev *)OBJ_MEMBER_UINT(SELF, NRev_offset_data);
    *out = j->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: NRev_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( NRev_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: NRev_ctrl_mix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( NRev_ctrl_mix )
{
    NRev * j = (NRev *)OBJ_MEMBER_UINT(SELF, NRev_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setEffectMix( f );
    RETURN->v_float = (t_CKFLOAT) j->effectMix;
}


//-----------------------------------------------------------------------------
// name: NRev_cget_mix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( NRev_cget_mix )
{
    NRev * j = (NRev *)OBJ_MEMBER_UINT(SELF, NRev_offset_data);
    RETURN->v_float = (t_CKFLOAT) j->effectMix;
}



// PitShift
//-----------------------------------------------------------------------------
// name: PitShift_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( PitShift_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, PitShift_offset_data) = (t_CKUINT)new PitShift( );
}


//-----------------------------------------------------------------------------
// name: PitShift_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( PitShift_dtor )
{
    delete (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    OBJ_MEMBER_UINT(SELF, PitShift_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: PitShift_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( PitShift_tick )
{
    PitShift * p = (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    *out = p->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PitShift_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( PitShift_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PitShift_ctrl_shift()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PitShift_ctrl_shift )
{
    PitShift * p = (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    p->setShift( f );
    RETURN->v_float = (t_CKFLOAT)  1.0 - p->rate;
}


//-----------------------------------------------------------------------------
// name: PitShift_cget_shift()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PitShift_cget_shift )
{
    PitShift * p = (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    RETURN->v_float = (t_CKFLOAT)  1.0 - p->rate;
}


//-----------------------------------------------------------------------------
// name: PitShift_ctrl_effectMix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PitShift_ctrl_effectMix )
{
    PitShift * p = (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    p->setEffectMix( f );
    RETURN->v_float = (t_CKFLOAT)  p->effectMix;
}


//-----------------------------------------------------------------------------
// name: PitShift_cget_effectMix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PitShift_cget_effectMix )
{
    PitShift * p = (PitShift *)OBJ_MEMBER_UINT(SELF, PitShift_offset_data);
    RETURN->v_float = (t_CKFLOAT)  p->effectMix;
}



// PRCRev
//-----------------------------------------------------------------------------
// name: PRCRev_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( PRCRev_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, PRCRev_offset_data) = (t_CKUINT)new PRCRev( 4.0f );
}


//-----------------------------------------------------------------------------
// name: PRCRev_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( PRCRev_dtor )
{
    delete (PRCRev *)OBJ_MEMBER_UINT(SELF, PRCRev_offset_data);
    OBJ_MEMBER_UINT(SELF, PRCRev_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: PRCRev_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( PRCRev_tick )
{
    PRCRev * j = (PRCRev *)OBJ_MEMBER_UINT(SELF, PRCRev_offset_data);
    *out = j->tick( in );
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PRCRev_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( PRCRev_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: PRCRev_ctrl_mix()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( PRCRev_ctrl_mix )
{
    PRCRev * j = (PRCRev *)OBJ_MEMBER_UINT(SELF, PRCRev_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    j->setEffectMix( f );
    RETURN->v_float = (t_CKFLOAT)j->effectMix;
}


//-----------------------------------------------------------------------------
// name: PRCRev_cget_mix()
// desc: CGET function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( PRCRev_cget_mix )
{
    PRCRev * j = (PRCRev *)OBJ_MEMBER_UINT(SELF, PRCRev_offset_data);
    RETURN->v_float = (t_CKFLOAT)j->effectMix;
}




//-----------------------------------------------------------------------------
// name: SubNoise_ctor()
// desc: CTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( SubNoise_ctor )
{
    // initialize member object
    OBJ_MEMBER_UINT(SELF, SubNoise_offset_data) = (t_CKUINT)new SubNoise( );
}


//-----------------------------------------------------------------------------
// name: SubNoise_dtor()
// desc: DTOR function ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( SubNoise_dtor )
{
    delete (SubNoise *)OBJ_MEMBER_UINT(SELF, SubNoise_offset_data);
    OBJ_MEMBER_UINT(SELF, SubNoise_offset_data) = 0;
}


//-----------------------------------------------------------------------------
// name: SubNoise_tick()
// desc: TICK function ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( SubNoise_tick )
{
    SubNoise * p = (SubNoise *)OBJ_MEMBER_UINT(SELF, SubNoise_offset_data);
    *out = p->tick();
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: SubNoise_pmsg()
// desc: PMSG function ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( SubNoise_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// name: SubNoise_ctrl_rate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( SubNoise_ctrl_rate )
{
    SubNoise * p = (SubNoise *)OBJ_MEMBER_UINT(SELF, SubNoise_offset_data);
    int i = GET_CK_INT(ARGS);
    p->setRate( i );
    RETURN->v_int = (t_CKINT) (int)((SubNoise *)OBJ_MEMBER_UINT(SELF, SubNoise_offset_data))->subRate();
}


//-----------------------------------------------------------------------------
// name: SubNoise_cget_rate()
// desc: CTRL function ...
//-----------------------------------------------------------------------------
CK_DLL_CGET( SubNoise_cget_rate )
{
    RETURN->v_int = (t_CKINT) (int)((SubNoise *)OBJ_MEMBER_UINT(SELF, SubNoise_offset_data))->subRate();
}




//-----------------------------------------------------------------------------
// BLT
//-----------------------------------------------------------------------------
CK_DLL_CTOR( BLT_ctor )
{ /* do nothing here */ }

CK_DLL_DTOR( BLT_dtor )
{ /* do nothing here */ }

CK_DLL_TICK( BLT_tick )
{
    CK_FPRINTF_STDERR( "BLT is virtual!\n" );
    return TRUE;
}

CK_DLL_PMSG( BLT_pmsg )
{
    return TRUE;
}

CK_DLL_CTRL( BLT_ctrl_phase )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    blt->setPhase( f );
    blt->m_phase = f;
    RETURN->v_float = f;
}

CK_DLL_CGET( BLT_cget_phase )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    RETURN->v_float = blt->getValuePhase();
}

CK_DLL_CTRL( BLT_ctrl_freq )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    t_CKFLOAT f = GET_NEXT_FLOAT(ARGS);
    blt->setFrequency( f );
    blt->m_freq = f;
    RETURN->v_float = f;
}

CK_DLL_CGET( BLT_cget_freq )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    RETURN->v_float = blt->getValueFreq();
}

CK_DLL_CTRL( BLT_ctrl_harmonics )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    t_CKINT i = GET_CK_INT(ARGS);
    if( i < 0 ) i = -i;
    blt->setHarmonics( i );
    blt->m_harmonics = i;
    RETURN->v_int = i;
}

CK_DLL_CGET( BLT_cget_harmonics )
{
    BLT * blt = (BLT *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    RETURN->v_int = blt->getValueHarmonics();
}


//-----------------------------------------------------------------------------
// Blit
//-----------------------------------------------------------------------------
CK_DLL_CTOR( Blit_ctor )
{
    Blit * blit = new Blit;
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = (t_CKUINT)blit;
}

CK_DLL_DTOR( Blit_dtor )
{
    delete (Blit *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = 0;
}

CK_DLL_TICK( Blit_tick )
{
    Blit * blit = (Blit *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    *out = blit->tick();
    return TRUE;
}

CK_DLL_PMSG( Blit_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// BlitSaw
//-----------------------------------------------------------------------------
CK_DLL_CTOR( BlitSaw_ctor )
{
    BlitSaw * blit = new BlitSaw;
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = (t_CKUINT)blit;
}

CK_DLL_DTOR( BlitSaw_dtor )
{
    delete (BlitSaw *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = 0;
}

CK_DLL_TICK( BlitSaw_tick )
{
    BlitSaw * blit = (BlitSaw *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    *out = blit->tick();
    return TRUE;
}

CK_DLL_PMSG( BlitSaw_pmsg )
{
    return TRUE;
}


//-----------------------------------------------------------------------------
// BlitSquare
//-----------------------------------------------------------------------------
CK_DLL_CTOR( BlitSquare_ctor )
{
    BlitSquare * blit = new BlitSquare;
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = (t_CKUINT)blit;
}

CK_DLL_DTOR( BlitSquare_dtor )
{
    delete (BlitSquare *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    OBJ_MEMBER_UINT(SELF, BLT_offset_data) = 0;
}

CK_DLL_TICK( BlitSquare_tick )
{
    BlitSquare * blit = (BlitSquare *)OBJ_MEMBER_UINT(SELF, BLT_offset_data);
    *out = blit->tick();
    return TRUE;
}

CK_DLL_PMSG( BlitSquare_pmsg )
{
    return TRUE;
}

#endif