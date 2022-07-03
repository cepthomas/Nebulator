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
using Nebulator.Script;


namespace Nebulator.App
{

    public record ChannelSpec(string ChannelName, string DeviceId, int ChannelNumber, int Patch);



    public class Compiler : ScriptCompilerCore
    {
        #region Properties
        /// <summary>Channel info collected from the script.</summary>
        public List<ChannelSpec> ChannelSpecs { get; init; } = new();
        #endregion

        #region Fields
        ///// <summary>Main source file name.</summary>
        //readonly string _nebfn = "";

        ///// <summary>Code lines that define channels.</summary>
        //readonly List<string> _channelDescriptors = new();

        ///// <summary>Current hash for lines of interest.</summary>
        //int _chHash = 0;
        #endregion

        /// <summary>Called before compiler starts.</summary>
        public override void PreCompile()
        {
            ChannelSpecs.Clear();

            LocalDlls = new() { "NAudio", "NBagOfTricks", "NebOsc", "MidiLib", "Nebulator.Script" };

            Usings.Add("static NBagOfTricks.MusicDefinitions");

            //// Save hash of current channel descriptors to detect change in source code.
            //_chHash = string.Join("", _channelDescriptors).GetHashCode();
            //_channelDescriptors.Clear();
        }

        ///// <summary>Called after compiler finished.</summary>
        //public override void PostCompile()
        //{
        //    // Check for changed channel descriptors.
        //    if (string.Join("", _channelDescriptors).GetHashCode() != _chHash)
        //    {
        //        // Build new channels.
        //        foreach (string sch in _channelDescriptors)
        //        {
        //            try
        //            {
        //                var parts = sch.SplitByTokens("(),;");

        //                Channel ch = new()
        //                {
        //                    ChannelName = parts[1].Replace("\"", ""),
        //                    DeviceId = parts[2].Replace("\"", ""),
        //                    ChannelNumber = int.Parse(parts[3]),
        //                    Patch = MidiDefs.GetInstrumentOrDrumKitNumber(parts[4].Replace("\"", ""))
        //                };

        //                Channels.Add(ch);
        //            }
        //            catch (Exception)
        //            {
        //                Results.Add(new()
        //                {
        //                    ResultType = CompileResultType.Error,
        //                    Message = $"Bad statement:{sch}",
        //                    SourceFile = _nebfn,
        //                    LineNumber = -1
        //                });
        //            }
        //        }
        //    }
        //}

        /// <summary>Called for each line in the source file before compiling.</summary>
        public override bool PreprocessLine(string sline, FileContext pcont)
        {
            bool handled = false;

            // Channel spec - grab it.
            if (sline.StartsWith("Channel"))
            {
                try
                {
                    var parts = sline.Replace("\"", "").SplitByTokens("(),;");
                    ChannelSpec ch = new(parts[1], parts[2], int.Parse(parts[3]), MidiDefs.GetInstrumentOrDrumKitNumber(parts[4]));
                    ChannelSpecs.Add(ch);
                }
                catch (Exception)
                {
                    Results.Add(new()
                    {
                        ResultType = CompileResultType.Error,
                        Message = $"Bad statement:{sline}",
                        SourceFile = pcont.SourceFile,
                        LineNumber = pcont.LineNumber
                    });
                }

                // Exclude from output file.
                handled = true;
            }

            return handled;
        }
    }
}
