using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Script;


// string fn = Path.GetTempFileName() + ".html";
// File.WriteAllText(fn, string.Join(Environment.NewLine, htmlText));
// new Process { StartInfo = new ProcessStartInfo(fn) { UseShellExecute = true } }.Start();

//dotnet build ...

// output like:
// C:\Dev\comp_files\example_defs.cs(201,19): warning CS0108: 'example.Volume' hides inherited member 'NebScript.Volume'. Use the new keyword if hiding was intended. [C:\Dev\comp_files\comp_files.csproj]


namespace Nebulator.UI // Probably not forever home
{
    /// <summary>
    /// Parses/compiles *.neb file(s).
    /// </summary>
    public class Compiler
    {
        /// <summary>
        /// Parser helper class.
        /// </summary>
        class FileContext
        {
            /// <summary>Current source file.</summary>
            public string SourceFile { get; set; } = Definitions.UNKNOWN_STRING;

            /// <summary>Current source line.</summary>
            public int LineNumber { get; set; } = 1;

            /// <summary>Current parse state.</summary>
            public string State { get; set; } = "idle";

            /// <summary>Accumulated script code lines.</summary>
            public List<string> CodeLines { get; set; } = new List<string>();
        }

        #region Properties
        /// <summary>Accumulated errors.</summary>
        public List<ScriptError> Errors { get; } = new List<ScriptError>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }

        /// <summary>Specifies the temp dir used so client can track down runtime errors.</summary>
        public string TempDir { get; set; } = "";

        /// <summary>Do not include some neb only components.</summary>
        public bool Min { get; set; } = true;
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("Compiler");

        /// <summary>Starting directory.</summary>
        string _baseDir = Definitions.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Definitions.UNKNOWN_STRING;

        /// <summary>Accumulated lines to go in the constructor.</summary>
        List<string> _initLines = new List<string>();

        /// <summary>Products of file process. Key is generated file name.</summary>
        Dictionary<string, FileContext> _filesToCompile = new Dictionary<string, FileContext>();

        /// <summary>All the definitions for internal use.</summary>
        Dictionary<string, int> _defs = new Dictionary<string, int>();
        #endregion

