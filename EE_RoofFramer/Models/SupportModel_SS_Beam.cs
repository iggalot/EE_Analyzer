using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;

namespace EE_RoofFramer.Models
{
    /// <summary>
    /// Class to define simply supported supports for joists and rafters
    /// </summary>
    public abstract class SupportModel_SS_Beam
    {
        private const double support_radius = 10;


        List<RafterModel> lst_SupportedMembers { get; set; } = new List<RafterModel>();

        public Point3d SupportA_Loc { get; set; }
        public Point3d SupportB_Loc { get; set; }

        public LoadModel ReactionA { get; set; }
        public LoadModel ReactionB { get; set; }

        public Point3d Start { get; set; }
        public Point3d End { get; set; }

        public bool SupportsAreValid { get => ValidateSupports(); }

        public SupportModel_SS_Beam(Point3d start, Point3d end)
        {
            Start = start;
            End = end;

            SupportA_Loc = Start;
            SupportB_Loc = End;
        }

        private bool ValidateSupports()
        {
            bool is_valid = false;

            if (lst_SupportedMembers.Count > 2)
            {
                if(SupportA_Loc != SupportB_Loc)
                {
                    is_valid = true;
                }
            }

            return is_valid;
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
                    DrawCircle(Start, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, "CONTINUOUS");  // outer pier diameter
                    DrawCircle(End, support_radius, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, "CONTINUOUS");  // outer pier diameter
                }
                catch
                {

                }
            }
        }
    }
}
