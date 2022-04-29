using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace EE_AutoCADUtilities
{
    public class DrawObject
    {
        [CommandMethod("EEPLine")]
        public void DrawPLine()
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
                    edt.WriteMessage("Drawing a polyline object!");
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
        public void DrawArc()
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
        public void DrawCircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Specify the MText parameters (e.g. Textstring, insertionPoint)
                    edt.WriteMessage("Drawing a Circle object!");
                    Point3d centerPt = new Point3d(100, 100, 0);
                    double circleRadius = 100.0;
                    using (Circle circle = new Circle())
                    {
                        circle.Radius = circleRadius;
                        circle.Center = centerPt;

                        btr.AppendEntity(circle);
                        trans.AddNewlyCreatedDBObject(circle, true);
                    }
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
        public void DrawMText()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                    // Specify the MText parameters (e.g. Textstring, insertionPoint)
                    edt.WriteMessage("Drawing MText object.");

                    string txt = "Hello AutoCAD from C#!";
                    Point3d insPt = new Point3d(200, 200, 0);
                    using (MText mtx = new MText())
                    {
                        mtx.Contents = txt;
                        mtx.Location = insPt;

                        btr.AppendEntity(mtx);
                        trans.AddNewlyCreatedDBObject(mtx, true);
                    }
                    trans.Commit();

                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EELine")]
        public void DrawLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr=trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Send a message to the user
                    edt.WriteMessage("\nDrawing a Line object: ");

                    Point3d pt1 = new Point3d(0, 0, 0);
                    Point3d pt2 = new Point3d(100, 100, 0);
                    Line ln = new Line(pt1, pt2);
                    ln.ColorIndex = 1;  // Color is red
                    btr.AppendEntity(ln);
                    trans.AddNewlyCreatedDBObject(ln, true);
                    trans.Commit();

                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }
    }
}
