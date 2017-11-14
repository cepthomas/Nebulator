using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NAudio.Midi;
using NLog;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>Reads in and processes standard yahama style files.</summary>
    public class StyleParser
    {
        #region Properties gleaned from the style file
        /// <summary>All the part names.</summary>
        public List<string> Parts { get { return _events.Keys.Select(e => e.part).Distinct().ToList(); } }

        /// <summary>All the channel numbers.</summary>
        public List<int> Channels { get { return _events.Keys.Select(e => e.channel).Distinct().ToList(); } }

        /// <summary>Resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;

        /// <summary>Tempo, if supplied by file.</summary>
        public double Tempo { get; private set; } = 0.0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; private set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; private set; } = "";
        #endregion

        #region Fields
        /// <summary>All the midi events by part/channel groups. This is the verbatim content of the file with no processing.</summary>
        Dictionary<(string part, int channel), List<MidiEvent>> _events = new Dictionary<(string, int), List<MidiEvent>>();

        /// <summary>Name of current part being processed.</summary>
        string _currentPart = Globals.UNKNOWN_STRING;
        #endregion

        #region Public methods
        /// <summary>
        /// Read a style file. For style parsing, only a minimal set is included. You can add the rest.
        /// See http://www.wierzba.homepage.t-online.de/StyleFileDescription_v21.pdf.
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
                    switch (Encoding.UTF8.GetString(br.ReadBytes(4)))
                    {
                        case "MThd":
                            ReadMidiSection(br);

                            // Put all in abs time order.
                            foreach(var evts in _events.Values)
                            {
                                evts.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
                            }
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
        /// <param name="part"></param>
        /// <param name="channel"></param>
        /// <returns>The collection or null if invalid.</returns>
        public IList<MidiEvent> GetEvents(string part, int channel)
        {
            _events.TryGetValue((part, channel), out List<MidiEvent> ret);
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
            List<string> leftovers = new List<string>();
            int absoluteTime = 0;

            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            int fileFormat = Utils.SwapUInt16(br.ReadUInt16());
            int tracks = Utils.SwapUInt16(br.ReadUInt16());
            DeltaTicksPerQuarterNote = Utils.SwapUInt16(br.ReadUInt16());

            // Style midi section is always type 0 - only one track.
            if (fileFormat != 0 || tracks != 1)
            {
                throw new FormatException("Invalid file format for style");
            }

            string chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
            if (chunkHeader != "MTrk")
            {
                throw new FormatException("Invalid chunk header");
            }

            chunkSize = Utils.SwapUInt32(br.ReadUInt32());
            long startPos = br.BaseStream.Position;

            // Read all midi events.
            MidiEvent me = null; // current
            while (br.BaseStream.Position < startPos + chunkSize)
            {
                me = MidiEvent.ReadNextEvent(br, me);
                absoluteTime += me.DeltaTime;
                me.AbsoluteTime = absoluteTime;

                switch (me.CommandCode)
                {
                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.Marker:
                        {
                            // Indicates start of a new midi part. Bin per channel.
                            _currentPart = (me as TextEvent).Text;
                            absoluteTime = 0;
                            Console.WriteLine($">>>> part:{_currentPart}");
                        }
                        break;

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
                            leftovers.Add(evt.ToString());
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.SequenceTrackName:
                        {
                            // Indicates start of a new midi track.
                            leftovers.Add(me.ToString());
                        }
                        break;

                    case MidiCommandCode.MetaEvent when (me as MetaEvent).MetaEventType == MetaEventType.EndTrack:
                        {
                            // Indicates end of current midi track.
                            leftovers.Add(me.ToString());
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
                        leftovers.Add(me.ToString());
                        break;
                }
            }

            if (br.BaseStream.Position != startPos + chunkSize)
            {
                throw new Exception($"Read count error: {chunkSize}+{startPos}!={br.BaseStream.Position}");
            }
        }

        /// <summary>
        /// Read the CASM section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCASMSection(BinaryReader br)
        {
            // The information in the CASM section is necessary if the midi section does not follow the rules
            // for “simple” style files, which do not necessarily need a CASM section (see chapter 5.2.1 for
            // the rules). The CASM section gives instructions to the instrument on how to deal with the midi data.
            // This includes:
            // - Assigning the sixteen possible midi channels to 8 accompaniment channels which are
            //   available to a style in the instrument (9 = sub rhythm, 10 = rhythm, 11 = bass, 12 = chord 1,
            //   13 = chord 2, 14 = pad, 15 = phrase 1, 16 = phrase 2). More than one midi channel
            //   may be assigned to an accompaniment channel.
            // - Allowing the PSR to edit the source channel in StyleCreator. This setting is overridden by
            //   the instrument if the style has > 1 midi source channel assigned to an accompaniment
            //   channel. In this case the source channels are not editable.
            // - Muting/enabling specific notes or chords to trigger the accompaniment. In practice, only
            //   chord choices are used.
            // - The key that is used in the midi channel. Styles often use different keys for the midi data.
            //   Styles without a CASM must be in the key of CMaj7.
            // - How the chords and notes are transposed as chords are changed and how notes held
            //   through chord changes are reproduced.
            // - The range of notes generated by the style.
            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());
        }

        /// <summary>
        /// Read the CSEG section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCSEGSection(BinaryReader br)
        {
            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());
        }

        /// <summary>
        /// Read the Sdec section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadSdecSection(BinaryReader br)
        {
            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());
            // swallow for now
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Ctab section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCtabSection(BinaryReader br)
        {
            // Has some key and chord info.
            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());
            // swallow for now
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Cntt section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCnttSection(BinaryReader br)
        {
            uint chunkSize = Utils.SwapUInt32(br.ReadUInt32());
            // swallow for now
            br.ReadBytes((int)chunkSize);
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Helper function.
        /// </summary>
        /// <param name="evt"></param>
        void AddMidiEvent(MidiEvent evt)
        {
            if (evt is NoteEvent)
            {
                NoteEvent nevt = evt as NoteEvent;

                //if(nevt.Channel == 3) // && nevt.NoteNumber == 72)
                //{
                //    Console.WriteLine(evt.ToString());
                //}
            }

            if (!_events.ContainsKey((_currentPart, evt.Channel)))
            {
                _events.Add((_currentPart, evt.Channel), new List<MidiEvent>());
            }

            _events[(_currentPart, evt.Channel)].Add(evt);
        }
        #endregion
    }
}
