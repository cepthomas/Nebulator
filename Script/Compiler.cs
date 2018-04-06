using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using NLog;
using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.Dynamic;


namespace Nebulator.Script
{
    /// <summary>General script error container.</summary>
    public class ScriptError
    {
        public enum ScriptErrorType { None, Warning, Parse, Error, Runtime }

        /// <summary>Where it came from.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptErrorType ErrorType { get; set; } = ScriptErrorType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Message from parse or compile or runtime error.</summary>
        public string Message { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ErrorType} {SourceFile}({LineNumber}): {Message}";
    }

    /// <summary>
    /// Parses/compiles *.neb file(s).
    /// </summary>
    public class NebCompiler
    {
        #region Helper classes
        class FileParseContext
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
        #endregion

        #region Properties
        /// <summary>Accumulated errors.</summary>
        public List<ScriptError> Errors { get; } = new List<ScriptError>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }

        /// <summary>Specifies the temp dir for tracking down runtime errors.</summary>
        public string TempDir { get; set; } = "";
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Starting directory.</summary>
        string _baseDir = Utils.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Utils.UNKNOWN_STRING;

        /// <summary>Declared constants. Key is name.</summary>
        Dictionary<string, int> _consts = new Dictionary<string, int>();

        /// <summary>Accumulated lines to go in the constructor.</summary>
        List<string> _initLines = new List<string>();

        /// <summary>Parser state machine.</summary>
        StateMachine _sm = new StateMachine();

        /// <summary>Products of parsing process. Key is generated file name.</summary>
        Dictionary<string, FileParseContext> _filesToCompile = new Dictionary<string, FileParseContext>();

        /// <summary>The midi instrument definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiInstrumentDefs = new Dictionary<string, string>();

        /// <summary>The midi drum definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiDrumDefs = new Dictionary<string, string>();

        /// <summary>The midi controller definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiControllerDefs = new Dictionary<string, string>();
        #endregion
        
        #region Main function
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to topmost file.</param>
        /// <returns>The newly minted script object or null if failed.</returns>
        public ScriptCore Execute(string nebfn)
        {
            ScriptCore script = null;

            // Reset everything.
            _filesToCompile.Clear();
            _consts.Clear();
            _initLines.Clear();
            DynamicElements.Clear();
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

                ///// Parse.
                DateTime startTime = DateTime.Now; // metrics
                LoadDefinitions();
                InitStateMachine();
                Parse(nebfn);
                _logger.Info($"Parse took {(DateTime.Now - startTime).Milliseconds} msec.");

                ///// Compile.
                startTime = DateTime.Now;
                script = Compile();
                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {nebfn}.");
            }

