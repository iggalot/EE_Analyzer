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
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.MathHelpers;
using EE_Analyzer.Utilities;

namespace EE_RoofFramer.Models
{
    public class UniformLoadModel : BaseLoadModel
    {
        private Point3d _start_location_on_beam;
        private Point3d _end_location_on_beam;


        public Point3d ApplicationPointStart { get => _start_location_on_beam; set { _start_location_on_beam = value; } }
        public Point3d ApplicationPointEnd { get => _end_location_on_beam; set { _end_location_on_beam = value; } }


        public UniformLoadModel(int id, Point3d start_loc, Point3d end_loc, double dead, double live, double roof_live) 
            : base(id, dead, live, roof_live, LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
        {
            ApplicationPointStart = start_loc;
            ApplicationPointEnd = end_loc;
        }



        public UniformLoadModel(string line) : base()
        {
            string[] split_line = line.Split(',');
            int index = 0;

            try
            {
                if (split_line.Length > 5)
                {
                    // Check that this line is a "LOAD" designation "L"
                    if (split_line[index].Substring(0, 2).Equals("LU"))
                    {
                        // read the previous information that was stored in the file
                        Id = Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2));

                        LoadType = Int32.Parse(split_line[index + 1]);
                        DL = Double.Parse(split_line[index + 2]);  // DL
                        LL = Double.Parse(split_line[index + 3]);  // LL
                        RLL = Double.Parse(split_line[index + 4]); // RLL
                        ApplicationPointStart = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));
                        ApplicationPointEnd = new Point3d(Double.Parse(split_line[index + 8]), Double.Parse(split_line[index + 9]), Double.Parse(split_line[index + 10]));
                    }

                    if (split_line[index + 5].Equals("$"))
                        return;
                }
            }
            catch (System.Exception ex)
            {
                throw new SystemException("Error creating uniform load model[" + Id.ToString() + "]: " + ex.Message);
            }


            return;

        }

        public static void ParseUniformLoad()
        {

        }
        /// <summary>
        /// Returns the position of the resultant load -- for uniform loads its the midpoint between the start and end points
        /// </summary>
        /// <returns></returns>
        public override Point3d? GetResultantPoint3d()
        {
            return new Point3d(0.5*(ApplicationPointStart.X + ApplicationPointEnd.X),
                0.5 * (ApplicationPointStart.Y + ApplicationPointEnd.Y),
                0.5 * (ApplicationPointStart.Z + ApplicationPointEnd.Z));
        }

        public override void AddConcentratedLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict)
        {
            throw new NotImplementedException();
        }

        public override void AddConnection(ConnectionModel conn, IDictionary<int, ConnectionModel> dict)
        {
            throw new NotImplementedException();
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, ConnectionModel> conn_dict, IDictionary<int, BaseLoadModel> load_dict)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //Draw an icon for the uniform load
                    Point3d mid_pt = MathHelpers.GetMidpoint(ApplicationPointStart, ApplicationPointEnd);
                    Point3d pt1 = Point3dFromVectorOffset(mid_pt, new Vector3d(-4, 4, 0));
                    Point3d pt2 = Point3dFromVectorOffset(mid_pt, new Vector3d(4, -4, 0));
                    Point3d pt3 = Point3dFromVectorOffset(mid_pt, new Vector3d(-4, -4, 0));
                    Point3d pt4 = Point3dFromVectorOffset(mid_pt, new Vector3d(4, 4, 0));
                    DrawCircle(mid_pt, 4, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);

                    // Draw the icon for the load
                    Line ln1 = OffsetLine(new Line(pt1, pt2), 0);
                    MoveLineToLayer(ln1, layer_name);
                    Line ln2 = OffsetLine(new Line(pt3, pt4), 0);
                    MoveLineToLayer(ln1, layer_name);

                    // Draw the load label
                    DrawMtext(db, doc, mid_pt, this.ToString(), 1, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);

                    // Draw markers at start and stop location of uniform load line
                    DrawCircle(ApplicationPointStart, 2, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
                    DrawCircle(ApplicationPointEnd, 2, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);

                    trans.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Error drawing Load Model [" + Id.ToString() + "]: " + e.Message);
                    trans.Abort();
                }
            }

        }

        public override void AddUniformLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict)
        {
            throw new NotImplementedException();
        }

        public override void CalculateReactions(RoofFramingLayout roof_layout)
        {
            throw new NotImplementedException();
        }

        public override void HighlightStatus()
        {
            throw new NotImplementedException();
        }

        public override string ToFile()
        {
            string data = "";
            string data_prefix = "";
            if (LoadType == (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
            {
                data_prefix += "LU";
                data += data_prefix + Id.ToString() + "," + LoadType.ToString() + "," + DL + "," + LL + "," + RLL + ",";
                data += ApplicationPointStart.X.ToString() + "," + ApplicationPointStart.Y.ToString() + "," + ApplicationPointStart.Z.ToString() + ",";
                data += ApplicationPointEnd.X.ToString() + "," + ApplicationPointEnd.Y.ToString() + "," + ApplicationPointEnd.Z.ToString() + ",";
            }

            return data;
        }

        public override bool ValidateSupports()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateCalculations()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateSupportedBy()
        {
            throw new NotImplementedException();
        }
    }
}
