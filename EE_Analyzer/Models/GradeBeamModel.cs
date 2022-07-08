using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using static EE_Analyzer.Utilities.BlockObjects;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LineObjects;

namespace EE_Analyzer.Models
{
    public class GradeBeamModel : FoundationObject
    {
        private int _beamNum = 0;
        private const double _labelHt = EE_FDN_Settings.DEFAULT_GRADE_BEAM_INFO_TEXT_SIZE;

        //// Unit vector for the direction of the grade beam from start node to end node
        //private Vector3d VDirection { get; set; } = new Vector3d(1, 0, 0);
        //private Vector3d VPerpendicular { get; set; } = new Vector3d(1, 0, 0);

        // Depth of the grade beam
        public double Depth { get; set; }

        // Width of the grade beam
        public double Width { get; set; }

        //// Start point for the grade beam
        //public Point3d StartPt { get; set; }

        //// End point for the grade beam
        //public Point3d EndPt { get; set; }

        private Point3d TagEnd { get; set; }

        // AutoCAD Centerline object for the grade beam

        //public Point3d CL_Pt_A { get; set; }
        //public Point3d CL_Pt_B { get; set; }
        public Point3d E1_Pt_A { get; set; }
        public Point3d E1_Pt_B { get; set; }
        public Point3d E2_Pt_A { get; set; }
        public Point3d E2_Pt_B { get; set; }


        //// AutoCAD polyline object for the plan view of the beam centerline
        //public Line Centerline { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge one
        public Line Edge1 { get; set; } = null;

        // AutoCAD polyline object for the plan view of edge two
        public Line Edge2 { get; set; } = null;

        public StrandModel StrandInfo { get; set; } = null;

        // The index number for the grade beam
        public int BeamNum
        {
            get => _beamNum;
            set
            {
                _beamNum = value;
            }
        }

        //public bool IsTrimmed { get; set; } = false;
        //public bool IsHorizontal { get; set; } = true;

        // A label for the grade beam
        public string Label { get => "GB" + BeamNum.ToString(); }

        private IntersectPointData[] SortedGradeBeamIntersects = null;

