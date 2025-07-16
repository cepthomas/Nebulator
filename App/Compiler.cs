using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
using Ephemera.NScript;


namespace Nebulator.App
{
    /// <summary>One channel definition.</summary>
    public record ChannelSpec(string ChannelName, string DeviceId, int ChannelNumber, int Patch, bool IsDrums);

    /// <summary>Nebulator compiler.</summary>
    public class Compiler : CompilerCore
    {
        #region Properties
        /// <summary>Channel info collected from the script.</summary>
        public List<ChannelSpec> ChannelSpecs { get; init; } = [];
        #endregion

        /// <summary>Called before compiler starts.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PreCompile()
        {
            ChannelSpecs.Clear();

            // Our references.
            SystemDlls =
            [
                "System",
                "System.Private.CoreLib",
                "System.Runtime",
                "System.IO",
                "System.Collections",
                "System.Linq"
            ];

            LocalDlls =
            [
                "NAudio",
                "Ephemera.NBagOfTricks",
                "Ephemera.NebOsc",
                "Ephemera.MidiLib",
                "Nebulator.Script"
            ];

            Usings =
            [
                "System.Collections.Generic",
                "System.Diagnostics",
                "System.Text",
                "static Ephemera.NBagOfTricks.MusicDefinitions"
            ];
        }

        /// <summary>Called after compiler finished.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
            // Check for our app-specific directives.
            Directives.Where(d => d.dirname == "channel").ForEach(cdir =>
            {
                // Channel spec - grab it.
                try
                {
                    var parts = cdir.dirval.SplitByTokens(" ");
                    // #:channel keys  midiout 1  AcousticGrandPiano

                    // Is patch an instrument or drum?
                    bool isDrums = false;
                    int patch = MidiDefs.GetInstrumentNumber(parts[3]);
                    if (patch == -1)
                    {
                        patch = MidiDefs.GetDrumKitNumber(parts[3]);
                        isDrums = patch != -1;
                    }
                    if (patch == -1)
                    {
                        throw new ArgumentException("");
                    }
                    ChannelSpec ch = new(parts[0], parts[1], int.Parse(parts[2]), patch, isDrums);
                    ChannelSpecs.Add(ch);
                }
                catch (Exception)
                {
                    AddReport(ReportType.Syntax, ReportLevel.Error, $"Bad channel directive: {cdir.dirval}"); // TODO retrieve file/line?
                    throw new ScriptException(); // fatal
                }
            });
        }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <see cref="CompilerCore"/>
        protected override bool PreprocessLine(string sline, int lineNum, ScriptFile pcont)
        {
            bool handled = false;

            return handled;
        }
    }
}
