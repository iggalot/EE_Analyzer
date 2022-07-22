using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFrameApplication
{
    public class AnalyzerMainApplication
    {

        [CommandMethod("EE_RUN")]
        public static void RunEE_Anaalyzer()
        {
            RoofFramerApplication framer_app = new RoofFramerApplication();
            framer_app.Run();
        }
    }
}
