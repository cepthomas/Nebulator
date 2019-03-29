using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nebulator.Common;


namespace Nebulator.Script
{
    /// <summary>
    /// Parses/compiles *.neb file(s).
    /// </summary>
    public class NebCompiler
    {
        /// <summary>
        /// Parser helper class.
        /// </summary>
        class FileContext
        {
            /// <summary>Current source file.</summary>
            public string SourceFile { get; set; } = Utils.UNKNOWN_STRING;

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
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Starting directory.</summary>
        string _baseDir = Utils.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Utils.UNKNOWN_STRING;

        /// <summary>Accumulated lines to go in the constructor.</summary>
        List<string> _initLines = new List<string>();

        /// <summary>Products of file process. Key is generated file name.</summary>
        Dictionary<string, FileContext> _filesToCompile = new Dictionary<string, FileContext>();
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

            if (nebfn != Utils.UNKNOWN_STRING && File.Exists(nebfn))
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
        /// Top level parser.
        /// </summary>
        /// <param name="nebfn">Topmost file in collection.</param>
        void Parse(string nebfn)
        {
            // Start parsing from the main file. ParseOneFile is a recursive function.
            FileContext pcont = new FileContext()
            {
                SourceFile = nebfn,
                LineNumber = 1
            };

            ParseOneFile(pcont);

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
        }

        /// <summary>
        /// Parse one file. This is recursive to support nested #import.
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

                // Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

