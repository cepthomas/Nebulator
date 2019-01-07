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


namespace Nebulator.Controls
{
    /// <summary>
    /// A simple display of numerical series. Probably will grow with some fancy features later.
    /// </summary>
    public partial class Visualizer : UserControl
    {
        public enum ChartTypes { Line, Scatter, ScatterLine };

        /// <summary>Auto assigned colors.</summary>
        List<Color> _colors = new List<Color>() { Color.Firebrick, Color.CornflowerBlue, Color.MediumSeaGreen, Color.MediumOrchid, Color.DarkOrange, Color.DarkGoldenrod, Color.DarkSlateGray, Color.Khaki, Color.PaleVioletRed };
        // Or these sets from http://colorbrewer2.org qualitative:
        // Dark: Color.FromArgb(27, 158, 119), Color.FromArgb(217, 95, 2), Color.FromArgb(117, 112, 179), Color.FromArgb(231, 41, 138), Color.FromArgb(102, 166, 30), Color.FromArgb(230, 171, 2), Color.FromArgb(166, 118, 29), Color.FromArgb(102, 102, 102) <summary>Color definitions.</summary>
        // Light: Color.FromArgb(141, 211, 199), Color.FromArgb(255, 255, 179), Color.FromArgb(190, 186, 218), Color.FromArgb(251, 128, 114), Color.FromArgb(128, 177, 211), Color.FromArgb(253, 180, 98), Color.FromArgb(179, 222, 105), Color.FromArgb(252, 205, 229)
        static int _currentColor = 0;

        public class Series
        {
            public string Name { get; set; } = "No Name";
            public Color Color { get; set; } = Color.Empty;
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

        /// <summary>Current font to draw with.</summary>
        SKPaint _text = new SKPaint()
        {
            TextSize = 14,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Left,
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

        void skControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();
            Init();
            DrawText(canvas);

            switch (ChartType)
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

        /// <summary>Do some fixups maybe.</summary>
        void Init()
        {
            foreach (Series ser in AllSeries)
            {
                if(ser.Color == Color.Empty)
                {
                    ser.Color = _colors[_currentColor++ % _colors.Count];
                }
            }
        }

        void DrawText(SKCanvas canvas)
        {
            float xpos = 10;
            float ypos = 20;
            float yinc = 15;

            _text.Color = SKColors.Black;
            canvas.DrawText($"X range: {XMin} {XMax}", xpos, ypos, _text);
            ypos += yinc;
            canvas.DrawText($"Y range: {YMin} {YMax}", xpos, ypos, _text);
            ypos += yinc;

            ypos += yinc;

            foreach (Series ser in AllSeries)
            {
                _text.Color = ser.Color.ToSKColor();
                canvas.DrawText(ser.Name, xpos, ypos, _text);
                ypos += yinc;
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
                path.AddPoly(points, false);

                canvas.DrawPath(path, _pen);
            }
        }

        float Map(float val, float start1, float stop1, float start2, float stop2)
        {
            return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        }
    }
}
