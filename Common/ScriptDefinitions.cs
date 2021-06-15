using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NBagOfTricks;


namespace Nebulator.Common
{
    public class ScriptDefinitions
    {
        /// <summary>Current global defs.</summary>
        public static ScriptDefinitions TheDefinitions { get; set; } = new ScriptDefinitions();

        /// <summary>The midi instrument definitions from ScriptDefinitions.md.</summary>
        public Dictionary<string, string> InstrumentDefs { get; private set; } = new Dictionary<string, string>();

        /// <summary>The midi drum definitions from ScriptDefinitions.md.</summary>
        public Dictionary<string, string> DrumDefs { get; private set; } = new Dictionary<string, string>();

        /// <summary>The midi controller definitions from ScriptDefinitions.md.</summary>
        public Dictionary<string, string> ControllerDefs { get; private set; } = new Dictionary<string, string>();

        /// <summary>The chord definitions from ScriptDefinitions.md. Key is chord name, Value is list of constituent notes.</summary>
        public Dictionary<string, List<string>> ChordDefs { get; private set; } = new Dictionary<string, List<string>>();

        /// <summary>The scale definitions from ScriptDefinitions.md. Key is scale name, Value is list of constituent notes.</summary>
        public Dictionary<string, List<string>> ScaleDefs { get; private set; } = new Dictionary<string, List<string>>();

        /// <summary>Helper for internals. Really should be separate classes - avoiding over-OOPing.</summary>
        public int NoteControl { get; set; } = -1;

        /// <summary>Helper for internals.</summary>
        public int PitchControl { get; set; } = -1;

        /// <summary>
        /// Load chord and midi definitions from md doc file.
        /// </summary>
        public void Init()
        {
            InstrumentDefs.Clear();
            DrumDefs.Clear();
            ControllerDefs.Clear();
            ChordDefs.Clear();
            ScaleDefs.Clear();

            // Read the file.
            object section = null;

            string fpath = Path.Combine(MiscUtils.GetExeDir(), @"Resources\ScriptDefinitions.md");
            foreach (string sl in File.ReadAllLines(fpath))
            {
                List<string> parts = sl.SplitByToken("|");

                if (parts.Count > 1 && !parts[0].StartsWith("#"))
                {
                    switch (parts[0])
                    {
                        case "Instrument":
                            section = InstrumentDefs;
                            break;

                        case "Drum":
                            section = DrumDefs;
                            break;

                        case "Controller":
                            section = ControllerDefs;
                            break;

                        case "Chord":
                            section = ChordDefs;
                            break;

                        case "Scale":
                            section = ScaleDefs;
                            break;

                        case string s when !s.StartsWith("---"):
                            switch(section)
                            {
                                case Dictionary<string, string> sd:
                                    (section as Dictionary<string, string>)[parts[0]] = parts[1];
                                    break;

                                case Dictionary<string, List<string>> sd:
                                    (section as Dictionary<string, List<string>>).Add(parts[0], parts.GetRange(1, parts.Count - 1));
                                    break;

                                default:
                                    throw new Exception("Invalid script definition processing");
                            }
                            break;
                    }
                }
            }

            // Internals.
            NoteControl = int.Parse(ControllerDefs["NoteControl"]);
            PitchControl = int.Parse(ControllerDefs["PitchControl"]);
        }
    }
}
