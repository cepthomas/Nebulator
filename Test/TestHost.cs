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
using Nebulator.Script;


namespace Nebulator.Test
{
    public partial class TestHost : Form
    {
        public TestHost()
        {
            InitializeComponent();
        }

        private void TestHost_Load(object sender, EventArgs e)
        {
            // TopMost = true;

//            splitContainer1.Panel1.Controls.Add(new Script.Surface());


            Go_Click(null, null);
        }

        /// <summary>
        /// Go man go!
        /// </summary>
        void Go_Click(object sender, EventArgs e)
        {
            TestSimpleUT();

            // Utils.ExtractAPI(@"C:\Dev\Nebulator\Nebulator\Scripting\ScriptUi.cs");

            // var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            // var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            // Clipboard.SetText(string.Join(Environment.NewLine, v));

            // MainForm mf = new MainForm();
            // mf.OpenFile(@"C:\Dev\Nebulator\Examples\dev.np"); // airport  dev  example  lsys
        }

        /// <summary>
        /// Tester for simple UT.
        /// </summary>
        void TestSimpleUT()
        {
            TestRunner runner = new TestRunner();
            string[] cases = new string[] { "SUT", "SM" }; // { "SUT", "SUT_1", "SUT_2" };
            runner.RunCases(cases);

            // Show results
            textViewer.Clear();
            //textViewer.Colors.Clear();
            //textViewer.Colors.Add("*** ", Color.Pink);
            //textViewer.Colors.Add("!!! ", Color.Plum);
            //textViewer.Colors.Add("--- ", Color.LightGreen);

            //runner.Context.Lines.ForEach(l => textViewer.AddLine(l, false));
            runner.Context.Lines.ForEach(l => textViewer.AppendText(l));
        }
    }


    /// <summary>Entry point into the test application.</summary>
    static class Program
    {
        /// <summary>Main application thread.</summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            try
            {
                Application.Run(new TestHost());
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format($"Unhandled exception:{e.Message}{Environment.NewLine}{e.StackTrace}"));
            }
        }
    }
}
