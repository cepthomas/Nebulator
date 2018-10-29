using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;


// This file contains the most recent incarnation of a super simple unit test framework.
// The very original was based on Quicktest (http://www.tylerstreeter.net, http://quicktest.sourceforge.net/).
// Since then is has gone through many iterations and made many users happy. Now here's a .NET version.
// The original license is GNU Lesser General Public License OR BSD-style, which allows unrestricted use of the Quicktest code.


namespace PNUT
{
    /// <summary>Generate a human readable or junit format output.</summary>
    public enum OutputFormat { Readable, Xml };

    /// <summary>
    /// Accumulates general test info.
    /// </summary>
    public class TestContext
    {
        public OutputFormat Format { get; set; } = OutputFormat.Readable;
        public string CurrentSuiteId { get; set; } = "???";
        public bool CurrentSuitePass { get; set; } = true;
        public int NumSuitesRun { get; set; } = 0;
        public int NumSuitesFailed { get; set; } = 0;
        public bool CurrentCasePass { get; set; } = true;
        public int NumCasesRun { get; set; } = 0;
        public int NumCasesFailed { get; set; } = 0;

        public List<string> OutLines { get; set; } = new List<string>();
        public List<string> PropLines { get; set; } = new List<string>();
    }

    /// <summary>
    /// Specific exception type.
    /// </summary>
    class AssertException : Exception
    {
        public string File { get; }
        public int Line { get; }

        public AssertException(string msg, string file, int line) : base(msg)
        {
            File = file;
            Line = line;
        }
    }

    /// <summary>
    /// The orchestrator of the test execution.
    /// </summary>
    public class TestRunner
    {
        /// <summary>Format string.</summary>
        const string TIME_FORMAT = @"hh\:mm\:ss\.fff";

        /// <summary>Format string.</summary>
        const string DATE_TIME_FORMAT = "yyyy'-'MM'-'dd HH':'mm':'ss";

        /// <summary>Format string.</summary>
        const string DATE_TIME_FORMAT_MSEC = "yyyy'-'MM'-'dd HH':'mm':'ss.fff";

        /// <summary>The test context.</summary>
        public TestContext Context { get; } = new TestContext();

        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TestRunner(OutputFormat fmt)
        {
            Context.Format = fmt;
        }

        /// <summary>
        /// Run selected cases.
        /// </summary>
        /// <param name="which">List of names of test cases to run. If the test case names begin with these values they will run.</param>
        public void RunSuites(string[] which)
        {
            // Locate the test cases.
            Dictionary<string, TestSuite> suites = new Dictionary<string, TestSuite>();

            var tt = Assembly.GetExecutingAssembly().GetTypes();

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.BaseType != null && t.BaseType.Name.Contains("TestSuite"))
                {
                    // It's a test suite. Is it requested?
                    foreach (string ssuite in which)
                    {
                        if (t.Name.StartsWith(ssuite))
                        {
                            suites.Add(t.Name, Activator.CreateInstance(t) as TestSuite);
                        }
                    }
                }
            }

            DateTime startTime = DateTime.Now;

