using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.BlockObjects;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Collections.Generic;
using EE_Analyzer.Utilities;

namespace EE_Analyzer.TestingFunctions
{
    public class TestingFunctions
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

        //[CommandMethod("Test2")]
        //public void Test2()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor edt = doc.Editor;

        //    var options1 = new PromptEntityOptions("\nSelect Polyline");
        //    options1.SetRejectMessage("\nSelected object is not a polyline.");
        //    options1.AddAllowedClass(typeof(Polyline), true);

        //    var options2 = new PromptEntityOptions("\nSelect Line");
        //    options2.SetRejectMessage("\nSelected object is not a line.");
        //    options2.AddAllowedClass(typeof(Line), true);

        //    var pline_result = edt.GetEntity(options1);
        //    var line_result = edt.GetEntity(options2);

        //    using (Transaction trans = db.TransactionManager.StartTransaction())
        //    {
        //        var modelSpace = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

        //        if (pline_result.Status == PromptStatus.OK && line_result.Status == PromptStatus.OK)
        //        {
        //            var pline = trans.GetObject(pline_result.ObjectId, OpenMode.ForRead) as Polyline;
        //            var line = trans.GetObject(line_result.ObjectId, OpenMode.ForRead) as Line;

        //            Point3dCollection points = IntersectionPointsOnPolyline(line, pline);
        //            edt.WriteMessage("\n" + points.Count + " intersection points found");

        //            draw the circles
        //            double radius = (db.Extmax.Y - db.Extmin.Y) / 100.0;
        //            foreach (Point3d point in points)
        //            {
        //                var circle = new Circle(point, Vector3d.ZAxis, radius);
        //                circle.ColorIndex = 1;
        //                modelSpace.AppendEntity(circle);
        //                trans.AddNewlyCreatedDBObject(circle, true);
        //            }
        //            trans.Commit();
        //        }
        //        else
        //        {
        //            trans.Abort();
        //        }
        //    }
        //}

