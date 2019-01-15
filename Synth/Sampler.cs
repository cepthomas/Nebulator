

// TODON2 all the stk/chuck sampler stuff including applications.


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


///////////////////////////////////// applications //////////////////////////////////
///////////////////////////////////// applications //////////////////////////////////
///////////////////////////////////// applications //////////////////////////////////


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
    0,0,0,0,0,0,0,0         // 120-127
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

