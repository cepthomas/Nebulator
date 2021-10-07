using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NLog;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator.App
{
    /// <summary>General script result - error/warn etc.</summary>
    public enum CompileResultType
    {
        None,       // Not an error - could be info.
        Warning,    // Compiler warning.
        Error,      // Compiler error.
        Fatal,      // Internal error.
        Runtime     // Runtime error - user script.
    }

    /// <summary>General script result container.</summary>
    public class CompileResult
    {
        /// <summary>Where it came from.</summary>
        public CompileResultType ResultType { get; set; } = CompileResultType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Original source line number. -1 means invalid.</summary>
        public int LineNumber { get; set; } = -1;

        /// <summary>Content.</summary>
        public string Message { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ResultType} {SourceFile}({LineNumber}): {Message}";
    }

    /// <summary>Parser helper class.</summary>
    class FileContext
    {
        /// <summary>Current source file.</summary>
        public string SourceFile { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Current source line.</summary>
        public int LineNumber { get; set; } = 1;

        /// <summary>Accumulated script code lines.</summary>
        public List<string> CodeLines { get; set; } = new List<string>();
    }

    /// <summary>Parses/compiles *.neb file(s).</summary>
    public class Compiler
    {
        #region Properties
        /// <summary>The compiled script.</summary>
        public ScriptBase Script { get; set; } = new();

        /// <summary>Current active channels.</summary>
        public List<Channel> Channels { get; set; } = new();

        /// <summary>Accumulated errors/results.</summary>
        public List<CompileResult> Results { get; } = new List<CompileResult>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }

        /// <summary>Compile products are here.</summary>
        public string TempDir { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Errors considering warnings-as-errors setting.</summary>
        public int ErrorCount
        {
            get
            {
                int errorCount = Results.Where(r => r.ResultType == CompileResultType.Error || r.ResultType == CompileResultType.Fatal).Count();
                if (!UserSettings.TheSettings.IgnoreWarnings)
                {
                    errorCount += Results.Where(r => r.ResultType == CompileResultType.Warning).Count();
                }
                return errorCount;
            }
        }
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("Compiler");

        /// <summary>Starting directory.</summary>
        string _baseDir = Definitions.UNKNOWN_STRING;

        /// <summary>Main source file name.</summary>
        readonly string _nebfn = Definitions.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Definitions.UNKNOWN_STRING;

        /// <summary>Accumulated lines to go in the constructor.</summary>
        readonly List<string> _initLines = new();

        /// <summary>Products of file process. Key is generated file name.</summary>
        readonly Dictionary<string, FileContext> _filesToCompile = new();

        /// <summary>All the definitions for internal use. TODO2 more elegant/fast way?</summary>
        readonly Dictionary<string, int> _defs = new();

        /// <summary>Code lines that define channels.</summary>
        readonly List<string> _channelDescriptors = new();
        #endregion

        #region Public functions
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to main file.</param>
        public void Execute(string nebfn)
        {
            // Reset everything.
            Script = new();
            Channels.Clear();
            Results.Clear();
            _filesToCompile.Clear();
            _initLines.Clear();

            if (nebfn != Definitions.UNKNOWN_STRING && File.Exists(nebfn))
            {
                _logger.Info($"Compiling {nebfn}.");

                ///// Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(nebfn);
                StringBuilder sb = new();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                var dir = Path.GetDirectoryName(nebfn);
                _baseDir = dir!;

                ///// Compile.
                DateTime startTime = DateTime.Now; // for metrics

                // Save hash of current channel descriptors to detect change in source code.
                int chdesc = string.Join("", _channelDescriptors).GetHashCode();
                _channelDescriptors.Clear();

                // Process the source files.
                Parse(nebfn);

                // Check for changed channels.
                if (string.Join("", _channelDescriptors).GetHashCode() != chdesc)
                {
                    Channels = ProcessChannelDescs();
                }

                // Compile the processed files.
                Script = CompileNative();

                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {nebfn}.");
            }

            Script.Valid = ErrorCount == 0;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Top level compiler.
        /// </summary>
        /// <returns>Compiled script</returns>
        ScriptBase CompileNative()
        {
            ScriptBase script = new();

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
                    Arguments = $"build {TempDir}"
                    //Arguments = $"build --no-restore {TempDir}"
                };

                Process process = new() { StartInfo = stinfo };
                process.Start();

                //TODO1 blocks??? process.WaitForExit();

                // Process output.
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                if(stderr != "")
                {
                    CompileResult se = new()
                    {
                        ResultType = CompileResultType.Fatal,
                        Message = $"Really bad thing happened:{stderr}"
                    };
                    Results.Add(se);
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
                                CompileResult se = new();

                                // TODO1 need better parser!

                                // Parse the line.
                                var parts0 = l.SplitByTokens(":");

                                if (parts0[2].StartsWith("error")) se.ResultType = CompileResultType.Error;
                                else if (parts0[2].StartsWith("warning")) se.ResultType = CompileResultType.Warning;

                                var parts1 = parts0[1].SplitByTokens("(),");

                                // genned file name.
                                var gennedFileName = $"{parts0[0]}:{parts1[0]}";

                                // genned file line number.
                                int gennedFileLine = int.Parse(parts1[1]);

                                var parts2 = parts0[3].SplitByTokens("[");

                                se.Message = parts2[0];

                                // Get the original info.
                                if (_filesToCompile.TryGetValue(Path.GetFileName(gennedFileName), out var cont))
                                {
                                    string origLine = cont.CodeLines[gennedFileLine - 1];
                                    se.SourceFile = cont.SourceFile;
                                    int ind = origLine.LastIndexOf("//");
                                    if (ind != -1)
                                    {
                                        if(int.TryParse(origLine[(ind + 2)..], out int origLineNum))
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
                                }

                                Results.Add(se);
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
                    if (ErrorCount == 0)
                    {
                        // All good so far.
                        Assembly assy = Assembly.LoadFrom(Path.Combine(TempDir, "net5.0-windows", "UserScript.dll")); //TODO1 need to unload/free script to release dll file.

                        // Bind to the script interface.
                        foreach (Type t in assy.GetTypes())
                        {
                            if (t is not null && t.BaseType is not null && t.BaseType.Name == "ScriptBase")
                            {
                                // We have a good script file. Create the executable object.
                                object? o = Activator.CreateInstance(t);
                                if(o is not null)
                                {
                                    script = (ScriptBase)o;
                                }
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
                Results.Add(new CompileResult()
                {
                    ResultType = CompileResultType.Fatal,
                    Message = "Exception: " + ex.Message,
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
            //// Add the generated internal code files.
            //_filesToCompile.Add($"{_scriptName}_defs.cs", new FileContext()
            //{
            //    SourceFile = "",
            //    CodeLines = GenDefFileContents()
            //});

            // Start parsing from the main file. ParseOneFile is a recursive function.
            FileContext pcont = new()
            {
                SourceFile = nebfn,
                LineNumber = 1
            };

            ParseOneFile(pcont);
        }

        /// <summary>
        /// Parse one file. Recursive to support nested include(fn).
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool ParseOneFile(FileContext pcont)
        {
            bool valid = File.Exists(pcont.SourceFile);

            if (valid)
            {
                string genFn = $"{_scriptName}_src{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                // Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                // The content.
                ProcessScriptFile(pcont);

                // Postamble.
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

                // Test for preprocessor directives.
                string strim = s.Trim();

                //Include("path\name.neb");
                if (strim.StartsWith("Include"))
                {
                    bool valid = false;

                    List<string> parts = strim.SplitByTokens("\"");
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
                        Results.Add(new CompileResult()
                        {
                            ResultType = CompileResultType.Error,
                            Message = $"Invalid Include: {strim}",
                            SourceFile = pcont.SourceFile,
                            LineNumber = pcont.LineNumber
                        });
                    }
                }
                else if (strim.StartsWith("Channel"))
                {
                    _channelDescriptors.Add(strim);
                }
                else // plain line
                {
                    if (cline.Trim() != "")
                    {
                        // Store the whole line with line number tacked on and some indentation.
                        pcont.CodeLines.Add($"        {cline} //{pcont.LineNumber}");
                    }
                }
            }
        }

        /// <summary>
        /// Convert channel descriptors into objects.
        /// </summary>
        /// <returns></returns>
        List<Channel> ProcessChannelDescs()
        {
            List<Channel> channels = new();

            // Build new channels.
            foreach (string sch in _channelDescriptors)
            {
                try
                {
                    // Channel("keys",  MidiOut, 1, AcousticGrandPiano,  0.1);
                    List<string> parts = sch.SplitByTokens("(),;");

                    Channel ch = new()
                    {
                        ChannelName = parts[1].Replace("\"", ""),
                        DeviceType = (DeviceType)Enum.Parse(typeof(DeviceType), parts[2]),
                        ChannelNumber = int.Parse(parts[3]),
                        Patch = (InstrumentDef)Enum.Parse(typeof(InstrumentDef), parts[4]),
                        VolumeWobbleRange = double.Parse(parts[5])
                    };

                    channels.Add(ch);
                }
                catch (Exception)
                {
                    Results.Add(new()
                    {
                        ResultType = CompileResultType.Error,
                        Message = $"Bad statement:{sch}",
                        SourceFile = _nebfn,
                        //LineNumber = -1 // TODO2
                    });
                }
            }

            return channels;
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
                "using static Nebulator.Script.ScriptUtils;",
                "using static Nebulator.Common.InstrumentDef;",
                "using static Nebulator.Common.DrumDef;",
                "using static Nebulator.Common.ControllerDef;",
                "using static Nebulator.Common.SequenceMode;",
                "",
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
}
