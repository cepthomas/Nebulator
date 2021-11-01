using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NBagOfTricks;
using NBagOfTricks.ScriptCompiler;
using Nebulator.Common;


namespace Nebulator.App
{
    public class Compiler : ScriptCompilerCore
    {
        #region Properties
        /// <summary>Current active channels.</summary>
        public List<Channel> Channels { get; set; } = new();
        #endregion

        #region Fields
        /// <summary>Main source file name.</summary>
        readonly string _nebfn = Definitions.UNKNOWN_STRING;

        /// <summary>Code lines that define channels.</summary>
        readonly List<string> _channelDescriptors = new();

        /// <summary>Current hash for lines of interest.</summary>
        int _chHash = 0;
        #endregion

        /// <inheritdoc />
        public override void PreExecute()
        {
            Channels.Clear();

            LocalDlls = new()
            {
                "NAudio", "NLog", "NBagOfTricks", "NebOsc", "Nebulator.Common", "Nebulator.Script"
            };

            Usings.AddRange(new List<string>()
            {
                "static Nebulator.Script.ScriptUtils", "static Nebulator.Common.InstrumentDef",
                "static Nebulator.Common.DrumDef", "static Nebulator.Common.ControllerDef",
                "static Nebulator.Common.SequenceMode"
            });

            // Save hash of current channel descriptors to detect change in source code.
            _chHash = string.Join("", _channelDescriptors).GetHashCode();
            _channelDescriptors.Clear();
        }

        /// <inheritdoc />
        public override void PostExecute()
        {
            // Check for changed channel descriptors.
            if (string.Join("", _channelDescriptors).GetHashCode() != _chHash)
            {
               Channels = ProcessChannelDescs();
            }

        }

        /// <inheritdoc />
        public override bool PreprocessFile(string sline, FileContext pcont)
        {
            bool handled = false;

            if (sline.StartsWith("Channel"))
            {
               // Exclude from output file.
               _channelDescriptors.Add(sline);
               handled = true;
            }

            return handled;
        }

        /// <summary>
        /// Convert channel descriptors into partial Channel objects.
        /// </summary>
        /// <returns></returns>
        List<Channel> ProcessChannelDescs()
        {
           List<Channel> channels = new();

           // Build new channels.
           foreach (string sch in _channelDescriptors)
           {
               try
               {
                   List<string> parts = sch.SplitByTokens("(),;");

                   Channel ch = new()
                   {
                       ChannelName = parts[1].Replace("\"", ""),
                       DeviceType = (DeviceType)Enum.Parse(typeof(DeviceType), parts[2]),
                       ChannelNumber = int.Parse(parts[3]),
                       Patch = (InstrumentDef)Enum.Parse(typeof(InstrumentDef), parts[4]),
                       VolumeWobbleRange = double.Parse(parts[5])
                   };

                   channels.Add(ch);
               }
               catch (Exception)
               {
                   Results.Add(new()
                   {
                       ResultType = CompileResultType.Error,
                       Message = $"Bad statement:{sch}",
                       SourceFile = _nebfn,
                       LineNumber = -1
                   });
               }
           }

           return channels;
        }
    }
}
