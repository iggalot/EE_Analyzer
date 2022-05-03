using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using static EE_Analyzer.Utilities.LinetypeObjects;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.PolylineObjects;
using static EE_Analyzer.Utilities.LineObjects;


using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using EE_Analyzer.Models;

namespace EE_Analyzer
{
    public class FoundationLayout
    {
        // Holds the primary foundation perimeter polyline object.
        public static Polyline FDN_PERIMETER_POLYLINE { get; set; } = new Polyline();
        public static Polyline FDN_PERIMETER_INTERIOR_EDGE_POLYLINE { get; set; } = new Polyline();

        // Hold the bounding box for the foundation extents
        public static Polyline FDN_BOUNDARY_BOX { get; set; } = new Polyline();

        // Beam counter object -- deprecated by GradeBeamModel;
        private static int beamCount = 0;

        // Default tolerance
        private const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal

        // Default layers
        private const string DEFAULT_FDN_BOUNDINGBOX_LAYER = "_EE_FDN_BOUNDINGBOX"; // Contains the bounding box 
        private const string DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER = "_EE_FDN_PERIMETER";  // Contains the polyline for the perimeter of the foundation
        private const string DEFAULT_FDN_BEAMS_LAYER = "_EE_FDN_BEAMS"; // For the untrimmed ribs of the foundation
        private const string DEFAULT_FDN_BEAMS_TRIMMED_LAYER = "_EE_FDN_BEAMS_TRIMMED";
        private const string DEFAULT_FDN_BEAM_STRANDS_LAYER = "_EE_FDN_BEAM_STRANDS";
        private const string DEFAULT_FDN_SLAB_STRANDS_LAYER = "_EE_FDN_SLAB_STRANDS";
        private const string DEFAULT_FDN_TEXTS_LAYER = "_EE_FDN_TEXT";
        private const string DEFAULT_FDN_DIMENSIONS_LAYER = "_EE_FDN_DIMENSIONS";
        private const string DEFAULT_FDN_ANNOTATION_LAYER = "_EE_FDN_ANNOTATION_LAYER"; // for notes and markers

        // Data storage Entities
        private static List<Line> BeamLines { get; set; } = new List<Line>();
        private static List<Polyline> StrandLines { get; set; } = new List<Polyline>();
        private static List<GradeBeamModel> lstInteriorGradeBeamsUntrimmed { get; set; } = new List<GradeBeamModel>();

        public static int Beam_X_Qty { get; set; }
        public static int Beam_X_Strand_Qty { get; set; }
        public static int Beam_X_Slab_Strand_Qty { get; set; }

        public static double Beam_X_Spacing { get; set; }
        public static double Beam_X_Width { get; set; }
        public static double Beam_X_Depth { get; set; }

        public static int Beam_Y_Qty { get; set; }
        public static int Beam_Y_Strand_Qty { get; set; }
        public static int Beam_Y_Slab_Strand_Qty { get; set; }

        public static double Beam_Y_Spacing { get; set; }
        public static double Beam_Y_Width { get; set; }
        public static double Beam_Y_Depth { get; set; }



