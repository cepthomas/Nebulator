using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nebulator.Test
{
    public partial class TestHost : UserControl
    {
        public TestHost()
        {
            InitializeComponent();
        }

        private void TestHost_Load(object sender, EventArgs e)
        {

        }

        public void Go()
        {
            MainForm mf = ParentForm as MainForm;
            TestSUT();

            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\test1.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\declarative.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\algorithmic.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\import.neb");
            //MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
        }

        void TestSUT()
        {
            TestRunner runner = new TestRunner();
            string[] cases = new string[] { "LWUT" };
            //string[] cases = new string[] { "LWUT_1", "LWUT_2" };
            runner.RunCases(cases);

            // Show results
            textViewer.Colors.Clear();
            textViewer.Clear();
            textViewer.Colors.Add("*** ", Color.Pink);
            textViewer.Colors.Add("!!! ", Color.Plum);
            textViewer.Colors.Add("--- ", Color.LightGreen);

            runner.Context.Lines.ForEach(l => textViewer.AddLine(l, false));
        }
    }
}
