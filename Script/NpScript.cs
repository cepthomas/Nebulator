using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Dynamic;


// Processing API stuff.


namespace Nebulator.Script
{
    /// <summary>
    /// Processing emulation script stuff.
    /// The properties and functions are organized similarly to the API specified in https://processing.org/reference/.
    /// </summary>
    public partial class ScriptCore
    {
        #region Internal fields
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Current font to draw.</summary>
        Font _font = new Font("Arial", 12f, GraphicsUnit.Pixel);

        /// <summary>Current pen to draw.</summary>
        Pen _pen = new Pen(Color.Black, 1f) { LineJoin = LineJoin.Round, EndCap = LineCap.Round, StartCap = LineCap.Round };

        /// <summary>Current brush to draw.</summary>
        SolidBrush _brush = new SolidBrush(Color.Transparent);

        /// <summary>Current text alignment.</summary>
        int _xAlign = LEFT;

        /// <summary>Current text alignment.</summary>
        int _yAlign = BASELINE;

        /// <summary>General purpose stack</summary>
        Stack<object> _matrixStack = new Stack<object>();

        /// <summary>Background color. Internal so Surface can access.</summary>
        internal Color _bgColor = Color.LightGray;

        /// <summary>Smoothing option. Internal so Surface can access.</summary>
        internal bool _smooth = true;

        /// <summary>Loop option. Internal so Surface can access.</summary>
        internal bool _loop = true;

        /// <summary>Redraw option. Internal so Surface can access.</summary>
        internal bool _redraw = false;

        /// <summary>Current working Graphics object to draw on. Internal so Surface can access.</summary>
        internal Graphics _gr = null;
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
        protected void loop() { _loop = true; }
        protected void noLoop() { _loop = false; }
        //protected void popStyle() { NotImpl(nameof(popStyle)); }
        //protected void pushStyle() { NotImpl(nameof(pushStyle)); }
        protected void redraw() { _redraw = true; }
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
        public int frameRate() { return DynamicElements.FrameRate; }
        public void frameRate(int num) { DynamicElements.FrameRate = num; }
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
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x1, y1, x2, y2, angle1, angle2);

            switch (style)
            {
                case CHORD:
                    _gr.FillPath(_brush, path);
                    path.CloseFigure();
                    _gr.DrawPath(_pen, path);
                    break;

                case OPEN:
                    _gr.FillPath(_brush, path);
                    _gr.DrawArc(_pen, x1, y1, x2, y2, angle1, angle2);
                    break;

                case PIE:
                    _gr.FillPie(_brush, x1 - 0.75f, y1 - 0.75f, x2 + 0.25f, y2 + 0.25f, angle1, angle2);
                    _gr.DrawPie(_pen, x1, y1, x2, y2, angle1, angle2);
                    break;
            }
        }

        public void arc(float x1, float y1, float x2, float y2, float angle1, float angle2)
        {
            arc(x1, y1, x2, y2, angle1, angle2 - angle1, OPEN);
        }

        public void ellipse(float x1, float y1, float w, float h)
        {
            // Convert to GDI coords.
            x1 -= w / 2;
            y1 -= h / 2;
            _gr.FillEllipse(_brush, x1, y1, w, h);
            _gr.DrawEllipse(_pen, x1, y1, w, h);
        }

        public void line(float x1, float y1, float x2, float y2)
        {
            _gr.DrawLine(_pen, x1, y1, x2, y2);
        }

        public void point(int x, int y)
        {
            if (_pen.Width == 1)
            {
                SmoothingMode smoothingMode = _gr.SmoothingMode;
                _gr.SmoothingMode = SmoothingMode.None;
                _gr.FillRectangle(new SolidBrush(_pen.Color), x, y, 1, 1);
                _gr.SmoothingMode = smoothingMode;
            }
            else
            {
                _gr.FillEllipse(new SolidBrush(_pen.Color), x - (_pen.Width - 1) / 2f, y - (_pen.Width - 1) / 2f, _pen.Width, _pen.Width);
            }
        }

        public void quad(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            Point[] points = new Point[4] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3), new Point(x4, y4) };
            _gr.FillPolygon(_brush, points);
            _gr.DrawPolygon(_pen, points);
        }

        public void rect(float x1, float y1, float w, float h)
        {
            _gr.FillRectangle(_brush, x1, y1, w, h);
            _gr.DrawRectangle(_pen, x1, y1, w, h);
        }

        public void triangle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            PointF[] points = new PointF[3] { new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3) };
            _gr.FillPolygon(_brush, points);
            _gr.DrawPolygon(_pen, points);
        }
        #endregion

        #region Shape - Curves
        public void bezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            _gr.DrawBezier(_pen, x1, y1, x2, y2, x3, y3, x4, y4);
        }

        //public void bezierDetail() { NotImpl(nameof(bezierDetail)); }
        //public void bezierPoint() { NotImpl(nameof(bezierPoint)); }
        //public void bezierTangent() { NotImpl(nameof(bezierTangent)); }

        public void curve(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            _gr.DrawCurve(_pen, new Point[4] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3), new Point(x4, y4) }, 1, 1, 0.5f);
        }

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
                case PROJECT:
                    _pen.StartCap = LineCap.Square;
                    _pen.EndCap = LineCap.Square;
                    break;

                case ROUND:
                    _pen.StartCap = LineCap.Round;
                    _pen.EndCap = LineCap.Round;
                    break;

                case SQUARE:
                    _pen.StartCap = LineCap.Flat;
                    _pen.EndCap = LineCap.Flat;
                    break;
            }
        }

        public void strokeJoin(int style)
        {
            switch (style)
            {
                case BEVEL:
                    _pen.LineJoin = LineJoin.Bevel;
                    break;

                case MITER:
                    _pen.LineJoin = LineJoin.Miter;
                    break;

                case ROUND:
                    _pen.LineJoin = LineJoin.Round;
                    break;
            }
        }

        public void strokeWeight(int w) { _pen.Width = w; }
        #endregion


