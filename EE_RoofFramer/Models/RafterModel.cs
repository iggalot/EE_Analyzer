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
    public class RafterModel
    {
        private int _id = 0;
        private static int next_id = 0;

        private double Spacing = 24; // tributary width (or rafter spacing)

        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }

        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt);  }

        // Reaction connections for beams supporting this rafter
        public List<int> lst_SupportConnections { get; set; } = new List<int> { };
        public List<int> lst_UniformLoadModels { get; set; } = new List<int> { };
        public List<int> lst_PtLoadModels { get; set; } = new List<int> { };




        public Line Centerline { get; set; } 

        // unit vector for direction of the rafter
        public Vector3d vDir { get; set; }

        public Double Length { get; set; }

        public int Id { get => _id; set { _id = value; next_id++; } }

        private bool isDeterminate
        {
            get => (lst_SupportConnections.Count < 2) ? false : true;
        }
        
        public RafterModel()
        {
            UpdateCalculations();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="start">start point</param>
        /// <param name="end">end point</param>
        /// <param name="spacing">tributary width or rafter spacing</param>
        public RafterModel(Point3d start, Point3d end, double spacing)
        {
            StartPt = start;
            EndPt = end;

            Spacing = spacing;  // spacing between adjacent rafters

            Id = next_id;

            UpdateCalculations();
        }

        /// <summary>
        /// constructor to create our object from a line of text -- used when parsing the string file
        /// </summary>
        /// <param name="line"></param>
        public RafterModel(string line)
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            if (split_line.Length >= 8)
            {
                if (split_line[index].Substring(0, 1).Equals("R"))
                {
                    Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));

                    // Read spacing
                    Spacing = Double.Parse(split_line[index + 1]);
                    // 0, 1, 2 -- First three values are the start point coord
                    StartPt = new Point3d(Double.Parse(split_line[index + 2]), Double.Parse(split_line[index + 3]), Double.Parse(split_line[index + 4]));
                    // 3, 4, 5 == Next three values are the end point coord
                    EndPt = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));

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
                    return;
                }
            }
           
        }

        /// <summary>
        /// Updates calculations for the rafter model
        /// </summary>
        private void UpdateCalculations()
        {
            vDir = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));
            Length = MathHelpers.Magnitude(StartPt.GetVectorTo(EndPt));
            ComputeSupportReactions();
        }

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int,ConnectionModel> conn_dict, IDictionary<int,LoadModel> load_dict)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                   // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                   // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Centerline = OffsetLine(new Line(StartPt, EndPt), 0) as Line;
                    MoveLineToLayer(Centerline, layer_name);

                    // indicate if the rafters are adequately supported.
                    MarkRafterSupportStatus();

                    // Draw support connections
                    foreach (var item in lst_UniformLoadModels)
                    {
                        if (load_dict.ContainsKey(item))
                        {
                            load_dict[item].AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
                        }
                    }

                    // Draw support connections
                    foreach (var item in lst_SupportConnections)
                    {
                        if (conn_dict.ContainsKey(item))
                        {
                            conn_dict[item].AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);
                        }
                    }

                    //// Draw the support reactions
                    //if(lst_SupportConnections.Count > 2)
                    //{
                    //    if (Reaction_StartSupport != null)
                    //    {
                    //        DrawMtext(db, doc, lst_SupportConnections[0].ConnectionPoint, Reaction_StartSupport.ToString(), 3, layer_name);
                    //    }

                    //    if (Reaction_EndSupport != null)
                    //    {
                    //        DrawMtext(db, doc, lst_SupportConnections[0].ConnectionPoint, Reaction_EndSupport.ToString(), 3, layer_name);
                    //    }
                    //}


                    // Mark the supports
                    DrawCircle(StartPt, 6, layer_name);
                    DrawCircle(EndPt, 3, layer_name);

                    // Add the rafter ID label
                    DrawMtext(db, doc, MidPt, "#"+Id.ToString(), 3, layer_name);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter [" + Id.ToString() + "] information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }    
            }
        }

        /// <summary>
        /// Contains the information format to record this object in a text file
        /// </summary>
        /// <returns></returns>
        public string ToFile()
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
        public void AddUniformLoads(LoadModel load_model)
        {
            lst_UniformLoadModels.Add(load_model.Id);
            UpdateCalculations();
        }

        private void ComputeSupportReactions()
        {
            double dl = 0;
            double ll = 0;
            double rll = 0;

            if(isDeterminate is true)
            {
                //Point3d start = lst_SupportConnections[0].ConnectionPoint;
                //Point3d end = lst_SupportConnections[1].ConnectionPoint;
                //double a = MathHelpers.Magnitude(start.GetVectorTo(end));
                //double b = MathHelpers.Magnitude(StartPt.GetVectorTo(start));
                //double c = MathHelpers.Magnitude(end.GetVectorTo(EndPt));

                //double total_length = a + b + c;
                //double x_bar = 0.5 * total_length - a;

                //foreach (var item in UniformLoadModels)
                //{
                //    dl += (0.5 * total_length / 12.0 - x_bar) * item.DL;
                //    ll += (0.5 * total_length / 12.0 - x_bar) * item.LL;
                //    rll += (0.5 * total_length / 12.0 - x_bar) * item.RLL;
                //}
            }

 //           Reaction_StartSupport = new LoadModel(dl, ll, rll);
 //           Reaction_EndSupport = new LoadModel(dl, ll, rll);

        }

        /// <summary>
        /// Add connection information for beams that are supporting this rafter
        /// </summary>
        /// <param name="conn"></param>
        public void AddConnection(ConnectionModel conn)
        {
            lst_SupportConnections.Add(conn.Id);
            UpdateCalculations();
        }


        /// <summary>
        /// Change the color of the rafter line based on its support status
        /// </summary>
        public void MarkRafterSupportStatus()
        {
            if (isDeterminate)
            {
                ChangeLineColor(Centerline, "BLUE");
            }
            else
            {
                ChangeLineColor(Centerline, "RED");
            }
        }
    }
}
