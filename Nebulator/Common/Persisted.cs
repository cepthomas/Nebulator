using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


namespace Nebulator.Common
{
    /// <summary>
    /// General purpose storage for things that change at runtime which we want to save and restore.
    /// I don't much like the idea of using a shadow file but it seems expedient for now.
    /// </summary>
    [Serializable]
    public class Persisted
    {
        /// <summary>The project file name.</summary> 
        string _fn = Globals.UNKNOWN_STRING;

        /// <summary>Master speed in Ticks per minute - roughly equivalent to BPM.</summary>
        public double Speed { get; set; } = 80.0;

        /// <summary>Master volume.</summary>
        public int Volume { get; set; } = 100;

        /// <summary>Misc dynamic values that we want to persist. Things like volumes that are not defined in a .neb file.</summary>
        public Dictionary<string, int> Values { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <returns>value</returns>
        public int GetValue(string owner, string valname)
        {
            string key = MakeKey(owner, valname);
            if (!Values.ContainsKey(key))
            {
                int defval = 0; // def value for new - a bit crude...
                switch (valname)
                {
                    case "speed": defval = 1000; break;
                    case "volume": defval = 90; break;
                }
                Values.Add(key, defval);
            }
            return Values[key];
        }

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <param name="value"></param>
        public void SetValue(string owner, string valname, int value)
        {
            string key = MakeKey(owner, valname);
            // Add if missing.
            if (!Values.ContainsKey(key))
            {
                Values.Add(key, value);
            }

            // Mark dirty if changed.
            if (Values[key] != value)
            {
                Values[key] = value;
            }
        }

        /// <summary>
        /// Common key maker.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <returns></returns>
        string MakeKey(string owner, string valname)
        {
            return owner + "." + valname;
        }

        #region Persistence
        /// <summary>
        /// Save to json file.
        /// </summary>
        public void Save()
        {
            if (_fn != Globals.UNKNOWN_STRING)
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_fn, json);
            }
        }

        /// <summary>
        /// Create from json file.
        /// </summary>
        public static Persisted Load(string fn)
        {
            Persisted pers;

            try
            {
                string json = File.ReadAllText(fn);
                pers = JsonConvert.DeserializeObject<Persisted>(json);
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                pers = new Persisted();
            }

            pers._fn = fn;
            return pers;
        }
        #endregion
    }
}
