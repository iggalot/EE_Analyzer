using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
        public static Line OffsetLine(Line line, double offset_dist)
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
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

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
                    doc.Editor.WriteMessage("Error offseting line: " + ex.Message);
                    trans.Abort();
                    throw new System.Exception("Error offseting line object" + ex.Message);
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
                        ed.WriteMessage("\nLayer [" + layer_name + " not found in MoveLineToLayer");
                        throw new System.Exception("Layer [" + layer_name + "] not currently loaded");

                    }

                    // Get the layer's id and use it
                    ObjectId lid = lt[layer_name];

                    //obj.LayerId = lid;
                    Entity ent = trans.GetObject(obj.Id, OpenMode.ForWrite) as Entity;
                    ent.LayerId = lid;

                    trans.Commit();

                    //ed.WriteMessage("\n-- Line [" + obj.Handle.ToString() + "] successfully moved to layer [" + layer_name + "]");
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError moving line [" + obj.Handle.ToString() + "] to [" + layer_name + "]: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static void ChangeLineColor(Line obj, string color)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int colorIndex = ColorIndexFromString(color);

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    //obj.LayerId = lid;
                    Entity ent = trans.GetObject(obj.Id, OpenMode.ForWrite) as Entity;
                    ent.ColorIndex = colorIndex;

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError changing line [" + obj.Handle.ToString() + "] to color [" + color + "]: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static int ColorIndexFromString(string color)
        {
            int result = -1;
            if(Int32.TryParse(color, out result))
            {
                result = ((result >= 0) && (result <= 256) ? result : 256);
            } else
            {
                try
                {
                    MyColors colorIndex = (MyColors)Enum.Parse(typeof(MyColors), color, true);
                    if(Enum.IsDefined(typeof(MyColors), colorIndex))
                    {
                        result = (int)colorIndex;
                    }
                } catch (ArgumentException)
                {

                    return 256;
                }
            }
            return result;
        }

        private enum MyColors
        {
            ByBlock = 0,
            Red = 1,
            Yellow = 2,
            Green = 3,
            Cyan = 4,
            Blue = 5,
            Magenta = 6,
            White = 7,
            Grey = 8,
            ByLayer = 256
        };

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
                    LinetypeTable lt = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                    if (!lt.Has(linetype_name))
                    {
                        ed.WriteMessage("\nLinetype [" + linetype_name + " not found in LineSetLinetype");
                        throw new System.Exception("Linetype [" + linetype_name + "] not currently loaded");
                    }

                    ObjectId ltid = lt[linetype_name];
                    //obj.LinetypeId = lt[linetype_name];

                    //obj.LayerId = lid;
                    Entity ent = trans.GetObject(obj.Id, OpenMode.ForWrite) as Entity;
                    ent.LinetypeId = ltid;

                    trans.Commit();

                    //ed.WriteMessage("\n-- Line [" + obj.Handle.ToString() + "] successfully changed to linetype [" + linetype_name + "]");

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError changing linetypes for line [" + obj.Handle.ToString() + "] to [" + linetype_name + "]" + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static Line TrimLinetoPolyline(Database db, Document doc, Polyline poly, Line ln)
        {
            Line line = new Line();

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError trimming line [" + ln.Handle.ToString() + "] to to polyline [" + poly.Handle.ToString() + "]" + ex.Message);
                    trans.Abort();
                }
            }

            return line;
        }
        
        public static Point3d[] PromptUserforLineEndPoints(Database db, Document doc)
        {
            Point3d[] points = new Point3d[2]; ;

            PromptPointResult pPtRes;
            PromptPointOptions pPtOpts = new PromptPointOptions("");

            // Prompt for the start point
            pPtOpts.Message = "\nEnter the start point of the line: ";
            pPtRes = doc.Editor.GetPoint(pPtOpts);
            points[0] = pPtRes.Value;

            // Exit if the user presses ESC or cancels the command
            if (pPtRes.Status == PromptStatus.Cancel) 
                return points;

            // Prompt for the end point
            pPtOpts.Message = "\nEnter the end point of the line: ";
            pPtOpts.UseBasePoint = true;
            pPtOpts.BasePoint = points[0];
            pPtRes = doc.Editor.GetPoint(pPtOpts);
            
            points[1] = pPtRes.Value;

            if (pPtRes.Status == PromptStatus.Cancel) 
                return null;

            return points;
        }

    }
}
