using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
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

        /// <summary>All the important time points and their names.</summary>
        public Dictionary<Time, string> TimeDefs { get; } = new Dictionary<Time, string>();
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

        #region Main method
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
            _dynamic = new Dynamic();
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

            // Try fully qualified
            if (File.Exists(pcont.SourceFile))
            {
                // OK - leave as is.
                valid = true;
            }
            else // Try relative
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
                pcont.CodeLines.AddRange(new List<string>
                {
                    $"//{pcont.SourceFile}",
                    "using  Nebulator.Scripting;",
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

                    List<string> allparts = line.SplitByTokens("(),;= ");

                    // What is it?
                    bool handled = false;

                    if (allparts.Count >= 2)
                    {
                        handled = true;
                        switch (allparts[0])
                        {
                            case "include": ParseInclude(pcont, allparts); break;
                            case "loop": ParseLoop(pcont, allparts); break;
                            case "note": ParseNote(pcont, allparts); break;
                            default:
                                switch (allparts[1])
                                {
                                    case "const": ParseConst(pcont, allparts); break;
                                    case "var": ParseVar(pcont, allparts); break;
                                    case "lever": ParseLever(pcont, allparts); break;
                                    case "track": ParseTrack(pcont, allparts); break;
                                    case "seq": ParseSeq(pcont, allparts); break;
                                    case "midiin": ParseMidiController(pcont, allparts); break;
                                    case "midiout": ParseMidiController(pcont, allparts); break;
                                    default: handled = false; break;
                                }
                                break;
                        }
                    }

                    if(!handled)
                    {
                        // Assume anything else is script.
                        ParseScriptLine(pcont, allparts, line);
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
                "using Nebulator.Scripting;",
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
        /// Create the file containing extra stuff. TODO2 Probably shouldn't do this every time.
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
                "using Nebulator.Scripting;",
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
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid include: " + ex.Message);
            }
        }

        private void ParseConst(FileParseContext pcont, List<string> parms)
        {
            // PART1 const 0
            // 0     1     2

            try
            {
                _consts[parms[0]] = int.Parse(parms[2]);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid const: " + ex.Message);
            }
        }

        private void ParseVar(FileParseContext pcont, List<string> parms)
        {
            // COL1 var 200
            // 0    1   2

            try
            {
                Variable v = new Variable()
                {
                    Name = parms[0],
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
            // KEYS_VERSE1 seq 16
            // 0           1   2

            try
            {
                Sequence ns = new Sequence()
                {
                    Name = parms[0],
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
            // note WHEN WHICH VEL 1.50*
            // 0    1    2     3   4
            // WHEN: 1.23, __1___1___1___1_ (or #)
            // WHICH: 60, C.4, C.4.m7, RideCymbal1
            // VEL: 90, const

            try
            {
                // Support note string, number, drum.
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
                    List<Time> t = ParseTime(pcont, parms[4]);
                    n.Duration = t[0];
                }

                // The rest is common.
                n.Volume = ParseConstRef(pcont, parms[3]);

                List<Time> whens = ParseTime(pcont, parms[1]);
                foreach(Time t in whens)
                {
                    Note ncl = new Note(n) { When = t };
                    _dynamic.Sequences.Values.Last().Notes.Add(ncl);
                }
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid note: " + ex.Message);
            }
        }

        private void ParseLoop(FileParseContext pcont, List<string> parms)
        {
            // loop PART1 PART2 KEYS_VERSE1
            // 0    1     2     3

            try
            {
                Loop nl = new Loop()
                {
                    StartTick = ParseConstRef(pcont, parms[1]),
                    EndTick = ParseConstRef(pcont, parms[2]),
                    SequenceName = parms[3]
                };
                _dynamic.Tracks.Values.Last().Loops.Add(nl);

                // Save any important times.
                if (!parms[1].IsInteger())
                {
                    TimeDefs[new Time(nl.StartTick, 0)] = parms[1];
                }

                if (!parms[2].IsInteger())
                {
                    TimeDefs[new Time(nl.EndTick, 0)] = parms[2];
                }
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
                // KEYS track 1 5 0 0
                // 0    1     2 3 4 5

                Track nt = new Track()
                {
                    Name = parms[0],
                    Channel = int.Parse(parms[2]),
                    WobbleVolume = parms.Count > 3 ? ParseConstRef(pcont, parms[3]) : 0,
                    WobbleTimeBefore = parms.Count > 4 ? -ParseConstRef(pcont, parms[4]) : 0,
                    WobbleTimeAfter = parms.Count > 5 ? ParseConstRef(pcont, parms[5]) : 0
                };
                _dynamic.Tracks.Add(nt.Name, nt);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid track: " + ex.Message);
            }
        }

        private void ParseMidiController(FileParseContext pcont, List<string> parms)
        {
            // MI midiin  1 2     MODN
            // MO midiout 1 Pitch PITCH
            // 0  1       2 3     4

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

                switch(parms[1])
                {
                    case "midiin":
                        _dynamic.InputMidis.Add(parms[0], ctl);
                        break;

                    case "midiout":
                        _dynamic.OutputMidis.Add(parms[0], ctl);
                        break;

                    default:
                        throw new Exception("");
                }
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid midi controller: " + ex.Message);
            }
        }

        private void ParseLever(FileParseContext pcont, List<string> parms)
        {
            // LEVER1 lever 0 255 COL1
            // 0      1     2 3   4

            try
            {
                LeverControlPoint ctl = new LeverControlPoint()
                {
                    Min = int.Parse(parms[2]),
                    Max = int.Parse(parms[3]),
                    RefVar = ParseVarRef(pcont, parms[4])
                };
                _dynamic.Levers.Add(parms[0], ctl);
            }
            catch (Exception ex)
            {
                AddParseError(pcont, "Invalid lever controller: " + ex.Message);
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
                AddParseError(pcont, "Invalid time");
                times.Clear();
            }
            return times;
        }
        #endregion
    }
}