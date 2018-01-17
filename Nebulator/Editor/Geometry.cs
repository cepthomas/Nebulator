using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;


namespace Nebulator.Editor
{
    public enum IntersectType
    {
        /// <summary>Lines are parallel.</summary>
        NoIntersection,
        /// <summary>The two lines intersect.</summary>
        BoundedIntersection,
        /// <summary>The two lines extended would intersect.</summary>
        UnboundedIntersection
    };

    /// <summary>General purpose container for results of these calculations. They mean different things depending on function.</summary>
    public class GeometryResult
    {
        public bool valid = false;
        public float measurement = float.NaN; // distance, ...
        public int pointIndex1 = -1; // one end of Points[]
        public int pointIndex2 = -1; // maybe another end of Points[]
        public PointF closest = new PointF(0, 0);
        public IntersectType intersect = IntersectType.NoIntersection;
    }

    /// <summary>Support for polygon processing.</summary>
    public class PolygonF
    {
        /// <summary>Points in the polygon. A 90 sided polygon is called an enneacontagon and a 10000 sided polygon is called a myriagon.</summary>
        public List<PointF> Points { get; set; } = new List<PointF>();

        /// <summary>Can be a polyline too.</summary>
        public bool Closed { get; set; } = true;


        /*** TODOG some or all of these::::::::::::::::
        /// <summary>Border shape types for tagging indication.</summary>
        public enum BorderShapeType { None = 0, Circle = 1, Square = 2, Triangle = 3 }

        /// <summary>Shape types for displaying a single point.</summary>
        public enum PointShapeType { Dot = 0, Circle = 1, Square = 2, Triangle = 3, X = 4, Asterix = 5, Plus = 6, Minus = 7, Smiley = 8 }

        /// <summary>Color used when drawing point. Fill?</summary>
        public Color PointColor { get; set; } = Color.Black;

        /// <summary>PointShape used when drawing point.</summary>
        public PointShapeType ShapeType { get; set; } = PointShapeType.Circle;

        /// <summary>BorderShape used when drawing point.</summary>
        public BorderShapeType BorderType { get; set; } = BorderShapeType.None;

        /// <summary>The color to be used when drawing the line.</summary>
        public Color LineColor { get; set; } = Color.Black;

        /// <summary>The color to be used when filling a closed polygon.</summary>
        public Color FillColor { get; set; } = Color.White;

        /// <summary>Gets or sets the line type.</summary>
        public DashStyle LineType { get; set; } = DashStyle.Solid;

        /// <summary>The width to be used when drawing a line.</summary>
        public float LineWidth { get; set; } = 3.0f;

        /// <summary>The width to be used when drawing a Point.</summary>
        public float PointWidth { get; set; } = 2.0f;

        ///<summary>Gets or sets the current x label format specifier</summary>
        public string XFormatSpecifier { get; set; } = ""; // { get { return _xValues.FormatSpecifier; } set { _xValues.FormatSpecifier = value; } }

        ///<summary>Gets or sets the current y label format specifier</summary>
        public string YFormatSpecifier { get; set; } = ""; // { get { return _yValues.FormatSpecifier; } set { _yValues.FormatSpecifier = value; } }

        /// <summary>The readable value for the y point.</summary>
        public string Desc { get; set; } = "";

        /// <summary>The unique id for this point.</summary>
        public int Id { get; set; } = -1;

        /// <summary>The user tag.</summary>
        public object Tag { get; set; } = null;

        /// <summary>For showing/hiding.</summary>
        public bool Hide { get; set; } = false;

        /// <summary>This point has been selected while in the chart, show a different color.</summary>
        public bool Selected { get; set; } = false;
        ***/



        public void Add(PointF p)
        {
            Points.Add(p);
        }

        public void InsertVertex(int where, PointF which) //TODOG
        {
            //int ind = 0;
            //for (int i = 0; i < Points.Count; i++)
            //{
            //    if(where <= Points)
            //}
        }


        /// <summary>
        /// Test if the point is over a corner point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GeometryResult IsCornerPoint(PointF point)
        {
            GeometryResult res = new GeometryResult();

            // See if we're over one of the polygon's corner points.
            for (int i = 0; i < Points.Count; i++)
            {
                // See if we're over this point.
                LineF line = new LineF() { Start = Points[i], End = point };
                if (line.Length < Geometry.HitRange)
                {
                    // We're over this point.
                    res.valid = true;
                    res.pointIndex1 = i;
                    res.closest = point;
                    res.measurement = line.Length;
                }
            }

            return res;
        }

