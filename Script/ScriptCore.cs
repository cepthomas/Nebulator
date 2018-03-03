﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using NLog;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.Dynamic;

// Internal stuff not associated with Processing or Nebulator APIs.

namespace Nebulator.Script
{
    /// <summary>Stuff shared between Main and Script on a per step basis.</summary>
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

    public partial class ScriptCore
    {
        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Properties
        /// <summary>Set of things shared between host and script at runtime.</summary>
        public RuntimeContext Context { get; set; } = new RuntimeContext();
        #endregion

        #region Events
        /// <summary>
        /// Script functions that are called from the main nebulator. They are identified by name/key.
        /// Typically they are controller input handlers such that the key is the name of the input.
        /// </summary>
        protected Dictionary<string, ScriptFunction> _scriptFunctions = new Dictionary<string, ScriptFunction>();
        public delegate void ScriptFunction();
        #endregion

        #region Public functions
        /// <summary>
        /// Execute a script function. Minimal error checking, presumably the compiler did that.
        /// Caller will have to deal with any runtime exceptions.
        /// </summary>
        /// <param name="funcName"></param>
        public void ExecScriptFunction(string funcName)
        {
            if (_scriptFunctions.ContainsKey(funcName))
            {
                _scriptFunctions[funcName].Invoke();
            }
        }

        /// <summary>
        /// Generate steps from sequence notes.
        /// </summary>
        /// <param name="track">Which track to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startTick">Which tick to start at.</param>
        public static StepCollection ConvertToSteps(Track track, Sequence seq, int startTick)
        {
            StepCollection steps = new StepCollection();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                int toffset = startTick == -1 ? 0 : track.NextTime();

                Time startNoteTime = new Time(startTick, toffset) + seqel.When;
                Time stopNoteTime = startNoteTime + seqel.Duration;

                if (seqel.Function != "")
                {
                    StepInternal step = new StepInternal()
                    {
                        TrackName = track.Name,
                        Channel = track.Channel,
                        Function = seqel.Function
                    };
                    steps.AddStep(startNoteTime, step);
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        int vel = track.NextVol(seqel.Volume);
                        StepNoteOn step = new StepNoteOn()
                        {
                            TrackName = track.Name,
                            Channel = track.Channel,
                            NoteNumber = noteNum,
                            NoteNumberToPlay = noteNum,
                            Velocity = vel,
                            VelocityToPlay = vel,
                            Duration = seqel.Duration
                        };
                        steps.AddStep(startNoteTime, step);

                        //// Maybe add a deferred stop note.
                        //if (stopNoteTime != startNoteTime)
                        //{
                        //    steps.AddStep(stopNoteTime, new StepNoteOff(step));
                        //}
                        //// else client is taking care of it.
                    }
                }
            }

            return steps;
        }
        #endregion

        #region Private functions
        /// <summary>Handle unimplemented script elements that we can safely ignore. But do tell the user.</summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        void NotImpl(string name, string desc = "")
        {
            _logger.Warn($"{name} not implemented. {desc}");
        }

        /// <summary>Bounds check a color definition.
        Color SafeColor(int r, int g, int b, int a)
        {
            r = constrain(r, 0, 255);
            g = constrain(g, 0, 255);
            b = constrain(b, 0, 255);
            a = constrain(a, 0, 255);
            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
}