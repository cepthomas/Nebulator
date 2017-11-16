using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nebulator.Common;


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

            //TestGrid();

            //TestSimpleUT();

            //Utils.ExtractAPI(@"C:\Dev\GitHub\Nebulator\Nebulator\Scripting\ScriptUi.cs");


            mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\test1.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\declarative.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\algorithmic.neb");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
        }

        void TestGrid()
        {
            MainForm mf = ParentForm as MainForm;
            Random rand = new Random(111);

            mf.grid1.ToolTipEvent += ((s, e) => e.Text = "TT_" + rand.Next().ToString());

            List<PointF> data = new List<PointF>();
            for (int i = 0; i < 10; i++)
            {
                data.Add(new PointF(i, rand.Next(20, 80)));
            }

            mf.grid1.InitData(data);
        }

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

        private void btnGo_Click(object sender, EventArgs e)
        {
            Go();
        }
    }
}
