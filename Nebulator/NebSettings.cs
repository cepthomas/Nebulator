using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Newtonsoft.Json;
using Nebulator.Common;


namespace Nebulator
{
    [Serializable]
    public class NebSettings
    {
        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo VirtualKeyboardInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000 };

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static NebSettings TheSettings { get; set; } = new NebSettings();

        /// <summary>Default constructor.</summary>
        public NebSettings()
        {
        }

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static void Load(string appDir)
        {
            TheSettings = null;
            string fn = Path.Combine(appDir, "neb.json");

            try
            {
                string json = File.ReadAllText(fn);
                TheSettings = JsonConvert.DeserializeObject<NebSettings>(json);
                TheSettings._fn = fn;
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                TheSettings = new NebSettings
                {
                    _fn = fn
                };
            }
        }
        #endregion
    }
}
