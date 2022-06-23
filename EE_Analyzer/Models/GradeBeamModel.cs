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

        // Unit vector for the direction of the grade beam from start node to end node
        private Vector3d VDirection { get; set; } = new Vector3d(1, 0, 0);
        private Vector3d VPerpendicular { get; set; } = new Vector3d(1, 0, 0);

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

        public Point3d CL_Pt_A { get; set; }
        public Point3d CL_Pt_B { get; set; }
        public Point3d E1_Pt_A { get; set; }
        public Point3d E1_Pt_B { get; set; }
        public Point3d E2_Pt_A { get; set; }
        public Point3d E2_Pt_B { get; set; }


        // AutoCAD polyline object for the plan view of the beam centerline
        public Line Centerline { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge one
        public Line Edge1 { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge two
        public Line Edge2 { get; set; } = null;

        public StrandModel StrandInfo { get; set; } = null;

        // The index number for the grade beam
        public int BeamNum { 
            get => _beamNum; 
            set {
                _beamNum = value;
            } 
        }

        public bool IsTrimmed { get; set; } = false;
        public bool IsHorizontal { get; set; } = true;

        // A label for the grade beam
        public string Label { get => "GB" + BeamNum.ToString(); }

        public GradeBeamModel(Point3d start, Point3d end, int num_strands, bool is_trimmed, bool is_horizontal, int beam_num, double width = 12.0, double depth = 24.0)
        {
            // Set basic info
            IsHorizontal = is_horizontal;
            Width = width;
            Depth = depth;
            IsTrimmed = is_trimmed;
            _beamNum = beam_num;  // update the grade beam number

            // swap the start point and end point based on lowest X then lowest Y
            bool shouldSwap = false;
            if (is_horizontal)
            {
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
            } else
            {
                if (start.Y > end.Y)
                {
                    shouldSwap = true;
                }
                else if (start.Y == end.Y)
                {
                    if (start.X > end.X)
                    {
                        shouldSwap = true;
                    }
                }
                else
                {
                    shouldSwap = false;
                }
            }

            // Now swap the points
            if (shouldSwap is true)
            {
                StartPt = end;
                EndPt = start;
                CL_Pt_A = end;
                CL_Pt_B = start;
            }
            else
            {
                StartPt = start;
                EndPt = end;
                CL_Pt_A = start;
                CL_Pt_B = end;
            }

            // which end of the beam to display the labels
            TagEnd = StartPt;

            // set the direction unit vector
            VDirection = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));
            VPerpendicular = MathHelpers.Normalize(MathHelpers.CrossProduct(Vector3d.ZAxis, VDirection));

            // Computes the end points for the edge lines of Edge 1 and Edge2
            E1_Pt_A = MathHelpers.Point3dFromVectorOffset(CL_Pt_A, VPerpendicular * width * 0.5);
            E1_Pt_B = MathHelpers.Point3dFromVectorOffset(CL_Pt_B, VPerpendicular * width * 0.5);
            E2_Pt_A = MathHelpers.Point3dFromVectorOffset(CL_Pt_A, (-1.0) * VPerpendicular * width * 0.5);
            E2_Pt_B = MathHelpers.Point3dFromVectorOffset(CL_Pt_B, (-1.0) * VPerpendicular * width * 0.5);

            // Create the beam strand info
            StrandInfo = new StrandModel(CL_Pt_A, CL_Pt_B, num_strands, true, is_trimmed);

        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc)
        {
            string layer_name = "";
            if(IsTrimmed)
            {
                layer_name = EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
            } else
            {
                layer_name = EE_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER;
            }

            DBObjectCollection coll = new DBObjectCollection();


            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                
                if(CL_Pt_A != null && CL_Pt_B != null)
                {
                    // if strand qty = 0, we need to draw the centerline, otherwise we don't draw it because the strand line will be present
                    if(StrandInfo.Qty == 0)
                    {
                        // Create the center line objects
                        Centerline = OffsetLine(new Line(CL_Pt_A, CL_Pt_B), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database

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

                }

                // Edge 1
                if (E1_Pt_A != null && E1_Pt_B != null)
                {
                    // Create the center line objects
                    Edge1 = OffsetLine(new Line(E1_Pt_A, E1_Pt_B), 0) as Line;  // Must create the edge 1 this way to have it added to the AutoCAD database

                    try
                    {
                        MoveLineToLayer(Edge1, layer_name);
                        LineSetLinetype(Edge1, "HIDDENX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding Edge 1 of Gradebeam entities to AutoCAD DB: " + ex.Message);
                        trans.Abort();
                        return;
                    }
                }

                // Edge 2
                if (E2_Pt_A != null && E2_Pt_B != null)
                {
                    // Create the center line objects
                    Edge2 = OffsetLine(new Line(E2_Pt_A, E2_Pt_B), 0) as Line;  // Must create the edge 2 line this way to have it added to the AutoCAD database

                    try
                    {
                        MoveLineToLayer(Edge2, layer_name);
                        LineSetLinetype(Edge2, "HIDDENX2");
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding Edge 2 of Gradebeam entities to AutoCAD DB: " + ex.Message);
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