                if (pcont.SourceFile.EndsWith(".nebc")) // .nebc composition file
                {
                    string parseState = "IDLE";
                    string currentSeq = "";
                    int numInst = 0;

                    for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                    {
                        string sline = sourceLines[pcont.LineNumber - 1].Trim();

                        // Remove any comments.
                        int pos = sline.IndexOf("//");
                        string cline = pos >= 0 ? sline.Left(pos) : sline;
                        List<string> inParts = cline.SplitByToken(" ");


                        try
                        {
                            if (inParts.Count > 0)
                            {

                                // DEF KEYS_DEF_VOL 0.8
                                // SEQ PIANO_MAIN
                                // 00.00   G3     0.7   0.60
                                // 
                                // SEQ DRUMS_SIMPLE
                                // x-------x-------x-------x-------   AcousticBassDrum   BASS_DRUM_VOL
                                // 
                                // COMP    DRUMS           PIANO          BASS
                                // 15.00   SKIP            FuncPiano      BASS_VERSE
                                // 16.00   DRUMS_VERSE     PIANO_MAIN     BASS_VERSE
                                // 32.00


                                string sout = "";


                                switch (inParts[0])
                                {
                                    case "DEF":
                                        if (parseState == "IDLE")
                                        {
                                            sout = $"const double {inParts[1]} = {inParts[2]};";
                                            parseState = "IDLE";
                                        }
                                        else
                                        {
                                            throw new Exception();
                                        }
                                        break;

                                    case "SEQ":
                                        if(parseState == "IDLE")
                                        {
                                            currentSeq = inParts[1];
                                            sout = $"NSequence {inParts[1]};";
                                            parseState = "SEQ";
                                        }
                                        else
                                        {
                                            throw new Exception();
                                        }
                                        break;

                                    case "COMP":
                                        if (parseState == "IDLE")
                                        {
                                            if(numInst > 0)
                                            {
                                                // Can't have more than one composition.
                                                throw new Exception();
                                            }

                                            sout = $"NComposition comp = new NComposition();";

                                            numInst = inParts.Count - 1;
                                            for (int i = 1; i < numInst; i++)
                                            {
                                                sout += $"comp.Instruments.Add({inParts[i]});  ";
                                            }

                                            parseState = "COMP";
                                        }
                                        else
                                        {
                                            throw new Exception();
                                        }
                                        break;

                                    case "":
                                        parseState = "IDLE";
                                        break;

                                    default:
                                        if (parseState == "SEQ")
                                        {
                                            //     PIANO_MAIN.Add("00.00", "G3", "0.7", "0.60");
                                            //     DRUMS_SIMPLE.Add("x-------x-------x-------x-------", "AcousticBassDrum", "BASS_DRUM_VOL");

                                            sout = $"NSequence PIANO_MAIN;";
                                        }
                                        else if (parseState == "COMP")
                                        {
                                            sout = $"comp.Add({inParts.GetRange(1, inParts.Count - 1)});";
                                        }
                                        else
                                        {
                                            throw new Exception();
                                        }
                                        break;
                                }



                                // const double KEYS_DEF_VOL = 0.8;
                                // const double BASS_DRUM_VOL = 0.8;

                                // NSequence PIANO_MAIN;
                                // NSequence DRUMS_SIMPLE;

                                // public NComposition Init()
                                // {
                                //     NComposition comp = new NComposition();

                                //     PIANO_MAIN = createSequence();
                                //     PIANO_MAIN.Add("00.00", "G3", "0.7", "0.60");
                                //     PIANO_MAIN.Add("01.00", "D3", "0.7", "0.60");
                                //     PIANO_MAIN.Add("02.00", "A3.m7", "0.7", "0.60");
                                //     PIANO_MAIN.Add("03.00", "66", "0.7", "0.60");
                                //     PIANO_MAIN.Add("03.48", "F#", "0.7", "0.60");


                                //     DRUMS_SIMPLE = createSequence();
                                //     DRUMS_SIMPLE.Add("x-------x-------x-------x-------", "AcousticBassDrum", "BASS_DRUM_VOL");
                                //     DRUMS_SIMPLE.Add("----x-------x-x-----x-------x-x-", "AcousticSnare", "0.6");
                                //     DRUMS_SIMPLE.Add("------xx------xx------xx------xx", "ClosedHiHat", "0.8");


                                //     comp.Instruments.Add("DRUMS");
                                //     comp.Instruments.Add("PIANO");
                                //     comp.Instruments.Add("BASS");


                                //     comp.Add("00.00", "DRUMS_SIMPLE", "PIANO_MAIN", "BASS_VERSE");
                                //     // etc...
                                //     comp.Add("15.00", "SKIP", "FuncPiano", "MUTE");
                                //     // etc...
                                //     comp.Add("32.00");

                                //     return comp;
                                // }


                                if (sout != "")
                                {
                                    // Store the whole line with line number tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                                    pcont.CodeLines.Add($"{cline} //{pcont.LineNumber}");
                                }
                            }
                            else
                            {
                                // Nothing to do.
                                parseState = "IDLE";
                            }
                        }
                        catch (Exception ex)
                        {
                            Errors.Add(new ScriptError()
                            {
                                ErrorType = ScriptErrorType.Error,
                                Message = $"Script error in {sline}",
                                SourceFile = pcont.SourceFile,
                                LineNumber = pcont.LineNumber
                            });
                        }
                    }
                }
                else // regular .neb script code file
                {
                    for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                    {
                        string s = sourceLines[pcont.LineNumber - 1].Trim();

                        // Remove any comments.
                        int pos = s.IndexOf("//");
                        string cline = pos >= 0 ? s.Left(pos) : s;

                        // Test for nested files
                        // #import "path\name.neb"
                        // #import "include path\split file name.neb"
                        if (s.StartsWith("#import"))
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
                                    Message = $"Invalid #import: {fn}",
                                    SourceFile = pcont.SourceFile,
                                    LineNumber = pcont.LineNumber
                                });
                            }
                        }
                        else
                        {
                            if (cline != "")
                            {
                                // Store the whole line with line number tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                                pcont.CodeLines.Add($"{cline} //{pcont.LineNumber}");
                            }
                        }
                    }
                }

                // Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }

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
                cp.ReferencedAssemblies.Add("Nebulator.Common.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Script.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Device.dll");
                cp.ReferencedAssemblies.Add("NAudio.dll");

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
                    File.WriteAllLines(fullpath, Utils.FormatSourceCode(ci.CodeLines));
                    paths.Add(fullpath);
                }

                // Make it compile. Maybe try Roslyn again? For c#6+.
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

                    if (script == null)
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
                        string origFileName = Utils.UNKNOWN_STRING;

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


        NComposition ParseNewTODO(FileContext pcont)
        {
            NComposition comp = new NComposition();


            List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

            // Preamble.
            pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

            for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
            {
                string s = sourceLines[pcont.LineNumber - 1].Trim();

                // Remove any comments.
                int pos = s.IndexOf("//");
                string cline = pos >= 0 ? s.Left(pos) : s;

                //// Test for nested files
                //// #import "path\name.neb"
                //// #import "include path\split file name.neb"
                //if (s.StartsWith("#import"))
                //{
                //    List<string> parts = s.SplitByTokens("\";");
                //    string fn = parts.Last();

                //    // Recursive call to parse this file
                //    FileContext subcont = new FileContext()
                //    {
                //        SourceFile = fn,
                //        LineNumber = 1
                //    };

                //    if (!ParseOneFile(subcont))
                //    {
                //        Errors.Add(new ScriptError()
                //        {
                //            ErrorType = ScriptErrorType.Error,
                //            Message = $"Invalid #import: {fn}",
                //            SourceFile = pcont.SourceFile,
                //            LineNumber = pcont.LineNumber
                //        });
                //    }
                //}
                //else
                {
                    List<string> parts = cline.SplitByToken(" ");

                    if(parts.Count > 0)
                    {




                    }




                    if (cline != "")
                    {
                        // Store the whole line with line number tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                        pcont.CodeLines.Add($"{cline} //{pcont.LineNumber}");
                    }
                }
            }






            return comp;
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
            // Reference to current script so nested classes have access to it. Processing uses java which would not require this minor hack.
            codeLines.Add("protected static NebScript s;");
            codeLines.Add($"public {_scriptName}() : base()");
            codeLines.Add("{");
            codeLines.Add("s = this;");
            _initLines.ForEach(l => codeLines.Add(l));
            codeLines.Add("}");

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

            // The various defines.
            codeLines.Add("///// General Midi Instruments");
            ScriptDefinitions.TheDefinitions.InstrumentDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {ScriptDefinitions.TheDefinitions.InstrumentDefs[k]};"));
            codeLines.Add("///// General Midi Drums");
            ScriptDefinitions.TheDefinitions.DrumDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {ScriptDefinitions.TheDefinitions.DrumDefs[k]};"));
            codeLines.Add("///// Midi Controllers");
            ScriptDefinitions.TheDefinitions.ControllerDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {ScriptDefinitions.TheDefinitions.ControllerDefs[k]};"));
            codeLines.Add("///// Device Types");
            Enum.GetValues(typeof(Device.DeviceType)).Cast<Device.DeviceType>().ForEach(e => codeLines.Add($"const int {e.ToString()} = {(int)e};"));
            codeLines.Add("///// Meter Types");
            Enum.GetValues(typeof(DisplayType)).Cast<DisplayType>().ForEach(e => codeLines.Add($"const int {e.ToString()} = {(int)e};"));

            // Bottom stuff.
            codeLines.AddRange(GenBottomOfFile());

            return codeLines;
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
                "using Nebulator.Common;",
                "using Nebulator.Script;",
                "using Nebulator.Device;",
                "using NAudio;",
                "namespace Nebulator.UserScript",
                "{",
                $"public partial class {_scriptName} : NebScript",
                "{"
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
                "}",
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
        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptErrorType ErrorType { get; set; } = ScriptErrorType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Content.</summary>
        public string Message { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ErrorType} {SourceFile}({LineNumber}): {Message}";
    }
}
