using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.HatchObjects;

namespace EE_RoofFramer.Models
{
    public class ConnectionToExternalLoad : BaseConnectionModel
    {
        int LoadModelId { get; set; }

        public ConnectionToExternalLoad(int id, int load_id, Point3d pt, int supported_by, int supporting_above, string layer_name = EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER) 
            : base(id, pt, supported_by, supporting_above, ConnectionTypes.CONN_TYPE_MBR_TO_LOAD, layer_name = EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER)
        {
            LoadModelId = load_id;
        }

        public ConnectionToExternalLoad(string line, string layer_name) : base(line, layer_name)
        {
            AboveConn = -1;
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                { 
                    DrawCircle(this.ConnectionPoint, EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH, EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER);  // outer pier diameter
                    AddCircularHatch(this.ConnectionPoint, EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH * 0.9, EE_ROOF_Settings.DEFAULT_CONNECTION_HATCH_TYPE, EE_ROOF_Settings.DEFAULT_CONNECTION_HATCH_PATTERNSCALE);

                    trans.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\nError drawing concentrated load connection [" + Id.ToString() + "]: " + e.Message);
                    trans.Abort();
                }
            }
        }
    }
}
