using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using static EE_Analyzer.Utilities.EE_Helpers;

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

        //            // draw the circles
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
    }
}
