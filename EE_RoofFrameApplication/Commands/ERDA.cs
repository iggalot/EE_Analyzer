using EE_RoofFramer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFrameApplication.Commands
{
    public class ERDA : acEE_Command
    {
        /// <summary>
        /// Command ERDA to reload and redraw the stored model information.
        /// </summary>
        //       [CommandMethod("ERDA")]
        public static void ReloadDrawingFromFile()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            // No need to reload or save on this.
        }
    }
}
