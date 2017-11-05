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


// This file contains the most recent incarnation of a dead nuts simple unit test framework.
// The very original was based on Quicktest (http://www.tylerstreeter.net, http://quicktest.sourceforge.net/).
// Since then is has gone through many iterations and made many users happy. Now here's a .NET version.
// The original license is GNU Lesser General Public License OR BSD-style, which allows unrestricted use of the Quicktest code.


namespace Nebulator.Test
{
    // Helper for config information.
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

    // The class that encapsulates an individual test case.
    public abstract class TestCase
    {
        public enum StepFlag { None, Error, Comment, Inspect };

        // Properties
        public int StepCnt { get; set; }
        public TestContext Context { get; set; }

        // All test case specifications must supply this.
        public abstract void RunCase();

        public void Record(StepFlag flag, string message)
        {
            switch (flag)
            {
                case StepFlag.Error:
                    {
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
                        string fn = Globals.UNKNOWN_STRING;
                        foreach (StackFrame frame in new StackTrace(true).GetFrames())
                        {
                            //Console.WriteLine(frame.GetMethod().Name);
                            if (frame.GetMethod().Name.Contains("RunCase"))
                            {
                                line = frame.GetFileLineNumber();
                                fn = Path.GetFileName(frame.GetFileName());
                            }
                        }

                        Context.WriteLine($"*** {fn}:{line} {message}");
                    }
                    break;

                case StepFlag.Comment:
                    {
                        Context.WriteLine($"--- {message}");
                    }
                    break;

                case StepFlag.Inspect:
                    {
                        Context.WriteLine($"!!! {message}");
                    }
                    break;

                case StepFlag.None:
                    {
                        if (message.Length > 0)
                        {
                            Context.WriteLine($"    {message}");
                        }
                        else
                        {
                            Context.WriteLine($"");
                        }
                    }
                    break;
            }
        }

        #region Tests
        protected void UT_INFO(string s)
        {
            Record(StepFlag.None, s);
        }

        protected void UT_STEP(int num, string desc)
        {
            Record(StepFlag.Comment, $"Start Step {num} {desc}");
            StepCnt++;
        }

        protected void UT_STEP_END()
        {
            Record(StepFlag.Comment, $"Step Complete");
            Record(StepFlag.None, "");
        }

        protected bool UT_CHECK_STR_EQUAL(string value1, string value2)
        {
            bool pass = true;
            if (value1 != value2)
            {
                Record(StepFlag.Error, $"{value1} != {value2}");
                pass = false;
            }
            return pass;
        }

