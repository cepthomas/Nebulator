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
using Nebulator.Controls;
using Nebulator.UI;


namespace Nebulator.Test
{
    public partial class TestHost : Form
    {
        MainForm _mf = null;

        public TestHost(Form parent)
        {
            InitializeComponent();
            _mf = parent as MainForm;
        }

        private void TestHost_Load(object sender, EventArgs e)
        {
            TopMost = true;
            Go();
        }

        public void Go()
        {
            TestEditor();

            //TestGrid();

            //TestSimpleUT();

            //Utils.ExtractAPI(@"C:\Dev\GitHub\Nebulator\Nebulator\Scripting\ScriptUi.cs");

            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example1.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example2.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example3.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\lsys.neb");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
        }

        /// <summary>
        /// Tester for chart/grid.
        /// </summary>
        void TestEditor()
        {
            NebEditor ned = new NebEditor() { Dock = DockStyle.Fill };
            splitContainer1.Panel1.Controls.Add(ned);
            ned.Init(@"C:\Dev\GitHub\Nebulator\Examples\example.neb");
            ned.Show();
        }

        /// <summary>
        /// Tester for chart/grid.
        /// </summary>
        void TestGrid()
        {
            Grid grid = new Grid() { Dock = DockStyle.Fill };
            splitContainer1.Panel1.Controls.Add(grid);
            Random rand = new Random(111);
            grid.ToolTipEvent += ((s, e) => e.Text = "TT_" + rand.Next().ToString());
            List<PointF> data = new List<PointF>();
            for (int i = 0; i < 10; i++)
            {
                data.Add(new PointF(i, rand.Next(20, 80)));
            }
            grid.InitData(data);
            grid.Show();
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
