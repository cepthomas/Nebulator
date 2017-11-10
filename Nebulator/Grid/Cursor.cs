using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Nebulator.Grid
{
    /// <summary>Class for storing info about an X axis cursor. Just float right now.</summary>
    public class GridCursor
    {
        /// <summary>The unique ID number associated with this cursor.</summary>
        public int Id { get; private set; }

        /// <summary>The color to be used when drawing this cursor.</summary>
        public Color Color { get; set; }

        /// <summary>Gets or sets the line type.</summary>
        public DashStyle LineType { get; set; }

        /// <summary>The width to be used when drawing this cursor.</summary>
        public float LineWidth { get; set; }

        /// <summary>Where it's located.</summary>
        public float Position { get; set; }

        /// <summary>Ensure unique id numbers.</summary>
        private static int _nextId = 1;

        /// <summary>Default constructor.</summary>
        public GridCursor()
        {
            Id = _nextId++;
            Color = Color.Blue;
            LineType = DashStyle.Solid;
            LineWidth = 1.5f;
            Position = 0.0f;
        }
    }

    class Cursor
    {

        // #region Draw Cursors
        // /// <summary>Draw a chart cursor using the specified graphics.</summary>
        // /// <param name="g">The Graphics object to use.</param>
        // /// <param name="series">The cursor to plot.</param>
        // private void DrawCursor(Graphics g, ChartCursor cursor)
        // {
        //     Pen pen = new Pen(cursor.Color, cursor.LineWidth);
        //     pen.DashStyle = cursor.LineType;

        //     // Shift the point to proper position on client screen.
        //     List<PointF> points = new List<PointF>();

        //     PointF clientPoint = GetClientPoint(new PointF((float)cursor.Position, _yMinScale));
        //     points.Add(clientPoint);
        //     clientPoint = GetClientPoint(new PointF((float)cursor.Position, _yMaxScale));
        //     points.Add(clientPoint);

        //     // Draw the lines for the corrected values.
        //     g.DrawLines(pen, points.ToArray());
        // }

        // /// <summary>Find the closest cursor to the given point.</summary>
        // /// <param name="point">Mouse point</param>
        // /// <returns>The closest cursor or null if not in range.</returns>
        // private ChartCursor GetClosestCursor(Point point)
        // {
        //     ChartCursor closest = null;

        //     foreach (ChartCursor c in _cursors)
        //     {
        //         PointF clientPoint = GetClientPoint(new PointF((float)c.Position, point.Y));
        //         if (Math.Abs(point.X - clientPoint.X) < MOUSE_SELECT_RANGE)
        //         {
        //             closest = c;
        //             break;
        //         }
        //     }

        //     return closest;
        // }
        // #endregion

        // #region Public methods for interacting with the cursors
        // /// <summary>Add a new cursor to the collection.</summary>
        // /// <param name="position"></param>
        // /// <param name="color"></param>
        // /// <param name="lineWidth"></param>
        // /// <returns>Id of the new cursor.</returns>
        // public int AddCursor(float position, Color color, float lineWidth = 1.5f)
        // {
        //     ChartCursor cursor = new ChartCursor()
        //     {
        //         Position = position,
        //         Color = color,
        //         LineWidth = lineWidth
        //     };

        //     _cursors.Add(cursor);

        //     return cursor.Id;
        // }

        // /// <summary>Remove the specified cursor from the collection.</summary>
        // /// <param name="id"></param>
        // public void RemoveCursor(int id)
        // {
        //     var qry = from c in _cursors where c.Id == id select c;

        //     if (qry.Any()) // found it
        //     {
        //         _cursors.Remove(qry.First());
        //     }
        // }

        // /// <summary>Remove all cursors from the collection.</summary>
        // /// <param name="id"></param>
        // public void RemoveAllCursors()
        // {
        //     _cursors.Clear();
        // }

        // /// <summary>Relocate the specified cursor. Client needs to call Refresh() after done.</summary>
        // /// <param name="id"></param>
        // /// <param name="position"></param>
        // public void MoveCursor(int id, float position)
        // {
        //     var qry = from c in _cursors where c.Id == id select c;

        //     if (qry.Any()) // found it
        //     {
        //         qry.First().Position = position;
        //     }
        // }

        // /// <summary>Find the closest cursor to the given point.</summary>
        // /// <param name="point">Mouse point</param>
        // /// <returns>The cursor id or -1 if not in range.</returns>
        // public int CursorHit(Point point)
        // {
        //     int id = -1;

        //     ChartCursor c = GetClosestCursor(point);
        //     if (c != null)
        //     {

        //         id = c.Id;
        //     }

        //     return id;
        // }
        // #endregion




    }
}