        #region Public functions
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to main file.</param>
        /// <returns>The newly minted script object or null if failed.</returns>
        public NebScript Execute(string nebfn)
        {
            NebScript script = null;

            // Reset everything.
            _filesToCompile.Clear();
            _initLines.Clear();

            Errors.Clear();

            if (nebfn != Definitions.UNKNOWN_STRING && File.Exists(nebfn))
            {
                _logger.Info($"Compiling {nebfn}.");

                ///// Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(nebfn);
                StringBuilder sb = new StringBuilder();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                _baseDir = Path.GetDirectoryName(nebfn);

                ///// Compile.
                DateTime startTime = DateTime.Now; // for metrics
                Parse(nebfn);
                script = Compile();

                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {nebfn}.");
            }

            return Errors.Count == 0 ? script : null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Top level compiler.
        /// </summary>
        /// <returns>Compiled script</returns>
        NebScript Compile()
        {
            NebScript script = null;

            try // many ways to go wrong...
            {
                // Set the compiler parameters.
                CompilerParameters cp = new CompilerParameters()
                {
                    GenerateExecutable = false,
                    //OutputAssembly = _scriptName, -- don't do this!
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = false,
                    IncludeDebugInformation = true
                };

                // The usual suspects.
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Core.dll");
                cp.ReferencedAssemblies.Add("System.Drawing.dll");
                cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                cp.ReferencedAssemblies.Add("System.Data.dll");
                cp.ReferencedAssemblies.Add("NAudio.dll");
                cp.ReferencedAssemblies.Add("NBagOfTricks.dll");
                cp.ReferencedAssemblies.Add("NebOsc.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Common.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Script.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Device.dll");

                // Add the generated source files.
                List<string> paths = new List<string>();

                // Create output area.
                TempDir = Path.Combine(_baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, ci.CodeLines);
                    //File.WriteAllLines(fullpath, Tools.FormatSourceCode(ci.CodeLines));
                    paths.Add(fullpath);
                }

                // Make it compile.
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, paths.ToArray());

                if (cr.Errors.Count == 0)
                {
                    Assembly assy = cr.CompiledAssembly;

                    // Bind to the script interface.
                    foreach (Type t in assy.GetTypes())
                    {
                        if (t.BaseType != null && t.BaseType.Name == "NebScript")
                        {
                            // We have a good script file. Create the executable object.
                            Object o = Activator.CreateInstance(t);
                            script = o as NebScript;
                        }
                    }

                    if (script is null)
                    {
                        throw new Exception("Could not instantiate script");
                    }
                }
                else
                {
                    foreach (CompilerError err in cr.Errors)
                    {
                        // The line should end with source line number: "//1234"
                        int origLineNum = 0; // defaults
                        string origFileName = Definitions.UNKNOWN_STRING;

                        // Dig out the offending source code information.
                        string fpath = Path.GetFileName(err.FileName.ToLower());
                        if (_filesToCompile.ContainsKey(fpath))
                        {
                            FileContext ci = _filesToCompile[fpath];
                            origFileName = ci.SourceFile;
                            string origLine = ci.CodeLines[err.Line - 1];
                            int ind = origLine.LastIndexOf("//");

                            if (origFileName == "" || ind == -1)
                            {
                                // Must be an internal error. Do the best we can.
                                Errors.Add(new ScriptError()
                                {
                                    ErrorType = err.IsWarning ? ScriptErrorType.Warning : ScriptErrorType.Error,
                                    SourceFile = err.FileName,
                                    LineNumber = err.Line,
                                    Message = $"InternalError: {err.ErrorText} in: {origLine}"
                                });
                            }
                            else
                            {
                                int.TryParse(origLine.Substring(ind + 2), out origLineNum);
                                Errors.Add(new ScriptError()
                                {
                                    ErrorType = err.IsWarning ? ScriptErrorType.Warning : ScriptErrorType.Error,
                                    SourceFile = origFileName,
                                    LineNumber = origLineNum,
                                    Message = err.ErrorText
                                });
                            }
                        }
                        else
                        {
                            Errors.Add(new ScriptError()
                            {
                                ErrorType = err.IsWarning ? ScriptErrorType.Warning : ScriptErrorType.Error,
                                SourceFile = "NoSourceFile",
                                LineNumber = -1,
                                Message = err.ErrorText
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new ScriptError()
                {
                    ErrorType = ScriptErrorType.Error,
                    Message = "Exception: " + ex.Message,
                    SourceFile = "",
                    LineNumber = 0
                });
            }

            return script;
        }

        /// <summary>
        /// Top level parser.
        /// </summary>
        /// <param name="nebfn">Topmost file in collection.</param>
        void Parse(string nebfn)
        {
            // Add the generated internal code files.
            _filesToCompile.Add($"{_scriptName}_wrapper.cs", new FileContext()
            {
                SourceFile = "",
                CodeLines = GenMainFileContents()
            });

            _filesToCompile.Add($"{_scriptName}_defs.cs", new FileContext()
            {
                SourceFile = "",
                CodeLines = GenDefFileContents()
            });

            // Start parsing from the main file. ParseOneFile is a recursive function.
            FileContext pcont = new FileContext()
            {
                SourceFile = nebfn,
                LineNumber = 1
            };

            ParseOneFile(pcont);
        }

        /// <summary>
        /// Parse one file. This is recursive to support nested #include.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool ParseOneFile(FileContext pcont)
        {
            bool valid = false;

            // Try fully qualified.
            if (File.Exists(pcont.SourceFile))
            {
                // OK - leave as is.
                valid = true;
            }
            else // Try relative.
            {
                string fn = Path.Combine(_baseDir, pcont.SourceFile);
                if (File.Exists(fn))
                {
                    pcont.SourceFile = fn;
                    valid = true;
                }
            }

            if (valid)
            {
                string genFn = $"{_scriptName}_src{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                ///// Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                ///// The content.
                ProcessScriptFile(pcont);

                ///// Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }

        /// <summary>
        /// Process one plain script file.
        /// </summary>
        /// <param name="pcont"></param>
        void ProcessScriptFile(FileContext pcont)
        {
            List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

            for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
            {
                string s = sourceLines[pcont.LineNumber - 1];

                // Remove any comments. Single line type only.
                int pos = s.IndexOf("//");
                string cline = pos >= 0 ? s.Left(pos) : s;

                // Test for nested files
                // #include "path\name.neb"
                // #include "include path\split file name.neb"
                if (s.StartsWith("#include"))
                {
                    List<string> parts = s.SplitByTokens("\";");
                    string fn = parts.Last();

                    // Recursive call to parse this file
                    FileContext subcont = new FileContext()
                    {
                        SourceFile = fn,
                        LineNumber = 1
                    };

                    if (!ParseOneFile(subcont))
                    {
                        Errors.Add(new ScriptError()
                        {
                            ErrorType = ScriptErrorType.Error,
                            Message = $"Invalid #include: {fn}",
                            SourceFile = pcont.SourceFile,
                            LineNumber = pcont.LineNumber
                        });
                    }
                }
                else
                {
                    if (cline != "")
                    {
                        // Store the whole line with line number tacked on.
                        pcont.CodeLines.Add($"        {cline} //{pcont.LineNumber}");
                    }
                }
            }   
        }

        /// <summary>
        /// Create the file containing all the nebulator glue.
        /// </summary>
        /// <returns></returns>
        List<string> GenMainFileContents()
        {
            // Create the main/generated file. Indicated by empty source file name.
            List<string> codeLines = GenTopOfFile("");

            // Collected init stuff goes in a constructor.
            // Reference to current script so nested classes have access to it. TODO1 fixed in C#9 with static using.
            codeLines.Add($"        protected static NebScript s;");
            codeLines.Add($"        public {_scriptName}() : base()");
            codeLines.Add( "        {");
            codeLines.Add( "            s = this;");
            _initLines.ForEach(l => codeLines.Add("            " + l));
            codeLines.Add( "        }");

            // Bottom stuff.
            codeLines.AddRange(GenBottomOfFile());

            return codeLines;
        }

        /// <summary>
        /// Create the file containing definitions.
        /// </summary>
        /// <returns></returns>
        List<string> GenDefFileContents()
        {
            // Create the supplementary file. Indicated by empty source file name.
            List<string> codeLines = GenTopOfFile("");

            // Various defines.
            WriteDefValues(ScriptDefinitions.TheDefinitions.InstrumentDefs, "General Midi Instruments");
            WriteDefValues(ScriptDefinitions.TheDefinitions.DrumDefs, "General Midi Drums");
            WriteDefValues(ScriptDefinitions.TheDefinitions.ControllerDefs, "Midi Controllers");

            // Some enums.
            WriteEnumValues<DeviceType>();
//            WriteEnumValues<DisplayType>();
            WriteEnumValues<SequenceMode>();

            // Bottom stuff.
            codeLines.AddRange(GenBottomOfFile());

            return codeLines;

            #region Some DRY helpers
            void WriteEnumValues<T>() where T : Enum
            {
                codeLines.Add($"        ///// {typeof(T).ToString()}");
                Enum.GetValues(typeof(T)).Cast<T>().ForEach(e =>
                {
                    int val = Convert.ToInt32(e);
                    codeLines.Add($"        const int {e.ToString()} = {val};");
                    _defs.Add(e.ToString(), val);
                });
            }

            void WriteDefValues(Dictionary<string, string> vals, string txt)
            {
                codeLines.Add($"        ///// {txt}");
                vals.Keys.ForEach(k =>
                {
                    int val = int.Parse(vals[k]);
                    codeLines.Add($"        const int {k} = {val};");
                    _defs.Add(k, val);
                });
            }
            #endregion
        }

        /// <summary>
        /// Create the boilerplate file top stuff.
        /// </summary>
        /// <param name="fn">Source file name. Empty means it's an internal file.</param>
        /// <returns></returns>
        List<string> GenTopOfFile(string fn)
        {
            // Create the common contents.
            List<string> codeLines = new List<string>
            {
                $"//{fn}",
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using System.Linq;",
                "using System.Drawing;",
                "using System.Windows.Forms;",
                "using NAudio;",
                "using NBagOfTricks;",
                "using Nebulator.Common;",
                "using Nebulator.Script;",
                "using Nebulator.Device;",
                "namespace Nebulator.UserScript",
                "{",
               $"    public partial class {_scriptName} : NebScript",
                "    {"
            };

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file bottom stuff.
        /// </summary>
        /// <returns></returns>
        List<string> GenBottomOfFile()
        {
            // Create the common contents.
            List<string> codeLines = new List<string>
            {
                "    }",
                "}"
            };

            return codeLines;
        }
        #endregion
    }

    /// <summary>General script error.</summary>
    public enum ScriptErrorType { None, Warning, Error, Runtime }

    /// <summary>General script error container.</summary>
    public class ScriptError
    {
        /// <summary>Where it came from.</summary>
//        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptErrorType ErrorType { get; set; } = ScriptErrorType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Content.</summary>
        public string Message { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ErrorType} {SourceFile}({LineNumber}): {Message}";
    }
}