        /// <summary>
        /// Test if the point is over a polygon's edge. Note that edge does include end points.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GeometryResult IsEdgePoint(PointF point)
        {
            GeometryResult res = new GeometryResult();
            res.measurement = float.MaxValue;

            // See if we're over one of the segments.
            for (int pind1 = 0; pind1 < Points.Count; pind1++)
            {
                // Get the index of the polygon's next point.
                int pind2 = (pind1 + 1) % Points.Count;

                // See if we're over the segment between these points.
                LineF line = new LineF() { Start = Points[pind1], End = Points[pind2] };
                GeometryResult dist = line.Distance(point);

                if (dist.measurement < Geometry.HitRange && dist.measurement < res.measurement)
                {
                    // We're over this segment.
                    res.valid = true;
                    res.pointIndex1 = pind1;
                    res.pointIndex2 = pind2;
                    res.closest = dist.closest;
                    res.measurement = dist.measurement;
                }
            }

            return res;
        }

        /// <summary>
        /// Test point in polygon body. Note that this does include end and corner points.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GeometryResult ContainsPoint(PointF point)
        {
            GeometryResult res = new GeometryResult();

            // Make a GraphicsPath representing the polygon.
            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(Points.ToArray());

            // See if the point is inside the GraphicsPath.
            res.valid = path.IsVisible(point);
            return res;
        }

        ///// <summary>
        ///// Find intersection of line with polygon. TODO2 1 to N possible. Figure out when it's needed/useful.
        ///// </summary>
        ///// <param name="l">The line</param>
        ///// <returns></returns>
        //public List<GeometryResult> Intersect(LineF l)
        //{
        //    List<GeometryResult> res = new List<GeometryResult>();

        //    for (int i = 0; i < Points.Count - 1; i++)
        //    {
        //        LineF lt = new LineF() { Start = Points[i], End = Points[i + 1] };
        //        GeometryResult gres = l.Intersect(lt, out res.closest) == IntersectType.BoundedIntersection;
        //    }

        //    return res;
        //}
    }

    /// <summary>Helpful line class.</summary>
    public class LineF
    {
        public PointF Start { get; set; } = new PointF();
        public PointF End { get; set; } = new PointF();

        public float Length
        {
            get
            {
                float x2 = (float)Math.Pow(End.X - Start.X, 2.0);
                float y2 = (float)Math.Pow(End.Y - Start.Y, 2.0);
                return (float)Math.Sqrt(x2 + y2);
            }
            set
            {
                float x = Start.X + (float)Math.Cos(Angle) * value;
                float y = Start.Y + (float)Math.Sin(Angle) * value;
                End = new PointF(x, y);
            }
        }

        /// <summary>
        /// Angle in radians.
        /// </summary>
        public float Angle
        {
            get
            {
                float xDiff = End.X - Start.X;
                float yDiff = End.Y - Start.Y;
                return (float)Math.Atan2(yDiff, xDiff);
            }
            set
            {
                float currentAngle = Angle;
                PointF p = Geometry.RotatePoint(End, Start, value - currentAngle);
                End = p;
            }
        }

        /// <summary>
        /// Adjust one or both ends of the line.
        /// </summary>
        /// <param name="start">Amount to adjust at start point.</param>
        /// <param name="end">Amount to adjust at end point.</param>
        public void Resize(float start, float end)
        {
            float cos = (float)Math.Cos(Angle);
            float sin = (float)Math.Sin(Angle);

            float x = Start.X + cos * start;
            float y = Start.Y + sin * start;
            Start = new PointF(x, y);

            x = End.X + cos * end;
            y = End.Y + sin * end;
            End = new PointF(x, y);
        }

        /// <summary>
        /// Check for intersection of this line with another.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public GeometryResult Intersect(LineF l)
        {
            // http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/

            GeometryResult res = new GeometryResult();
            res.valid = true;

            //// True if the lines containing the segments intersect.
            //bool lines_intersect;
            //// True if the segments intersect.
            //bool segments_intersect;
            //// The point on the first segment that is closest to the point of intersection.
            //PointF close_Start;
            //// The point on the second segment that is closest to the point of intersection.
            //PointF close_End;

            // Get the segment parameters.
            float dx12 = End.X - Start.X;
            float dy12 = End.Y - Start.Y;
            float dx34 = l.End.X - l.Start.X;
            float dy34 = l.End.Y - l.Start.Y;

            // Solve for t1 and t2.
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1 = ((Start.X - l.Start.X) * dy34 + (l.Start.Y - Start.Y) * dx34) / denominator;

            if (float.IsInfinity(t1))
            {
                // The lines are parallel-ish.
                res.intersect = IntersectType.NoIntersection;
                //lines_intersect = false;
                //segments_intersect = false;
                //intersection = new PointF(float.NaN, float.NaN);
                //close_Start = new PointF(float.NaN, float.NaN);
                //close_End = new PointF(float.NaN, float.NaN);
            }
            else
            {
                //lines_intersect = true;

                float t2 = ((l.Start.X - Start.X) * dy12 + (Start.Y - l.Start.Y) * dx12) / -denominator;

                // Find the point of intersection.
                res.closest = new PointF(Start.X + dx12 * t1, Start.Y + dy12 * t1);

                // The segments intersect if t1 and t2 are between 0 and 1.
                res.intersect = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1)) ? IntersectType.BoundedIntersection : IntersectType.UnboundedIntersection;