        protected bool UT_CHECK_STR_NOT_EQUAL(string value1, string value2)
        {
            bool pass = true;
            if (value1 != value2)
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should not equal " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        ////////////// TODO Implement all these:

        // Prints the test step description with step number. Has a parameter to enable or disable the step.
        protected void UT_STEP_ENB(int num, string desc, bool enb)
        {
            // std::ostringstream oss;
            // oss << "Step " << num << ": " << desc;
            // if(!enb) oss << " Not executed.";
            // Record(config, TestCase::None, __LINE__, oss.str());
            // mStepCnt++;
            // config.CurrentStepPass = true;
            // config.CurrentFile = __FILE__;
            // if(enb) {
        }

        // Prints the given message.
        protected void UT_INSP(string message, string parm)
        {
            // std::ostringstream oss;
            // oss << message << " Parm:" << parm;
            // Record(config, TestCase::Inspect, __LINE__, oss.str());
        }

        // Prints the condition and info and gens assert.
        protected void UT_ASSERT_1(int value1, int value2, string info)
        {
            if ((value1) != (value2))
            {
                // std::ostringstream oss;
                // oss << "Assert: " << info << " " << #value1 << "<" << value1 << "> should equal " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
                // assert(false);
            }
        }

        // Prints the condition and gens assert.
        protected void UT_ASSERT(int value1, int value2)
        {
            if ((value1) != (value2))
            {
                // std::ostringstream oss;
                // oss << "Assert" << " " << #value1 << "<" << value1 << "> should equal " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
                // assert(false);
            }
        }

        // Checks whether the given condition is true.
        protected bool UT_CHECK(bool condition)
        {
            bool pass = true;
            if (!(condition))
            {
                // std::ostringstream oss;
                // oss << "Condition failed:" << #condition;
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is equal to the second.
        protected bool UT_CHECK_EQUAL(int value1, int value2)
        {
            bool pass = true;
            if ((value1) != (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should equal " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is not equal to the second.
        protected bool UT_CHECK_NOT_EQUAL(int value1, int value2)
        {
            bool pass = true;
            if ((value1) == (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should not equal " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is less than the second.
        protected bool UT_CHECK_LESS(int value1, int value2)
        {
            bool pass = true;
            if ((value1) >= (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should be less than " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is less than or equal to the second.
        protected bool UT_CHECK_LESS_OR_EQUAL(int value1, int value2)
        {
            bool pass = true;
            if ((value1) > (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should be less than or equal to " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is greater than the second.
        protected bool UT_CHECK_GREATER(int value1, int value2)
        {
            bool pass = true;
            if ((value1) <= (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should be greater than " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is greater than or equal to the second.
        protected bool UT_CHECK_GREATER_OR_EQUAL(int value1, int value2)
        {
            bool pass = true;
            if ((value1) < (value2))
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should be greater than or equal to " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the first parameter is within the given tolerance from
        // the second parameter.  This is useful for comparing floating point values.
        protected bool UT_CHECK_CLOSE(float value1, float value2, float tolerance)
        {
            bool pass = true;
            if (Math.Abs((value1) - (value2)) > tolerance)
            {
                // std::ostringstream oss;
                // oss << #value1 << "<" << value1 << "> should be within " << tolerance << " of " << #value2 << "<" << value2 << ">";
                // Record(config, TestCase::Error, __LINE__, oss.str());
            }
            return pass;
        }

        // Checks whether the string is empty, otherwise prints the contents.
        protected bool UT_CHECK_STR_EMPTY(string value)
        {
            bool pass = true;
            // if (strlen(value))
            // {
            //     std::ostringstream oss;
            //     oss << #value << "<" << value << "> should be empty";
            //     Record(config, TestCase::Error, __LINE__, oss.str());
            // }
            return pass;
        }

        // Fails unconditionally and prints the given message.
        protected void UT_FAIL(string message)
        {
            //Record(config, TestCase::Error, __LINE__, (message));
        }
        #endregion

        #region Utilities
        protected string RandStr(int num) // TODO
        {
            return "not-real-string";
        }
        #endregion
    }

    // The orchestrator of the test execution.
    public class TestRunner
    {
        public const string DATE_TIME_FORMAT = "yyyy'-'MM'-'dd HH':'mm':'ss";
        public const string DATE_TIME_FORMAT_MSEC = "yyyy'-'MM'-'dd HH':'mm':'ss.fff";

        public TestContext Context { get; } = new TestContext();

        // Constructor.
        public TestRunner()
        {
            Context.Reset();
        }

        /// <summary>
        /// Run selected cases.
        /// </summary>
        /// <param name="which">List of names of test cases to run.</param>
        public void RunCases(string[] which)
        {
            // Locate the test cases.
            Dictionary<string, TestCase> cases = new Dictionary<string, TestCase>();

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.BaseType != null && t.BaseType.Name == "TestCase")
                {
                    cases.Add(t.Name, Activator.CreateInstance(t) as TestCase);
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
                // Is this case requested?
                foreach (string scase in which)
                {
                    if (sc.StartsWith(scase))
                    {
                        // New case. Reset states.
                        TestCase tc = cases[sc];
                        tc.Context = Context;
                        Context.CurrentCasePass = true;
                        Context.CurrentStepPass = true;

                        // Document the start of the case.
                        tc.Record(TestCase.StepFlag.Comment, $"Start Case {sc}");

                        // Run the case.
                        tc.RunCase();

                        // Completed the case, update the counts.
                        Context.NumCasesRun++;
                        Context.NumStepsRun += tc.StepCnt;
                    }
                }
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
}
