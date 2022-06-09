﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

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
        /// Sort an array of point3d[] by X value from smallest X to largest X
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static Point3d[] sortPoint3dListByHorizontally(List<Point3d> lst)
        {
            Point3d[] sort_arr = lst.ToArray();
            Point3d temp;

            for (int j = 0; j < lst.Count - 1; j++)
            {
                for (int i = 0; i < lst.Count - 1; i++)
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
        /// Sort an array of point3d[] by X value from smallest Y to largest Y
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static Point3d[] sortPoint3dListByVertically(List<Point3d> lst)
        {
            Point3d[] sort_arr = lst.ToArray();
            Point3d temp;

            for (int j = 0; j < lst.Count - 1; j++)
            {
                for (int i = 0; i < lst.Count - 1; i++)
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

        /// <summary>
        /// Finds all of the intersection points of a line segment crossing a closed polygon
        /// </summary>
        /// <param name="b1">first point on the line segment</param>
        /// <param name="b2">second point on the line segment</param>
        /// <param name="poly">the closed polyline to evaluate</param>
        /// <returns>A point3d[] array of the points sorted from lowest X to highest X or from lowest Y to highest Y</returns>
        /// <exception cref="System.Exception"></exception>
        public static Point3d[] TrimAndSortIntersectionPoints(Point3d b1, Point3d b2, Polyline poly)
        {
            int numVerts = poly.NumberOfVertices;

            List<Point3d> beam_points = new List<Point3d>();

            for (int i = 0; i < numVerts; i++)
            {
                try
                {
                    // Get the ends of the interior current polyline segment
                    Point3d p1 = poly.GetPoint3dAt(i % numVerts);
                    Point3d p2 = poly.GetPoint3dAt((i + 1) % numVerts);

                    //if (p1 == b1 || p1 == b2)
                    //{
                    //    beam_points.Add(p1);
                    //    continue;
                    //}
                    //if (p2 == b1 || p2 == b2)
                    //{
                    //    beam_points.Add(p2);
                    //    continue;
                    //}

                    double dist = MathHelpers.Distance3DBetween(p1, p2);

                    Point3d grade_beam_intPt;
                    grade_beam_intPt = FindPointOfIntersectLines_FromPoint3d(
                        b1,
                        b2,
                        p1,
                        p2);

                    if (grade_beam_intPt == null)
                    {
                        continue;
                    }
                    else
                    {
                        //double slope1_line_segment = EE_Helpers.GetSlopeOfPts(b1, b2);
                        //double slope2_line_segment = EE_Helpers.GetSlopeOfPts(b2, b1);
                        //double slope_polyline_segment = EE_Helpers.GetSlopeOfPts(p1, p2);
                        //// if the slope of the two line segments are parallel and the X or Y coordinates match, add the intersection as the average of the two polyline segment end points 
                        //if ((slope1_line_segment == slope_polyline_segment) || (slope2_line_segment == slope_polyline_segment))
                        //{
                        //    // if the vertices of the polyline are on the line segment
                        //    //     (vertical segment test)       ||      (horizontal segment test)
                        //    if ((b1.X == p1.X && b1.X == p2.X && b2.X == p1.X && b2.X == p2.X)
                        //        || (b1.Y == p1.Y && b1.Y == p2.Y && b2.Y == p1.Y && b2.Y == p2.Y))
                        //    {
                        //        // assign the midpoint of the polyline segment as the intersection point
                        //        beam_points.Add(new Point3d(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y), 0));
                        //        continue;
                        //    }
                        //}

                        // If the first point is exactly a vertex point, add it to the list
                        if (p1 == b1 || p1 == b2)
                        {
                            beam_points.Add(p1);
                            continue;
                        } else
                        {
                            // If the distance from the intPt to both p1 and P2 is less than the distance between p1 and p2
                            // the intPT must be between P1 and P2 
                            if ((MathHelpers.Distance3DBetween(grade_beam_intPt, p1) <= dist) && (MathHelpers.Distance3DBetween(grade_beam_intPt, p2) <= dist))
                            {
                                beam_points.Add(grade_beam_intPt);
                            }
                        }

                    }
                }
                catch (System.Exception e)
                {
                    return null;
                }
            }

            try
            {
                if (beam_points is null)
                {
                    return null;
                }
                else if (beam_points.Count < 2)
                {
                    if (beam_points.Count == 0)
                    {
                        //MessageBox.Show("No intersection points found");
                        return null;
                    }
                    else
                    {
                        //MessageBox.Show(beam_points.Count.ToString() + " intersection point found at " + beam_points[0].X + " , " + beam_points[0].Y);
                        Point3d[] sorted_points = new Point3d[beam_points.Count];
                        beam_points.CopyTo(sorted_points, 0);
                        return sorted_points;
                    }
                }
                else
                {
                    try
                    {
                        Point3d[] sorted_points = new Point3d[beam_points.Count];

                        // If the point is horizontal
                        if (Math.Abs(beam_points[1].Y - beam_points[0].Y) < EE_Settings.DEFAULT_HORIZONTAL_TOLERANCE)
                        {
                            sorted_points = sortPoint3dListByHorizontally(beam_points);
                        }
                        // Otherwise it is vertical
                        else
                        {
                            sorted_points = sortPoint3dListByVertically(beam_points);
                        }

                        if (sorted_points is null)
                        {
                            throw new System.Exception("\nError sorting the intersection points in TrimLines method.");
                        }

                        return sorted_points;
                    }
                    catch (System.Exception e)
                    {
                        //MessageBox.Show("\nError finding sorted intersection points");
                        return null;
                    }

                }
            }
            catch (System.Exception e)
            {
                //MessageBox.Show("\nError in TrimAndSortIntersectionPoints function");
                return null;
            }

            return null;

        }
    }
}
