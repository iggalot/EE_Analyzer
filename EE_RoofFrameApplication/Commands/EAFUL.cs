using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
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
    /// <summary>
    /// Command EAFUL to create a full uniform load on an object
    /// </summary>
    public class EAFUL : acEE_Command
    {
        /// <summary>
        /// Command EAFUL adds a full length uniform load to the full length of a member
        /// </summary>
        //        [CommandMethod("EAFUL")]
        public static void AddFullUniformLoadToLayout()
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

            double dead = 100;
            double live = 200;
            double roof_live = 300;
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

                    // second is the line a support beam?  If end points are the same, the line is invalid
                    if (start != end)
                    {
                        // create the load and add it to the dictionaries
                        BaseLoadModel uni_load_model = new LoadModelUniform(CurrentRoofFramingLayout.GetNewId(), start, end, dead, live, roof_live);
                        CurrentRoofFramingLayout.AddLoadToLayout(uni_load_model);                                // add the load to dictionary
                        CurrentRoofFramingLayout.AddAppliedLoadToLayout(uni_load_model.Id, model_num);           // add the applied load to dictionary

                        // create the connection and add it to the dictionaries and to the supporting member
                        BaseConnectionModel conn_model = new ConnectionToExternalLoad(CurrentRoofFramingLayout.GetNewId(), uni_load_model.Id, MathHelpers.GetMidpoint(start, end), model_num, uni_load_model.Id);
                        CurrentRoofFramingLayout.AddConnectionToLayout(conn_model);                         // add the connection to dictionary
                        CurrentRoofFramingLayout.UpdatedSingleSupportedByList(uni_load_model);

                        model_below.AddConnection(conn_model);     // connection to the member below -- no connection above for loads
                    }
                    else
                    {
                        doc.Editor.WriteMessage("Error: Start and End points cannot be the same on uniform looad on member [" + model_num.ToString() + "]");
                    }
                    tr.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Unable to add loads to load model [DL: " + dead + " LL: " + live + "  RLL: " + roof_live + "]: " + e.Message);
                    tr.Abort();
                }
            }

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);

        }
    }
}