        public GradeBeamModel(Point3d start, Point3d end, int num_strands, bool is_trimmed, bool is_horizontal, int beam_num, double width = 12.0, double depth = 24.0) : base(start, end, 0, is_horizontal)
        {
            // Set basic info
            Width = width;
            Depth = depth;
            IsTrimmed = is_trimmed;
            _beamNum = beam_num;  // update the grade beam number

            // which end of the beam to display the labels
            TagEnd = StartPt;

            // Computes the end points for the edge lines of Edge 1 and Edge2
            E1_Pt_A = MathHelpers.Point3dFromVectorOffset(CL_Pt_A, VPerpendicular * width * 0.5);
            E1_Pt_B = MathHelpers.Point3dFromVectorOffset(CL_Pt_B, VPerpendicular * width * 0.5);
            E2_Pt_A = MathHelpers.Point3dFromVectorOffset(CL_Pt_A, (-1.0) * VPerpendicular * width * 0.5);
            E2_Pt_B = MathHelpers.Point3dFromVectorOffset(CL_Pt_B, (-1.0) * VPerpendicular * width * 0.5);

            // Create the beam strand info
            StrandInfo = new StrandModel(CL_Pt_A, CL_Pt_B, width, num_strands, true, is_trimmed, is_horizontal);

        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public override void AddToAutoCADDatabase(Database db, Document doc)
        {
            string layer_name = "";
            if (IsTrimmed)
            {
                layer_name = EE_FDN_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
            }
            else
            {
                layer_name = EE_FDN_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER;
            }

            DBObjectCollection coll = new DBObjectCollection();


            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                if (CL_Pt_A != null && CL_Pt_B != null)
                {

                    //// if strand qty = 0, we need to draw the centerline, otherwise we don't draw it because the strand line will be present
                    //if(StrandInfo.Qty == 0)
                    //{
                    //    // Create the center line objects
                    //    Centerline = OffsetLine(new Line(CL_Pt_A, CL_Pt_B), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database

                    //    try
                    //    {
                    //        MoveLineToLayer(Centerline, layer_name);
                    //        LineSetLinetype(Centerline, "CENTER2");
                    //    }
                    //    catch (System.Exception ex)
                    //    {
                    //        doc.Editor.WriteMessage("\nError encountered while adding Centerline of Gradebeam entities to AutoCAD DB: " + ex.Message);
                    //        trans.Abort();
                    //        return;
                    //    }
                    //}

                    Centerline = OffsetLine(new Line(CL_Pt_A, CL_Pt_B), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database
                    MoveLineToLayer(Centerline, layer_name);
                    LineSetLinetype(Centerline, "CENTER2");

                }

                // Edge 1
                if (E1_Pt_A != null && E1_Pt_B != null)
                {
                    // draw line segment from E1_pt_A to first grade beam first point
                    if (SortedGradeBeamIntersects != null && SortedGradeBeamIntersects.Length > 0)
                    {
                        Line ln;
                        Point3d first = E1_Pt_A;
                        Point3d second = E1_Pt_B;

                        try
                        {
                            for (int i = 0; i < SortedGradeBeamIntersects.Length; i++)
                            {
                                Point3d cl_int_pt = SortedGradeBeamIntersects[i].Point;
                                Point3d a = MathHelpers.Point3dFromVectorOffset(cl_int_pt, VPerpendicular * 0.5 * Width);
                                second = MathHelpers.Point3dFromVectorOffset(a, VDirection * -0.5 * Width);
                                ln = OffsetLine(new Line(first, second), 0) as Line;
                                MoveLineToLayer(ln, layer_name);
                                LineSetLinetype(ln, "HIDDENX2");

                                // the new first point
                                first = MathHelpers.Point3dFromVectorOffset(second, VDirection * Width);
                            }

                            // Now draw the last segment
                            ln = OffsetLine(new Line(first, E1_Pt_B), 0) as Line;
                            MoveLineToLayer(ln, layer_name);
                            LineSetLinetype(ln, "HIDDENX2");
                        }
                        catch (System.Exception ex)
                        {
                            doc.Editor.WriteMessage("\nError encountered while adding Edge 1 of Gradebeam entities to AutoCAD DB: " + ex.Message);
                            trans.Abort();
                            return;
                        }

                        // draw line segment from first grade beam second point to E1_pt_B
                        Edge1 = new Line(E1_Pt_A, E1_Pt_B);  // Must create the edge 1 this way to have it added to the AutoCAD database
                    }
                }

                // Edge 2
                if (E2_Pt_A != null && E2_Pt_B != null)
                {
                    // draw line segment from E1_pt_A to first grade beam first point
                    if (SortedGradeBeamIntersects != null && SortedGradeBeamIntersects.Length > 0)
                    {
                        Line ln;
                        Point3d first = E2_Pt_A;
                        Point3d second = E2_Pt_B;

                        try
                        {
                            for (int i = 0; i < SortedGradeBeamIntersects.Length; i++)
                            {
                                Point3d cl_int_pt = SortedGradeBeamIntersects[i].Point;
                                Point3d a = MathHelpers.Point3dFromVectorOffset(cl_int_pt, VPerpendicular * -0.5 * Width);
                                second = MathHelpers.Point3dFromVectorOffset(a, VDirection * -0.5 * Width);
                                ln = OffsetLine(new Line(first, second), 0) as Line;
                                MoveLineToLayer(ln, layer_name);
                                LineSetLinetype(ln, "HIDDENX2");

                                // the new first point
                                first = MathHelpers.Point3dFromVectorOffset(second, VDirection * Width);
                            }

                            // Now draw the last segment
                            ln = OffsetLine(new Line(first, E2_Pt_B), 0) as Line;
                            MoveLineToLayer(ln, layer_name);
                            LineSetLinetype(ln, "HIDDENX2");
                        }
                        catch (System.Exception ex)
                        {
                            doc.Editor.WriteMessage("\nError encountered while adding Edge 2 of Gradebeam entities to AutoCAD DB: " + ex.Message);
                            trans.Abort();
                            return;
                        }

                        // draw line segment from first grade beam second point to E1_pt_B
                        Edge2 = new Line(E2_Pt_A, E2_Pt_B);  // Must create the edge 1 this way to have it added to the AutoCAD database
                    }
                }

                try
                {
                    // Draw the beam label
                    if (IsTrimmed)
                    {
                        layer_name = EE_FDN_Settings.DEFAULT_FDN_TEXTS_LAYER;
                    }
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
                var angle = Angle;

                try
                {
                    //mtx = new MText();

                    mtx.Contents = Label;
                    mtx.Location = new Point3d(insPt.X - Math.Sin(angle) * 1.25 * Width, insPt.Y + Math.Cos(angle) * 1.25 * Width, 0);
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
                strand_layer = EE_FDN_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
            }
            else
            {
                strand_layer = EE_FDN_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER;
            }

            return strand_layer;
        }

        /// <summary>
        /// Function to determine the intersection points of the centerline of this gradebeam with
        /// all other gradebeams in the list.
        /// </summary>
        /// <param name="gb_models"></param>
        public void SetGradeBeamIntersects(List<GradeBeamModel> gb_models)
        {
            List<IntersectPointData> lst = new List<IntersectPointData>();

            foreach (GradeBeamModel gb in gb_models)
            {
                // Find the intersection point
                IntersectPointData p1_data = FindPointOfIntersectLines_FromPoint3d(
                    this.CL_Pt_A,
                    this.CL_Pt_B,
                    gb.CL_Pt_A,
                    gb.CL_Pt_B
                    );

                if (p1_data == null)
                {
                    continue;
                }
                else
                {
                    if (p1_data.isWithinSegment)
                    {
                        lst.Add(p1_data);
                    }
                }
            }

            if (lst.Count < 0)
            {
                return;
            }

            // Create our list of grade beam intersects
            SortedGradeBeamIntersects = new IntersectPointData[lst.Count];
            IntersectPointData[] sort_arr;

            if (this.IsHorizontal is true)
            {
                sort_arr = lst.ToArray();
                IntersectPointData temp;

                for (int j = 0; j < lst.Count - 1; j++)
                {
                    for (int i = 0; i < lst.Count - 1; i++)
                    {
                        if (sort_arr[i].Point.X > sort_arr[i + 1].Point.X)
                        {
                            temp = sort_arr[i + 1];
                            sort_arr[i + 1] = sort_arr[i];
                            sort_arr[i] = temp;
                        }
                    }
                }
            }
            else
            {
                sort_arr = lst.ToArray();
                IntersectPointData temp;

                for (int j = 0; j < lst.Count - 1; j++)
                {
                    for (int i = 0; i < lst.Count - 1; i++)
                    {
                        if (sort_arr[i].Point.Y > sort_arr[i + 1].Point.Y)
                        {
                            temp = sort_arr[i + 1];
                            sort_arr[i + 1] = sort_arr[i];
                            sort_arr[i] = temp;
                        }
                    }
                }
            }

            SortedGradeBeamIntersects = sort_arr;

            return;
        }
    }
}
