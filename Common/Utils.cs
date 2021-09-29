using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Nebulator.Common
{
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal, Mute, Solo }

    /// <summary>Trace log markers.</summary>
    public enum TraceCat { SND, RCV }

    /// <summary>The patch types. Numerical value is the midi number.</summary>
    public enum Patch
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
        Helicopter, Applause, Gunshot
    };

    public static class Definitions
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>General UI.</summary>
        public const int BORDER_WIDTH = 1;

        /// <summary>Standard midi.</summary>
        public const int MAX_MIDI = 127;
        #endregion
    }

    /// <summary>
    /// Static utility functions.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i">Number to fix.</param>
        /// <returns>Fixed number.</returns>
        public static UInt32 FixEndian(UInt32 i)
        {
            return BitConverter.IsLittleEndian ?
                ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24) :
                i;
        }

        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i">Number to fix.</param>
        /// <returns>Fixed number.</returns>
        public static UInt16 FixEndian(UInt16 i)
        {
            return BitConverter.IsLittleEndian ?
                (UInt16)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8)) :
                i;
        }
    }



    /// <summary>Converter for selecting property value from known lists.TODO2 better home?</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "MidiInDevice":
                    rec = new(); 
                    for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                    }
                    break;

                case "MidiOutDevice":
                    rec = new(); 
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;

                //case "Patch":
                //    rec = new(ScriptDefinitions.TheDefinitions.Patches);
                //    break;
            }

            return new StandardValuesCollection(rec);
        }
    }
}
