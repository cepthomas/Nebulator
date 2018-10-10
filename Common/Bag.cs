using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;



namespace Nebulator.Common
{
    /// <summary>
    /// General purpose storage for things using a two part key.
    /// </summary>
    [Serializable]
    public class Bag
    {
        /// <summary>The file name.</summary> 
        string _fn = Utils.UNKNOWN_STRING;

        /// <summary>Misc dynamic values that we want to persist. Needs to be public so serializer can see it.</summary>
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

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
        /// <returns>The value or null if not in the collection.</returns>
        public object GetValue(string owner, string valname) //TODO handle null???
        {
            string key = MakeKey(owner, valname);
            return Values.ContainsKey(key) ? Values[key] : null;
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
            if (_fn != Utils.UNKNOWN_STRING)
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_fn, json);
            }
        }

        /// <summary>
        /// Create from json file.
        /// </summary>
        public static Bag Load(string fn)
        {
            Bag bag;

            try
            {
                string json = File.ReadAllText(fn);
                bag = JsonConvert.DeserializeObject<Bag>(json);
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                bag = new Bag();
            }

            bag._fn = fn;
            return bag;
        }
        #endregion
    }
}
