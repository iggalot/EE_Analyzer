using EE_RoofFramer;
using EE_RoofFramer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.DrawObject;

namespace EE_RoofFrameApplication.Commands
{
    public class EPRC : acEE_Command
    {

        /// <summary>
        /// Command EPRC to perform the calculations
        /// </summary>
        //       [CommandMethod("EPRC")]
        public static void PerformReactionCalculations()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

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

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);
        }
    }
}
