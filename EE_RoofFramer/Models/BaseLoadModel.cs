using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.MathHelpers;


namespace EE_RoofFramer.Models
{
    public enum LoadTypes
    {
        LOAD_TYPE_FULL_UNIFORM_LOAD = 0,
        LOAD_TYPE_CONCENTRATED_LOAD = 1
    }

    public class BaseLoadModel : acStructuralObject
    {
        // Dead load
        public double DL { get; set; }
        // Live load
        public double LL { get; set; }
        // Roof live load
        public double RLL { get; set; }

        public int LoadType { get; set; } = (int)LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD;

        /// <summary>
        /// Default constructor -- used for parsing load types
        /// </summary>
        public BaseLoadModel()
        {

        }

        public BaseLoadModel(int id) : base(id)
        {

        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dead">dead loads</param>
        /// <param name="live">live loads</param>
        /// <param name="roof_live">roof live loads</param>
        public BaseLoadModel(int id, double dead, double live, double roof_live, LoadTypes load_type = LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD) : base(id)
        {
            DL = dead;
            LL = live;
            RLL = roof_live;
            LoadType = (int)load_type;
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, BaseConnectionModel> conn_dict, IDictionary<int, BaseLoadModel> load_dict)
        { 
        
        }

        public override string ToString()
        {
            return "DL: " + Math.Ceiling(DL) + "\nLL: " + Math.Ceiling(LL) + "\nRLL: " + Math.Ceiling(RLL) + " (lbs)";
        }

        public override string ToFile()
        {
            return "";
        }

        /// <summary>
        /// Return the basic factored load combination of 1.2*DL + 1.6*LL
        /// </summary>
        /// <returns></returns>
        public double ComputeFactoredLoad()
        {
            return 1.2*DL + 1.6*LL;
        }

        // Necessary calculations for our Load Model
        public virtual double? GetResultant_DL() { return null; }
        public virtual double? GetResultant_LL() { return null; }
        public virtual double? GetResultant_RLL() { return null; }
        public virtual Point3d? GetResultantPoint3d() { return null; }

        protected override void UpdateCalculations() { }
        public override bool ValidateSupports() { return false; }
        public override void AddConnection(BaseConnectionModel conn, IDictionary<int, BaseConnectionModel> dict) { }
        public override void AddUniformLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict) { }
        public override void AddConcentratedLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict) { }
        public override void HighlightStatus() { }

        protected override void UpdateSupportedBy() { }

        public override void CalculateReactions(RoofFramingLayout layout)
        {
            throw new NotImplementedException();
        }
    }
}