            return script;
        }

        /// <summary>
        /// Load chord and midi definitions.
        /// </summary>
        void LoadDefinitions()
        {
            try
            {
                _midiInstrumentDefs.Clear();
                _midiDrumDefs.Clear();
                _midiControllerDefs.Clear();

                // Read the file.
                Dictionary<string, string> section = new Dictionary<string, string>();

                foreach (string sl in File.ReadAllLines(@"Resources\ScriptDefinitions.md"))
                {
                    List<string> parts = sl.SplitByToken("|");

                    if (parts.Count > 1)
                    {
                        switch (parts[0])
                        {
                            case "Instrument":
                                section = _midiInstrumentDefs;
                                break;

                            case "Drum":
                                section = _midiDrumDefs;
                                break;

                            case "Controller":
                                section = _midiControllerDefs;
                                break;

                            case string s when !s.StartsWith("---"):
                                section[parts[0]] = parts[1];
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Couldn't load the definitions file: " + ex.Message);
            }
        }
        #endregion

        #region Top level parser
        /// <summary>
        /// Top level parser.
        /// </summary>
        /// <param name="nebfn">Topmost file in collection.</param>
        void Parse(string nebfn)
        {
            // Start parsing from the main file. ParseOneFile is a recursive function.
            FileParseContext pcont = new FileParseContext()
            {
                SourceFile = nebfn,
                LineNumber = 1
            };
            ParseOneFile(pcont);

            // Finished. Patch up some forward refs. Probably should be a two pass compile.
            foreach (Section sect in DynamicElements.Sections.Values)
            {
                foreach (SectionTrack st in sect.SectionTracks)
                {
                    if (DynamicElements.Tracks[st.TrackName] == null)
                    {
                        pcont.LineNumber = 0; // Don't know the real line number.
                        AddParseError(pcont, $"Invalid track name: {st.TrackName}");
                    }

                    foreach (string sseq in st.SequenceNames)
                    {
                        if (DynamicElements.Sequences[sseq] == null)
                        {
                            pcont.LineNumber = 0; // Don't know the real line number.
                            AddParseError(pcont, $"Invalid sequence name: {sseq}");
                        }
                    }
                }
            }

            // Add the generated internal code files.
            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = GenMainFileContents()
            });

            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = GenDefFileContents()
            });
        }

        /// <summary>
        /// Parse one file. This is recursive to support nested include.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool ParseOneFile(FileParseContext pcont)
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
                string genFn = $"{_scriptName}_{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

                // Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                {
                    string s = sourceLines[pcont.LineNumber - 1].Trim();

                    // Remove any comments.
                    int pos = s.IndexOf("//");
                    string cline = pos >= 0 ? s.Left(pos) : s;

                    // Run it.
                    SmFuncArg farg = new SmFuncArg()
                    {
                        Context = pcont,
                        Args = cline.SplitByTokens(" ;"),
                        CleanedLine = cline
                    };

                    // What's the event?
                    string evt = farg.Args.Count == 0 ? "empty" : farg.Args[0];
                    _sm.ProcessEvent(evt, farg);
                }

                // Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }
        #endregion

        #region Parser state machine
        /// <summary>
        /// Generic payload for state machine functions.
        /// </summary>
        class SmFuncArg
        {
            public FileParseContext Context { get; set; } = null;
            public List<string> Args { get; set; } = null;
            public string CleanedLine { get; set; } = "";
        }

        /// <summary>
        /// Initialize the state machine.
        /// </summary>
        bool InitStateMachine()
        {
            State[] states = new State[]
            {
                new State("idle", null, null,
                    new Transition("include", "", ParseInclude),
                    new Transition("constant", "", ParseConstant),
                    new Transition("variable", "", ParseVariable),
                    new Transition("notes", "", ParseNotes),
                    new Transition("track", "", ParseTrack),
                    new Transition("lever", "", ParseLever),
                    new Transition("midictlin", "", ParseMidiController),
                    new Transition("midictlout", "", ParseMidiController),
                    new Transition("section", "do_section", ParseSection),
                    new Transition("sequence", "do_sequence", ParseSequence),
                    new Transition("empty"), // just swallow these
                    new Transition("", "", ParseScriptLine)), // everything else is assumed part of a script

                new State("do_section", null, null,
                    new Transition("empty", "idle"), // done section
                    new Transition("", "", ParseSectionTrack)), // element of section

                new State("do_sequence", null, null,
                    new Transition("empty", "idle"), // done sequence
                    new Transition("", "", ParseSequenceElement)), // element of sequence
            };

            bool valid = _sm.Init(states, "idle");
            return valid;
        }

        /// <summary>
        /// State machine parse error handler.
        /// </summary>
        /// <param name="o"></param>
        private void SmError(object o)
        {
            SmFuncArg farg = o as SmFuncArg;
            AddParseError(farg.Context, "State machine error");
        }
        #endregion

        #region Top level compiler
        /// <summary>
        /// Top level compiler.
        /// </summary>
        /// <returns></returns>
        ScriptCore Compile()
        {
            ScriptCore script = null;

            try
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
                cp.ReferencedAssemblies.Add("Nebulator.exe");
                cp.ReferencedAssemblies.Add("Nebulator.Common.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Midi.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Dynamic.dll");
                cp.ReferencedAssemblies.Add("Nebulator.Script.dll");

                // Add the generated source files.
                List<string> paths = new List<string>();

                // Create output area.
                TempDir = Path.Combine(_baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileParseContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, Utils.FormatSourceCode(ci.CodeLines));
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
                        if (t.BaseType != null && t.BaseType.Name == "ScriptCore")
                        {
                            // We have a good script file. Create the executable object.
                            Object o = Activator.CreateInstance(t);
                            script = o as ScriptCore;
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
                            FileParseContext ci = _filesToCompile[fpath];
                            origFileName = ci.SourceFile;
                            string origLine = ci.CodeLines[err.Line - 1];
                            int ind = origLine.LastIndexOf("//");

                            if (origFileName == "" || ind == -1)
                            {
                                // Must be an internal error. Do the best we can.
                                Errors.Add(new ScriptError()
                                {
                                    ErrorType = err.IsWarning ? ScriptError.ScriptErrorType.Warning : ScriptError.ScriptErrorType.Error,
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
                                    ErrorType = err.IsWarning ? ScriptError.ScriptErrorType.Warning : ScriptError.ScriptErrorType.Error,
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
                                ErrorType = err.IsWarning ? ScriptError.ScriptErrorType.Warning : ScriptError.ScriptErrorType.Error,
                                SourceFile = "None",
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
                    ErrorType = ScriptError.ScriptErrorType.Error,
                    Message = "Exception: " + ex.Message,
                    SourceFile = "",
                    LineNumber = 0
                });
            }

            return script;
        }

        /// <summary>
        /// Create the file containing all the nebulator glue.
        /// </summary>
        /// <returns></returns>
        List<string> GenMainFileContents()
        {
            // Create the main/generated file. Indicated by empty source file name.
            List<string> codeLines = GenTopOfFile("");

            // The constants.
            _consts.Keys.ForEach(v => codeLines.Add($"const int {v} = {_consts[v]};"));

            // The declared vars with the system hooks.
            DynamicElements.Vars.Values.ForEach(v => codeLines.Add($"int {v.Name} {{ get {{ return DynamicElements.Vars[\"{v.Name}\"].Value; }} set {{ DynamicElements.Vars[\"{v.Name}\"].Value = value; }} }}"));

            // Needed for runtime script statuses.
            DynamicElements.Tracks.Values.ForEach(t => codeLines.Add($"Track {t.Name} {{ get {{ return DynamicElements.Tracks[\"{t.Name}\"]; }} }}"));

            // Used for manual/trigger inputs.
            DynamicElements.Sequences.Values.ForEach(s => codeLines.Add($"Sequence {s.Name} {{ get {{ return DynamicElements.Sequences[\"{s.Name}\"]; }} }}"));

            // Collected init stuff goes in a constructor.
            // Reference to current script so nested classes have access to it. Processing uses java which would not require this minor hack.
            codeLines.Add("protected static ScriptCore s;");
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
            _midiInstrumentDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiInstrumentDefs[k]};"));
            _midiDrumDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiDrumDefs[k]};"));
            _midiControllerDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiControllerDefs[k]};"));

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
                //"using System.Drawing;",
                //"using System.Drawing.Drawing2D;",
                "using System.Windows.Forms;",
                "using Nebulator.Common;",
                "using Nebulator.Dynamic;",
                "using Nebulator.Script;",
                "namespace Nebulator.UserScript",
                "{",
                $"public partial class {_scriptName} : ScriptCore",
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

        #region Specific line parsers
        private void ParseInclude(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // include path\name.neb
            // 0       1
            // or:
            // include path\split file name.neb
            // 0       1          2    3

            try
            {
                // Handle spaces in path.
                string fn = string.Join(" ", farg.Args.GetRange(1, farg.Args.Count - 1));

                if (!SourceFiles.Contains(fn)) // Check for already done.
                {
                    // Recursive call to parse this file
                    FileParseContext subcont = new FileParseContext()
                    {
                        SourceFile = fn,
                        LineNumber = 1
                    };

                    if (!ParseOneFile(subcont))
                    {
                        throw new Exception(fn);
                    }
                }
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid include: {farg.Args[1]}");
            }
        }

        private void ParseConstant(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // constant DRUM_DEF_VOL 100
            // 0        1            2

            try
            {
                _consts[farg.Args[1]] = int.Parse(farg.Args[2]);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid const: {farg.Args[1]}");
            }
        }

        private void ParseVariable(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // variable PITCH 8192
            // 0        1     2

            try
            {
                Variable v = new Variable()
                {
                    Name = farg.Args[1],
                    Value = int.Parse(farg.Args[2])
                };
                DynamicElements.Vars.Add(v.Name, v);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid variable: {farg.Args[1]}");
            }
        }

        private void ParseNotes(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // notes MY_CHORD 1 4 6 b13
            // notes MY_SCALE 1 3 4 b7
            // 0     1        2 ....

            try
            {
                DynamicElements.NoteDefs.Add(farg.Args[1], farg.Args.GetRange(2, farg.Args.Count - 2));
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid notes: {farg.Args[1]}");
            }
        }

        private void ParseMidiController(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // midictlin  MI1 1 4     MODN
            // midictlout MO1 1 Pitch PITCH
            // 0          1   2 3     4

            try
            {
                int mctlr = 0;

                switch (farg.Args[3])
                {
                    case string s when s.IsInteger():
                        mctlr = int.Parse(farg.Args[3]);
                        break;

                    case "Pitch":
                        mctlr = MidiInterface.CTRL_PITCH;
                        break;

                    default:
                        mctlr = MidiInterface.TranslateController(farg.Args[3]);
                        break;
                }

                MidiControlPoint ctl = new MidiControlPoint()
                {
                    Channel = int.Parse(farg.Args[2]),
                    MidiController = mctlr,
                    RefVar = ParseVarRef(farg.Context, farg.Args[4])
                };

                switch (farg.Args[0])
                {
                    case "midictlin":
                        DynamicElements.InputMidis.Add(farg.Args[1], ctl);
                        break;

                    case "midictlout":
                        DynamicElements.OutputMidis.Add(farg.Args[1], ctl);
                        break;

                    default:
                        throw new Exception("");
                }
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid midi controller: {farg.Args[1]}");
            }
        }

        private void ParseLever(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // lever L3 -10 10 MODN
            // 0     1  2   3  4

            try
            {
                LeverControlPoint ctl = new LeverControlPoint()
                {
                    Min = int.Parse(farg.Args[2]),
                    Max = int.Parse(farg.Args[3]),
                    RefVar = ParseVarRef(farg.Context, farg.Args[4])
                };
                DynamicElements.Levers.Add(farg.Args[1], ctl);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid lever: {farg.Args[1]}");
            }
        }

        private void ParseTrack(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // track KEYS 1 5 0 0
            // 0     1    2 3 4 5

            try
            {
                Track nt = new Track()
                {
                    Name = farg.Args[1],
                    Channel = int.Parse(farg.Args[2]),
                    WobbleVolume = farg.Args.Count > 3 ? ParseConstRef(farg.Context, farg.Args[3]) : 0,
                    WobbleTimeBefore = farg.Args.Count > 4 ? -ParseConstRef(farg.Context, farg.Args[4]) : 0,
                    WobbleTimeAfter = farg.Args.Count > 5 ? ParseConstRef(farg.Context, farg.Args[5]) : 0
                };
                DynamicElements.Tracks.Add(nt.Name, nt);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid track: {farg.Args[1]}");
            }
        }

        private void ParseSection(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // section PART1 0 32
            // 0       1     2 3

            try
            {
                Section s = new Section()
                {
                    Name = farg.Args[1],
                    Start = ParseConstRef(farg.Context, farg.Args[2]),
                    Length = ParseConstRef(farg.Context, farg.Args[3])
                };
                DynamicElements.Sections.Add(s.Name, s);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid section: {farg.Args[1]}");
            }
        }

        private void ParseSectionTrack(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // KEYS   SEQ1           SEQ2       algoXXX()        SEQ2
            // DRUMS  DRUMS_SIMPLE   DRUMS_X
            // 0      1              2          3                4

            try
            {
                SectionTrack st = new SectionTrack()
                {
                    TrackName = farg.Args[0]
                };

                for (int i = 1; i < farg.Args.Count; i++)
                {
                    st.SequenceNames.Add(farg.Args[i]);
                }
                DynamicElements.Sections.Values.Last().SectionTracks.Add(st);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid section track: {farg.Args[0]}");
            }
        }

        private void ParseSequence(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // sequence DRUMS_SIMPLE 8
            // 0        1            2

            try
            {
                Sequence ns = new Sequence()
                {
                    Name = farg.Args[1],
                    Length = ParseConstRef(farg.Context, farg.Args[2]),
                };
                DynamicElements.Sequences.Add(ns.Name, ns);
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid sequence: {farg.Args[1]}");
            }
        }

        private void ParseSequenceElement(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            // WHEN WHICH VEL 1.50
            // 0    1     2   3
            // where:
            // 0 is one of 1.23, __x___x___x___x_, function()
            // 1 is one of 60, C.4, C.4.m7, RideCymbal1
            // 2 is one of 90, const (opt)
            // 3 is 1.23 (opt)

            // e.g.
            // 00.00  G.3  90  0.60
            // 01.00  58  90  0.60
            // ----x-------x-x-----x-------x-x- AcousticSnare 80
            // algoDynamic()  90

            try
            {
                // Support note string, number, drum.
                SequenceElement seqel = null;

                if (farg.Args[1].IsInteger())
                {
                    // Simple note number.
                    seqel = new SequenceElement(int.Parse(farg.Args[1]))
                    {
                        Volume = ParseConstRef(farg.Context, farg.Args[2])
                    };
                }
                else if (_midiDrumDefs.ContainsKey(farg.Args[1]))
                {
                    // It's a drum.
                    seqel = new SequenceElement(int.Parse(_midiDrumDefs[farg.Args[1]]))
                    {
                        Volume = ParseConstRef(farg.Context, farg.Args[2]),
                        Duration = new Time(1) // nominal duration
                    };
                }
                else
                {
                    // Note or function string form.
                    seqel = new SequenceElement(farg.Args[1])
                    {
                        Volume = ParseConstRef(farg.Context, farg.Args[2])
                    };

                    if (seqel.Function != "")
                    {
                        _initLines.Add($"_scriptFunctions.Add(\"{seqel.Function}\", {seqel.Function});");
                    }
                    else if(seqel.Notes.Count == 0)
                    {
                        AddParseError(farg.Context, $"Invalid note: {farg.Args[1]}");
                    }
                }

                // Optional duration for musical note.
                if (farg.Args.Count > 3)
                {
                    List<Time> t = ParseTime(farg.Context, farg.Args[3]);
                    seqel.Duration = t[0];
                }

                List<Time> whens = ParseTime(farg.Context, farg.Args[0]);
                foreach (Time t in whens)
                {
                    SequenceElement ncl = new SequenceElement(seqel) { When = t };
                    DynamicElements.Sequences.Values.Last().Elements.Add(ncl);
                }
            }
            catch (Exception)
            {
                AddParseError(farg.Context, $"Invalid note: {farg.Args[1]}");
            }
        }

        private void ParseScriptLine(object o)
        {
            SmFuncArg farg = o as SmFuncArg;

            try
            {
                if (farg.CleanedLine != "")
                {
                    // Store the whole line with line tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                    farg.Context.CodeLines.Add($"{farg.CleanedLine} //{farg.Context.LineNumber}");
                }

                // Test for event handler.
                foreach (string p in farg.Args)
                {
                    if (p.StartsWith("On_"))
                    {
                        // Get the variable name this handler is attached to.
                        List<string> fs = p.SplitByTokens("(");
                        string n = fs[0].Replace("On_", "");
                        _initLines.Add($"_scriptFunctions.Add(\"{n}\", {fs[0]});");
                    }
                }
            }
            catch (Exception)
            {
                AddParseError(farg.Context, "Invalid function line");
            }
        }
        #endregion

        #region Common parser helpers
        /// <summary>
        /// Add an error.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <param name="msg">The message.</param>
        void AddParseError(FileParseContext pcont, string msg)
        {
            Errors.Add(new ScriptError()
            {
                ErrorType = ScriptError.ScriptErrorType.Parse,
                Message = msg,
                SourceFile = pcont.SourceFile,
                LineNumber = pcont.LineNumber
            });
        }

        /// <summary>
        /// Parse a native file value for simple init or Var reference.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <param name="s">The line.</param>
        /// <returns>Referenced Var or new Var or null if invalid.</returns>
        Variable ParseVarRef(FileParseContext pcont, string s)
        {
            // Default is simple/fixed var.
            Variable v = new Variable() { Name = "" };
            try
            {
                // Test for simple value init.
                v.Value = int.Parse(s);
            }
            catch (Exception)
            {
                // Assume it is the name of a reference.
                v = DynamicElements.Vars[s];
                if (v is null)
                {
                    AddParseError(pcont, $"Invalid reference: {s}");
                }
            }
            return v;
        }

        /// <summary>
        /// Parse a native file value for simple init or const replacement.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <param name="s">The line.</param>
        /// <returns>Initializer from number or const.</returns>
        int ParseConstRef(FileParseContext pcont, string s)
        {
            // Default is simple/fixed var.
            int c = int.MinValue;
            try
            {
                // Test for simple value init.
                c = int.Parse(s);
            }
            catch (Exception)
            {
                // Assumes it is the name of a defined value.
                if (_consts.ContainsKey(s))
                {
                    c = _consts[s];
                }
                else
                {
                    AddParseError(pcont, $"Invalid reference: {s}");
                }
            }
            return c;
        }

        /// <summary>
        /// Parse a native file line.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <param name="s">The line.</param>
        /// <returns>A list of the parsed times - will be empty if failed.</returns>
        List<Time> ParseTime(FileParseContext pcont, string s)
        {
            List<Time> times = new List<Time>();

            try
            {
                // Test for pattern or Time.
                if (s.Contains("."))
                {
                    // Single time value.
                    var parts = s.SplitByToken(".");

                    // Check for valid fractional part.
                    if (int.TryParse(parts[1], out int result))
                    {
                        if (result >= Time.TOCKS_PER_TICK)
                        {
                            throw null; // too big
                        }

                        times.Add(new Time()
                        {
                            Tick = int.Parse(parts[0]),
                            Tock = int.Parse(parts[1])
                        });
                    }
                    else
                    {
                        // Try parsing fractions: 1/2, 3/4, 5/8 3/16 9/32 etc.
                        var frac = parts[1].SplitByToken("/");

                        if (frac.Count != 2)
                        {
                            throw (null); // incorrect number
                        }

                        double d = double.Parse(frac[0]) / double.Parse(frac[1]);

                        if (d >= 1.0)
                        {
                            throw (null); // invalid fraction
                        }

                        // Scale.
                        d *= Time.TOCKS_PER_TICK;

                        // Truncate.
                        d = Math.Floor(d);

                        times.Add(new Time()
                        {
                            Tick = int.Parse(parts[0]),
                            Tock = (int)d
                        });
                    }
                }
                else
                {
                    // Try pattern. Each hit is 1/16 note - fixed res for now.
                    // x---x---x---x---

                    const int PATTERN_SIZE = 4;

                    for (int i = 0; i < s.Length; i++)
                    {
                        switch (s[i])
                        {
                            case 'x':
                                // Note on.
                                times.Add(new Time(i / PATTERN_SIZE, (i % PATTERN_SIZE) * Time.TOCKS_PER_TICK / PATTERN_SIZE));
                                break;

                            case '-':
                                // No note, skip.
                                break;

                            default:
                                // Invalid
                                throw null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid time: {s}");
                times.Clear();
            }
            return times;
        }
        #endregion
    }
}
