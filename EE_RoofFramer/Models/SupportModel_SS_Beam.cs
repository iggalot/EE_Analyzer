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

        public List<LoadModel> LoadModels { get; set; } = new List<LoadModel> { };

        public Line Centerline { get; set; }
        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }        
        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt); }

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
        /// Routine to draw the support
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc)
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

                    // Draw the connection info
                    foreach(SupportConnection conn in lst_SupportConnections)
                    {
                        conn.AddToAutoCADDatabase(db, doc);
                    }
                }
                catch (System.Exception ex)
                {
                    throw new InvalidOperationException("Error adding support beam to AutoCAD drawing: " + ex.Message);
                    trans.Abort();
                }

                trans.Commit();
            }
        }

        public void AddSupportConnection(SupportConnection connect)
        {
            // Add the connection information to the beam
            lst_SupportConnections.Add(connect);
        }

        /// <summary>
        /// Add a Load Model to the rafter
        /// </summary>
        /// <param name="dead">Dead load in psf</param>
        /// <param name="live">Live load in psf</param>
        /// <param name="roof_live">Roof live load in psf</param>
        public void AddLoads(double dead, double live, double roof_live)
        {
            LoadModels.Add(new LoadModel(dead, live, roof_live));
            ComputeSupportReactions();
        }
        /// <summary>
        /// 
        /// </summary>
        private void ComputeSupportReactions()
        {
            double dl = -1;
            double ll = -1;
            double rll = -1;

            ReactionA = new LoadModel(dl, ll, rll);
            ReactionB = new LoadModel(dl, ll, rll);
        }
    }
}
