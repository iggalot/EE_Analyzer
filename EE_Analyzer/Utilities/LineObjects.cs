using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_Analyzer.Utilities
{
    public static class LineObjects
    {
        /// <summary>
        /// Offsets an Autocad Polyline by a specified distance
        /// </summary>
        /// <param name="pline">polyline object to offset</param>
        /// <param name="offset_dist">"+" makes the object bigger and "-" makes it smaller</param>
        public static Line OffsetLine(Line line, double offset_dist, BlockTable bt, BlockTableRecord btr)
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            //doc.Editor.WriteMessage("offsetting foundation line by: " + offset_dist);

            Line newPline = new Line();
            DBObjectCollection objCollection;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    objCollection = line.GetOffsetCurves(offset_dist);

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
                    newPline = objCollection[0] as Line;

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

            return newPline;
        }

        public static void MoveLineToLayer(Line obj, string layer_name)
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
                    doc.Editor.WriteMessage("Error moving polyline [" + obj.Handle.ToString() + "] to [" + layer_name + "]: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static void LineSetLinetype(Line obj, string linetype_name)
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

    }
}
