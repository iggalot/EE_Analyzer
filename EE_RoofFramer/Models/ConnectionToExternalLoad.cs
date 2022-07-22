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

        public ConnectionToExternalLoad(int id, int load_id, Point3d pt, int supported_by, int supporting_above) 
            : base(id, pt, supported_by, supporting_above, ConnectionTypes.CONN_TYPE_MBR_TO_LOAD)
        {
            LoadModelId = load_id;
        }

        public ConnectionToExternalLoad(string line) : base(line)
        {
            AboveConn = -1;
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            double icon_size = EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                { 
                    DrawCircle(this.ConnectionPoint, icon_size, layer_name);  // outer pier diameter
                    AddCircularHatch(this.ConnectionPoint, icon_size * 0.9, EE_ROOF_Settings.DEFAULT_CONNECTION_HATCH_TYPE, EE_ROOF_Settings.DEFAULT_CONNECTION_HATCH_PATTERNSCALE);

                    string support_str = "ID: " + Id.ToString() + "\n" + "A: " + AboveConn.ToString() + "\n" + "B: " + BelowConn.ToString();
                    DrawMtext(db, doc, ConnectionPoint, support_str, 1, layer_name);

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
