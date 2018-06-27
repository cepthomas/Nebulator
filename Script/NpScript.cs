using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using MoreLinq;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Nebulator.Common;


// Processing emulation script stuff.
// The properties and functions are organized similarly to the API specified in https://processing.org/reference/.


namespace Nebulator.Script
{
    public partial class ScriptCore
    {
        #region Fields
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Current font to draw.</summary>
        SKPaint _textPaint = new SKPaint()
        {
            TextSize = 12,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Left,
            IsAntialias = true
        };

        /// <summary>Current pen to draw.</summary>
        SKPaint _pen = new SKPaint()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            IsStroke = true,
            StrokeWidth = 1,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current brush to draw.</summary>
        SKPaint _fill = new SKPaint()
        {
            Color = SKColors.Transparent,
            Style = SKPaintStyle.Fill,
            IsStroke = false,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current drawing points.</summary>
        List<SKPoint> _vertexes = new List<SKPoint>();

        /// <summary>General purpose stack</summary>
        Stack<SKMatrix> _matrixStack = new Stack<SKMatrix>();

        /// <summary>Background color.</summary>
        SKColor _bgColor = SKColors.LightGray;

        /// <summary>Smoothing option.</summary>
        bool _smooth = true;
        #endregion

        #region Properties - Nebulator not processing!

        /// <summary>Loop option.</summary>
        public bool Loop { get; private set; } = true;

        /// <summary>Redraw option.</summary>
        public bool Redraw { get; internal set; } = false;

        /// <summary>Current working object to draw on.</summary>
        public SKCanvas Canvas { get; internal set; } = null;
        #endregion

        #region Definitions - same values as Processing
        //---- Math
        public const float QUARTER_PI = (float)(Math.PI / 4.0);
        public const float HALF_PI = (float)(Math.PI / 2.0);
        public const float PI = (float)(Math.PI);
        public const float TWO_PI = (float)(Math.PI * 2.0);
        public const float TAU = (float)(Math.PI * 2.0);
        //---- Mouse buttons, keyboard arrows
        public const int LEFT = 37;
        public const int UP = 38;
        public const int RIGHT = 39;
        public const int DOWN = 40;
        public const int CENTER = 3;
        //---- Keys
        public const int BACKSPACE = 8;
        public const int TAB = 9;
        public const int ENTER = 6;
        public const int RETURN = 13;
        public const int ESC = 27;
        public const int DELETE = 127;
        //---- keyCodes
        public const int CODED = 0xFF;
        public const int ALT = 8;
        public const int CTRL = 2;
        public const int SHIFT = 1;
        //---- Arc styles
        public const int OPEN = 1;
        public const int CHORD = 2;
        public const int PIE = 3;
        //---- Alignment
        public const int BASELINE = 0;
        public const int TOP = 101;
        public const int BOTTOM = 102;
        //---- Drawing defs
        public const int CORNER = 0;
        public const int CORNERS = 1;
        public const int RADIUS = 2;
        public const int SQUARE = 1;
        public const int ROUND = 2;
        public const int PROJECT = 4;
        public const int MITER = 8;
        public const int BEVEL = 32;
        //---- Color mode
        public const int RGB = 1;
        //public const int ARGB = 2;
        public const int HSB = 3;
        //public const int ALPHA = 4;
        //---- Cursor types
        public const int ARROW = 0;
        public const int CROSS = 1;
        public const int TEXT = 2;
        public const int WAIT = 3;
        public const int HAND = 12;
        public const int MOVE = 13;
        //---- Misc items
        public const int CLOSE = 2;
        #endregion

        #region Structure
        //---- Function overrides
        public virtual void setup() { }
        public virtual void draw() { }

        //---- Script functions
        protected void exit() { NotImpl(nameof(exit), "This is probably not what you want to do."); exit(); }
        protected void loop() { Loop = true; }
        protected void noLoop() { Loop = false; }
        //protected void popStyle() { NotImpl(nameof(popStyle)); }
        //protected void pushStyle() { NotImpl(nameof(pushStyle)); }
        protected void redraw() { Redraw = true; }
        //protected void thread() { NotImpl(nameof(thread)); }
        #endregion

        #region Environment 
        //---- Script properties
        //public void cursor(int which) { NotImpl(nameof(cursor)); }
        //public void cursor(PImage image) { NotImpl(nameof(cursor)); }
        //public void cursor(PImage image, int x, int y) { NotImpl(nameof(cursor)); }
        //public void delay(int msec) { NotImpl(nameof(delay)); }
        //public int displayDensity() { NotImpl(nameof(displayDensity)); }
        public bool focused { get; internal set; }
        public int frameCount { get; internal set; } = 1;
        public int frameRate() { return RuntimeContext.FrameRate; }
        public void frameRate(int num) { RuntimeContext.FrameRate = num; }
        public void fullScreen() { NotImpl(nameof(fullScreen), "Size is set by main form."); }
        public int height { get; internal set; }
        //public void noCursor() { NotImpl(nameof(noCursor)); }
        public void noSmooth() { _smooth = false; }
        public void pixelDensity(int density) { NotImpl(nameof(pixelDensity)); }
        public int pixelHeight { get { NotImpl(nameof(pixelHeight), "Assume 1."); return 1; } }
        public int pixelWidth { get { NotImpl(nameof(pixelWidth), "Assume 1."); return 1; } }
        public void size(int w, int h) { NotImpl(nameof(size), "Size is set by main form."); }
        public void smooth() { _smooth = true; }
        public void smooth(int level) { _smooth = level > 0; }
        public int width { get; internal set; }
        #endregion

        #region Data
        //public string binary(object value) { NotImpl(nameof(binary)); }
        //public bool boolean(object value) { NotImpl(nameof(boolean)); }
        //public byte @byte (object value) { NotImpl(nameof(@byte)); }
        //public char @char (object value) { NotImpl(nameof(@char)); }
        //public float @float(object value) { NotImpl(nameof(@float)); }
        //public string hex(object value) { NotImpl(nameof(hex)); }
        public int @int(float val) { return (int)val; }
        public int @int(string val) { return int.Parse(val); }
        public string str(object value) { return value.ToString(); }
        //public int unbinary(string value) { NotImpl(nameof(unbinary)); }
        //public int unhex(string value) { NotImpl(nameof(unhex)); }

        #region Data - String Functions
        public string join(string[] list, char separator) { return string.Join(separator.ToString(), list); }
        //public string match() { NotImpl(nameof(match)); }
        //public string matchAll() { NotImpl(nameof(matchAll)); }
        //public string nf() { NotImpl(nameof(nf)); }
        //public string nfc() { NotImpl(nameof(nfc)); }
        //public string nfp() { NotImpl(nameof(nfp)); }
        //public string nfs() { NotImpl(nameof(nfs)); }
        public string[] split(string value, char delim) { return value.SplitByToken(delim.ToString()).ToArray(); }
        public string[] split(string value, string delim) { return value.SplitByToken(delim).ToArray(); }
        public string[] splitTokens(string value, string delim) { return value.SplitByTokens(delim).ToArray(); }
        public string trim(string str) { return str.Trim(); }
        public string[] trim(string[] array) { return array.Select(i => i.Trim()).ToArray(); }
        #endregion

        #region Data - Array Functions
        //public void append() { NotImpl(nameof(append)); }
        //public void arrayCopy() { NotImpl(nameof(arrayCopy)); }
        //public void concat() { NotImpl(nameof(concat)); }
        //public void expand() { NotImpl(nameof(expand)); }
        //public void reverse() { NotImpl(nameof(reverse)); }
        //public void shorten() { NotImpl(nameof(shorten)); }
        //public void sort() { NotImpl(nameof(sort)); }
        //public void splice() { NotImpl(nameof(splice)); }
        //public void subset() { NotImpl(nameof(subset)); }
        #endregion
        #endregion

        #region Shape 
        //public void createShape() { NotImpl(nameof(createShape)); }
        //public void loadShape() { NotImpl(nameof(loadShape)); }

        #region Shape - 2D Primitives
        public void arc(float x1, float y1, float x2, float y2, float angle1, float angle2, int style)
        {
            x1 -= width / 2;
            y1 -= height / 2;
            angle1 = Utils.RadiansToDegrees(angle1);
            angle2 = Utils.RadiansToDegrees(angle2);
            SKPath path = new SKPath();
            SKRect rect = new SKRect(x1, y1, x2, y2);
            path.AddArc(rect, angle1, angle2);

            //https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/arcs

            switch (style)
            {
                case OPEN: // default is OPEN stroke with a PIE fill.
                    if(_fill.Color != SKColors.Transparent)
                    {
                        Canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        Canvas.DrawPath(path, _pen);
                    }
                    break;

                case CHORD:
                    path.Close();
                    if (_fill.Color != SKColors.Transparent)
                    {
                        Canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        Canvas.DrawPath(path, _pen);
                    }
                    break;

                case PIE:
                    path.MoveTo(rect.MidX, rect.MidY);
                    if (_fill.Color != SKColors.Transparent)
                    {
                        Canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        Canvas.DrawPath(path, _pen);
                    }
                    break;
            }
        }

        public void arc(float x1, float y1, float x2, float y2, float angle1, float angle2)
        {
            arc(x1, y1, x2, y2, angle1, angle2 - angle1, OPEN);
        }

        public void ellipse(float x1, float y1, float w, float h)
        {
            if (_fill.Color != SKColors.Transparent)
            {
                Canvas.DrawOval(x1, y1, w / 2, h / 2, _fill);
            }

            if(_pen.StrokeWidth != 0)
            {
                Canvas.DrawOval(x1, y1, w / 2, h / 2, _pen);
            }
        }

        public void line(float x1, float y1, float x2, float y2)
        {
            Canvas.DrawLine(x1, y1, x2, y2, _pen);
        }

        public void point(float x, float y)
        {
            Canvas.DrawPoint(x, y, _pen);
        }

        public void quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            SKPoint[] points = new SKPoint[4] { new SKPoint(x1, y1), new SKPoint(x2, y2), new SKPoint(x3, y3), new SKPoint(x4, y4) };

            if (_fill.Color != SKColors.Transparent)
            {
                Canvas.DrawPoints(SKPointMode.Polygon, points, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                Canvas.DrawPoints(SKPointMode.Polygon, points, _pen);
            }
        }

        public void rect(float x1, float y1, float w, float h)
        {
            if (_fill.Color != SKColors.Transparent)
            {
                Canvas.DrawRect(x1, y1, w, h, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                Canvas.DrawRect(x1, y1, w, h, _pen);
            }
        }

        public void triangle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            SKPoint[] points = new SKPoint[3] { new SKPoint(x1, y1), new SKPoint(x2, y2), new SKPoint(x3, y3) };

            if (_fill.Color != SKColors.Transparent)
            {
                Canvas.DrawPoints(SKPointMode.Lines, points, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                Canvas.DrawPoints(SKPointMode.Lines, points, _pen);
            }
        }
        #endregion

        #region Shape - Curves
        public void bezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            // Draw path with cubic Bezier curve.
            using (SKPath path = new SKPath())
            {
                path.MoveTo(x1, y1);
                path.CubicTo(x2, y2, x3, y3, x4, y4);
                Canvas.DrawPath(path, _pen);
            }
        }

        public void curve(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) { NotImpl(nameof(curve)); }
        //{
        //    //Draws a curved line on the screen. The first and second parameters specify the beginning control point and the last two 
        //    //parameters specify the ending control point. The middle parameters specify the start and stop of the curve. 
        //    //Longer curves can be created by putting a series of curve() functions together or using curveVertex().
        //    //An additional function called curveTightness() provides control for the visual quality of the curve. 
        //    //The curve() function is an implementation of Catmull-Rom splines.
        //    //The GDI function: Draws a cardinal spline through a specified array of Point structures.

        //    //_gr.DrawCurve(_pen, new SKPoint[4] { new SKPoint(x1, y1), new SKPoint(x2, y2), new SKPoint(x3, y3), new SKPoint(x4, y4) }, 1, 1, 0.5f);
        //}

        //public void curveDetail() { NotImpl(nameof(curveDetail)); }
        //public void curvePoint() { NotImpl(nameof(curvePoint)); }
        //public void curveTangent() { NotImpl(nameof(curveTangent)); }
        //public void curveTightness() { NotImpl(nameof(curveTightness)); }
        #endregion

        #region Shape - 3D Primitives
        //public void box() { NotImpl(nameof(box)); }
        //public void sphere() { NotImpl(nameof(sphere)); }
        //public void sphereDetail() { NotImpl(nameof(sphereDetail)); }
        #endregion

        #region Shape - Attributes
        public void ellipseMode(int mode) { NotImpl(nameof(ellipseMode), "Assume CORNER mode."); }
        public void rectMode(int mode) { NotImpl(nameof(rectMode), "Assume CORNER mode."); }

        public void strokeCap(int style)
        {
            switch (style)
            {
                case PROJECT: _pen.StrokeCap = SKStrokeCap.Square; break;
                case ROUND:   _pen.StrokeCap = SKStrokeCap.Round; break;
                case SQUARE:  _pen.StrokeCap = SKStrokeCap.Butt; break;
            }
        }

        public void strokeJoin(int style)
        {
            switch (style)
            {
                case BEVEL:  _pen.StrokeJoin = SKStrokeJoin.Bevel; break;
                case MITER:  _pen.StrokeJoin = SKStrokeJoin.Miter; break;
                case ROUND:  _pen.StrokeJoin = SKStrokeJoin.Round; break;
            }
        }

        public void strokeWeight(int w) { _pen.StrokeWidth = w; }
        #endregion

        #region Shape - Vertex
        //public void beginContour() { NotImpl(nameof(beginContour)); }
        public void beginShape() { _vertexes.Clear(); }
        //public void beginShape(int kind) { NotImpl(nameof(beginShape)); } // POINTS, LINES, TRIANGLES, TRIANGLE_FAN, TRIANGLE_STRIP, QUADS, and QUAD_STRIP
        //public void bezierVertex() { NotImpl(nameof(bezierVertex)); }
        //public void curveVertex() { NotImpl(nameof(curveVertex)); }
        //public void endContour() { NotImpl(nameof(endContour)); }
        public void endShape(int mode = -1)
        {
            SKPoint[] points = _vertexes.ToArray();

            if (mode == -1) // Not closed - draw lines.
            {
                Canvas.DrawPoints(SKPointMode.Lines, points, _pen);
            }
            else if (mode == CLOSE)
            {
                using (var path = new SKPath())
                {
                    for (int i = 0; i < _vertexes.Count; i++)
                    {
                        if(i == 0)
                        {
                            path.MoveTo(_vertexes[i]);
                        }
                        else
                        {
                            path.LineTo(_vertexes[i]);
                        }
                    }
                    path.Close();

                    if (_fill.Color != SKColors.Transparent)
                    {
                        Canvas.DrawPath(path, _fill);
                    }

                    if (_pen.StrokeWidth != 0)
                    {
                        Canvas.DrawPath(path, _pen);
                    }
                }
            }
            else
            {
                NotImpl(nameof(endShape));
            }
        }

        //public void quadraticVertex() { NotImpl(nameof(quadraticVertex)); }
        public void vertex(float x, float y) { _vertexes.Add(new SKPoint(x, y)); } // Just x/y.
        #endregion

        #region Shape - Loading & Displaying
        //public void shape() { NotImpl(nameof(shape)); }
        //public void shapeMode() { NotImpl(nameof(shapeMode)); }
        #endregion
        #endregion

        #region Input
        #region Input - Mouse
        //---- Script properties
        public bool mouseIsPressed { get; internal set; } = false;
        public int mouseButton { get; internal set; } = LEFT;
        public int mouseWheelValue { get; internal set; } = 0;
        public int mouseX { get; internal set; } = 0;
        public int mouseY { get; internal set; } = 0;
        public int pMouseX { get; internal set; } = 0;
        public int pMouseY { get; internal set; } = 0;
        //---- Function overrides
        public virtual void mouseClicked() { }
        public virtual void mouseDragged() { }
        public virtual void mouseMoved() { }
        public virtual void mousePressed() { }
        public virtual void mouseReleased() { }
        public virtual void mouseWheel() { }
        #endregion

        #region Input - Keyboard
        //---- Script properties
        public char key { get; internal set; } = ' ';
        public int keyCode { get; internal set; } = 0;
        public bool keyIsPressed { get; internal set; } = false;
        //---- Function overrides
        public virtual void keyPressed() { }
        public virtual void keyReleased() { }
        public virtual void keyTyped() { }
        #endregion

        #region Input - Files
        //public void createInput() { NotImpl(nameof(createInput)); }
        //public void createReader() { NotImpl(nameof(createReader)); }
        //public void launch() { NotImpl(nameof(launch)); }
        //public void loadBytes() { NotImpl(nameof(loadBytes)); }
        //public void loadJSONArray() { NotImpl(nameof(loadJSONArray)); }
        //public void loadJSONObject() { NotImpl(nameof(loadJSONObject)); }
        public string[] loadStrings(string filename) { return File.ReadAllLines(filename); }
        //public void loadTable() { NotImpl(nameof(loadTable)); }
        //public void loadXML() { NotImpl(nameof(loadXML)); }
        //public void parseJSONArray() { NotImpl(nameof(parseJSONArray)); }
        //public void parseJSONObject() { NotImpl(nameof(parseJSONObject)); }
        //public void parseXML() { NotImpl(nameof(parseXML)); }
        //public void selectFolder() { NotImpl(nameof(selectFolder)); }
        //public void selectInput() { NotImpl(nameof(selectInput)); }
        #endregion

        #region Input - Time & Date
        public int day() { return DateTime.Now.Day; }
        public int hour() { return DateTime.Now.Hour; }
        public int minute() { return DateTime.Now.Minute; }
        public int millis() { return (int)(RuntimeContext.RealTime * 1000); }
        public int month() { return DateTime.Now.Month; }
        public int second() { return DateTime.Now.Second; }
        public int year() { return DateTime.Now.Year; }
        #endregion
        #endregion

        #region Output
        #region Output - Text Area
        //public void println(params object[] vars) { NotImpl(nameof(print), "Use print()."); }
        public void print(params object[] vars)
        {
            _logger.Info(string.Join(" ", vars));
        }
        public void printArray(Array what)
        {
            for (int i = 0; i < what.Length; i++)
            {
                _logger.Info($"array[{i}]: {what.GetValue(i)}");
            }
        }
        #endregion

        #region Output - Image
        //public void save(string fn) { NotImpl(nameof(save)); }
        //public void saveFrame() { NotImpl(nameof(saveFrame)); }
        //public void saveFrame(string fn) { NotImpl(nameof(saveFrame)); }
        #endregion

        #region Output - Files
        //public void beginRaw() { NotImpl(nameof(beginRaw)); }
        //public void beginRecord() { NotImpl(nameof(beginRecord)); }
        //public void createOutput() { NotImpl(nameof(createOutput)); }
        //public void createWriter() { NotImpl(nameof(createWriter)); }
        //public void endRaw() { NotImpl(nameof(endRaw)); }
        //public void endRecord() { NotImpl(nameof(endRecord)); }
        #endregion

        #region Output - PrintWriter
        //public void saveBytes() { NotImpl(nameof(saveBytes)); }
        //public void saveJSONArray() { NotImpl(nameof(saveJSONArray)); }
        //public void saveJSONObject() { NotImpl(nameof(saveJSONObject)); }
        //public void saveStream() { NotImpl(nameof(saveStream)); }
        //public void saveStrings() { NotImpl(nameof(saveStrings)); }
        //public void saveTable() { NotImpl(nameof(saveTable)); }
        //public void saveXML() { NotImpl(nameof(saveXML)); }
        //public void selectOutput() { NotImpl(nameof(selectOutput)); }
        #endregion
        #endregion

        #region Transform 
        //public void applyMatrix() { NotImpl(nameof(applyMatrix)); }
        public void popMatrix() { Canvas.SetMatrix(_matrixStack.Pop()); }
        //public void printMatrix() { NotImpl(nameof(printMatrix)); }
        public void pushMatrix() { _matrixStack.Push(Canvas.TotalMatrix); }
        //public void resetMatrix() { NotImpl(nameof(resetMatrix)); }
        public void rotate(float angle) { Canvas.RotateRadians(angle); }
        //public void rotateX() { NotImpl(nameof(rotateX)); }
        //public void rotateY() { NotImpl(nameof(rotateY)); }
        //public void rotateZ() { NotImpl(nameof(rotateZ)); }
        public void scale(float sc) { Canvas.Scale(sc); }
        public void scale(float scx, float scy) { Canvas.Scale(scx, scy); }
        //public void shearX() { NotImpl(nameof(shearX)); }
        //public void shearY() { NotImpl(nameof(shearY)); }
        public void translate(float dx, float dy) { Canvas.Translate(dx, dy); }
        #endregion

        #region Lights & Camera
        #region Lights & Camera - Lights
        //public string ambientLight() { NotImpl(nameof(ambientLight)); }
        //public string directionalLight() { NotImpl(nameof(directionalLight)); }
        //public string lightFalloff() { NotImpl(nameof(lightFalloff)); }
        //public string lights() { NotImpl(nameof(lights)); }
        //public string lightSpecular() { NotImpl(nameof(lightSpecular)); }
        //public string noLights() { NotImpl(nameof(noLights)); }
        //public string normal() { NotImpl(nameof(normal)); }
        //public string pointLight() { NotImpl(nameof(pointLight)); }
        //public string spotLight() { NotImpl(nameof(spotLight)); }
        #endregion

        #region Lights & Camera - Camera
        //public string beginCamera() { NotImpl(nameof(beginCamera)); }
        //public string camera() { NotImpl(nameof(camera)); }
        //public string endCamera() { NotImpl(nameof(endCamera)); }
        //public string frustum() { NotImpl(nameof(frustum)); }
        //public string ortho() { NotImpl(nameof(ortho)); }
        //public string perspective() { NotImpl(nameof(perspective)); }
        //public string printCamera() { NotImpl(nameof(printCamera)); }
        //public string printProjection() { NotImpl(nameof(printProjection)); }
        #endregion

        #region Lights & Camera - Coordinates
        //public string modelX() { NotImpl(nameof(modelX)); }
        //public string modelY() { NotImpl(nameof(modelY)); }
        //public string modelZ() { NotImpl(nameof(modelZ)); }
        //public string screenX() { NotImpl(nameof(screenX)); }
        //public string screenY() { NotImpl(nameof(screenY)); }
        //public string screenZ() { NotImpl(nameof(screenZ)); }
        #endregion

        #region Lights & Camera - Material Properties
        //public string ambient() { NotImpl(nameof(ambient)); }
        //public string emissive() { NotImpl(nameof(emissive)); }
        //public string shininess() { NotImpl(nameof(shininess)); }
        //public string specular() { NotImpl(nameof(specular)); }
        #endregion
        #endregion

        #region Color
        #region Color - Setting
        //public void background(int rgb) { NotImpl(nameof(background)); }
        //public void background(int rgb, float alpha) { NotImpl(nameof(background)); }
        public void background(float gray) { _bgColor = SafeColor(gray, gray, gray, 255); if(Canvas != null) Canvas.Clear(_bgColor); }
        public void background(float gray, float alpha) { _bgColor = SafeColor(gray, gray, gray, alpha); if (Canvas != null) Canvas.Clear(_bgColor); }
        public void background(float v1, float v2, float v3) { color c = new color(v1, v2, v3, 255); _bgColor = c.NativeColor; if (Canvas != null) Canvas.Clear(_bgColor); }
        public void background(float v1, float v2, float v3, float alpha) { color c = new color(v1, v2, v3, alpha); _bgColor = c.NativeColor; if (Canvas != null) Canvas.Clear(_bgColor); }
        public void background(PImage img) { if (Canvas != null) Canvas.DrawBitmap(img.bmp, new SKRect(0, 0, width, height)); }
        public void colorMode(int mode, float max = 255) { Script.color.SetMode(mode, max, max, max, max); }
        public void colorMode(int mode, int max1, int max2, int max3, int maxA = 255) { Script.color.SetMode(mode, max1, max2, max3, maxA); }
        //public void fill(int rgb) { NotImpl(nameof(fill)); }
        //public void fill(int rgb, float alpha) { NotImpl(nameof(fill)); }
        public void fill(color clr) { _fill.Color = SafeColor(clr.R, clr.G, clr.B, clr.A); }
        public void fill(float gray) { _fill.Color = SafeColor(gray, gray, gray, 255); }
        public void fill(float gray, float alpha) { _fill.Color = SafeColor(gray, gray, gray, alpha); }
        public void fill(float v1, float v2, float v3) { color c = new color(v1, v2, v3, 255); _fill.Color = c.NativeColor; }
        public void fill(float v1, float v2, float v3, float alpha) { color c = new color(v1, v2, v3, alpha); _fill.Color = c.NativeColor; }
        public void noFill() { _fill.Color = SKColors.Transparent; }
        public void noStroke() { _pen.StrokeWidth = 0; }
        //public void stroke(int rgb) { NotImpl(nameof(stroke)); }
        //public void stroke(int rgb, float alpha) { NotImpl(nameof(stroke)); }
        public void stroke(float gray) { _pen.Color = SafeColor(gray, gray, gray, 255); }
        public void stroke(float gray, float alpha) { _pen.Color = SafeColor(gray, gray, gray, alpha); }
        public void stroke(float v1, float v2, float v3) { color c = new color(v1, v2, v3, 255); _pen.Color = c.NativeColor; }
        public void stroke(float v1, float v2, float v3, float alpha) { color c = new color(v1, v2, v3, alpha); _pen.Color = c.NativeColor; }
        #endregion

        #region Color - Creating & Reading
        public color color(float v1, float v2, float v3, float a = 255) { return new color(v1, v2, v3, a); }
        public color color(float gray, float a = 255) { return new color(gray, a); }
        public float alpha(color color) { return color.A; }
        public float blue(color color) { return color.B; }
        public float brightness(color color) { return color.Brightness; }
        public float green(color color) { return color.G; }
        public float hue(color color) { return color.Hue; }
        public float red(color color) { return color.R; }
        public float saturation(color color) { return color.Saturation; }

        public color lerpColor(color c1, color c2, float amt)
        {
            amt = constrain(amt, 0, 1);
            float r = lerp(c1.R, c2.R, amt);
            float b = lerp(c1.B, c2.B, amt);
            float g = lerp(c1.G, c2.G, amt);
            float a = lerp(c1.A, c2.A, amt);
            return new color(r, g, b, a);
        }
        #endregion
        #endregion

        #region Image 
        //public PImage createImage(int w, int h, int format) { NotImpl(nameof(PImage)); }

        #region Image - Loading & Displaying
        public void image(PImage img, float x, float y)
        {
            Canvas.DrawBitmap(img.bmp, x, y); // unscaled
        }

        public void image(PImage img, float x1, float y1, float x2, float y2)
        {
            Canvas.DrawBitmap(img.bmp, new SKRect(x1, y1, x2, y2)); // scaled
        }

        public void imageMode(int mode) { NotImpl(nameof(imageMode), "Assume CORNER mode."); }

        public PImage loadImage(string filename)
        {
            return new PImage(filename);
        }

        //public void noTint() { NotImpl(nameof(noTint)); }
        //public void requestImage() { NotImpl(nameof(requestImage)); }
        //public void tint() { NotImpl(nameof(tint)); }
        #endregion

        #region Image - Textures
        //public void texture() { NotImpl(nameof(texture)); }
        //public void textureMode() { NotImpl(nameof(textureMode)); }
        //public void textureWrap() { NotImpl(nameof(textureWrap)); }
        #endregion

        #region Image - Pixels
        // Even though you may have drawn a shape with colorMode(HSB), the numbers returned will be in RGB format.
        // pixels[]
        //public void blend() { NotImpl(nameof(blend)); }
        //public void copy() { NotImpl(nameof(copy)); }
        //public void filter() { NotImpl(nameof(filter)); }
        //public PImage get(int x, int y, int w, int h) { NotImpl(nameof(get)); }
        //public color get(int x, int y) { NotImpl(nameof(get)); }
        //public void loadPixels() { NotImpl(nameof(loadPixels)); }
        //public void set(int x, int y, color pcolor) { NotImpl(nameof(set)); }
        //public void set(int x, int y, PImage src) { NotImpl(nameof(set)); }
        //public void updatePixels() { NotImpl(nameof(updatePixels)); }
        #endregion
        #endregion

        #region Rendering 
        //public void blendMode() { NotImpl(nameof(blendMode)); }
        //public void clip() { NotImpl(nameof(clip)); }
        //public void createGraphics() { NotImpl(nameof(createGraphics)); }
        //public void noClip() { NotImpl(nameof(noClip)); }

        #region Rendering - Shaders
        //public void loadShader() { NotImpl(nameof(loadShader)); }
        //public void resetShader() { NotImpl(nameof(resetShader)); }
        //public void shader() { NotImpl(nameof(shader)); }
        #endregion
        #endregion

        #region Typography
        #region Typography - Loading & Displaying
        public PFont createFont(string name, int size)
        {
            return new PFont(name, size);
        }

        //public PFont loadFont() { NotImpl(nameof(loadFont)); }

        public void text(string s, float x, float y)
        {
            Canvas.DrawText(s, x, y, _textPaint);
        }

        public void textFont(PFont font)
        {
            _textPaint.TextSize = font.size;
            _textPaint.Typeface = SKTypeface.FromFamilyName(font.name);
        }
        #endregion

        #region Typography - Attributes
        public void textAlign(int alignX)
        {
            switch(alignX)
            {
                case LEFT: _textPaint.TextAlign = SKTextAlign.Left; break;
                case CENTER: _textPaint.TextAlign = SKTextAlign.Center; break;
                case RIGHT: _textPaint.TextAlign = SKTextAlign.Right; break;
            }
            NotImpl(nameof(textAlign));
        }
        public void textAlign(int alignX, int alignY) { NotImpl(nameof(textAlign)); }
        //public void textLeading(int leading) { NotImpl(nameof(textLeading)); }
        //public void textMode() { NotImpl(nameof(textMode)); }
        public void textSize(int pts) { _textPaint.TextSize = pts; }
        float textWidth(string s) { return _textPaint.MeasureText(s); }
        float textWidth(char ch) { return textWidth(ch.ToString()); }
        #endregion

        #region Typography - Metrics
        //public int textAscent() { return (int)Math.Round(_font.FontFamily.GetCellAscent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        //public int textDescent() { return (int)Math.Round(_font.FontFamily.GetCellDescent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        #endregion
        #endregion

        #region Math
        #region Math - Calculation
        public int abs(int val) { return Math.Abs(val); }
        public float abs(float val) { return Math.Abs(val); }
        public int ceil(float val) { return (int)Math.Ceiling(val); }
        public float constrain(float val, float min, float max) { return Utils.Constrain(val, min, max); }
        public int constrain(int val, int min, int max) { return Utils.Constrain(val, min, max); }
        public float dist(float x1, float y1, float x2, float y2) { return (float)Math.Sqrt(sq(x1 - x2) + sq(y1 - y2)); }
        public float dist(float x1, float y1, float z1, float x2, float y2, float z2) { return (float)Math.Sqrt(sq(x1 - x2) + sq(y1 - y2) + sq(z1 - z2)); }
        public float exp(float exponent) { return (float)Math.Exp(exponent); }
        public int floor(float val) { return (int)Math.Floor(val); }
        public float lerp(float start, float stop, float amt) { return start + (stop - start) * amt; }
        public float log(float val) { return (float)Math.Log(val); }
        public float mag(float x, float y) { return (float)Math.Sqrt(sq(x) + sq(y)); }
        public float mag(float x, float y, float z) { return (float)Math.Sqrt(sq(x) + sq(y) + sq(z)); }
        public float map(float val, float start1, float stop1, float start2, float stop2) { return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1); }
        public float max(float val1, float val2) { return Math.Max(val1, val2); }
        public float max(float[] vals) { return vals.Max(); }
        public int max(int val1, int val2) { return Math.Max(val1, val2); }
        public int max(int[] vals) { return vals.Max(); }
        public float min(float val1, float val2) { return Math.Min(val1, val2); }
        public float min(float[] vals) { return vals.Min(); }
        public int min(int val1, int val2) { return Math.Min(val1, val2); }
        public int min(int[] vals) { return vals.Min(); }
        public float norm(float val, float start, float stop) { return (val - start) / (stop - start); }
        public float pow(float val, float exponent) { return (float)Math.Pow(val, exponent); }
        public int round(float val) { return (int)Math.Round(val); }
        public float sq(float val) { return (float)Math.Pow(val, 2); }
        public float sqrt(float val) { return (float)Math.Sqrt(val); }
        public int truncate(float val) { return (int)val; }
        #endregion

