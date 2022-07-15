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


namespace EE_RoofFramer.Models
{
    public class ConnectionModel
    {
        private Handle _objHandle; // persistant object identifier 
        private ObjectId _objID;  // non persistant object identifier

        public Handle Id { get => _objHandle; set { _objHandle = value; } }

        public Point3d ConnectionPoint { get; set; }

        public Line Centerline { get; set; }

        // id number of the object supported below this connection (the supporting item)
        public Handle BelowConn { get; set; }

        // id number of the object supported above this connection (item being supporteD)
        public Handle AboveConn { get; set; }

        public LoadModel Reactions { get; set; } = null;

        public ConnectionModel(Point3d pt, Handle supporting, Handle supported_by, string layer_name = EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER)
        {
            ConnectionPoint = pt;
            BelowConn = supported_by;
            AboveConn = supporting;


            // Object for the handle -- in this case an lineobject to make the ID
            Line ln = new Line(MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-3,3,0)),
                MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(3, -3, 0)));

            Centerline = OffsetLine(ln, 0) as Line;
            MoveLineToLayer(Centerline, layer_name);

            // Store the ID's and handles
            _objID = Centerline.Id;
            _objHandle = _objID.Handle;
        }

        public ConnectionModel(string line, string layer_name)
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            if (split_line.Length >= 6)
            {
                if (split_line[index].Substring(0, 2).Equals("SC"))
                {
                    string str = split_line[index].Substring(2, split_line[index].Length - 2);
                    _objHandle = new Handle(Int64.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier));

                    string below_str = split_line[index + 1].Substring(2, split_line[index + 1].Length - 2);
                    string above_str = split_line[index + 2].Substring(2, split_line[index + 2].Length - 2);
                    BelowConn = new Handle(Int64.Parse(below_str, System.Globalization.NumberStyles.AllowHexSpecifier));  // member id of supporting member
                    AboveConn = new Handle(Int64.Parse(above_str, System.Globalization.NumberStyles.AllowHexSpecifier));// member id of member above being supported
                    double x = Double.Parse(split_line[index + 3]);
                    double y = Double.Parse(split_line[index + 4]);
                    double z = Double.Parse(split_line[index + 5]);

                    ConnectionPoint = new Point3d(x, y, z);  // Point in space of the connection

                    // Object for the handle -- in this case an lineobject to make the ID
                    Line ln = new Line(MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-3, 3, 0)),
                        MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(3, -3, 0)));

                    Centerline = OffsetLine(ln, 0) as Line;
                    MoveLineToLayer(Centerline, layer_name);
                }
                else
                {
                    return;
                }
            }
 

            return;
        }

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            // Object for the handle -- in this case an lineobject to make the ID
            Line ln = new Line(MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-3, 3, 0)),
                MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(3, -3, 0)));

            Centerline = OffsetLine(ln, 0) as Line;
            MoveLineToLayer(Centerline, layer_name);

            // Store the ID's and handles
            _objID = Centerline.Id;
            _objHandle = _objID.Handle;

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

            string support_str = "A: " + AboveConn.ToString() + "\n" + "B: " + BelowConn.ToString();
            DrawMtext(db, doc, ConnectionPoint, support_str, 3, layer_name);
        }

        /// <summary>
        /// Creates the string for writing to the file
        /// </summary>
        /// <param name="prefix_identifier"></param>
        /// <returns></returns>
        public string ToFile()
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
    }
}