            // Run through to execute suites.
            foreach (string ss in suites.Keys)
            {
                Context.CurrentSuiteId = ss;
                TestSuite  tc = suites[ss];
                tc.Context = Context;
                Context.CurrentSuitePass = true;
                Context.CurrentCasePass = true;
                Context.PropLines.Clear();

                // Document the start of the suite.
                switch(Context.Format)
                {
                    case OutputFormat.Xml:
                        tc.RecordVerbatim($"    <testsuite name = {ss}>");
                        break;

                    case OutputFormat.Readable:
                        tc.RecordVerbatim($"Suite {ss}");
                        break;
                }

                try
                {
                    // Run the suite.
                    tc.RunSuite();
                }
                catch (AssertException ex)
                {
                    // Deliberate exception.
                    tc.RecordResult(false, ex.Message, ex.File, ex.Line);
                }
                catch (Exception ex)
                {
                    // Out of scope exception. Top frame contains the cause.
                    StackTrace st = new StackTrace(ex, true);
                    StackFrame frame = st.GetFrame(0);

                    int line = frame.GetFileLineNumber();
                    string fn = Path.GetFileName(frame.GetFileName());
                    string msg = $"{ex.Message} ({fn}:{line})";

                    tc.RecordResult(false, msg, fn, line);
                }

                // Completed the suite, update the counts.
                Context.NumSuitesRun++;
                Context.NumCasesRun += tc.CaseCnt;

                switch (Context.Format)
                {
                    case OutputFormat.Xml:
                        // Any properties?
                        if (Context.PropLines.Count() > 0)
                        {
                            tc.RecordVerbatim($"        <properties>");
                            Context.PropLines.ForEach(l => tc.RecordVerbatim(l));
                            tc.RecordVerbatim($"        </properties>");
                        }

                        tc.RecordVerbatim($"    </testsuite>");
                        break;

                    case OutputFormat.Readable:
                        Context.OutLines.Add($"");
                        break;
                }
            }

            // Finished the test run, prepare the summary.
            DateTime endTime = DateTime.Now;
            TimeSpan dur = endTime - startTime;
            string sdur = dur.ToString(TIME_FORMAT);
            List<string> preamble = new List<string>();

