using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using NLog;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Model;
using Nebulator.Midi;


namespace Nebulator.Engine
{
    class FileParseContext
    {
        /// <summary>Current source file.</summary>
        public string SourceFile { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>Current source line.</summary>
        public int LineNumber { get; set; } = 1;

        /// <summary>Accumulated script code lines.</summary>
        public List<string> CodeLines { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parses/compiles neb file(s).
    /// </summary>
    public class Compiler
    {
        #region Properties
        /// <summary>Accumulated errors.</summary>
        public List<ScriptError> Errors { get; } = new List<ScriptError>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }

        /// <summary>All the important time points and their names.</summary>
        public Dictionary<int, string> TimeDefs { get; } = new Dictionary<int, string>();
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Starting directory.</summary>
        string _baseDir = Globals.UNKNOWN_STRING;

        /// <summary>Script info.</summary>
        string _scriptName = Globals.UNKNOWN_STRING;

        /// <summary>Collected runtime variables, controls, etc.</summary>
        ScriptDynamic _dynamic = new ScriptDynamic();

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

        #region Main function
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to topmost file.</param>
        public Script Execute(string nebfn)
        {
            _logger.Info($"Compiling {nebfn}.");

            // Reset everything.
            _filesToCompile.Clear();
            _consts.Clear();
            _initLines.Clear();
            _dynamic = new ScriptDynamic();
            TimeDefs.Clear();
            Errors.Clear();

            // Init things.
            _scriptName = Path.GetFileNameWithoutExtension(nebfn);
            _baseDir = Path.GetDirectoryName(nebfn);
            LoadDefinitions();

            // Parse.
            DateTime startTime = DateTime.Now;
            Parse(nebfn);
            _logger.Info($"Parse files took {(DateTime.Now - startTime).Milliseconds} msec.");

            // Compile.
            startTime = DateTime.Now;
            Script script = Compile();
            _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");

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

            ///// Add the generated internal code.
            List<string> mainLines = GenMainFileContents();
            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = mainLines
            });

            List<string> suppLines = GenSuppFileContents();
            _filesToCompile.Add($"{_scriptName}_{_filesToCompile.Count}.cs", new FileParseContext()
            {
                SourceFile = "",
                CodeLines = suppLines
            });
        }

