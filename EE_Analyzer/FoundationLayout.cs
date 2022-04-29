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

namespace EE_Analyzer
{
    public class FoundationLayout
    {
        private const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal
        private static int beamCount = 0;

        private const string DEFAULT_FDN_BOUNDINGBOX_LAYER = "_EE_FDN_BOUNDINGBOX"; // Contains the bounding box 
        private const string DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER = "_EE_FDN_PERIMETER";  // Contains the polyline for the perimeter of the foundation
        private const string DEFAULT_FDN_BEAMS_LAYER = "_EE_FDN_BEAMS"; // For the untrimmed ribs of the foundation
        private const string DEFAULT_FDN_BEAMS_TRIMMED_LAYER = "_EE_FDN_BEAMS_TRIMMED";  
        private const string DEFAULT_FDN_BEAM_STRANDS_LAYER = "_EE_FDN_BEAM_STRANDS";
        private const string DEFAULT_FDN_SLAB_STRANDS_LAYER = "_EE_FDN_SLAB_STRANDS";
        private const string DEFAULT_FDN_TEXTS_LAYER = "_EE_FDN_TEXT";
        private const string DEFAULT_FDN_DIMENSIONS_LAYER = "_EE_FDN_DIMENSIONS";
        private const string DEFAULT_FDN_ANNOTATION_LAYER = "_EE_FDN_ANNOTATION_LAYER"; // for notes and markers

