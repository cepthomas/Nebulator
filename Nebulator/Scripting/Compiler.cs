using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using System.Text;
using NLog;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Scripting
{
    class FileParseContext
    {
        /// <summary>Current source file.</summary>
        public string SourceFile { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>Current source line.</summary>
        public int LineNumber { get; set; } = 1;

        /// <summary>Current parse state. One of idle, do_section, do_sequence, do_functions.</summary>
        public string State { get; set; } = "idle";

        /// <summary>Accumulated script code lines.</summary>
        public List<string> CodeLines { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parses/compiles neb file(s). TODO2 could use some speeding up.
    /// </summary>
    public class Compiler
    {
        #region Properties
        /// <summary>Accumulated errors.</summary>
        public List<ScriptError> Errors { get; } = new List<ScriptError>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Starting directory.</summary>
        string _baseDir = Globals.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Globals.UNKNOWN_STRING;

        /// <summary>Collected runtime variables, controls, etc.</summary>
        Dynamic _dynamic = new Dynamic();

        /// <summary>Declared constants. Key is name.</summary>
        Dictionary<string, int> _consts = new Dictionary<string, int>();

        /// <summary>Accumulated lines to go in the constructor.</summary>
        List<string> _initLines = new List<string>();

        /// <summary>Products of parsing process. Key is generated file name.</summary>
        Dictionary<string, FileParseContext> _filesToCompile = new Dictionary<string, FileParseContext>();

        /// <summary>The midi instrument definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiInstrumentDefs = new Dictionary<string, string>();

        /// <summary>The midi drum definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiDrumDefs = new Dictionary<string, string>();

        /// <summary>The midi controller definitions from ScriptDefinitions.md.</summary>
        Dictionary<string, string> _midiControllerDefs = new Dictionary<string, string>();
        #endregion



        /* state machine? TODO2
        StateMachine _sm = new StateMachine();

        /// <summary>
        /// Initialize the state machine.
        /// </summary>
        void InitStateMachine()
        {
            State[] states = new State[]
            {
                 new State("idle", null, null,
                     new Transition("include", ParseInclude),
                     new Transition("var", ParseVar),
                     new Transition("const", ParseConst),
                     new Transition("ctlin", ParseMidiInputController),
                     new Transition("ctlout", ParseMidiOutputController),
                     new Transition("ctlkbd", ParseKbdInputController),
                     new Transition("lever", ParseLever),
                     new Transition("patch", ParsePatch),
                     new Transition("", Error)), // invalid other events

                 new State("do_notes", null, null,
                     new Transition("indent", ParseNote),
                     new Transition("", Error)), // invalid other events

                 new State("do_loops", null, null,
                     new Transition("indent", ParseLoop),
                     new Transition("", Error)), // invalid other events

                 new State("do_functions", null, null,
                     new Transition("", ParseFunctionsLine)),
};

            //State[] states = new State[]
            //{
            //     // Any state gets this first
            //     new State("*", null, null,
            //         new Transition("seq", "do_notes", ParseSeq),
            //         new Transition("track", "do_loops", ParseTrack),
            //         new Transition("functions", "do_functions", ParseFunctions)),

            //     new State("idle", null, null,
            //         new Transition("composition", ParseComposition),
            //         new Transition("var", ParseVar),
            //         new Transition("const", ParseConst),
            //         new Transition("ctlin", ParseMidiInputController),
            //         new Transition("ctlout", ParseMidiOutputController),
            //         new Transition("ctlkbd", ParseKbdInputController),
            //         new Transition("lever", ParseLever),
            //         new Transition("patch", ParsePatch),
            //         new Transition("", Error)), // invalid other events

            //     new State("do_notes", null, null,
            //         new Transition("indent", ParseNote),
            //         new Transition("", Error)), // invalid other events

            //     new State("do_loops", null, null,
            //         new Transition("indent", ParseLoop),
            //         new Transition("", Error)), // invalid other events

            //     new State("do_functions", null, null,
            //         new Transition("", ParseFunctionsLine)),
            //};

            // Initialize the state machine.
            bool valid = _sm.Init(states, "idle");
        }
        */






        #region Main method
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to topmost file.</param>
        /// <returns>The newly minted script object or null if failed.</returns>
        public Script Execute(string nebfn)
        {
            Script script = null;

            if(nebfn != Globals.UNKNOWN_STRING && File.Exists(nebfn))
            {
                _logger.Info($"Compiling {nebfn}.");

                // Reset everything.
                _filesToCompile.Clear();
                _consts.Clear();
                _initLines.Clear();
                _dynamic = new Dynamic();
                Errors.Clear();

                // Init things.

                // Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(nebfn);
                StringBuilder sb = new StringBuilder();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();

                _baseDir = Path.GetDirectoryName(nebfn);
                LoadDefinitions();

                // Parse.
                DateTime startTime = DateTime.Now;
                Parse(nebfn);
                _logger.Info($"Parse files took {(DateTime.Now - startTime).Milliseconds} msec.");

                // Compile.
                startTime = DateTime.Now;
                script = Compile();
                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
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
            ///// Start processing from the main file. Recursive so will process any includes.
            FileParseContext pcont = new FileParseContext()
            {
                SourceFile = nebfn,
                LineNumber = 1
            };
            ParseOneFile(pcont);

            // Check some forward refs. TODO2 A bit clumsy - probably need a two pass compile.
            foreach(Section sect in _dynamic.Sections.Values)
            {
                foreach(SectionTrack st in sect.SectionTracks)
                {
                    if(_dynamic.Tracks[st.TrackName] == null)
                    {
                        pcont.LineNumber = 0; // Don't know the real line number.
                        AddParseError(pcont, $"Invalid track name:{st.TrackName}");
                    }

                    foreach(string sseq in st.SequenceNames)
                    {
                        if (_dynamic.Sequences[sseq] == null)
                        {
                            pcont.LineNumber = 0; // Don't know the real line number.
                            AddParseError(pcont, $"Invalid sequence name:{sseq}");
                        }
                    }
                }
            }

            ///// Add the generated internal code files.
            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = GenMainFileContents()
            });

            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = GenSuppFileContents()
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
                pcont.CodeLines.AddRange(GenCommonFileContents(pcont.SourceFile));

                for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                {
                    string s = sourceLines[pcont.LineNumber - 1].Trim();

                    // Remove any comments.
                    int pos = s.IndexOf("//");
                    string line = pos >= 0 ? s.Left(pos) : s;

                    List<string> allparts = line.SplitByTokens(" ;");

                    switch (pcont.State)
                    {
                        case "idle":
                            if (allparts.Count > 0)
                            {
                                switch (allparts[0])
                                {
                                    case "include":
                                        ParseInclude(pcont, allparts);
                                        break;

                                    case "constant":
                                        ParseConstant(pcont, allparts);
                                        break;

                                    case "variable":
                                        ParseVariable(pcont, allparts);
                                        break;

                                    case "track":
                                        ParseTrack(pcont, allparts);
                                        break;

                                    case "lever":
                                        ParseLever(pcont, allparts);
                                        break;

                                    case "midictlin":
                                        ParseMidiController(pcont, allparts);
                                        break;

                                    case "midictlout":
                                        ParseMidiController(pcont, allparts);
                                        break;

                                    case "section":
                                        ParseSection(pcont, allparts);
                                        pcont.State = "do_section";
                                        break;

                                    case "sequence":
                                        ParseSequence(pcont, allparts);
                                        pcont.State = "do_sequence";
                                        break;

                                    case "functions":
                                        pcont.State = "do_functions";
                                        break;
                                }
                            }
                            break;

                        case "do_section":
                            if (allparts.Count == 0)
                            {
                                // Empty line. Resets any current collections.
                                pcont.State = "idle";
                            }
                            else
                            {
                                ParseSectionTrack(pcont, allparts);
                            }
                            break;

                        case "do_sequence":
                            if (allparts.Count == 0)
                            {
                                // Empty line. Resets any current collections.
                                pcont.State = "idle";
                            }
                            else
                            {
                                ParseSequenceElement(pcont, allparts);
                            }
                            break;

                        case "do_functions":
                            if (allparts.Count == 0)
                            {
                                // Empty line. Skip it.
                            }
                            else
                            {
                                // Assume anything else is script.
                                ParseScriptLine(pcont, allparts, line);
                            }
                            break;
                    }
                }

                // Postamble.
                pcont.CodeLines.AddRange(new List<string>
                {
                    "}",
                    "}"
                });
            }

            return valid;
        }
        #endregion

        #region Top level compiler
        /// <summary>
        /// Top level compiler.
        /// </summary>
        /// <returns></returns>
        Script Compile()
        {
            Script script = null;

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

                // Add the generated source files.
                List<string> paths = new List<string>();

                // Create output area.
                string tempdir = Path.Combine(_baseDir, "temp");
                if (Directory.Exists(tempdir))
                {
                    Directory.GetFiles(tempdir).ForEach(f => File.Delete(f));
                }
                else
                {
                    Directory.CreateDirectory(tempdir);
                }

                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileParseContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(tempdir, genFn);
                    if (File.Exists(fullpath))
                    {
                        File.Delete(fullpath);
                    }
                    File.WriteAllLines(fullpath, ci.CodeLines.FormatSourceCode());
                    paths.Add(fullpath);
                }

                // Make it compile.
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, paths.ToArray());

                // TODO2 Would actually like to use roslyn for C#7 stuff but can't make it work:
                // Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider provider = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();
                // // Need to fix hardcoded path to compiler - why isn't this fixed by MS?
                // var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                // var settings = provider.GetType().GetField("_compilerSettings", flags).GetValue(provider);
                // settings.GetType().GetField("_compilerFullPath", flags).SetValue(settings, Environment.CurrentDirectory + @"\roslyn\csc.exe");

                if (cr.Errors.Count == 0)
                {
                    Assembly assy = cr.CompiledAssembly;

                    // Bind to the script interface.
                    foreach (Type t in assy.GetTypes())
                    {
                        if (t.BaseType != null && t.BaseType.Name == "Script")
                        {
                            // We have a good script file. Create the executable object.
                            Object o = Activator.CreateInstance(t);
                            script = o as Script;
                        }
                    }

                    if (script != null)
                    {
                        // Hand over the collected goods.
                        script.Dynamic = _dynamic;
                    }
                    else
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
                        string origFileName = Globals.UNKNOWN_STRING;

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
                                    ErrorType = ScriptErrorType.Compile,
                                    SourceFile = err.FileName,
                                    LineNumber = err.Line,
                                    Message = $"Internal Error: {err.ErrorText} in: {origLine}"
                                });
                            }
                            else
                            {
                                int.TryParse(origLine.Substring(ind + 2), out origLineNum);
                                Errors.Add(new ScriptError()
                                {
                                    ErrorType = ScriptErrorType.Compile,
                                    SourceFile = origFileName,
                                    LineNumber = origLineNum,
                                    Message = err.ErrorText
                                });
                            }
                        }
                        else
                        {
                            // Should never happen...
                            _logger.Error("This should never happen...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new ScriptError()
                {
                    ErrorType = ScriptErrorType.Compile,
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
            List<string> codeLines = GenCommonFileContents("");

            // The constants.
            _consts.Keys.ForEach(v => codeLines.Add($"const int {v} = {_consts[v]};"));

            // The declared vars with the system hooks.
            _dynamic.Vars.Values.ForEach(v => codeLines.Add($"int {v.Name} {{ get {{ return Dynamic.Vars[\"{v.Name}\"].Value; }} set {{ Dynamic.Vars[\"{v.Name}\"].Value = value; }} }}"));

            // Needed for runtime script statuses.
            _dynamic.Tracks.Values.ForEach(t => codeLines.Add($"Track {t.Name} {{ get {{ return Dynamic.Tracks[\"{t.Name}\"]; }} }}"));

            // Used for manual/trigger inputs.
            _dynamic.Sequences.Values.ForEach(s => codeLines.Add($"Sequence {s.Name} {{ get {{ return Dynamic.Sequences[\"{s.Name}\"]; }} }}"));

            // Collected init stuff goes in a constructor.
            codeLines.Add($"public {_scriptName}() : base()");
            codeLines.Add("{");
            _initLines.ForEach(l => codeLines.Add(l));
            codeLines.Add("}");

            // Bottom stuff.
            codeLines.Add("}");
            codeLines.Add("}");

            return codeLines;
        }

        /// <summary>
        /// Create the file containing extra stuff. TODO2 Probably shouldn't do this every time.
        /// </summary>
        /// <returns></returns>
        List<string> GenSuppFileContents()
        {
            // Create the supplementary file. Indicated by empty source file name.
            List<string> codeLines = GenCommonFileContents("");

            // The various defines.
            _midiInstrumentDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiInstrumentDefs[k]};"));
            _midiDrumDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiDrumDefs[k]};"));
            _midiControllerDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiControllerDefs[k]};"));

            // Bottom stuff.
            codeLines.Add("}");
            codeLines.Add("}");

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file stuff.
        /// </summary>
        /// <param name="fn">Source file name. Empty means it's an internal file.</param>
        /// <returns></returns>
        List<string> GenCommonFileContents(string fn)
        {
            // Create the supplementary file.
            List<string> codeLines = new List<string>
            {
                $"//{fn}",
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using System.Linq;",
                "using System.Drawing;",
                "using System.Drawing.Drawing2D;",
                "using System.Windows.Forms;",
                "using Nebulator.Common;",
                "using Nebulator.Scripting;",
                "namespace Nebulator.UserScript",
                "{",
                $"public partial class {_scriptName} : Script",
                "{"
            };

            return codeLines;
        }

        #endregion

        #region Specific line parsers
        private void ParseInclude(FileParseContext pcont, List<string> parms)
        {
            // include path\name.neb
            // 0       1
            // or:
            // include path\split file name.neb
            // 0       1          2    3

            try
            {
                // Handle spaces in path.
                string fn = string.Join(" ", parms.GetRange(1, parms.Count - 1));

                if (!SourceFiles.Contains(fn)) // Check for already done.
                {
                    // Recursive call to parse this file
                    FileParseContext subcont = new FileParseContext()
                    {
                        SourceFile = fn,
                        LineNumber = 1
                    };

                    if(!ParseOneFile(subcont))
                    {
                        throw new Exception(fn);
                    }
                }
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid include: {parms[1]}");
            }
        }

        private void ParseConstant(FileParseContext pcont, List<string> parms)
        {
            // constant DRUM_DEF_VOL 100
            // 0        1            2

            try
            {
                _consts[parms[1]] = int.Parse(parms[2]);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid const: {parms[1]}");
            }
        }

        private void ParseVariable(FileParseContext pcont, List<string> parms)
        {
            // variable PITCH 8192
            // 0        1     2

            try
            {
                Variable v = new Variable()
                {
                    Name = parms[1],
                    Value = int.Parse(parms[2])
                };
                _dynamic.Vars.Add(v.Name, v);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid variable: {parms[1]}");
            }
        }

        private void ParseSequence(FileParseContext pcont, List<string> parms)
        {
            // sequence DRUMS_SIMPLE 8
            // 0        1            2

            try
            {
                Sequence ns = new Sequence()
                {
                    Name = parms[1],
                    Length = ParseConstRef(pcont, parms[2]),
                };
                _dynamic.Sequences.Add(ns.Name, ns);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid sequence: {parms[1]}");
            }
        }

        private void ParseMidiController(FileParseContext pcont, List<string> parms)
        {
            // midictlin  MI1 1 4     MODN
            // midictlout MO1 1 Pitch PITCH
            // 0          1   2 3     4

            try
            {
                int mctlr = 0;

                switch (parms[3])
                {
                    case string s when s.IsInteger():
                        mctlr = int.Parse(parms[3]);
                        break;

                    case "Pitch":
                        mctlr = MidiInterface.CTRL_PITCH;
                        break;

                    default:
                        mctlr = MidiInterface.TranslateController(parms[3]);
                        break;
                }

                MidiControlPoint ctl = new MidiControlPoint()
                {
                    Channel = int.Parse(parms[2]),
                    MidiController = mctlr,
                    RefVar = ParseVarRef(pcont, parms[4])
                };

                switch (parms[0])
                {
                    case "midictlin":
                        _dynamic.InputMidis.Add(parms[1], ctl);
                        break;

                    case "midictlout":
                        _dynamic.OutputMidis.Add(parms[1], ctl);
                        break;

                    default:
                        throw new Exception("");
                }
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid midi controller: {parms[1]}");
            }
        }

        private void ParseLever(FileParseContext pcont, List<string> parms)
        {
            // lever L3 -10 10 MODN
            // 0     1  2   3  4

            try
            {
                LeverControlPoint ctl = new LeverControlPoint()
                {
                    Min = int.Parse(parms[2]),
                    Max = int.Parse(parms[3]),
                    RefVar = ParseVarRef(pcont, parms[4])
                };
                _dynamic.Levers.Add(parms[1], ctl);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid lever: {parms[1]}");
            }
        }

        private void ParseTrack(FileParseContext pcont, List<string> parms)
        {
            try
            {
                // track KEYS 1 5 0 0
                // 0     1    2 3 4 5

                Track nt = new Track()
                {
                    Name = parms[1],
                    Channel = int.Parse(parms[2]),
                    WobbleVolume = parms.Count > 3 ? ParseConstRef(pcont, parms[3]) : 0,
                    WobbleTimeBefore = parms.Count > 4 ? -ParseConstRef(pcont, parms[4]) : 0,
                    WobbleTimeAfter = parms.Count > 5 ? ParseConstRef(pcont, parms[5]) : 0
                };
                _dynamic.Tracks.Add(nt.Name, nt);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid track: {parms[1]}");
            }
        }

        private void ParseSection(FileParseContext pcont, List<string> parms)
        {
            // section PART1 0 32
            // 0       1     2 3

            try
            {
                Section s = new Section()
                {
                    Name = parms[1],
                    Start = ParseConstRef(pcont, parms[2]),
                    Length = ParseConstRef(pcont, parms[3])
                };
                _dynamic.Sections.Add(s.Name, s);
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid section: {parms[1]}");
            }
        }

        private void ParseSectionTrack(FileParseContext pcont, List<string> parms)
        {
            // KEYS   SEQ1           SEQ2       algoXXX()        SEQ2
            // DRUMS  DRUMS_SIMPLE   DRUMS_X
            // 0      1              2          3                4

            try
            {
                SectionTrack st = new SectionTrack()
                {
                    TrackName = parms[0]
                };

                for (int i = 1; i < parms.Count; i++)
                {
                    st.SequenceNames.Add(parms[i]);
                }
                _dynamic.Sections.Values.Last().SectionTracks.Add(st);
            }
            catch (Exception )
            {
                AddParseError(pcont, $"Invalid section track: {parms[0]}");
            }
        }

        private void ParseSequenceElement(FileParseContext pcont, List<string> parms)
        {
            // WHEN WHICH VEL 1.50
            // 0    1     2   3
            // 0: 1.23, __x___x___x___x_, function
            // 1: 60, C.4, C.4.m7, RideCymbal1
            // 2: vel (opt): 90, const
            // 3: dur (opt): 1.23

            // e.g.
            // 00.00  G.3  90  0.60
            // 01.00  58  90  0.60
            // ----x-------x-x-----x-------x-x- AcousticSnare 80
            // algoDynamic()  90

            try
            {
                // Support note string, number, drum.
                SequenceElement seqel = null;

                if (parms[1].IsInteger())
                {
                    // Simple note number.
                    seqel = new SequenceElement(int.Parse(parms[1]))
                    {
                        Volume = ParseConstRef(pcont, parms[2])
                    };
                }
                else if (_midiDrumDefs.ContainsKey(parms[1]))
                {
                    // It's a drum.
                    seqel = new SequenceElement(int.Parse(_midiDrumDefs[parms[1]]))
                    {
                        Volume = ParseConstRef(pcont, parms[2]),
                        Duration = new Time(1) // nominal duration
                    };
                }
                else
                {
                    // Note or function string form.
                    seqel = new SequenceElement(parms[1])
                    {
                        Volume = ParseConstRef(pcont, parms[2])
                    };

                    if(seqel.Function != "")
                    {
                        _initLines.Add($"_scriptFunctions.Add(\"{seqel.Function}\", {seqel.Function});");
                    }
                }

                // Optional duration for musical note.
                if (parms.Count > 3)
                {
                    List<Time> t = ParseTime(pcont, parms[3]);
                    seqel.Duration = t[0];
                }

                List<Time> whens = ParseTime(pcont, parms[0]);
                foreach (Time t in whens)
                {
                    SequenceElement ncl = new SequenceElement(seqel) { When = t };
                    _dynamic.Sequences.Values.Last().Elements.Add(ncl);
                }
            }
            catch (Exception)
            {
                AddParseError(pcont, $"Invalid note: {parms[1]}");
            }
        }

        private void ParseScriptLine(FileParseContext pcont, List<string> parms, string original)
        {
            // public void On_MODN()

            try
            {
                // Store the whole line with line tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                pcont.CodeLines.Add($"{original} //{pcont.LineNumber}");

                // Test for event handler.
                foreach (string p in parms)
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
                AddParseError(pcont, "Invalid function line");
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
                ErrorType = ScriptErrorType.Parse,
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
                v = _dynamic.Vars[s];
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
                if(_consts.ContainsKey(s))
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
                if(s.Contains("."))
                {
                    // Single time value.
                    var parts = s.SplitByToken(".");

                    // Check for valid fractional part.
                    if (int.TryParse(parts[1], out int result))
                    {
                        if (result >= Globals.TOCKS_PER_TICK)
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
                        d *= Globals.TOCKS_PER_TICK;

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
                        switch(s[i])
                        {
                            case 'x':
                                // Note on.
                                times.Add(new Time(i / PATTERN_SIZE, (i % PATTERN_SIZE) * Globals.TOCKS_PER_TICK / PATTERN_SIZE));
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