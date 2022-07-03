
public class utils : ScriptBase
{
    // Define some Neb vars.
    const double VEL = 0.8;


    ///// Functions /////

    // int notavar = 911;

    public bool Boing(int notenum = 0)
    {
        bool boinged = false;

        // Print("boing");
        if(notenum == 0)
        {
            notenum = Random(30, 80);
            boinged = true;
        }

        //SendNote("synth", notenum, VEL, 1.0);
        return boinged;
    }
}


// Example of an included class.
public class Dummy : ScriptBase
{
    int _thing = 0;

    public Dummy(int thing)
    {
        _thing = thing;
    }

    public int GetThing(int value)
    {
        return _thing * Random(value);
    }
}
