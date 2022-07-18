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
        public static int _next_id = 0;

        // inteiger identifier
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                _next_id++;
            }
        }

        public Handle _current_objHandle; // the handle of the item after it is reloaded
        public ObjectId _current_objID;  // non persistant object identifier
        public Handle CurrentHandle { get => _current_objHandle; set { _current_objHandle = value; } }

        public Handle _old_objHandle; // the previous handle object identifier 
        public ObjectId _old_objID; // the previous object identifier
        public Handle OldHandle { get => _current_objHandle; set { _current_objHandle = value; } }
        public bool IsDeterminate { get; set; } = false;

        public IDictionary<int, ConnectionModel> ConnectionDictionary { get; set; } = new Dictionary<int, ConnectionModel>();
        public IDictionary<int, LoadModel> LoadDictionary { get; set; } = new Dictionary<int, LoadModel>();

        public List<int> lst_SupportConnections { get; set; } = new List<int>();
        public List<int> lst_UniformLoadModels { get; set; } = new List<int> { };
        public List<int> lst_PtLoadModels { get; set; } = new List<int> { };
        public List<int> lst_SupportedBy { get; set; } = new List<int>(); // Support connection numbers for connections that are supporting this rafter




        protected abstract void UpdateCalculations();
        protected abstract void UpdateSupportedBy();

        public abstract void AddToAutoCADDatabase(Database db, Document doc, string layer_name, IDictionary<int, ConnectionModel> conn_dict, IDictionary<int, LoadModel> load_dict);
        public abstract string ToFile();

        public abstract void AddConnection(ConnectionModel conn, IDictionary<int, ConnectionModel> dict);
        public abstract void AddUniformLoads(LoadModel load_model, IDictionary<int, LoadModel> dict);
        public abstract void AddConcentratedLoads(LoadModel load_model, IDictionary<int, LoadModel> dict);

        // For drawing the status of the member -- color changes for indeterminancy etc.
        public abstract void MarkSupportStatus();

        public abstract bool ValidateSupports();

        public acStructuralObject()
        {
            Id = _next_id;
        }
    }
}
