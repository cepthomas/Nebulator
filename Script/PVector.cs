using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using MoreLinq;
using SkiaSharp;
using Nebulator.Common;


namespace Nebulator.Script
{
    /// <summary>
    /// A port of the Processing PVector class.
    /// </summary>
    public class PVector //implements Serializable
    {
        static Random _rand = new Random();
        protected float[] _array;

        public float x;
        public float y;
        public float z;

        public PVector()
        {
        }

        public PVector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public PVector(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.z = 0;
        }

        public PVector set(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            return this;
        }

        public PVector set(float x, float y)
        {
            this.x = x;
            this.y = y;
            return this;
        }

        public PVector set(PVector v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            return this;
        }

        public PVector set(float[] source)
        {
            if (source.Length >= 2)
            {
                x = source[0];
                y = source[1];
            }

            if (source.Length >= 3)
            {
                z = source[2];
            }

            return this;
        }

        static public PVector random2D(PVector target = null)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);

            if (target == null)
            {
                target = new PVector((float)Math.Cos(angle), (float)Math.Sin(angle), 0);
            }
            else
            {
                target.set((float)Math.Cos(angle), (float)Math.Sin(angle), 0);
            }

            return target;
        }

        static public PVector random3D(PVector target = null)
        {
            float angle;
            float vz;

            angle = (float)(_rand.NextDouble() * Math.PI * 2);
            vz = (float)(_rand.NextDouble() * 2 - 1);

            float vx = (float)(Math.Sqrt(1 - vz * vz) * Math.Cos(angle));
            float vy = (float)(Math.Sqrt(1 - vz * vz) * Math.Sin(angle));

            if (target == null)
            {
                target = new PVector(vx, vy, vz);
                //target.normalize(); // Should be unnecessary
            }
            else
            {
                target.set(vx, vy, vz);
            }

            return target;
        }

        // This is the original:
        //static public PVector random2D()
        //{
        //    return random2D(null, null);
        //}

        //static public PVector random2D(PApplet parent)
        //{
        //    return random2D(null, parent);
        //}

        //static public PVector random2D(PVector target)
        //{
        //    return random2D(target, null);
        //}

        //static public PVector random2D(PVector target, PApplet parent)
        //{

        //    return (parent == null) ?
        //           fromAngle((float)(Math.random() * Math.PI * 2), target) :
        //           fromAngle(parent.random(PConstants.TAU), target);
        //}

        //static public PVector random3D()
        //{
        //    return random3D(null, null);
        //}

        //static public PVector random3D(PApplet parent)
        //{
        //    return random3D(null, parent);
        //}

        //static public PVector random3D(PVector target)
        //{
        //    return random3D(target, null);
        //}

        //static public PVector random3D(PVector target, PApplet parent)
        //{
        //    float angle;
        //    float vz;
        //    if (parent == null)
        //    {
        //        angle = (float)(Math.random() * Math.PI * 2);
        //        vz = (float)(Math.random() * 2 - 1);
        //    }
        //    else
        //    {
        //        angle = parent.random(PConstants.TWO_PI);
        //        vz = parent.random(-1, 1);
        //    }
        //    float vx = (float)(Math.Sqrt(1 - vz * vz) * Math.cos(angle));
        //    float vy = (float)(Math.Sqrt(1 - vz * vz) * Math.sin(angle));
        //    if (target == null)
        //    {
        //        target = new PVector(vx, vy, vz);
        //        //target.normalize(); // Should be unnecessary
        //    }
        //    else
        //    {
        //        target.set(vx, vy, vz);
        //    }
        //    return target;
        //}

        //static public PVector fromAngle(float angle)
        //{
        //    return fromAngle(angle,null);
        //}

        //static public PVector fromAngle(float angle, PVector target)
        //{
        //    if (target == null)
        //    {
        //        target = new PVector((float)Math.Cos(angle),(float)Math.Sin(angle),0);
        //    }
        //    else
        //    {
        //        target.set((float)Math.Cos(angle),(float)Math.Sin(angle),0);
        //    }
        //    return target;
        //}


        public PVector copy()
        {
            return new PVector(x, y, z);
        }

        [Obsolete()]
        public PVector get()
        {
            return copy();
        }

        public float[] get(float[] target)
        {
            if (target == null)
            {
                return new float[] { x, y, z };
            }

            if (target.Length >= 2)
            {
                target[0] = x;
                target[1] = y;
            }

            if (target.Length >= 3)
            {
                target[2] = z;
            }

            return target;
        }

        public float mag()
        {
            return (float) Math.Sqrt(x*x + y*y + z*z);
        }

        public float magSq()
        {
            return (x*x + y*y + z*z);
        }

        public PVector add(PVector v)
        {
            x += v.x;
            y += v.y;
            z += v.z;
            return this;
        }

        public PVector add(float x, float y)
        {
            this.x += x;
            this.y += y;
            return this;
        }

        public PVector add(float x, float y, float z)
        {
            this.x += x;
            this.y += y;
            this.z += z;
            return this;
        }

        static public PVector add(PVector v1, PVector v2)
        {
            return add(v1, v2, null);
        }

        static public PVector add(PVector v1, PVector v2, PVector target)
        {
            if (target == null)
            {
                target = new PVector(v1.x + v2.x,v1.y + v2.y, v1.z + v2.z);
            }
            else
            {
                target.set(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
            }

            return target;
        }

        public PVector sub(PVector v)
        {
            x -= v.x;
            y -= v.y;
            z -= v.z;
            return this;
        }

        public PVector sub(float x, float y)
        {
            this.x -= x;
            this.y -= y;
            return this;
        }

        public PVector sub(float x, float y, float z)
        {
            this.x -= x;
            this.y -= y;
            this.z -= z;
            return this;
        }

        static public PVector sub(PVector v1, PVector v2)
        {
            return sub(v1, v2, null);
        }

        static public PVector sub(PVector v1, PVector v2, PVector target)
        {
            if (target == null)
            {
                target = new PVector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
            }
            else
            {
                target.set(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
            }

            return target;
        }

        public PVector mult(float n)
        {
            x *= n;
            y *= n;
            z *= n;
            return this;
        }

        static public PVector mult(PVector v, float n)
        {
            return mult(v, n, null);
        }

        static public PVector mult(PVector v, float n, PVector target)
        {
            if (target == null)
            {
                target = new PVector(v.x*n, v.y*n, v.z*n);
            }
            else
            {
                target.set(v.x*n, v.y*n, v.z*n);
            }

            return target;
        }

        public PVector div(float n)
        {
            x /= n;
            y /= n;
            z /= n;
            return this;
        }

        static public PVector div(PVector v, float n)
        {
            return div(v, n, null);
        }

        static public PVector div(PVector v, float n, PVector target)
        {
            if (target == null)
            {
                target = new PVector(v.x/n, v.y/n, v.z/n);
            }
            else
            {
                target.set(v.x/n, v.y/n, v.z/n);
            }
            return target;
        }

        public float dist(PVector v)
        {
            float dx = x - v.x;
            float dy = y - v.y;
            float dz = z - v.z;
            return (float) Math.Sqrt(dx*dx + dy*dy + dz*dz);
        }

        static public float dist(PVector v1, PVector v2)
        {
            float dx = v1.x - v2.x;
            float dy = v1.y - v2.y;
            float dz = v1.z - v2.z;
            return (float) Math.Sqrt(dx*dx + dy*dy + dz*dz);
        }

        public float dot(PVector v)
        {
            return x*v.x + y*v.y + z*v.z;
        }

        public float dot(float x, float y, float z)
        {
            return this.x*x + this.y*y + this.z*z;
        }

        static public float dot(PVector v1, PVector v2)
        {
            return v1.x*v2.x + v1.y*v2.y + v1.z*v2.z;
        }

        public PVector cross(PVector v)
        {
            return cross(v, null);
        }

        public PVector cross(PVector v, PVector target)
        {
            float crossX = y * v.z - v.y * z;
            float crossY = z * v.x - v.z * x;
            float crossZ = x * v.y - v.x * y;

            if (target == null)
            {
                target = new PVector(crossX, crossY, crossZ);
            }
            else
            {
                target.set(crossX, crossY, crossZ);
            }

            return target;
        }

        static public PVector cross(PVector v1, PVector v2, PVector target)
        {
            float crossX = v1.y * v2.z - v2.y * v1.z;
            float crossY = v1.z * v2.x - v2.z * v1.x;
            float crossZ = v1.x * v2.y - v2.x * v1.y;

            if (target == null)
            {
                target = new PVector(crossX, crossY, crossZ);
            }
            else
            {
                target.set(crossX, crossY, crossZ);
            }

            return target;
        }

        public PVector normalize()
        {
            float m = mag();
            if (m != 0 && m != 1)
            {
                div(m);
            }

            return this;
        }

        public PVector normalize(PVector target)
        {
            if (target == null)
            {
                target = new PVector();
            }
            float m = mag();
            if (m > 0)
            {
                target.set(x/m, y/m, z/m);
            }
            else
            {
                target.set(x, y, z);
            }
            return target;
        }

        public PVector limit(float max)
        {
            if (magSq() > max*max)
            {
                normalize();
                mult(max);
            }
            return this;
        }

        public PVector setMag(float len)
        {
            normalize();
            mult(len);
            return this;
        }

        public PVector setMag(PVector target, float len)
        {
            target = normalize(target);
            target.mult(len);
            return target;
        }

        public float heading()
        {
            float angle = (float) Math.Atan2(y, x);
            return angle;
        }

        [Obsolete("Use heading().")]
        public float heading2D()
        {
            return heading();
        }

        public PVector rotate(float theta)
        {
           float temp = x;
           // Might need to check for rounding errors like with angleBetween function?
           x = x * (float)(Math.Cos(theta) - y * Math.Sin(theta));
           y = temp * (float)(Math.Sin(theta) + y * Math.Cos(theta));

           return this;
        }

        float lerp(float start, float stop, float amt)
        {
            return start + (stop - start) * amt;
        }

        public PVector lerp(PVector v, float amt)
        {
           x = lerp(x, v.x, amt);
           y = lerp(y, v.y, amt);
           z = lerp(z, v.z, amt);
           return this;
        }

        public static PVector lerp(PVector v1, PVector v2, float amt)
        {
           PVector v = v1.copy();
           v.lerp(v2, amt);
           return v;
        }

        public PVector lerp(float x, float y, float z, float amt)
        {
           this.x = lerp(this.x, x, amt);
           this.y = lerp(this.y, y, amt);
           this.z = lerp(this.z, z, amt);
           return this;
        }

        static public float angleBetween(PVector v1, PVector v2)
        {
           // We get NaN if we pass in a zero vector which can cause problems
           // Zero seems like a reasonable angle between a (0, 0, 0) vector and something else.
           if (v1.x == 0 && v1.y == 0 && v1.z == 0 ) return 0.0f;
           if (v2.x == 0 && v2.y == 0 && v2.z == 0 ) return 0.0f;

           double dot = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
           double v1mag = Math.Sqrt(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
           double v2mag = Math.Sqrt(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
           // This should be a number between -1 and 1, since it's "normalized"
           double amt = dot / (v1mag * v2mag);
           // But if it's not due to rounding error, then we need to fix it
           // http://code.google.com/p/processing/issues/detail?id=340
           // Otherwise if outside the range, acos() will return NaN
           // http://www.cppreference.com/wiki/c/math/acos
           if (amt <= -1)
           {
               return ScriptCore.PI;
           }
           else if (amt >= 1)
           {
               // http://code.google.com/p/processing/issues/detail?id=435
               return 0;
           }

           return (float) Math.Acos(amt);
        }

        public override string ToString()
        {
            return $"x:{x} y:{y} z:{z}";
        }

        public float[] array()
        {
            if (_array == null)
            {
                _array = new float[3];
            }
            _array[0] = x;
            _array[1] = y;
            _array[2] = z;

            return _array;
        }

        public override bool Equals(object obj)
        {
            bool ret = false;
            if (obj is PVector)
            {
                PVector p = obj as PVector;
                ret = x == p.x && y == p.y && z == p.z;
            }
            return ret;
        }

        //public static bool operator ==(PVector v1, PVector v2)
        //{
        //    return (object)v1 != null && (object)v2 != null && v1.Equals(v2);
        //}

        //public static bool operator !=(PVector v1, PVector v2)
        //{
        //    return (object)v1 == null || (object)v2 == null || !v1.Equals(v2);
        //}

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (float f in new float[]{ x, y, z})
            {
                foreach( byte b in BitConverter.GetBytes(f))
                {
                    hash <<= 4;
                    hash &= b;
                }
            }

            return hash;
        }
    }
}