using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NBagOfTricks;


namespace Nebulator.Common
{
    /// <summary>Definitions for use inside scripts.</summary>
    public class ScriptDefinitions
    {
        /// <summary>Current global defs.</summary>
        public static ScriptDefinitions TheDefinitions { get; private set; } = new ScriptDefinitions();

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

        // /// <summary>The midi instrument names ordered by patch numbers.</summary>
        // public string[] Patches { get; private set; } = new string[Definitions.MAX_MIDI+1];

        /// <summary>Helper for internals. Really should be separate classes - avoiding over-OOPing.</summary>
        public int NoteControl { get; set; } = -1;

        /// <summary>Helper for internals.</summary>
        public int PitchControl { get; set; } = -1;

        /// <summary>
        /// Load chord and midi definitions from md doc file.
        /// NOTE!! This is a local file copied from the wiki project - if that one is updated, recopy to the local.
        /// </summary>
        public void Init()
        {
            InstrumentDefs.Clear();
            DrumDefs.Clear();
            ControllerDefs.Clear();
            ChordDefs.Clear();
            ScaleDefs.Clear();

            // Read the file.
            object? currentSection = null;

            string fpath = Path.Combine(MiscUtils.GetExeDir(), @"Resources\ScriptDefinitions.md");
            foreach (string sl in File.ReadAllLines(fpath))
            {
                List<string> parts = sl.SplitByToken("|");

                if (parts.Count > 1 && !parts[0].StartsWith("#"))
                {
                    switch (parts[0])
                    {
                        case "Instrument":
                            currentSection = InstrumentDefs;
                            break;

                        case "Drum":
                            currentSection = DrumDefs;
                            break;

                        case "Controller":
                            currentSection = ControllerDefs;
                            break;

                        case "Chord":
                            currentSection = ChordDefs;
                            break;

                        case "Scale":
                            currentSection = ScaleDefs;
                            break;

                        case string s when !s.StartsWith("---"):
                            switch (currentSection)
                            {
                                case Dictionary<string, string> sd:
                                    sd[parts[0]] = parts[1];
                                    break;

                                case Dictionary<string, List<string>> sd:
                                    sd.Add(parts[0], parts.GetRange(1, parts.Count - 1));
                                    break;

                                case null:
                                    // Ignore.
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

            //// Patches.
            //Patches.ForEach(p => p = "NoPatch"); // default
            //InstrumentDefs.ForEach( kv =>
            //{
            //    if(int.TryParse(kv.Value, out int inum) && inum >= 0 && inum <= Definitions.MAX_MIDI)
            //    {
            //        Patches[inum] = kv.Key;
            //    }
            //});
        }
    }
}
