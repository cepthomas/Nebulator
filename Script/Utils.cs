using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.MidiLib;
using Ephemera.NBagOfTricks;
using Ephemera.MusicLib;


namespace Nebulator.Script
{
    public class Utils
    {
        /// <summary>
        /// Gets note number for music or drum names.
        /// </summary>
        /// <param name="snotes"></param>
        /// <returns></returns>
        public static List<int> ParseNotes(string snotes)
        {
            List<int> notes = MusicDefs.GetNotesFromString(snotes);
            if (!notes.Any())
            {
                // It might be a drum.
                int id = MidiDefs.Drums.GetId(snotes);
                if (id >= 0)
                {
                    notes.Add(id);
                }
                else
                {
                    // Not a drum either - error!
                    throw new InvalidOperationException($"Invalid notes [{snotes}]");
                }
            }

            return notes;
        }
    }
}
