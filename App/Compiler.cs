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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Nebulator.App
{
    /// <summary>General script result - error/warn etc.</summary>
    public enum CompileResultType
    {
        Info,       // Not an error.
        Warning,    // Compiler warning.
        Error,      // Compiler error.
        Fatal,      // Internal error.
        Runtime     // Runtime error - user script.
    }

    /// <summary>General script result container.</summary>
    public class CompileResult
    {
        /// <summary>Where it came from.</summary>
        public CompileResultType ResultType { get; set; } = CompileResultType.Info;

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
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("Compiler");

        /// <summary>Main source file name.</summary>
        readonly string _nebfn = Definitions.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Definitions.UNKNOWN_STRING;

        /// <summary>Accumulated lines to go in the constructor.</summary>
        readonly List<string> _initLines = new();

        /// <summary>Products of file preprocess. Key is generated file name.</summary>
        readonly Dictionary<string, FileContext> _filesToCompile = new();

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

                ///// Compile.
                DateTime startTime = DateTime.Now; // for metrics

                // Save hash of current channel descriptors to detect change in source code.
                int chdesc = string.Join("", _channelDescriptors).GetHashCode();
                _channelDescriptors.Clear();

                ///// Process the source files into something that can be compiled. ParseOneFile is a recursive function.
                FileContext pcont = new()
                {
                    SourceFile = nebfn,
                    LineNumber = 1
                };
                PreprocessFile(pcont);

                ///// Check for changed channel descriptors.
                if (string.Join("", _channelDescriptors).GetHashCode() != chdesc)
                {
                    Channels = ProcessChannelDescs();
                }

                ///// Compile the processed files.
                Script = CompileNative(dir!);

                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {nebfn}.");
            }

            int errorCount = Results.Where(r => r.ResultType == CompileResultType.Error || r.ResultType == CompileResultType.Fatal).Count();
            Script.Valid = errorCount == 0;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// The actual compiler driver.
        /// </summary>
        /// <returns>Compiled script</returns>
        ScriptBase CompileNative(string baseDir)
        {
            ScriptBase script = new();

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                ///// Assemble constituents.
                List<SyntaxTree> trees = new();

                // Write the generated source files to temp build area.
                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, ci.CodeLines);

                    // Build a syntax tree.
                    string code = File.ReadAllText(fullpath);
                    CSharpParseOptions popts = new();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, genFn);
                    trees.Add(tree);
                }

                //3. We now build up a list of references needed to compile the code.
                var references = new List<MetadataReference>();
                // System stuff location.
                var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);
                // Project refs like nuget.
                var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // System dlls.
                foreach (var dll in new[] { "System", "System.Core", "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Drawing", "System.Linq" })
                {
                    references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll")));
                }

                // Local dlls.
                foreach (var dll in new[] { "NAudio", "NLog", "NBagOfTricks", "NebOsc", "Nebulator.Common", "Nebulator.Script" })
                {
                    references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll")));
                }

                ///// Emit to stream
                var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                //TODO2 other opts?
                //  <WarningLevel>4</WarningLevel>
                //  <!-- <NoWarn>CS1591;CA1822;CS0414</NoWarn> -->  CS8019?
                //  <WarningsAsErrors>NU1605</WarningsAsErrors>

                var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);
                var ms = new MemoryStream();
                EmitResult result = compilation.Emit(ms);
                if (result.Success)
                {
                    //Load into currently running assembly. Normally we'd probably want to do this in an AppDomain
                    var assy = Assembly.Load(ms.ToArray());
                    foreach (Type t in assy.GetTypes())
                    {
                        if (t.BaseType != null && t.BaseType.Name == "ScriptBase")
                        {
                            // We have a good script file. Create the executable object.
                            object? o = Activator.CreateInstance(t);
                            if(o is not null)
                            {
                                script = (ScriptBase)o;
                            }
                        }
                    }
                }

                ///// Results.
                // List<string> diags = new();
                // result.Diagnostics.ForEach(d => diags.Add(d.ToString()));
                // File.WriteAllLines(@"C:\Dev\repos\Nebulator\Examples\temp\compiler.txt", diags);
                foreach (var diag in result.Diagnostics)
                {
                    CompileResult se = new();
                    se.Message = diag.GetMessage();
                    bool keep = true;

                    switch (diag.Severity)
                    {
                        case DiagnosticSeverity.Error:
                            se.ResultType = CompileResultType.Error;
                            break;

                        case DiagnosticSeverity.Warning:
                            if (UserSettings.TheSettings.IgnoreWarnings)
                            {
                                keep = false;
                            }
                            else
                            {
                                se.ResultType = CompileResultType.Warning;
                            }
                            break;

                        case DiagnosticSeverity.Info:
                            se.ResultType = CompileResultType.Info;
                            break;

                        case DiagnosticSeverity.Hidden:
                            if (UserSettings.TheSettings.IgnoreWarnings)
                            {
                                keep = false;
                            }
                            else
                            {
                                //?? se.ResultType = CompileResultType.Warning;
                            }
                            break;
                    }

                    var genFileName = diag.Location.SourceTree!.FilePath;
                    var genLineNum = diag.Location.GetLineSpan().StartLinePosition.Line; // 0-based

                    // Get the original info.
                    if (_filesToCompile.TryGetValue(Path.GetFileName(genFileName), out var context))
                    {
                        se.SourceFile = context.SourceFile;
                        // Dig out the original line number.
                        string origLine = context.CodeLines[genLineNum];
                        int ind = origLine.LastIndexOf("//");
                        if (ind != -1)
                        {
                            se.LineNumber = int.TryParse(origLine[(ind + 2)..], out int origLineNum) ? origLineNum : -1; // 1-based
                        }
                    }
                    else
                    {
                        // Presumably internal generated file - should never have errors.
                        se.SourceFile = "NoSourceFile";
                    }

                    if(keep)
                    {
                        Results.Add(se);
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
        /// Parse one file. Recursive to support nested include(fn).
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool PreprocessFile(FileContext pcont)
        {
            bool valid = File.Exists(pcont.SourceFile);

            if (valid)
            {
                string genFn = $"{_scriptName}_src{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                ///// Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                ///// The content.
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
                        // Exclude from output file.
                        List<string> parts = strim.SplitByTokens("\"");
                        if (parts.Count == 3)
                        {
                            string fn = Path.Combine(UserSettings.TheSettings.WorkPath, parts[1]);

                            // Recursive call to parse this file
                            FileContext subcont = new()
                            {
                                SourceFile = fn,
                                LineNumber = 1
                            };
                            valid = PreprocessFile(subcont);
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
                        // Exclude from output file.
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

                ///// Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }

        /// <summary>
        /// Convert channel descriptors into partial Channel objects.
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
                        LineNumber = -1
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
            string origin = fn == "" ? "internal" : fn;

            // Create the common contents.
            List<string> codeLines = new()
            {
                $"// Created from:{origin}",
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using System.Linq;",
                "using System.Drawing;",
                "using NAudio;",
                "using NLog;",
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
