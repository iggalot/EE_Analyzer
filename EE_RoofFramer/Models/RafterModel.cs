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

        public List<LoadModel> PtLoadModels { get; set; } = new List<LoadModel> { };
  
        // Contains a list of UniformLoadModels
        public List<LoadModel> UniformLoadModels { get; set; } = new List<LoadModel> { };

        // Reaction connections for beams supporting this rafter
        public List<SupportConnection> lst_SupportConnections { get; set; } = new List<SupportConnection> { };

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

            if (split_line.Length > 8)
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


                    bool should_parse_uniform_load = true;

                    index = index + 8;  // start index of the first L: marker

                    //while (should_parse_uniform_load is true)
                    //{
                    //    should_parse_uniform_load = false;
                    //    if (split_line[index].Equals("LU"))
                    //    {
                    //        should_parse_uniform_load = true;

                    //        double dl = Double.Parse(split_line[index + 1]);  // DL
                    //        double ll = Double.Parse(split_line[index + 2]);  // LL
                    //        double rll = Double.Parse(split_line[index + 3]); // RLL
                    //        UniformLoadModels.Add(new LoadModel(dl, ll, rll));
                    //        index = index + 4;

                    //        if (split_line[index].Equals("$"))
                    //            return;
                    //    }
                    //}


                    //bool should_parse_pt_load = true;

                    //while (should_parse_pt_load is true)
                    //{
                    //    if (split_line[index].Equals("LC"))
                    //    {
                    //        should_parse_pt_load = true;

                    //        double dl = Double.Parse(split_line[index + 1]);  // DL
                    //        double ll = Double.Parse(split_line[index + 2]);  // LL
                    //        double rll = Double.Parse(split_line[index + 3]); // RLL
                    //        PtLoadModels.Add(new LoadModel(dl, ll, rll));
                    //        index = index + 4;

                    //        if (split_line[index].Equals("$"))
                    //            return;
                    //    }
                    //}

                    //                // Parse the support connections -- this should probably be in its own file
                    //                bool should_parse_support_conn = true;
                    //                while (should_parse_support_conn is true)
                    //                {
                    //                    should_parse_support_conn = false;
                    //                    if (split_line[index].Substring(0,2).Equals("SC"))
                    //                    {
                    //                        should_parse_support_conn = true;

                    //                        // Parse the id number af the "S" symbol
                    //                        Id = Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2));

                    //                        int supporting = Int32.Parse(split_line[index + 1]); // RLL
                    //                        int supportedby = Int32.Parse(split_line[index + 2]); // RLL

                    ////                        SupportConnection supp = new SupportConnection(new Point3d(x, y, z), supporting, supportedby);
                    ////                        supp.Id = Id;  // reassign the support connection id number

                    //                        // Add the support back to our rafter object
                    //                        AddSupportConnection(supp);
                    //                        index = index + 6;

                    //                        if (split_line[index].Equals("$"))
                    //                            return;
                    //                    }
                    //                }

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

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
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

                    // Draw the uniform load values
                    foreach(var item in UniformLoadModels)
                    {
                        item.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
                    }

                    // Draw support connections
                    foreach (var item in lst_SupportConnections)
                    {
                        item.AddToAutoCADDatabase(db, doc);
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

            // Uniform Loads
            foreach (var item in UniformLoadModels) 
            {
                data += "LU" + item.Id + ",";
            }

            //// Concentrated Loads
            //foreach (var item in PtLoadModels)
            //{
            //    data += "LC" + item.Id;
            //}

            // add supported by connections
            foreach (var item in lst_SupportConnections)
            {
                data += "SC" + ",";
                data += item.Id;
            }

            // End of record
            data += "$";

            return data;
        }

        /// <summary>
        /// Add a Load Model to the rafter
        /// </summary>
        /// <param name="dead">Dead load in psf</param>
        /// <param name="live">Live load in psf</param>
        /// <param name="roof_live">Roof live load in psf</param>
        public void AddUniformLoads(double dead, double live, double roof_live)
        {
            UniformLoadModels.Add(new LoadModel(dead, live, roof_live));
            UpdateCalculations();
        }

        /// <summary>
        /// Add a load model object
        /// </summary>
        /// <param name="load_model"></param>
        public void AddUniformLoads(LoadModel load_model)
        {
            UniformLoadModels.Add(load_model);
            UpdateCalculations();
        }

        private void ComputeSupportReactions()
        {
            double dl = 0;
            double ll = 0;
            double rll = 0;

            if(isDeterminate is true)
            {
                Point3d start = lst_SupportConnections[0].ConnectionPoint;
                Point3d end = lst_SupportConnections[1].ConnectionPoint;
                double a = MathHelpers.Magnitude(start.GetVectorTo(end));
                double b = MathHelpers.Magnitude(StartPt.GetVectorTo(start));
                double c = MathHelpers.Magnitude(end.GetVectorTo(EndPt));

                double total_length = a + b + c;
                double x_bar = 0.5 * total_length - a;

                foreach (var item in UniformLoadModels)
                {
                    dl += (0.5 * total_length / 12.0 - x_bar) * item.DL;
                    ll += (0.5 * total_length / 12.0 - x_bar) * item.LL;
                    rll += (0.5 * total_length / 12.0 - x_bar) * item.RLL;
                }
            }

 //           Reaction_StartSupport = new LoadModel(dl, ll, rll);
 //           Reaction_EndSupport = new LoadModel(dl, ll, rll);

        }

        /// <summary>
        /// Add connection information for beams that are supporting this rafter
        /// </summary>
        /// <param name="conn"></param>
        public void AddSupportConnection(SupportConnection conn)
        {
            lst_SupportConnections.Add(conn);
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
