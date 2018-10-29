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
            TestRunner runner = new TestRunner(OutputFormat.Readable);
            string[] cases = new string[] { "OSC" };
            runner.RunSuites(cases);
        }
    }



    // TODOX this:
    //// Server debug stuff.
    //TestClient client = new TestClient();
    //Task.Run(async () => { await client.Run(); });


    //// Midi utils stuff.
    //ExportMidi("test.mid");

    //var v = MidiUtils.ImportFile(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
    //var v = MidiUtils.ImportFile(@"C:\Users\cet\OneDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
    //var v = MidiUtils.ImportFile(@"C:\Dev\Play1\AMBIENT.MID");
    //Clipboard.SetText(string.Join(Environment.NewLine, v));

}
