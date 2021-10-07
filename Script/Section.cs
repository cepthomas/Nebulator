using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;


namespace Nebulator.Script
{
    /// <summary>
    /// One section definition.
    /// </summary>
    public class Section : IEnumerable
    {
        #region Properties
        /// <summary>List of sequences in this section.</summary>
        public SectionElements Elements { get; set; } = new SectionElements();

        /// <summary>Length in beats.</summary>
        public int Beats { get; set; } = 0;

        /// <summary>Readable.</summary>
        public string Name { get; set; } = "";
        #endregion

        /// <summary>
        /// Enumerator for section.
        /// </summary>
        /// <returns>Tuple of (channel, sequence, beat)</returns>
        public IEnumerator GetEnumerator()
        {
            // Flatten section into sequences.
            foreach (SectionElement sect in Elements)
            {
                int seqBeat = 0;
                Sequence? seqnull = null; // default means ignore

                bool ok = sect.Sequences is not null && sect.Sequences.Length > 0;

                if(sect.Sequences is not null)
                {
                    switch(sect.Mode)
                    {
                        case SequenceMode.Once:
                            foreach (Sequence seq in sect.Sequences)
                            {
                                yield return (sect.Channel, seq, seqBeat);
                                seqBeat += seq.Beats;
                            }
                            break;

                        case SequenceMode.Loop:
                            while (seqBeat < Beats)
                            {
                                yield return (sect.Channel, sect.Sequences[0], seqBeat);
                                seqBeat += sect.Sequences[0].Beats;
                            }
                            break;
                    }
                }

                if (!ok)
                {
                    yield return (sect.Channel, seqnull, seqBeat);
                }
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"Section: Beats:{Beats} Name:{Name} Elements:{Elements.Count}";
        }
    }

    /// <summary>
    /// Specialized container. Has Add() to support initialization.
    /// </summary>
    public class SectionElements : List<SectionElement>
    {
        /// <summary>
        /// Add 0-N elements.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="seqMode">One of enum SequenceMode</param>
        /// <param name="sequences"></param>
        public void Add(string channel, SequenceMode seqMode, params Sequence[] sequences)
        {
            SectionElement sel = new()
            {
                Channel = channel,
                Mode = seqMode,
                Sequences = sequences
            };

            this.Add(sel);
        }
    }

    /// <summary>
    /// Sequence(s) to play.
    /// </summary>
    public class SectionElement
    {
        #region Properties
        /// <summary>Associated channel.</summary>
        public string Channel { get; set; } = Definitions.UNKNOWN_STRING;
        //public Channel Channel { get; set; } = null;

        /// <summary>How to process it.</summary>
        public SequenceMode Mode { get; set; } = SequenceMode.Once;

        /// <summary>Associated sequences.</summary>
        public Sequence[] Sequences { get; set; } = Array.Empty<Sequence>();
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"SectionElement: Channel:{Channel}";
        }
    }
}
