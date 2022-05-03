using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;

namespace EE_Analyzer.Models
{
    public class GradeBeamModel
    {
        private static int _beamNum = 0;

        // Unit vector for the direction of the grade beam
        private Vector3d vDirection { get; set; } = new Vector3d(1, 0, 0);

        // Depth of the grade beam
        private double Depth { get; set; }

        // Width of the grade beam
        private double Width { get; set; }

        // Start point for the grade beam
        private Point3d StartPt { get; set; }

        // End point for the grade beam
        private Point3d EndPt { get; set; }

        private Point3d TagEnd { get; set; }


        // AutoCAD Centerline object for the grade beam
        public Line Centerline { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge one
        public Line Edge1 { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge two
        public Line Edge2 { get; set; } = null;

        // The index number for the grade beam
        private int BeamNum { get; set; }

        private string Label { get; } = "B" + _beamNum.ToString();

        public GradeBeamModel(Point3d start, Point3d end, double width = 12.0, double depth = 24.0)
        {
            // Set basic info
            StartPt = start;
            EndPt = end;
            Width = width;
            Depth = depth;

            // which end of the beam to display the labels
            TagEnd = StartPt;

            // set the direction unit vector
            vDirection = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));

            BeamNum = _beamNum++;  // update the grade beam number
        }
    }




}
