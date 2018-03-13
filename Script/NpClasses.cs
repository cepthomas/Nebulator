using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using Nebulator.Common;

// Processing API stuff.

namespace Nebulator.Script
{

    public class ColorModeX
    {
        // colorMode(HSB, 360, 100, 100);
        // colorMode(RGB, 100);
        // 
        // mode    int: Either RGB or HSB, corresponding to Red/Green/Blue and Hue/Saturation/Brightness
        // max float: range for all color elements --- if just one
        // max1    float: range for the red or hue depending on the current color mode
        // max2    float: range for the green or saturation depending on the current color mode
        // max3    float: range for the blue or brightness depending on the current color mode

        /// <summary>Color mode: RGB or HSB. Internal so Surface can access./summary>
        public int mode = ScriptCore.RGB;
        public float max1 = 0;
        public float max2 = 0;
        public float max3 = 0;
        public float maxA = 0;

        /// <summary>Current global color mode. Internal so Surface can access./summary>
        internal static ColorModeX ColorMode { get; set; } = new ColorModeX();
    }


    /// <summary>
    /// Map Processing color to native. Processing uses a 32 bit value as color - this uses a class. TODO ???
    /// Note that .NET calls it HSV but is actually HSL so don't use the Color.GetHue() etc functions.
    /// https://blogs.msdn.microsoft.com/cjacks/2006/04/12/converting-from-hsb-to-rgb-in-net/
    /// </summary>
    public class color
    {
        public float Hue { get; private set; } = 0;
        public float Saturation { get; private set; } = 0;
        public float Brightness { get; private set; } = 0;

        public int R { get; private set; } = 0;
        public int G { get; private set; } = 0;
        public int B { get; private set; } = 0;
        public int A { get; private set; } = 0;

        public Color NativeColor
        {
            get { return Color.FromArgb(A, R, G, B); }
            set { FromNative(value); }
        }

        public color(float v1, float v2, float v3, float a = 255)
        {
            if(ColorModeX.ColorMode.mode == ScriptCore.HSB)
            {
                FromHSB(v1, v2, v3, a);
            }
            else // RGB
            {
                FromRGB((int)v1, (int)v2, (int)v3, (int)a);
            }
        }

        public color(string hex) // like "#RRVVBB" or "0xAARRVVBB"
        {
            string s = hex.Replace("#", "").Replace("0x", "");
            FromARGB(Convert.ToInt32(s, 16)); // or int.Parse("3A", NumberStyles.HexNumber)
        }

        public color(float gray, float a = 255)
        {
            FromRGB((int)gray, (int)gray, (int)gray, (int)a);
        }

        public color(Color native)
        {
            NativeColor = native;
        }

        #region Converters
        void FromHSB(float h, float s, float b, float a)
        {
            // Normalize input values.
            Hue = (float)Utils.Map(h, 0, ColorModeX.ColorMode.max1, 0, 1.0);
            Saturation = (float)Utils.Map(s, 0, ColorModeX.ColorMode.max2, 0, 1.0);
            Brightness = (float)Utils.Map(b, 0, ColorModeX.ColorMode.max3, 0, 1.0);
            A = (int)Utils.Map(a, 0, ColorModeX.ColorMode.maxA, 0, 255);

            // Convert them.
            R = G = B = (int)(Brightness * 255.0f + 0.5f);

            if (Saturation != 0)
            {
                float hv = (Hue - (float)Math.Floor(Hue)) * 6.0f;
                float f = hv - (float)Math.Floor(hv);
                float p = Brightness * (1.0f - Saturation);
                float q = Brightness * (1.0f - Saturation * f);
                float t = Brightness * (1.0f - (Saturation * (1.0f - f)));

                switch ((int)hv)
                {
                    case 0:
                        G = (int)(t * 255.0f + 0.5f);
                        B = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        R = (int)(q * 255.0f + 0.5f);
                        B = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        R = (int)(p * 255.0f + 0.5f);
                        B = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        R = (int)(p * 255.0f + 0.5f);
                        G = (int)(q * 255.0f + 0.5f);
                        break;
                    case 4:
                        R = (int)(t * 255.0f + 0.5f);
                        G = (int)(p * 255.0f + 0.5f);
                        break;
                    case 5:
                        G = (int)(p * 255.0f + 0.5f);
                        B = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }
        }

        void FromRGB(int r, int g, int b, int a)
        {
            // Normalize input values.
            R = Utils.Map(r, 0, (int)ColorModeX.ColorMode.max1, 0, 255);
            G = Utils.Map(g, 0, (int)ColorModeX.ColorMode.max2, 0, 255);
            B = Utils.Map(b, 0, (int)ColorModeX.ColorMode.max3, 0, 255);
            A = (int)Utils.Map(a, 0, ColorModeX.ColorMode.maxA, 0, 255);

            // Calc corresponding values.
            float cmax = (R > G) ? R : G;
            if (B > cmax)
            {
                cmax = B;
            }

            float cmin = (R < G) ? R : G;
            if (B < cmin)
            {
                cmin = B;
            }

            Brightness = cmax / 255.0f;
            Saturation = cmax != 0 ? (cmax - cmin) / cmax : 0;
            if (Saturation == 0)
            {
                Hue = 0;
            }
            else
            {
                float redc = (cmax - r) / (cmax - cmin);
                float greenc = (cmax - G) / (cmax - cmin);
                float bluec = (cmax - B) / (cmax - cmin);

                if (R == cmax)
                {
                    Hue = bluec - greenc;
                }
                else if (G == cmax)
                {
                    Hue = 2.0f + redc - bluec;
                }
                else
                {
                    Hue = 4.0f + greenc - redc;
                }

                Hue = Hue / 6.0f;
                if (Hue < 0)
                {
                    Hue = Hue + 1.0f;
                }
            }
        }

        void FromARGB(int argb)
        {
            int b = argb >> 00 & 0xFF;
            int g = argb >> 16 & 0xFF;
            int r = argb >> 32 & 0xFF;
            int a = argb >> 48 & 0xFF;

            FromRGB(r, g, b, a);
        }

        void FromNative(Color col)
        {
            FromRGB(col.R, col.G, col.B, col.A);
        }
        #endregion
    }

