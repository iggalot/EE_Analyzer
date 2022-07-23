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
    public class EAFS : acEE_Command
    {

        /// <summary>
        /// The command EAFS to add a new foundation support to a point on the model.
        /// </summary>
        //        [CommandMethod("EAFS")]
        public static void CreateNewFoundationSupport()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            try
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    // Select the line object for the point
                    ObjectId line_obj_id = DoSelectLine(db, doc);

                    if (line_obj_id == ObjectId.Null)
                    {
                        doc.Editor.WriteMessage("Invalid line selected in Create New foundation Support");
                    }

                    DBObject obj = tr.GetObject(line_obj_id, OpenMode.ForRead);
                    ResultBuffer rb = obj.XData;

                    int model_num = -1;

                    // look for the numeric id in each line of the XData
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.TypeCode == 1000)   // 1000 is the code in XData corresponding to the member id number
                        {
                            model_num = Int32.Parse((string)tv.Value);
                            break;  // found it so stop searching
                        }
                    }

                    // if no data on this object, then cancel the operation
                    if (model_num == -1)
                    {
                        doc.Editor.WriteMessage("\nNo valid XData id found EE line object or beam model found.  Cancelling command.");
                        tr.Abort();
                        return;
                    }

                    // Select the nearest point on the line object
                    Point3d pt = DoSelectPointOnLine(db, doc, line_obj_id);
                    if ((pt == null) || (pt.X == Double.MaxValue && pt.Y == Double.MaxValue && pt.Z == Double.MaxValue))
                    {
                        doc.Editor.WriteMessage("\nInvalid point selected in Create New foundation Support");
                        return;
                    }


                    // Add the connection to the member
                    // find the rafter
                    bool found = false;
                    Point3d start;
                    Point3d end;

                    acStructuralObject model_above = null;
                    // first is the line a rafter?
                    if (CurrentRoofFramingLayout.dctRafters_Trimmed.ContainsKey(model_num))
                    {
                        model_above = CurrentRoofFramingLayout.dctRafters_Trimmed[model_num];
                        start = ((RafterModel)model_above).StartPt;
                        end = ((RafterModel)model_above).EndPt;
                        found = true;
                    }
                    // if we don't find it in the rafters, then check the support beams list?
                    else if (CurrentRoofFramingLayout.dctSupportBeams.ContainsKey(model_num))
                    {
                        model_above = CurrentRoofFramingLayout.dctSupportBeams[model_num];
                        start = ((SupportModel_SS_Beam)model_above).StartPt;
                        end = ((SupportModel_SS_Beam)model_above).EndPt;
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

                    // Create the connecton to the foundation and add it to the member being supported
                    BaseConnectionModel fdn_conn = new ConnectionToFoundation(CurrentRoofFramingLayout.GetNewId(), pt, model_num);          // create the foundation connection
                    CurrentRoofFramingLayout.AddConnectionToLayout(fdn_conn);                                                               // add the connection to our list
                    model_above.AddConnection(fdn_conn);                                                                                    // add the connection to the member above
                }
            }
            catch (System.Exception e)
            {
                doc.Editor.WriteMessage("\nError creating foundation connection: " + "]: " + e.Message);
                return;
            }

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);
        }
    }
}
