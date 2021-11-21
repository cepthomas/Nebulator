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
    /// <summary>The patch types. Numerical value is the midi number. Also includes GM drum kit defs for convenience.</summary>
    public enum InstrumentDef
    {
        AcousticGrandPiano = 0, BrightAcousticPiano, ElectricGrandPiano, HonkyTonkPiano, ElectricPiano1, ElectricPiano2, Harpsichord,
        Clavinet, Celesta, Glockenspiel, MusicBox, Vibraphone, Marimba, Xylophone, TubularBells, Dulcimer, DrawbarOrgan,
        PercussiveOrgan, RockOrgan, ChurchOrgan, ReedOrgan, Accordion, Harmonica, TangoAccordion, AcousticGuitarNylon,
        AcousticGuitarSteel, ElectricGuitarJazz, ElectricGuitarClean, ElectricGuitarMuted, OverdrivenGuitar, DistortionGuitar,
        GuitarHarmonics, AcousticBass, ElectricBassFinger, ElectricBassPick, FretlessBass, SlapBass1, SlapBass2, SynthBass1,
        SynthBass2, Violin, Viola, Cello, Contrabass, TremoloStrings, PizzicatoStrings, OrchestralHarp, Timpani,
        StringEnsemble1, StringEnsemble2, SynthStrings1, SynthStrings2, ChoirAahs, VoiceOohs, SynthVoice, OrchestraHit,
        Trumpet, Trombone, Tuba, MutedTrumpet, FrenchHorn, BrassSection, SynthBrass1, SynthBrass2, SopranoSax, AltoSax,
        TenorSax, BaritoneSax, Oboe, EnglishHorn, Bassoon, Clarinet, Piccolo, Flute, Recorder, PanFlute, BlownBottle,
        Shakuhachi, Whistle, Ocarina, Lead1Square, Lead2Sawtooth, Lead3Calliope, Lead4Chiff, Lead5Charang, Lead6Voice,
        Lead7Fifths, Lead8BassAndLead, Pad1NewAge, Pad2Warm, Pad3Polysynth, Pad4Choir, Pad5Bowed, Pad6Metallic, Pad7Halo,
        Pad8Sweep, Fx1Rain, Fx2Soundtrack, Fx3Crystal, Fx4Atmosphere, Fx5Brightness, Fx6Goblins, Fx7Echoes, Fx8SciFi,
        Sitar, Banjo, Shamisen, Koto, Kalimba, BagPipe, Fiddle, Shanai, TinkleBell, Agogo, SteelDrums, Woodblock,
        TaikoDrum, MelodicTom, SynthDrum, ReverseCymbal, GuitarFretNoise, BreathNoise, Seashore, BirdTweet, TelephoneRing,
        Helicopter, Applause, Gunshot,
        // GM Drum Kits
        Standard = 0, Room = 8, Power = 16, Electronic = 24, TR808 = 25, Jazz = 32, Brush = 40, Orchestra = 48, SFX = 56
    };

    /// <summary>Supported drum names.</summary>
    public enum DrumDef
    {
        AcousticBassDrum = 35, BassDrum1 = 36, SideStick = 37, AcousticSnare = 38, HandClap = 39, ElectricSnare = 40, LowFloorTom = 41,
        ClosedHiHat = 42, HighFloorTom = 43, PedalHiHat = 44, LowTom = 45, OpenHiHat = 46, LowMidTom = 47, HiMidTom = 48, CrashCymbal1 = 49,
        HighTom = 50, RideCymbal1 = 51, ChineseCymbal = 52, RideBell = 53, Tambourine = 54, SplashCymbal = 55, Cowbell = 56, CrashCymbal2 = 57,
        Vibraslap = 58, RideCymbal2 = 59, HiBongo = 60, LowBongo = 61, MuteHiConga = 62, OpenHiConga = 63, LowConga = 64, HighTimbale = 65,
        LowTimbale = 66, HighAgogo = 67, LowAgogo = 68, Cabasa = 69, Maracas = 70, ShortWhistle = 71, LongWhistle = 72, ShortGuiro = 73,
        LongGuiro = 74, Claves = 75, HiWoodBlock = 76, LowWoodBlock = 77, MuteCuica = 78, OpenCuica = 79, MuteTriangle = 80, OpenTriangle = 81
    }

    /// <summary>Supported controllers.</summary>
    public enum ControllerDef
    {
        BankSelect = 0, Modulation = 1, BreathController = 2, FootController = 4, PortamentoTime = 5, Volume = 7, Balance = 8, Pan = 10,
        Expression = 11, BankSelectLSB = 32, ModulationLSB = 33, BreathControllerLSB = 34, FootControllerLSB = 36, PortamentoTimeLSB = 37,
        VolumeLSB = 39, BalanceLSB = 40, PanLSB = 42, ExpressionLSB = 43, Sustain = 64, Portamento = 65, Sostenuto = 66, SoftPedal = 67,
        Legato = 68, Sustain2 = 69, PortamentoControl = 84, AllSoundOff = 120, ResetAllControllers = 121, LocalKeyboard = 122, AllNotesOff = 123,
        // Specials for internal use.
        NoteControl = 250, PitchControl = 251, None = 252
    }

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
