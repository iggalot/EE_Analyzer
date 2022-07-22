using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using EE_Analyzer.Utilities;
using EE_RoofFramer;
using EE_RoofFramer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.LinetypeObjects;
using static EE_Analyzer.Utilities.ModifyAutoCADGraphics;
using static EE_Analyzer.Utilities.PolylineObjects;
using static EE_Analyzer.Utilities.DimensionObjects;
using static EE_Analyzer.Utilities.SelectionObjects;
using static EE_Analyzer.Utilities.XData;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
// Get our AutoCAD API objects



namespace EE_RoofFrameApplication
{
    public class RoofFramerApplication
    {
        public static Document doc { get; } = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public static Database db { get; } = doc.Database;

        // Constructor for setting up our application
        public RoofFramerApplication()
        {


        }
        
        public void Run()
        {
            // check that the user is authorized
            if (!ValidateUser())
                return;

            LoadEE_RoofFramingCommands();
        }

        // Register the commands within Autocad.
        private void LoadEE_RoofFramingCommands()
        {
            Autodesk.AutoCAD.Internal.Utils.AddCommand("ECRL", "ECRL", "ECRL", CommandFlags.Modal, CreateRafterLayout);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EANB", "EANB", "EANB", CommandFlags.Modal, CreateNewSupportBeam);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("ERDA", "ERDA", "ERDA", CommandFlags.Modal, ReloadDrawingFromFile);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EPRC", "EPRC", "EPRC", CommandFlags.Modal, PerformReactionCalculations);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAPL", "EAPL", "EAPL", CommandFlags.Modal, AddPointLoadToMember);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAFUL", "EAFUL", "EAFUL", CommandFlags.Modal, AddFullUniformLoadToLayout);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAFS", "EAFS", "EAFS", CommandFlags.Modal, CreateNewFoundationSupport);
        }

        private RoofFramingLayout OnCreateLayout()
        {
            RoofFramingLayout layout = new RoofFramingLayout(db, doc);            
            LoadAutoCADSettings();
            layout.ReadAllDataFromFiles();     // read existing data from the text files
            Thread.Sleep(1000);
            layout.DrawAllRoofFraming();       // redraw the data now that it's read

            return layout;
        }


        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        private void LoadAutoCADSettings()
        {
            // Load our linetype
            LoadLineTypes("CENTER", doc, db);
            LoadLineTypes("DASHED", doc, db);
            LoadLineTypes("HIDDEN", doc, db);
            LoadLineTypes("CENTERX2", doc, db);
            LoadLineTypes("DASHEDX2", doc, db);
            LoadLineTypes("HIDDENX2", doc, db);
            LoadLineTypes("CENTER2", doc, db);
            LoadLineTypes("DASHED2", doc, db);
            LoadLineTypes("HIDDEN2", doc, db);

            // Create our layers
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_BOUNDINGBOX_LAYER, doc, db, 4); // cyan
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER, doc, db, 3); // green
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RIDGE_SUPPORT_LAYER, doc, db, 3); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_HIPVALLEY_SUPPORT_LAYER, doc, db, 3); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WALL_SUPPORT_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, doc, db, 140);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER, doc, db, 52); // gold color 
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER, doc, db, 32); // burnt orange color 

            //Create the EE dimension style
            CreateEE_DimensionStyle(EE_ROOF_Settings.DEFAULT_EE_DIMSTYLE_NAME);
        }

        /// <summary>
        /// Rudimentary user lock for the application
        /// </summary>
        /// <returns></returns>
        protected bool ValidateUser()
        {
            // rudimentary copy protection based on current time 
            if (EE_ROOF_Settings.APP_REGISTRATION_DATE < DateTime.Now.AddDays(-1 * EE_ROOF_Settings.DAYS_UNTIL_EXPIRES))
            {
                // Update the expires 
                MessageBox.Show("Time has expired on this application. Contact the developer for a new licensed version.");
                return false;
            }

            return true;
        }

        private void OnCommandTerminate(RoofFramingLayout layout)
        {
            layout.DrawAllRoofFraming();    // redraw the drawing since the data has been changed
            layout.WriteAllDataToFiles();   // save the work to file
        }

        #region Command definitions
        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        private void EE_ApplicationSetup()
        {
            // Load our linetype
            LoadLineTypes("CENTER", doc, db);
            LoadLineTypes("DASHED", doc, db);
            LoadLineTypes("HIDDEN", doc, db);
            LoadLineTypes("CENTERX2", doc, db);
            LoadLineTypes("DASHEDX2", doc, db);
            LoadLineTypes("HIDDENX2", doc, db);
            LoadLineTypes("CENTER2", doc, db);
            LoadLineTypes("DASHED2", doc, db);
            LoadLineTypes("HIDDEN2", doc, db);

            // Create our layers
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_BOUNDINGBOX_LAYER, doc, db, 4); // cyan
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER, doc, db, 3); // green
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, doc, db, 2); // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RIDGE_SUPPORT_LAYER, doc, db, 3); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_HIPVALLEY_SUPPORT_LAYER, doc, db, 3); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WALL_SUPPORT_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 2); // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 140); // blue


            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, doc, db, 140);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER, doc, db, 52); // gold color 
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER, doc, db, 32); // burnt orange color 

            //Create the EE dimension style
            CreateEE_DimensionStyle(EE_ROOF_Settings.DEFAULT_EE_DIMSTYLE_NAME);
        }

        /// <summary>
        /// Main functionality for ECRL command
        /// </summary>
        //        [CommandMethod("ECRL")]
        private void CreateRafterLayout()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            // Set up our initial work.  If false, end the program.
            if (CurrentRoofFramingLayout.OnRoofFramingLayoutCreate() is false)
                return;

            CurrentRoofFramingLayout.FirstLoad = true;

            // Keep reloading the dialog box if we are in preview mode
            while (CurrentRoofFramingLayout.PreviewMode == true)
            {
                EE_ROOFInputDialog dialog;
                if (CurrentRoofFramingLayout.FirstLoad)
                {
                    // Use the default values
                    dialog = new EE_ROOFInputDialog(CurrentRoofFramingLayout, CurrentRoofFramingLayout.ShouldClose, CurrentRoofFramingLayout.CurrentDirectionMode);
                    CurrentRoofFramingLayout.FirstLoad = false;
                }
                else
                {
                    // Otherwise reload the previous iteration values
                    dialog = new EE_ROOFInputDialog(CurrentRoofFramingLayout, CurrentRoofFramingLayout.ShouldClose, CurrentRoofFramingLayout.CurrentDirectionMode);
                }

                CurrentRoofFramingLayout.ShouldClose = dialog.dialog_should_close;
                CurrentRoofFramingLayout.IsComplete = dialog.dialog_is_complete;

                if (dialog.dialog_should_close || dialog.dialog_is_complete)
                {
                    dialog.DialogResult = false;
                    break; // exit our loop
                }
                else
                {
                    var result = AcAp.ShowModalWindow(dialog);
                    if (result.Value)
                    {
                        doc.Editor.WriteMessage("\nDialog displayed and successfully entered");
                    }
                }

                if (dialog.DialogResult == false)
                {
                    break;
                }

                CurrentRoofFramingLayout.CurrentDirectionMode = dialog.current_preview_mode_number;
            }

            //            CurrentFoundationLayout.PerformRafterReactionCalculations(); // compute the support reactions

            this.OnCommandTerminate(CurrentRoofFramingLayout);
        }

        /// <summary>
        /// The ENB command to add a new beam to the screen.
        /// </summary>
        //        [CommandMethod("ENB")]
        private void CreateNewSupportBeam()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            Point3d[] pt = PromptUserforLineEndPoints(db, doc);

            if ((pt == null) || pt.Length < 2)
            {
                doc.Editor.WriteMessage("\nInvalid input for endpoints in CreateNewSupportBeam");
                return;
            }

            // Create the new beam and set the XDAta object number reference
            SupportModel_SS_Beam beam = new SupportModel_SS_Beam(CurrentRoofFramingLayout.GetNewId(), pt[0], pt[1], EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

            // Now check if the support intersects the rafter
            foreach (KeyValuePair<int, RafterModel> kvp in CurrentRoofFramingLayout.dctRafters_Trimmed)
            {
                IntersectPointData intPt = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(beam.StartPt, beam.EndPt, kvp.Value.StartPt, kvp.Value.EndPt);
                if (intPt is null)
                {
                    continue;
                }

                // if its a valid intersection, make a new connection, add it to the support and the rafter,
                if (intPt.isWithinSegment is true)
                {
                    BaseConnectionModel support_conn 
                        = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), intPt.Point, kvp.Value.Id, beam.Id, ConnectionTypes.CONN_TYPE_MBR_TO_MBR);

                    CurrentRoofFramingLayout.AddConnectionToLayout(support_conn);       // add the connection to dictionary
                    beam.AddConnection(support_conn);                                   // Update the beam object to indicate the support connection
                    kvp.Value.AddConnection(support_conn);                              // update the rafter object to indicate the support in it
                }
            }

            // Add the beam to the support beams list
            CurrentRoofFramingLayout.AddBeamToLayout(beam);

            // Need to slow down the program otherwise it races through reading the data and goes straight to drawing.
            Thread.Sleep(1000);

            this.OnCommandTerminate(CurrentRoofFramingLayout);
        }

        /// <summary>
        /// Command ERDA to reload and redraw the model information information.
        /// </summary>
        //       [CommandMethod("ERDA")]
        private void ReloadDrawingFromFile()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items
        }

        /// <summary>
        /// Command EPRC to perform the calculations
        /// </summary>
        //       [CommandMethod("EPRC")]
        private void PerformReactionCalculations()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            // Do our calculations
            foreach (KeyValuePair<int, RafterModel> item in CurrentRoofFramingLayout.dctRafters_Trimmed)
            {
                // Get a rafter
                RafterModel rafter = item.Value;

                rafter.CalculateReactions(CurrentRoofFramingLayout);

                // Needed in case the rafter reactions haven't been calculated due to being indeterminate
                if (rafter.Reactions.Count >= 2)
                {
                    // Reactions A
                    DrawMtext(db, doc, ((LoadModelConcentrated)rafter.Reactions[0]).ApplicationPoint, rafter.Reactions[0].ToString(), 2, EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER);
                    // Reaction B
                    DrawMtext(db, doc, ((LoadModelConcentrated)rafter.Reactions[1]).ApplicationPoint, rafter.Reactions[1].ToString(), 2, EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER);
                }
            }

            this.OnCommandTerminate(CurrentRoofFramingLayout);
        }

        /// <summary>
        /// Command EAPL function to add a point load to user selected member
        /// </summary>
        //        [CommandMethod("EAPL")]
        private void AddPointLoadToMember()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

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

                    Point3d load_pt = SelectionObjects.DoSelectPointOnLine(db, doc, selectedID);
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
                    BaseLoadModel conc_model = new LoadModelConcentrated(CurrentRoofFramingLayout.GetNewId(), load_pt, dead, live, roof_live);
                    CurrentRoofFramingLayout.AddLoadToLayout(conc_model);                           // add the load to dictionary
                    CurrentRoofFramingLayout.AddAppliedLoadToLayout(conc_model.Id, model_num);      // add the applied load to dictionary

                    // Add the connection to the dictioniaries and to the member below
                    BaseConnectionModel conn_model = new ConnectionToExternalLoad(CurrentRoofFramingLayout.GetNewId(), conc_model.Id, load_pt, model_num, conc_model.Id);
                    CurrentRoofFramingLayout.AddConnectionToLayout(conn_model);                     // add the connection to dictionary
                    model_below.AddConnection(conn_model);  // connection to the member above

                    tr.Commit();
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("Unable to add concentrated load to member id[ " + model_num + "] -- DL: " + dead + " LL: " + live + "  RLL: " + roof_live + "]: " + e.Message);
                    tr.Abort();
                }
            }

            this.OnCommandTerminate(CurrentRoofFramingLayout);

        }

        /// <summary>
        /// Command EAFUL adds a full length uniform load to the full length of a member
        /// </summary>
        //        [CommandMethod("EAFUL")]
        private void AddFullUniformLoadToLayout()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

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
                        BaseLoadModel uni_model = new LoadModelUniform(CurrentRoofFramingLayout.GetNewId(), start, end, dead, live, roof_live);
                        CurrentRoofFramingLayout.AddLoadToLayout(uni_model);                                // add the load to dictionary
                        CurrentRoofFramingLayout.AddAppliedLoadToLayout(uni_model.Id, model_num);           // add the applied load to dictionary
                        
                        // create the connection and add it to the dictionaries and to the supporting member
                        BaseConnectionModel conn_model = new ConnectionToExternalLoad(CurrentRoofFramingLayout.GetNewId(), uni_model.Id, MathHelpers.GetMidpoint(start, end), model_num, uni_model.Id);
                        CurrentRoofFramingLayout.AddConnectionToLayout(conn_model);                         // add the connection to dictionary
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

            this.OnCommandTerminate(CurrentRoofFramingLayout);

        }

        /// <summary>
        /// The command EAFS to add a new foundation support to a point on the model.
        /// </summary>
        //        [CommandMethod("EAFS")]
        private void CreateNewFoundationSupport()
        {
            RoofFramingLayout CurrentRoofFramingLayout = this.OnCreateLayout();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

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

            this.OnCommandTerminate(CurrentRoofFramingLayout);
        }
        #endregion
    }
}
