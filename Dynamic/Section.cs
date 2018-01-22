using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// One top level section.
    /// </summary>
    public class Section
    {
        #region Properties
        /// <summary>The name for this section.</summary>
        public string Name { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Start Tick.</summary>
        public int Start { get; set; } = 0;

        /// <summary>Length in Ticks.</summary>
        public int Length { get; set; } = 0;

        /// <summary>Contained track info.</summary>
        public List<SectionTrack> SectionTracks { get; set; } = new List<SectionTrack>();
        #endregion
    }

    /// <summary>
    /// One track/row in the Section.
    /// </summary>
    public class SectionTrack
    {
        #region Properties
        /// <summary>The name for the associated track.</summary>
        public string TrackName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The names for the associated Sequences.</summary>
        public List<string> SequenceNames { get; set; } = new List<string>();
        #endregion
    }
}
