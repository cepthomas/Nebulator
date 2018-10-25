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
        static void Main(string[] args)
        {
            TestRunner runner = new TestRunner(OutputFormat.Xml);
            string[] cases = new string[] { "OSC" };
            runner.RunSuites(cases);
        }
    }
}
