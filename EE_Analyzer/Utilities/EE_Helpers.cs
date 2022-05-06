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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <param name="isParallel"></param>
        /// <returns></returns>
        public static Point3d FindPointOfIntersectLines_2D(Line l1, Line l2)
        {
            var A1 = l1.EndPoint.Y - l1.StartPoint.Y;
            var B1 = l1.EndPoint.X - l1.StartPoint.X;
            var C1 = (A1 * l1.StartPoint.X + B1 * l1.StartPoint.Y);

            var A2 = l2.EndPoint.Y - l2.StartPoint.Y;
            var B2 = l2.EndPoint.X - l2.StartPoint.X;
            var C2 = (A2 * l2.StartPoint.X + B2 * l2.StartPoint.Y);

            // compute the determinant
            var delta = A1 * B2 - A2 * B1;
            double intX, intY;

            
            if (delta == 0)
            {
                // Lines are parallel, but are they the same line?z
                intX = double.MaxValue;
                intY = double.MaxValue;
            }
            else
            {
                intX = (B2 * C1 - B1 * C2) / delta;
                intY = (A1 * C2 - A2 * C1) / delta;

            }
            return new Point3d(intX, intY, 0);
        }

        public static Point3d FindPointOfIntersectLines_FromPoint3d(Point3d A1, Point3d A2, Point3d B1, Point3d B2)
        {
            Line l1 = new Line(A1, A2);
            Line l2 = new Line(B1, B2);

            // If the two lines are collinear, return the average of B1 and B2
            if(PtsAreColinear_2D(A1, A2, B1) && PtsAreColinear_2D(A1, A2, B2))
            {
                return new Point3d(0.5 * (B1.X + B2.X), 0.5 * (B1.Y + B2.Y), 0.5 * (B1.Z + B2.Z));
                
            } else
            {
                Point3d point = FindPointOfIntersectLines_2D(l1, l2);
                return point;
            }
        }

        /// <summary>
        /// Function to determine if three point3d objects are colinear
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static bool PtsAreColinear_2D(Point3d p1, Point3d p2, Point3d p3)
        {
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            double x3 = p3.X;
            double y3 = p3.Y;

            /* Calculation the area of 
            triangle. We have skipped
            multiplication with 0.5 to
            avoid floating point computations */
            double a = x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2);

            if (a == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static double GetSlopeOfPts(Point3d p1, Point3d p2)
        {
            return (Math.Atan((p2.Y - p1.Y) / (p2.X - p1.X)));
        }

        //public static Line TrimLineToPolyline(Line ln, Polyline pl)
        //{
        //    // find segment of polyline that brackets the line to be trimmed

        //    // Test the segments of the polyline to find the 

        //    Line testline = new Line(pl.GetPoint3dAt(0), pl.GetPoint3dAt(1));

        //    bool isParallel = false;
        //    Point3d newPt = FindPointOfIntersectLines_2D(ln, testline, out isParallel);
        //    if(isParallel == false{
        //        ln.StartPoint = newPt;
        //        return ln;
        //    }
        //}
    }
}