            switch (Context.Format)
            {
                case OutputFormat.Xml:
                    preamble.Add($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    preamble.Add($"<testsuites tests={Context.NumCasesRun} failures={Context.NumCasesFailed} time={sdur} >");
                    break;

                case OutputFormat.Readable:
                    string pass = Context.NumCasesFailed > 0 ? "Fail" : "Pass";

                    preamble.Add($"#------------------------------------------------------------------");
                    preamble.Add($"# Unit Test Report");
                    preamble.Add($"# Start Time: {startTime.ToString(DATE_TIME_FORMAT_MSEC)}");
                    preamble.Add($"# Duration: {sdur}");
                    //preamble.Add($"# Suites Run: {Context.NumSuitesRun}");
                    //preamble.Add($"# Suites Failed: {Context.NumSuitesFailed}");
                    preamble.Add($"# Cases Run: {Context.NumCasesRun}");
                    preamble.Add($"# Cases Failed: {Context.NumCasesFailed}");
                    preamble.Add($"# Test Result: {pass}");
                    preamble.Add($"#------------------------------------------------------------------");
                    break;
            }

            Context.OutLines.InsertRange(0, preamble);
            if(Context.Format == OutputFormat.Xml)
            {
                Context.OutLines.Add($"</testsuites>");
            }

            Context.OutLines.ForEach(l => Console.WriteLine(l));
#if DEBUG
            Context.OutLines.ForEach(l => Debug.WriteLine(l));
#endif

        }
    }

    /// <summary>
    /// The class that encapsulates an individual test suite.
    /// </summary>
    public abstract class TestSuite
    {
        const string UNKNOWN_FILE = "???";
        const int UNKNOWN_LINE = -1;

        public int CaseCnt { get; set; } = 0;
        public TestContext Context { get; set; } = null;

        // All test case specifications must supply this.
        public abstract void RunSuite();

        /// <summary>
        /// Record a test result.
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="message"></param>
        /// <param name="file"></param>
        /// <param name="line"></param>
        public void RecordResult(bool pass, string message, string file, int line)
        {
            CaseCnt++;

            if (pass)
            {
                switch (Context.Format)
                {
                    case OutputFormat.Xml:
                        Context.OutLines.Add($"        <testcase name=\"{Context.CurrentSuiteId}.{CaseCnt}\" classname=\"{Context.CurrentSuiteId}\" />");
                        break;

                    case OutputFormat.Readable:
                        break;
                }
            }
            else
            {
                switch (Context.Format)
                {
                    case OutputFormat.Xml:
                        Context.OutLines.Add($"        <testcase name=\"{Context.CurrentSuiteId}.{CaseCnt} \" classname=\"{Context.CurrentSuiteId}\">");
                        Context.OutLines.Add($"            <failure message=\"{file}:{line} {message}\"></failure>");
                        Context.OutLines.Add($"        </testcase>");
                        break;

                    case OutputFormat.Readable:
                        Context.OutLines.Add($"! ({file}:{line}) {Context.CurrentSuiteId}.{CaseCnt} {message}");
                        break;
                }
            }
        }

        /// <summary>
        /// Record a property into the report.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void RecordProperty(string name, string value)
        {
            switch (Context.Format)
            {
                case OutputFormat.Xml:
                    Context.PropLines.Add($"            <property name=\"{name}\" value=\"{value}\" />");
                    break;

                case OutputFormat.Readable:
                    Context.OutLines.Add($"Property {name}:{value}");
                    break;
            }
        }

        /// <summary>
        /// Record a verbatim text line into the report.
        /// </summary>
        /// <param name="message"></param>
        public void RecordVerbatim(string message)
        {
            Context.OutLines.Add(message);
        }

        #region Test macros - Boilerplate
        protected void UT_INFO(string message, params object[] vars)
        {
            if(Context.Format == OutputFormat.Readable)
            {
                RecordVerbatim($"{message} {string.Join(", ", vars)}");
            }
        }

        protected void UT_PROPERTY<T>(string name, T value)
        {
            RecordProperty(name, value.ToString());
        }
        #endregion

        #region Test macros - Basic
        // Checks whether the given condition is true.
        protected bool UT_TRUE(bool condition, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE)
        {
            bool pass = true;
            if (!condition)
            {
                RecordResult(false, $"condition should be true", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the given condition is false.
        protected bool UT_FALSE(bool condition, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE)
        {
            bool pass = true;
            if (condition)
            {
                RecordResult(false, $"condition should be false", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Prints the condition and gens assert/exception.
        protected void UT_ASSERT<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            if (value1.CompareTo(value2) != 0)
            {
                throw new AssertException($"[{value1}] != [{value2}]", file, line);
            }
        }
        #endregion

        #region Test macros - Comparers
        // Checks whether the first parameter is equal to the second.
        protected bool UT_EQUAL<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != 0)
            {
                RecordResult(false, $"[{value1}] != [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is not equal to the second.
        protected bool UT_NOT_EQUAL<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == 0)
            {
                RecordResult(false, $"[{value1}] == [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is less than the second.
        protected bool UT_LESS<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != -1)
            {
                RecordResult(false, $"[{value1}] not less than [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is less than or equal to the second.
        protected bool UT_LESS_OR_EQUAL<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == 1)
            {
                RecordResult(false, $"[{value1}] not less than or equal [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is greater than the second.
        protected bool UT_GREATER<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) != 1)
            {
                RecordResult(false, $"[{value1}] not greater than [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is greater than or equal to the second.
        protected bool UT_GREATER_OR_EQUAL<T>(T value1, T value2, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE) where T : IComparable
        {
            bool pass = true;
            if (value1.CompareTo(value2) == -1)
            {
                RecordResult(false, $"[{value1}] not greater than or equal [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }

        // Checks whether the first parameter is within the given tolerance from the second parameter.
        // This is useful for comparing floating point values.
        protected bool UT_CLOSE(double value1, double value2, double tolerance, [CallerFilePath] string file = UNKNOWN_FILE, [CallerLineNumber] int line = UNKNOWN_LINE)
        {
            bool pass = true;
            if (Math.Abs(value1 - value2) > tolerance)
            {
                RecordResult(false, $"[{value1}] not close enough to [{value2}]", file, line);
                pass = false;
            }
            else
            {
                RecordResult(true, $"", file, line);
            }
            return pass;
        }
        #endregion
    }
}
