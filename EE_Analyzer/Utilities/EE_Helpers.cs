using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Windows;
using static EE_Analyzer.Utilities.DrawObject;


namespace EE_Analyzer.Utilities
{
    /// <summary>
    /// Class object for data relating to finding intersection points of lines with polylines.
    /// Mostly used for passing back full data.
    /// </summary>
    public class IntersectPointData
    {
        public Point3d Point;
        public bool isParallel;
        public bool isWithinSegment;
        public string logMessage = "";
    }

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
        public static Point3d[] sortPoint3dPointCollectionByHorizontally(Point3dCollection coll)
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
        public static Point3d[] sortPoint3dCollectionByVertically(Point3dCollection coll)
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

        /// <summary>
        /// Sorts a list points on a horizontal line by smallest x to largest x coordinate or
        /// on a vertical line by smallest y to largest y coordinate
        /// </summary>
        /// <param name="lst">a list points</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public static Point3d[] SortPointsHorizontallyOrVertically(List<Point3d> lst)
        {
            Point3d[] sorted_list;
            // If the point is horizontal
            if (Math.Abs(lst[1].Y - lst[0].Y) < EE_FDN_Settings.DEFAULT_HORIZONTAL_TOLERANCE)
            {
                sorted_list = sortPoint3dListByHorizontally(lst);
            }
            // Otherwise it is vertical
            else
            {
                sorted_list = sortPoint3dListByVertically(lst);
            }

            if (sorted_list is null)
            {
                throw new System.Exception("\nError sorting the intersection points in TrimLines method.");
            }

            return sorted_list;
        }


        public static List<Point3d> FindPolylineIntersectionPoints(Line ln, Polyline poly)
        {
            if (poly == null || poly.Closed == false)
            {
                throw new InvalidOperationException("Invalid polyline object.  Make sure the polyline is closed and has at least 2 vertices.");
            }
            int numVerts = poly.NumberOfVertices;

            if (ln == null)
            {
                throw new InvalidOperationException("Invalid line object.");
            }

            Point3d b1 = ln.StartPoint;
            Point3d b2 = ln.EndPoint;

            List<Point3d> intPtList = new List<Point3d>();

            for (int i = 0; i < numVerts; i++)
            {
                Point3d p1 = poly.GetPoint3dAt(i % numVerts);
                Point3d p2 = poly.GetPoint3dAt((i + 1) % numVerts);

                //Determine if the intersection point is a valid point within the polyline segment.
                IntersectPointData intersectPointData = (EE_Helpers.FindPointOfIntersectLines_FromPoint3d(b1, b2, p1, p2));

                if (intersectPointData == null)
                    continue;

                Point3d intPt = intersectPointData.Point;

                if (intersectPointData.isParallel is true)
                {
                    continue;
                }
                else
                {
                    if (intersectPointData.isWithinSegment is true)
                    {
                        intPtList.Add(intersectPointData.Point);
                    }
                }
            }

            return intPtList;
        }

