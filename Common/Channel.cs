using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.ComponentModel;


namespace Nebulator.Common
{
    /// <summary>One channel output.</summary>
    [Serializable]
    public class Channel
    {
        #region Properties
        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IOutputDevice Device { get; set; } = null;

        /// <summary>The device type for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>The device name for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string DeviceName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The UI name for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public string ChannelName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use 1-16.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public int ChannelNumber { get; set; } = 1;

        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public int Patch { get; set; } = 0;

        /// <summary>Current volume.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double Volume { get; set; } = 0.8;

        /// <summary>How wobbly.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double WobbleRange { get; set; } = 0.0;

        /// <summary>Wobbler for volume (optional).</summary>
        [JsonIgnore]
        [Browsable(false)]
        public Wobbler VolWobbler { get; set; } = null;

        /// <summary>Current state for this channel.</summary>
        [Browsable(false)]
        public ChannelState State { get; set; } = ChannelState.Normal;
        #endregion

        /// <summary>Get the next volume.</summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            return VolWobbler is null ? def : VolWobbler.Next(def);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ChannelName:{ChannelName}";
            return s;
        }
    }

        /// <summary>The midi instrument definitions.</summary>
        static readonly string[] _instrumentDefs = new string[]
        {
            "AcousticGrandPiano", "BrightAcousticPiano", "ElectricGrandPiano", "HonkyTonkPiano", "ElectricPiano1", "ElectricPiano2", "Harpsichord",
            "Clavinet", "Celesta", "Glockenspiel", "MusicBox", "Vibraphone", "Marimba", "Xylophone", "TubularBells", "Dulcimer", "DrawbarOrgan",
            "PercussiveOrgan", "RockOrgan", "ChurchOrgan", "ReedOrgan", "Accordion", "Harmonica", "TangoAccordion", "AcousticGuitarNylon",
            "AcousticGuitarSteel", "ElectricGuitarJazz", "ElectricGuitarClean", "ElectricGuitarMuted", "OverdrivenGuitar", "DistortionGuitar",
            "GuitarHarmonics", "AcousticBass", "ElectricBassFinger", "ElectricBassPick", "FretlessBass", "SlapBass1", "SlapBass2", "SynthBass1",
            "SynthBass2", "Violin", "Viola", "Cello", "Contrabass", "TremoloStrings", "PizzicatoStrings", "OrchestralHarp", "Timpani",
            "StringEnsemble1", "StringEnsemble2", "SynthStrings1", "SynthStrings2", "ChoirAahs", "VoiceOohs", "SynthVoice", "OrchestraHit",
            "Trumpet", "Trombone", "Tuba", "MutedTrumpet", "FrenchHorn", "BrassSection", "SynthBrass1", "SynthBrass2", "SopranoSax", "AltoSax",
            "TenorSax", "BaritoneSax", "Oboe", "EnglishHorn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "PanFlute", "BlownBottle",
            "Shakuhachi", "Whistle", "Ocarina", "Lead1Square", "Lead2Sawtooth", "Lead3Calliope", "Lead4Chiff", "Lead5Charang", "Lead6Voice",
            "Lead7Fifths", "Lead8BassAndLead", "Pad1NewAge", "Pad2Warm", "Pad3Polysynth", "Pad4Choir", "Pad5Bowed", "Pad6Metallic", "Pad7Halo",
            "Pad8Sweep", "Fx1Rain", "Fx2Soundtrack", "Fx3Crystal", "Fx4Atmosphere", "Fx5Brightness", "Fx6Goblins", "Fx7Echoes", "Fx8SciFi",
            "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "BagPipe", "Fiddle", "Shanai", "TinkleBell", "Agogo", "SteelDrums", "Woodblock",
            "TaikoDrum", "MelodicTom", "SynthDrum", "ReverseCymbal", "GuitarFretNoise", "BreathNoise", "Seashore", "BirdTweet", "TelephoneRing",
            "Helicopter", "Applause", "Gunshot"
        };

/* ****** TODO0 patches - need to be an enum or array.
            // Fill patch list.
            for (int i = 0; i <= MidiDefs.MAX_MIDI; i++)
            {
                cmbPatchList.Items.Add(MidiDefs.GetInstrumentDef(i));
            }
            cmbPatchList.SelectedIndex = 0;


        void Patch_Click(object sender, EventArgs e)
        {
            bool valid = int.TryParse(txtPatchChannel.Text, out int pch);
            if (valid && pch >= 1 && pch <= MidiDefs.NUM_CHANNELS)
            {
                PatchChangeEvent evt = new PatchChangeEvent(0, pch, cmbPatchList.SelectedIndex);
                MidiSend(evt);

                // Update UI.
                _playChannels[pch - 1].Patch = cmbPatchList.SelectedIndex;
                InitChannelsGrid();
            }
            else
            {
                //txtPatchChannel.Text = "";
                LogMessage("ERROR", "Invalid patch channel");
            }
        }


        void Patterns_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetPatternEvents(lbPatterns.SelectedItem.ToString());
            InitChannelsGrid();

            // Might need to update the patches.
            foreach (var ch in _mfile.Channels)
            {
                if (ch.Value != -1)
                {
                    PatchChangeEvent evt = new PatchChangeEvent(0, ch.Key, ch.Value);
                    MidiSend(evt);
                }
            }

            Rewind();
            Play();
        }
**** */

}
