using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;
using Nebulator.Common;


// This file contains the most recent incarnation of a super simple unit test framework.
// The very original was based on Quicktest (http://www.tylerstreeter.net, http://quicktest.sourceforge.net/).
// Since then is has gone through many iterations and made many users happy. Now here's a .NET version.
// The original license is GNU Lesser General Public License OR BSD-style, which allows unrestricted use of the Quicktest code.


namespace Nebulator.Test
{
    /// <summary>
    /// Accumulates general test info.
    /// </summary>
    public class TestContext
    {
        // Properties.
        public bool CurrentCasePass { get; set; } = true;
        public int NumCasesRun { get; set; } = 0;
        public int NumCasesFailed { get; set; } = 0;
        public bool CurrentStepPass { get; set; } = true;
        public int NumStepsRun { get; set; } = 0;
        public int NumStepsFailed { get; set; } = 0;

        public List<string> Lines { get; set; } = new List<string>();

        public void WriteLine(string s)
        {
            Lines.Add(s);
        }

        public void Reset()
        {
            CurrentCasePass = true;
            NumCasesRun = 0;
            NumCasesFailed = 0;
            CurrentStepPass = true;
            NumStepsRun = 0;
            NumStepsFailed = 0;
        }
    }

    /// <summary>
    /// Specific exception type.
    /// </summary>
    class AssertException : Exception
    {
        public string AssertInfo { get; }

        public AssertException(string msg)
        {
            AssertInfo = msg;
        }
    }

    /// <summary>
    /// The orchestrator of the test execution.
    /// </summary>
    public class TestRunner
    {
        /// <summary>Format string.</summary>
        public const string DATE_TIME_FORMAT = "yyyy'-'MM'-'dd HH':'mm':'ss";

        /// <summary>Format string.</summary>
        public const string DATE_TIME_FORMAT_MSEC = "yyyy'-'MM'-'dd HH':'mm':'ss.fff";

        /// <summary>The test context.</summary>
        public TestContext Context { get; } = new TestContext();

        /// <summary>
        /// Constructor.
        /// </summary>
        public TestRunner()
        {
            Context.Reset();
        }

        /// <summary>
        /// Run selected cases.
        /// </summary>
        /// <param name="which">List of names of test cases to run. If the test case names begin with these values they will run.</param>
        public void RunCases(string[] which)
        {
            // Locate the test cases.
            Dictionary<string, TestCase> cases = new Dictionary<string, TestCase>();

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.BaseType != null && t.BaseType.Name == "TestCase")
                {
                    // It's a test case. Is it requested?
                    foreach (string scase in which)
                    {
                        if (t.Name.StartsWith(scase))
                        {
                            cases.Add(t.Name, Activator.CreateInstance(t) as TestCase);
                        }
                    }
                }
            }

            // Seed randomizer for random data generation.
            int seed = DateTime.Now.Millisecond;
            Random rand = new Random(seed);

            string sTime = DateTime.Now.ToString(DATE_TIME_FORMAT_MSEC);

            Context.WriteLine($"#------------------------------------------------------------------");
            Context.WriteLine($"# Simple Unit Tester Report");
            Context.WriteLine($"# Start Time: {sTime}");
            Context.WriteLine($"# Randomizer: {seed}");
            Context.WriteLine($"#");
            Context.WriteLine($"# --- General information.");
            Context.WriteLine($"# !!! Something that should be verified by the test engineer.");
            Context.WriteLine($"# *** Test failure.");
            Context.WriteLine($"#--------------------------------------------------------------------");

            // Run through to execute cases.
            foreach (string sc in cases.Keys)
            {
                TestCase tc = cases[sc];
                tc.Context = Context;
                Context.CurrentCasePass = true;
                Context.CurrentStepPass = true;

                // Document the start of the case.
                tc.Record(TestCase.StepFlag.Comment, $"Start Case {sc}");

                try
                {
                    // Run the case.
                    tc.RunCase();
                }
                catch (AssertException ex)
                {
                    // Deliberate exception.
                    tc.Record(TestCase.StepFlag.Assert, ex);
                }
                catch (Exception ex)
                {
                    // Out of scope exception. Top frame contains the cause.
                    StackTrace st = new StackTrace(ex, true);
                    StackFrame frame = st.GetFrame(0);

                    int line = frame.GetFileLineNumber();
                    string fn = Path.GetFileName(frame.GetFileName());
                    string msg = $"{ex.Message} ({fn}:{line})";

                    tc.Record(TestCase.StepFlag.Error, msg);
                }

                // Completed the case, update the counts.
                Context.NumCasesRun++;
                Context.NumStepsRun += tc.StepCnt;
            }

