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

        public List<SupportConnection> lst_SupportConnections { get; set; } = new List<SupportConnection>();

        public List<LoadModel> UniformLoadModels { get; set; } = new List<LoadModel> { };
        public List<LoadModel> PtLoadModels { get; set; } = new List<LoadModel> { };


        public Line Centerline { get; set; }
        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }        
        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt); }

        private double Spacing { get; set; } = 0;  // no spacing for beams

        public Point3d SupportA_Loc { get; set; }
        public Point3d SupportB_Loc { get; set; }

        public LoadModel ReactionA { get; set; }
        public LoadModel ReactionB { get; set; }

        public Point3d Start { get; set; }
        public Point3d End { get; set; }

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
            Start = start;
            End = end;

            SupportA_Loc = Start;
            SupportB_Loc = End;

            Id = next_id;

            UpdateCalculations();
        }


        public SupportModel_SS_Beam(string line)
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;
            if(split_line.Length > 8)
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

                    while (should_parse_uniform_load is true)
                    {
                        should_parse_uniform_load = false;
                        if (split_line[index].Equals("LU"))
                        {
                            should_parse_uniform_load = true;

                            double dl = Double.Parse(split_line[index + 1]);  // DL
                            double ll = Double.Parse(split_line[index + 2]);  // LL
                            double rll = Double.Parse(split_line[index + 3]); // RLL
                            UniformLoadModels.Add(new LoadModel(dl, ll, rll));
                            index = index + 4;

                            if (split_line[index].Equals("$"))
                                return;
                        }
                    }
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
        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    DrawCircle(Start, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);  // outer support diameter
                    DrawCircle(End, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);  // outer support diameter

                    // Draw the beam object
                    Centerline = new Line(Start, End);
                    MoveLineToLayer(OffsetLine(Centerline,0), EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

                    // Draw the support beam label
                    DrawMtext(db, doc, MidPt, Id.ToString(), 3, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

                    // Draw the uniform load values
                    foreach (var item in UniformLoadModels)
                    {
                        item.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
                    }

                    // Draw support connections
                    foreach (var item in lst_SupportConnections)
                    {
                        item.AddToAutoCADDatabase(db, doc);
                    }

                    // Draw the support beam label
                    DrawMtext(db, doc, MidPt, Id.ToString(), 3, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

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
        public void AddSupportConnection(SupportConnection connect)
        {
            // Add the connection information to the beam
            lst_SupportConnections.Add(connect);

            UpdateCalculations();
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
        /// Add a Load Model to the rafter
        /// </summary>
        /// <param name="dead">Dead load in psf</param>
        /// <param name="live">Live load in psf</param>
        /// <param name="roof_live">Roof live load in psf</param>
        public void AddPtLoads(double dead, double live, double roof_live)
        {
            PtLoadModels.Add(new LoadModel(dead, live, roof_live));
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
            data += "," + Spacing.ToString();

            // Uniform Loads
            foreach (var item in UniformLoadModels)
            {
                data += "LU" + item.Id + ",";
            }

            //// Concentrated Loads
            //foreach (var item in ConcentratedLoadModels)
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
    }
}
