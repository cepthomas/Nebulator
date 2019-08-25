using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.Script
{
    /// <summary>How to play the sequence in the section.</summary>
    public enum SequenceMode { Once, Loop }

    /// <summary>
    /// One section definition.
    /// </summary>
    public class NSection : IEnumerable // Exposes an enumerator, which supports a simple iteration over a non-generic collection.
    {
        #region Properties
        /// <summary>List of sequences in this section.</summary>
        public NSectionElements Elements { get; set; } = new NSectionElements();

        /// <summary>Length in measures.</summary>
        public int Measures { get; set; } = 0;

        /// <summary>Readable.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;
        #endregion

        /// <summary>
        /// Enumerator for section.
        /// </summary>
        /// <returns>Tuple of (channel, sequence, beat)</returns>
        public IEnumerator GetEnumerator()
        {
            // Flatten section into sequences.
            foreach (NSectionElement sect in Elements)
            {
                int seqBeat = 0;
                NSequence seqnull = null; // default means ignore

                bool ok = sect.Sequences != null && sect.Sequences.Count() > 0;

                if(ok)
                {
                    switch(sect.Mode)
                    {
                        case SequenceMode.Once:
                            foreach (NSequence seq in sect.Sequences)
                            {
                                yield return (sect.Channel, seq, seqBeat);
                                seqBeat += seq.Measures * Time.BEATS_PER_MEAS;
                            }
                            break;

                        case SequenceMode.Loop:
                            while (seqBeat < Measures * Time.BEATS_PER_MEAS)
                            {
                                yield return (sect.Channel, sect.Sequences[0], seqBeat);
                                seqBeat += sect.Sequences[0].Measures * Time.BEATS_PER_MEAS;
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
            return $"NSection: Measures:{Measures} Name:{Name} Elements:{Elements.Count}";
        }
    }

    /// <summary>
    /// Specialized container.
    /// </summary>
    public class NSectionElements : List<NSectionElement>
    {

        /// <summary>
        /// Add 0-N elements.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="seqMode">One of enum SequenceMode</param>
        /// <param name="sequences"></param>
        public void Add(NChannel channel, int seqMode, params NSequence[] sequences)
        {
            NSectionElement sel = new NSectionElement()
            {
                Channel = channel,
                Mode = (SequenceMode)seqMode,
                Sequences = sequences
            };

            this.Add(sel);
        }
    }

    /// <summary>
    /// Sequence(s) to play.
    /// </summary>
    public class NSectionElement
    {
        #region Properties
        /// <summary>Associated channel.</summary>
        public NChannel Channel { get; set; } = null;

        /// <summary>How to process it.</summary>
        public SequenceMode Mode { get; set; } = SequenceMode.Once;

        /// <summary>Associated sequences.</summary>
        public NSequence[] Sequences { get; set; } = null;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NSectionElement: Channel:{Channel.Name}";
        }
    }
}
