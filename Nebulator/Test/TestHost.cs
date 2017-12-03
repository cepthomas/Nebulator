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

            mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example1.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\example2.neb");
            //mf.OpenFile(@"C:\Dev\GitHub\Nebulator\Examples\lsys.neb");

            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Mambo.sty");
            //var v = MidiUtils.ImportStyle(@"C:\Users\cet\SkyDrive\OneDrive Documents\nebulator\midi\styles-jazzy\Funk.sty");
            //Clipboard.SetText(string.Join(Environment.NewLine, v));
        }

        // TODO2 graphics faster: - try drawRecursive() in script. SharpDx? Separate thread? https://stackoverflow.com/questions/26220964/sharpdxhow-to-place-sharpdx-window-in-winforms-window







        #region new lsys for audio ??

        // scales to play with. assumes [0] is the key.
        //int[] MajorScale = new[] { 1, 2, 3, 4, 5, 6, 7 };
        //int[] NaturalMinorScale = new[] { 1, 2, b3, 4, 5, b6, b7 };
        //Chord D#.4.M:   1 3 5

        // notes to play: which, when, dur.

        //GetScale("Algerian", "C.4");


        List<Point> lines = new List<Point>();

        class State
        {
            public string key = "C";



            public double size;
            public double angle;
            public double direction;
            public double x;
            public double y;
            public State Clone() { return (State)MemberwiseClone(); }
        }

        void genLSysMusic(double sizeGrowth, double angleGrowth, double initAngle, double initSize)
        {
            lines.Clear();

            var states = new Stack<State>();
            var str = "L";

            var state = new State()
            {
                x = 200,
                y = 200,
                direction = 0,
                size = initSize,
                angle = initAngle
            };

            lines.Clear();

            // The rules.
            var tbl = new Dictionary<char, string>
            {
                { 'L', "|-S!L!Y" },
                { 'S', "[F[FF-YS]F)G]+" },
                { 'Y', "--[F-)<F-FG]-" },
                { 'G', "FGF[Y+>F]+Y" }
            };

            // Rewrite.
            for (var i = 0; i < 8; i++) // was 12
            {
                var sb = new StringBuilder();
                foreach (var elt in str)
                {
                    if (tbl.ContainsKey(elt))
                    {
                        sb.Append(tbl[elt]);
                    }
                    else
                    {
                        sb.Append(elt);
                    }
                }
                str = sb.ToString();
            }

            // Make lines.
            foreach (var elt in str)
            {
                switch (elt)
                {
                    case 'F':
                        var new_x = state.x + state.size * Math.Cos(state.direction * Math.PI / 180.0);
                        var new_y = state.y + state.size * Math.Sin(state.direction * Math.PI / 180.0);

                        lines.Add(new Point((int)state.x, (int)state.y));
                        lines.Add(new Point((int)new_x, (int)new_y));

                        state.x = new_x;
                        state.y = new_y;
                        break;

                    case '+': state.direction += state.angle; break;
                    case '-': state.direction -= state.angle; break;
                    case '>': state.size *= (1.0 - sizeGrowth); break;
                    case '<': state.size *= (1.0 + sizeGrowth); break;
                    case ')': state.angle *= (1 + angleGrowth); break;
                    case '(': state.angle *= (1 - angleGrowth); break;
                    case '[': states.Push(state.Clone()); break;
                    case ']': state = states.Pop(); break;
                    case '!': state.angle *= -1.0; break;
                    case '|': state.direction += 180.0; break;

                    default:
                        break;
                }
            }
        }
        #endregion






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


        class MyScript : Script // Just for dev. TODO2 maybe useful later?
        {
            // Typical from compiler output.
            Track BALL { get { return Dynamic.Tracks["BALL"]; } }
            Sequence DYNAMIC_SEQ { get { return Dynamic.Sequences["DYNAMIC_SEQ"]; } }
            const int WHEN2 = 32;
            int MODN { get { return Dynamic.Vars["MODN"].Value; } set { Dynamic.Vars["MODN"].Value = value; } }


            // New stuff?
            public const int EVERY = -1;

            void Exec(int tick, Action act)
            {
                Exec(new Time(tick, 0), act);
            }

            void Exec(int[] ticks, Action act)
            {
                foreach (int tick in ticks) { Exec(new Time(tick, 0), act); }
            }

            void Exec(int tick, int tock, Action act)
            {
                Exec(new Time(tick, tock), act);
            }

            void Exec(Time time, Action act)
            {
                if (!ScriptActions.ContainsKey(time))
                {
                    ScriptActions.Add(time, new List<Action>());
                }
                ScriptActions[time].Add(act);
            }

            ///<summary>The main collection of actions. The key is the time to send the list.</summary>
            public Dictionary<Time, List<Action>> ScriptActions { get; set; } = new Dictionary<Time, List<Action>>();

            void SomeAction()
            {
                //int dodo = 99;
            }


            // From script source.
            ///// Play a sequence periodically.
            public void go2()
            {
                // Define script actions like this:
                Exec(1, () =>
                {
                    //int todo = 99;
                });

                // or like this:
                Exec(new[] { 2, 3, 8 }, () =>
                {
                    //int todo = 99;
                });

                // or like this:
                Exec(2, SomeAction);
                Exec(3, SomeAction);
                Exec(8, SomeAction);

                ///// New logic:
                Exec(new[] { 0, 16 }, () =>
                {
                    playSequence(BALL, DYNAMIC_SEQ);
                });
                Exec(8, () =>
                {
                    sendMidiNote(BALL, "D.4", 95, 0.00); // named note on, no chase
                });
                Exec(12, () =>
                {
                    sendMidiNote(BALL, 62, 0, 0.00); // numbered note off
                });
                Exec(new[] { 24, 25, 26, 27 }, () =>
                {
                    int notenum = (int)random(40, 70);
                    sendMidiNote(BALL, notenum, 95, 1.09);
                });


                ///// Original:
                if (tock == 0)
                {
                    // On the one - time to do something.
                    switch (tick)
                    {
                        case 0:
                        case 16:
                            playSequence(BALL, DYNAMIC_SEQ);
                            break;

                        case 8:
                            sendMidiNote(BALL, "D.4", 95, 0.00); // named note on, no chase
                            break;

                        case 12:
                            sendMidiNote(BALL, 62, 0, 0.00); // numbered note off
                            break;

                        case 24:
                        case 25:
                        case 26:
                        case 27:
                            int notenum = (int)random(40, 70);
                            sendMidiNote(BALL, notenum, 95, 1.09);
                            break;
                    }
                }
            }
        }
    }
}