// Current drawing points.
List<Point> _vertexes = new List<Point>();



        #region Shape - Vertex
        //public void beginContour() { NotImpl(nameof(beginContour)); }
        public void beginShape() { _vertexes.Clear(); }
        //public void beginShape(int kind) { NotImpl(nameof(beginShape)); } // POINTS, LINES, TRIANGLES, TRIANGLE_FAN, TRIANGLE_STRIP, QUADS, and QUAD_STRIP
        //public void bezierVertex() { NotImpl(nameof(bezierVertex)); }
        //public void curveVertex() { NotImpl(nameof(curveVertex)); }
        //public void endContour() { NotImpl(nameof(endContour)); }
        public void endShape(int mode = -1)
        {
            if (mode == -1) // Not closed - draw lines.
            {
                _gr.DrawLines(_pen, _vertexes.ToArray());
            }
            else if (mode == CLOSE)
            {
                _gr.DrawPolygon(_pen, _vertexes.ToArray());
                if (_brush.Color != Color.Transparent)
                {
                    _gr.FillPolygon(_brush, _vertexes.ToArray());
                }
            }
            else
            {
                NotImpl(nameof(endShape));
            }
        }
        //public void quadraticVertex() { NotImpl(nameof(quadraticVertex)); }
        public void vertex(int x, int y) { _vertexes.Add(new Point(x, y)); } // Just x/y.
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
        public int millis() { return (int)(DynamicElements.RealTime * 1000); }
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
            _logger.Info($"print: {string.Join(" ", vars)}");
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
        public void popMatrix() { _gr.Transform = (Matrix)_matrixStack.Pop(); }
        //public void printMatrix() { NotImpl(nameof(printMatrix)); }
        public void pushMatrix() { _matrixStack.Push(_gr.Transform); }
        //public void resetMatrix() { NotImpl(nameof(resetMatrix)); }
        public void rotate(float angle) { _gr.RotateTransform((angle * 180.0f / PI)); }
        //public void rotateX() { NotImpl(nameof(rotateX)); }
        //public void rotateY() { NotImpl(nameof(rotateY)); }
        //public void rotateZ() { NotImpl(nameof(rotateZ)); }
        public void scale(float sc) { _gr.ScaleTransform(sc, sc); }
        public void scale(float scx, float scy) { _gr.ScaleTransform(scx, scy); }
        //public void shearX() { NotImpl(nameof(shearX)); }
        //public void shearY() { NotImpl(nameof(shearY)); }
        public void translate(float dx, float dy) { _gr.TranslateTransform(dx, dy); }
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
        public void background(int r, int g, int b, int a) { _bgColor = SafeColor(r, g, b, a); _gr.Clear(_bgColor); }
        public void background(int r, int g, int b) { background(r, g, b, 255); }
        public void background(int gray) { background(gray, gray, gray, 255); }
        public void background(int gray, int a) { background(gray, gray, gray, a); }
        public void background(color pcolor) { background(pcolor.R, pcolor.G, pcolor.B, 255); }
        public void background(string pcolor) { background(new color(pcolor)); }
        public void background(string pcolor, int alpha) { background(new color(pcolor)); }
        public void background(PImage img) { _gr.DrawImage(img.image(), 0, 0, width, height); }
        public void colorMode(int mode, float max = 255) { ColorModeX.ColorMode = new ColorModeX() { mode = mode, max1 = max, max2 = max, max3 = max, maxA = max }; }
        public void colorMode(int mode, int max1, int max2, int max3, int maxA = 255) { ColorModeX.ColorMode = new ColorModeX() { mode = mode, max1 = max1, max2 = max2, max3 = max3, maxA = maxA }; }
        public void fill(int r, int g, int b, int a) { _brush.Color = SafeColor(r, g, b, a); }
        public void fill(int r, int g, int b) { fill(r, g, b, 255); }
        public void fill(int gray) { fill(gray, gray, gray, 255); }
        public void fill(int gray, int a) { fill(gray, gray, gray, a); }
        public void fill(color pcolor) { _brush.Color = pcolor.NativeColor; _pen.Color = pcolor.NativeColor; }
        public void fill(color pcolor, int a) { fill(pcolor.R, pcolor.G, pcolor.B, a); }
        public void fill(string scolor) { fill(scolor); }
        public void fill(string scolor, int a) { fill(new color(scolor), a); }
        public void noFill() { _brush.Color = Color.Transparent; }
        public void noStroke() { _pen.Width = 0; }
        public void stroke(int r, int g, int b, int a) { _pen.Color = SafeColor(r, g, b, a); }
        public void stroke(int r, int g, int b) { stroke(r, g, b, 255); }
        public void stroke(int gray) { stroke(gray, gray, gray, 255); }
        public void stroke(int gray, int a) { stroke(gray, gray, gray, a); }
        public void stroke(color pcolor) { _pen.Color = pcolor.NativeColor; } 
        public void stroke(color pcolor, int a) { stroke(pcolor.R, pcolor.G, pcolor.B, a); }
        public void stroke(string scolor) { stroke(new color(scolor)); }
        public void stroke(string scolor, int a) { stroke(new color(scolor), a); }
        #endregion

        #region Color - Creating & Reading
        public color color(float v1, float v2, float v3, float a = 255) { return new color(v1, v2, v3, a); }
        public color color(float gray, float a = 255) { return new color(gray, a); }
        public int alpha(color color) { return color.A; }
        public int blue(color color) { return color.B; }
        public float brightness(color color) { return color.Brightness; }
        public int green(color color) { return color.G; }
        public float hue(color color) { return color.Hue; }
        public int red(color color) { return color.R; }
        public float saturation(color color) { return color.Saturation; }

        // Calculates a color between two colors at a specific increment. The amt parameter is the amount to interpolate between the two values where 0.0 is equal to the first point, 0.1 is very near the first point, 0.5 is halfway in between, etc. 
        // An amount below 0 will be treated as 0. Likewise, amounts above 1 will be capped at 1. This is different from the behavior of lerp(), but necessary because otherwise numbers outside the range will produce strange and unexpected colors.
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
            _gr.DrawImageUnscaled(img.image(), (int)x, (int)y);
            //_gr.DrawImage(img.image(), (int)x, (int)y, img.image().Width, img.image().Height);
        }

        public void image(PImage img, float x1, float y1, float x2, float y2)
        {
            _gr.DrawImage(img.image(), x1, y1, x2, y2);
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
            StringFormat format = new StringFormat();

            switch (_yAlign)
            {
                case LEFT: format.Alignment = StringAlignment.Near; break;
                case CENTER: format.Alignment = StringAlignment.Center; break;
                case RIGHT: format.Alignment = StringAlignment.Far; break;
            }

            switch (_xAlign)
            {
                case TOP: format.LineAlignment = StringAlignment.Near; break;
                case CENTER: format.LineAlignment = StringAlignment.Center; break;
                case BOTTOM: format.LineAlignment = StringAlignment.Far; break;
                case BASELINE: format.LineAlignment = StringAlignment.Far; break;
            }

            _gr.DrawString(s, _font, _brush, x, y, format);
        }

        public void textFont(PFont font)
        {
            _font = font.NativeFont;
        }
        #endregion

        #region Typography - Attributes
        //public void textAlign(int alignX) { NotImpl(nameof(textAlign)); }
        //public void textAlign(int alignX, int alignY) { NotImpl(nameof(textAlign)); }
        //public void textLeading(int leading) { NotImpl(nameof(textLeading)); }
        //public void textMode() { NotImpl(nameof(textMode)); }
        public void textSize(int pts) { _font = new Font(_font.Name, pts); }
        public int textWidth(string s) { return (int)Math.Round(_gr.MeasureString(s, _font, width).Width); }
        public int textWidth(char ch) { return textWidth(ch.ToString()); }
        #endregion

        #region Typography - Metrics
        public int textAscent() { return (int)Math.Round(_font.FontFamily.GetCellAscent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        public int textDescent() { return (int)Math.Round(_font.FontFamily.GetCellDescent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        #endregion
        #endregion

        #region Math
        #region Math - Calculation
        public int abs(int val) { return Math.Abs(val); }
        public float abs(float val) { return Math.Abs(val); }
        public int ceil(float val) { return (int)Math.Ceiling(val); }
        public float constrain(float val, float min, float max) { return (float)Utils.Constrain(val, min, max); }
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
        public float max(float[] vals) { return vals.Max(); }
        public int max(int[] vals) { return vals.Max(); }
        public float min(float[] vals) { return vals.Min(); }
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
