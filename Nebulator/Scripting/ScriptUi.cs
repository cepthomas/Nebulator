using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows.Forms;
using NLog;
using Nebulator.Common;


namespace Nebulator.Scripting
{
    /// <summary>
    /// Processing emulation script stuff. The properties and functions are organized similarly to the API in https://processing.org/reference/.
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

        #region Fields - storage for state
        Font _font = new Font("Arial", 12f, GraphicsUnit.Pixel);
        Pen _pen = new Pen(Color.Black, 1f) { LineJoin = LineJoin.Round, EndCap = LineCap.Round, StartCap = LineCap.Round };
        SolidBrush _brush = new SolidBrush(Color.Transparent);
        Color _bgColor = Color.LightGray;
        bool _smooth = false;
        int _xAlign = LEFT;
        int _yAlign = BASELINE;
        bool _mousePressed = false;
        Stack<object> _matrixStack = new Stack<object>();
        #endregion

        #region Definitions
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
        public const int CODED = 0xFF; //65535;
        // public const int ALT = 8;
        // public const int CTRL = 2;
        // public const int SHIFT = 1;

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
        #endregion

        #region Structure
        //---- Function overrides
        public virtual void setup() { }
        public virtual void draw() { }

        // exit()
        // loop()
        // noLoop()
        // popStyle()
        // pushStyle()
        // redraw()
        // thread()
        #endregion

        #region Environment 
        //---- Script properties
        public int frameCount { get; private set; } = 1;
        public int height { get { return Surface.Height; } }
        public int width { get { return Surface.Width; } }

        // cursor()
        // delay()
        // displayDensity()
        // focused
        // frameRate()
        // frameRate
        // fullScreen()
        // noCursor()

        public void noSmooth()
        {
            _smooth = false;
        }

        // pixelDensity()
        // pixelHeight
        // pixelWidth
        // settings()
        // size()

        public void smooth()
        {
            _smooth = true;
        }
        public void smooth(int level) // either 2, 3, 4, or 8 depending on the renderer
        {
            _smooth = level > 0;
        }
        #endregion

        #region Data
        // binary()
        // boolean()
        // byte()
        // char()
        // float()
        // hex()

        public int @int(float val)
        {
            return (int)val;
        }

        // str()
        // unbinary()
        // unhex()

        #region Data - String Functions
        // join()
        // match()
        // matchAll()
        // nf()
        // nfc()
        // nfp()
        // nfs()
        // split()
        // splitTokens()
        // trim()
        #endregion

        #region Data - Array Functions
        // append()
        // arrayCopy()
        // concat()
        // expand()
        // reverse()
        // shorten()
        // sort()
        // splice()
        // subset()
        #endregion
        #endregion

        #region Shape 
        // createShape()
        // loadShape()

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
            x1 -= width / 2;
            y1 -= height / 2;
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

        // bezierDetail()
        // bezierPoint()
        // bezierTangent()

