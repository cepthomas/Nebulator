
//TODON3 STK leftover filters, generators, samplers, instruments.  Eventually remove any unused.

using System;

namespace Nebulator.Synth
{


#if MAYBE_PORT_THIS

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
        m_time = m_target / (_rate * SynthCommon.SampleRate);
    }

    public void Key(bool on)
    {
        _target = on ? m_target : 0.0;
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

    //////// these - also check for valid values.

    void setRate(double aRate)
    {
        _rate = aRate;

        m_time = (_target - _value) / (_rate * SynthCommon.SampleRate);

        if( m_time < 0.0 )
            m_time = -m_time;
    }

    void setTime(double aTime)
    {
        if( aTime == 0.0 )
            _rate = double.MaxValue;
        else
            _rate = (_target - _value) / (aTime * SynthCommon.SampleRate);

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

#endif
}