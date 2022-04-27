﻿using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace EE_Analyzer
{
    public class FoundationLayout
    {
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

                ed.WriteMessage(curves.Length + " polylines found");

                // get all intersections (brute force algorithm O(n²) complexity)
                var points = new Point3dCollection();
                for (int i = 0; i < curves.Length - 1; i++)
                {
                    ed.WriteMessage(i + "points found");

                    for (int j = i + 1; j < curves.Length; j++)
                    {
                        curves[i].IntersectWith(curves[j], Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                        ed.WriteMessage(points.Count + "points found");
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
                    edt.WriteMessage(points.Count + " intersection points found");

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

        private Point3dCollection IntersectionPointsOnPolyline(Line ln, Polyline pline)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var points = new Point3dCollection();

            var curves = ln;
            ed.WriteMessage("curves.Length: " + curves.Length);
            for (int i = 0; i < curves.Length - 1; i++)
            {
 //               for (int j = i + 1; j < curves.Length; j++)
 //               {
                    curves.IntersectWith(pline, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
 //               }
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

        private Line TrimLineToPolyline(Line ln, Polyline pl)
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
                    doc.Editor.WriteMessage("Linetype [" + ltypeName + "] is already loaded.");
                    trans.Abort();
                }
                else
                {
                    // Load the linetype
                    db.LoadLineTypeFile(ltypeName, "acad.lin");
                    doc.Editor.WriteMessage("Linetype [" + ltypeName + "] was created successfully.");
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
        public void DrawFoundationDetails()
        {
            double sx = 120;  // horiz space between beams
            double vert_beam_width = 0.833;  // vertical direction beam width

            double sy = 120;  // vertical space between beams
            double horiz_beam_width = 0.833; // horizontal direction beam width

            double circle_radius = sx * 0.1;

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

            var result = edt.GetEntity(options);
            if (result.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {


                    try
                    {
                        BlockTable bt;
                        bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                        BlockTableRecord btr;
                        btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        pline = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;


                        // Send a message to the user
                        edt.WriteMessage("\nFoundation polyline selected");

                        var numVertices = pline.NumberOfVertices;
                        var lstVertices = GetVertices(pline);

                        if (lstVertices.Count < 4)
                        {
                            edt.WriteMessage("Foundation must have at least four sides.  The selected polygon only has " + lstVertices.Count);
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
                        edt.WriteMessage("Drawing a polyline object!");
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


                        // Draw centerlines of intermediate beams
                        // draw horizontal beams
                        int count = 0;
                        while (boundP1.Y + count * sy < boundP2.Y && count < max_beams)
                        {
                            try
                            {
                                // Send a message to the user
                                edt.WriteMessage("\nDrawing a horizontal Line object: ");

                                Point3d p1 = new Point3d(boundP1.X, boundP1.Y + count * sy, 0);
                                Point3d p2 = new Point3d(boundP4.X, boundP1.Y + count * sy, 0);
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
                                edt.WriteMessage("Error encountered: " + ex.Message);
                                trans.Abort();
                            }

                            count++;
                        }

                        // draw vertical beams
                        count = 0;
                        while (boundP1.X + count * sx < boundP4.X && count < max_beams)
                        {
                            try
                            {
                                // Send a message to the user
                                edt.WriteMessage("\nDrawing a vertical Line object: ");

                                Point3d p1 = new Point3d(boundP1.X + count * sx, boundP1.Y, 0);
                                Point3d p2 = new Point3d(boundP1.X + count * sx, boundP2.Y , 0);
                                Line ln = new Line(p1, p2);
                                ln.ColorIndex = 2;  // Color is red

                                ln.Linetype = "DASHED";

                                btr.AppendEntity(ln);
                                trans.AddNewlyCreatedDBObject(ln, true);

                                // add out beam lines to our collection.
                                BeamLines.Add(ln);
                            }
                            catch (System.Exception ex)
                            {
                                edt.WriteMessage("Error encountered: " + ex.Message);
                                trans.Abort();
                            }

                            count++;
                        }

                        // draw horizontal centerline for beams

                        // draw vertical centerlinee for intermediate beams

                        trans.Commit();

                    }
                    catch (System.Exception ex)
                    {
                        edt.WriteMessage("Error encountered: " + ex.Message);
                        trans.Abort();
                    }
                }

                edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

                //Now trim the beams
                using (Transaction trans2 = db.TransactionManager.StartTransaction())
                {
                    try
                    {

                        var modelSpace2 = (BlockTableRecord)trans2.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                        edt.WriteMessage("After modelSpace2");

                        // Trim the line to the physical edge of the slab (not the limits rectangle)
                        Point3dCollection points = null;

                        if (pline != null && BeamLines.Count > 0)
                        {
                            foreach (var beamline in BeamLines)
                            {
                                points = IntersectionPointsOnPolyline(beamline, pline);

                                if (points != null)
                                    edt.WriteMessage("\n" + points.Count + " points are intersecting the BeamLines");
                                else
                                    edt.WriteMessage("\nNo points of intersection found");

                                // TODO:  Change these circles for the intersection points to be the new endpoints of the beam and strand lines

                                // draw the circles
                                //double radius = (db.Extmax.Y - db.Extmin.Y) / 100.0;
                                double radius = circle_radius;

                                if (points != null)
                                {
                                    foreach (Point3d point in points)
                                    {
                                        var circle = new Circle(point, Vector3d.ZAxis, radius);
                                        circle.ColorIndex = 1;
                                        modelSpace2.AppendEntity(circle);
                                        trans2.AddNewlyCreatedDBObject(circle, true);
                                    }
                                }
                            }
                        }
                        else
                        {
                            edt.WriteMessage("Error with foundation polyline object.");
                        }


                    }
                    catch (System.Exception ex)
                    {
                        edt.WriteMessage("Trans2: Error encountered: " + ex.Message);
                        trans2.Abort();
                    }

                    trans2.Commit();
                }
            }
        }
    }
}
