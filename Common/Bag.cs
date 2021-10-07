using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Nebulator.Common
{
    /// <summary>
    /// General purpose storage for things using a two part key.
    /// </summary>
    [Serializable]
    public class Bag
    {
        /// <summary>The file name.</summary> 
        [JsonIgnore]
        string FileName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Misc dynamic values that we want to persist. Needs to be public so serializer can see it.</summary>
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        /// <summary>Is this bag ok?</summary>
        [JsonIgnore]
        public bool Valid { get; set; } = false;

        /// <summary>
        /// Reset everything.
        /// </summary>
        public void Clear()
        {
            Values.Clear();
        }

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <param name="defval"></param>
        /// <returns>The value or default if not in the collection.</returns>
        public double GetDouble(string owner, string valname, double defval)
        {
            double ret = double.NaN;

            string key = MakeKey(owner, valname);
            if (Values.ContainsKey(key))
            {
                var val = (JsonElement)Values[key];
                if (val.ValueKind == JsonValueKind.Number)
                {
                    ret = val.GetDouble();
                }
            }
            else
            {
                ret = defval;
            }

            return ret;
        }

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <param name="defval"></param>
        /// <returns>The value or default if not in the collection.</returns>
        public int GetInteger(string owner, string valname, int defval)
        {
            int ret = int.MinValue;

            string key = MakeKey(owner, valname);
            if (Values.ContainsKey(key))
            {
                var val = (JsonElement)Values[key];
                if (val.ValueKind == JsonValueKind.Number)
                {
                    var d = val.GetDouble();
                    ret = (int)d;
                }
            }
            else
            {
                ret = defval;
            }

            return ret;
        }

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <param name="defval"></param>
        /// <returns>The value or default if not in the collection.</returns>
        public string GetString(string owner, string valname, string defval)
        {
            string ret;

            string key = MakeKey(owner, valname);
            if (Values.ContainsKey(key))
            {
                var val = (JsonElement)Values[key];
                if (val.ValueKind == JsonValueKind.String)
                {
                    ret = val.GetString()!;

                }
                else
                {
                    ret = val.ToString()!;
                }
            }
            else
            {
                ret = defval;
            }

            return ret;
        }

        /// <summary>
        /// Lazy helper.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="valname"></param>
        /// <param name="value"></param>
        public void SetValue(string owner, string valname, object value)
        {
            string key = MakeKey(owner, valname);
            Values[key] = value;
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
            if (Valid && FileName != Definitions.UNKNOWN_STRING)
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(FileName, json);
            }
        }

        /// <summary>
        /// Create from json file.
        /// </summary>
        public static Bag Load(string fn)
        {
            Bag bag = new();

            if(File.Exists(fn))
            {
                JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
                string json = File.ReadAllText(fn);
                var jpc = JsonSerializer.Deserialize<Bag>(json, opts);

                if (jpc is not null)
                {
                    bag = jpc;
                    bag.FileName = fn;
                    bag.Valid = true;
                }
            }
            else
            {
                // Doesn't exist, create a new one.
                bag = new();
                bag.FileName = fn;
                bag.Valid = true;
            }

            return bag;
        }
        #endregion
    }
}
