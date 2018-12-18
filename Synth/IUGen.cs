using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

// TODOX On the performance front, the killer is the .NET garbage collector. You have to hope that it
//doesn't kick in during a callback. With ASIO you often run at super low latencies (<10ms), and
//the garbage collector can cause a callback to be missed, meaning you'd hear a glitch in the audio output.


//public class SignalGenerator : ISampleProvider
//public class AdsrSampleProvider : ISampleProvider
//public class MultiplexingSampleProvider : ISampleProvider

//C:\Users\cet\Desktop\sound-xxx\minim-cpp-master\src\ugens

//C:\Users\cet\Desktop\sound-xxx/Minim-master/src/main/java/ddf/minim/ugens/Abs.java

//C:\Users\cet\Desktop\sound-xxx/processing-sound-master/src/processing/sound/SawOsc.java

//jsyn-master/src/com/jsyn/unitgen/SineOscillator.java


namespace Nebulator.Synth
{
    /// <summary>Category types.</summary>
    public enum UGenType { Generator, Processor }

    interface IUGen : ISampleProvider
    {

        UGenType UGenType { get; }


        IUGen Input { get; }



        #region Properties
        /// <summary></summary>
        int NumInputs { get; }

        /// <summary></summary>
        int NumOutputs { get; }
        #endregion


        // Chuck: All ugen's have at least the following four parameters:
        // 
        // .gain - (double, READ/WRITE) - set gain.
        // .op - (int, READ/WRITE) - set operation type  
        //    0: stop - always output 0.
        //    1: normal operation, add all inputs (default).
        //    2: normal operation, subtract all inputs starting from the earliest connected.
        //    3: normal operation, multiply all inputs.    
        //    4: normal operation, divide inputs starting from the earliest connected.
        //    -1: passthru - all inputs to the ugen are summed and passed directly to output.  
        // .last - (double, READ/WRITE) - returns the last sample computed by the unit generator as a double.
        // .channels - (int, READ only) - the number channels on the UGen
        // .chan - (int) - returns a reference on a channel (0 -N-1)
        // .isConnectedTo( Ugen )  returns 1 if the unit generator connects to the argument, 0 otherwise.


        #region Functions
        /// Start a note with the given frequency and amplitude.
        void NoteOn(double frequency, double amplitude);
        /// Start envelope toward "on" target.
        //void keyOn();

        /// Stop a note with the given amplitude (speed of decay).
        void NoteOff(double amplitude = 0.0);
        /// Start envelope toward "off" target.
        //void keyOff();

        /// Perform the control change specified by id and value.
        void ControlChange(string controlId, object value);

        /// Perform the control change specified by number and value.
        //    void controlChange(int number, double value);

        /// Make a neb ugen from underlying library.
        bool Create(string utype, int id);


        ///
        void Clear();
        /// and/or
        void Reset();
        #endregion

    }
}


/*

#ifndef NEB_UGEN_H
#define NEB_UGEN_H

#include <QString>
#include <QVariant>
#include <QFile>
#include <QTextStream>

#include "Stk.h"
// #include "Sampler.h"
// #include "FormSwep.h"
// #include "BiQuad.h"
// #include "Noise.h"
// #include "BlitSaw.h"


using namespace stk;

namespace nebsyn {



//enum class ParamName { a0, a1, a2, allpass, attackRate, attackTime };
enum class ParamType { pdouble, pint, pdur };
enum class ParamAccess { rw, ro, wo };

//ParamName ParamType Access ParamDescription


typedef struct
{
    //ParamName name;
    QString name;
    ParamType type;
    ParamAccess access;
} UGenParam;

typedef void (*controlSetter)(Stkdouble);
//typedef void (UGen::*controlSetter)(Stkdouble);

typedef struct
{
    //ParamName name;
    QString name;
    int numIns;
    int numOuts;
    QMap<QString, UGenParam> params;
    QMap<QString, controlSetter> controlSetters;
} UGenDesc;


// We'll use our own frame def.
typedef struct
{
    Stkdouble left;
    Stkdouble right; // not used if mono
    //TickFrame() : left(qQNaN()), right(qQNaN()) { }
} TickFrame;


    
/////////////////////////////////////////////////////////

class UGen : public Stk // TODOXX
{

private:


public:


    // bool qIsNaN(double d)
    // double qQNaN()  Returns the bit pattern of a quiet NaN as a double.


public:
    /// Class constructor.
    UGen();

    /// Class destructor.
    ~UGen();

    /// Make a neb ugen from underlying library.
    bool create(QString& utype, int id);

    /// Make the collection once.
    static bool loadDescs();

    // Chuck: All ugen's have at least the following four parameters:
    // 
    // .gain - (double, READ/WRITE) - set gain.
    // .op - (int, READ/WRITE) - set operation type  
    //    0: stop - always output 0.
    //    1: normal operation, add all inputs (default).
    //    2: normal operation, subtract all inputs starting from the earliest connected.
    //    3: normal operation, multiply all inputs.    
    //    4: normal operation, divide inputs starting from the earliest connected.
    //    -1: passthru - all inputs to the ugen are summed and passed directly to output.  
    // .last - (double, READ/WRITE) - returns the last sample computed by the unit generator as a double.
    // .channels - (int, READ only) - the number channels on the UGen
    // .chan - (int) - returns a reference on a channel (0 -N-1)
    // .isConnectedTo( Ugen )  returns 1 if the unit generator connects to the argument, 0 otherwise.


    ///
    void clear();
    /// and/or
    void reset();


    // ///
    // const StkFrames& lastFrame() const { return _lastFrame; };
    // ///
    // Stkdouble lastOut(uint channel);
    // ///
    // uint channelsOut() const { return _lastFrame.channels(); };
    // ///
    // uint channelsIn() const { return _numIns; };


    /// Start a note with the given frequency and amplitude.
    void noteOn(Stkdouble frequency, Stkdouble amplitude);
    /// Start envelope toward "on" target.
    //void keyOn();

    /// Stop a note with the given amplitude (speed of decay).
    void noteOff(Stkdouble amplitude);
    /// Start envelope toward "off" target.
    //void keyOff();

    /// Perform the control change specified by id and value.
    void controlChange(const QString& controlId, QVariant value);
    /// Perform the control change specified by number and value.
//    void controlChange(int number, Stkdouble value);


    ////// tick functions ///////
    /// Compute and return one output sample.
    //virtual Stkdouble tick(uint channel = 0) = 0;

    TickFrame tick(); // for generators
    TickFrame tick(TickFrame input); // for processors

    // /// Input one or two sample(s).
    // void tick(Stkdouble input);
    // void tick(Stkdouble linput, Stkdouble rinput);
    // /// Fill a channel of the StkFrames object with computed outputs.
    // void tick(StkFrames& frames, uint channel);


    //////// Other functions ?? ////////
    /// Set instrument parameters for a particular frequency.
    //void setFrequency(Stkdouble frequency);



protected:
    // ADSR      adsr_; 
    // FileLoop* loop_;
    // OnePole   filter_;
    // BiQuad    biquad_;
    // Noise     noise_;
    // Stkdouble  baseFrequency_;
    // Stkdouble  loopGain_;


    //QString _utype;
    //QList<Param> _params;

    UGenDesc _desc;
    int _id;

    // Last output.
    //StkFrames _lastFrame;
    TickFrame _lastFrame;

    void* wrapped;
//    Stk* wrapped;

};


} // namespace

#endif
*/