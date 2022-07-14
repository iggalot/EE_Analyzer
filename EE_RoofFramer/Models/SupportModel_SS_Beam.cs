using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.LineObjects;

namespace EE_RoofFramer.Models
{
    /// <summary>
    /// Class to define simply supported supports for joists and rafters
    /// </summary>
    public class SupportModel_SS_Beam
    {
        private const double support_radius = 10;

        private int _id = 0;
        private static int next_id = 0;

        public List<int> lst_SupportConnections { get; set; } = new List<int>();

        public List<int> lst_UniformLoadModels { get; set; } = new List<int> { };
        public List<int> lst_PtLoadModels { get; set; } = new List<int> { };


        public Line Centerline { get; set; }
        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }        
        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt); }

        private double Spacing { get; set; } = 0;  // no spacing for beams

        public Point3d SupportA_Loc { get; set; }
        public Point3d SupportB_Loc { get; set; }

        public LoadModel ReactionA { get; set; }
        public LoadModel ReactionB { get; set; }

        // unit vector for direction of the rafter
        public Vector3d vDir { get; set; }

        public Double Length { get; set; }

        public int Id { get => _id; set { _id = value; next_id++; } }

        private bool isDeterminate
        {
            get => (lst_SupportConnections.Count < 2) ? false : true;
        }

        public bool SupportsAreValid { get => ValidateSupports(); }

        public SupportModel_SS_Beam(Point3d start, Point3d end)
        {
            StartPt = start;
            EndPt = end;

            SupportA_Loc = StartPt;
            SupportB_Loc = EndPt;

            Id = next_id;

            UpdateCalculations();
        }


        public SupportModel_SS_Beam(string line)
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "BEAM" designation "B"
            int index = 0;
            int num_items = 0;
            if(split_line.Length >= 8)
            {
                if (split_line[index].Substring(0, 1).Equals("B"))
                {
                    Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));

                    // Read spacing
                    Spacing = Double.Parse(split_line[index + 1]);
                    // 0, 1, 2 -- First three values are the start point coord
                    StartPt = new Point3d(Double.Parse(split_line[index + 2]), Double.Parse(split_line[index + 3]), Double.Parse(split_line[index + 4]));
                    // 3, 4, 5 == Next three values are the end point coord
                    EndPt = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));

                    index = index + 8;

                    bool should_continue = true;
                    while (should_continue)
                    {
                        if(split_line[index].Equals("$"))
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
                        }
                        else
                        {
                            should_continue = false;
                        }
                    }

                    UpdateCalculations();
                }
            }
        }



        private bool ValidateSupports()
        {
            bool is_valid = false;

            if (lst_SupportConnections.Count > 2)
            {
                if(SupportA_Loc != SupportB_Loc)
                {
                    is_valid = true;
                }
            }

            return is_valid;
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


        /// <summary>
        /// Routine to draw the support beam
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, ConnectionModel> dict)
        {

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    DrawCircle(StartPt, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);  // outer support diameter
                    DrawCircle(EndPt, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);  // outer support diameter

                    // Draw the beam object
                    Centerline = new Line(StartPt, EndPt);
                    MoveLineToLayer(OffsetLine(Centerline,0), EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

                    // Draw the support beam label
                    DrawMtext(db, doc, MidPt, Id.ToString(), 3, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

                    // Draw support connections
                    foreach (var item in lst_SupportConnections)
                    {
                        if (dict.ContainsKey(item))
                        {
                            dict[item].AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);
                        }
                    }

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error adding support beam to AutoCAD drawing: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        /// <summary>
        /// Adds a support connection to this beam
        /// </summary>
        /// <param name="connect"></param>
        public void AddConnection(ConnectionModel connect)
        {
            // Add the connection information to the beam
            lst_SupportConnections.Add(connect.Id);

            UpdateCalculations();
        }

        /// <summary>
        /// Add a Load Model to the rafter
        /// </summary>
        /// <param name="dead">Dead load in psf</param>
        /// <param name="live">Live load in psf</param>
        /// <param name="roof_live">Roof live load in psf</param>
        public void AddUniformLoads(LoadModel model)
        {
            lst_UniformLoadModels.Add(model.Id);
            UpdateCalculations();
        }

        /// <summary>
        /// Add a Load Model to the rafter
        /// </summary>
        /// <param name="dead">Dead load in psf</param>
        /// <param name="live">Live load in psf</param>
        /// <param name="roof_live">Roof live load in psf</param>
        public void AddPtLoads(LoadModel model)
        {
            lst_PtLoadModels.Add(model.Id);
            UpdateCalculations();
        }



        /// <summary>
        /// Computes the support reactions
        /// </summary>
        private void ComputeSupportReactions()
        {
            double dl = -1;
            double ll = -1;
            double rll = -1;

            ReactionA = new LoadModel(dl, ll, rll);
            ReactionB = new LoadModel(dl, ll, rll);
        }

        /// <summary>
        /// Contains the information format to record this object in a text file
        /// </summary>
        /// <returns></returns>
        public string ToFile()
        {
            string data = "";
            //RT prefix indicates its a trimmed rafter
            data += "B" + Id.ToString();                // Rafter ID
            data += "," + Spacing.ToString() + ",";

            data += StartPt.X.ToString() + "," + StartPt.Y.ToString() + "," + StartPt.Z.ToString() + ",";
            data += EndPt.X.ToString() + "," + EndPt.Y.ToString() + "," + EndPt.Z.ToString() + ",";


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
    }
}
