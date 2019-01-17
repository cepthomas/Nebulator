using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;



namespace Nebulator.Visualizer
{

    public class DataPoint
    {
        public DataSeries Owner { get; set; } = null;
        // Data points in normal x/y client coordinates.
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;

        // Where currently in the UI.
        public SKPoint ClientPoint { get; set; }
    
        public override string ToString()
        {
            return $"X:{X:0.00}  Y:{Y:0.00}{Environment.NewLine}Series:{Owner.Name}";
        }
    }

    ///<summary></summary>
    public class DataSeries
    {
        ///<summary></summary>
        public string Name { get; set; } = "No Name";

        ///<summary></summary>
        public Color Color { get; set; } = Color.Empty;

        ///<summary>Data points in normal x/y coordinates.</summary>
        public List<DataPoint> Points { get; set; } = new List<DataPoint>();

        ///<summary></summary>
        public void AddPoint(double x, double y)
        {
            Points.Add(new DataPoint() { X = x, Y = y, Owner = this });
        }
    }
}
