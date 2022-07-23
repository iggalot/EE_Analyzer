using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_RoofFramer;
using EE_RoofFramer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.SelectionObjects;

namespace EE_RoofFrameApplication.Commands
{
    public class EAPL : acEE_Command
    {
        /// <summary>
        /// Command EAPL function to add a point load to user selected member
        /// </summary>
        //        [CommandMethod("EAPL")]
        public static void AddPointLoadToMember()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            // Select the existing line
            ObjectId selectedID = DoSelectLine(db, doc);

            // Check if the object is valid
            if (selectedID == ObjectId.Null)
            {
                doc.Editor.WriteMessage("Error selecting line, or line does not contain XData tag yet.");
                return;
            }

            double dead = 1000;
            double live = 2000;
            double roof_live = 3000;

            int model_num = -1;
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                try
                {
                    DBObject obj = tr.GetObject(selectedID, OpenMode.ForRead);
                    ResultBuffer rb = obj.XData;

                    // look for the numeric id in each line of the XData
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.TypeCode == 1000)   // 1000 is the code in XData corresponding to the member id number
                        {
                            model_num = Int32.Parse((string)tv.Value);
                            break;  // found it so stop searching
                        }
                    }

                    if (model_num == -1)
                    {
                        doc.Editor.WriteMessage("\nNo valid EE rafter or beam model found.  Cancelling command.");
                        tr.Abort();
                        return;
                    }

                    Point3d load_pt = DoSelectPointOnLine(db, doc, selectedID);
                    // Get member model:
                    bool found = false;
                    Point3d start = new Point3d();
                    Point3d end = new Point3d();

                    acStructuralObject model_below = null;
                    // first is the line a rafter?
                    if (CurrentRoofFramingLayout.dctRafters_Trimmed.ContainsKey(model_num))
                    {
                        model_below = CurrentRoofFramingLayout.dctRafters_Trimmed[model_num];
                        start = ((RafterModel)model_below).StartPt;
                        end = ((RafterModel)model_below).EndPt;
                        found = true;
                    }
                    // if we don't find it in the rafters, then check the support beams list?

                    else if (CurrentRoofFramingLayout.dctSupportBeams.ContainsKey(model_num))
                    {
                        model_below = CurrentRoofFramingLayout.dctSupportBeams[model_num];

                        start = ((SupportModel_SS_Beam)model_below).StartPt;
                        end = ((SupportModel_SS_Beam)model_below).EndPt;
                        found = true;
                    }
                    else
                    {
                        found = false;
                    }

                    // Object wasnt found
                    if (!found)
                    {
                        doc.Editor.WriteMessage("Model [" + model_num + "] was not found in the rafter or support beams table");
                        return;
                    }

                    // Add the concentrated load to the dictionaries
                    BaseLoadModel conc_load_model = new LoadModelConcentrated(CurrentRoofFramingLayout.GetNewId(), load_pt, dead, live, roof_live);
                    CurrentRoofFramingLayout.AddLoadToLayout(conc_load_model);                           // add the load to dictionary
                    CurrentRoofFramingLayout.AddAppliedLoadToLayout(conc_load_model.Id, model_num);      // add the applied load to dictionary

                    // Add the connection to the dictioniaries and to the member below
                    BaseConnectionModel conn_model = new ConnectionToExternalLoad(CurrentRoofFramingLayout.GetNewId(), conc_load_model.Id, load_pt, model_num, conc_load_model.Id);
                    CurrentRoofFramingLayout.AddConnectionToLayout(conn_model);                     // add the connection to dictionary
                    CurrentRoofFramingLayout.UpdatedSingleSupportedByList(conc_load_model);
                    model_below.AddConnection(conn_model);  // connection to the member above

                    tr.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Unable to add concentrated load to member id[ " + model_num + "] -- DL: " + dead + " LL: " + live + "  RLL: " + roof_live + "]: " + e.Message);
                    tr.Abort();
                }
            }

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);

        }
    }
}
