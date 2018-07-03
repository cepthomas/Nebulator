using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using NLog;
using Nebulator.Common;
using Nebulator.Protocol;


namespace Nebulator.Script
{
    /// <summary>
    /// Things shared between host and script at runtime.
    /// </summary>
    public class RuntimeContext
    {
        /// <summary>Main -> Script</summary>
        public Time StepTime { get; set; } = new Time();

        /// <summary>Main -> Script</summary>
        public bool Playing { get; set; } = false;

        /// <summary>Main -> Script</summary>
        public float RealTime { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public float Speed { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public int Volume { get; set; } = 0;

        /// <summary>Main -> Script -> Main</summary>
        public int FrameRate { get; set; } = 0;

        /// <summary>Steps added by script functions at runtime e.g. playSequence(). Script -> Main</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();
    }
}