    /// <summary>
    /// Map Processing PImage class to native.
    /// </summary>
    public class PImage
    {
        Bitmap _bmp;

        //https://www.codeproject.com/Articles/1217543/The-astounding-Pickovers-biomorphs
        //TODO Instead of using the SetPixel method of the Bitmap class, I have used an optimized way by using an array of integers
        //containing the pixel colors. At the end of the process, this data is copied into the Bitmap using a single operation.


        public color[] pixels { get; private set; }
        public int width { get { return _bmp.Width; } }
        public int height { get { return _bmp.Height; } }
        public PImage(string fname) { _bmp = new Bitmap(fname); }
        public PImage(Bitmap bm) { _bmp = bm; }
        public color get(int x, int y) { return new color(_bmp.GetPixel(x, y)); }
        public PImage get(int x, int y, int width, int height) { return new PImage(_bmp.Clone(new Rectangle(x, y, width, height), _bmp.PixelFormat)); }
        public void set(int x, int y, color color) { _bmp.SetPixel(x, y, color.NativeColor); }
        public void set(int x, int y, PImage img) { Graphics.FromImage(_bmp).DrawImageUnscaled(img.image(), x, y); }
        //public bool save(string filename) { NotImpl(nameof(save)); }
        //public void loadPixels() { NotImpl(nameof(loadPixels)); }
        //public void updatePixels() { NotImpl(nameof(updatePixels)); }
        //public void updatePixels(int x, int y, int w, int h) { NotImpl(nameof(updatePixels)); }

        // Added native:
        public Bitmap image() { return _bmp; }

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
    /// Map Processing PFont class to native.
    /// </summary>
    public class PFont
    {
        public Font NativeFont { get; } = null;
        public PFont(string name, int size) { NativeFont = new Font(name, size, GraphicsUnit.Pixel); }
    }

    /// <summary>
    /// Port of Processing java class.
    /// </summary>
    public class Event
    {
        public int getAction() { return _action; }
        public int getModifiers() { return _modifiers; }
        public int getFlavor() { return _flavor; }
        public object getNativeObject() { return _nativeObject; }
        public long getMillis() { return _millis; }
        public bool isShiftDown() { return (_modifiers & SHIFT) != 0; }
        public bool isControlDown() { return (_modifiers & CTRL) != 0; }
        public bool isMetaDown() { return (_modifiers & META) != 0; }
        public bool isAltDown() { return (_modifiers & ALT) != 0; }

        public const int SHIFT = 1 << 0;
        public const int CTRL = 1 << 1;
        public const int META = 1 << 2;
        public const int ALT = 1 << 3;
        public const int KEY = 1;
        public const int MOUSE = 2;
        public const int TOUCH = 3;

        protected object _nativeObject;
        protected long _millis;
        protected int _action;
        protected int _modifiers;
        protected int _flavor;

        public Event(object nativeObject, long millis, int action, int modifiers)
        {
            _nativeObject = nativeObject;
            _millis = millis;
            _action = action;
            _modifiers = modifiers;
        }
    }

    /// <summary>
    /// Port of Processing java class.
    /// </summary>
    public class MouseEvent : Event
    {
        public int getX() { return _x; }
        public int getY() { return _y; }
        public int getButton() { return _button; }
        public int getCount() { return _count; }

        public const int PRESS = 1;
        public const int RELEASE = 2;
        public const int CLICK = 3;
        public const int DRAG = 4;
        public const int MOVE = 5;
        public const int ENTER = 6;
        public const int EXIT = 7;
        public const int WHEEL = 8;

        protected int _x;
        protected int _y;
        protected int _button;
        protected int _count;

        public MouseEvent(object nativeObject, long millis, int action, int modifiers, int x, int y, int button, int count) :
            base(nativeObject, millis, action, modifiers)
        {
            _flavor = MOUSE;
            _x = x;
            _y = y;
            _button = button;
            _count = count;
        }

        public override string ToString()
        {
            string sact = Utils.UNKNOWN_STRING;
            switch (_action)
            {
                case CLICK: sact = "CLICK"; break;
                case DRAG: sact = "DRAG"; break;
                case ENTER: sact = "ENTER"; break;
                case EXIT: sact = "EXIT"; break;
                case MOVE: sact = "MOVE"; break;
                case PRESS: sact = "PRESS"; break;
                case RELEASE: sact = "RELEASE"; break;
                case WHEEL: sact = "WHEEL"; break;
            }

            return $"<MouseEvent {sact}@{_x},{_y} count:{_count} button:{_button}>";
        }
    }
}
