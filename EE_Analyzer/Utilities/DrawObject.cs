using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace EE_Analyzer.Utilities
{
    public static class DrawObject
    {
        [CommandMethod("EEPLine")]
        public static void DrawPLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Get the BlockTable object
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Specify the polyline parameters 
                    edt.WriteMessage("\nDrawing a polyline object!");
                    Polyline pl = new Polyline();
                    pl.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                    pl.AddVertexAt(1, new Point2d(10, 10), 0, 0, 0);
                    pl.AddVertexAt(1, new Point2d(20, 20), 0, 0, 0);
                    pl.AddVertexAt(1, new Point2d(30, 30), 0, 0, 0);
                    pl.AddVertexAt(1, new Point2d(40, 40), 0, 0, 0);
                    pl.AddVertexAt(1, new Point2d(50, 50), 0, 0, 0);


                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EEArc")]
        public static void DrawArc()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Get the BlockTable object
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Specify the arc parameters 
                    edt.WriteMessage("Drawing an arc object!");
                    Point3d centerPt = new Point3d(10, 10, 0);
                    double arcRad = 20.0;
                    double startAngle = 1.0; // radians
                    double endAngle = 3.0;
                    Arc arc = new Arc(centerPt, arcRad, startAngle, endAngle);

                    // Set the default properties
                    arc.SetDatabaseDefaults();
                    btr.AppendEntity(arc);
                    trans.AddNewlyCreatedDBObject(arc, true);
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EECircle")]
        public static void DrawEECircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DrawCircle(new Point3d(100, 100, 0), 100, "0");
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EEMtext")]
        public static void DrawMTextTest()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            doc.Editor.WriteMessage("Drawing MText object.");

            Point3d insPt = new Point3d(200, 200, 0);
            string txt = "Hello AutoCAD from C#!";

            DrawMtext(db, doc, insPt, txt, 10.0, "0", 0.0);
        }

        /// <summary>
        /// Utility function to draw mtext
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="pt"></param>
        /// <param name="txt"></param>
        /// <param name="size"></param>
        /// <param name="layer"></param>
        /// <param name="rot"></param>
        public static void DrawMtext(Database db, Document doc, Point3d pt, string txt, double size, string layer, double rot = 0.0)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    using (MText mtx = new MText())
                    {
                        mtx.Location = pt;
                        mtx.Contents = txt;
                        mtx.Rotation = rot;
                        mtx.TextHeight = size;
                        mtx.Layer = layer;

                        btr.AppendEntity(mtx);
                        trans.AddNewlyCreatedDBObject(mtx, true);
                    }
                    trans.Commit();

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error encountered creating MText: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static Line DrawLine(Point3d pt1, Point3d pt2, string layer_name)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            if (pt1 == null || pt2 == null)
                throw new System.Exception("\nInvalid point data received at DrawLine");

            Line ln = null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr=trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Send a message to the user
                    // edt.WriteMessage("\nDrawing a Line object: ");

                    // pt1 = new Point3d(0, 0, 0);
                    //Point3d pt2 = new Point3d(100, 100, 0);
                    ln = new Line();
                    ln.StartPoint = pt1;
                    ln.EndPoint = pt2;
                    ln.Layer = layer_name;

                    btr.AppendEntity(ln);
                    trans.AddNewlyCreatedDBObject(ln, true);
                    trans.Commit();

                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered drawing line from " + pt1.X + "," + pt1.Y + " to " + pt2.X + "," + pt2.Y +" -- " + ex.Message);
                    trans.Abort();
                }
            }
            return ln;
        }

        public static void DrawCircle(Point3d centerPt, double circleRadius, string layer_name)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            if (centerPt == null || circleRadius <= 0)
                throw new System.Exception("\nInvalid data received at DrawCircle");

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Specify the MText parameters (e.g. Textstring, insertionPoint)
                    //edt.WriteMessage("\nDrawing a Circle object at: " + centerPt.X + ", " + centerPt.Y + ", " + centerPt.Z);
                    using (Circle circle = new Circle())
                    {
                        circle.Radius = circleRadius;
                        circle.Center = centerPt;
                        circle.Layer = layer_name;

                        btr.AppendEntity(circle);
                        trans.AddNewlyCreatedDBObject(circle, true);
                    }
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered drawing circle at: " + centerPt.X + ", " + centerPt.Y + ", " + centerPt.Z + ex.Message);
                    trans.Abort();
                }
            }
        }
    }
}