        [CommandMethod("EE_FDN")]
        public static void DrawFoundationDetails(
            int x_qty, double x_spa, double x_depth, double x_width,
            int y_qty, double y_spa, double y_depth, double y_width,
            int bx_strand_qty, int sx_strand_qty, int by_strand_qty, int sy_strand_qty)
        {
            Beam_X_Spacing = x_spa;  // spacing between horizontal beams
            Beam_X_Width = x_width;  // horizontal beam width
            Beam_X_Depth = x_depth;  // horizontal beam depth
            Beam_X_Qty = x_qty;         // horizontal beam qty
            Beam_X_Strand_Qty = bx_strand_qty;  // number of strands in each x-direction beam
            Beam_X_Slab_Strand_Qty = sx_strand_qty;  // number of strands in x-direction slab

            Beam_Y_Spacing = y_spa;  // spacing between vertical beams
            Beam_Y_Width = y_width;  // vertical beam width
            Beam_Y_Depth = y_depth;  // vertical beam depth
            Beam_Y_Qty = y_qty;         // vertical beam qty 
            Beam_Y_Strand_Qty = by_strand_qty;  // number of strands in each y-direction beam
            Beam_Y_Slab_Strand_Qty = sy_strand_qty;  // number of strands in y-direction slab

            double circle_radius = Beam_X_Spacing * 0.1; // for marking the intersections of beams and strands with the foundation polyline

            int max_beams = 75;  // define the maximum number of beams in a given direction -- in case we get into an infinite loop situation.

            // Get our AutoCAD API objects
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            #region Application Setup
            // Set up layers and linetypes and AutoCAD drawings items
            EE_ApplicationSetup(doc, db);
            #endregion

            #region Select Foundation Beam in AutoCAD
            // Selects the foundation polyline and corrects the winding order to be clockwise.

            var options = new PromptEntityOptions("\nSelect Foundation Polyline");
            options.SetRejectMessage("\nSelected object is not a polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            // Select the polyline for the foundation
            var result = edt.GetEntity(options);

            FDN_PERIMETER_POLYLINE = ProcessFoundationPerimeter(db, edt, result);

            if (FDN_PERIMETER_POLYLINE is null)
            {
                throw new System.Exception("\nInvalid foundation perimeter line selected.");
            }
            else
            {
                doc.Editor.WriteMessage("\nFoundation perimeter line selected");
            }
            #endregion

            #region Create and Draw Bounding Box
            var lstVertices = GetVertices(FDN_PERIMETER_POLYLINE);
            doc.Editor.WriteMessage("\n--Foundation perimeter has " + lstVertices.Count + " vertices.");
            FDN_BOUNDARY_BOX = CreateFoundationBoundingBox(db, edt, lstVertices);
            doc.Editor.WriteMessage("\n-- Creating foundation bounding box.");
            if (FDN_BOUNDARY_BOX is null)
            {
                throw new System.Exception("Invalid foundation boundary box created.");
            }
            #endregion

            #region Draw Foundation Perimeter Beam
            // TODO:  Return a GradeBeam object for the foundation perimeter?
            //////////////////////////////////////////////////////
            // Draw the perimeter beam -- perimeter line will be continuous, inner edge line will be hidden
            // both will be assigned to the boundary perimeter layer
            //////////////////////////////////////////////////////
            doc.Editor.WriteMessage("\nDrawing foundation perimeter beam");
            DrawFoundationPerimeterBeam(db, doc, Beam_X_Width);
            doc.Editor.WriteMessage("\nDrawing foundation perimeter beam completed");
            #endregion

            #region Draw Untrimmed Grade Beams
            // First draw the horizontal grade beams
            doc.Editor.WriteMessage("\nDrawing interior grade beams");
            // draw horizontal grade beams
            DrawFoundationInteriorGradeBeamsHorizontalUntrimmed(db, doc, Beam_X_Width, Beam_X_Spacing, Beam_X_Depth);
            DrawFoundationInteriorGradeBeamsVerticalUntrimmed(db, doc, Beam_Y_Width, Beam_Y_Spacing, Beam_Y_Depth);


            // draw vertical grade beams
            doc.Editor.WriteMessage("\nDrawing Interior grade beams completed.");
            #endregion

            #region Trim Grade Beam Lines

            // Trim the line to the physical edge of the slab (not the limits rectangle)
            //Line trimmedCenterLine = TrimLineToPolyline(centerLine, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);

            #endregion

            #region Draw Strands
            #endregion

            #region Draw Beam Labels
            #endregion

            #region Draw Strand Labels
            #endregion

            #region Bill of Materials
            // compute concrete volumes

            // compute strand quantities
            #endregion

            #region Section Details
            #endregion

            #region Additional Steel
            #endregion
        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="p1">start point of centerline</param>
        /// <param name="p2">end point of centerline</param>
        /// <param name="width">width of the beam</param>
        /// <param name="depth">depth of the beam</param>
        /// <param name="bt"></param>
        /// <param name="btr"></param>
        private static void DrawGradeBeamUntrimmed(Database db, Document doc, Point3d p1, Point3d p2, double width, double depth, BlockTable bt, BlockTableRecord btr)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Line centerLine = new Line(p1, p2);

                    //ln.ColorIndex = 1;  // Color is red
                    centerLine.Linetype = "CENTERX2";
                    centerLine.Layer = DEFAULT_FDN_BEAMS_LAYER;
                    btr.AppendEntity(centerLine);
                    trans.AddNewlyCreatedDBObject(centerLine, true);

                    // Add boundaries of our beams
                    // edge 1
                    Line edge1 = OffsetLine(centerLine, width * 0.5, bt, btr) as Line;
                    MoveLineToLayer(edge1, DEFAULT_FDN_BEAMS_LAYER);
                    LineSetLinetype(edge1, "HIDDENX2");
                    // edge 2
                    Line edge2 = OffsetLine(centerLine, -width * 0.5, bt, btr) as Line;
                    MoveLineToLayer(edge2, DEFAULT_FDN_BEAMS_LAYER);
                    LineSetLinetype(edge2, "HIDDENX2");

                    // Create our GradeBeamModel
                    GradeBeamModel model = new GradeBeamModel(p1, p2, width, depth);
                    model.Centerline = centerLine;
                    model.Edge1 = edge1;
                    model.Edge2 = edge2;
                    // and add it to our untrimmed list
                    lstInteriorGradeBeamsUntrimmed.Add(model);

                    // commit the transaction
                    trans.Commit();

                } catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered in drawing grade beam function: " + ex.Message);
                    trans.Abort();
                    return;
                }
            }
        }

        /// <summary>
        /// Draws the interior grade beam
        /// </summary>
        /// <param name="spa"></param>
        /// <param name="width"></param>
        /// <param name="max_beams"></param>
        private static void DrawFoundationInteriorGradeBeamsHorizontalUntrimmed(Database db, Document doc, double width, double spacing, double depth)
        {

            var bbox_points = GetVertices(FDN_BOUNDARY_BOX);

            if(bbox_points is null || (bbox_points.Count != 4))
            {
                throw new System.Exception("\nFoundation bounding box must have four points");
            }

            int count = 0;
            // offset the first beam by half a beam width
            while (bbox_points[0].Y + (width * 0.5) + count * spacing < bbox_points[1].Y)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Point3d p1 = new Point3d(bbox_points[0].X, bbox_points[0].Y + (width * 0.5) + count * spacing, 0);
                        Point3d p2 = new Point3d(bbox_points[3].X, bbox_points[3].Y + (width * 0.5) + count * spacing, 0);

                        if(p1 == p2)
                        {
                            doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping grade beam here.");
                            continue;
                        }
                        // reverse the points so the smallest X is on the left
                        if (p1.X > p2.X)
                        {
                            Point3d temp = p1;
                            p1 = p2;
                            p2 = temp;

                        } 
                        DrawGradeBeamUntrimmed(db, doc, p1, p2, Beam_X_Width, Beam_X_Depth, bt, btr);

                        trans.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered - drawing grade beams: " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    count++;
                }
            }
        }


        /// <summary>
        /// Draws the interior grade beam
        /// </summary>
        /// <param name="spa"></param>
        /// <param name="width"></param>
        /// <param name="max_beams"></param>
        private static void DrawFoundationInteriorGradeBeamsVerticalUntrimmed(Database db, Document doc, double width, double spacing, double depth)
        {

            var bbox_points = GetVertices(FDN_BOUNDARY_BOX);

            if (bbox_points is null || (bbox_points.Count != 4))
            {
                throw new System.Exception("\nFoundation bounding box must have four points");
            }

            int count = 0;
            // offset the first beam by half a beam width
            while (bbox_points[0].X + (Beam_Y_Width * 0.5) + count * Beam_Y_Spacing < bbox_points[3].X)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Point3d p1 = new Point3d(bbox_points[0].X + (Beam_Y_Width * 0.5) + count * Beam_Y_Spacing, bbox_points[0].Y, 0);
                        Point3d p2 = new Point3d(bbox_points[0].X + (Beam_Y_Width * 0.5) + count * Beam_Y_Spacing, bbox_points[1].Y, 0);

                        if (p1 == p2)
                        {
                            doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping grade beam here.");
                            continue;
                        }
                        // reverse the points so the smallest Y is at the bottom
                        if (p1.Y > p2.Y)
                        {
                            Point3d temp = p1;
                            p1 = p2;
                            p2 = temp;

                        }
                        DrawGradeBeamUntrimmed(db, doc, p1, p2, Beam_Y_Width, Beam_Y_Depth, bt, btr);

                        trans.Commit();

                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered - drawing grade beams: " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    count++;
                }
            }
        }

        /// <summary>
        /// Draws the foundation perimeter grade beam by using offset.  
        /// Draws the centerline and the interior edge
        /// </summary>
        /// <param name="db">AutoCAD database</param>
        /// <param name="doc">AutoCAD document</param>
        /// <param name="beam_x_width">Width of the grade beam</param>
        private static void DrawFoundationPerimeterBeam(Database db, Document doc, double beam_x_width)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Redraw the perimeter beam
                    doc.Editor.WriteMessage("\nCreating perimeter beam outer edge line.");
                    MovePolylineToLayer(FDN_PERIMETER_POLYLINE, DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(FDN_PERIMETER_POLYLINE, "CONTINUOUS", bt, btr);

                    // Draw the perimeter beam centerline
                    doc.Editor.WriteMessage("\nCreating perimeter beam center line.");
                    Polyline centerPerimeterBeamPolyline = OffsetPolyline(FDN_PERIMETER_POLYLINE, beam_x_width * 0.5, bt, btr);
                    MovePolylineToLayer(centerPerimeterBeamPolyline, DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(centerPerimeterBeamPolyline, "CENTER", bt, btr);

                    // Offset the perimeter polyline and move it to its appropriate layer
                    doc.Editor.WriteMessage("\nCreating perimeter beam inner edge line.");
                    FDN_PERIMETER_INTERIOR_EDGE_POLYLINE = OffsetPolyline(FDN_PERIMETER_POLYLINE, beam_x_width, bt, btr);
                    MovePolylineToLayer(FDN_PERIMETER_INTERIOR_EDGE_POLYLINE, DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(FDN_PERIMETER_INTERIOR_EDGE_POLYLINE, "HIDDEN", bt, btr);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered processing foundation polyline winding direction: " + ex.Message);
                    trans.Abort();
                }

                return;
            }
        }

        /// <summary>
        /// Verifies the selection of the foundation beam and adjusts the winding order to be clockwise (if necessary)
        /// </summary>
        /// <param name="db">AutoCAD database</param>
        /// <param name="edt">AutoCAD Editor</param>
        /// <param name="result">result from the entity selection in AutoCAD</param>
        /// <param name="foundationPerimeterPolyline">stores the foundation perimeter polyline</param>
        /// <exception cref="System.Exception"></exception>
        private static Polyline ProcessFoundationPerimeter(Database db, Editor edt, PromptEntityResult result)
        {
            Polyline foundationPerimeterPolyline = new Polyline();
            if (result.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        foundationPerimeterPolyline = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;

                        ///////////////////////////////////////////////////
                        /// Now start processing  the foundation polylines
                        /// ///////////////////////////////////////////////
                        int numVertices = foundationPerimeterPolyline.NumberOfVertices;
                        var lstVertices = GetVertices(foundationPerimeterPolyline);

                        if (lstVertices.Count < 4)
                        {
                            edt.WriteMessage("\nFoundation must have at least four sides.  The selected polygon only has " + lstVertices.Count);
                            trans.Abort();
                            return null;
                        }
                        else
                        {
                            // Check that the polyline is in a clockwise winding.  If not, then reverse the polyline direction
                            // -- necessary for the offset functions later to work correctly.
                            if (!PolylineIsWoundClockwise(foundationPerimeterPolyline))
                            {
                                edt.WriteMessage("\nReversing foundation polyline direction to make it Clockwise");
                                ReversePolylineDirection(foundationPerimeterPolyline);
                            }
                            else
                            {
                                // Do nothing since it's already clockwise.
                            }
                        }

                        trans.Commit();

                    } catch (System.Exception ex)
                    {
                        edt.WriteMessage("\nError encountered processing foundation polyline winding direction: " + ex.Message);
                        trans.Abort();
                        return null;
                    }

                    return foundationPerimeterPolyline;
                }
            }
            else
            {
                throw new System.Exception("Unknown error in selection the foundation Polyline.");
            }
        }

        /// <summary>
        /// Determines the rectangular boundaing box for a list of Point2D points.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="edt"></param>
        /// <param name="lstVertices"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private static Polyline CreateFoundationBoundingBox(Database db, Editor edt, List<Point2d> lstVertices)
        {
            if(lstVertices is null)
            {
                throw new System.Exception("Error in creating bounding box from foundation perimeter beam");
            }

            var lx = lstVertices[0].X;
            var ly = lstVertices[0].Y;
            var rx = lstVertices[0].X;
            var ry = lstVertices[0].Y;
            var tx = lstVertices[0].X;
            var ty = lstVertices[0].Y;
            var bx = lstVertices[0].X;
            var by = lstVertices[0].Y;


            // Finds the extreme limits of the foundation
            foreach (var vert in lstVertices)
            {
                if (vert.X < lx)
                    lx = vert.X;

                if (vert.X > rx)
                    rx = vert.X;

                if (vert.Y < by)
                    by = vert.Y;

                if (vert.Y > ty)
                    ty = vert.Y;
            }

            // Draw the limits of the foundation
            Point2d boundP1 = new Point2d(lx, by);  // lower left
            Point2d boundP2 = new Point2d(lx, ty);  // upper left
            Point2d boundP3 = new Point2d(rx, ty);  // upper right
            Point2d boundP4 = new Point2d(rx, by);  // bottom right

            Polyline pl = new Polyline();

            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                try
                {
                    // Specify the polyline parameters 
                    pl.AddVertexAt(0, boundP1, 0, 0, 0);
                    pl.AddVertexAt(1, boundP2, 0, 0, 0);
                    pl.AddVertexAt(2, boundP3, 0, 0, 0);
                    pl.AddVertexAt(3, boundP4, 0, 0, 0);
                    pl.Closed = true;
                    pl.ColorIndex = 140; // cyan color

                    // assign the layer
                    pl.Layer = DEFAULT_FDN_BOUNDINGBOX_LAYER;

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("\nError encountered drawing foundation boundary line: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        private static void EE_ApplicationSetup(Document doc, Database db)
        {
            // Load our linetype
            LoadLineTypes("CENTER", doc, db);
            LoadLineTypes("DASHED", doc, db);
            LoadLineTypes("HIDDEN", doc, db);
            LoadLineTypes("CENTERX2", doc, db);
            LoadLineTypes("DASHEDX2", doc, db);
            LoadLineTypes("HIDDENX2", doc, db);
            LoadLineTypes("CENTER2", doc, db);
            LoadLineTypes("DASHED2", doc, db);
            LoadLineTypes("HIDDEN2", doc, db);

            // Create our layers
            CreateLayer(DEFAULT_FDN_BOUNDINGBOX_LAYER, doc, db, 4); // cyan
            CreateLayer(DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, doc, db, 3); // cyan
            CreateLayer(DEFAULT_FDN_BEAMS_LAYER, doc, db, 1); // red
            CreateLayer(DEFAULT_FDN_BEAMS_TRIMMED_LAYER, doc, db, 140); // blue
            CreateLayer(DEFAULT_FDN_BEAM_STRANDS_LAYER, doc, db, 3);  // green
            CreateLayer(DEFAULT_FDN_SLAB_STRANDS_LAYER, doc, db, 2);  // yellow
            CreateLayer(DEFAULT_FDN_TEXTS_LAYER, doc, db, 3); // yellow
            CreateLayer(DEFAULT_FDN_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(DEFAULT_FDN_ANNOTATION_LAYER, doc, db, 1); // yellow
        }









        [CommandMethod("JIM")]
        public void ShowModalWpfDialogCmd()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;
            //double radius; // radius default value
            //string layer;  // layer default value

            // private fields initialization (initial default values)
            // initial default values
           // layer = (string)AcAp.GetSystemVariable("clayer");
            //radius = 10.0;

            //var layers = GetAllLayerNamesList();
            //if (!layers.Contains(layer))
            //{
            //    layer = (string)AcAp.GetSystemVariable("clayer");
            //}

            // shows the dialog box
            //var dialog = new EE_FDNInputDialog(layers, layer, radius);
            var dialog = new EE_FDNInputDialog();

            var result = AcAp.ShowModalWindow(dialog);
            if (result.Value)
            {
                edt.WriteMessage("\nDialog displayed and successfully entered");
            }   

            //if (result.Value)
            //{
            //    // fields update
            //    //layer = dialog.Layer;
            //    //radius = dialog.Radius;

            //    //// circle drawing
            //    //var ppr = ed.GetPoint("\nSpecify the center: ");
            //    //if (ppr.Status == PromptStatus.OK)
            //    //{
            //    //    // drawing the circle in current space
            //    //    using (var tr = db.TransactionManager.StartTransaction())
            //    //    {
            //    //        //var curSpace =
            //    //        //    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            //    //        //using (var circle = new Circle(ppr.Value, Vector3d.ZAxis, radius))
            //    //        //{
            //    //        //    try
            //    //        //    {
            //    //        //        circle.TransformBy(ed.CurrentUserCoordinateSystem);
            //    //        //        circle.Layer = layer;
            //    //        //        curSpace.AppendEntity(circle);
            //    //        //        tr.AddNewlyCreatedDBObject(circle, true);
            //    //        //    } catch (System.Exception ex)
            //    //        //    {
                                
            //    //        //    }
            //    //        //}
            //    //        tr.Commit();
            //    //    }
            //    //}
            //}
        }

        // Add number labels for each line segment
        private static void AddBeamLabels(Editor edt, Transaction trans, BlockTableRecord btr, Point3d[] points, int i, double angle)
        {
            string txt = "Beam " + beamCount.ToString();
            Point3d insPt = points[i];
            using (MText mtx = new MText())
            {
                try
                {
                    mtx.Contents = txt;
                    mtx.Location = insPt;
                    mtx.TextHeight = 4;
                    mtx.ColorIndex = 2;

                    mtx.Layer = DEFAULT_FDN_TEXTS_LAYER;

                    mtx.Rotation = angle;

                    btr.AppendEntity(mtx);
                    trans.AddNewlyCreatedDBObject(mtx, true);
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
                    trans.Abort();
                    return;
                }
            }
        }
    }
}














        //                ////////////////////////////////////////////////////
        //                // Draw centerlines of intermediate horizontal beams
        //                ////////////////////////////////////////////////////
        //                int count = 0;

        //                // offset the first beam by half a beam width
        //                while (boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa < boundP2.Y && count < max_beams)
        //                {
        //                    try
        //                    {
        //                        Point3d p1 = new Point3d(boundP1.X, boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa, 0);
        //                        Point3d p2 = new Point3d(boundP4.X, boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa, 0);
        //                        Line ln = new Line(p1, p2);

        //                        // Trim the line to the physical edge of the slab (not the limits rectangle)
        //                        ln = TrimLineToPolyline(ln, pl);

        //                        //ln.ColorIndex = 1;  // Color is red
        //                        ln.Linetype = "CENTERX2";
        //                        ln.Layer = DEFAULT_FDN_BEAMS_LAYER;

        //                        btr.AppendEntity(ln);
        //                        trans.AddNewlyCreatedDBObject(ln, true);

        //                        // add our beam lines to our collection.
        //                        BeamLines.Add(ln);

        //                        // Add boundaries of our beams
        //                        // edge 1
        //                        Line edge1 = OffsetLine(ln, beam_x_width * 0.5, bt, btr) as Line;
        //                        MoveLineToLayer(edge1, DEFAULT_FDN_BEAMS_LAYER);
        //                        LineSetLinetype(edge1, "HIDDENX2");
        //                        // edge 2
        //                        Line edge2 = OffsetLine(ln, -beam_x_width * 0.5, bt, btr) as Line;
        //                        MoveLineToLayer(edge2, DEFAULT_FDN_BEAMS_LAYER);
        //                        LineSetLinetype(edge2, "HIDDENX2");

        //                        //     BeamLines.Add(edge1);
        //                        //     BeamLines.Add(edge2);
        //                    }
        //                    catch (System.Exception ex)
        //                    {
        //                        edt.WriteMessage("\nError encountered - drawing horizontal beams: " + ex.Message);
        //                        trans.Abort();
        //                        return;
        //                    }

        //                    count++;
        //                }

        //                ////////////////////////////////////////////////////
        //                // Draw centerlines of intermediate vertical beams
        //                ////////////////////////////////////////////////////
        //                count = 0;

        //                // offset the first beam by have a beam width
        //                while (boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa < boundP4.X && count < max_beams)
        //                {
        //                    try
        //                    {
        //                        // Send a message to the user
        //                        Point3d p1 = new Point3d(boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa, boundP1.Y, 0);
        //                        Point3d p2 = new Point3d(boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa, boundP2.Y, 0);
        //                        Line ln = new Line(p1, p2);
        //                        //ln.ColorIndex = 2;  // Color is red

        //                        ln.Linetype = "CENTERX2";
        //                        ln.Layer = DEFAULT_FDN_BEAMS_LAYER;

        //                        btr.AppendEntity(ln);
        //                        trans.AddNewlyCreatedDBObject(ln, true);

        //                        // add out beam lines to our collection.
        //                        BeamLines.Add(ln);

        //                        // Add boundaries of our beams
        //                        // edge 1
        //                        Line edge1 = OffsetLine(ln, beam_y_width * 0.5, bt, btr) as Line;
        //                        MoveLineToLayer(edge1, DEFAULT_FDN_BEAMS_LAYER);
        //                        LineSetLinetype(edge1, "HIDDENX2");
        //                        // edge 2
        //                        Line edge2 = OffsetLine(ln, -beam_y_width * 0.5, bt, btr) as Line;
        //                        MoveLineToLayer(edge2, DEFAULT_FDN_BEAMS_LAYER);
        //                        LineSetLinetype(edge2, "HIDDENX2");

        //                        //     BeamLines.Add(edge1);
        //                        //     BeamLines.Add(edge2);

        //                        count++;
        //                    }
        //                    catch (System.Exception ex)
        //                    {
        //                        edt.WriteMessage("\nError encountered - drawing beam centerline extent lines: " + ex.Message);
        //                        trans.Abort();
        //                        return;
        //                    }

        //                }

        //                #region GradeBeams
        //                ///////////////////////////////////////////////////////////////////////////  
        //                // For each grade beam, find intersection points of the grade beams with
        //                // the foundation border using the entities of the BeamList
        //                //
        //                // Trim the line to the physical edge of the slab (not the limits rectangle)
        //                ///////////////////////////////////////////////////////////////////////////
        //                // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

        //                Point3dCollection points = null;
        //                if (foundationPerimeterPolyline != null && BeamLines.Count > 0)
        //                {
        //                    foreach (var beamline in BeamLines)
        //                    {
        //                        // Get the collection of intersection points and sort them
        //                        //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
        //                        points = IntersectionPointsOnPolyline(beamline, foundationPerimeterPolyline);
        //                        Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, points);

        //                        // We have the intersection points for each of the beam center lines, now mark them with circles
        //                        if (sorted_points != null)
        //                        {
        //                            // Mark the circles for intersection
        //                            for (int i = 0; i < sorted_points.Length; i++)
        //                            {
        //                                double radius = circle_radius;

        //                                try
        //                                {
        //                                    var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
        //                                    circle.ColorIndex = 1;
        //                                    circle.Layer = DEFAULT_FDN_ANNOTATION_LAYER;

        //                                    modelSpace.AppendEntity(circle);
        //                                    trans.AddNewlyCreatedDBObject(circle, true);
        //                                }
        //                                catch (System.Exception ex)
        //                                {
        //                                    edt.WriteMessage("\nError encountered while drawing intersection points: " + ex.Message);
        //                                    trans.Abort();
        //                                    return;
        //                                }
        //                            }

        //                            // Draw the trimmed beam center lines
        //                            try
        //                            {
        //                                TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, foundationPerimeterPolyline, true);
        //                            }
        //                            catch (System.Exception ex)
        //                            {
        //                                edt.WriteMessage("\nError encountered while trimming beam centerline to foundation extents: " + ex.Message);
        //                                trans.Abort();
        //                                return;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    edt.WriteMessage("\nError with creating beam objects");
        //                    trans.Abort();
        //                    return;
        //                }

        //                #endregion


        //                #region GradeBeam Strands
        //                ///////////////////////////////////////////////////////////////////////////  
        //                // For each grade beam, find intersection points of the grade beams with
        //                // the foundation border using the entities of the BeamList
        //                //
        //                // Trim the line to the physical edge of the slab (not the limits rectangle)
        //                ///////////////////////////////////////////////////////////////////////////
        //                // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

        //                Point3dCollection strand_points = null;
        //                if (foundationPerimeterPolyline != null && BeamLines.Count > 0)
        //                {
        //                    foreach (var beamline in BeamLines)
        //                    {
        //                        // Get the collection of intersection points and sort them
        //                        //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
        //                        strand_points = IntersectionPointsOnPolyline(beamline, foundationPerimeterPolyline);
        //                        Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, strand_points);

        //                        // We have the intersection points for each of the beam center lines, now mark them with circles
        //                        if (sorted_points != null)
        //                        {
        //                            // Mark the circles for intersection
        //                            for (int i = 0; i < sorted_points.Length; i++)
        //                            {
        //                                double radius = circle_radius * 0.25;

        //                                try
        //                                {
        //                                    // Draw the strand markers
        //                                    var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
        //                                    circle.Layer = DEFAULT_FDN_BEAM_STRANDS_LAYER;

        //                                    modelSpace.AppendEntity(circle);
        //                                    trans.AddNewlyCreatedDBObject(circle, true);

        //                                    // Add strand labels -- stopping for the last point
        //                                    if (i != sorted_points.Length - 1)
        //                                    {
        //                                        // Only display at the start end
        //                                        if (i % 2 == 0)
        //                                        {
        //                                            AddStrandLabel(edt, trans, btr, sorted_points[i], sorted_points[i + 1]);
        //                                        }
        //                                    }
        //                                }
        //                                catch (System.Exception ex)
        //                                {
        //                                    edt.WriteMessage("\nError encountered while drawing strand intersection points: " + ex.Message);
        //                                    trans.Abort();
        //                                    return;
        //                                }
        //                            }

        //                            // Draw the trimmed strand center lines
        //                            try
        //                            {
        //                                TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, foundationPerimeterPolyline, false);
        //                            }
        //                            catch (System.Exception ex)
        //                            {
        //                                edt.WriteMessage("\nError encountered while trimming strand line to foundation extents: " + ex.Message);
        //                                trans.Abort();
        //                                return;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    edt.WriteMessage("\nError with creating beam objects");
        //                    trans.Abort();
        //                    return;
        //                }

        //                #endregion 

        //                trans.Commit();
        //            }
        //            catch (System.Exception ex)
        //            {
        //                edt.WriteMessage("\nError encountered while drawing beam objects: " + ex.Message);
        //                trans.Abort();
        //                return;
        //            }
        //        }
        //    }
        //}



        //private static void AddStrandLabel(Editor edt, Transaction trans, BlockTableRecord btr, Point3d pt1, Point3d pt2)
        //{
        //    Vector3d vector = pt1.GetVectorTo(pt2);
        //    var length = vector.Length;

        //    // TODO:  CHANGE STRAND LABEL TO HANDLE DOUBLE AND TRIPLE STRANDS (DS, TS)
        //    string txt = "S" + Math.Ceiling((length / 12.0) *10).ToString();
        //    Point3d insPt = pt1;

        //    // Get the angle of the polyline
        //    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //    using (MText mtx = new MText())
        //    {
        //        try
        //        {
        //            mtx.Contents = txt;
        //            mtx.Location = insPt;
        //            mtx.TextHeight = 2;
        //            mtx.ColorIndex = 3;

        //            mtx.Layer = DEFAULT_FDN_TEXTS_LAYER;

        //            mtx.Rotation = angle;

        //            btr.AppendEntity(mtx);
        //            trans.AddNewlyCreatedDBObject(mtx, true);
        //        }
        //        catch (System.Exception ex)
        //        {
        //            edt.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
        //            trans.Abort();
        //            return;
        //        }

        //    }
        //}

        ///// <summary>
        ///// Takes a Point3DCollection and sorts the points from left to right and bottom to top and returns
        ///// an Point3d[]
        ///// </summary>
        ///// <param name="edt">AutoCAD editor object (for messaging)</param>
        ///// <param name="points">A <see cref="Points3dCollection"/> of points </param>
        ///// <returns>An array of Points3d[]</returns>
        //private static Point3d[] SortIntersectionPoint3DArray(Editor edt, Point3dCollection points)
        //{
        //    // Sort the collection of points into an array sorted from descending to ascending
        //    Point3d[] sorted_points = new Point3d[points.Count];

        //    // If the point is horizontal
        //    if (Math.Abs(points[1].Y - points[0].Y) < DEFAULT_HORIZONTAL_TOLERANCE)
        //    {
        //        //edt.WriteMessage("\nBeam " + beamCount + " is horizontal");
        //        sorted_points = sortPoint3dByHorizontally(points);
        //    }
        //    // Otherwise it is vertical
        //    else
        //    {
        //        //edt.WriteMessage("\nBeam " + beamCount + " is vertical");
        //        sorted_points = sortPoint3dByVertically(points);
        //    }

        //    if (sorted_points != null)
        //        edt.WriteMessage("\n" + sorted_points.Length + " points are intersecting the polyline");
        //    else
        //        edt.WriteMessage("\nNo points of intersection found");
        //    return sorted_points;
        //}

        ///// <summary>
        ///// Trim the beam lines to the foundation polyline.
        ///// Beam lines are drawn from the fondation extens polyline so they always start and end outside of an edge.
        ///// For beamlines that cross multiple edges at 'X' location
        ///// ------X----------------X-------------------X---------------X------------------X----------------X-----
        /////       0                1                   2               3                  4                5
        /////
        ///// should be broken into adjacent pairs
        /////       X----------------X                   X---------------X                  X----------------X
        /////       0                1                   2               3                  4                5
        ///// 
        ///// This function will draw them as a polyline so that we can vary the width and eventually cause a strand stagger
        ///// </summary>
        ///// <param name="edt">Autocad Editor object</param>
        ///// <param name="trans">The current database transaction</param>
        ///// <param name="btr">The AutoCAD model space block table record</param>
        ///// <param name="points">The points to trim</param>

        //private static void TrimLinesToPolylineIntersection(Editor edt, Transaction trans, BlockTableRecord btr, Point3d[] points, Polyline pline, bool shouldAddLabel=true)
        //{

        //    // Must be an even number of points currently
        //    // TODO:  Determine logic to hand odd number of intersection points -- which could occur for a tangent point to a corner.
        //    if(points.Length % 2 == 0)
        //    {
        //        for (int i = 0; i < points.Length; i = i + 2)
        //        {
        //            // Send a message to the user
        //            //edt.WriteMessage("\nDrawing a shortened Line object: ");
        //            Polyline pl = new Polyline();
        //            Point2d pt1 = new Point2d(points[i].X, points[i].Y);
        //            Point2d pt2 = new Point2d(points[i + 1].X, points[i + 1].Y);

        //            try
        //            {
        //                pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                pl.Closed = false;
        //                pl.ColorIndex = 150; // blue color
        //                pl.ConstantWidth = 1;
        //                pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                pl.Linetype = "CENTER";

        //                // Set the default properties
        //                pl.SetDatabaseDefaults();
        //                btr.AppendEntity(pl);
        //                trans.AddNewlyCreatedDBObject(pl, true);
        //            } catch (System.Exception ex)
        //            {
        //                edt.WriteMessage("\nError encountered while drawing trimmed beam line object from Pt1: (" 
        //                    + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                trans.Abort();
        //                return;
        //            }

        //            // Get the angle of the polyline
        //            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //            // Add number labels for each line segment
        //            if(shouldAddLabel)
        //                AddBeamLabels(edt, trans, btr, points, i, angle);

        //            beamCount++;
        //        }
        //    } else
        //    {
        //        for (int i = 0; i < points.Length; i = i + 2)
        //        {
        //            // Send a message to the user
        //            //edt.WriteMessage("\nDrawing a shortened Line object: ");

        //            Polyline pl = new Polyline();
        //            Point2d pt1 = new Point2d(points[i].X, points[i].Y);
        //            Point2d pt2 = new Point2d(points[points.Length-1].X, points[points.Length - 1].Y);

        //            // Assume Line AB and Polyline segment CD
        //            var overlap_case = HorizontalTestLineOverLapPolyline(pline, pt1, pt2);

        //            switch (overlap_case)
        //            {
        //                case 0:
        //                    {
        //                        try
        //                        {
        //                            pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                            pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                            pl.Closed = false;
        //                            pl.ColorIndex = 2; // yellow color
        //                            pl.ConstantWidth = 8;
        //                            pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                            pl.Linetype = "CENTER";

        //                            // Set the default properties
        //                            pl.SetDatabaseDefaults();
        //                            btr.AppendEntity(pl);
        //                            trans.AddNewlyCreatedDBObject(pl, true);

        //                            // Get the angle of the polyline
        //                            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //                            // Add number labels for each line segment
        //                            AddBeamLabels(edt, trans, btr, points, i, angle);

        //                        } catch (System.Exception ex)
        //                        {
        //                            edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
        //                                + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                            trans.Abort();
        //                            return;
        //                        }

        //                        break;
        //                    }

        //                default:
        //                    {
        //                        try
        //                        {
        //                            pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                            pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                            pl.Closed = false;
        //                            pl.ColorIndex = 1; // red color
        //                            pl.ConstantWidth = 8;

        //                            // Set the default properties
        //                            pl.SetDatabaseDefaults();
        //                            btr.AppendEntity(pl);
        //                            trans.AddNewlyCreatedDBObject(pl, true);
        //                            pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                            pl.Linetype = "CENTER";

        //                            // Get the angle of the polyline
        //                            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //                            // Add number labels for each line segment
        //                            AddBeamLabels(edt, trans, btr, points, i, angle);

        //                        }
        //                        catch (System.Exception ex)
        //                        {
        //                            edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
        //                                + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                            trans.Abort();
        //                            return;
        //                        }

        //                        break;
        //                    }
        //            }
        //        }

        //        edt.WriteMessage("\nError: The points list should have an even number of points");
        //    }
        //}






