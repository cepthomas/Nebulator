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
    /// <summary>Nebulator compiler.</summary>
    public class Compiler : CompilerCore
    {
        /// <summary>Called before compiler starts.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PreCompile()
        {
            // Our references.
            SystemDlls =
            [
                "System",
                "System.Private.CoreLib",
                "System.Runtime",
                "System.Collections",
                "System.Drawing"
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
                "System.Text",
                "static Ephemera.NBagOfTricks.MusicDefinitions"
            ];
        }

        /// <summary>Called after compiler finished.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
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
