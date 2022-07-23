using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFramer.Models
{
    public abstract class acStructuralObject
    {
        private int _id = 0;

        // integer identifier
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        // All of the connection made to this member
        public List<int> lst_SupportConnections { get; set; } = new List<int>();

        // All of the connections where this member is the ABOVECONN
        public List<int> lst_SupportedBy { get; set; } = new List<int>(); // Support connection numbers for connections that are supporting this rafter




        protected abstract void UpdateCalculations();
        public abstract void AddToAutoCADDatabase(Database db, Document doc, string layer_name);
        public abstract string ToFile();

        public abstract void AddConnection(BaseConnectionModel conn);
        public abstract void AddUniformLoads(BaseLoadModel load_model);
        public abstract void AddConcentratedLoads(BaseLoadModel load_model);

        // For drawing the status of the member -- color changes for indeterminancy etc.
        public abstract void HighlightStatus();

        public abstract bool ValidateSupports();

        public abstract void CalculateReactions(RoofFramingLayout layout);

        public acStructuralObject(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public acStructuralObject()
        {

        }
    }
}