                //// Find the closest points on the segments. TODOG ?
                //if (t1 < 0)
                //{
                //    t1 = 0;
                //}
                //else if (t1 > 1)
                //{
                //    t1 = 1;
                //}

                //if (t2 < 0)
                //{
                //    t2 = 0;
                //}
                //else if (t2 > 1)
                //{
                //    t2 = 1;
                //}

                //close_Start = new PointF(Start.X + dx12 * t1, Start.Y + dy12 * t1);
                //close_End = new PointF(l.Start.X + dx34 * t2, l.Start.Y + dy34 * t2);
            }

            return res;
        }

        /// <summary>
        /// Distance from the point to this line.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public GeometryResult Distance(PointF pt)
        {
            GeometryResult res = new GeometryResult();

            float dx = End.X - Start.X;
            float dy = End.Y - Start.Y;

            if (dx == 0 && dy == 0)
            {
                // It's a point not a line segment.
                dx = pt.X - Start.X;
                dy = pt.Y - Start.Y;

                res.measurement = (float)Math.Sqrt(dx * dx + dy * dy);
                res.valid = true;
                res.closest = pt;
            }
            else
            {
                // Calculate the t that minimizes the distance.
                float t = ((pt.X - Start.X) * dx + (pt.Y - Start.Y) * dy) / (dx * dx + dy * dy);

                // See if this represents one of the segment's end points or a point in the middle.
                if (t < 0)
                {
                    res.closest = new PointF(Start.X, Start.Y);
                    dx = pt.X - Start.X;
                    dy = pt.Y - Start.Y;
                }
                else if (t > 1)
                {
                    res.closest = new PointF(End.X, End.Y);
                    dx = pt.X - End.X;
                    dy = pt.Y - End.Y;
                }
                else
                {
                    res.closest = new PointF(Start.X + t * dx, Start.Y + t * dy);
                    dx = pt.X - res.closest.X;
                    dy = pt.Y - res.closest.Y;
                }

                res.measurement = (float)Math.Sqrt(dx * dx + dy * dy);
                res.valid = true;
            }

            return res;
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Start:{Start} End:{End} Angle:{Angle} Length:{Length}";
        }
    }

    /// <summary>Geometry utilities.</summary>
    public class Geometry
    {
        /// <summary>The size of an object for mouse purposes.</summary>
        public static float HitRange { get; set; } = 3;

        /// <summary>Rotate a point around center origin.</summary>
        /// <param name="point">Point to rotate.</param>
        /// <param name="angleInRadians">The rotation angle in radians.</param>
        public static PointF RotatePoint(PointF point, float angleInRadians)
        {
            float sin = (float)Math.Sin(angleInRadians);
            float cos = (float)Math.Cos(angleInRadians);

            //x′= x cos⁡θ − y sin⁡θ
            //y′= x sinθ + y cosθ
            float xnew = point.X * cos - point.Y * sin;
            float ynew = point.X * sin + point.Y * cos;

            PointF pnew = new PointF(xnew, ynew);

            return pnew;
        }

        /// <summary>Rotates one point around another.</summary>
        /// <param name="point">Point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInRadians">The rotation angle in radians.</param>
        /// <returns>Rotated point</returns>
        public static PointF RotatePoint(PointF point, PointF centerPoint, float angleInRadians)
        {
            //If you rotate point(px, py) around point(ox, oy) by angle theta you'll get:
            //p'x = cos(theta) * (px-ox) - sin(theta) * (py-oy) + ox
            //p'y = sin(theta) * (px-ox) + cos(theta) * (py-oy) + oy

            float cos = (float)Math.Cos(angleInRadians);
            float sin = (float)Math.Sin(angleInRadians);
            float x = (cos * (point.X - centerPoint.X) - sin * (point.Y - centerPoint.Y) + centerPoint.X);
            float y = (sin * (point.X - centerPoint.X) + cos * (point.Y - centerPoint.Y) + centerPoint.Y);

            return new PointF(x, y);
        }
    }
}
