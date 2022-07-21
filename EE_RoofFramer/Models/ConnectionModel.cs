using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.XData;



namespace EE_RoofFramer.Models
{
    public class ConnectionModel : acStructuralObject
    {
        public Point3d ConnectionPoint { get; set; }

        // id number of the object supported below this connection (the supporting item)
        public int BelowConn { get; set; }

        // id number of the object supported above this connection (item being supporteD)
        public int AboveConn { get; set; }

        public BaseLoadModel Reactions { get; set; } = null;

        public ConnectionModel() : base()
        {

        }

        public ConnectionModel(int id, Point3d pt, int supporting, int supported_by, string layer_name = EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER) : base(id)
        {
            ConnectionPoint = pt;
            BelowConn = supported_by;
            AboveConn = supporting;
        }

        public ConnectionModel(string line, string layer_name) : base()
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            try
            {
                if (split_line.Length >= 6)
                {
                    if (split_line[index].Substring(0, 2).Equals("SC"))
                    {
                        // read the previous information that was stored in the file
                        Id = Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2));

                        BelowConn = Int32.Parse(split_line[index + 1]);  // member id of supporting member
                        AboveConn = Int32.Parse(split_line[index + 2]);  // member id of member above being supported
                        double x = Double.Parse(split_line[index + 3]);
                        double y = Double.Parse(split_line[index + 4]);
                        double z = Double.Parse(split_line[index + 5]);

                        ConnectionPoint = new Point3d(x, y, z);  // Point in space of the connection
                    }
                    else
                    {
                        return;
                    }
                }
            } catch (System.Exception ex)
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                doc.Editor.WriteMessage("Error parsing connection #[" + Id.ToString() +"]: " + ex.Message);
            }
 

            return;
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, ConnectionModel> conn_dict, IDictionary<int, BaseLoadModel> load_dict)
        {
            ConnectionDictionary = conn_dict;
            LoadDictionary = load_dict;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Draw a box shape for a support connection
                    Point3d pt1 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-5, -5, 0));
                    Point3d pt2 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-5, 5, 0));
                    Point3d pt3 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(5, 5, 0));
                    Point3d pt4 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(5, -5, 0));

                    Line ln1 = OffsetLine(new Line(pt1, pt2), 0) as Line;
                    MoveLineToLayer(ln1, layer_name);

                    Line ln2 = OffsetLine(new Line(pt2, pt3), 0) as Line;
                    MoveLineToLayer(ln2, layer_name);

                    Line ln3 = OffsetLine(new Line(pt3, pt4), 0) as Line;
                    MoveLineToLayer(ln3, layer_name);

                    Line ln4 = OffsetLine(new Line(pt4, pt1), 0) as Line;
                    MoveLineToLayer(ln4, layer_name);

                    string support_str = "ID: " + Id.ToString() + "\n" + "A: " + AboveConn.ToString() + "\n" + "B: " + BelowConn.ToString();
                    DrawMtext(db, doc, ConnectionPoint, support_str, 1, layer_name);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding connection [" + CurrentHandle.ToString() + "] information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }
            }
        }


        /// <summary>
        /// Creates the string for writing to the file
        /// </summary>
        /// <param name="prefix_identifier"></param>
        /// <returns></returns>
        public override string ToFile()
        {
            string data = "";
            data += "SC" + Id.ToString() + "," + BelowConn.ToString() + "," + AboveConn.ToString() + ",";
            data += ConnectionPoint.X.ToString() + "," + ConnectionPoint.Y.ToString() + "," + ConnectionPoint.Z.ToString() + ",";
            data += "$";
            return data;
        }


        public override string ToString()
        {
            return "SC" + Id.ToString() + "\nB:" + BelowConn.ToString() + "\nA: " + AboveConn.ToString();

        }

        protected override void UpdateCalculations() { }
        public override bool ValidateSupports() { return false; }
        public override void AddConnection(ConnectionModel conn, IDictionary<int, ConnectionModel> dict) { }
        public override void AddUniformLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict) { }
        public override void AddConcentratedLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict) { }
        public override void HighlightStatus() { }

        protected override void UpdateSupportedBy() { }

        public override void CalculateReactions(RoofFramingLayout layout)
        {
            throw new NotImplementedException();
        }
    }
}