        [CommandMethod("EE_FDN")]
        public static void DrawFoundationDetails(int x_qty, double x_spa, double x_depth, double x_width, 
            int y_qty, double y_spa, double y_depth, double y_width,
            int bx_strand_qty, int sx_strand_qty, int by_strand_qty, int sy_strand_qty)
        {
            double beam_x_spa = x_spa;  // spacing between horizontal beams
            double beam_x_width = x_width;  // horizontal beam width
            double beam_x_depth = x_depth;  // horizontal beam depth
            int beam_x_qty = x_qty;         // horizontal beam qty
            int beam_x_strand_qty = bx_strand_qty;  // number of strands in each x-direction beam
            int slab_x_strand_qty = sx_strand_qty;  // number of strands in x-direction slab

            double beam_y_spa = y_spa;  // spacing between vertical beams
            double beam_y_width = y_width;  // vertical beam width
            double beam_y_depth = y_depth;  // vertical beam depth
            int beam_y_qty = y_qty;         // vertical beam qty 
            int beam_y_strand_qty = by_strand_qty;  // number of strands in each y-direction beam
            int slab_y_strand_qty = sy_strand_qty;  // number of strands in y-direction slab


            double circle_radius = beam_x_spa * 0.1; // for marking the intersections of beams and strands with the foundation polyline

            int max_beams = 75;  // define the maximum number of beams in a given direction -- in case we get into an infinite loop situation.

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

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



            var options = new PromptEntityOptions("\nSelect Foundation Polyline");
            options.SetRejectMessage("\nSelected object is not a polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            List<Line> BeamLines = new List<Line>();
            Polyline pline = null;

            // Select the polyline for the foundation
            var result = edt.GetEntity(options);

            if (result.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    var modelSpace = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    try
                    {
                        BlockTable bt;
                        bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                        BlockTableRecord btr;
                        btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        pline = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;
                     
                        ///////////////////////////////////////////////////
                        /// Now start processing  the foundation polylines
                        /// ///////////////////////////////////////////////
                        var numVertices = pline.NumberOfVertices;
                        var lstVertices = GetVertices(pline);

                        if (lstVertices.Count < 4)
                        {
                            edt.WriteMessage("\nFoundation must have at least four sides.  The selected polygon only has " + lstVertices.Count);
                            return;
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
                            // Displays all the vertices
                            edt.WriteMessage("\nV: " + vert.X + " , " + vert.Y);
                            
                            
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
                        // TODO:  Add to FDN layer
                        Point2d boundP1 = new Point2d(lx, by);  // lower left
                        Point2d boundP2 = new Point2d(lx, ty);  // upper left
                        Point2d boundP3 = new Point2d(rx, ty);  // upper right
                        Point2d boundP4 = new Point2d(rx, by);  // bottom right

                        Polyline pl = new Polyline();

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
                        } 
                        catch (System.Exception ex)
                        {
                            edt.WriteMessage("\nError encountered drawing foundation boundary line: " + ex.Message);
                            trans.Abort();
                            return;
                        }

                        // Check that the polyline is in a clockwise winding.  If not, then reverse the polyline direction
                        // -- necessary for the offset functions later to work correctly.
                        if (!PolylineIsWoundClockwise(pline))
                        {
                            edt.WriteMessage("\nReversing foundation polyline direction to make it Clockwise");
                            ReversePolylineDirection(pline);
                        }

                        //////////////////////////////////////////////////////
                        // Draw the perimeter beam -- perimeter line will be continuous, inner edge line will be hidden
                        // both will be assigned to the boundary perimeter layer
                        //////////////////////////////////////////////////////
                        MovePolylineToLayer(pline, DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER);
                        PolylineSetLinetype(pline, "CONTINUOUS");

                        // Offset the perimeter polyline and move it to its appropriate layer
                        Polyline innerPerimeterBeamPolyline = OffsetPolyline(pline, beam_x_width, bt, btr);
                        MovePolylineToLayer(innerPerimeterBeamPolyline, DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER);
                        PolylineSetLinetype(innerPerimeterBeamPolyline, "HIDDEN");

                        ////////////////////////////////////////////////////
                        // Draw centerlines of intermediate horizontal beams
                        ////////////////////////////////////////////////////
                        int count = 0;

                        // offset the first beam by half a beam width
                        while (boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa < boundP2.Y && count < max_beams)
                        {
                            try
                            {
                                Point3d p1 = new Point3d(boundP1.X, boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa, 0);
                                Point3d p2 = new Point3d(boundP4.X, boundP1.Y + (beam_x_width * 0.5) + count * beam_x_spa, 0);
                                Line ln = new Line(p1, p2);

                                // Trim the line to the physical edge of the slab (not the limits rectangle)
                                ln = TrimLineToPolyline(ln, pl);

                                //ln.ColorIndex = 1;  // Color is red
                                ln.Linetype = "CENTERX2";                                
                                ln.Layer = DEFAULT_FDN_BEAMS_LAYER;

                                btr.AppendEntity(ln);
                                trans.AddNewlyCreatedDBObject(ln, true);

                                // add our beam lines to our collection.
                                BeamLines.Add(ln);

                                // Add boundaries of our beams
                                // edge 1
                                Line edge1 = OffsetLine(ln, beam_x_width * 0.5, bt, btr) as Line;
                                MoveLineToLayer(edge1, DEFAULT_FDN_BEAMS_LAYER);
                                LineSetLinetype(edge1, "HIDDENX2");
                                // edge 2
                                Line edge2 = OffsetLine(ln, -beam_x_width * 0.5, bt, btr) as Line;
                                MoveLineToLayer(edge2, DEFAULT_FDN_BEAMS_LAYER);
                                LineSetLinetype(edge2, "HIDDENX2");

                           //     BeamLines.Add(edge1);
                           //     BeamLines.Add(edge2);
                            }
                            catch (System.Exception ex)
                            {
                                edt.WriteMessage("\nError encountered - drawing horizontal beams: " + ex.Message);
                                trans.Abort();
                                return;
                            }

                            count++;
                        }

                        ////////////////////////////////////////////////////
                        // Draw centerlines of intermediate vertical beams
                        ////////////////////////////////////////////////////
                        count = 0;
                        
                        // offset the first beam by have a beam width
                        while (boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa < boundP4.X && count < max_beams)
                        {
                            try
                            {
                                // Send a message to the user
                                Point3d p1 = new Point3d(boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa, boundP1.Y, 0);
                                Point3d p2 = new Point3d(boundP1.X + (beam_y_width * 0.5) + count * beam_y_spa, boundP2.Y, 0);
                                Line ln = new Line(p1, p2);
                                //ln.ColorIndex = 2;  // Color is red

                                ln.Linetype = "CENTERX2";                                
                                ln.Layer = DEFAULT_FDN_BEAMS_LAYER;

                                btr.AppendEntity(ln);
                                trans.AddNewlyCreatedDBObject(ln, true);

                                // add out beam lines to our collection.
                                BeamLines.Add(ln);

                                // Add boundaries of our beams
                                // edge 1
                                Line edge1 = OffsetLine(ln, beam_y_width * 0.5, bt, btr) as Line;
                                MoveLineToLayer(edge1, DEFAULT_FDN_BEAMS_LAYER);
                                LineSetLinetype(edge1, "HIDDENX2");
                                // edge 2
                                Line edge2 = OffsetLine(ln, -beam_y_width * 0.5, bt, btr) as Line;
                                MoveLineToLayer(edge2, DEFAULT_FDN_BEAMS_LAYER);
                                LineSetLinetype(edge2, "HIDDENX2");

                           //     BeamLines.Add(edge1);
                           //     BeamLines.Add(edge2);

                                count++;
                            } catch (System.Exception ex)
                            {
                                edt.WriteMessage("\nError encountered - drawing beam centerline extent lines: " + ex.Message);
                                trans.Abort();
                                return;
                            }

                        }

                        #region GradeBeams
                        ///////////////////////////////////////////////////////////////////////////  
                        // For each grade beam, find intersection points of the grade beams with
                        // the foundation border using the entities of the BeamList
                        //
                        // Trim the line to the physical edge of the slab (not the limits rectangle)
                        ///////////////////////////////////////////////////////////////////////////
                        // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

                        Point3dCollection points = null;
                        if (pline != null && BeamLines.Count > 0)
                        {
                            foreach (var beamline in BeamLines)
                            {
                                // Get the collection of intersection points and sort them
                                //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
                                points = IntersectionPointsOnPolyline(beamline, pline);
                                Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, points);

                                // We have the intersection points for each of the beam center lines, now mark them with circles
                                if (sorted_points != null)
                                {
                                    // Mark the circles for intersection
                                    for (int i = 0; i < sorted_points.Length; i++)
                                    {
                                        double radius = circle_radius;

                                        try
                                        {
                                            var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
                                            circle.ColorIndex = 1;
                                            circle.Layer = DEFAULT_FDN_ANNOTATION_LAYER;

                                            modelSpace.AppendEntity(circle);
                                            trans.AddNewlyCreatedDBObject(circle, true);
                                        }
                                        catch (System.Exception ex)
                                        {
                                            edt.WriteMessage("\nError encountered while drawing intersection points: " + ex.Message);
                                            trans.Abort();
                                            return;
                                        }
                                    }

                                    // Draw the trimmed beam center lines
                                    try
                                    {
                                        TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, pline, true);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        edt.WriteMessage("\nError encountered while trimming beam centerline to foundation extents: " + ex.Message);
                                        trans.Abort();
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            edt.WriteMessage("\nError with creating beam objects");
                            trans.Abort();
                            return;
                        }

                        #endregion


                        #region GradeBeam Strands
                        ///////////////////////////////////////////////////////////////////////////  
                        // For each grade beam, find intersection points of the grade beams with
                        // the foundation border using the entities of the BeamList
                        //
                        // Trim the line to the physical edge of the slab (not the limits rectangle)
                        ///////////////////////////////////////////////////////////////////////////
                        // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

                        Point3dCollection strand_points = null;
                        if (pline != null && BeamLines.Count > 0)
                        {
                            foreach (var beamline in BeamLines)
                            {
                                // Get the collection of intersection points and sort them
                                //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
                                strand_points = IntersectionPointsOnPolyline(beamline, pline);
                                Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, strand_points);

                                // We have the intersection points for each of the beam center lines, now mark them with circles
                                if (sorted_points != null)
                                {
                                    // Mark the circles for intersection
                                    for (int i = 0; i < sorted_points.Length; i++)
                                    {
                                        double radius = circle_radius*0.25;

                                        try
                                        {
                                            // Draw the strand markers
                                            var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
                                            circle.Layer = DEFAULT_FDN_BEAM_STRANDS_LAYER;

                                            modelSpace.AppendEntity(circle);
                                            trans.AddNewlyCreatedDBObject(circle, true);

                                            // Add strand labels -- stopping for the last point
                                            if (i != sorted_points.Length - 1)
                                            {
                                                // Only display at the start end
                                                if (i % 2 == 0)
                                                {
                                                    AddStrandLabel(edt, trans, btr, sorted_points[i], sorted_points[i + 1]);
                                                }
                                            }
                                        }
                                        catch (System.Exception ex)
                                        {
                                            edt.WriteMessage("\nError encountered while drawing strand intersection points: " + ex.Message);
                                            trans.Abort();
                                            return;
                                        }
                                    }

                                    // Draw the trimmed strand center lines
                                    try
                                    {
                                        TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, pline, false);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        edt.WriteMessage("\nError encountered while trimming strand line to foundation extents: " + ex.Message);
                                        trans.Abort();
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            edt.WriteMessage("\nError with creating beam objects");
                            trans.Abort();
                            return;
                        }

                        #endregion 

                        trans.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        edt.WriteMessage("\nError encountered while drawing beam objects: " + ex.Message);
                        trans.Abort();
                        return;
                    }
                }
            }
        }

        private static void AddStrandLabel(Editor edt, Transaction trans, BlockTableRecord btr, Point3d pt1, Point3d pt2)
        {
            Vector3d vector = pt1.GetVectorTo(pt2);
            var length = vector.Length;

            // TODO:  CHANGE STRAND LABEL TO HANDLE DOUBLE AND TRIPLE STRANDS (DS, TS)
            string txt = "S" + Math.Ceiling((length / 12.0) *10).ToString();
            Point3d insPt = pt1;

            // Get the angle of the polyline
            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

            using (MText mtx = new MText())
            {
                try
                {
                    mtx.Contents = txt;
                    mtx.Location = insPt;
                    mtx.TextHeight = 2;
                    mtx.ColorIndex = 3;

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

        /// <summary>
        /// Takes a Point3DCollection and sorts the points from left to right and bottom to top and returns
        /// an Point3d[]
        /// </summary>
        /// <param name="edt">AutoCAD editor object (for messaging)</param>
        /// <param name="points">A <see cref="Points3dCollection"/> of points </param>
        /// <returns>An array of Points3d[]</returns>
        private static Point3d[] SortIntersectionPoint3DArray(Editor edt, Point3dCollection points)
        {
            // Sort the collection of points into an array sorted from descending to ascending
            Point3d[] sorted_points = new Point3d[points.Count];

            // If the point is horizontal
            if (Math.Abs(points[1].Y - points[0].Y) < DEFAULT_HORIZONTAL_TOLERANCE)
            {
                //edt.WriteMessage("\nBeam " + beamCount + " is horizontal");
                sorted_points = sortPoint3dByHorizontally(points);
            }
            // Otherwise it is vertical
            else
            {
                //edt.WriteMessage("\nBeam " + beamCount + " is vertical");
                sorted_points = sortPoint3dByVertically(points);
            }

            if (sorted_points != null)
                edt.WriteMessage("\n" + sorted_points.Length + " points are intersecting the polyline");
            else
                edt.WriteMessage("\nNo points of intersection found");
            return sorted_points;
        }

        /// <summary>
        /// Trim the beam lines to the foundation polyline.
        /// Beam lines are drawn from the fondation extens polyline so they always start and end outside of an edge.
        /// For beamlines that cross multiple edges at 'X' location
        /// ------X----------------X-------------------X---------------X------------------X----------------X-----
        ///       0                1                   2               3                  4                5
        ///
        /// should be broken into adjacent pairs
        ///       X----------------X                   X---------------X                  X----------------X
        ///       0                1                   2               3                  4                5
        /// 
        /// This function will draw them as a polyline so that we can vary the width and eventually cause a strand stagger
        /// </summary>
        /// <param name="edt">Autocad Editor object</param>
        /// <param name="trans">The current database transaction</param>
        /// <param name="btr">The AutoCAD model space block table record</param>
        /// <param name="points">The points to trim</param>

        private static void TrimLinesToPolylineIntersection(Editor edt, Transaction trans, BlockTableRecord btr, Point3d[] points, Polyline pline, bool shouldAddLabel=true)
        {
           
            // Must be an even number of points currently
            // TODO:  Determine logic to hand odd number of intersection points -- which could occur for a tangent point to a corner.
            if(points.Length % 2 == 0)
            {
                for (int i = 0; i < points.Length; i = i + 2)
                {
                    // Send a message to the user
                    //edt.WriteMessage("\nDrawing a shortened Line object: ");
                    Polyline pl = new Polyline();
                    Point2d pt1 = new Point2d(points[i].X, points[i].Y);
                    Point2d pt2 = new Point2d(points[i + 1].X, points[i + 1].Y);

                    try
                    {
                        pl.AddVertexAt(0, pt1, 0, 0, 0);
                        pl.AddVertexAt(1, pt2, 0, 0, 0);
                        pl.Closed = false;
                        pl.ColorIndex = 150; // blue color
                        pl.ConstantWidth = 1;
                        pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
                        pl.Linetype = "CENTER";

                        // Set the default properties
                        pl.SetDatabaseDefaults();
                        btr.AppendEntity(pl);
                        trans.AddNewlyCreatedDBObject(pl, true);
                    } catch (System.Exception ex)
                    {
                        edt.WriteMessage("\nError encountered while drawing trimmed beam line object from Pt1: (" 
                            + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
                        trans.Abort();
                        return;
                    }

                    // Get the angle of the polyline
                    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                    // Add number labels for each line segment
                    if(shouldAddLabel)
                        AddBeamLabels(edt, trans, btr, points, i, angle);

                    beamCount++;
                }
            } else
            {
                for (int i = 0; i < points.Length; i = i + 2)
                {
                    // Send a message to the user
                    //edt.WriteMessage("\nDrawing a shortened Line object: ");

                    Polyline pl = new Polyline();
                    Point2d pt1 = new Point2d(points[i].X, points[i].Y);
                    Point2d pt2 = new Point2d(points[points.Length-1].X, points[points.Length - 1].Y);

                    // Assume Line AB and Polyline segment CD
                    var overlap_case = HorizontalTestLineOverLapPolyline(pline, pt1, pt2);

                    switch (overlap_case)
                    {
                        case 0:
                            {
                                try
                                {
                                    pl.AddVertexAt(0, pt1, 0, 0, 0);
                                    pl.AddVertexAt(1, pt2, 0, 0, 0);
                                    pl.Closed = false;
                                    pl.ColorIndex = 2; // yellow color
                                    pl.ConstantWidth = 8;
                                    pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
                                    pl.Linetype = "CENTER";

                                    // Set the default properties
                                    pl.SetDatabaseDefaults();
                                    btr.AppendEntity(pl);
                                    trans.AddNewlyCreatedDBObject(pl, true);

                                    // Get the angle of the polyline
                                    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                                    // Add number labels for each line segment
                                    AddBeamLabels(edt, trans, btr, points, i, angle);
                            
                                } catch (System.Exception ex)
                                {
                                    edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
                                        + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
                                    trans.Abort();
                                    return;
                                }

                                break;
                            }

                        default:
                            {
                                try
                                {
                                    pl.AddVertexAt(0, pt1, 0, 0, 0);
                                    pl.AddVertexAt(1, pt2, 0, 0, 0);
                                    pl.Closed = false;
                                    pl.ColorIndex = 1; // red color
                                    pl.ConstantWidth = 8;

                                    // Set the default properties
                                    pl.SetDatabaseDefaults();
                                    btr.AppendEntity(pl);
                                    trans.AddNewlyCreatedDBObject(pl, true);
                                    pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
                                    pl.Linetype = "CENTER";

                                    // Get the angle of the polyline
                                    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                                    // Add number labels for each line segment
                                    AddBeamLabels(edt, trans, btr, points, i, angle);

                                }
                                catch (System.Exception ex)
                                {
                                    edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
                                        + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
                                    trans.Abort();
                                    return;
                                }

                                break;
                            }
                    }
                }

                edt.WriteMessage("\nError: The points list should have an even number of points");
            }
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
                } catch (System.Exception ex)
                {
                    edt.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
                    trans.Abort();
                    return;
                }

            }
        }

        private static int HorizontalTestLineOverLapPolyline(Polyline pline, Point2d A, Point2d B)
        {

            if(pline.NumberOfVertices < 2)
            {
                throw new System.Exception("Polyline must have at least 2 vertices");
            }

            int overlap_case = 0;

            Point2d temp;
            // swap A and B so that A is always on the left
            if(A.X > B.X)
            {
                temp = A;
                A = B;
                B = temp;
            }
            
            var C = pline.GetPoint2dAt(0);
            var D = pline.GetPoint2dAt(1);

            // swap C and D so that C is always on the left
            if (C.X > D.X)
            {
                temp = C;
                C = D;
                D = temp;
            }

            // Check endpoints
            // Case 1: C and D both within line AB
            // A =========== C ----------- D ========= B or
            //     -- create line segments AC and DB
            if ((C.X > A.X) && (C.X < B.X) && (D.X > A.X) && (D.X < B.X))
            {
                overlap_case = 1;
            }

            // Case 2: C within and D is not within AB
            // A =========== C ----------- B --------- D or
            //     -- create line segment AC and move B to D
            if ((C.X > A.X) && (C.X < B.X) && (D.X > A.X) && (D.X > B.X))
            {
                overlap_case = 2;
            }

            // Case 3: C is not within and D is within AB
            // A =========== D ----------- B --------- C or
            //     -- create line segment AD and move B to C
            if ((C.X > A.X) && (C.X > B.X) && (D.X > A.X) && (D.X < B.X))
            {
                overlap_case = 3;
            }

            // Case 4: Both C and D are outside AB
            // C ----------- D   A ========= B or
            // A =========== B   C ---------- D  
            //     -- create line AB
            if (((C.X < A.X) && (C.X < B.X) && (D.X < A.X) && (D.X < B.X)) ||
                ((C.X > A.X) && (C.X > B.X) && (D.X > A.X) && (D.X > B.X))
                )
            {
                overlap_case = 4;
            }

            return overlap_case;
        }

        [CommandMethod("JIM")]
        public void ShowModalWpfDialogCmd()
        {
            Document doc;
            Database db;
            Editor ed;
            double radius; // radius default value
            string layer;  // layer default value

            // private fields initialization (initial default values)
            doc = AcAp.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;
            // initial default values
            layer = (string)AcAp.GetSystemVariable("clayer");
            radius = 10.0;

            var layers = GetAllLayerNamesList();
            if (!layers.Contains(layer))
            {
                layer = (string)AcAp.GetSystemVariable("clayer");
            }

            // shows the dialog box
            var dialog = new EE_FDNInputDialog(layers, layer, radius);
            var result = AcAp.ShowModalWindow(dialog);
            if (result.Value)
            {
                // fields update
                layer = dialog.Layer;
                radius = dialog.Radius;

                // circle drawing
                var ppr = ed.GetPoint("\nSpecify the center: ");
                if (ppr.Status == PromptStatus.OK)
                {
                    // drawing the circlr in current space
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var curSpace =
                            (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        using (var circle = new Circle(ppr.Value, Vector3d.ZAxis, radius))
                        {
                            circle.TransformBy(ed.CurrentUserCoordinateSystem);
                            circle.Layer = layer;
                            curSpace.AppendEntity(circle);
                            tr.AddNewlyCreatedDBObject(circle, true);
                        }
                        tr.Commit();
                    }
                }
            }

        }
    }
}
