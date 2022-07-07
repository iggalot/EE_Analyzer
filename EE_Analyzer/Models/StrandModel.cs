using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.MathHelpers;


namespace EE_Analyzer.Models
{
    public class StrandModel : FoundationObject
    {
        // strand arrow parameters
        private double strand_icon_length = EE_Settings.DEFAULT_STRAND_ARROW_LENGTH;
        private double strand_icon_width = EE_Settings.DEFAULT_STRAND_ARROW_WIDTH;

        private int _strand_id = 0;
        public int StrandID { get; set; }

        //public Point3d StartPt { get; set; }
        //public Point3d EndPt { get; set; }
        public string Label { get; set; } = "SXX";
        //public double Length { get; set; }

        //public Line Centerline { get; set; }

        public Point3d LiveEnd { get; set; }
        public int Qty { get; set; } = 1;

        //public Polyline LiveEndIcon;
        //public Polyline DeadEndIcon;

        public bool IsBeamStrand { get; set; } = false;
        //public bool IsTrimmed { get; set; } = false; 

        public StrandModel(Point3d start, Point3d end, double perim_beam_wdith, int qty, bool isBeamStrand, bool isTrimmed, bool is_horizontal) : base(start, end, perim_beam_wdith, is_horizontal)
        {
            IsBeamStrand = isBeamStrand;
            IsTrimmed = isTrimmed;
            Qty = qty;

            string str = Qty + "x ";
            if (isBeamStrand)
            {
                str += "B";
            }

            Label = str + "S" + (Math.Ceiling(Length * 10 / 12).ToString());

            StrandID = _strand_id;
            _strand_id++;

            // Offset the beam strands by the width of the perimeter beam.  Slab strands do not need this offset since they are trimmed to another line.
            // StartPt end goes in negative VDirection
            // EndPt end goes in positive Vdirection
            if (isBeamStrand)
            {
                StartPt = Point3dFromVectorOffset(StartPt, -perim_beam_wdith * VDirection);
                EndPt = Point3dFromVectorOffset(EndPt, perim_beam_wdith * VDirection);
            }


        }

        /// <summary>
        /// Retrieves the necessary layer name for the strands being drawn
        /// </summary>
        /// <returns></returns>
        private string GetDrawingLayer()
        {
            string strand_layer = "";
            if (IsBeamStrand is true && IsTrimmed == true)
            {
                strand_layer = EE_Settings.DEFAULT_FDN_BEAM_STRANDS_TRIMMED_LAYER;
            }
            else if (IsBeamStrand is false && IsTrimmed == true)
            {
                strand_layer = EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER;

            }
            else if (IsBeamStrand is true && IsTrimmed == false)
            {
                strand_layer = EE_Settings.DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER;
            }
            else
            {
                strand_layer = EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER;
            }

            return strand_layer;
        }

        public override void AddToAutoCADDatabase(Database db, Document doc)
        {
            if (Qty > 0)
            {
                string layer_name = "";
                if (IsBeamStrand is true)
                {
                    if (IsTrimmed == true)
                    {
                        layer_name = EE_Settings.DEFAULT_FDN_BEAM_STRANDS_TRIMMED_LAYER;
                    }
                    else
                    {
                        layer_name = EE_Settings.DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER;

                    }
                }
                else
                {
                    if (IsTrimmed == true)
                    {
                        layer_name = EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER;
                    }
                    else
                    {
                        layer_name = EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER;

                    }
                }

                //// Draw the strand centerline
                //Centerline = OffsetLine(new Line(StartPt, EndPt), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database
                //MoveLineToLayer(Centerline, layer_name);
                //LineSetLinetype(Centerline, "CENTERX2");

                //doc.Editor.WriteMessage("Drawing LiveEnd");
                DrawLiveEndIcon(db, doc, layer_name);
                //doc.Editor.WriteMessage("Drawing DeadEnd");
                DrawDeadEndIcon(db, doc, layer_name);
                //doc.Editor.WriteMessage("Drawing StrandLabel");
                //DrawStrandLabel(db, doc, layer_name);
            }
        }

        public Polyline DrawLiveEndIcon(Database db, Document doc, string layer_name)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline pl = new Polyline();

