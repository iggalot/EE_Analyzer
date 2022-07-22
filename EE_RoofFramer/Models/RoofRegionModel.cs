using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFramer.Models
{
    /// <summary>
    /// Class that contains model information that defines a roof region
    /// </summary>
    public class RoofRegionModel : acStructuralObject
    {
        public List<int> RaftersList { get; set; } = new List<int>();

        // Slope of the roof region in degrees
        public double Slope { get; set; } = 0;

        // Unit vector defining the uphill direction of the slope
        public Vector3d vDir { get; set; } = new Vector3d(0, 0, 0);

        // Contains the polyline that defines this region
        public Point3d[] RegionBoundary { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lst_rafter">list of rafters to assign to this region</param>
        /// <param name="dead_psf">Dead load for the region in psf in direction of gravity (-z)</param>
        /// <param name="live_psf">Live load for the region in psf in direction of gravity (-z)</param>
        /// <param name="roof_live_psf">Roof live load for the region in psf in direction of gravity (-z)</param>
        public RoofRegionModel(int id, Polyline pline, List<RafterModel> lst_rafter) : base(id)
        {
            int num_vertices = pline.NumberOfVertices;
            RegionBoundary = new Point3d[num_vertices];

            // Get the vertices of the boundary polyline
            for (int i = 0; i < num_vertices; i++)
            {
                RegionBoundary[i] = pline.GetPoint3dAt(i);
            }

            // Add the rafters to the region
            foreach(var rafter in lst_rafter)
            {
                RaftersList.Add(rafter.Id);
            }

            // Take a token rafter and set the uphill direction as start to end
            if(lst_rafter != null && lst_rafter.Count > 0)
            {
                vDir = MathHelpers.Normalize(lst_rafter[0].StartPt.GetVectorTo(lst_rafter[0].EndPt));
            }
        }

        /// <summary>
        /// Constructor used for parsing the data files from a string of text
        /// </summary>
        /// <param name="line"></param>
        public RoofRegionModel(string line) : base()
        {

        }

        #region Abstract class implementation

        /// <summary>
        /// The string to write contents to file
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string ToFile()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Routine to draw this object 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="layer_name"></param>
        /// <param name="conn_dict"></param>
        /// <param name="load_dict"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            throw new NotImplementedException();
        }

        public override void AddConcentratedLoads(BaseLoadModel load_model) { }
        public override void AddConnection(BaseConnectionModel conn) { }
        public override void AddUniformLoads(BaseLoadModel load_model) { }
        public override void HighlightStatus() { }
        public override bool ValidateSupports() { return false; }
        protected override void UpdateCalculations() { }

        public override void CalculateReactions(RoofFramingLayout layout)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
