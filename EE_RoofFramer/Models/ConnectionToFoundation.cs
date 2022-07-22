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

        public ConnectionToFoundation(int id, Point3d pt, int supporting) 
            : base(id, pt, supporting, -1, ConnectionTypes.CONN_TYPE_MBR_TO_FDN)
        {

        }

        public ConnectionToFoundation(string line) : base(line)
        {
            BelowConn = -1;
        }


        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            layer_name = EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER;

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
                    pline.Layer = layer_name;
                    pline.Linetype = "HIDDEN2";
                    //pline.SetDatabaseDefaults();
                    ObjectId plineId = btr.AppendEntity(pline);
                    trans.AddNewlyCreatedDBObject(pline, true);

                    // Add the associative hatch
                    AddRectangularHatch(ConnectionPoint, plineId, EE_FDN_Settings.DEFAULT_PIER_HATCH_TYPE, EE_FDN_Settings.DEFAULT_HATCH_PATTERNSCALE);

                    // add a label
                    string support_str = "ID: " + Id.ToString() + "\n" + "A: " + AboveConn.ToString() + "\n" + "B: " + BelowConn.ToString();
                    DrawMtext(db, doc, ConnectionPoint, support_str, 1, layer_name);

                    trans.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\nError drawing foundation connection [" + Id.ToString() + "]: " + e.Message);
                    trans.Abort();
                }
            }
        }
    }
}
