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
    public static class SelectionObjects
    {

        public static ObjectId DoSelectLine(Database db, Document doc)
        {
            PromptEntityOptions options = new PromptEntityOptions("\nSelect Member");
            options.SetRejectMessage("\nSelected object is not a line object.");
            options.AddAllowedClass(typeof(Line), true);

            // Select the polyline for the foundation
            PromptEntityResult result = doc.Editor.GetEntity(options);

            if (result.Status == PromptStatus.OK)
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    // Get the selected object from the autocad database
                    DBObject obj = tr.GetObject(result.ObjectId, OpenMode.ForRead);
                    ResultBuffer rb = obj.XData;
                    if (rb == null)
                    {
                        doc.Editor.WriteMessage("\nSelected Entity " + result.ObjectId + " does not have XData attached.");
                        return ObjectId.Null;
                    }
                    else
                    {
                        return obj.Id;
                    }
                }
            }

            return ObjectId.Null;
        }

        public static Point3d DoSelectPointOnLine(Database db, Document doc, ObjectId line_obj_id)
        {
            PromptEntityOptions options = new PromptEntityOptions("\nSelect Point on Member");

            // Select the polyline for the foundation
            PromptEntityResult result = doc.Editor.GetEntity(options);

            if (result.Status == PromptStatus.OK)
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    // Get the selected object from the autocad database
                    Line obj = tr.GetObject(line_obj_id, OpenMode.ForRead) as Line;
                    ResultBuffer rb = obj.XData;

                    if (rb == null)
                    {
                        return new Point3d(Double.MaxValue, Double.MaxValue, Double.MaxValue);  // set it to a max value
                    }
                    else
                    {
                        // Get the picked point on the screen.
                        Point3d pick_pt = result.PickedPoint;

                        return MathHelpers.NearestPointOnLine(pick_pt, obj.StartPoint, obj.EndPoint);
                    }
                }
            }

            return new Point3d(Double.MaxValue, Double.MaxValue, Double.MaxValue);  // set it to a max value

        }


    }
}
