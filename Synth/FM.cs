

//https://www.keithmcmillen.com/blog/simple-synthesis-part-10-frequency-modulation/


// ChucK/STK source code to be ported. TODON2

// info: http://electro-music.com/forum/topic-18641.html
// https://ccrma.stanford.edu/software/snd/snd/fm.html


// SinOsc modulator => SinOsc carrier => dac; 
// 2 => carrier.sync; // use carrier's input to modulate its frequency 
// 330 => modulator.freq; // modulator's frequency 
// 500 => modulator.gain; // modulation depth 
// 220 => carrier.freq; // carrier's frequency 


class Instrmnt : public Stk
{
 public:
  //! Default constructor.
  Instrmnt();

  //! Class destructor.
  virtual ~Instrmnt();

  //! Start a note with the given frequency and amplitude.
  virtual void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude) = 0;

  //! Stop a note with the given amplitude (speed of decay).
  virtual void noteOff(MY_FLOAT amplitude) = 0;

  //! Set instrument parameters for a particular frequency.
  virtual void setFrequency(MY_FLOAT frequency);

  //! Return the last output value.
  MY_FLOAT lastOut() const;

  //! Return the last left output value.
  MY_FLOAT lastOutLeft() const;

  //! Return the last right output value.
  MY_FLOAT lastOutRight() const;

  //! Compute one output sample.
  virtual MY_FLOAT tick() = 0;

  //! Computer \e vectorSize outputs and return them in \e vector.
  virtual MY_FLOAT *tick(MY_FLOAT *vector, unsigned int vectorSize);
  
  //! Perform the control change specified by \e number and \e value (0.0 - 128.0).
  virtual void controlChange(int number, MY_FLOAT value);

public: // SWAP formerly protected
  // chuck
  t_CKFLOAT m_frequency;

  MY_FLOAT lastOutput;
};



