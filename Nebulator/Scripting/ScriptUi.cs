using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using NLog;
using Nebulator.Common;

// Processing java source: https://github.com/processing/processing/blob/master/core/src/processing


namespace Nebulator.Scripting
{
    /// <summary>
    /// Processing emulation script stuff.
    /// The properties and functions are organized similarly to the API in https://processing.org/reference/.
    /// </summary>
    public partial class Script
    {
        #region Fields - internal
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Current working Graphics object to draw on. Most ops use this to draw with.</summary>
        Graphics _gr = null;

        /// <summary>Current working bitmap. Some ops are happier manipulating the bitmap directly.</summary>
        Bitmap _bmp = null;
        #endregion

        #region Fields - storage for current state
        Font _font = new Font("Arial", 12f, GraphicsUnit.Pixel);
        Pen _pen = new Pen(Color.Black, 1f) { LineJoin = LineJoin.Round, EndCap = LineCap.Round, StartCap = LineCap.Round };
        SolidBrush _brush = new SolidBrush(Color.Transparent);
        Color _bgColor = Color.LightGray;
        bool _smooth = false;
        int _xAlign = LEFT;
        int _yAlign = BASELINE;
        Stack<object> _matrixStack = new Stack<object>();
        Bag _style = new Bag();
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
        // keyCodes
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
        public const int HSB = 3;
        //---- Cursor types
        public const int ARROW = 0;
        public const int CROSS = 1;
        public const int TEXT = 2;
        public const int WAIT = 3;
        public const int HAND = 12;
        public const int MOVE = 13;
        #endregion

        #region Structure
        //---- Function overrides
        public virtual void setup() { }
        public virtual void draw() { }
        
        //---- Script functions
        public void exit() { throw new NotSupportedException(); }
        public void loop() { throw new NotSupportedException(); }
        public void noLoop() { throw new NotSupportedException(); }
        // TODO2 save all fonts, styles etc in a Bag. Restore in popStyle(). These:
        // fill(), stroke(), tint(), strokeWeight(), strokeCap(), strokeJoin(), imageMode(), rectMode(), 
        // ellipseMode(), shapeMode(), colorMode(), textAlign(), textFont(), textMode(), textSize(), textLeading(), 
        // emissive(), specular(), shininess(), ambient()
        public void popStyle() { throw new NotSupportedException(); }
        public void pushStyle() { throw new NotSupportedException(); }

        // Executes the code within draw() one time. This functions allows the program to update the display window only when necessary, for example when an event registered by mousePressed() or keyPressed() occurs.
        // In structuring a program, it only makes sense to call redraw() within events such as mousePressed(). This is because redraw() does not run draw() immediately(it only sets a flag that indicates an update is needed). 
        // The redraw() function does not work properly when called inside draw(). To enable/disable animations, use loop() and noLoop().
        public void redraw() { throw new NotSupportedException(); }

        public void thread() { throw new NotSupportedException(); }
        #endregion

        #region Environment 
        //---- Script properties
        public int frameCount { get; private set; } = 1;
        public int height { get { return Surface.Height; } }
        public int width { get { return Surface.Width; } }
        public void cursor(int which) { throw new NotSupportedException(); }
        public void cursor(PImage image) { throw new NotSupportedException(); }
        public void cursor(PImage image, int x, int y) { throw new NotSupportedException(); }
        public void delay(int msec) { throw new NotSupportedException(); }
        public int displayDensity() { throw new NotSupportedException(); }
        public bool focused { get { return Surface.Focused; } }

        public int frameRate
        {
            get
            {
                ScriptEventArgs args = new ScriptEventArgs();
                ScriptEvent?.Invoke(this, args);
                return (int)args.FrameRate;
            }
            set
            {
                ScriptEvent?.Invoke(this, new ScriptEventArgs() { FrameRate = value });
            }
        }

