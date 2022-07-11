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


namespace EE_RoofFramer.Models
{
    public class RafterModel
    {
        private double Spacing = 24;

        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }

        public List<LoadModel> LoadModels { get; set; } = new List<LoadModel> { };

        private LoadModel Reaction_StartSupport { get; set; }
        private LoadModel Reaction_EndSupport { get; set; }


        Line Centerline { get; set; } 

        // unit vector for direction of the rafter
        public Vector3d vDir { get; set; }

        public Double Length { get; set; }

        public RafterModel()
        {

        }

        public RafterModel(Point3d start, Point3d end, double spacing)
        {
            StartPt = start;
            EndPt = end;

            Spacing = spacing;  // spacing between adjacent rafters

            vDir = MathHelpers.Normalize(start.GetVectorTo(end));
            Length = MathHelpers.Magnitude(StartPt.GetVectorTo(EndPt));


        }

        public void AddToAutoCADDatabase(Database db, Document doc)
        {

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                   // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                   // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Centerline = OffsetLine(new Line(StartPt, EndPt), 0) as Line;
                    MoveLineToLayer(Centerline, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

                    // Draw the reaction values
                    DrawMtext(db, doc, StartPt, Reaction_StartSupport.ToString(), 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);
                    DrawMtext(db, doc, EndPt, Reaction_EndSupport.ToString(), 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }    
            }
        }

        public void AddLoads(double dead, double live, double roof_live)
        {
            LoadModels.Add(new LoadModel(dead, live, roof_live));
            ComputeSupportReactions();
        }
        private void ComputeSupportReactions()
        {
            double dl = 0;
            double ll = 0;
            double rll = 0;

            foreach (var item in LoadModels)
            {
                dl += 0.5 * Length / 12.0 * item.DL;
                ll += 0.5 * Length / 12.0 * item.LL;
                rll += 0.5 * Length / 12.0 * item.RLL;
            }

            Reaction_StartSupport = new LoadModel(dl, ll, rll);
            Reaction_EndSupport = new LoadModel(dl, ll, rll);
        }
    }
}
