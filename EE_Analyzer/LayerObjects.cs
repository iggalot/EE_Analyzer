using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

namespace EE_Analyzer
{
    public class LayerObjects
    {
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
                if(lyTab.Has(sNewLayerName) == false)
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
