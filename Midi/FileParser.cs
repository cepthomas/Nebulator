using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>Reads in and processes standard midi files.</summary>
    public class FileParser
    {
        #region Properties gleaned from the file
        /// <summary>Channel info: key is number, value is name.</summary>
        public Dictionary<int, string> Channels { get; private set; } = new Dictionary<int, string>();

        /// <summary>Resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;

        /// <summary>Tempo, if supplied by file.</summary>
        public double Tempo { get; private set; } = 0.0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; private set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; private set; } = "";

        /// <summary>Bits of unimportant (maybe) info in the file.</summary>
        public List<string> Leftovers { get; private set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>All the midi events by part/channel groups. This is the verbatim content of the file with no processing.</summary>
        Dictionary<int, List<MidiEvent>> _events = new Dictionary<int, List<MidiEvent>>();
        #endregion

        #region Public methods
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fileName"></param>
        public void ProcessFile(string fileName)
        {
            // Init everything.
            _events.Clear();
            DeltaTicksPerQuarterNote = 0;
            Tempo = 0.0;
            TimeSig = "";
            KeySig = "";

            using (var br = new BinaryReader(File.OpenRead(fileName)))
            {
                bool done = false;

                while (!done)
                {
                    var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));
                   // Debug.WriteLine(">>>sectionName " + sectionName);

                    switch (sectionName)
                    {
                        case "MThd":
                            ReadMidiSection(br);
                            break;

                        case "MTrk":
                            ReadMTrk(br);
                            break;

                        case "CASM":
                            ReadCASMSection(br);
                            break;

                        case "CSEG":
                            ReadCSEGSection(br);
                            break;

                        case "Sdec":
                            ReadSdecSection(br);
                            break;

                        case "Ctab":
                            ReadCtabSection(br);
                            break;

                        case "Cntt":
                            ReadCnttSection(br);
                            break;

                        default:
                            // ignore the rest.
                            done = true;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Helper to get an event collection.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>The collection or null if invalid.</returns>
        public IList<MidiEvent> GetEvents(int channel)
        {
            _events.TryGetValue(channel, out List<MidiEvent> ret);
            return ret;
        }
        #endregion

        #region Section parsers
        /// <summary>
        /// Read the midi section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadMidiSection(BinaryReader br)
        {
            uint chunkSize = Utils.FixEndian(br.ReadUInt32());

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            int fileFormat = Utils.FixEndian(br.ReadUInt16());
            // Style midi section is always type 0 - only one track.
            //if (fileFormat != 0 || tracks != 1)
            //{
            //    throw new FormatException("Invalid file format for style");
            //}
            int tracks = Utils.FixEndian(br.ReadUInt16());
            DeltaTicksPerQuarterNote = Utils.FixEndian(br.ReadUInt16());
        }

        /// <summary>
        /// Read a midi track chunk.
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        int ReadMTrk(BinaryReader br)
        {
            // Defaults.
            int chnum = 0;
            string chname = "???";

            uint chunkSize = Utils.FixEndian(br.ReadUInt32());
            long startPos = br.BaseStream.Position;
            int absoluteTime = 0;

            // Read all midi events. https://www.csie.ntu.edu.tw/~r92092/ref/midi/
            MidiEvent me = null; // current
            while (br.BaseStream.Position < startPos + chunkSize)
            {
                me = MidiEvent.ReadNextEvent(br, me);
                absoluteTime += me.DeltaTime;
                me.AbsoluteTime = absoluteTime;

                switch (me.CommandCode)
                {
                    case MidiCommandCode.NoteOn:
                        {
                            NoteOnEvent evt = me as NoteOnEvent;
                            AddMidiEvent(evt);
                        }
                        break;

                    case MidiCommandCode.NoteOff:
                        {
                            NoteEvent evt = me as NoteEvent;
                            AddMidiEvent(evt);
                        }
                        break;

                    case MidiCommandCode.ControlChange:
                        {
                            ControlChangeEvent evt = me as ControlChangeEvent;
                            AddMidiEvent(evt);
                        }
                        break;

                    case MidiCommandCode.PitchWheelChange:
                        {
                            PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                            AddMidiEvent(evt);
                        }
                        break;

                    case MidiCommandCode.PatchChange:
                        {
                            PatchChangeEvent evt = me as PatchChangeEvent;
                            chname = PatchChangeEvent.GetPatchName(evt.Patch);
                            AddMidiEvent(evt);
                        }
                        break;

                    case MidiCommandCode.Sysex:
                        {
                            SysexEvent evt = me as SysexEvent;
                            string s = evt.ToString().Replace(Environment.NewLine, " ");
                            Leftovers.Add($"Sysex:{s}");
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.TrackSequenceNumber:
                        {
                            TrackSequenceNumberEvent evt = me as TrackSequenceNumberEvent;
                            chnum = evt.Channel;
                            Leftovers.Add($"TrackSequenceNumber:{evt.Channel}");
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.SequenceTrackName:
                        {
                            TextEvent evt = me as TextEvent;
                            chname = evt.Text;
                            Leftovers.Add($"SequenceTrackName:{evt.Text} Channel:{evt.Channel}");
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.Marker:
                        {
                            // Indicates start of a new midi part. Bin per channel.
                            //_currentPart = (me as TextEvent).Text;
                            Leftovers.Add($"Marker:{me}");
                            absoluteTime = 0;
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.EndTrack:
                        {
                            // Indicates end of current midi track.
                            Leftovers.Add($"EndTrack:{me}");
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.SetTempo:
                        {
                            TempoEvent evt = me as TempoEvent;
                            Tempo = evt.Tempo;
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.TimeSignature:
                        {
                            TimeSignatureEvent evt = me as TimeSignatureEvent;
                            TimeSig = evt.TimeSignature;
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.KeySignature:
                        {
                            KeySignatureEvent evt = me as KeySignatureEvent;
                            KeySig = evt.ToString();
                        }
                        break;

                    default:
                        Leftovers.Add($"Other:{me}");
                        break;
                }
            }

            // Local function.
            void AddMidiEvent(MidiEvent evt)
            {
                if (!_events.ContainsKey(evt.Channel))
                {
                    _events.Add(evt.Channel, new List<MidiEvent>());
                }

                if (!Channels.ContainsKey(evt.Channel))
                {
                    Channels.Add(evt.Channel, evt.Channel == 10 ? "Drums" : chname);
                }

                _events[evt.Channel].Add(evt);
            }

            return absoluteTime;
        }
        #endregion
    }
}
