using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using NBagOfTricks.PNUT;
using Nebulator.Common;

namespace Nebulator.Test //TODO2??
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ScriptDefinitions.TheDefinitions.Init();

            TestRunner runner = new TestRunner(OutputFormat.Readable);
            string[] cases = new string[] { "MIDI" };
            runner.RunSuites(cases);
        }
    }
}
