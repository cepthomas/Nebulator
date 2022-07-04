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
    /// <summary>One channel definition.</summary>
    public record ChannelSpec(string ChannelName, string DeviceId, int ChannelNumber, int Patch, bool IsDrums);

    /// <summary>Nebulator compiler.</summary>
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

        /// <summary>Called after compiler finished.</summary>
        public override void PostCompile()
        {
        }

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
                    // Is patch an instrument or drum?
                    bool isDrums = false;
                    int patch = MidiDefs.GetInstrumentNumber(parts[4]);
                    if (patch == -1)
                    {
                        patch = MidiDefs.GetDrumKitNumber(parts[4]);
                        isDrums = patch != -1;
                    }
                    if (patch == -1)
                    {
                        throw new ArgumentException("");
                    }
                    ChannelSpec ch = new(parts[1], parts[2], int.Parse(parts[3]), patch, isDrums);
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