        public void curve(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            _gr.DrawCurve(_pen, new Point[4] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3), new Point(x4, y4) }, 1, 1, 0.5f);
        }

        // curveDetail()
        // curvePoint()
        // curveTangent()
        // curveTightness()
        #endregion

        #region Shape - 3D Primitives
        // box()
        // sphere()
        // sphereDetail()
        #endregion

        #region Shape - Attributes
        // ellipseMode()
        // rectMode()

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

        public void strokeWeight(int width)
        {
            _pen.Width = width;
        }
        #endregion

        #region Shape - Vertex
        // beginContour()
        // beginShape()
        // bezierVertex()
        // curveVertex()
        // endContour()
        // endShape()
        // quadraticVertex()
        // vertex()
        #endregion

        #region Shape - Loading & Displaying
        // shape()
        // shapeMode()
        #endregion
        #endregion

        #region Input
        #region Input - Mouse
        //---- Script properties
        public bool mousePressed { get { return _mousePressed; } }
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
        public virtual void mousePressedEvt() { }
        public virtual void mouseReleased() { }
        public virtual void mouseWheel() { }

        // mouseButton
        // mouseClicked()
        // mouseDragged()
        // mouseMoved()
        // mousePressed()
        // mousePressed
        // mouseReleased()
        // mouseWheel()
        // mouseX
        // mouseY
        // pmouseX
        // pmouseY
        #endregion

        #region Input - Keyboard
        //---- Script properties
        public char key { get; internal set; } = ' ';
        public int keyCode { get; private set; } = 0;
        // New properties added:
        public bool alt { get; private set; } = false;
        public bool ctrl { get; private set; } = false;
        public bool shift { get; private set; } = false;

        //---- Function overrides
        public virtual void keyPressed() { }
        public virtual void keyReleased() { }

        // key
        // keyCode
        // keyPressed()
        // keyPressed
        // keyReleased()
        // keyTyped()
        #endregion

        #region Input - Files
        // createInput()
        // createReader()
        // launch()
        // loadBytes()
        // loadJSONArray()
        // loadJSONObject()
        // loadStrings()
        // loadTable()
        // loadXML()
        // parseJSONArray()
        // parseJSONObject()
        // parseXML()
        // selectFolder()
        // selectInput()
        #endregion

        #region Input - Time & Date
        // day()
        // hour()
        // millis()
        // minute()
        // month()
        // second()
        // year()
        #endregion
        #endregion

        #region Output
        #region Output - Text Area
        public void print(params object[] vars)
        {
            ScriptEvent?.Invoke(this, new ScriptEventArgs() { Message = string.Join(" ", vars) });
        }

        public void println(params object[] vars)
        {
            ScriptEvent?.Invoke(this, new ScriptEventArgs() { Message = string.Join(" ", vars) + Environment.NewLine });
        }

        // printArray()
        #endregion

        #region Output - Image
        // save()
        // saveFrame()
        #endregion

        #region Output - Files
        // beginRaw()
        // beginRecord()
        // createOutput()
        // createWriter()
        // endRaw()
        // endRecord()
        #endregion

        #region Output - PrintWriter
        // saveBytes()
        // saveJSONArray()
        // saveJSONObject()
        // saveStream()
        // saveStrings()
        // saveTable()
        // saveXML()
        // selectOutput()
        #endregion
        #endregion

        #region Transform 
        // applyMatrix()

        public void popMatrix()
        {
            _gr.Transform = (Matrix)_matrixStack.Pop();
        }

        // printMatrix()

        public void pushMatrix()
        {
            _matrixStack.Push(_gr.Transform);
        }

        // resetMatrix()

        public void rotate(float angle)
        {
            _gr.RotateTransform((angle * 180.0f / PI));
        }

        // rotateX()
        // rotateY()
        // rotateZ()

        public void scale(float sc)
        {
            _gr.ScaleTransform(sc, sc);
        }

        public void scale(float scx, float scy)
        {
            _gr.ScaleTransform(scx, scy);
        }

        // shearX()
        // shearY()

        public void translate(float dx, float dy)
        {
            _gr.TranslateTransform(dx, dy);
        }
        #endregion

        #region Lights & Camera
        #region Lights & Camera - Lights
        // ambientLight()
        // directionalLight()
        // lightFalloff()
        // lights()
        // lightSpecular()
        // noLights()
        // normal()
        // pointLight()
        // spotLight()
        #endregion

        #region Lights & Camera - Camera
        // beginCamera()
        // camera()
        // endCamera()
        // frustum()
        // ortho()
        // perspective()
        // printCamera()
        // printProjection()
        #endregion

        #region Lights & Camera - Coordinates
        // modelX()
        // modelY()
        // modelZ()
        // screenX()
        // screenY()
        // screenZ()
        #endregion

        #region Lights & Camera - Material Properties
        // ambient()
        // emissive()
        // shininess()
        // specular()
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

        public void background(int r, int g, int b)
        {
            background(r, g, b, 255);
        }

        public void background(int gray)
        {
            background(gray, gray, gray, 255);
        }

        public void background(int gray, int a)
        {
            background(gray, gray, gray, a);
        }

        public void background(PColor pcolor)
        {
            background(pcolor.NativeColor.R, pcolor.NativeColor.G, pcolor.NativeColor.B, 255);
        }

        public void background(string pcolor)
        {
            background(new PColor(pcolor));
        }

        public void background(string pcolor, int alpha)
        {
            background(new PColor(pcolor));
        }

        public void background(PImage img)
        {
            _gr.DrawImage(img.image(), 0, 0, width, height);
        }

        // clear()
        // colorMode()

        public void fill(int r, int g, int b)
        {
            fill(r, g, b, 255);
        }

        public void fill(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            _brush.Color = Color.FromArgb(a, r, g, b);
        }

        public void fill(int gray)
        {
            fill(gray, gray, gray, 255);
        }

        public void fill(int gray, int a)
        {
            fill(gray, gray, gray, a);
        }

        public void fill(PColor pcolor)
        {
            _brush.Color = pcolor.NativeColor;
        }

        public void fill(PColor pcolor, int a)
        {
            fill(pcolor, a);
        }

        public void fill(string scolor)
        {
            fill(scolor);
        }

        public void fill(string scolor, int a)
        {
            fill(new PColor(scolor), a);
        }

        public void noFill()
        {
            _brush.Color = Color.Transparent;
        }

        public void noStroke()
        {
            _pen.Width = 0;
        }

        public void stroke(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            _pen.Color = Color.FromArgb(a, r, g, b);
        }

        public void stroke(int r, int g, int b)
        {
            stroke(r, g, b, 255);
        }

        public void stroke(int gray)
        {
            stroke(gray, gray, gray, 255);
        }

        public void stroke(int gray, int a)
        {
            stroke(gray, gray, gray, a);
        }

        public void stroke(PColor pcolor)
        {
            _pen.Color = pcolor.NativeColor;
        }

        public void stroke(PColor pcolor, int a)
        {
            stroke(pcolor.NativeColor.R, pcolor.NativeColor.G, pcolor.NativeColor.B, a);
        }

        public void stroke(string scolor)
        {
            stroke(new PColor(scolor));
        }

        public void stroke(string scolor, int a)
        {
            stroke(new PColor(scolor), a);
        }
        #endregion

        #region Color - Creating & Reading
        public PColor color(int r, int g, int b)
        {
            return new PColor(r, g, b);
        }

        public PColor color(int gray)
        {
            return new PColor(gray);
        }

        // alpha()
        // blue()
        // brightness()
        // green()
        // hue()
        // lerpColor()
        // red()
        // saturation()
        #endregion
        #endregion

        #region Image 
        // createImage()

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

        // imageMode()

        public PImage loadImage(string filename)
        {
            return new PImage(filename);
        }

        // noTint()
        // requestImage()
        // tint()
        #endregion

        #region Image - Textures
        // texture()
        // textureMode()
        // textureWrap()
        #endregion

        #region Image - Pixels
        // pixels[]
        // blend()
        // copy()
        // filter()

        public PImage get(int x, int y, int width, int height)
        {
            return new PImage(_bmp.Clone(new Rectangle(x, y, width, height), _bmp.PixelFormat));
        }

        public PColor get(int x, int y)
        {
            return new PColor(_bmp.GetPixel(x, y));
        }

        // loadPixels()

        public void set(int x, int y, PColor pcolor)
        {
            _bmp.SetPixel(x, y, pcolor.NativeColor);
        }

        public void set(int x, int y, PImage src)
        {
            Graphics tgt = Graphics.FromImage(_bmp);
            tgt.DrawImageUnscaled(src.image(), x, y);
        }

        // updatePixels()
        #endregion
        #endregion

        #region Rendering 
        // blendMode()
        // clip()
        // createGraphics()
        // noClip()

        #region Rendering - Shaders
        // loadShader()
        // resetShader()
        // shader()
        #endregion
        #endregion

        #region Typography
        #region Typography - Loading & Displaying
        public PFont createFont(string name, int size)
        {
            return new PFont(name, size);
        }

        // loadFont()

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

            // FUTURE support text wrapping.
            _gr.DrawString(s, _font, _brush, x, y, format);
        }

        // textFont()
        #endregion

        #region Typography - Attributes
        // textAlign()
        // textLeading()
        // textMode()

        public void textSize(int pts)
        {
            _font = new Font(_font.Name, pts);
        }

        public int textWidth(string s)
        {
            return (int)Math.Round(_gr.MeasureString(s, _font, width).Width);
        }
        #endregion

        #region Typography - Metrics
        // textAscent()
        // textDescent()
        #endregion
        #endregion

        #region Math
        #region Math - Calculation
        public int abs(int val)
        {
            return Math.Abs(val);
        }

        public float abs(float val)
        {
            return Math.Abs(val);
        }

        public int ceil(float val)
        {
            return (int)Math.Ceiling(val);
        }

        public float constrain(float val, float min, float max)
        {
            return (float)Utils.Constrain(val, min, max);
        }

        public int constrain(int val, int min, int max)
        {
            return Utils.Constrain(val, min, max);
        }

        public float dist(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(sq(x1 - x2) + sq(y1 - y2));
        }

        public float dist(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return (float)Math.Sqrt(sq(x1 - x2) + sq(y1 - y2) + sq(z1 - z2));
        }

        public float exp(float exponent)
        {
            return (float)Math.Exp(exponent);
        }

        public int floor(float val)
        {
            return (int)Math.Floor(val);
        }

        public float lerp(float start, float stop, float amt)
        {
            return start + (stop - start) * amt;
        }

        public float log(float val)
        {
            return (float)Math.Log(val);
        }

        public float mag(float x, float y)
        {
            return (float)Math.Sqrt(sq(x) + sq(y));
        }

        public float mag(float x, float y, float z)
        {
            return (float)Math.Sqrt(sq(x) + sq(y) + sq(z));
        }

        public float map(float val, float start1, float stop1, float start2, float stop2)
        {
            return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        }

        public float max(float[] vals)
        {
            return vals.Max();
        }

        public int max(int[] vals)
        {
            return vals.Max();
        }

        public float min(float[] vals)
        {
            return vals.Min();
        }

        public int min(int[] vals)
        {
            return vals.Min();
        }

        public float norm(float val, float start, float stop)
        {
            return (val - start) / (stop - start);
        }

        public float pow(float val, float exponent)
        {
            return (float)Math.Pow(val, exponent);
        }

        public int round(float val)
        {
            return (int)Math.Round(val);
        }

        public float sq(float val)
        {
            return (float)Math.Pow(val, 2);
        }

        public float sqrt(float val)
        {
            return (float)Math.Sqrt(val);
        }
        public int truncate(float val)
        {
            return (int)val;
        }
        #endregion

        #region Math - Trigonometry
        public float acos(float val)
        {
            return (float)Math.Acos(val);
        }

        public float asin(float val)
        {
            return (float)Math.Asin(val);
        }

        public float atan(float val)
        {
            return (float)Math.Atan(val);
        }

        public float atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        public float cos(float angle)
        {
            return (float)Math.Cos(angle);
        }

        public float degrees(float rad)
        {
            return 360.0f * rad / (2.0f * PI);
        }

        public float radians(float degrees)
        {
            return 2.0f * PI * degrees / 360.0f;
        }

        public float sin(float angle)
        {
            return (float)Math.Sin(angle);
        }

        public float tan(float angle)
        {
            return (float)Math.Tan(angle);
        }
        #endregion

        #region Math - Random
        // noise()
        // noiseDetail()
        // noiseSeed()

        public float random(float max)
        {
            return (float)_rand.NextDouble() * max;
        }

        public float random(float min, float max)
        {
            return min + (float)_rand.NextDouble() * (max - min);
        }

        public float randomGaussian()
        {
            return (float)Utils.NextGaussian(_rand);
        }

        public void randomSeed(int seed)
        {
            _rand = new Random(seed);
        }
        #endregion
        #endregion
    }

    #region Map Processing objects to native
    /// <summary>
    /// Renamed color to PColor because of collision with color() function.
    /// </summary>
    public class PColor
    {
        public Color NativeColor { get; } = Color.Black;

        public PColor(int r, int g, int b)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            NativeColor = Color.FromArgb(r, g, b);
        }

        public PColor(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            NativeColor = Color.FromArgb(a, r, g, b);
        }

        public PColor(string hex) // like #RRVVBB or 0xAARRVVBB
        {
            string s = hex.Replace("#", "").Replace("0x", "");
            NativeColor = Color.FromArgb(int.Parse(s));
        }

        public PColor(int gray)
        {
            gray = Utils.Constrain(gray, 0, 255);
            NativeColor = Color.FromArgb(gray, gray, gray);
        }

        public PColor(int gray, int a)
        {
            gray = Utils.Constrain(gray, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            NativeColor = Color.FromArgb(a, gray, gray, gray);
        }

        public PColor(Color native)
        {
            NativeColor = native;
        }
    }

    /// <summary>
    /// Map image.
    /// </summary>
    public class PImage
    {
        Bitmap _bmp;

        public int width
        {
            get { return _bmp.Width; }
        }

        public int height
        {
            get { return _bmp.Height; }
        }

        public PImage(string fname)
        {
            _bmp = new Bitmap(fname);
        }

        public PImage(Bitmap bm)
        {
            _bmp = bm;
        }

        public Bitmap image()
        {
            return _bmp;
        }

        public PColor getPixel(int x, int y)
        {
            return new PColor(_bmp.GetPixel(x, y));
        }

        public PImage getSubImage(int x, int y, int width, int height)
        {
            return new PImage(_bmp.Clone(new Rectangle(x, y, width, height), _bmp.PixelFormat));
        }

        public void setArea(int x, int y, PColor color)
        {
            _bmp.SetPixel(x, y, color.NativeColor);
        }

        public void setArea(int x, int y, PImage img)
        {
            Graphics.FromImage(_bmp).DrawImageUnscaled(img.image(), x, y);
        }

        public void resize(int width, int height)
        {
            if (width == 0) // proportional
            {
                width = this.width * height / this.height;
            }
            else if (height == 0) // proportional
            {
                height = this.height * width / this.width;
            }
            Bitmap bmap = new Bitmap(width, height);
            Graphics.FromImage(bmap).DrawImage(_bmp, 0, 0, width, height);
            _bmp = bmap;
        }
    }

    /// <summary>
    /// Map font.
    /// </summary>
    public class PFont
    {
        public Font NativeFont { get; } = null;

        public PFont(string name, int size)
        {
            NativeFont = new Font(name, size, GraphicsUnit.Pixel);
        }
    }
    #endregion
}
