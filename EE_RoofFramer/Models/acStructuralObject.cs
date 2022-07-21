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

        public Handle _current_objHandle; // the handle of the item after it is reloaded
        public ObjectId _current_objID;  // non persistant object identifier
        public Handle CurrentHandle { get => _current_objHandle; set { _current_objHandle = value; } }

        public Handle _old_objHandle; // the previous handle object identifier 
        public ObjectId _old_objID; // the previous object identifier
        public Handle OldHandle { get => _current_objHandle; set { _current_objHandle = value; } }
        public bool IsDeterminate { get; set; } = false;

        public IDictionary<int, BaseConnectionModel> ConnectionDictionary { get; set; } = new Dictionary<int, BaseConnectionModel>();
        public IDictionary<int, BaseLoadModel> LoadDictionary { get; set; } = new Dictionary<int, BaseLoadModel>();

        public List<int> lst_SupportConnections { get; set; } = new List<int>();
        public List<int> lst_SupportedBy { get; set; } = new List<int>(); // Support connection numbers for connections that are supporting this rafter




        protected abstract void UpdateCalculations();
        protected abstract void UpdateSupportedBy();

        public abstract void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, BaseConnectionModel> conn_dict, IDictionary<int, BaseLoadModel> load_dict);
        public abstract string ToFile();

        public abstract void AddConnection(BaseConnectionModel conn, IDictionary<int, BaseConnectionModel> dict);
        public abstract void AddUniformLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict);
        public abstract void AddConcentratedLoads(BaseLoadModel load_model, IDictionary<int, BaseLoadModel> dict);

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