        /// <summary>
        /// Find the location where two line segements intersect
        /// </summary>
        /// <param name="l1">autocad line object #1</param>
        /// <param name="l2">autocad line objtxt #2</param>
        /// <param name="withinSegment">The coordinate must be within the line segments</param>
        /// <param name="areParallel">returns if the lines are parallel. This needs to be checked everytime as the intersection point defaults to a really large value otherwise</param>
        /// <returns></returns>
        public static IntersectPointData FindPointOfIntersectLines_2D(Line l1, Line l2)
        {
            double tol = 0.001;  // a tolerance fudge factor since autocad is having issues with rounding at the 9th and 10th decimal place
            double A1 = l1.EndPoint.Y - l1.StartPoint.Y;
            double A2 = l2.EndPoint.Y - l2.StartPoint.Y;
            double B1 = l1.StartPoint.X - l1.EndPoint.X;
            double B2 = l2.StartPoint.X - l2.EndPoint.X;
            double C1 = A1 * l1.StartPoint.X + B1 * l1.StartPoint.Y;
            double C2 = A2 * l2.StartPoint.X + B2 * l2.StartPoint.Y;

            // compute the determinant
            double det = A1 * B2 - A2 * B1;

            double intX, intY;

            IntersectPointData intPtData = new IntersectPointData();
            intPtData.isParallel = LinesAreParallel(l1, l2);

            if (intPtData.isParallel is true)
            {
                // Lines are parallel, but are they the same line?
                intX = double.MaxValue;
                intY = double.MaxValue;
                intPtData.isWithinSegment = false; // cant intersect if the lines are parallel
                //MessageBox.Show("segment is parallel");
                //MessageBox.Show("A1: " + A1 + "\n" + "  B1: " + B1 + "\n" + "  C1: " + C1 + "\n" +
                //    "A2: " + A2 + "\n" + "  B2: " + B2 + "\n" + "  C2: " + C2 + "\n" +
                //    "delta: " + delta);
            }
            else
            {
                intX = (B2 * C1 - B1 * C2) / det;
                intY = (A1 * C2 - A2 * C1) / det;

                intPtData.isWithinSegment = true;
                string msg = "";
                //// Check that the intersection point is between the endpoints of both lines assuming it isnt
                if (((Math.Min(l1.StartPoint.X, l1.EndPoint.X) - tol <= intX) && (Math.Max(l1.StartPoint.X, l1.EndPoint.X) + tol >= intX)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 1 X - failed";
                }
                else if (((Math.Min(l2.StartPoint.X, l2.EndPoint.X) - tol <= intX) && (Math.Max(l2.StartPoint.X, l2.EndPoint.X) + tol >= intX)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 2 X - failed";

                }
                else if (((Math.Min(l1.StartPoint.Y, l1.EndPoint.Y) - tol <= intY) && (Math.Max(l1.StartPoint.Y, l1.EndPoint.Y) + tol >= intY)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 3 X - failed";

                }
                else if (((Math.Min(l2.StartPoint.Y, l2.EndPoint.Y) - tol <= intY) && (Math.Max(l2.StartPoint.Y, l2.EndPoint.Y) + tol >= intY)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 4 X - failed";

                }
                else
                {
                    intPtData.isWithinSegment = true;
                    msg += "intersection point is within line segment limits";

                }
            }

            intPtData.Point = new Point3d(intX, intY, 0);

            return intPtData;
        }

        public static IntersectPointData FindPointOfIntersectLines_FromPoint3d(Point3d A1, Point3d A2, Point3d B1, Point3d B2)
        {
            // Check if the points are the same -- usually occurs when one line is comparing to itself
            if (A1 == B1 && A2 == B2)
            {
                return null;
            }

            Line l1, l2;
            if (A1.X < A2.X)
            {
                l1 = new Line(A1, A2);
            }
            else
            {
                l1 = new Line(A2, A1);
            }

            if (B1.X < B2.X)
            {
                l2 = new Line(B1, B2);
            }
            else
            {
                l2 = new Line(B2, B1);
            }

            return FindPointOfIntersectLines_2D(l1, l2);
        }

        /// <summary>
        /// Function to determine if three point3d objects are colinear
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static bool PointtsAreColinear_2D(Point3d p1, Point3d p2, Point3d p3)
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

        public static bool LinesAreParallel(Line l1, Line l2)
        {
            double A1 = l1.EndPoint.Y - l1.StartPoint.Y;
            double A2 = l2.EndPoint.Y - l2.StartPoint.Y;
            double B1 = l1.StartPoint.X - l1.EndPoint.X;
            double B2 = l2.StartPoint.X - l2.EndPoint.X;
            double C1 = A1 * l1.StartPoint.X + B1 * l1.StartPoint.Y;
            double C2 = A2 * l2.StartPoint.X + B2 * l2.StartPoint.Y;

            double det = A1 * B2 - A2 * B1;
            return det == 0;
        }

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

            //MessageBox.Show("Polyline has " + numVerts.ToString() + " vertices");
            for (int i = 0; i < numVerts; i++)
            {
                try
                {
                    // Get the ends of the interior current polyline segment
                    Point3d p1 = poly.GetPoint3dAt(i % numVerts);
                    Point3d p2 = poly.GetPoint3dAt((i + 1) % numVerts);
                    //DrawCircle(p1, EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

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

                    IntersectPointData intersectPtData = FindPointOfIntersectLines_FromPoint3d(
                        b1,
                        b2,
                        p1,
                        p2
                        );

                    if (intersectPtData == null)
                        continue;

                    grade_beam_intPt = intersectPtData.Point;

                    if (grade_beam_intPt == null)
                    {
                        //MessageBox.Show("No intersection point found");
                        continue;
                    }
                    else
                    {
                        //DrawCircle(grade_beam_intPt, EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

                        //MessageBox.Show("-- intersection point found");

                        //                      beam_points.Add(grade_beam_intPt);

                        double slope1_line_segment = EE_Helpers.GetSlopeOfPts(b1, b2);
                        double slope2_line_segment = EE_Helpers.GetSlopeOfPts(b2, b1);
                        double slope_polyline_segment = EE_Helpers.GetSlopeOfPts(p1, p2);
                        // if the slope of the two line segments are parallel and the X or Y coordinates match, add the intersection as the average of the two polyline segment end points 
                        if ((slope1_line_segment == slope_polyline_segment) || (slope2_line_segment == slope_polyline_segment))
                        {
                            // if the vertices of the polyline are on the line segment
                            //     (vertical segment test)       ||      (horizontal segment test)
                            if ((b1.X == p1.X && b1.X == p2.X && b2.X == p1.X && b2.X == p2.X)
                                || (b1.Y == p1.Y && b1.Y == p2.Y && b2.Y == p1.Y && b2.Y == p2.Y))
                            {
                                // add both points to the list
                                beam_points.Add(p1);
                                beam_points.Add(p2);
                                //                                // assign the midpoint of the polyline segment as the intersection point
                                //                                beam_points.Add(new Point3d(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y), 0));
                                continue;
                            }
                        }

                        // If the first point is exactly a vertex point, add it to the list
                        // We wont do it for the second point as it should only be assigned to one segment
                        if (p1 == b1 || p1 == b2)
                        {
                            beam_points.Add(p1);
                            continue;
                        }
                        else
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
                    MessageBox.Show("------------------ERRROR---------------------");
                    return null;
                }
            }

            foreach (var p in beam_points)
            {
                DrawCircle(p, EE_FDN_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_FDN_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER);
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
                        if (Math.Abs(beam_points[1].Y - beam_points[0].Y) < EE_FDN_Settings.DEFAULT_HORIZONTAL_TOLERANCE)
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
        }
    }
}
