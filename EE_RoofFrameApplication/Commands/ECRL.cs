using EE_RoofFramer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFrameApplication.Commands
{
    /// <summary>
    /// Command to create a rafter layout from a polyline.
    /// </summary>
    public class ECRL : acEE_Command
    {
        /// <summary>
        /// Main functionality for ECRL command
        /// </summary>
        //        [CommandMethod("ECRL")]
        public static void CreateRafterLayout()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

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
                    var result = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(dialog);
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

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);
        }
    }
}
