using EE_RoofFramer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EE_RoofFrameApplication;

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

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Threading;

namespace EE_RoofFrameApplication.Commands
{
    public abstract class acEE_Command
    {
        public static Document doc { get; } = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public static Database db { get; } = doc.Database;

        public static RoofFramingLayout OnCommndExecute()
        {
            RoofFramingLayout layout = new RoofFramingLayout(db, doc);
            RoofFramerApplication.LoadAutoCADSettings();
            layout.ReadAllDataFromFiles();       // read existing data from the text files
            layout.UpdateAllSupportedByLists();  // update the support_by conditions

            Thread.Sleep(1000);
            layout.DrawAllRoofFraming();       // redraw the data now that it's read

            return layout;
        }
        public static void OnCommandTerminate(RoofFramingLayout layout)
        {
            layout.DrawAllRoofFraming();    // redraw the drawing since the data has been changed
            layout.WriteAllDataToFiles();   // save the work to file
        }


    }
}