        public void fullScreen() { throw new NotSupportedException(); }
        public void noCursor() { throw new NotSupportedException(); }
        public void noSmooth() { _smooth = false; }
        public void pixelDensity(int density) { throw new NotSupportedException(); }
        public int pixelHeight { get { throw new NotSupportedException(); } }
        public int pixelWidth { get { throw new NotSupportedException(); } }
        public void size(int width, int height) { throw new NotSupportedException(); }
        public void smooth() { _smooth = true; }
        public void smooth(int level) { _smooth = level > 0; }
        #endregion

        #region Data
        public string binary(object value) { throw new NotSupportedException(); }
        public bool boolean(object value) { throw new NotSupportedException(); }
        public byte @byte (object value) { throw new NotSupportedException(); }
        public char @char (object value) { throw new NotSupportedException(); }
        public float @float(object value) { throw new NotSupportedException(); }
        public string hex(object value) { throw new NotSupportedException(); }
        public int @int(float val) { return (int)val; }
        public string str(object value) { return value.ToString(); }
        public int unbinary(string value) { throw new NotSupportedException(); }
        public int unhex(string value) { throw new NotSupportedException(); }

        #region Data - String Functions
        public string join(params object[] ps) { throw new NotSupportedException(); }
        public string match(params object[] ps) { throw new NotSupportedException(); }
        public string matchAll(params object[] ps) { throw new NotSupportedException(); }
        public string nf(params object[] ps) { throw new NotSupportedException(); }
        public string nfc(params object[] ps) { throw new NotSupportedException(); }
        public string nfp(params object[] ps) { throw new NotSupportedException(); }
        public string nfs(params object[] ps) { throw new NotSupportedException(); }
        public string split(params object[] ps) { throw new NotSupportedException(); }
        public string splitTokens(params object[] ps) { throw new NotSupportedException(); }
        public string trim(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Data - Array Functions
        public void append(params object[] ps) { throw new NotSupportedException(); }
        public void arrayCopy(params object[] ps) { throw new NotSupportedException(); }
        public void concat(params object[] ps) { throw new NotSupportedException(); }
        public void expand(params object[] ps) { throw new NotSupportedException(); }
        public void reverse(params object[] ps) { throw new NotSupportedException(); }
        public void shorten(params object[] ps) { throw new NotSupportedException(); }
        public void sort(params object[] ps) { throw new NotSupportedException(); }
        public void splice(params object[] ps) { throw new NotSupportedException(); }
        public void subset(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Shape 
        public void createShape(params object[] ps) { throw new NotSupportedException(); }
        public void loadShape(params object[] ps) { throw new NotSupportedException(); }

        #region Shape - 2D Primitives
        public void arc(float x1, float y1, float x2, float y2, float angle1, float angle2, int style)
        {
            x1 -= width / 2;
            y1 -= height / 2;
            angle1 *= 57.2957801818848f; // 360 / 57.29 = 2*PI
            angle2 *= 57.2957801818848f;
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

        public void ellipse(float x1, float y1, float width, float height)
        {
            x1 -= width / 2;
            y1 -= height / 2;
            _gr.FillEllipse(_brush, x1, y1, width, height);
            _gr.DrawEllipse(_pen, x1, y1, width, height);
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
            _gr.FillRectangle(_brush, x1 - 0.5f, y1 - 0.5f, w, h);
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

        public void bezierDetail(params object[] ps) { throw new NotSupportedException(); }
        public void bezierPoint(params object[] ps) { throw new NotSupportedException(); }
        public void bezierTangent(params object[] ps) { throw new NotSupportedException(); }

        public void curve(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            _gr.DrawCurve(_pen, new Point[4] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3), new Point(x4, y4) }, 1, 1, 0.5f);
        }

        public void curveDetail(params object[] ps) { throw new NotSupportedException(); }
        public void curvePoint(params object[] ps) { throw new NotSupportedException(); }
        public void curveTangent(params object[] ps) { throw new NotSupportedException(); }
        public void curveTightness(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Shape - 3D Primitives
        public void box(params object[] ps) { throw new NotSupportedException(); }
        public void sphere(params object[] ps) { throw new NotSupportedException(); }
        public void sphereDetail(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Shape - Attributes
        public void ellipseMode(int mode) { throw new NotSupportedException(); }
        public void rectMode(int mode) { throw new NotSupportedException(); }

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

        public void strokeWeight(int width) { _pen.Width = width; }
        #endregion

        #region Shape - Vertex
        public void beginContour(params object[] ps) { throw new NotSupportedException(); }
        public void beginShape(params object[] ps) { throw new NotSupportedException(); }
        public void bezierVertex(params object[] ps) { throw new NotSupportedException(); }
        public void curveVertex(params object[] ps) { throw new NotSupportedException(); }
        public void endContour(params object[] ps) { throw new NotSupportedException(); }
        public void endShape(params object[] ps) { throw new NotSupportedException(); }
        public void quadraticVertex(params object[] ps) { throw new NotSupportedException(); }
        public void vertex(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Shape - Loading & Displaying
        public void shape(params object[] ps) { throw new NotSupportedException(); }
        public void shapeMode(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Input
        #region Input - Mouse
        //---- Script properties
        public bool mousePressedP { get; private set; } = false;
        public int mouseButton { get; private set; } = LEFT;
        public int mouseWheelValue { get; private set; } = 0;
        public int mouseX { get; private set; } = 0;
        public int mouseY { get; private set; } = 0;
        public int pMouseX { get; private set; } = 0;
        public int pMouseY { get; private set; } = 0;
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
        public int keyCode { get; private set; } = 0;
        public bool keyPressedP { get; private set; } = false;
        //---- Function overrides
        public virtual void keyPressed() { }
        public virtual void keyReleased() { }
        public virtual void keyTyped() { }
        #endregion

        #region Input - Files
        public void createInput(params object[] ps) { throw new NotSupportedException(); }
        public void createReader(params object[] ps) { throw new NotSupportedException(); }
        public void launch(params object[] ps) { throw new NotSupportedException(); }
        public void loadBytes(params object[] ps) { throw new NotSupportedException(); }
        public void loadJSONArray(params object[] ps) { throw new NotSupportedException(); }
        public void loadJSONObject(params object[] ps) { throw new NotSupportedException(); }
        public void loadStrings(params object[] ps) { throw new NotSupportedException(); }
        public void loadTable(params object[] ps) { throw new NotSupportedException(); }
        public void loadXML(params object[] ps) { throw new NotSupportedException(); }
        public void parseJSONArray(params object[] ps) { throw new NotSupportedException(); }
        public void parseJSONObject(params object[] ps) { throw new NotSupportedException(); }
        public void parseXML(params object[] ps) { throw new NotSupportedException(); }
        public void selectFolder(params object[] ps) { throw new NotSupportedException(); }
        public void selectInput(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Input - Time & Date
        public int day() { return DateTime.Now.Day; }
        public int hour() { return DateTime.Now.Hour; }
        public int minute() { return DateTime.Now.Minute; }
        public int millis() { return DateTime.Now.Millisecond; }
        public int month() { return DateTime.Now.Month; }
        public int second() { return DateTime.Now.Second; }
        public int year() { return DateTime.Now.Year; }
        #endregion
        #endregion

        #region Output
        #region Output - Text Area
        public void print(params object[] vars) { ScriptEvent?.Invoke(this, new ScriptEventArgs() { Message = string.Join(" ", vars) }); }
        public void println(params object[] vars) { ScriptEvent?.Invoke(this, new ScriptEventArgs() { Message = string.Join(" ", vars) + Environment.NewLine }); }
        public void printArray(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Output - Image
        public void save(string fn) { throw new NotSupportedException(); }
        public void saveFrame() { throw new NotSupportedException(); }
        public void saveFrame(string fn) { throw new NotSupportedException(); }
        #endregion

        #region Output - Files
        public void beginRaw(params object[] ps) { throw new NotSupportedException(); }
        public void beginRecord(params object[] ps) { throw new NotSupportedException(); }
        public void createOutput(params object[] ps) { throw new NotSupportedException(); }
        public void createWriter(params object[] ps) { throw new NotSupportedException(); }
        public void endRaw(params object[] ps) { throw new NotSupportedException(); }
        public void endRecord(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Output - PrintWriter
        public void saveBytes(params object[] ps) { throw new NotSupportedException(); }
        public void saveJSONArray(params object[] ps) { throw new NotSupportedException(); }
        public void saveJSONObject(params object[] ps) { throw new NotSupportedException(); }
        public void saveStream(params object[] ps) { throw new NotSupportedException(); }
        public void saveStrings(params object[] ps) { throw new NotSupportedException(); }
        public void saveTable(params object[] ps) { throw new NotSupportedException(); }
        public void saveXML(params object[] ps) { throw new NotSupportedException(); }
        public void selectOutput(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Transform 
        public void applyMatrix(params object[] ps) { throw new NotSupportedException(); }
        public void popMatrix() { _gr.Transform = (Matrix)_matrixStack.Pop(); }
        public void printMatrix(params object[] ps) { throw new NotSupportedException(); }
        public void pushMatrix() { _matrixStack.Push(_gr.Transform); }
        public void resetMatrix(params object[] ps) { throw new NotSupportedException(); }
        public void rotate(float angle) { _gr.RotateTransform((angle * 180.0f / PI)); }
        public void rotateX(params object[] ps) { throw new NotSupportedException(); }
        public void rotateY(params object[] ps) { throw new NotSupportedException(); }
        public void rotateZ(params object[] ps) { throw new NotSupportedException(); }
        public void scale(float sc) { _gr.ScaleTransform(sc, sc); }
        public void scale(float scx, float scy) { _gr.ScaleTransform(scx, scy); }
        public void shearX(params object[] ps) { throw new NotSupportedException(); }
        public void shearY(params object[] ps) { throw new NotSupportedException(); }
        public void translate(float dx, float dy) { _gr.TranslateTransform(dx, dy); }
        #endregion

        #region Lights & Camera
        #region Lights & Camera - Lights
        public string ambientLight(params object[] ps) { throw new NotSupportedException(); }
        public string directionalLight(params object[] ps) { throw new NotSupportedException(); }
        public string lightFalloff(params object[] ps) { throw new NotSupportedException(); }
        public string lights(params object[] ps) { throw new NotSupportedException(); }
        public string lightSpecular(params object[] ps) { throw new NotSupportedException(); }
        public string noLights(params object[] ps) { throw new NotSupportedException(); }
        public string normal(params object[] ps) { throw new NotSupportedException(); }
        public string pointLight(params object[] ps) { throw new NotSupportedException(); }
        public string spotLight(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Lights & Camera - Camera
        public string beginCamera(params object[] ps) { throw new NotSupportedException(); }
        public string camera(params object[] ps) { throw new NotSupportedException(); }
        public string endCamera(params object[] ps) { throw new NotSupportedException(); }
        public string frustum(params object[] ps) { throw new NotSupportedException(); }
        public string ortho(params object[] ps) { throw new NotSupportedException(); }
        public string perspective(params object[] ps) { throw new NotSupportedException(); }
        public string printCamera(params object[] ps) { throw new NotSupportedException(); }
        public string printProjection(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Lights & Camera - Coordinates
        public string modelX(params object[] ps) { throw new NotSupportedException(); }
        public string modelY(params object[] ps) { throw new NotSupportedException(); }
        public string modelZ(params object[] ps) { throw new NotSupportedException(); }
        public string screenX(params object[] ps) { throw new NotSupportedException(); }
        public string screenY(params object[] ps) { throw new NotSupportedException(); }
        public string screenZ(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Lights & Camera - Material Properties
        public string ambient(params object[] ps) { throw new NotSupportedException(); }
        public string emissive(params object[] ps) { throw new NotSupportedException(); }
        public string shininess(params object[] ps) { throw new NotSupportedException(); }
        public string specular(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Color
        #region Color - Setting
        public void background(int r, int g, int b, int a)
        {
            r = constrain(r, 0, 255);
            g = constrain(g, 0, 255);
            b = constrain(b, 0, 255);
            a = constrain(a, 0, 255);
            _bgColor = Color.FromArgb(a, r, g, b);
            _gr.Clear(_bgColor);
        }

        public void background(int r, int g, int b) { background(r, g, b, 255); }
        public void background(int gray) { background(gray, gray, gray, 255); }
        public void background(int gray, int a) { background(gray, gray, gray, a); }
        public void background(color pcolor) { background(pcolor.NativeColor.R, pcolor.NativeColor.G, pcolor.NativeColor.B, 255); }
        public void background(string pcolor) { background(new color(pcolor)); }
        public void background(string pcolor, int alpha) { background(new color(pcolor)); }
        public void background(PImage img) { _gr.DrawImage(img.image(), 0, 0, width, height); }
        public void clear() { throw new NotSupportedException(); }
        public void colorMode(params object[] ps) { throw new NotSupportedException(); }

        public void fill(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            _brush.Color = Color.FromArgb(a, r, g, b);
        }

        public void fill(int r, int g, int b) { fill(r, g, b, 255); }
        public void fill(int gray) { fill(gray, gray, gray, 255); }
        public void fill(int gray, int a) { fill(gray, gray, gray, a); }
        public void fill(color pcolor) { _brush.Color = pcolor.NativeColor; }
        public void fill(color pcolor, int a) { fill(pcolor, a); }
        public void fill(string scolor) { fill(scolor); }
        public void fill(string scolor, int a) { fill(new color(scolor), a); }
        public void noFill() { _brush.Color = Color.Transparent; }
        public void noStroke() { _pen.Width = 0; }

        public void stroke(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            _pen.Color = Color.FromArgb(a, r, g, b);
        }

        public void stroke(int r, int g, int b) { stroke(r, g, b, 255); }
        public void stroke(int gray) { stroke(gray, gray, gray, 255); }
        public void stroke(int gray, int a) { stroke(gray, gray, gray, a); }
        public void stroke(color pcolor) { _pen.Color = pcolor.NativeColor; } 
        public void stroke(color pcolor, int a) { stroke(pcolor.NativeColor.R, pcolor.NativeColor.G, pcolor.NativeColor.B, a); }
        public void stroke(string scolor) { stroke(new color(scolor)); }
        public void stroke(string scolor, int a) { stroke(new color(scolor), a); }
        #endregion

        #region Color - Creating & Reading
        public color color(int r, int g, int b) { return new color(r, g, b); }
        public color color(int gray) { return new color(gray); }
        public int alpha(color color) { return color.NativeColor.A; }
        public int blue(color color) { return color.NativeColor.B; }
        public float brightness(color color) { return color.NativeColor.GetBrightness(); }
        public int green(color color) { return color.NativeColor.G; }
        public float hue(color color) { return color.NativeColor.GetHue(); }
        public int red(color color) { return color.NativeColor.R; }
        public float saturation(color color) { return color.NativeColor.GetSaturation(); }

        // Calculates a color between two colors at a specific increment. The amt parameter is the amount to interpolate between the two values where 0.0 is equal to the first point, 0.1 is very near the first point, 0.5 is halfway in between, etc. 
        // An amount below 0 will be treated as 0. Likewise, amounts above 1 will be capped at 1. This is different from the behavior of lerp(), but necessary because otherwise numbers outside the range will produce strange and unexpected colors.
        public color lerpColor(color c1, color c2, float amt)
        {
            amt = constrain(amt, 0, 1);
            int r = (int)lerp(c1.NativeColor.R, c2.NativeColor.R, amt);
            int b = (int)lerp(c1.NativeColor.B, c2.NativeColor.B, amt);
            int g = (int)lerp(c1.NativeColor.G, c2.NativeColor.G, amt);
            int a = (int)lerp(c1.NativeColor.A, c2.NativeColor.A, amt);
            return new color(r, g, b, a);
        }

        #endregion
        #endregion

        #region Image 
        public PImage createImage(params object[] ps) { throw new NotSupportedException(); }

        #region Image - Loading & Displaying
        public void image(PImage img, float x, float y)
        {
            float width = img.image().Width;
            float height = img.image().Height;
            _gr.DrawImageUnscaled(img.image(), (int)x, (int)y);
        }

        public void image(PImage img, float x1, float y1, float x2, float y2)
        {
            _gr.DrawImage(img.image(), x1, y1, x2, y2);
        }

        public void imageMode(int mode) { throw new NotSupportedException(); }

        public PImage loadImage(string filename)
        {
            return new PImage(filename);
        }

        public void noTint(params object[] ps) { throw new NotSupportedException(); }
        public void requestImage(params object[] ps) { throw new NotSupportedException(); }
        public void tint(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Image - Textures
        public void texture(params object[] ps) { throw new NotSupportedException(); }
        public void textureMode(params object[] ps) { throw new NotSupportedException(); }
        public void textureWrap(params object[] ps) { throw new NotSupportedException(); }
        #endregion

        #region Image - Pixels
        // pixels[]
        public void blend(params object[] ps) { throw new NotSupportedException(); }
        public void copy(params object[] ps) { throw new NotSupportedException(); }
        public void filter(params object[] ps) { throw new NotSupportedException(); }

        public PImage get(int x, int y, int width, int height)
        {
            return new PImage(_bmp.Clone(new Rectangle(x, y, width, height), _bmp.PixelFormat));
        }

        public color get(int x, int y)
        {
            return new color(_bmp.GetPixel(x, y));
        }

        public void loadPixels(params object[] ps) { throw new NotSupportedException(); }

        public void set(int x, int y, color pcolor)
        {
            _bmp.SetPixel(x, y, pcolor.NativeColor);
        }

        public void set(int x, int y, PImage src)
        {
            Graphics tgt = Graphics.FromImage(_bmp);
            tgt.DrawImageUnscaled(src.image(), x, y);
        }

        public void updatePixels(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Rendering 
        public void blendMode(params object[] ps) { throw new NotSupportedException(); }
        public void clip(params object[] ps) { throw new NotSupportedException(); }
        public void createGraphics(params object[] ps) { throw new NotSupportedException(); }
        public void noClip(params object[] ps) { throw new NotSupportedException(); }

        #region Rendering - Shaders
        public void loadShader(params object[] ps) { throw new NotSupportedException(); }
        public void resetShader(params object[] ps) { throw new NotSupportedException(); }
        public void shader(params object[] ps) { throw new NotSupportedException(); }
        #endregion
        #endregion

        #region Typography
        #region Typography - Loading & Displaying
        public PFont createFont(string name, int size)
        {
            return new PFont(name, size);
        }

        public PFont loadFont(params object[] ps) { throw new NotSupportedException(); }

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

            // TODO2 support text wrapping.
            _gr.DrawString(s, _font, _brush, x, y, format);
        }

        public void textFont(PFont font)
        {
            _font = font.NativeFont;
        }
        #endregion

        #region Typography - Attributes
        public void textAlign(int alignX) { throw new NotSupportedException(); }
        public void textAlign(int alignX, int alignY) { throw new NotSupportedException(); }
        public void textLeading(int leading) { throw new NotSupportedException(); }
        public void textMode(params object[] ps) { throw new NotSupportedException(); }
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
        public void noise(params object[] ps) { throw new NotSupportedException(); }
        public void noiseDetail(params object[] ps) { throw new NotSupportedException(); }
        public void noiseSeed(params object[] ps) { throw new NotSupportedException(); }
        public float random(float max) { return (float)_rand.NextDouble() * max; }
        public float random(float min, float max) { return min + (float)_rand.NextDouble() * (max - min); }
        public float randomGaussian() { return (float)Utils.NextGaussian(_rand); }
        public void randomSeed(int seed) { _rand = new Random(seed); }
        #endregion
        #endregion
    }
}
