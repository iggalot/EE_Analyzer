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
using EE_RoofFrameApplication.Commands;
// Get our AutoCAD API objects



namespace EE_RoofFrameApplication
{
    public class RoofFramerApplication
    {
        public static Document doc { get; } = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public static Database db { get; } = doc.Database;
        
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
            Autodesk.AutoCAD.Internal.Utils.AddCommand("ECRL", "ECRL", "ECRL", CommandFlags.Modal, ECRL.CreateRafterLayout);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EANB", "EANB", "EANB", CommandFlags.Modal, EANB.CreateNewSupportBeam);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("ERDA", "ERDA", "ERDA", CommandFlags.Modal, ERDA.ReloadDrawingFromFile);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EPRC", "EPRC", "EPRC", CommandFlags.Modal, EPRC.PerformReactionCalculations);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAPL", "EAPL", "EAPL", CommandFlags.Modal, EAPL.AddPointLoadToMember);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAFUL", "EAFUL", "EAFUL", CommandFlags.Modal, EAFUL.AddFullUniformLoadToLayout);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("EAFS", "EAFS", "EAFS", CommandFlags.Modal, EAFS.CreateNewFoundationSupport);
            Autodesk.AutoCAD.Internal.Utils.AddCommand("ECWA", "ECWA", "ECWA", CommandFlags.Modal, ECWA.CreateWallAssembly);

        }

        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        public static void LoadAutoCADSettings()
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
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, doc, db, 6); // magenta
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, doc, db, 140);  // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER, doc, db, 52); // gold color 
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_FDN_LAYER, doc, db, 32); // burnt orange color 
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WALL_SUPPORT_LAYER, doc, db, 44); // brown color 


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
    }
}
