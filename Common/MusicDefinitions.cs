using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NBagOfTricks;
using System.Diagnostics;

namespace Nebulator.Common
{
    /// <summary>Definitions for use inside scripts. For doc see MusicDefinitions.md.</summary>
    public static class MusicDefinitions
    {
        /// <summary>The chord/scale note definitions. Key is chord/scale name, value is list of constituent notes.</summary>
        public static Dictionary<string, List<string>> NoteDefs { get; private set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Load chord and midi definitions.
        /// </summary>
        public static void Init()
        {
            NoteDefs.Clear();

            foreach(string sl in _chordDefs.Concat(_scaleDefs))
            {
                List<string> parts = sl.SplitByToken(" ");
                var name = parts[0];
                parts.RemoveAt(0);
                NoteDefs[name] = parts;
            }
        }

        /// <summary>All the chord defs.</summary>
        static readonly List<string> _chordDefs = new()
        {
            "M 1 3 5", "m 1 b3 5", "7 1 3 5 b7", "M7 1 3 5 7", "m7 1 b3 5 b7", "6 1 3 5 6", "m6 1 b3 5 6", "o 1 b3 b5", "o7 1 b3 b5 bb7",
            "m7b5 1 b3 b5 b7", "\\+ 1 3 #5", "7#5 1 3 #5 b7", "9 1 3 5 b7 9", "7#9 1 3 5 b7 #9", "M9 1 3 5 7 9", "Madd9 1 3 5 9", "m9 1 b3 5 b7 9",
            "madd9 1 b3 5 9", "11 1 3 5 b7 9 11", "m11 1 b3 5 b7 9 11", "7#11 1 3 5 b7 #11", "M7#11 1 3 5 7 9 #11", "13 1 3 5 b7 9 11 13",
            "M13 1 3 5 7 9 11 13", "m13 1 b3 5 b7 9 11 13", "sus4 1 4 5", "sus2 1 2 5", "5 1 5"
        };

        /// <summary>All the def defs.</summary>
        static readonly List<string> _scaleDefs = new()
        {
            "Acoustic 1 2 3 #4 5 6 b7", "Aeolian 1 2 b3 4 5 b6 b7", "NaturalMinor 1 2 b3 4 5 b6 b7", "Algerian 1 2 b3 #4 5 b6 7",
            "Altered 1 b2 b3 b4 b5 b6 b7", "Augmented 1 b3 3 5 #5 7", "Bebop 1 2 3 4 5 6 b7 7", "Blues 1 b3 4 b5 5 b7",
            "Chromatic 1 #1 2 #2 3 4 #4 5 #5 6 #6 7", "Dorian 1 2 b3 4 5 6 b7", "DoubleHarmonic 1 b2 3 4 5 b6 7", "Enigmatic 1 b2 3 #4 #5 #6 7",
            "Flamenco 1 b2 3 4 5 b6 7", "Gypsy 1 2 b3 #4 5 b6 b7", "HalfDiminished 1 2 b3 4 b5 b6 b7", "HarmonicMajor 1 2 3 4 5 b6 7",
            "HarmonicMinor 1 2 b3 4 5 b6 7", "Hirajoshi 1 3 #4 5 7", "HungarianGypsy 1 2 b3 #4 5 b6 7", "HungarianMinor 1 2 b3 #4 5 b6 7",
            "In 1 b2 4 5 b6", "Insen 1 b2 4 5 b7", "Ionian 1 2 3 4 5 6 7", "Istrian 1 b2 b3 b4 b5 5", "Iwato 1 b2 4 b5 b7", "Locrian 1 b2 b3 4 b5 b6 b7",
            "LydianAugmented 1 2 3 #4 #5 6 7", "Lydian 1 2 3 #4 5 6 7", "Major 1 2 3 4 5 6 7", "MajorBebop 1 2 3 4 5 #5 6 7", "MajorLocrian 1 2 3 4 b5 b6 b7",
            "MajorPentatonic 1 2 3 5 6", "MelodicMinorAscending 1 2 b3 4 5 6 7", "MelodicMinorDescending 1 2 b3 4 5 b6 b7 8", "MinorPentatonic 1 b3 4 5 b7",
            "Mixolydian 1 2 3 4 5 6 b7", "NeapolitanMajor 1 b2 b3 4 5 6 7", "NeapolitanMinor 1 b2 b3 4 5 b6 7", "Octatonic 1 2 b3 4 b5 b6 6 7",
            "Persian 1 b2 3 4 b5 b6 7", "PhrygianDominant 1 b2 3 4 5 b6 b7", "Phrygian 1 b2 b3 4 5 b6 b7", "Prometheus 1 2 3 #4 6 b7",
            "Tritone 1 b2 3 b5 5 b7", "UkrainianDorian 1 2 b3 #4 5 6 b7", "WholeTone 1 2 3 #4 #5 #6", "Yo 1 b3 4 5 b7", 
        };
    }
}