                try
                {
                    // Get the angle of the polyline
                    var angle = Angle;

                    Point2d pt1 = new Point2d(StartPt.X, StartPt.Y);

                    // Set the first vertex
                    pl.AddVertexAt(0, pt1, 0, 0, 0);
                    pl.SetEndWidthAt(0, strand_icon_width);
                    pl.SetStartWidthAt(0, 0);

                    // Specify the polyline parameters 
                    for (int i = 0; i < Qty; i++)
                    {
                        Point2d pt2 = new Point2d(StartPt.X + Math.Cos(angle) * (i + 1) * strand_icon_length, StartPt.Y + Math.Sin(angle) * (i + 1) * strand_icon_length);
                        pl.AddVertexAt(i + 1, pt2, 0, 0, 0);
                        pl.SetEndWidthAt(i + 1, strand_icon_width);
                        pl.SetStartWidthAt(i + 1, 0.0);
                    }

                    //pl.Closed = true;

                    // assign the layer
                    pl.Layer = layer_name;

                    // Set the default properties
                    pl.ReverseCurve();
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    // Add a line segment  to show part of the strand
                    double default_length = 8 * strand_icon_length;
                    Point3d line_end_pt = new Point3d(StartPt.X + Math.Cos(angle) * (default_length), StartPt.Y + Math.Sin(angle) * (default_length), 0);
                    Line ln = OffsetLine(new Line(new Point3d(pt1.X, pt1.Y, 0), line_end_pt), 0);
                    MoveLineToLayer(ln, layer_name);

                    // Draw the strand label
                    MText mtx = new MText();
                    try
                    {
                        mtx.Contents = Label;
                        mtx.Location = new Point3d(pt1.X + Math.Cos(angle) * 0.35 * default_length, pt1.Y + Math.Sin(angle) * 0.35 * default_length, 0);
                        mtx.TextHeight = EE_Settings.DEFAULT_STRAND_INFO_TEXT_SIZE;

                        mtx.Layer = layer_name;

                        mtx.Rotation = angle;

                        btr.AppendEntity(mtx);
                        trans.AddNewlyCreatedDBObject(mtx, true);
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
                        trans.Abort();
                    }

                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered drawing strand line: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        public Polyline DrawDeadEndIcon(Database db, Document doc, string layer_name)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline pl = new Polyline();

                try
                {
                    // Get the angle of the polyline
                    var angle = Angle;

                    // And add 180degrees to reverse it.
                    var angle_reverse = angle + Math.PI;

                    Point2d pt1 = new Point2d(EndPt.X, EndPt.Y);

                    pl.AddVertexAt(0, pt1, 0, strand_icon_width, strand_icon_width);
                    Point2d pt2 = new Point2d(EndPt.X + (0.8 * strand_icon_length) * Math.Cos(angle_reverse), EndPt.Y + (0.8 * strand_icon_length) * Math.Sin(angle_reverse));
                    pl.AddVertexAt(1, pt2, 0, strand_icon_width, strand_icon_width);

                    //// For matching number of dead end icons
                    //// Specify the polyline parameters 
                    //for (int i = 0; i < 2 * Qty; i++)
                    //{
                    //    Point2d pt2 = new Point2d(EndPt.X + (i + 1) * (0.8 * strand_icon_length) * Math.Cos(angle_reverse), EndPt.Y + (i + 1) * (0.8 * strand_icon_length) * Math.Sin(angle_reverse));
                    //    if (i % 2 != 0)
                    //    {
                    //        pl.AddVertexAt(i + 1, pt2, 0, strand_icon_width, strand_icon_width);
                    //    }
                    //    else
                    //    {
                    //        pl.AddVertexAt(i + 1, pt2, 0, 0, 0);
                    //    }
                    //}

                    // assign the layer
                    pl.Layer = layer_name;

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);


                    // Add a line segment  to show part of the strand
                    double default_length = 5 * strand_icon_length;
                    Point3d line_end_pt = new Point3d(pt1.X + Math.Cos(angle_reverse) * (default_length), pt1.Y + Math.Sin(angle_reverse) * (default_length), 0);
                    Line ln = OffsetLine(new Line(new Point3d(pt1.X, pt1.Y, 0), line_end_pt), 0);
                    MoveLineToLayer(ln, layer_name);


                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered drawing strand dead end icon: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }
    }
}
