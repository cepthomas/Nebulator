using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PNUT;


namespace Nebulator.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            TestRunner runner = new TestRunner(OutputFormat.Readable);
            string[] cases = new string[] { "SYNTH" /*, "OSC", "MIDI", "SERVER"*/ };
            runner.RunSuites(cases);
        }
    }
}
