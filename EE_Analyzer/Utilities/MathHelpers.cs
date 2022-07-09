using Autodesk.AutoCAD.Geometry;
using System;

namespace EE_Analyzer.Utilities
{
    public static class MathHelpers
    {
        /// <summary>
        /// Determine the magnitude (length) of a 3d vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double Magnitude(Vector3d v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        /// <summary>
        /// Create a unit vector from a Vector3d
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3d Normalize(Vector3d v)
        {
            var length = Magnitude(v);

            return (new Vector3d(v.X / length, v.Y / length, v.Z / length));
        }

        /// <summary>
        /// Returns a cross product C = A x B
        /// </summary>
        /// <param name="a">Vector A</param>
        /// <param name="b">Vector B</param>
        /// <returns></returns>
        public static Vector3d CrossProduct(Vector3d a, Vector3d b)
        {
            double i_coeff = a.Y * b.Z - a.Z * b.Y;
            double j_coeff = (-1.0) * (a.X * b.Z - a.Z * b.X);
            double k_coeff = a.X * b.Y - a.Y * b.X;

            return new Vector3d(i_coeff, j_coeff, k_coeff);
        }

        /// <summary>
        /// Computes a dot product or vector projection of A onto nonzero vector b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double DotProduct(Vector3d a, Vector3d b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// Returns a Point3d coordinate from a vector offset from a point.
        /// </summary>
        /// <param name="p0">Base point</param>
        /// <param name="offset">Vector offset</param>
        /// <returns></returns>
        public static Point3d Point3dFromVectorOffset(Point3d p0, Vector3d offset)
        {
            return (p0 + offset);
        }

        /// <summary>
        /// Find the planar distance between two Point2d points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance2DBetween(Point2d p1, Point2d p2)
        {
            Vector3d v = new Vector3d(p2.X - p1.X, p2.Y - p1.Y, 0.0);
            return Magnitude(v);
        }

        /// <summary>
        /// Find the planar distance between two Point2d points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance3DBetween(Point3d p1, Point3d p2)
        {
            Vector3d v = new Vector3d(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Magnitude(v);
        }

        public static Point2d Point2dTransformByAngle(Point2d p1, double angle)
        {
            double vx = p1.X * Math.Cos(angle) + p1.Y * Math.Sin(angle);
            double vy = -p1.X * Math.Sin(angle) + p1.Y * Math.Cos(angle);

            return new Point2d(vx, vy);
        }

        public static Point3d GetMidpoint(Point3d p1, Point3d p2)
        {
            Point3d p = new Point3d(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y), 0.5 * (p1.Z + p2.Z));
            return p;
        }
    }
}
