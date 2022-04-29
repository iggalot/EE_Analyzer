using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace EE_Analyzer.Utilities
{
    public static class PolylineObjects
    {
        // Return a list of vertices for a selected polyline
        public static List<Point2d> GetVertices(Polyline pl)
        {
            List<Point2d> vertices = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d pt = pl.GetPoint2dAt(i);
                vertices.Add(pt);
            }

            return vertices;
        }

        /// <summary>
        /// Offsets an Autocad Polyline by a specified distance
        /// </summary>
        /// <param name="pline">polyline object to offset</param>
        /// <param name="offset_dist">"+" makes the object bigger and "-" makes it smaller</param>
        public static Polyline OffsetPolyline(Polyline pline, double offset_dist, BlockTable bt, BlockTableRecord btr)
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            doc.Editor.WriteMessage("offsetting foundation line by: " + offset_dist);

            Polyline newPline = new Polyline();
            DBObjectCollection objCollection;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    objCollection = pline.GetOffsetCurves(offset_dist);

                    // Step through the new objects created
                    foreach (Entity ent in objCollection)
                    {
                        // Add each offset object
                        btr.AppendEntity(ent);
                        trans.AddNewlyCreatedDBObject(ent, true);

                        //// This time access the properties directly

                        //doc.Editor.WriteMessage("\nType:        " +

                        //  ent.GetType().ToString());

                        //doc.Editor.WriteMessage("\n  Handle:    " +

                        //  ent.Handle.ToString());

                        //doc.Editor.WriteMessage("\n  Layer:      " +

                        //  ent.Layer.ToString());

                        //doc.Editor.WriteMessage("\n  Linetype:  " +

                        //  ent.Linetype.ToString());

                        //doc.Editor.WriteMessage("\n  Lineweight: " +

                        //  ent.LineWeight.ToString());

                        //doc.Editor.WriteMessage("\n  ColorIndex: " +

                        //  ent.ColorIndex.ToString());

                        //doc.Editor.WriteMessage("\n  Color:      " +

                        //  ent.Color.ToString());


                    }

                    // capture the offset polyline and return
                    newPline = objCollection[0] as Polyline;

                    // Save the new objects to the database
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error offseting polyline: " + ex.Message);
                    trans.Abort();
                    throw new System.Exception("Error offseting polyline object");
                }
            }

            return newPline ;
        }
        public static void MovePolylineToLayer(Polyline obj, string layer_name)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(layer_name))
                    {
                        ed.WriteMessage("\nLayer [" + layer_name + " not found in MovePolylineToLayer");
                    }

                    // Get the layer's id and use it
                    ObjectId lid = lt[layer_name];

                    obj.LayerId = lid;

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error moving polyline [" + obj.Handle.ToString() + "] to [" + layer_name + "]");
                    trans.Abort();
                }
            }
        }

        public static void PolylineSetLinetype(Polyline obj, string linetype_name)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


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

                    LinetypeTable lt = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                    if (!lt.Has(linetype_name))
                    {
                        ed.WriteMessage("\nLinetype [" + linetype_name + " not found in PolylineSetLinetype");
                    }

                    obj.LinetypeId = lt[linetype_name];

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError changing linetypes for polyline [" + obj.Handle.ToString() + "] to [" + linetype_name + "]");
                    trans.Abort();
                }
            }
        }

        struct PerVertexData
        {
            public Point2d pt;
            public double bulge;
            public double startWidth;
            public double endWidth;
        }

        /// <summary>
        /// Determine if a polyline is wound clockwise
        /// </summary>
        /// <param name="pline"></param>
        /// <returns></returns>
        public static bool PolylineIsWoundClockwise(Polyline pline)
        {
            double sum = 0.0;
            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                Point2d pt1 = pline.GetPoint2dAt(i);
                Point2d pt2 = pline.GetPoint2dAt((i + 1) % pline.NumberOfVertices);
                sum += (pt2.X - pt1.X) * (pt2.Y + pt1.Y);
            }
            return sum > 0.0;
        }


        /// <summary>
        /// Utility function to reverse the direction of a polyline.
        /// </summary>
        /// <param name="pline"></param>
        public static void ReversePolylineDirection(Polyline pline)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                var obj = tr.GetObject(pline.ObjectId, OpenMode.ForRead);
                var pl = obj as Polyline;

                if (pl != null)
                {
                    // Collect our per-vertex data
                    List<PerVertexData> vertData =
                      new List<PerVertexData>(pl.NumberOfVertices);

                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        PerVertexData pvd = new PerVertexData();
                        pvd.bulge = (i > 0 ? pl.GetBulgeAt(i - 1) : 0);
                        pvd.startWidth = (i > 0 ? pl.GetStartWidthAt(i - 1) : 0);
                        pvd.endWidth = (i > 0 ? pl.GetEndWidthAt(i - 1) : 0);
                        pvd.pt = pl.GetPoint2dAt(i);

                        vertData.Add(pvd);
                    }

                    // Now let's make sure we can edit the polyline
                    pl.UpgradeOpen();

                    // Write the data back to the polyline, but in
                    // reverse order

                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        PerVertexData pvd =
                        vertData[pl.NumberOfVertices - (i + 1)];
                        
                        pl.SetPointAt(i, pvd.pt);
                        pl.SetBulgeAt(i, -pvd.bulge);
                        pl.SetStartWidthAt(i, pvd.endWidth);
                        pl.SetEndWidthAt(i, pvd.startWidth);
                    }
                }
                tr.Commit();
            }
        }
    }
}
