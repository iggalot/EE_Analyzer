using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace EE_Analyzer.Utilities
{
    public static class LinetypeObjects
    {
        // Loads on autocad linetype into the drawing.
        public static void LoadLineTypes(string name, Document doc, Database db)
        {
            // change the linetype
            string ltypeName = name;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltTab = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                if (ltTab.Has(ltypeName))
                {
                    doc.Editor.WriteMessage("\nLinetype [" + ltypeName + "] is already loaded.");
                    trans.Abort();
                }
                else
                {
                    // Load the linetype
                    db.LoadLineTypeFile(ltypeName, "acad.lin");
                    doc.Editor.WriteMessage("\nLinetype [" + ltypeName + "] was created successfully.");
                    trans.Commit();
                }
            }
        }
    }
}
