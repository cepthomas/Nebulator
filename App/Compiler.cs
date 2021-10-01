using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator.App
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

        /// <summary>Do not include some neb only components.</summary>
        public string TempDir { get; set; } = Definitions.UNKNOWN_STRING;
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("Compiler");

        /// <summary>Starting directory.</summary>
        string _baseDir = Definitions.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Definitions.UNKNOWN_STRING;

        /// <summary>Accumulated lines to go in the constructor.</summary>
        readonly List<string> _initLines = new();

        /// <summary>Products of file process. Key is generated file name.</summary>
        readonly Dictionary<string, FileContext> _filesToCompile = new();

        /// <summary>All the definitions for internal use. TODO2 more elegant/fast way?</summary>
        readonly Dictionary<string, int> _defs = new();

        //const string GENNED_FILE = ""
        #endregion

        #region Public functions
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to main file.</param>
        /// <returns>The newly minted script object or null if failed.</returns>
        public ScriptBase Execute(string nebfn)
        {
            ScriptBase script = null;

            // Reset everything.
            _filesToCompile.Clear();
            _initLines.Clear();

            Errors.Clear();

            if (nebfn != Definitions.UNKNOWN_STRING && File.Exists(nebfn))
            {
                _logger.Info($"Compiling {nebfn}.");

                ///// Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(nebfn);
                StringBuilder sb = new();
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
        ScriptBase Compile()
        {
            ScriptBase script = null;

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(_baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                //Directory.GetFiles(TempDir).Where(f => f.EndsWith(".cs")).ForEach(f => File.Delete(f));
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                // Get project file template. Fix the assembly location.
                string fpath = Path.Combine(MiscUtils.GetExeDir(), @"Resources\UserScriptTemplate.txt");
                string inputDllPath = Environment.CurrentDirectory;
                string stempl = File.ReadAllText(fpath);
                stempl = stempl.Replace("%DLL_PATH%", inputDllPath);

                // Write project file to temp.
                string projFn = Path.Combine(TempDir, "UserScript.csproj");
                File.WriteAllText(projFn, stempl);

                // Write the generated source files.
                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, ci.CodeLines);
                }

                // Make it compile.
                ProcessStartInfo stinfo = new()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    Arguments = $"build --no-restore {TempDir}"
                };

                Process process = new() { StartInfo = stinfo };
                process.Start();
                
 //???               process.WaitForExit();

                // Process output.
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                if(stderr != "")
                {
                    ScriptError se = new()
                    {
                        ErrorType = ScriptErrorType.Error,
                        LineNumber = -1,
                        SourceFile = "",
                        Message = $"Really bad thing happened:{stderr}"
                    };
                    Errors.Add(se);
                }
                else
                {
                    List<string> outlines = stdout.SplitByToken("\r\n");

                    // Because there are dupes, wait for the go-ahead.
                    bool collect = false;

                    foreach(var l in outlines)
                    {
                        if(collect)
                        {
                            if (l.Contains(": error ") || l.Contains(": warning "))
                            {
                                ScriptError se = new();

                                // Parse the line.
                                var parts0 = l.SplitByTokens(":");

                                if (parts0[2].StartsWith("error")) se.ErrorType = ScriptErrorType.Error;
                                if (parts0[2].StartsWith("warning")) se.ErrorType = ScriptErrorType.Warning;

                                var parts1 = parts0[1].SplitByTokens("(),");

                                // genned file name.
                                var gennedFileName = $"{parts0[0]}:{parts1[0]}";

                                // genned file line number.
                                int gennedFileLine = int.Parse(parts1[1]);

                                var parts2 = parts0[3].SplitByTokens("[");

                                se.Message = parts2[0];

                                // Get the original info.
                                if (_filesToCompile.TryGetValue(Path.GetFileName(gennedFileName), out FileContext cont))
                                {
                                    string origLine = cont.CodeLines[gennedFileLine - 1];
                                    se.SourceFile = cont.SourceFile;
                                    int ind = origLine.LastIndexOf("//");
                                    if (ind != -1)
                                    {
                                        if(int.TryParse(origLine.Substring(ind + 2), out int origLineNum))
                                        {
                                            se.LineNumber = origLineNum;
                                        }
                                        else
                                        {
                                            se.LineNumber = -1;
                                        }
                                    }
                                }
                                else
                                {
                                    // Presumably internal generated file - should never have errors.
                                    se.SourceFile = "NoSourceFile";
                                    se.LineNumber = -1;
                                }

                                Errors.Add(se);
                            }
                            else if (l.StartsWith("Time Elapsed"))
                            {
                                //TODO2 Do something with this?
                            }
                            else
                            {
                                // ????
                            }
                        }
                        else
                        {
                            collect = l.StartsWith("Build ");
                        }
                    }

                    // Process the output.
                    if (Errors.Count == 0)
                    {
                        // All good so far.
                        Assembly assy = Assembly.LoadFrom(Path.Combine(TempDir, "net5.0-windows", "UserScript.dll"));

                        // Bind to the script interface.
                        foreach (Type t in assy.GetTypes())
                        {
                            if (t.BaseType != null && t.BaseType.Name == "ScriptBase")
                            {
                                // We have a good script file. Create the executable object.
                                object o = Activator.CreateInstance(t);
                                script = o as ScriptBase;
                            }
                        }

                        if (script is null)
                        {
                            throw new Exception("Could not instantiate script");
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
            FileContext pcont = new()
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
            List<string> sourceLines = new(File.ReadAllLines(pcont.SourceFile));

            for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
            {
                string s = sourceLines[pcont.LineNumber - 1];

                // Remove any comments. Single line type only.
                int pos = s.IndexOf("//");
                string cline = pos >= 0 ? s.Left(pos) : s;

                // Test for nested files
                //Include("path\name.neb");
                //Include("path\split file name.neb");
                if (s.Trim().StartsWith("Include"))
                {
                    bool valid = false;

                    List<string> parts = s.SplitByTokens("\"");
                    if(parts.Count == 3)
                    {
                        string fn = Path.Combine(UserSettings.TheSettings.WorkPath, parts[1]);

                        // Recursive call to parse this file
                        FileContext subcont = new()
                        {
                            SourceFile = fn,
                            LineNumber = 1
                        };

                        valid = ParseOneFile(subcont);
                    }

                    if (!valid)
                    {
                        Errors.Add(new ScriptError()
                        {
                            ErrorType = ScriptErrorType.Error,
                            Message = $"Invalid Include: {s}",
                            SourceFile = pcont.SourceFile,
                            LineNumber = pcont.LineNumber
                        });
                    }
                }
                else if (s.Trim().StartsWith("Config"))
                {
                    // Remove these.
                }
                else // plain line
                {
                    if (cline != "")
                    {
                        // Store the whole line with line number tacked on.
                        pcont.CodeLines.Add($"{cline} //{pcont.LineNumber}");
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
            codeLines.Add($"        protected static ScriptBase s;");
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
            WriteEnumValues<SequenceMode>();

            // Bottom stuff.
            codeLines.AddRange(GenBottomOfFile());

            return codeLines;

            #region Some DRY helpers
            void WriteEnumValues<T>() where T : Enum
            {
                codeLines.Add($"        ///// {typeof(T)}");
                Enum.GetValues(typeof(T)).Cast<T>().ForEach(e =>
                {
                    int val = Convert.ToInt32(e);
                    codeLines.Add($"        const int {e} = {val};");
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
            List<string> codeLines = new()
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
                "namespace Nebulator.UserScript",
                "{",
               $"    public partial class {_scriptName} : ScriptBase",
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
            List<string> codeLines = new()
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
        //[JsonConverter(typeof(StringEnumConverter))]
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
