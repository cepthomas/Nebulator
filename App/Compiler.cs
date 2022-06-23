using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NBagOfTricks;
using NBagOfTricks.ScriptCompiler;
using MidiLib;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator.App
{
    public class Compiler : ScriptCompilerCore
    {
        #region Properties
        /// <summary>Channel info collected from the script.</summary>
        public List<Channel_XXX> Channels_XXX { get; set; } = new();
        #endregion

        #region Fields
        /// <summary>Main source file name.</summary>
        readonly string _nebfn = "";

        /// <summary>Code lines that define channels.</summary>
        readonly List<string> _channelDescriptors = new();

        /// <summary>Current hash for lines of interest.</summary>
        int _chHash = 0;
        #endregion

        /// <inheritdoc />
        public override void PreExecute()
        {
            Channels_XXX.Clear();

            LocalDlls = new()
            {
                "NAudio", "NBagOfTricks", "NebOsc", "Nebulator.Common", "Nebulator.Script"
            };

            Usings.AddRange(new List<string>()
            {
                "static Nebulator.Script.ScriptUtils", "static Nebulator.Common.InstrumentDef",
                "static Nebulator.Common.DrumDef", "static Nebulator.Common.ControllerDef"
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
                // Build new channels.
                foreach (string sch in _channelDescriptors)
                {
                    try
                    {
                        var parts = sch.SplitByTokens("(),;");

                        Channel ch = new()
                        {
                            ChannelName = parts[1].Replace("\"", ""),
                            ChannelNumber = int.Parse(parts[3]),
                            Patch = MidiDefs.GetInstrumentNumber(parts[4]),
                            VolumeWobbleRange = double.Parse(parts[5])
                        };

                        Channel_XXX ch_XXX = new()
                        {
                            DeviceName = parts[2],
                            Channel = ch
                        };

                        Channels_XXX.Add(ch_XXX);
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
            }
        }

        ///// <inheritdoc />
        //public override bool PreprocessFile(string sline, FileContext pcont)
        //{
        //    bool handled = false;

        //    if (sline.StartsWith("Channel"))
        //    {
        //       // Exclude from output file.
        //       _channelDescriptors.Add(sline);
        //       handled = true;
        //    }

        //    return handled;
        //}
    }
}
