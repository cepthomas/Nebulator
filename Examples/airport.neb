
// A take on Eno's "algorithmic" Music for Airports - ported from github.com/teropa/musicforairports.js

Channel("sound",  midiout, 1,  Pad2Warm);

class TapeLoop
{
    public string snote;
    public BarTime duration;
    public BarTime delay;
    public BarTime nextStart;
    public TapeLoop(string nt, double dur, double del)
    {
        snote = nt;
        duration = new BarTime(dur);
        delay = new BarTime(del);
        nextStart = new BarTime(del);
    }
}

public class airport : ScriptBase
{
    const double SOUND_VOL = 0.8;

    // Possible loops.
    List<TapeLoop> _loops = new List<TapeLoop>();

    public override void Setup()
    {
        Tempo = 70;
        
        // Set up the _loops.
        _loops.Clear();
        // Key is Ab.
        _loops.Add(new TapeLoop("Ab4", 17.3,  8.1));
        _loops.Add(new TapeLoop("Ab5", 17.2,  3.1));
        // 3rd
        _loops.Add(new TapeLoop("C5",  21.1,  5.3));
        // 4th
        _loops.Add(new TapeLoop("Db5", 18.2,  12.3));
        // 5th
        _loops.Add(new TapeLoop("Eb5", 20.0,  9.2));
        // 6th
        _loops.Add(new TapeLoop("F4",  19.3,  4.2));
        _loops.Add(new TapeLoop("F5",  20.0,  14.1));
    }

    public override void Step()
    {
        foreach(TapeLoop l in _loops)
        {
            if(StepTime >= l.nextStart)
            {
                Print("Starting note", l.snote);
                SendNote("sound", l.snote, SOUND_VOL, l.duration);
                // Calc next time.
                l.nextStart = StepTime + l.delay + l.duration;
            }
        }
    }
}
