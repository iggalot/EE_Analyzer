using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using static EE_Analyzer.Utilities.LineObjects;

namespace EE_Analyzer.Models
{
    public class GradeBeamModel
    {
        private static int _beamNum = 0;

        // Unit vector for the direction of the grade beam
        private Vector3d vDirection { get; set; } = new Vector3d(1, 0, 0);

        // Depth of the grade beam
        public double Depth { get; set; }

        // Width of the grade beam
        public double Width { get; set; }

        // Start point for the grade beam
        public Point3d StartPt { get; set; }

        // End point for the grade beam
        public Point3d EndPt { get; set; }

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

            // Create the center line, edge1, and edge2 objects
            Centerline = OffsetLine(new Line(start, end), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database
            Edge1 = OffsetLine(Centerline, width * 0.5) as Line;
            Edge2 = OffsetLine(Centerline, -width * 0.5) as Line;

            BeamNum = _beamNum++;  // update the grade beam number
        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name = EE_Settings.DEFAULT_FDN_BEAMS_LAYER)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                try
                {
                    try {
                        MoveLineToLayer(Centerline, layer_name);
                        LineSetLinetype(Centerline, "CENTERX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding Centerline of Gradebeam entities to AutoCAD DB: " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    try
                    {
                        // edge 1
                        MoveLineToLayer(Edge1, layer_name);
                        LineSetLinetype(Edge1, "HIDDENX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding EdgeLine1 of Gradebeam entities to AutoCAD DB: " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    // edge 2
                    try
                    {
                        MoveLineToLayer(Edge2, layer_name);
                        LineSetLinetype(Edge2, "HIDDENX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding EdgeLine2 of Gradebeam entities to AutoCAD DB: " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    // commit the transaction
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered while adding GradeBeamModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                    return;
                }
            }
        }
    }
}
