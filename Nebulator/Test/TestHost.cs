using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Nebulator.Common;
using Nebulator.Scripting;


namespace Nebulator.Test
{
    public partial class TestHost : Form
    {
        MainForm mf = null;

        public TestHost(Form parent)
        {
            InitializeComponent();
            mf = parent as MainForm;
        }

        private void TestHost_Load(object sender, EventArgs e)
        {
            //TopMost = true;
        }

        public void Go()
        {
            //TestGrid();

            //TestSimpleUT();

            //Utils.ExtractAPI(@"C:\Dev\GitHub\Nebulator\Nebulator\Scripting\ScriptUi.cs");

            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example1.neb");
           // mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example2.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example3.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\lsys.neb");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
        }

        // TODO2 graphics faster: - try drawRecursive() in script. SharpDx? Separate thread? https://stackoverflow.com/questions/26220964/sharpdxhow-to-place-sharpdx-window-in-winforms-window


        /// <summary>
        /// Tester for chart/grid.
        /// </summary>
        void TestGrid()
        {
            MainForm mf = ParentForm as MainForm;
            Random rand = new Random(111);

            //mf.grid1.ToolTipEvent += ((s, e) => e.Text = "TT_" + rand.Next().ToString());

            List<PointF> data = new List<PointF>();
            for (int i = 0; i < 10; i++)
            {
                data.Add(new PointF(i, rand.Next(20, 80)));
            }

            //mf.grid1.InitData(data);
        }

        /// <summary>
        /// Tester for simple UT.
        /// </summary>
        void TestSimpleUT()
        {
            TestRunner runner = new TestRunner();
            string[] cases = new string[] { "SUT" };
            //string[] cases = new string[] { "SUT_1", "SUT_2" };
            runner.RunCases(cases);

            // Show results
            textViewer.Colors.Clear();
            textViewer.Clear();
            textViewer.Colors.Add("*** ", Color.Pink);
            textViewer.Colors.Add("!!! ", Color.Plum);
            textViewer.Colors.Add("--- ", Color.LightGreen);

            runner.Context.Lines.ForEach(l => textViewer.AddLine(l, false));
        }

        /// <summary>
        /// Go man go!
        /// </summary>
        void Go_Click(object sender, EventArgs e)
        {
            Go();
        }
    }
}
