using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.HatchObjects;

namespace EE_RoofFramer.Models
{
    public class ConnectionToFoundation : BaseConnectionModel
    {
        // The reaction to the foundation
        public double Reaction { get; set; }

        public ConnectionToFoundation(int id, Point3d pt, int supporting, string layer_name = EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER) 
            : base(id, pt, supporting, -1, ConnectionTypes.CONN_TYPE_MBR_TO_FDN)
        {

        }

        public ConnectionToFoundation(string line, string layer_name) : base(line, layer_name)
        {
            BelowConn = -1;
        }


        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, BaseConnectionModel> conn_dict, IDictionary<int, BaseLoadModel> load_dict)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                try
                {
                    Polyline pline = new Polyline();
                    pline.AddVertexAt(0, new Point2d(ConnectionPoint.X - EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0, ConnectionPoint.Y - EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0), 0, 0, 0);
                    pline.AddVertexAt(1, new Point2d(ConnectionPoint.X - EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0, ConnectionPoint.Y + EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0), 0, 0, 0);
                    pline.AddVertexAt(2, new Point2d(ConnectionPoint.X + EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0, ConnectionPoint.Y + EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0), 0, 0, 0);
                    pline.AddVertexAt(3, new Point2d(ConnectionPoint.X + EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0, ConnectionPoint.Y - EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0 / 2.0), 0, 0, 0);

                    pline.SetDatabaseDefaults();
                    pline.Closed = true;
                    pline.Layer = EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER;
                    pline.Linetype = "HIDDEN2";
                    //pline.SetDatabaseDefaults();
                    ObjectId plineId = btr.AppendEntity(pline);
                    trans.AddNewlyCreatedDBObject(pline, true);

                    // Add the associative hatch
                    AddRectangularHatch(ConnectionPoint, plineId, EE_FDN_Settings.DEFAULT_PIER_HATCH_TYPE, EE_FDN_Settings.DEFAULT_HATCH_PATTERNSCALE);

                    trans.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\nError drawing foundation connection [" + Id.ToString() + "]");
                    trans.Abort();
                }
            }
        }
    }
}
