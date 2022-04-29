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

                } catch (System.Exception ex)
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


    }
}
