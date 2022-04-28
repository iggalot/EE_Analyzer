using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_Analyzer
{
    public class FoundationLayout
    {
        private const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal
        private static int beamCount = 0;

        [CommandMethod("TEST")]
        public void Test()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpace = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                // get all curve entities in model space
                var curveClass = RXObject.GetClass(typeof(Curve));
                var curves = modelSpace
                    .Cast<ObjectId>()
                    .Where(id => id.ObjectClass.IsDerivedFrom(curveClass))
                    .Select(id => (Curve)tr.GetObject(id, OpenMode.ForRead))
                    .ToArray();

                ed.WriteMessage("\n" + curves.Length + " polylines found");

                // get all intersections (brute force algorithm O(n²) complexity)
                var points = new Point3dCollection();
                for (int i = 0; i < curves.Length - 1; i++)
                {
                    ed.WriteMessage("\n" + i + "points found");

                    for (int j = i + 1; j < curves.Length; j++)
                    {
                        curves[i].IntersectWith(curves[j], Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                        ed.WriteMessage("\n" + points.Count + "points found");
                    }
                }

                // draw the circles
                double radius = (db.Extmax.Y - db.Extmin.Y) / 100.0;
                foreach (Point3d point in points)
                {
                    var circle = new Circle(point, Vector3d.ZAxis, radius);
                    circle.ColorIndex = 1;
                    modelSpace.AppendEntity(circle);
                    tr.AddNewlyCreatedDBObject(circle, true);
                }
                tr.Commit();
            }
        }

        [CommandMethod("Test2")]
        public void Test2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            var options1 = new PromptEntityOptions("\nSelect Polyline");
            options1.SetRejectMessage("\nSelected object is not a polyline.");
            options1.AddAllowedClass(typeof(Polyline), true);

            var options2 = new PromptEntityOptions("\nSelect Line");
            options2.SetRejectMessage("\nSelected object is not a line.");
            options2.AddAllowedClass(typeof(Line), true);

            var pline_result = edt.GetEntity(options1);
            var line_result = edt.GetEntity(options2);

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                var modelSpace = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                if (pline_result.Status == PromptStatus.OK && line_result.Status == PromptStatus.OK)
                {
                    var pline = trans.GetObject(pline_result.ObjectId, OpenMode.ForRead) as Polyline;
                    var line = trans.GetObject(line_result.ObjectId, OpenMode.ForRead) as Line;

                    Point3dCollection points = IntersectionPointsOnPolyline(line, pline);
                    edt.WriteMessage("\n" + points.Count + " intersection points found");

                    // draw the circles
                    double radius = (db.Extmax.Y - db.Extmin.Y) / 100.0;
                    foreach (Point3d point in points)
                    {
                        var circle = new Circle(point, Vector3d.ZAxis, radius);
                        circle.ColorIndex = 1;
                        modelSpace.AppendEntity(circle);
                        trans.AddNewlyCreatedDBObject(circle, true);
                    }
                    trans.Commit();
                } else
                {
                    trans.Abort();
                }
            }
        }

        private string DisplayPrint3DCollection(Point3dCollection coll)
        {
            string str = "";
            foreach (Point3d point in coll)
            {
                str += point.X + " , " + point.Y + "\n";
            }
            return str;
        }

        /// <summary>
        /// Bubble sort for x-direction of Point3dCollection
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        private static Point3d[] sortPoint3dByHorizontally(Point3dCollection coll)
        {
            Point3d[] sort_arr = new Point3d[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3d temp;

            for (int j = 0; j < coll.Count-1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].X > sort_arr[i + 1].X)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                        
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Bubble sort for Point3d of y-direction
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        private static Point3d[] sortPoint3dByVertically(Point3dCollection coll)
        {
            Point3d[] sort_arr = new Point3d[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3d temp;

            for (int j = 0; j < coll.Count - 1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].Y > sort_arr[i + 1].Y)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }

                }
            }
            return sort_arr;
        }

        private static Point3dCollection IntersectionPointsOnPolyline(Line ln, Polyline pline)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var points = new Point3dCollection();

            var curves = ln;
            for (int i = 0; i < curves.Length - 1; i++)
            {
                    curves.IntersectWith(pline, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
            }
            return points;
        }

        // Find the point where two line segments intersect
        private static Point3d FindPointOfIntersectLines_2D(Line l1, Line l2)
        {
            var A1 = -(l1.EndPoint.Y - l1.StartPoint.Y);
            var B1 = l1.EndPoint.X - l1.StartPoint.X;
            var C1 = (A1 * l1.StartPoint.X + B1 * l1.StartPoint.Y);

            var A2 = -(l2.EndPoint.Y - l2.StartPoint.Y);
            var B2 = l2.EndPoint.X - l2.StartPoint.X;
            var C2 = (A2 * l2.StartPoint.X + B2 * l2.StartPoint.Y);

            var delta = A1 * B2 - A2 * B1;
            var intX = (B2 * C1 - B1 * C2)/ delta;
            var inty = (A1 * C2 - A2 * C1) / delta;

            return new Point3d(intX, inty, 0);

        }

        private static Line TrimLineToPolyline(Line ln, Polyline pl)
        {
            // find segment of polyline that brackets the line to be trimmed

            // Test the segments of the polyline to find the 

            Line testline = new Line(pl.GetPoint3dAt(0), pl.GetPoint3dAt(1));

            Point3d newPt = FindPointOfIntersectLines_2D(ln, testline);
            ln.StartPoint = newPt;
            return ln;
        }

        // Loads on autocad linetype into the drawing.
        private static void LoadLineTypes(string name, Document doc, Database db)
        {
            // change the linetype
            string ltypeName = name;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltTab = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                if (ltTab.Has(ltypeName))
                {
                    doc.Editor.WriteMessage("\nLinetype [" + ltypeName + "] is already loaded.");
                    trans.Abort();
                }
                else
                {
                    // Load the linetype
                    db.LoadLineTypeFile(ltypeName, "acad.lin");
                    doc.Editor.WriteMessage("\nLinetype [" + ltypeName + "] was created successfully.");
                    trans.Commit();                 }
            }
        }

        // Return a list of vertices for a selected polyline
        public static List<Point2d> GetVertices(Polyline pl)
        {
            Transaction tr = pl.Database.TransactionManager.TopTransaction;
            
            List<Point2d> vertices = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d pt = pl.GetPoint2dAt(i);
                vertices.Add(pt);
            }

            return vertices;
        }

        [CommandMethod("EE_FDN")]
        public static void DrawFoundationDetails(int x_qty, double x_spa, double x_depth, double x_width, int y_qty, double y_spa, double y_depth, double y_width)
        {
            double beam_x_spa = x_spa;  // spacing between horizontal beams
            double beam_x_width = x_width;  // horizontal beam width
            double beam_x_depth = x_depth;  // horizontal beam depth
            int beam_x_qty = x_qty;         // horizontal beam qty

            double beam_y_spa = y_spa;  // spacing between vertical beams
            double beam_y_width = y_width;  // vertical beam width
            double beam_y_depth = y_depth;  // vertical beam depth
            int beam_y_qty = y_qty;         // vertical beam qty 


            double circle_radius = beam_x_spa * 0.1;

            int max_beams = 50;  // define the maximum number of beams in a given direction.

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            // Load our linetype
            LoadLineTypes("CENTER", doc, db);
            LoadLineTypes("DASHED", doc, db);

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


                        // Send a message to the user
                        edt.WriteMessage("\nFoundation polyline selected");

                        ///////////////////////////////////////////
                        /// This processes the foundation polyline
                        /// ///////////////////////////////////////
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

                        // Specify the polyline parameters 
                        edt.WriteMessage("\nDrawing a polyline object!");
                        Polyline pl = new Polyline();
                        pl.AddVertexAt(0, boundP1, 0, 0, 0);
                        pl.AddVertexAt(1, boundP2, 0, 0, 0);
                        pl.AddVertexAt(2, boundP3, 0, 0, 0);
                        pl.AddVertexAt(3, boundP4, 0, 0, 0);
                        pl.Closed = true;
                        pl.ColorIndex = 140; // cyan color
 
                        // Set the default properties
                        pl.SetDatabaseDefaults();
                        btr.AppendEntity(pl);
                        trans.AddNewlyCreatedDBObject(pl, true);

                        ////////////////////////////////////////////////////
                        // Draw centerlines of intermediate horizontal beams
                        ////////////////////////////////////////////////////
                        int count = 0;
                        while (boundP1.Y + count * beam_x_spa < boundP2.Y && count < max_beams)
                        {
                            try
                            {
                                Point3d p1 = new Point3d(boundP1.X, boundP1.Y + count * beam_x_spa, 0);
                                Point3d p2 = new Point3d(boundP4.X, boundP1.Y + count * beam_x_spa, 0);
                                Line ln = new Line(p1, p2);

                                // Trim the line to the physical edge of the slab (not the limits rectangle)
                                ln = TrimLineToPolyline(ln, pl);

                                ln.ColorIndex = 1;  // Color is red
                                ln.Linetype = "CENTER";

                                btr.AppendEntity(ln);
                                trans.AddNewlyCreatedDBObject(ln, true);

                                // add out beam lines to our collection.
                                BeamLines.Add(ln);
                            }
                            catch (System.Exception ex)
                            {
                                edt.WriteMessage("\nError encountered - drawing horizontal beams: " + ex.Message);
                                trans.Abort();
                            }

                            count++;
                        }

                        ////////////////////////////////////////////////////
                        // Draw centerlines of intermediate vertical beams
                        ////////////////////////////////////////////////////
                        count = 0;
                        while (boundP1.X + count * beam_y_spa < boundP4.X && count < max_beams)
                        {
                            try
                            {
                                // Send a message to the user
                                Point3d p1 = new Point3d(boundP1.X + count * beam_y_spa, boundP1.Y, 0);
                                Point3d p2 = new Point3d(boundP1.X + count * beam_y_spa, boundP2.Y, 0);
                                Line ln = new Line(p1, p2);
                                ln.ColorIndex = 2;  // Color is red

                                ln.Linetype = "DASHED";

                                btr.AppendEntity(ln);
                                trans.AddNewlyCreatedDBObject(ln, true);

                                // add out beam lines to our collection.
                                BeamLines.Add(ln);

                                count++;
                            } catch (System.Exception ex)
                            {
                                edt.WriteMessage("\nError encountered - drawing beam centerline extent lines: " + ex.Message);
                                trans.Abort();
                            }

                        }

                        ///////////////////////////////////////////////////////////////////////////  
                        // For each grade beam, find intersection points of the grade beams with
                        // the foundation border using the entities of the BeamList
                        //
                        // Trim the line to the physical edge of the slab (not the limits rectangle)
                        ///////////////////////////////////////////////////////////////////////////
                        edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

                        Point3dCollection points = null;

                        if (pline != null && BeamLines.Count > 0)
                        {
                            foreach (var beamline in BeamLines)
                            {
                                // Get the collection of intersection points
                                points = IntersectionPointsOnPolyline(beamline, pline);

                                // Sort the collection of points into an array sorted from descending to ascending
                                Point3d[] sorted_points = new Point3d[points.Count];
                                

                                // If the point is horizontal
                                if (Math.Abs(points[1].Y-points[0].Y) < DEFAULT_HORIZONTAL_TOLERANCE)
                                {
                                    edt.WriteMessage("\nBeam " + beamCount + " is horizontal");
                                    sorted_points = sortPoint3dByHorizontally(points);
                                }
                                // Otherwise it is vertical
                                else
                                {
                                    edt.WriteMessage("\nBeam " + beamCount + " is vertical");
                                    sorted_points = sortPoint3dByVertically(points);

                                }

                                //edt.WriteMessage("\nBEFORE===============================================");
                                //edt.WriteMessage(DisplayPrint3DCollection(points));

                                //points = new Point3dCollection(temp);

                                //edt.WriteMessage("\nAFTER================================================");
                                //edt.WriteMessage(DisplayPrint3DCollection(points));


                                if (sorted_points != null)
                                    edt.WriteMessage("\n" + sorted_points.Length + " points are intersecting the BeamLines");
                                else
                                    edt.WriteMessage("\nNo points of intersection found");

                                // TODO:  Change these circles for the intersection points to be the new endpoints of the beam and strand lines
                                // We have the intersection points of the strand lines, now start trimming


                                // draw the circles
                                //double radius = (db.Extmax.Y - db.Extmin.Y) / 100.0;
                                double radius = circle_radius;

                                try
                                {
                                    if (points != null)
                                    {
                                        // Mark the circles for intersection
                                        for (int i = 0; i < sorted_points.Length; i++)
                                        {
                                            var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
                                            circle.ColorIndex = 1;
                                            modelSpace.AppendEntity(circle);
                                            trans.AddNewlyCreatedDBObject(circle, true);
                                        }

                                        // Draw trimmed beam center lines
                                        TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, pline);

                                    }
                                } 
                                catch (System.Exception ex)
                                {
                                    edt.WriteMessage("\nError encountered - trimming beam centerline to foundation extents: " + ex.Message);
                                    trans.Abort();
                                }

                            }
                        }
                        else
                        {
                            edt.WriteMessage("\nError with foundation polyline object.");
                            trans.Abort();
                        }

                        trans.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        edt.WriteMessage("\nError encountered while drawing beam objects: " + ex.Message);
                        trans.Abort();
                    }
                }
            }
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

        private static void TrimLinesToPolylineIntersection(Editor edt, Transaction trans, BlockTableRecord btr, Point3d[] points, Polyline pline)
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

                    pl.AddVertexAt(0, pt1, 0, 0, 0);
                    pl.AddVertexAt(1, pt2, 0, 0, 0);
                    pl.Closed = false;
                    pl.ColorIndex = 150; // blue color
                    pl.ConstantWidth = 4;

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    // Get the angle of the polyline
                    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                    // Add number labels for each line segment
                    AddBeamLabels(trans, btr, points, i, angle);

                    //Line ln = new Line(points[i], points[i + 1]);
                    //ln.ColorIndex = 150;  // Color is blue
                    ////ln.Linetype = "DASHED";
                    //btr.AppendEntity(ln);
                    //trans.AddNewlyCreatedDBObject(ln, true);
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
                                pl.AddVertexAt(0, pt1, 0, 0, 0);
                                pl.AddVertexAt(1, pt2, 0, 0, 0);
                                pl.Closed = false;
                                pl.ColorIndex = 2; // yellow color
                                pl.ConstantWidth = 8;

                                // Set the default properties
                                pl.SetDatabaseDefaults();
                                btr.AppendEntity(pl);
                                trans.AddNewlyCreatedDBObject(pl, true);

                                // Get the angle of the polyline
                                var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                                // Add number labels for each line segment
                                AddBeamLabels(trans, btr, points, i, angle);

                                break;
                            }

                        default:
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

                                // Get the angle of the polyline
                                var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

                                // Add number labels for each line segment
                                AddBeamLabels(trans, btr, points, i, angle);

                                break;

                            }
                    }
                }

                edt.WriteMessage("\nError: The points list should have an even number of points");
            }
        }

        // Add number labels for each line segment
        private static void AddBeamLabels(Transaction trans, BlockTableRecord btr, Point3d[] points, int i, double angle)
        {
            string txt = "Beam " + beamCount.ToString();
            Point3d insPt = points[i];
            using (MText mtx = new MText())
            {
                mtx.Contents = txt;
                mtx.Location = insPt;
                mtx.TextHeight = 10;
                mtx.ColorIndex = 2;

                mtx.Rotation = angle;

                btr.AppendEntity(mtx);
                trans.AddNewlyCreatedDBObject(mtx, true);
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

            var layers = GetLayerNames();
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

        /// <summary>
        /// Gets the layer list.
        /// </summary>
        /// <param name="db">Database instance this method applies to.</param>
        /// <returns>Layer names list.</returns>
        private List<string> GetLayerNames()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                return ((LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead))
                    .Cast<ObjectId>()
                    .Select(id => ((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name)
                    .ToList();
            }
        }
    }
}
