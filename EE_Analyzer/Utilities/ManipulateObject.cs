using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace EE_Analyzer.Utilities

{
    public class ManipulateObject
    {
        [CommandMethod("EESingleCopy")]
        public static void SingleCopy()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the BlockTable for read
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the block table record Modelspace for write
                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create a circle that is 2, 3 wih a radius of 4.25
                    using (Circle c1 = new Circle())
                    {
                        c1.Center = new Point3d(2, 3, 0);
                        c1.Radius = 4.25;

                        // Add the new object to the BlockTable record
                        btr.AppendEntity(c1);
                        trans.AddNewlyCreatedDBObject(c1, true);

                        // Create a copy of the circle and change its radius
                        Circle c1Clone = c1.Clone() as Circle;
                        c1Clone.Radius = 1.0;

                        // Add the cloned circle to the BlockTable record
                        btr.AppendEntity(c1Clone);
                        trans.AddNewlyCreatedDBObject(c1Clone, true);
                    }
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EEMove")]
        public static void MoveObject()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the BlockTable for read
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the block table record Modelspace for write
                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create an circle that is at 2,2 with a radius of 0.5
                    using (Circle c1 = new Circle())
                    {
                        c1.Center = new Point3d(2, 2, 0);
                        c1.Radius = 0.5;

                        // Create a matrix and move the circle using a vector from (0,0,0)
                        Point3d startPt = new Point3d(0, 0, 0);
                        Vector3d destVector = startPt.GetVectorTo(new Point3d(2, 0, 0));

                        c1.TransformBy(Matrix3d.Displacement(destVector));

                        // Add the new object to the block table record
                        btr.AppendEntity(c1);
                        trans.AddNewlyCreatedDBObject(c1, true);
                    }
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        [CommandMethod("EEEraseObject")]
        public static void EraseObject()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the BlockTable for read
                    BlockTable bt;
                    bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the block table record Modelspace for write
                    BlockTableRecord btr;
                    btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create a new lightweight polyline to erase
                    using (Polyline pl = new Polyline())
                    {
                        pl.AddVertexAt(0, new Point2d(2, 4), 0, 0, 0);
                        pl.AddVertexAt(1, new Point2d(4, 2), 0, 0, 0);
                        pl.AddVertexAt(2, new Point2d(6, 4), 0, 0, 0);

                        // Add the new object to the block table record
                        btr.AppendEntity(pl);
                        trans.AddNewlyCreatedDBObject(pl, true);

                        doc.SendStringToExecute("._zoom e ", false, false, false);

                        // update the display and an alert message
                        doc.Editor.Regen();
                        Application.ShowAlertDialog("Erase the newly added polyline.");

                        // Erase the polyline from the drawing
                        pl.Erase(true);
                    }
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error encountered: " + ex.Message);
                    trans.Abort();
                }
            }
        }
    }
}
