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
    public class SupportConnection
    {
        private double width = 4;
        private int _id = 0;
        private static int next_id = 0;

        public Point3d ConnectionPoint { get; set; }

        // id number of the object supported below this connection (the supporting item)
        public int BelowConn { get; set; } = -1;

        // id number of the object supported above this connection (item being supporteD)
        public int AboveConn { get; set; } = -1;

        public int Id { get => _id; set { _id = value; next_id++; } }

        public SupportConnection()
        {

        }

        public SupportConnection(Point3d pt, int supporting, int supported_by)
        {
            ConnectionPoint = pt;
            BelowConn = supported_by;
            AboveConn = supporting;

            Id = next_id;
        }

        public void AddToAutoCADDatabase(Database db, Document doc)
        {

            // Draw a box shape for a support connection
            Point3d pt1 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-5, -5, 0));
            Point3d pt2 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(-5, 5, 0));
            Point3d pt3 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(5, 5, 0));
            Point3d pt4 = MathHelpers.Point3dFromVectorOffset(ConnectionPoint, new Vector3d(5, -5, 0));

            Line ln1 = OffsetLine(new Line(pt1, pt2), 0) as Line;
            MoveLineToLayer(ln1, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);
            
            Line ln2 = OffsetLine(new Line(pt2, pt3), 0) as Line;
            MoveLineToLayer(ln2, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);

            Line ln3 = OffsetLine(new Line(pt3, pt4), 0) as Line;
            MoveLineToLayer(ln3, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);

            Line ln4 = OffsetLine(new Line(pt4, pt1), 0) as Line;
            MoveLineToLayer(ln4, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);

            if(BelowConn != null && AboveConn != null)
            {
                string support_str = "A: " + AboveConn.ToString() + "\n" + "B: " + BelowConn.ToString();
                DrawMtext(db, doc, ConnectionPoint, support_str, 3, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);
            }
        }

        /// <summary>
        /// Creates the string for writing to the file
        /// </summary>
        /// <param name="prefix_identifier"></param>
        /// <returns></returns>
        public string ToFile(string prefix_identifier)
        {
            string data = "";
            data += prefix_identifier + Id.ToString() + "," + ConnectionPoint.X + "," + ConnectionPoint.Y + "," + ConnectionPoint.Z + "," + AboveConn + "," + BelowConn + ",";
            return data;
        }
    }
}