        [CommandMethod("EELayerList")]
        public static void LayerList()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable lyTab = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId lyID in lyTab)
                {
                    LayerTableRecord lytr = trans.GetObject(lyID, OpenMode.ForRead) as LayerTableRecord;
                    doc.Editor.WriteMessage("\nLayer name: " + lytr.Name);
                }

                // Commit the transaction
                trans.Commit();
            }
        }

        [CommandMethod("EELayerCreate")]
        public static void LayerCreate()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable lyTab = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                string sNewLayerName = "EE_Analyzer-NewLayer";
                if (lyTab.Has(sNewLayerName) == false)
                {
                    LayerTableRecord newLTR = new LayerTableRecord();
                    //Assign the layer a color a
                    //nd name
                    newLTR.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                    newLTR.Name = sNewLayerName;

                    //Upgrade the Layer table for write
                    lyTab.UpgradeOpen();

                    //Append the new layer to the layer table and update the transaction
                    lyTab.Add(newLTR);
                    trans.AddNewlyCreatedDBObject(newLTR, true);
                    doc.Editor.WriteMessage("\nLayer created: " + newLTR.Name);
                }

                // Commit the transaction
                trans.Commit();
            }
        }

        /// <summary>
        /// Function to test the line-polyline algorithm
        /// </summary>
        [CommandMethod("EEINT")]
        public void MarkIntersection()
        {
            Polyline FDN_POLY = new Polyline();

            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            // Parse the polyline object in the drawing
            var options = new PromptEntityOptions("\nSelect Foundation Polyline");
            options.SetRejectMessage("\nSelected object is not a polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            // Select the polyline for the foundation
            var polyresult = edt.GetEntity(options);

            Polyline poly = new Polyline();
            if (polyresult.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        poly = trans.GetObject(polyresult.ObjectId, OpenMode.ForRead) as Polyline;
                        trans.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        trans.Abort();
                    }
                }
            }

            // process the foundation line to correct the winding order
            //FDN_POLY = ProcessFoundationPerimeter(db, edt, polyresult);

            var options2 = new PromptEntityOptions("\nSelect Line Object");
            options2.SetRejectMessage("\nSelected object is not a line.");
            options2.AddAllowedClass(typeof(Line), true);

            // Select the polyline for the foundation
            var lnresult = edt.GetEntity(options2);

            Line ln = new Line();
            if (lnresult.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        ln = trans.GetObject(lnresult.ObjectId, OpenMode.ForRead) as Line;
                        trans.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        trans.Abort();
                    }
                }
            }

            int numVerts = poly.NumberOfVertices;

            Point3d b1 = ln.StartPoint;
            Point3d b2 = ln.EndPoint;

            // Add markers for debugging and labelling
            DrawCircle(b1, 8, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
            DrawMtext(db, doc, b1, "LA", 6, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
            DrawCircle(b2, 8, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
            DrawMtext(db, doc, b2, "LB", 6, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

            List<Point3d> intPtList = new List<Point3d>();

            for (int i = 0; i < numVerts; i++)
            {
                doc.Editor.WriteMessage("\nseg " + i.ToString());
                bool isValid = true;
                string str = "c";
                Point3d p1 = poly.GetPoint3dAt(i % numVerts);
                Point3d p2 = poly.GetPoint3dAt((i + 1) % numVerts);

                // Label the polyline segments on the drawing.
                DrawMtext(db, doc, MathHelpers.GetMidpoint(p1, p2), i.ToString(), 6, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

                // Label end points of polyline segments
                DrawCircle(p1, 8, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
                DrawMtext(db, doc, p1, "A", 6, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
                DrawCircle(p2, 8, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
                DrawMtext(db, doc, p2, "B", 6, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

                //Determine if the intersection point is a valid point within the polyline segment.
                IntersectPointData intersectPointData = (EE_Helpers.FindPointOfIntersectLines_FromPoint3d(b1, b2, p1, p2));

                if (intersectPointData == null)
                    continue;
                else
                {
                    Point3d intPt = intersectPointData.Point;
                    doc.Editor.WriteMessage("\n--intPt " + intPt.X + "," + intPt.Y);
                    doc.Editor.WriteMessage("\n--b1 " + b1.X + "," + b1.Y);
                    doc.Editor.WriteMessage("\n--b2 " + b2.X + "," + b2.Y);
                    doc.Editor.WriteMessage("\n--p1 " + p1.X + "," + p1.Y);
                    doc.Editor.WriteMessage("\n--p2 " + p2.X + "," + p2.Y);

                    // if the intersection point is within the two line segments (meaining that the cross)
                    if (intersectPointData.isWithinSegment is true)
                    {
                        doc.Editor.WriteMessage("\nintersection point was within the line segment.");
                    }
                    else
                    {
                        doc.Editor.WriteMessage("\nintersection point not within segment");
                    }
                    doc.Editor.WriteMessage("\n--" + intersectPointData.logMessage);


                    if (intersectPointData.isParallel is true)
                    {
                        doc.Editor.WriteMessage("\nline segment " + i.ToString() + " was parallel");
                        // skip since intersection points are not possible for parallel lines
                        continue;
                    }

                    // Add text for debugging
                    DrawMtext(db, doc, intersectPointData.Point, i.ToString() + ":" + str, 10, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

                    if (intersectPointData.isWithinSegment is true)
                    {
                        intPtList.Add(intersectPointData.Point);
                        DrawCircle(intersectPointData.Point, 15, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
                        DrawMtext(db, doc, intersectPointData.Point, i.ToString() + ":" + str, 10, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);
                        continue;
                    }
                }

            }
        }

        [CommandMethod("CB")]
        public static void BlockTest()
        {
            DBObjectCollection db_obj_coll1 = SquareOfLines(20);
            DBObjectCollection db_obj_coll2 = SquareOfLines(5);

            foreach(var acDBObject in db_obj_coll2)
            {
                 db_obj_coll1.Add((DBObject)acDBObject);
            }

            CreateBlock(db_obj_coll1, "name1");
            InsertBlock(new Point3d(0, 0, 0), "name1");
            InsertBlock(new Point3d(10, 10, 0), "name1");
            InsertBlock(new Point3d(20, 20, 0), "name1");

        }

        [CommandMethod("CBTestFull")]
        public static void BlockTestFull()
        {
            Document doc =
              Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr =
              db.TransactionManager.StartTransaction();

            using (tr)
            {
                // Get the block table from the drawing
                BlockTable bt =
                  (BlockTable)tr.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead
                  );

                // Check the block name, to see whether it's
                // already in use

                PromptStringOptions pso =
                  new PromptStringOptions(
                    "\nEnter new block name: "
                  );

                pso.AllowSpaces = true;

                // A variable for the block's name
                string blkName = "";

                do
                {
                    PromptResult pr = ed.GetString(pso);

                    // Just return if the user cancelled
                    // (will abort the transaction as we drop out of the using
                    // statement's scope)

                    if (pr.Status != PromptStatus.OK)
                        return;

                    try
                    {
                        // Validate the provided symbol table name
                        SymbolUtilityServices.ValidateSymbolName(
                          pr.StringResult,
                          false
                        );

                        // Only set the block name if it isn't in use
                        if (bt.Has(pr.StringResult))
                            ed.WriteMessage(
                              "\nA block with this name already exists."
                            );
                        else
                            blkName = pr.StringResult;
                    }
                    catch
                    {
                        // An exception has been thrown, indicating the
                        // name is invalid
                        ed.WriteMessage(
                          "\nInvalid block name."
                        );
                    }
                } while (blkName == "");

                // Create our new block table record...
                BlockTableRecord btr = new BlockTableRecord();

                // ... and set its properties
                btr.Name = blkName;

                // Add the new block to the block table
                bt.UpgradeOpen();
                ObjectId btrId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                // Add some lines to the block to form a square
                // (the entities belong directly to the block)
                DBObjectCollection ents = SquareOfLines(5);
                foreach (Entity ent in ents)
                {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }

                // Add a block reference to the model space
                BlockTableRecord ms =
                  (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite
                  );

                BlockReference br =
                  new BlockReference(Point3d.Origin, btrId);

                ms.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // Commit the transaction
                tr.Commit();

                // Report what we've done
                ed.WriteMessage(
                  "\nCreated block named \"{0}\" containing {1} entities.",
                  blkName, ents.Count
                );
            }
        }

        private static DBObjectCollection SquareOfLines(double size)
        {
            // A function to generate a set of entities for our block

            DBObjectCollection ents = new DBObjectCollection();

            Point3d[] pts =
            {
                new Point3d(-size, -size, 0),
                new Point3d(size, -size, 0),
                new Point3d(size, size, 0),
                new Point3d(-size, size, 0)
              };

            int max = pts.GetUpperBound(0);

            for (int i = 0; i <= max; i++)
            {
                int j = (i == max ? 0 : i + 1);
                Line ln = new Line(pts[i], pts[j]);
                ents.Add(ln);
            }

            return ents;
        }

        [CommandMethod("DHOR")]
        public static void DimHorizontal()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            // get the first point
            var options = new PromptPointOptions("\nPick the first point: ");
            var result = ed.GetPoint(options);
            if (result.Status != PromptStatus.OK)
                return;
            var pt1 = result.Value;

            // get the second point (must have a different X value)
            options.Message = "\nPick the second point: ";
            options.BasePoint = pt1;
            options.UseBasePoint = true;
            while (true)
            {
                result = ed.GetPoint(options);
                if (result.Status != PromptStatus.OK)
                    return;
                if (result.Value.X != pt1.X)
                    break; ;
                ed.WriteMessage("\nSecond point must have different X value.");
            }
            var pt2 = result.Value;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // compute the 'dimensionLinePoint' (max Y value + the current dimstyle text height X 5
                var dimstyle = (DimStyleTableRecord)tr.GetObject(db.Dimstyle, OpenMode.ForRead);
                var pt3 = new Point3d(pt1.X, Math.Max(pt1.Y, pt2.Y) + 5 * dimstyle.Dimtxt, 0.0);

                // create a new RotatedDimension
                var cSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var dim = new RotatedDimension(0.0, pt1, pt2, pt3, "", db.Dimstyle);
                dim.TransformBy(ed.CurrentUserCoordinateSystem);
                cSpace.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
                tr.Commit();
            }
        }

        [CommandMethod("DVER")]
        public static void DimVertical()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            // get the first point
            var options = new PromptPointOptions("\nPick the first point: ");
            var result = ed.GetPoint(options);
            if (result.Status != PromptStatus.OK)
                return;
            var pt1 = result.Value;

            // get the second point (must have a different Y value)
            options.Message = "\nPick the second point: ";
            options.BasePoint = pt1;
            options.UseBasePoint = true;
            while (true)
            {
                result = ed.GetPoint(options);
                if (result.Status != PromptStatus.OK)
                    return;
                if (result.Value.Y != pt1.Y)
                    break; ;
                ed.WriteMessage("\nSecond point must have different Y value.");
            }
            var pt2 = result.Value;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // compute the 'dimensionLinePoint' (max X value + the current dimstyle text height X 5
                var dimstyle = (DimStyleTableRecord)tr.GetObject(db.Dimstyle, OpenMode.ForRead);
                var pt3 = new Point3d(Math.Max(pt1.X, pt2.X) + 5 * dimstyle.Dimtxt, pt1.Y, 0.0);

                // create a new RotatedDimension
                var cSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var dim = new RotatedDimension(0.5 * Math.PI, pt1, pt2, pt3, "", db.Dimstyle);
                dim.TransformBy(ed.CurrentUserCoordinateSystem);
                cSpace.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
                tr.Commit();
            }
        }
    }
}
