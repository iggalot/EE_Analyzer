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

namespace EE_RoofFramer.Models
{
    public class LoadModelConcentrated : BaseLoadModel
    {
        private Point3d _location_on_beam = new Point3d();

        public Point3d ApplicationPoint { get => _location_on_beam; set { _location_on_beam = value; } }

        public LoadModelConcentrated(int id, Point3d loc , double dead, double live, double roof_live) 
            : base(id, dead, live, roof_live, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD)
        {
            ApplicationPoint = loc;
        }

        public LoadModelConcentrated(string line)
        {
            string[] split_line = line.Split(',');
            int index = 0;

            try
            {
                if (split_line.Length > 6)
                {
                     if (split_line[index].Substring(0, 1).Equals("L"))
                    {
                        // read the previous information that was stored in the file
                        Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));

                        LoadType = Int32.Parse(split_line[index + 1]);
                        DL = Double.Parse(split_line[index + 2]);  // DL
                        LL = Double.Parse(split_line[index + 3]);  // LL
                        RLL = Double.Parse(split_line[index + 4]); // RLL
                        ApplicationPoint = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));
                    }
 
                    if (split_line[index + 5].Equals("$"))
                        return;
                }
            }
            catch (System.Exception ex)
            {
                throw new SystemException("Error creating concentrated load model[" + Id.ToString() + "]: " + ex.Message);
            }

            return;
        }

        /// <summary>
        /// Returns the position of the resultant load -- for concentrated loads its the application point
        /// </summary>
        /// <returns></returns>
        public override Point3d? GetResultantPoint3d()
        {
            return _location_on_beam;
        }

        public override void AddConcentratedLoads(BaseLoadModel load_model)
        {
            throw new NotImplementedException();
        }

        public override void AddConnection(BaseConnectionModel conn)
        {
            throw new NotImplementedException();
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            double icon_size = EE_ROOF_Settings.DEFAULT_ROOF_CONN_ICON_WIDTH / 2.0;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //Draw an icon for the uniform load
                    Point3d pt5 = Point3dFromVectorOffset(ApplicationPoint, new Vector3d(-icon_size, 0, 0));
                    Point3d pt6 = Point3dFromVectorOffset(ApplicationPoint, new Vector3d(icon_size, 0, 0));
                    Point3d pt7 = Point3dFromVectorOffset(ApplicationPoint, new Vector3d(0, -icon_size, 0));
                    Point3d pt8 = Point3dFromVectorOffset(ApplicationPoint, new Vector3d(0, icon_size, 0));

                    // Draw the icon for the load
                    Line ln1 = OffsetLine(new Line(pt5, pt8), 0);
                    MoveLineToLayer(ln1, layer_name);
                    Line ln2 = OffsetLine(new Line(pt8, pt6), 0);
                    MoveLineToLayer(ln2, layer_name);
                    Line ln3 = OffsetLine(new Line(pt6, pt7), 0);
                    MoveLineToLayer(ln3, layer_name);
                    Line ln4 = OffsetLine(new Line(pt7, pt5), 0);
                    MoveLineToLayer(ln4, layer_name);

                    // Draw the load label
                    DrawMtext(db, doc, ApplicationPoint, this.ToString(), 1, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);

                    // Mark the load label location
                    DrawCircle(ApplicationPoint, icon_size * 0.5, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
                    DrawCircle(ApplicationPoint, icon_size * 0.5 * 0.95, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);

                    trans.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Error drawing Load Model [" + Id.ToString() + "]: " + e.Message);
                    trans.Abort();
                }
            }

        }

        public override void AddUniformLoads(BaseLoadModel load_model)
        {
            throw new NotImplementedException();
        }

        public override void CalculateReactions(RoofFramingLayout layout)
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
            if (LoadType == (int)LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD)
            {
                data_prefix += "L";
                data += data_prefix + Id.ToString() + "," + LoadType.ToString() + "," + DL + "," + LL + "," + RLL + ",";
                data += ApplicationPoint.X.ToString() + "," + ApplicationPoint.Y.ToString() + "," + ApplicationPoint.Z.ToString() + ",";
            }
            return data;
        }

        public override string ToString()
        {
            return "DL: " + Math.Ceiling(DL) + "\nLL: " + Math.Ceiling(LL) + "\nRLL: " + Math.Ceiling(RLL) + " (lbs)";
        }

        public override bool ValidateSupports()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateCalculations()
        {
            throw new NotImplementedException();
        }
    }
}