            // Finished the test run, prepare the summary.
            sTime = DateTime.Now.ToString(DATE_TIME_FORMAT_MSEC);
            string pass = Context.NumStepsFailed > 0 ? "Fail" : "Pass";

            Context.WriteLine($"");
            Context.WriteLine($"#------------------------------------------------------------------");
            Context.WriteLine($"# End Time: {sTime}");
            Context.WriteLine($"# Cases Run: {Context.NumCasesRun}");
            Context.WriteLine($"# Cases Failed: {Context.NumCasesFailed}");
            Context.WriteLine($"# Steps Run: {Context.NumStepsRun}");
            Context.WriteLine($"# Steps Failed: {Context.NumStepsFailed}");
            Context.WriteLine($"# Test Result: {pass}");
            Context.WriteLine($"#------------------------------------------------------------------");
        }
    }

    /// <summary>
    /// The class that encapsulates an individual test case.
    /// </summary>
    public abstract class TestCase
    {
        public enum StepFlag { None, Error, Comment, Inspect, Assert };

        Random _rand = new Random();

        // Properties
        public int StepCnt { get; set; }
        public TestContext Context { get; set; }

        // All test case specifications must supply this.
        public abstract void RunCase();

        /// <summary>
        /// Records something of interest.
        /// </summary>
        /// <param name="flag">Type of info</param>
        /// <param name="o">Usually either a string or an exception</param>
        public void Record(StepFlag flag, object o)
        {
            switch (flag)
            {
                case StepFlag.Error:
                case StepFlag.Assert:
                    // Update the states and counts.
                    if (Context.CurrentCasePass)
                    {
                        Context.CurrentCasePass = false;
                        Context.NumCasesFailed++;
                    }

                    if (Context.CurrentStepPass)
                    {
                        Context.CurrentStepPass = false;
                        Context.NumStepsFailed++;
                    }

                    // Output the error string with file/line.
                    int line = -1;
                    string fn = Definitions.UNKNOWN_STRING;
                    string msg = Definitions.UNKNOWN_STRING;
                    StackTrace st = null;

                    if (flag == StepFlag.Error)
                    {
                        st = new StackTrace(true);
                        msg = o as string;
                    }
                    else // Assert
                    {
                        st = new StackTrace(o as AssertException, true);
                        msg = (o as AssertException).AssertInfo;
                    }

                    foreach (StackFrame frame in st.GetFrames())
                    {
                        Console.WriteLine(frame.GetMethod().Name);
                        if (frame.GetMethod().Name == "RunCase")
                        {
                            line = frame.GetFileLineNumber();
                            fn = Path.GetFileName(frame.GetFileName());
                            break;
                        }
                    }

                    string sout = $"*** {msg}";
                    if (line != -1)
                    {
                        sout += $" ({fn}:{line})";
                    }
                    Context.WriteLine(sout);
                    break;

                case StepFlag.Comment:
                    Context.WriteLine($"--- {o}");
                    break;

                case StepFlag.Inspect:
                    Context.WriteLine($"!!! {o}");
                    break;

                case StepFlag.None:
                    string s = o as string;
                    if (s.Length > 0)
                    {
                        Context.WriteLine($"    {s}");
                    }
                    else
                    {
                        Context.WriteLine($"");
                    }
                    break;
            }
        }

        #region Test macros - Boilerplate
        protected void UT_INFO(string message, params object[] vars)
        {
            Record(StepFlag.None, $"{message} {string.Join(", ", vars)}");
        }

        protected void UT_STEP(int num, string desc)
        {
            Record(StepFlag.Comment, $"Start Step {num} {desc}");
            StepCnt++;
        }

        // Prints the given message for inspection.
        protected void UT_INSPECT(string message, params object[] vars)
        {
            Record(StepFlag.Inspect, $"{message} {string.Join(", ", vars)}");
        }

        // Fails unconditionally and prints the given message.
        protected void UT_FAIL(string message, params object[] vars)
        {
            Record(StepFlag.Error, $"{message} {string.Join(", ", vars)}");
        }

        // Prints the condition and gens assert/exception.
        protected void UT_ASSERT<T>(T value1, T value2) where T : IComparable
        {
            if (value1.CompareTo(value2) != 0)
            {
                throw new AssertException($"{value1} != {value2}");
            }
        }
        #endregion

        #region Test macros - Strings
        protected bool UT_STR_EQUAL(string value1, string value2)
        {
            bool pass = true;
            if (value1 != value2)
            {
                Record(StepFlag.Error, $"{value1} != {value2}");
                pass = false;
            }
            return pass;
        }

        protected bool UT_STR_NOT_EQUAL(string value1, string value2)
        {
            bool pass = true;
            if (value1 == value2)
            {
                Record(StepFlag.Error, $"{value1} != {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the string is empty, otherwise prints the contents.
        protected bool UT_STR_EMPTY(string value)
        {
            bool pass = true;
            if (string.IsNullOrEmpty(value))
            {
                Record(StepFlag.Error, $"String empty");
                pass = false;
            }
            return pass;
        }
        #endregion

        #region Test macros - Comparers
        //Less than zero - The current instance precedes the object specified by the CompareTo method in the sort order.
        //Zero - This current instance occurs in the same position in the sort order as the object specified by the CompareTo method.
        //Greater than zero - This current instance follows the object specified by the CompareTo method in the sort order.

        // Checks whether the first parameter is equal to the second.
        protected bool UT_EQUAL<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != 0)
            {
                Record(StepFlag.Error, $"{value1} != {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is not equal to the second.
        protected bool UT_NOT_EQUAL<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == 0)
            {
                Record(StepFlag.Error, $"{value1} == {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is less than the second.
        protected bool UT_LESS<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != -1)
            {
                Record(StepFlag.Error, $"{value1} not less than {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is less than or equal to the second.
        protected bool UT_LESS_OR_EQUAL<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == 1)
            {
                Record(StepFlag.Error, $"{value1} not less than or equal {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is greater than the second.
        protected bool UT_GREATER<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != 1)
            {
                Record(StepFlag.Error, $"{value1} not greater than {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is greater than or equal to the second.
        protected bool UT_GREATER_OR_EQUAL<T>(T value1, T value2) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == -1)
            {
                Record(StepFlag.Error, $"{value1} not greater than or equal {value2}");
                pass = false;
            }
            return pass;
        }

        // Checks whether the first parameter is within the given tolerance from
        // the second parameter.  This is useful for comparing floating point values.
        protected bool UT_CLOSE(double value1, double value2, double tolerance)
        {
            bool pass = true;
            if (Math.Abs(value1 - value2) > tolerance)
            {
                Record(StepFlag.Error, $"{value1} not close enough to {value2}");
                pass = false;
            }
            return pass;
        }
        #endregion

        #region Test macros - Misc
        // Checks whether the given condition is true.
        protected bool UT_CHECK(bool condition, string info)
        {
            bool pass = true;
            if (!condition)
            {
                Record(StepFlag.Error, $"condition fail: {info}");
                pass = false;
            }
            return pass;
        }
        #endregion

        #region Utilities
        protected string RandStr(int num)
        {
            char[] chars = new char[num];
            for (int i = 0; i < num; i++)
            {
                chars[i] = (char)_rand.Next(32, 126); // printables
            }

            return new string(chars);
        }

        protected int RandRange(int min, int max)
        {
            return _rand.Next(min, max);
        }

        protected double RandRange(double min, double max)
        {
            double dr = _rand.NextDouble();
            double v = min + (max - min) * dr;
            return v;
        }
        #endregion
    }
}
