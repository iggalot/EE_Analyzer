using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFramer.Models
{
    public enum LoadTypes
    {
        LOAD_TYPE_FULL_UNIFORM_LOAD = 0,
        LOAD_TYPE_CONCENTRATED_LOAD = 1
    }

    public class LoadModel
    {
        private int _id = 0;
        private static int next_id = 0;
        
        public int Id { get => _id; set { _id = value; next_id++; } }

        // Dead load
        public double DL { get; set; }
        // Live load
        public double LL { get; set; }
        // Roof live load
        public double RLL { get; set; }

        public int LoadType { get; set; } = (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD;

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
        public LoadModel(double dead, double live, double roof_live, LoadTypes load_type = LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
        {
            DL = dead;
            LL = live;
            RLL = roof_live;
            LoadType = (int)load_type;

            Id = next_id;
        }

        /// <summary>
        /// constructor to create our object from a line of text -- used when parsing the string file
        /// </summary>
        /// <param name="line"></param>
        public LoadModel(string line)
        {
            string[] split_line = line.Split(',');
            int index = 0;
            bool should_parse_load = true;

            if(split_line.Length > 5)
            {
                while (should_parse_load is true)
                {
                    should_parse_load = false;
                    // Check that this line is a "LOAD" designation "L"
                    if (split_line[index].Substring(0, 2).Equals("LU"))
                    {
                        should_parse_load = true;
                        Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));
                        LoadType = Int32.Parse(split_line[index + 1]);
                        DL = Double.Parse(split_line[index + 2]);  // DL
                        LL = Double.Parse(split_line[index + 3]);  // LL
                        RLL = Double.Parse(split_line[index + 4]); // RLL
                    }
                    else if (split_line[index].Substring(0, 2).Equals("LC"))
                    {
                        should_parse_load = false;
                        throw new NotImplementedException("Concentrated loads are not yet handled");
                    }
                    else
                    {
                        should_parse_load = false;
                        return;
                    }

                    if (split_line[index + 5].Equals("$"))
                        return;
                }
            }


            return;
        }

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {

                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Error drawing Load Model [" + Id.ToString() + "]: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Constructor that takes four string values for each of the components.
        /// </summary>
        /// <param name="str1">the prefix and id of this load model -- in form of LXXXXX</param>
        /// <param name="str2">the dead load string</param>
        /// <param name="str3">the live load string</param>
        /// <param name="str4">the roof live load string</param>
        public LoadModel(string str1, string str2, string str3, string str4, string str5)
        {
            if (str1.Substring(0, 1).Equals("L"))
            {
                Id = Int32.Parse(str1.Substring(1, str1.Length - 1)); // first letter of this string is an "L"
                LoadType = Int32.Parse(str2);
                DL = Double.Parse(str3);  // DL
                LL = Double.Parse(str4);  // LL
                RLL = Double.Parse(str5); // RLL
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
            } else  if (LoadType == (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD)
            {
                data_prefix += "LC";
            }
            data += data_prefix + Id.ToString() + "," + LoadType.ToString() + "," + DL + "," + LL + "," + RLL + ",";
            return data;
        }

        
    }
}