/***************************************************/
/*! \class FM
    \brief STK abstract FM synthesis base class.

    This class controls an arbitrary number of
    waves and envelopes, determined via a
    constructor argument.

    Control Change Numbers:
       - Control One = 2
       - Control Two = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


class FM : public Instrmnt
{
 public:
  //! Class constructor, taking the number of wave/envelope operators to control.
  FM( int operators = 4 );

  //! Class destructor.
  virtual ~FM();

  //! Reset and clear all wave and envelope states.
  void clear();

  //! Load the rawwave filenames in waves.
  void loadWaves(const char **filenames);

  //! Set instrument parameters for a particular frequency.
  virtual void setFrequency(MY_FLOAT frequency);

  //! Set the frequency ratio for the specified wave.
  void setRatio(int waveIndex, MY_FLOAT ratio);

  //! Set the gain for the specified wave.
  void setGain(int waveIndex, MY_FLOAT gain);

  //! Set the modulation speed in Hz.
  void setModulationSpeed(MY_FLOAT mSpeed);

  //! Set the modulation depth.
  void setModulationDepth(MY_FLOAT mDepth);

  //! Set the value of control1.
  void setControl1(MY_FLOAT cVal);

  //! Set the value of control1.
  void setControl2(MY_FLOAT cVal);

  //! Start envelopes toward "on" targets.
  void keyOn();

  //! Start envelopes toward "off" targets.
  void keyOff();

  //! Stop a note with the given amplitude (speed of decay).
  void noteOff(MY_FLOAT amplitude);

  //! Pure virtual function ... must be defined in subclasses.
  virtual MY_FLOAT tick() = 0;

  //! Perform the control change specified by \e number and \e value (0.0 - 128.0).
  virtual void controlChange(int number, MY_FLOAT value);

 public: // SWAP formerly protected  
  ADSR     **adsr; 
  WaveLoop **waves;
  WaveLoop *vibrato;
  TwoZero  *twozero;
  int nOperators;
  MY_FLOAT baseFrequency;
  MY_FLOAT *ratios;
  MY_FLOAT *gains;
  MY_FLOAT modDepth;
  MY_FLOAT control1;
  MY_FLOAT control2;
  MY_FLOAT __FM_gains[100];
  MY_FLOAT __FM_susLevels[16];
  MY_FLOAT __FM_attTimes[32];

};


FM :: FM(int operators)
    : nOperators(operators)
{
    if ( nOperators <= 0 )
    {
        char msg[256];
        sprintf(msg, "[chuck](via FM): Invalid number of operators (%d) argument to constructor!", operators);
        handleError(msg, StkError::FUNCTION_ARGUMENT);
    }

    twozero = new TwoZero();
    twozero->setB2( -1.0 );
    twozero->setGain( 0.0 );

    // Concatenate the STK rawwave path to the rawwave file
    vibrato = new WaveLoop( "special:sinewave", TRUE );
    vibrato->setFrequency(6.0);

    int i;
    ratios = (double *) new double[nOperators];
    gains = (double *) new double[nOperators];
    adsr = (ADSR **) calloc( nOperators, sizeof(ADSR *) );
    waves = (WaveLoop **) calloc( nOperators, sizeof(WaveLoop *) );
    for (i=0; i<nOperators; i++ )
    {
        ratios[i] = 1.0;
        gains[i] = 1.0;
        adsr[i] = new ADSR();
    }

    modDepth = (double) 0.0;
    control1 = (double) 1.0;
    control2 = (double) 1.0;
    baseFrequency = (double) 440.0;

    // chuck
    m_frequency = baseFrequency;

    double temp = 1.0;
    for (i=99; i>=0; i--)
    {
        __FM_gains[i] = temp;
        temp *= 0.933033;
    }

    temp = 1.0;
    for (i=15; i>=0; i--)
    {
        __FM_susLevels[i] = temp;
        temp *= 0.707101;
    }

    temp = 8.498186;
    for (i=0; i<32; i++)
    {
        __FM_attTimes[i] = temp;
        temp *= 0.707101;
    }
}

FM :: ~FM()
{
    delete vibrato;
    delete twozero;

    delete [] ratios;
    delete [] gains;
    for (int i=0; i<nOperators; i++ )
    {
        delete adsr[i];
        delete waves[i];
    }

    free(adsr);
    free(waves);
}

void FM :: loadWaves(const char **filenames )
{
    for (int i=0; i<nOperators; i++ )
        waves[i] = new WaveLoop( filenames[i], TRUE );
}

void FM :: setFrequency(double frequency)
{
    baseFrequency = frequency;

    for (int i=0; i<nOperators; i++ )
        waves[i]->setFrequency( baseFrequency * ratios[i] );

    // chuck
    m_frequency = baseFrequency;
}

void FM :: setRatio(int waveIndex, double ratio)
{
    if ( waveIndex < 0 )
    {
        CK_STDCERR << "[chuck](via STK): FM: setRatio waveIndex parameter is less than zero!" << CK_STDENDL;
        return;
    }
    else if ( waveIndex >= nOperators )
    {
        CK_STDCERR << "[chuck](via STK): FM: setRatio waveIndex parameter is greater than the number of operators!" << CK_STDENDL;
        return;
    }

    ratios[waveIndex] = ratio;
    if (ratio > 0.0)
        waves[waveIndex]->setFrequency(baseFrequency * ratio);
    else
        waves[waveIndex]->setFrequency(ratio);
}

void FM :: setGain(int waveIndex, double gain)
{
    if ( waveIndex < 0 )
    {
        CK_STDCERR << "[chuck](via STK): FM: setGain waveIndex parameter is less than zero!" << CK_STDENDL;
        return;
    }
    else if ( waveIndex >= nOperators )
    {
        CK_STDCERR << "[chuck](via STK): FM: setGain waveIndex parameter is greater than the number of operators!" << CK_STDENDL;
        return;
    }

    gains[waveIndex] = gain;
}

void FM :: setModulationSpeed(double mSpeed)
{
    vibrato->setFrequency(mSpeed);
}

void FM :: setModulationDepth(double mDepth)
{
    modDepth = mDepth;
}

void FM :: setControl1(double cVal)
{
    control1 = cVal * (double) 2.0;
}

void FM :: setControl2(double cVal)
{
    control2 = cVal * (double) 2.0;
}

void FM :: keyOn()
{
    for (int i=0; i<nOperators; i++ )
        adsr[i]->keyOn();
}

void FM :: keyOff()
{
    for (int i=0; i<nOperators; i++ )
        adsr[i]->keyOff();
}

void FM :: noteOff(double amplitude)
{
    keyOff();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "[chuck](via STK): FM: NoteOff amplitude = " << amplitude << CK_STDENDL;
#endif
}

void FM :: controlChange(int number, double value)
{
    double norm = value * ONE_OVER_128;
    if ( norm < 0 )
    {
        norm = 0.0;
        CK_STDCERR << "[chuck](via STK): FM: Control value less than zero!" << CK_STDENDL;
    }
    else if ( norm > 1.0 )
    {
        norm = 1.0;
        CK_STDCERR << "[chuck](via STK): FM: Control value exceeds nominal range!" << CK_STDENDL;
    }

    if (number == __SK_Breath_) // 2
        setControl1( norm );
    else if (number == __SK_FootControl_) // 4
        setControl2( norm );
    else if (number == __SK_ModFrequency_) // 11
        setModulationSpeed( norm * 12.0);
    else if (number == __SK_ModWheel_) // 1
        setModulationDepth( norm );
    else if (number == __SK_AfterTouch_Cont_)   // 128
    {
        //adsr[0]->setTarget( norm );
        adsr[1]->setTarget( norm );
        //adsr[2]->setTarget( norm );
        adsr[3]->setTarget( norm );
    }
    else
        CK_STDCERR << "[chuck](via STK): FM: Undefined Control Number (" << number << ")!!" << CK_STDENDL;

#if defined(_STK_DEBUG_)
    CK_STDCERR << "[chuck](via STK): FM: controlChange number = " << number << ", value = " << value << CK_STDENDL;
#endif
}








/***************************************************/
/*! \class FMVoices
    \brief STK singing FM synthesis instrument.

    This class implements 3 carriers and a common
    modulator, also referred to as algorithm 6 of
    the TX81Z.

    \code
    Algorithm 6 is :
                        /->1 -\
                     4-|-->2 - +-> Out
                        \->3 -/
    \endcode

    Control Change Numbers:
       - Vowel = 2
       - Spectral Tilt = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class FMVoices : public FM
{
 public:
  //! Class constructor.
  FMVoices();

  //! Class destructor.
  ~FMVoices();

  //! Set instrument parameters for a particular frequency.
  virtual void setFrequency(MY_FLOAT frequency);

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn( MY_FLOAT amplitude) { noteOn(baseFrequency, amplitude); }

  //! Compute one output sample.
  MY_FLOAT tick();

  //! Perform the control change specified by \e number and \e value (0.0 - 128.0).
  virtual void controlChange(int number, MY_FLOAT value);

 public: // SWAP formerly protected
  int currentVowel;
  MY_FLOAT tilt[3];
  MY_FLOAT mods[3];
};

FMVoices :: FMVoices()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( "special:sinewave", TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 2.00);
    this->setRatio(1, 4.00);
    this->setRatio(2, 12.0);
    this->setRatio(3, 1.00);

    gains[3] = __FM_gains[80];

    adsr[0]->setAllTimes( 0.05, 0.05, __FM_susLevels[15], 0.05);
    adsr[1]->setAllTimes( 0.05, 0.05, __FM_susLevels[15], 0.05);
    adsr[2]->setAllTimes( 0.05, 0.05, __FM_susLevels[15], 0.05);
    adsr[3]->setAllTimes( 0.01, 0.01, __FM_susLevels[15], 0.5);

    twozero->setGain( 0.0 );
    modDepth = (double) 0.005;
    currentVowel = 0;
    tilt[0] = 1.0;
    tilt[1] = 0.5;
    tilt[2] = 0.2;
    mods[0] = 1.0;
    mods[1] = 1.1;
    mods[2] = 1.1;
    baseFrequency = 110.0;
    setFrequency( 110.0 );
}

FMVoices :: ~FMVoices()
{
}

void FMVoices :: setFrequency(double frequency)
{
    double temp, temp2 = 0.0;
    int tempi = 0;
    unsigned int i = 0;

    if (currentVowel < 32)
    {
        i = currentVowel;
        temp2 = (double) 0.9;
    }
    else if (currentVowel < 64)
    {
        i = currentVowel - 32;
        temp2 = (double) 1.0;
    }
    else if (currentVowel < 96)
    {
        i = currentVowel - 64;
        temp2 = (double) 1.1;
    }
    else if (currentVowel <= 128)
    {
        i = currentVowel - 96;
        temp2 = (double) 1.2;
    }

    baseFrequency = frequency;
    temp = (temp2 * Phonemes::formantFrequency(i, 0) / baseFrequency) + 0.5;
    tempi = (int) temp;
    this->setRatio(0,(double) tempi);
    temp = (temp2 * Phonemes::formantFrequency(i, 1) / baseFrequency) + 0.5;
    tempi = (int) temp;
    this->setRatio(1,(double) tempi);
    temp = (temp2 * Phonemes::formantFrequency(i, 2) / baseFrequency) + 0.5;
    tempi = (int) temp;
    this->setRatio(2, (double) tempi);
    gains[0] = 1.0;
    gains[1] = 1.0;
    gains[2] = 1.0;

    // chuck
    m_frequency = baseFrequency;
}

void FMVoices :: noteOn(double frequency, double amplitude)
{
    this->setFrequency(frequency);
    tilt[0] = amplitude;
    tilt[1] = amplitude * amplitude;
    tilt[2] = tilt[1] * amplitude;
    this->keyOn();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "[chuck](via STK): FMVoices: NoteOn frequency = " << frequency << ", amplitude = " << amplitude << CK_STDENDL;
#endif
}

double FMVoices :: tick()
{
    register double temp, temp2;

    temp = gains[3] * adsr[3]->tick() * waves[3]->tick();
    temp2 = vibrato->tick() * modDepth * (double) 0.1;

    waves[0]->setFrequency(baseFrequency * (1.0 + temp2) * ratios[0]);
    waves[1]->setFrequency(baseFrequency * (1.0 + temp2) * ratios[1]);
    waves[2]->setFrequency(baseFrequency * (1.0 + temp2) * ratios[2]);
    waves[3]->setFrequency(baseFrequency * (1.0 + temp2) * ratios[3]);

    waves[0]->addPhaseOffset(temp * mods[0]);
    waves[1]->addPhaseOffset(temp * mods[1]);
    waves[2]->addPhaseOffset(temp * mods[2]);
    waves[3]->addPhaseOffset(twozero->lastOut());
    twozero->tick(temp);
    temp =  gains[0] * tilt[0] * adsr[0]->tick() * waves[0]->tick();
    temp += gains[1] * tilt[1] * adsr[1]->tick() * waves[1]->tick();
    temp += gains[2] * tilt[2] * adsr[2]->tick() * waves[2]->tick();

    return temp * 0.33;
}

void FMVoices :: controlChange(int number, double value)
{
    double norm = value * ONE_OVER_128;
    if ( norm < 0 )
    {
        norm = 0.0;
        CK_STDCERR << "[chuck](via STK): FMVoices: Control value less than zero!" << CK_STDENDL;
    }
    else if ( norm > 1.0 )
    {
        norm = 1.0;
        CK_STDCERR << "[chuck](via STK): FMVoices: Control value exceeds nominal range!" << CK_STDENDL;
    }


    if (number == __SK_Breath_) // 2
        gains[3] = __FM_gains[(int) ( norm * 99.9 )];
    else if (number == __SK_FootControl_)   // 4
    {
        currentVowel = (int) (norm * 128.0);
        this->setFrequency(baseFrequency);
    }
    else if (number == __SK_ModFrequency_) // 11
        this->setModulationSpeed( norm * 12.0);
    else if (number == __SK_ModWheel_) // 1
        this->setModulationDepth( norm );
    else if (number == __SK_AfterTouch_Cont_)   // 128
    {
        tilt[0] = norm;
        tilt[1] = norm * norm;
        tilt[2] = tilt[1] * norm;
    }
    else
        CK_STDCERR << "[chuck](via STK): FMVoices: Undefined Control Number (" << number << ")!!" << CK_STDENDL;

#if defined(_STK_DEBUG_)
    CK_STDCERR << "[chuck](via STK): FMVoices: controlChange number = " << number << ", value = " << value << CK_STDENDL;
#endif
}

/***************************************************/
/*! \class BeeThree
    \brief STK Hammond-oid organ FM synthesis instrument.

    This class implements a simple 4 operator
    topology, also referred to as algorithm 8 of
    the TX81Z.

    \code
    Algorithm 8 is :
                     1 --.
                     2 -\|
                         +-> Out
                     3 -/|
                     4 --
    \endcode

    Control Change Numbers:
       - Operator 4 (feedback) Gain = 2
       - Operator 3 Gain = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class BeeThree : public FM
{
 public:
  //! Class constructor.
  BeeThree();

  //! Class destructor.
  ~BeeThree();

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn( MY_FLOAT amplitude) { noteOn( baseFrequency, amplitude ); }
  //! Compute one output sample.
  MY_FLOAT tick();
};

BeeThree :: BeeThree()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( (Stk::rawwavePath() + "special:sinewave").c_str(), TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 0.999);
    this->setRatio(1, 1.997);
    this->setRatio(2, 3.006);
    this->setRatio(3, 6.009);

    gains[0] = __FM_gains[95];
    gains[1] = __FM_gains[95];
    gains[2] = __FM_gains[99];
    gains[3] = __FM_gains[95];

    adsr[0]->setAllTimes( 0.005, 0.003, 1.0, 0.01);
    adsr[1]->setAllTimes( 0.005, 0.003, 1.0, 0.01);
    adsr[2]->setAllTimes( 0.005, 0.003, 1.0, 0.01);
    adsr[3]->setAllTimes( 0.005, 0.001, 0.4, 0.03);

    twozero->setGain( 0.1 );
}


void BeeThree :: noteOn(double frequency, double amplitude)
{
    gains[0] = amplitude * __FM_gains[95];
    gains[1] = amplitude * __FM_gains[95];
    gains[2] = amplitude * __FM_gains[99];
    gains[3] = amplitude * __FM_gains[95];
    this->setFrequency(frequency);
    this->keyOn();
}

double BeeThree :: tick()
{
    register double temp;

    if (modDepth > 0.0)
    {
        temp = 1.0 + (modDepth * vibrato->tick() * 0.1);
        waves[0]->setFrequency(baseFrequency * temp * ratios[0]);
        waves[1]->setFrequency(baseFrequency * temp * ratios[1]);
        waves[2]->setFrequency(baseFrequency * temp * ratios[2]);
        waves[3]->setFrequency(baseFrequency * temp * ratios[3]);
    }

    waves[3]->addPhaseOffset(twozero->lastOut());
    temp = control1 * 2.0 * gains[3] * adsr[3]->tick() * waves[3]->tick();
    twozero->tick(temp);

    temp += control2 * 2.0 * gains[2] * adsr[2]->tick() * waves[2]->tick();
    temp += gains[1] * adsr[1]->tick() * waves[1]->tick();
    temp += gains[0] * adsr[0]->tick() * waves[0]->tick();

    lastOutput = temp * 0.125;
    return lastOutput;
}



/***************************************************/
/*! \class HevyMetl
    \brief STK heavy metal FM synthesis instrument.

    This class implements 3 cascade operators with
    feedback modulation, also referred to as
    algorithm 3 of the TX81Z.

    Algorithm 3 is :     4--\
                    3-->2-- + -->1-->Out

    Control Change Numbers:
       - Total Modulator Index = 2
       - Modulator Crossfade = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class HevyMetl : public FM
{
 public:
  //! Class constructor.
  HevyMetl();

  //! Class destructor.
  ~HevyMetl();

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn( MY_FLOAT amplitude) { noteOn(baseFrequency, amplitude); }

  //! Compute one output sample.
  MY_FLOAT tick();
};

HevyMetl :: HevyMetl()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( "special:sinewave", TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 1.0 * 1.000);
    this->setRatio(1, 4.0 * 0.999);
    this->setRatio(2, 3.0 * 1.001);
    this->setRatio(3, 0.5 * 1.002);

    gains[0] = __FM_gains[92];
    gains[1] = __FM_gains[76];
    gains[2] = __FM_gains[91];
    gains[3] = __FM_gains[68];

    adsr[0]->setAllTimes( 0.001, 0.001, 1.0, 0.01);
    adsr[1]->setAllTimes( 0.001, 0.010, 1.0, 0.50);
    adsr[2]->setAllTimes( 0.010, 0.005, 1.0, 0.20);
    adsr[3]->setAllTimes( 0.030, 0.010, 0.2, 0.20);

    twozero->setGain( 2.0 );
    vibrato->setFrequency( 5.5 );
    modDepth = 0.0;
}

HevyMetl :: ~HevyMetl()
{
}

void HevyMetl :: noteOn(double frequency, double amplitude)
{
    gains[0] = amplitude * __FM_gains[92];
    gains[1] = amplitude * __FM_gains[76];
    gains[2] = amplitude * __FM_gains[91];
    gains[3] = amplitude * __FM_gains[68];
    this->setFrequency(frequency);
    this->keyOn();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "HevyMetl: NoteOn frequency = " << frequency << ", amplitude = " << amplitude << CK_STDENDL;
#endif
}

double HevyMetl :: tick()
{
    register double temp;

    temp = vibrato->tick() * modDepth * 0.2;
    waves[0]->setFrequency(baseFrequency * (1.0 + temp) * ratios[0]);
    waves[1]->setFrequency(baseFrequency * (1.0 + temp) * ratios[1]);
    waves[2]->setFrequency(baseFrequency * (1.0 + temp) * ratios[2]);
    waves[3]->setFrequency(baseFrequency * (1.0 + temp) * ratios[3]);

    temp = gains[2] * adsr[2]->tick() * waves[2]->tick();
    waves[1]->addPhaseOffset(temp);

    waves[3]->addPhaseOffset(twozero->lastOut());
    temp = (1.0 - (control2 * 0.5)) * gains[3] * adsr[3]->tick() * waves[3]->tick();
    twozero->tick(temp);

    temp += control2 * (double) 0.5 * gains[1] * adsr[1]->tick() * waves[1]->tick();
    temp = temp * control1;

    waves[0]->addPhaseOffset(temp);
    temp = gains[0] * adsr[0]->tick() * waves[0]->tick();

    lastOutput = temp * 0.5;
    return lastOutput;
}



/***************************************************/
/*! \class Rhodey
    \brief STK Fender Rhodes-like electric piano FM
           synthesis instrument.

    This class implements two simple FM Pairs
    summed together, also referred to as algorithm
    5 of the TX81Z.

    \code
    Algorithm 5 is :  4->3--\
                             + --> Out
                      2->1--/
    \endcode

    Control Change Numbers:
       - Modulator Index One = 2
       - Crossfade of Outputs = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class Rhodey : public FM
{
 public:
  //! Class constructor.
  Rhodey();

  //! Class destructor.
  ~Rhodey();

  //! Set instrument parameters for a particular frequency.
  void setFrequency(MY_FLOAT frequency);

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn(MY_FLOAT amplitude) { noteOn(baseFrequency * 0.5, amplitude ); } 

  //! Compute one output sample.
  MY_FLOAT tick();
};

Rhodey :: Rhodey()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( "special:sinewave", TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 1.0);
    this->setRatio(1, 0.5);
    this->setRatio(2, 1.0);
    this->setRatio(3, 15.0);

    gains[0] = __FM_gains[99];
    gains[1] = __FM_gains[90];
    gains[2] = __FM_gains[99];
    gains[3] = __FM_gains[67];

    adsr[0]->setAllTimes( 0.001, 1.50, 0.0, 0.04);
    adsr[1]->setAllTimes( 0.001, 1.50, 0.0, 0.04);
    adsr[2]->setAllTimes( 0.001, 1.00, 0.0, 0.04);
    adsr[3]->setAllTimes( 0.001, 0.25, 0.0, 0.04);

    twozero->setGain((double) 1.0);
}

Rhodey :: ~Rhodey()
{
}

void Rhodey :: setFrequency(double frequency)
{
    baseFrequency = frequency * (double) 2.0;

    for (int i=0; i<nOperators; i++ )
        waves[i]->setFrequency( baseFrequency * ratios[i] );

    // chuck
    m_frequency = baseFrequency * .5;
}

void Rhodey :: noteOn(double frequency, double amplitude)
{
    gains[0] = amplitude * __FM_gains[99];
    gains[1] = amplitude * __FM_gains[90];
    gains[2] = amplitude * __FM_gains[99];
    gains[3] = amplitude * __FM_gains[67];
    this->setFrequency(frequency);
    this->keyOn();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "Rhodey: NoteOn frequency = " << frequency << ", amplitude = " << amplitude << CK_STDENDL;
#endif
}

double Rhodey :: tick()
{
    double temp, temp2;

    temp = gains[1] * adsr[1]->tick() * waves[1]->tick();
    temp = temp * control1;

    waves[0]->addPhaseOffset(temp);
    waves[3]->addPhaseOffset(twozero->lastOut());
    temp = gains[3] * adsr[3]->tick() * waves[3]->tick();
    twozero->tick(temp);

    waves[2]->addPhaseOffset(temp);
    temp = ( 1.0 - (control2 * 0.5)) * gains[0] * adsr[0]->tick() * waves[0]->tick();
    temp += control2 * 0.5 * gains[2] * adsr[2]->tick() * waves[2]->tick();

    // Calculate amplitude modulation and apply it to output.
    temp2 = vibrato->tick() * modDepth;
    temp = temp * (1.0 + temp2);

    lastOutput = temp * 0.5;
    return lastOutput;
}

/***************************************************/
/*! \class TubeBell
    \brief STK tubular bell (orchestral chime) FM
           synthesis instrument.

    This class implements two simple FM Pairs
    summed together, also referred to as algorithm
    5 of the TX81Z.

    \code
    Algorithm 5 is :  4->3--\
                             + --> Out
                      2->1--/
    \endcode

    Control Change Numbers:
       - Modulator Index One = 2
       - Crossfade of Outputs = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class TubeBell : public FM
{
 public:
  //! Class constructor.
  TubeBell();

  //! Class destructor.
  ~TubeBell();

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn( MY_FLOAT amplitude) { noteOn(baseFrequency, amplitude); }

  //! Compute one output sample.
  MY_FLOAT tick();
};

TubeBell :: TubeBell()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( "special:sinewave", TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 1.0   * 0.995);
    this->setRatio(1, 1.414 * 0.995);
    this->setRatio(2, 1.0   * 1.005);
    this->setRatio(3, 1.414 * 1.000);

    gains[0] = __FM_gains[94];
    gains[1] = __FM_gains[76];
    gains[2] = __FM_gains[99];
    gains[3] = __FM_gains[71];

    adsr[0]->setAllTimes( 0.005, 4.0, 0.0, 0.04);
    adsr[1]->setAllTimes( 0.005, 4.0, 0.0, 0.04);
    adsr[2]->setAllTimes( 0.001, 2.0, 0.0, 0.04);
    adsr[3]->setAllTimes( 0.004, 4.0, 0.0, 0.04);

    twozero->setGain( 0.5 );
    vibrato->setFrequency( 2.0 );
}

TubeBell :: ~TubeBell()
{
}

void TubeBell :: noteOn(double frequency, double amplitude)
{
    gains[0] = amplitude * __FM_gains[94];
    gains[1] = amplitude * __FM_gains[76];
    gains[2] = amplitude * __FM_gains[99];
    gains[3] = amplitude * __FM_gains[71];
    this->setFrequency(frequency);
    this->keyOn();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "TubeBell: NoteOn frequency = " << frequency << ", amplitude = " << amplitude << CK_STDENDL;
#endif
}

double TubeBell :: tick()
{
    double temp, temp2;

    temp = gains[1] * adsr[1]->tick() * waves[1]->tick();
    temp = temp * control1;

    waves[0]->addPhaseOffset(temp);
    waves[3]->addPhaseOffset(twozero->lastOut());
    temp = gains[3] * adsr[3]->tick() * waves[3]->tick();
    twozero->tick(temp);

    waves[2]->addPhaseOffset(temp);
    temp = ( 1.0 - (control2 * 0.5)) * gains[0] * adsr[0]->tick() * waves[0]->tick();
    temp += control2 * 0.5 * gains[2] * adsr[2]->tick() * waves[2]->tick();

    // Calculate amplitude modulation and apply it to output.
    temp2 = vibrato->tick() * modDepth;
    temp = temp * (1.0 + temp2);

    lastOutput = temp * 0.5;
    return lastOutput;
}

/***************************************************/
/*! \class Wurley
    \brief STK Wurlitzer electric piano FM
           synthesis instrument.

    This class implements two simple FM Pairs
    summed together, also referred to as algorithm
    5 of the TX81Z.

    \code
    Algorithm 5 is :  4->3--\
                             + --> Out
                      2->1--/
    \endcode

    Control Change Numbers:
       - Modulator Index One = 2
       - Crossfade of Outputs = 4
       - LFO Speed = 11
       - LFO Depth = 1
       - ADSR 2 & 4 Target = 128

    The basic Chowning/Stanford FM patent expired
    in 1995, but there exist follow-on patents,
    mostly assigned to Yamaha.  If you are of the
    type who should worry about this (making
    money) worry away.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/

class Wurley : public FM
{
 public:
  //! Class constructor.
  Wurley();

  //! Class destructor.
  ~Wurley();

  //! Set instrument parameters for a particular frequency.
  void setFrequency(MY_FLOAT frequency);

  //! Start a note with the given frequency and amplitude.
  void noteOn(MY_FLOAT frequency, MY_FLOAT amplitude);
  void noteOn(MY_FLOAT amplitude) { noteOn ( baseFrequency, amplitude ); }
  
  // CHUCK HACK:
  virtual void controlChange( int which, MY_FLOAT value );

  //! Compute one output sample.
  MY_FLOAT tick();
};

Wurley :: Wurley()
    : FM()
{
    // Concatenate the STK rawwave path to the rawwave files
    for ( int i=0; i<3; i++ )
        waves[i] = new WaveLoop( "special:sinewave", TRUE );
    waves[3] = new WaveLoop( "special:fwavblnk", TRUE );

    this->setRatio(0, 1.0);
    this->setRatio(1, 4.0);
    this->setRatio(2, -510.0);
    this->setRatio(3, -510.0);

    gains[0] = __FM_gains[99];
    gains[1] = __FM_gains[82];
    gains[2] = __FM_gains[92];
    gains[3] = __FM_gains[68];

    adsr[0]->setAllTimes( 0.001, 1.50, 0.0, 0.04);
    adsr[1]->setAllTimes( 0.001, 1.50, 0.0, 0.04);
    adsr[2]->setAllTimes( 0.001, 0.25, 0.0, 0.04);
    adsr[3]->setAllTimes( 0.001, 0.15, 0.0, 0.04);

    twozero->setGain( 2.0 );
    vibrato->setFrequency( 8.0 );
}

Wurley :: ~Wurley()
{
}

void Wurley :: setFrequency(double frequency)
{
    baseFrequency = frequency;
    waves[0]->setFrequency(baseFrequency * ratios[0]);
    waves[1]->setFrequency(baseFrequency * ratios[1]);
    waves[2]->setFrequency(ratios[2]);    // Note here a 'fixed resonance'.
    waves[3]->setFrequency(ratios[3]);

    // chuck
    m_frequency = baseFrequency;
}

void Wurley :: noteOn(double frequency, double amplitude)
{
    gains[0] = amplitude * __FM_gains[99];
    gains[1] = amplitude * __FM_gains[82];
    gains[2] = amplitude * __FM_gains[82];
    gains[3] = amplitude * __FM_gains[68];
    this->setFrequency(frequency);
    this->keyOn();

#if defined(_STK_DEBUG_)
    CK_STDCERR << "Wurley: NoteOn frequency = " << frequency << ", amplitude = " << amplitude << CK_STDENDL;
#endif
}

double Wurley :: tick()
{
    double temp, temp2;

    temp = gains[1] * adsr[1]->tick() * waves[1]->tick();
    temp = temp * control1;

    waves[0]->addPhaseOffset(temp);
    waves[3]->addPhaseOffset(twozero->lastOut());
    temp = gains[3] * adsr[3]->tick() * waves[3]->tick();
    twozero->tick(temp);

    waves[2]->addPhaseOffset(temp);
    temp = ( 1.0 - (control2 * 0.5)) * gains[0] * adsr[0]->tick() * waves[0]->tick();
    temp += control2 * 0.5 * gains[2] * adsr[2]->tick() * waves[2]->tick();

    // Calculate amplitude modulation and apply it to output.
    temp2 = vibrato->tick() * modDepth;
    temp = temp * (1.0 + temp2);

    lastOutput = temp * 0.5;
    return lastOutput;
}

// CHUCK HACK:
void Wurley :: controlChange( int which, double value )
{
    if( which == 3 )
    {
        adsr[0]->setAllTimes( 0.001, 1.50 * value, 0.0, 0.04);
        adsr[1]->setAllTimes( 0.001, 1.50 * value, 0.0, 0.04);
        adsr[2]->setAllTimes( 0.001, 0.25 * value, 0.0, 0.04);
        adsr[3]->setAllTimes( 0.001, 0.15 * value, 0.0, 0.04);
    }
    else
    {
        // call parent
        FM::controlChange( which, value );
    }
}

