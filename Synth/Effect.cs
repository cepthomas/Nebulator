

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

