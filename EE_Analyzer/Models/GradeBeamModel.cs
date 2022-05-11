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
        private const double _labelHt = 5;

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

        public StrandModel StrandInfo { get; set; } = null;

        // The index number for the grade beam
        public int BeamNum { get; set; }

        private string Label { get; } = "GB" + _beamNum.ToString();

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
        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                
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

                try
                {
                    // Draw the beam label
                    DrawBeamLabel(db, doc, layer_name);
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding beam labels to Gradebeam entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                    return;
                }

                try
                {
                    // Add the strand info
                    doc.Editor.WriteMessage("\nAdding Strand Info for Strand #" + StrandInfo.Id);
                    StrandInfo.AddToAutoCADDatabase(db, doc, layer_name);
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding strand info to Gradebeam entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                    return;
                }

                // commit the transaction
                trans.Commit();
            }
        }

        // Add number labels for each line segment
        private void DrawBeamLabel(Database db, Document doc, string layer_name)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Point3d insPt = StartPt;

                // Get the angle of the polyline
                var angle = Math.Atan((EndPt.Y - StartPt.Y) / (EndPt.X - StartPt.X));

                try
                {
                MText mtx = new MText();

                    mtx.Contents = Label;
                    mtx.Location = new Point3d(insPt.X - Math.Sin(angle) * 1.25* Width, insPt.Y + Math.Cos(angle) * 1.25 * Width, 0);
                    mtx.TextHeight = _labelHt;

                    mtx.Layer = layer_name;

                    mtx.Rotation = angle;

                    btr.AppendEntity(mtx);
                    trans.AddNewlyCreatedDBObject(mtx, true);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
                    trans.Abort();
                    return;
                }
            }
        }
    }
}
