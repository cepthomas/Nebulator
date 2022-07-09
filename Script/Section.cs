using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;


namespace Nebulator.Script
{
    /// <summary>
    /// One section definition.
    /// </summary>
    public class Section
    {
        #region Properties
        /// <summary>Collection of sequences in this section.</summary>
        public SectionElements Elements { get; set; } = new SectionElements();

        /// <summary>Length in beats.</summary>
        public int Beats { get; set; } = 0;

        /// <summary>Readable.</summary>
        public string Name { get; set; } = "";
        #endregion

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
        /// <param name="sequences"></param>
        public void Add(string chname, params Sequence[] sequences)
        {
            SectionElement sel = new()
            {
                ChannelName = chname,
                Sequences = sequences
            };

            Add(sel);
        }
    }

    /// <summary>
    /// Sequence(s) to play.
    /// </summary>
    public class SectionElement
    {
        #region Properties
        /// <summary>Associated channel.</summary>
        public string ChannelName { get; set; } = "???";

        /// <summary>Associated sequences.</summary>
        public Sequence[] Sequences { get; set; } = Array.Empty<Sequence>();
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"SectionElement: ChannelName:{ChannelName}";
        }
    }
}
