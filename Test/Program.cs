using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PNUT;

// TODOX2 - if you create an app with SkiaSharp, make sure to uncheck Build config box "Prefer 32 bit".

namespace Nebulator.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            TestRunner runner = new TestRunner(OutputFormat.Readable);
            string[] cases = new string[] { "SYNTH_Vis" /*, "OSC", "MIDI", "SERVER"*/ };
            runner.RunSuites(cases);
        }
    }
}
