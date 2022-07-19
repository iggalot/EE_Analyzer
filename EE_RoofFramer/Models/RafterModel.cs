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
using static EE_Analyzer.Utilities.LayerObjects;


namespace EE_RoofFramer.Models
{
    public class RafterModel : acStructuralObject
    {        
        private double Spacing = 24; // tributary width (or rafter spacing)

        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }

        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt); }


        public LoadModel Reaction_SupportA{ get; set; }
        public LoadModel Reaction_SupportB { get; set; }

        public Line Centerline { get; set; } 

        // unit vector for direction of the rafter
        public Vector3d vDir { get; set; }

        public Double Length { get; set; }


       // public int Id { get => _id; set { _id = value; next_id++; } }

        public bool ReactionsCalculatedCorrectly = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="start">start point</param>
        /// <param name="end">end point</param>
        /// <param name="spacing">tributary width or rafter spacing</param>
        public RafterModel(Point3d start, Point3d end, double spacing, string layer_name) : base()
        {
            StartPt = start;
            EndPt = end;

            Spacing = spacing;  // spacing between adjacent rafters

            // Centerline object
            Line ln = new Line(StartPt, EndPt);
            Centerline = OffsetLine(ln, 0) as Line;
            MoveLineToLayer(Centerline, layer_name);

            UpdateCalculations();
            MarkSupportStatus();
        }

        /// <summary>
        /// constructor to create our object from a line of text -- used when parsing the string file
        /// </summary>
        /// <param name="line"></param>
        public RafterModel(string line, string layer_name) : base()
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            if (split_line.Length >= 8)
            {
                if (split_line[index].Substring(0, 1).Equals("R"))
                {
                    // read the previous information that was stored in the file
                    Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));
                    _next_id = Id + 1;

                    // Read spacing
                    Spacing = Double.Parse(split_line[index + 1]);
                    // 0, 1, 2 -- First three values are the start point coord
                    StartPt = new Point3d(Double.Parse(split_line[index + 2]), Double.Parse(split_line[index + 3]), Double.Parse(split_line[index + 4]));
                    // 3, 4, 5 == Next three values are the end point coord
                    EndPt = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));

                    // Centerline object
                    Line ln = new Line(StartPt, EndPt);
                    Centerline = OffsetLine(ln, 0) as Line;
                    MoveLineToLayer(Centerline, layer_name);

                    index = index + 8;  // start index of the first L: marker

                    bool should_continue = true;
                    while (should_continue)
                    {
                        if (split_line[index].Equals("$"))
                        {
                            should_continue = false;
                            continue;
                        }
                        if (split_line[index].Length < 2)
                        {
                            should_continue = false;
                            continue;
                        }

                        if (split_line[index].Substring(0, 2).Equals("SC"))
                        {
                            lst_SupportConnections.Add(Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2)));
                            index++;
                        }
                        else if (split_line[index].Substring(0, 2).Equals("LU"))
                        {
                            lst_UniformLoadModels.Add(Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2)));
                            index++;
                        }
                        else if (split_line[index].Substring(0, 2).Equals("LC"))
                        {
                            lst_SupportConnections.Add(Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2)));
                            index++;
                        } else
                        {
                            should_continue = false;
                        }
                    }

                    UpdateCalculations();
                    MarkSupportStatus();
                    return;
                }
            }
        }

        /// <summary>
        /// Updates calculations for the rafter model
        /// </summary>
        protected override void UpdateCalculations()
        {
            vDir = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));
            Length = MathHelpers.Magnitude(StartPt.GetVectorTo(EndPt));
            UpdateSupportedBy();
            CheckDeterminance();
        }

        // Updates the list of supported by members
        protected override void UpdateSupportedBy()
        {
            lst_SupportedBy.Clear();

            // First find the supports that have this member as an "ABOVE" connection
            foreach (int item in lst_SupportConnections)
            {
                if (ConnectionDictionary != null)
                {
                    if (ConnectionDictionary.ContainsKey(item))
                    {
                        if (ConnectionDictionary[item].AboveConn == Id) // found a connection
                        {
                            lst_SupportedBy.Add(ConnectionDictionary[item].BelowConn); // record the BELOW value of the connection
                        }
                    }
                }
            }
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, ConnectionModel> conn_dict, IDictionary<int, LoadModel> load_dict)
        {
            ConnectionDictionary = conn_dict;
            LoadDictionary = load_dict;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Line ln = new Line(StartPt, EndPt);
                    Centerline = OffsetLine(ln, 0) as Line;
                    MoveLineToLayer(Centerline, layer_name);

                    // indicate if the rafters are adequately supported.
                    UpdateCalculations();
                    MarkSupportStatus();

                    DrawCircle(StartPt, 2, layer_name);
                    DrawCircle(EndPt, 2, layer_name);

                    // Add the rafter ID label
                    DrawMtext(db, doc, MidPt, "#"+Id.ToString(), 3, layer_name);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter [" + CurrentHandle.ToString() + "] information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }    
            }
        }

        /// <summary>
        /// Contains the information format to record this object in a text file
        /// </summary>
        /// <returns></returns>
        public override string ToFile()
        {
            string data = "";
            //RT prefix indicates its a trimmed rafter
            data += "R" + Id.ToString();                // Rafter ID
            data += ","+ Spacing.ToString() + ",";      // Trib width
            data += StartPt.X.ToString() + "," + StartPt.Y.ToString() + "," + StartPt.Z.ToString() + ",";   // Start pt
            data += EndPt.X.ToString() + "," + EndPt.Y.ToString() + "," + EndPt.Z.ToString() + ",";         // End pt

            // add Uniform Loads
            foreach (int item in lst_UniformLoadModels) 
            {
                data += "LU" + item + ",";
            }

            // add Concentrated Loads
            foreach (int item in lst_PtLoadModels)
            {
                data += "LC" + item + ",";
            }

            // add supported by connections
            foreach (int item in lst_SupportConnections)
            {
                data += "SC" + item + ",";
            }

            // End of record
            data += "$";

            return data;
        }

        /// <summary>
        /// Add a load model object
        /// </summary>
        /// <param name="load_model"></param>
        public override void AddUniformLoads(LoadModel load_model, IDictionary<int, LoadModel> dict)
        {
            LoadDictionary = dict;
            lst_UniformLoadModels.Add(load_model.Id);
            UpdateCalculations();
        }

        public override void AddConcentratedLoads(LoadModel load_model, IDictionary<int, LoadModel> dict)
        {
            LoadDictionary = dict;
            lst_UniformLoadModels.Add(load_model.Id);
            UpdateCalculations();
        }

        /// <summary>
        /// Add connection information for beams that are supporting this rafter
        /// </summary>
        /// <param name="conn"></param>
        public override void AddConnection(ConnectionModel conn, IDictionary<int, ConnectionModel> dict)
        {
            ConnectionDictionary = dict;
            lst_SupportConnections.Add(conn.Id);
            UpdateCalculations();
        }


        /// <summary>
        /// Change the color of the rafter line based on its support status
        /// </summary>
        public override void MarkSupportStatus()
        {
            if (CheckDeterminance() == true)
            {
                ChangeLineColor(Centerline, EE_ROOF_Settings.RAFTER_DETERMINATE_PASS_COLOR);
            }
            else
            {
                ChangeLineColor(Centerline, EE_ROOF_Settings.RAFTER_DETERMINATE_FAIL_COLOR);
            }
        }

        private bool CheckDeterminance()
        {
            bool is_determinate = true;
            if ((lst_SupportConnections is null) || (lst_SupportConnections.Count < 2))
            {
                is_determinate = false;
            }

            IsDeterminate = is_determinate;
            return is_determinate;
        }

        public override bool ValidateSupports()
        {
            bool is_valid = false;

            if (lst_SupportConnections.Count > 2)
            {
                is_valid = true;
            }

            return is_valid;
        }
    }
}
