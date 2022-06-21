using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.BlockObjects;
using static EE_Analyzer.Utilities.DrawObject;


using static EE_Analyzer.Utilities.EE_Helpers;


namespace EE_Analyzer.Models
{
    public class GradeBeamModel
    {
        private int _beamNum = 0;
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

        public bool IsTrimmed { get; set; } = false;

        // A label for the grade beam
        public string Label { get => "GB" + BeamNum.ToString(); }

        public GradeBeamModel(Point3d start, Point3d end, Polyline boundary, int num_strands, bool is_trimmed, double width = 12.0, double depth = 24.0)
        {
            // Set basic info

            // swap the start point and end point based on lowest X then lowest Y
            bool shouldSwap = false;
            if (start.X > end.X)
            {
                shouldSwap = true;
            }
            else if (start.X == end.X)
            {
                if (start.Y > end.Y)
                {
                    shouldSwap = true;
                }
            }
            else
            {
                shouldSwap = false;
            }

            if (shouldSwap is true)
            {
                StartPt = end;
                EndPt = start;
            }
            else
            {
                StartPt = start;
                EndPt = end;
            }

            Width = width;
            Depth = depth;

            // which end of the beam to display the labels
            TagEnd = StartPt;

            // set the direction unit vector
            vDirection = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));

            // Create the center line, edge1, and edge2 objects
            Centerline = OffsetLine(new Line(start, end), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database
//            Edge1 = OffsetLine(Centerline, width * 0.5) as Line;
//            Edge2 = OffsetLine(Centerline, -width * 0.5) as Line;

            StrandInfo = new StrandModel(Centerline.StartPoint, Centerline.EndPoint, num_strands, true, is_trimmed);
            IsTrimmed = is_trimmed;

            BeamNum = _beamNum++;  // update the grade beam number
        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc)
        {
            string layer_name = GetDrawingLayer();

            DBObjectCollection coll = new DBObjectCollection();


            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                
                if(Centerline != null)
                {
 //                   Line ln = DrawLine(Centerline.StartPoint, Centerline.EndPoint, layer_name);
 //                   coll.Add(ln);

                    try
                    {
                        MoveLineToLayer(Centerline, layer_name);
                        LineSetLinetype(Centerline, "CENTERX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding Centerline of Gradebeam entities to AutoCAD DB: " + ex.Message);
                        trans.Abort();
                        return;
                    }
                }

                if(Edge1 != null)
                {
 //                   coll.Add(Edge1);
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
                }


                // edge 2
                if(Edge2 != null)
                {
//                    coll.Add(Edge2);
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
                }

                try
                {
                    // Draw the beam label
//                    coll.Add(DrawBeamLabel(db, doc, layer_name));
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
                    StrandInfo.AddToAutoCADDatabase(db, doc);
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

            //string block_name = "";
            //try
            //{
            //    if(IsTrimmed is true)
            //    {
            //        block_name = "TB" + BeamNum.ToString();
            //    } else
            //    {
            //        block_name = "UB" + BeamNum.ToString();
            //    }

            //    CreateBlock(coll, block_name);
            //    InsertBlock(Centerline.StartPoint, block_name);
            //}
            //catch (System.Exception ex)
            //{
            //    doc.Editor.WriteMessage("Error creating grade beam block");
            //}



        }

        // Add number labels for each line segment
        private MText DrawBeamLabel(Database db, Document doc, string layer_name)
        {
            MText mtx = new MText();
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
                    //mtx = new MText();

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
                }
            }
            return mtx;

        }

        /// <summary>
        /// Retrieves the necessary layer name for the strands being drawn
        /// </summary>
        /// <returns></returns>
        private string GetDrawingLayer()
        {
            string strand_layer = "";
            if (IsTrimmed == true)
            {
                strand_layer = EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
            }
            else
            {
                strand_layer = EE_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER;
            }

            return strand_layer;
        }
    }
}
