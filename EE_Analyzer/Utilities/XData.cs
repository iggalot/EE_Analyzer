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
    public class XData
    {
        /// <summary>
        /// Registers an application for use with XData -- the tag for XData piece.
        /// </summary>
        /// <param name="regAppName"></param>
        public static void AddRegAppTableRecord(string regAppName)
        {
            Document doc =
              Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            Transaction tr =
              doc.TransactionManager.StartTransaction();
            using (tr)
            {
                RegAppTable rat =
                  (RegAppTable)tr.GetObject(
                    db.RegAppTableId,
                    OpenMode.ForRead,
                    false
                  );

                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr =
                      new RegAppTableRecord();

                    ratr.Name = regAppName;
                    rat.Add(ratr);

                    tr.AddNewlyCreatedDBObject(ratr, true);
                }

                tr.Commit();
            }
        }

        static public void SetXData(string prefix, string id_string, ObjectId oid)
        {
            Document doc =
              Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                DBObject obj =
                    tr.GetObject(
                    oid,
                    OpenMode.ForWrite
                    );

                AddRegAppTableRecord(prefix);
                ResultBuffer rb =
                    new ResultBuffer(
                        new TypedValue(1001, prefix),
                        new TypedValue(1000, id_string)
                    );

                obj.XData = rb;
                rb.Dispose();
                tr.Commit();
            }
        }

        static public void GetXData(ObjectId oid)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            using (Transaction tr =
                doc.TransactionManager.StartTransaction())
            {
                DBObject obj =
                    tr.GetObject(
                    oid,
                    OpenMode.ForRead
                    );

                ResultBuffer rb = obj.XData;
                if (rb == null)
                {
                    ed.WriteMessage(
                        "\nEntity does not have XData attached."
                    );
                }
                else
                {
                    int n = 0;
                    foreach (TypedValue tv in rb)
                    {
                        ed.WriteMessage(
                            "\nTypedValue {0} - type: {1}, value: {2}",
                            n++,
                            tv.TypeCode,
                            tv.Value
                        );
                    }
                    rb.Dispose();
                }
            }
        }
    }
}
