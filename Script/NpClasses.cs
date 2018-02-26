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
    /// <summary>
    /// Map Processing color class to native.
    /// </summary>
    public class color
    {
        public Color NativeColor { get; set; } = Color.Black;

        public color(int r, int g, int b)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            NativeColor = Color.FromArgb(r, g, b);
        }

        public color(int r, int g, int b, int a)
        {
            r = Utils.Constrain(r, 0, 255);
            g = Utils.Constrain(g, 0, 255);
            b = Utils.Constrain(b, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            NativeColor = Color.FromArgb(a, r, g, b);
        }

        public color(string hex) // like #RRVVBB or 0xAARRVVBB
        {
            string s = hex.Replace("#", "").Replace("0x", "");
            NativeColor = Color.FromArgb(int.Parse(s));
        }

        public color(int gray)
        {
            gray = Utils.Constrain(gray, 0, 255);
            NativeColor = Color.FromArgb(gray, gray, gray);
        }

        public color(int gray, int a)
        {
            gray = Utils.Constrain(gray, 0, 255);
            a = Utils.Constrain(a, 0, 255);
            NativeColor = Color.FromArgb(a, gray, gray, gray);
        }

        public color(Color native)
        {
            NativeColor = native;
        }
    }

    /// <summary>
    /// Map Processing PImage class to native.
    /// </summary>
    public class PImage
    {
        Bitmap _bmp;

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

        // Added:
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
