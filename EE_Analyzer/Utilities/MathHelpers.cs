using Autodesk.AutoCAD.Geometry;
using System;

namespace EE_Analyzer.Utilities
{
    public static class MathHelpers
    {
        public static Vector3d Normalize(Vector3d v)
        {
            var length = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            
            return (new Vector3d(v.X / length, v.Y / length, v.Z / length));
        }
    }
}
