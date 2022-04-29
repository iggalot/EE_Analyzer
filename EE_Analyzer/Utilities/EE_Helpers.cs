using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace EE_Analyzer.Utilities
{
    public static class EE_Helpers
    {
        public static string DisplayPrint3DCollection(Point3dCollection coll)
        {
            string str = "";
            foreach (Point3d point in coll)
            {
                str += point.X + " , " + point.Y + "\n";
            }
            return str;
        }

        /// <summary>
        /// Bubble sort for x-direction of Point3dCollection
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static Point3d[] sortPoint3dByHorizontally(Point3dCollection coll)
        {
            Point3d[] sort_arr = new Point3d[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3d temp;

            for (int j = 0; j < coll.Count - 1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].X > sort_arr[i + 1].X)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Bubble sort for Point3d of y-direction
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static Point3d[] sortPoint3dByVertically(Point3dCollection coll)
        {
            Point3d[] sort_arr = new Point3d[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3d temp;

            for (int j = 0; j < coll.Count - 1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].Y > sort_arr[i + 1].Y)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        public static Point3dCollection IntersectionPointsOnPolyline(Line ln, Polyline pline)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var points = new Point3dCollection();

            var curves = ln;
            for (int i = 0; i < curves.Length - 1; i++)
            {
                curves.IntersectWith(pline, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
            }
            return points;
        }

        // Find the point where two line segments intersect
        public static Point3d FindPointOfIntersectLines_2D(Line l1, Line l2)
        {
            var A1 = -(l1.EndPoint.Y - l1.StartPoint.Y);
            var B1 = l1.EndPoint.X - l1.StartPoint.X;
            var C1 = (A1 * l1.StartPoint.X + B1 * l1.StartPoint.Y);

            var A2 = -(l2.EndPoint.Y - l2.StartPoint.Y);
            var B2 = l2.EndPoint.X - l2.StartPoint.X;
            var C2 = (A2 * l2.StartPoint.X + B2 * l2.StartPoint.Y);

            var delta = A1 * B2 - A2 * B1;
            var intX = (B2 * C1 - B1 * C2) / delta;
            var inty = (A1 * C2 - A2 * C1) / delta;

            return new Point3d(intX, inty, 0);
        }

        public static Line TrimLineToPolyline(Line ln, Polyline pl)
        {
            // find segment of polyline that brackets the line to be trimmed

            // Test the segments of the polyline to find the 

            Line testline = new Line(pl.GetPoint3dAt(0), pl.GetPoint3dAt(1));

            Point3d newPt = FindPointOfIntersectLines_2D(ln, testline);
            ln.StartPoint = newPt;
            return ln;
        }
    }
}