        /// <summary>
        /// Parse one file. This is recursive to support nested include.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool ParseOneFile(FileParseContext pcont)
        {
            bool valid = true;

            if (File.Exists(pcont.SourceFile)) // Try fully qualified
            {
                // OK - leave as is.
            }
            else if (File.Exists(Path.Combine(_baseDir, pcont.SourceFile))) // Try relative
            {
                pcont.SourceFile = Path.Combine(_baseDir, pcont.SourceFile); // Save the fully qualified path
            }
            else
            {
                valid = false;
            }

            if (valid)
            {
                string genFn = $"{_scriptName}_{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

                // Preamble.
                pcont.CodeLines.AddRange(new List<string>
                {
                    $"//{pcont.SourceFile}",
                    "using  Nebulator.Engine;",
                    "namespace Nebulator.UserScript",
                    "{",
                    $"partial class {_scriptName}",
                    "{"
                });

                for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                {
                    string s = sourceLines[pcont.LineNumber - 1].Trim();

                    // Remove any comments.
                    int pos = s.IndexOf("//");
                    string line = pos >= 0 ? s.Left(pos) : s;
                    List<string> allparts = line.SplitByTokens("(),; ");
                    List<string> minparts = line.SplitByTokens("(");

                    if (minparts.Count > 0)
                    {
                        switch (minparts[0])
                        {
                            case "include": ParseInclude(pcont, allparts); break;
                            case "const": ParseConst(pcont, allparts); break;
                            case "var": ParseVar(pcont, allparts); break;
                            case "midiin": ParseMidiController(pcont, allparts, true); break;
                            case "midiout": ParseMidiController(pcont, allparts, false); break;
                            case "lever": ParseLever(pcont, allparts); break;
                            case "track": ParseTrack(pcont, allparts); break;
                            case "seq": ParseSeq(pcont, allparts); break;
                            case "loop": ParseLoop(pcont, allparts); break;
                            case "note": ParseNote(pcont, allparts); break;
                            // Assume anything else is script.
                            default: ParseScriptLine(pcont, allparts, line); break;
                        }
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
                    //OutputAssembly = _scriptName,
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = false,
                    IncludeDebugInformation = true
                };

                // The usual suspects.
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Core.dll");
                cp.ReferencedAssemblies.Add("System.Data.dll");
                cp.ReferencedAssemblies.Add("Nebulator.exe");

                // Add the generated source files.
                List<string> paths = new List<string>();

                // Create output area.
                string tempdir = Path.Combine(_baseDir, "temp");
                if (!Directory.Exists(tempdir))
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

                // Would actually like to use roslyn but doesn't work:
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
                        // The line should end with line number: "//1234"
                        int origLineNum = 0; // defaults
                        string origFileName = Globals.UNKNOWN_STRING;
                        string msg = "";

                        // Dig out the offending source code.
                        string fpath = Path.GetFileName(err.FileName.ToLower());
                        if (_filesToCompile.ContainsKey(fpath))
                        {
                            FileParseContext ci = _filesToCompile[fpath];
                            origFileName = ci.SourceFile;
                            string serr = ci.CodeLines[err.Line - 1];

                            if (origFileName == "")
                            {
                                // Must be the internal generated file. Do the best we can.
                                origLineNum = err.Line;
                                msg = $"Error: {err.ErrorText} in: {serr}";
                            }
                            else
                            {
                                int ind = serr.LastIndexOf("//");

                                if (ind != -1)
                                {
                                    serr = serr.Substring(ind + 2);
                                    int.TryParse(serr, out origLineNum);
                                    msg = err.ErrorText;
                                }
                            }

                            Errors.Add(new ScriptError()
                            {
                                ErrorType = ScriptErrorType.Compile,
                                SourceFile = origFileName,
                                LineNumber = origLineNum,
                                Message = msg
                            });
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
            List<string> codeLines = new List<string>
            {
                "//",
                "using System;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using Nebulator.Common;",
                "using Nebulator.Model;",
                "using Nebulator.Engine;",
                "namespace Nebulator.UserScript",
                "{",
                $"public partial class {_scriptName} : Script",
                "{"
            };

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
        /// Create the file containing extra stuff. FUTURE Probably shouldn't do this every time...
        /// </summary>
        /// <returns></returns>
        List<string> GenSuppFileContents()
        {
            // Create the supplementary file. Indicated by empty source file name.
            List<string> codeLines = new List<string>
            {
                "//",
                "using System;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using Nebulator.Common;",
                "using Nebulator.Model;",
                "using Nebulator.Engine;",
                "namespace Nebulator.UserScript",
                "{",
                $"public partial class {_scriptName} : Script",
                "{"
            };

            // The various defines.
            _midiInstrumentDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiInstrumentDefs[k]};"));
            _midiDrumDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiDrumDefs[k]};"));
            _midiControllerDefs.Keys.ForEach(k => codeLines.Add($"const int {k} = {_midiControllerDefs[k]};"));

            // Bottom stuff.
            codeLines.Add("}");
            codeLines.Add("}");

            return codeLines;
        }
        #endregion

        #region Conversion to step collection
        /// <summary>
        /// Turn collected stuff into midi event sequence.
        /// </summary>
        public StepCollection ConvertToSteps()
        {
            StepCollection steps = new StepCollection();

            // Gather the sequence definitions.
            Dictionary<string, Sequence> sequences = _dynamic.Sequences.Values.Distinct().ToDictionary(i => i.Name, i => i);

            // Process the composition values.
            foreach (Track track in _dynamic.Tracks.Values)
            {
                Wobbler timeWobbler = new Wobbler()
                {
                    RangeLow = -track.WobbleTimeBefore,
                    RangeHigh = track.WobbleTimeAfter
                };

                Wobbler volWobbler = new Wobbler()
                {
                    RangeLow = -track.WobbleVolume,
                    RangeHigh = track.WobbleVolume
                };

                // Put the loops in time order.
                track.Loops.Sort((a, b) => a.StartTick.CompareTo(b.StartTick));

                foreach (Loop loop in track.Loops)
                {
                    // Get the loop sequence info.
                    Sequence nseq = sequences[loop.SequenceName];

                    for (int loopTick = loop.StartTick; loopTick < loop.EndTick; loopTick += nseq.Length)
                    {
                        foreach (Note note in nseq.Notes)
                        {
                            // Create the note start and stop times.
                            int toffset = timeWobbler.Next(loopTick);
                            Time startNoteTime = new Time(loopTick, toffset) + note.When;
                            Time stopNoteTime = startNoteTime + note.Duration;

                            // Process all note numbers.
                            foreach (int noteNum in note.NoteNumbers)
                            {
                                ///// Note on.
                                StepNoteOn step = new StepNoteOn()
                                {
                                    Tag = track,
                                    Channel = track.Channel,
                                    NoteNumber = noteNum,
                                    NoteNumberToPlay = noteNum,
                                    Velocity = volWobbler.Next(note.Volume),
                                    VelocityToPlay = volWobbler.Next(note.Volume),
                                    Duration = note.Duration
                                };
                                steps.AddStep(startNoteTime, step);

                                // Maybe add a deferred stop note.
                                if (stopNoteTime != startNoteTime)
                                {
                                    steps.AddStep(stopNoteTime, new StepNoteOff(step));
                                }
                                // else client is taking care of it.
                            }
                        }
                    }
                }
            }

            return steps;
        }
        #endregion

        #region Specific line parsers
        private void ParseInclude(FileParseContext pcont, List<string> parms)
        {
            try
            {
                // Handle spaces in path.
                string fn = string.Join("", parms.GetRange(1, parms.Count - 1));

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
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid include: " + ex.Message);
            }
        }

        private void ParseConst(FileParseContext pcont, List<string> parms)
        {
            try
            {
                _consts[parms[1]] = int.Parse(parms[2]);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid const: " + ex.Message);
            }
        }

        private void ParseVar(FileParseContext pcont, List<string> parms)
        {
            try
            {
                Variable v = new Variable()
                {
                    Name = parms[1],
                    Value = int.Parse(parms[2])
                };
                _dynamic.Vars.Add(v.Name, v);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid var: " + ex.Message);
            }
        }

        private void ParseSeq(FileParseContext pcont, List<string> parms)
        {
            try
            {
                Sequence ns = new Sequence()
                {
                    Name = parms[1],
                    Length = ParseConstRef(pcont, parms[2]),
                };
                _dynamic.Sequences.Add(ns.Name, ns);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid seq: " + ex.Message);
            }
        }

        private void ParseNote(FileParseContext pcont, List<string> parms)
        {
            try
            {
                // Support note string, number, drum.  03.10 C4 90 00.8
                Note n = null;

                if (parms[2].IsInteger())
                {
                    // Simple note number.
                    n = new Note(int.Parse(parms[2]));
                }
                else if (_midiDrumDefs.ContainsKey(parms[2]))
                {
                    // It's a drum.
                    n = new Note(int.Parse(_midiDrumDefs[parms[2]]))
                    {
                        Duration = new Time(1) // nominal duration
                    };
                }
                else
                {
                    // String form.
                    n = new Note(parms[2]);
                }

                // Optional duration for musical note.
                if (parms.Count > 4)
                {
                    Time t = ParseTime(pcont, parms[4]);
                    if (t != null)
                    {
                        n.Duration = t;
                    }
                }

                // The rest is common.
                n.When = ParseTime(pcont, parms[1]);
                n.Volume = ParseConstRef(pcont, parms[3]);
                _dynamic.Sequences.Values.Last().Notes.Add(n);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid note: " + ex.Message);
            }
        }

        private void ParseLoop(FileParseContext pcont, List<string> parms)
        {
            // loop(start-tick#, end-tick#, seq-name);
            try
            {
                Loop nl = new Loop()
                {
                    StartTick = ParseConstRef(pcont, parms[1]),
                    EndTick = ParseConstRef(pcont, parms[2]),
                    SequenceName = parms[3]
                };
                _dynamic.Tracks.Values.Last().Loops.Add(nl);

                TimeDefs[nl.StartTick] = parms[1];
                TimeDefs[nl.EndTick] = parms[2];
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid loop: " + ex.Message);
            }
        }

        private void ParseTrack(FileParseContext pcont, List<string> parms)
        {
            try
            {
                Track nt = new Track()
                {
                    Name = parms[1],
                    Channel = int.Parse(parms[2]),
                    WobbleVolume = parms.Count > 3 ? ParseConstRef(pcont, parms[3]) : 0,
                    WobbleTimeBefore = parms.Count > 4 ? ParseConstRef(pcont, parms[4]) : 0,
                    WobbleTimeAfter = parms.Count > 5 ? ParseConstRef(pcont, parms[5]) : 0
                };
                _dynamic.Tracks.Add(nt.Name, nt);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid track: " + ex.Message);
            }
        }

        private void ParseMidiController(FileParseContext pcont, List<string> parms, bool min)
        {
            try
            {
                int mctlr = 0;

                switch (parms[2])
                {
                    case string s when s.IsInteger():
                        mctlr = int.Parse(parms[2]);
                        break;

                    case "Pitch":
                        mctlr = MidiInterface.CTRL_PITCH;
                        break;

                    default:
                        mctlr = MidiInterface.TranslateController(parms[2]);
                        break;
                }

                MidiControlPoint ctl = new MidiControlPoint()
                {
                    Channel = int.Parse(parms[1]),
                    MidiController = mctlr,
                    RefVar = ParseVarRef(pcont, parms[3])
                };

                if (min)
                {
                    _dynamic.InputMidis.Add(parms[3], ctl);
                }
                else
                {
                    _dynamic.OutputMidis.Add(parms[3], ctl);
                }
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid midi in controller: " + ex.Message);
            }
        }

        private void ParseLever(FileParseContext pcont, List<string> parms)
        {
            try
            {
                LeverControlPoint ctl = new LeverControlPoint()
                {
                    Min = int.Parse(parms[1]),
                    Max = int.Parse(parms[2]),
                    RefVar = ParseVarRef(pcont, parms[3])
                };
                _dynamic.Levers.Add(parms[3], ctl);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid lever controller: " + ex.Message);
            }
        }

        private void ParseScriptLine(FileParseContext pcont, List<string> parms, string original)
        {
            try
            {
                // Store the whole line with line tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                pcont.CodeLines.Add($"{original} //{pcont.LineNumber}");

                // Test for event handler.
                foreach (string p in parms)
                {
                    if (p.StartsWith("On_"))
                    {
                        List<string> fs = p.SplitByTokens("(");
                        string n = fs[0].Replace("On_", "");
                        _initLines.Add($"_scriptFunctions.Add(\"{n}\", {fs[0]});");
                    }
                }
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid function line: " + ex.Message);
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
                    AddParseError(pcont, "Invalid reference name " + s);
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
                    AddParseError(pcont, "Invalid reference " + s);
                }
            }
            return c;
        }

        /// <summary>
        /// Parse a native file line.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <param name="s">The line.</param>
        Time ParseTime(FileParseContext pcont, string s)
        {
            Time t = null;
            try
            {
                var parts = s.SplitByToken(".");

                // Check for valid fractional part.
                if(int.TryParse(parts[1], out int result))
                {
                    if(result >= Globals.TOCKS_PER_TICK)
                    {
                        throw (null); // too big
                    }

                    t = new Time()
                    {
                        Tick = int.Parse(parts[0]),
                        Tock = int.Parse(parts[1])
                    };
                }
                else
                {
                    // Try parsing fractions: 1/2, 3/4, 5/8 3/16 9/32 etc.
                    var frac = parts[1].SplitByToken("/");

                    if(frac.Count != 2)
                    {
                        throw (null); // incorrect number
                    }

                    double d = double.Parse(frac[0]) / double.Parse(frac[1]);

                    if(d >= 1.0)
                    {
                        throw (null); // invalid fraction
                    }

                    // Scale.
                    d *= Globals.TOCKS_PER_TICK;

                    // Truncate.
                    d = Math.Floor(d);

                    t = new Time()
                    {
                        Tick = int.Parse(parts[0]),
                        Tock = (int)d
                    };
                }
            }
            catch (Exception)
            {
                AddParseError(pcont, "Invalid time");
            }
            return t;
        }
        #endregion
    }
}