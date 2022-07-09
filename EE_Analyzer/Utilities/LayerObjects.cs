using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Linq;

namespace EE_Analyzer.Utilities

{
    public static class LayerObjects
    {
        // Creates the necessary layers
        public static void CreateLayer(string name, Document doc, Database db, short color_index)
        {
            string layerName = name;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                try
                {
                    if (layTable.Has(name))
                    {
                        doc.Editor.WriteMessage("\nLayer [" + name + "] is already created.");
                        trans.Abort();
                    }
                    else
                    {
                        LayerTableRecord ltr = new LayerTableRecord();
                        // Create the layer
                        ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, color_index);
                        ltr.Name = name;

                        // Upgrade the layer table for write
                        layTable.UpgradeOpen();
                        // Append the new layer to the layer table and the transaction
                        layTable.Add(ltr);
                        trans.AddNewlyCreatedDBObject(ltr, true);

                        doc.Editor.WriteMessage("\nLayer [" + name + "] successfully created.");

                        trans.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error creating layer [" + name + "]: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        /// <summary>
        /// Gets the layer list.
        /// </summary>
        /// <param name="db">Database instance this method applies to.</param>
        /// <returns>Layer names list.</returns>
        public static List<string> GetAllLayerNamesList()
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

        // Creates the necessary layers
        public static void HideLayer(string name, Document doc, Database db)
        {
            string layerName = name;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                try
                {
                    if (layTable.Has(name))
                    {
                        // Upgrade the layer table for write
                        layTable.UpgradeOpen();

                        LayerTableRecord ltr = trans.GetObject(layTable[name], OpenMode.ForWrite) as LayerTableRecord;
                        ltr.IsOff = true;

                        doc.Editor.WriteMessage("\nLayer [" + name + "] is hidden.");

                        trans.Commit();
                    }
                    else
                    {
                        doc.Editor.WriteMessage("\nLayer [" + name + "] does not exist.");

                        trans.Abort();
                    }

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error hiding layer [" + name + "]: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        public static void DeleteAllObjectsOnLayer(string name, Document doc, Database db)
        {
            string layerName = name;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                try
                {
                    if (layTable.Has(name))
                    {
                        TypedValue[] tvs = new TypedValue[1]
                        {
                            new TypedValue((int)DxfCode.LayerName, layerName)
                        };

                        SelectionFilter sf = new SelectionFilter(tvs);

                        PromptSelectionResult psr = doc.Editor.SelectAll(sf);

                        ObjectIdCollection objColl;
                        if (psr.Status == PromptStatus.OK)
                        {
                            objColl = new ObjectIdCollection(psr.Value.GetObjectIds());

                            int entcount = 0;
                            foreach (ObjectId id in objColl)
                            {
                                Entity ent = trans.GetObject(id, OpenMode.ForWrite) as Entity;
                                ent.Erase();
                                entcount++;
                            }
                            //doc.Editor.WriteMessage("\n" + entcount.ToString() + " entities erased");
                        }

                        trans.Commit();
                    }
                    else
                    {
                        doc.Editor.WriteMessage("\nLayer [" + name + "] does not exist.");

                        trans.Abort();
                    }

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error hiding layer [" + name + "]: " + ex.Message);
                    trans.Abort();
                }
            }

            ModifyAutoCADGraphics.ForceRedraw(db, doc);
        }

        public static void MakeLayerCurrent(string name, Document doc, Database db)
        {
            // Start a transaction
            using (Transaction acTrans = doc.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(db.LayerTableId,
                                                   OpenMode.ForRead) as LayerTable;
                if (acLyrTbl.Has(name) == true)
                {
                    // Set the layer Center current
                    db.Clayer = acLyrTbl[name];
                    // Save the changes
                    acTrans.Commit();
                }
            }
        }

        public static string GetCurrentLayerName()
        {
            return Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("clayer").ToString();
        }
    }
}