        #region Math - Trigonometry
        public float acos(float val) { return (float)Math.Acos(val); }
        public float asin(float val) { return (float)Math.Asin(val); }
        public float atan(float val) { return (float)Math.Atan(val); }
        public float atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public float cos(float angle) { return (float)Math.Cos(angle); }
        public float degrees(float rad) { return 360.0f * rad / (2.0f * PI); }
        public float radians(float degrees) { return 2.0f * PI * degrees / 360.0f; }
        public float sin(float angle) { return (float)Math.Sin(angle); }
        public float tan(float angle) { return (float)Math.Tan(angle); }
        #endregion

        #region Math - Random
        //public void noise() { NotImpl(nameof(noise)); }
        //public void noiseDetail() { NotImpl(nameof(noiseDetail)); }
        //public void noiseSeed() { NotImpl(nameof(noiseSeed)); }
        public float random(float max) { return (float)_rand.NextDouble() * max; }
        public float random(float min, float max) { return min + (float)_rand.NextDouble() * (max - min); }
        public int random(int max) { return _rand.Next(max); }
        public int random(int min, int max) { return _rand.Next(min, max); }
        public float randomGaussian()
        {
            double mean = 0;
            double sigma = 1;
            var u1 = _rand.NextDouble();
            var u2 = _rand.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var randNormal = mean + sigma * randStdNormal;
            return (float)randNormal;
        }
        public void randomSeed(int seed) { _rand = new Random(seed); }
        #endregion
        #endregion
    }
}
