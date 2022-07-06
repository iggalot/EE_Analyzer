using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_Analyzer.Models
{
    public class FoundationObject
    {
        private static int _id = 0;


        // Unit vector for the direction of the grade beam from start node to end node
        public Vector3d VDirection { get; set; } = new Vector3d(1, 0, 0);
        public Vector3d VPerpendicular { get; set; } = new Vector3d(1, 0, 0);
        // Start point for the grade beam
        public Point3d StartPt { get; set; }

        // End point for the grade beam
        public Point3d EndPt { get; set; }

        public Point3d CL_Pt_A { get; set; }
        public Point3d CL_Pt_B { get; set; }

        public double Length { get; set; } = 0;
        public double Angle { get; set; } = 0;

        public Line Centerline { get; set; } = null;
        
        public bool IsTrimmed { get; set; } = false;
        public bool IsHorizontal { get; set; } = true;
        public int Id { get; set; }

        public virtual string Label { get; set; }
        public virtual void AddToAutoCADDatabase(Database db, Document doc) { }

        /// <summary>
        /// Constructor for superclass object
        /// </summary>
        public FoundationObject(Point3d start, Point3d end, double width, bool is_horizontal)
        {

            IsHorizontal = is_horizontal;

            // swap the start point and end point based on lowest X then lowest Y
            bool shouldSwap = false;
            if (is_horizontal)
            {
                if (start.X > end.X)
                {
                    shouldSwap = true;
                }
            }
            else
            {
                if (start.Y > end.Y)
                {
                    shouldSwap = true;
                }
            }

            Point3d temp;
            if (shouldSwap is true)
            {
                temp = start;
                start = end;
                end = temp;
            }

            StartPt = start;
            EndPt = end;

            CL_Pt_A = start;
            CL_Pt_B = end;

            Id = _id++;

            //DrawObject.DrawCircle(start, 10, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER);
            //DrawObject.DrawCircle(StartPt, 15, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER);

            // set the direction unit vector
            VDirection = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));
            VPerpendicular = MathHelpers.Normalize(MathHelpers.CrossProduct(Vector3d.ZAxis, VDirection));

            Length = MathHelpers.Distance3DBetween(start, end);

            Angle = Math.Abs(Math.Atan((EndPt.Y - StartPt.Y) / (EndPt.X - StartPt.X)));
        }
    }
}
