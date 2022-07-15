﻿using Autodesk.AutoCAD.ApplicationServices;
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
    public enum LoadTypes
    {
        LOAD_TYPE_FULL_UNIFORM_LOAD = 0,
        LOAD_TYPE_CONCENTRATED_LOAD = 1
    }

    public class LoadModel
    {
        private Handle _objHandle; // persistant object identifier 
        private ObjectId _objID;  // non persistant object identifier

        public Handle Id { get => _objHandle; set { _objHandle = value; } }


        // Dead load
        public double DL { get; set; }
        // Live load
        public double LL { get; set; }
        // Roof live load
        public double RLL { get; set; }

        public int LoadType { get; set; } = (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD;

        public Point3d LoadStartPoint { get; set; }
        public Point3d LoadEndPoint { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public LoadModel()
        {

        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dead">dead loads</param>
        /// <param name="live">live loads</param>
        /// <param name="roof_live">roof live loads</param>
        public LoadModel(double dead, double live, double roof_live, Point3d start, Point3d end, LoadTypes load_type = LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
        {
            DL = dead;
            LL = live;
            RLL = roof_live;
            LoadType = (int)load_type;

            LoadStartPoint = start;
            LoadEndPoint = end;
        }

        /// <summary>
        /// constructor to create our object from a line of text -- used when parsing the string file
        /// </summary>
        /// <param name="line"></param>
        public LoadModel(string line)
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
                        String str = (split_line[index].Substring(2, split_line[index].Length - 2));
                        _objHandle = new Handle(Int64.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier));

                        LoadType = Int32.Parse(split_line[index + 1]);
                        DL = Double.Parse(split_line[index + 2]);  // DL
                        LL = Double.Parse(split_line[index + 3]);  // LL
                        RLL = Double.Parse(split_line[index + 4]); // RLL
                        LoadStartPoint = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));
                        LoadEndPoint = new Point3d(Double.Parse(split_line[index + 8]), Double.Parse(split_line[index + 9]), Double.Parse(split_line[index + 10]));
                    }
                    else if (split_line[index].Substring(0, 2).Equals("LC"))
                    {
                        String str = (split_line[index].Substring(2, split_line[index].Length - 2));
                        _objHandle = new Handle(Int64.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier));

                        LoadType = Int32.Parse(split_line[index + 1]);
                        DL = Double.Parse(split_line[index + 2]);  // DL
                        LL = Double.Parse(split_line[index + 3]);  // LL
                        RLL = Double.Parse(split_line[index + 4]); // RLL
                        LoadStartPoint = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));
                        LoadEndPoint = LoadStartPoint;
                    }
                    else
                    {
                        return;
                    }

                    if (split_line[index + 5].Equals("$"))
                        return;
                }
            } catch (System.Exception ex)
            {
                throw new SystemException("In load model parsing:" + ex.Message);
            }


            return;
        }

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    switch (LoadType)
                    {
                        case (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD:
                            {
                                DrawLine(
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(-4, 4, 0)),
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(4, -4, 0)), layer_name);
                                DrawLine(
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(-4, -4, 0)),
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(4, 4, 0)), layer_name);
                                break;
                            }
                        case (int)LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD:
                            {
                                DrawLine(
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(-4, 4, 0)),
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(4, -4, 0)), layer_name);
                                DrawLine(
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(-4, -4, 0)),
                                    Point3dFromVectorOffset(LoadStartPoint, new Vector3d(4, 4, 0)), layer_name);
                                DrawCircle(LoadStartPoint, 8, layer_name);
                                break;
                            }
                        default:
                            break;
                    }
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Error drawing Load Model [" + Id.ToString() + "]: " + e.Message);
                }
            }
        }

        public override string ToString()
        {
            return "DL: " + Math.Ceiling(DL) + "\nLL: " + Math.Ceiling(LL) + "\nRLL: " + Math.Ceiling(RLL) + " (lbs)";
        }

        public string ToFile()
        {
            string data = "";
            string data_prefix = "";
            if (LoadType == (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
            {
                data_prefix += "LU";
                data += data_prefix + Id.ToString() + "," + LoadType.ToString() + "," + DL + "," + LL + "," + RLL + ",";
                data += LoadStartPoint.X.ToString() + "," + LoadStartPoint.Y.ToString() + "," + LoadStartPoint.Z.ToString() + ",";
                data += LoadEndPoint.X.ToString() + "," + LoadEndPoint.Y.ToString() + "," + LoadEndPoint.Z.ToString() + ",";

            } else  if (LoadType == (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
            {
                data_prefix += "LC";
                data += data_prefix + Id.ToString() + "," + LoadType.ToString() + "," + DL + "," + LL + "," + RLL + ",";
                data += LoadStartPoint.X.ToString() + "," + LoadStartPoint.Y.ToString() + "," + LoadStartPoint.Z.ToString() + ",";

            }
            return data;
        }

        
    }
}
