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



namespace Nebulator.Synth
{
    public partial class Visualizer : Form //TODOX2 move into common
    {
        public enum ChartTypes { Line, Scatter, ScatterLine };

        public class Series
        {
            public string Name { get; set; } = "???"; // TODOX2 DrawText(string text, float x, float y, SKPaint paint);
            public Color Color { get; set; } = Color.Black;
            public List<(float, float)> Points { get; set; } = new List<(float, float)>();
        }

        #region Stuff client fills in
        public List<Series> AllSeries { get; set; } = new List<Series>();
        public ChartTypes ChartType { get; set; } = ChartTypes.Scatter;
        public float XMin { get; set; } = -100;
        public float XMax { get; set; } = 100;
        public float YMin { get; set; } = -100;
        public float YMax { get; set; } = 100;
        public float DotSize { get; set; } = 3;
        public float LineSize { get; set; } = 1;
        #endregion

        /// <summary>Current pen to draw with.</summary>
        SKPaint _pen = new SKPaint()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            IsStroke = true,
            StrokeWidth = 2,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current brush to draw with.</summary>
        SKPaint _fill = new SKPaint()
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Fill,
            IsStroke = false,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Visualizer()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();
        }

        void Visualizer_Load(object sender, EventArgs e)
        {
        }

        void skControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();

            switch(ChartType)
            {
                case ChartTypes.Scatter:
                    DrawScatter(canvas);
                    break;

                case ChartTypes.Line:
                    DrawLines(canvas);
                    break;

                case ChartTypes.ScatterLine:
                    DrawLines(canvas);
                    DrawScatter(canvas);
                    break;
            }
        }

        void DrawScatter(SKCanvas canvas)
        {
            foreach (Series ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = DotSize;

                foreach ((float, float) pt in ser.Points)
                {
                    float x = Map(pt.Item1, XMin, XMax, 0, Width);
                    float y = Map(pt.Item2, YMin, YMax, 0, Height);

                    canvas.DrawPoint(x, y, _pen);
                }
            }
        }

        void DrawLines(SKCanvas canvas)
        {
            foreach (Series ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = LineSize;

                SKPoint[] points = new SKPoint[ser.Points.Count];
                for (int i = 0; i < ser.Points.Count; i++)
                {
                    float x = Map(ser.Points[i].Item1, XMin, XMax, 0, Width);
                    float y = Map(ser.Points[i].Item2, YMin, YMax, 0, Height);
                    points[i] = new SKPoint(x, y);
                }

                SKPath path = new SKPath();
                path.AddPoly(points);

                canvas.DrawPath(path, _pen);
            }
        }

        float Map(float val, float start1, float stop1, float start2, float stop2)
        {
            return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        }
    }
}
