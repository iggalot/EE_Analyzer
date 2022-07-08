using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LineObjects;


namespace EE_RoofFramer.Models
{
    public class RafterModel
    {
        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }
        
        Line Centerline { get; set; } 
        public Vector3d vDir { get; set; }


        public RafterModel()
        {

        }

        public RafterModel(Point3d start, Point3d end)
        {
            StartPt = start;
            EndPt = end;

            vDir = start.GetVectorTo(end);
        }

        public void AddToAutoCADDatabase(Database db, Document doc)
        {

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                   // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                   // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Centerline = OffsetLine(new Line(StartPt, EndPt), 0) as Line;
                    MoveLineToLayer(Centerline, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }    
            }
        }
    }
}
