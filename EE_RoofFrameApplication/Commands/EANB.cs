using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using EE_RoofFramer;
using EE_RoofFramer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.LineObjects;

namespace EE_RoofFrameApplication.Commands
{
    /// <summary>
    /// Command EANB to add a new steel beam member and to create connections with all rafters that it crosses
    /// </summary>
    public class EANB : acEE_Command
    {
        /// <summary>
        /// The EANB command to add a new beam to the screen.
        /// </summary>
        //        [CommandMethod("EANB")]
        public static void CreateNewSupportBeam()
        {
            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            Point3d[] pt = PromptUserforLineEndPoints(db, doc);

            if ((pt == null) || pt.Length < 2)
            {
                doc.Editor.WriteMessage("\nInvalid input for endpoints in CreateNewSupportBeam");
                return;
            }

            // Create the new beam and set the XDAta object number reference
            SupportModel_SS_Beam beam = new SupportModel_SS_Beam(CurrentRoofFramingLayout.GetNewId(), pt[0], pt[1]);

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
                    CurrentRoofFramingLayout.UpdatedSingleSupportedByList(beam);        // Update the supported by list on the beam

                    kvp.Value.AddConnection(support_conn);                              // update the rafter object to indicate the support in it
                    CurrentRoofFramingLayout.UpdatedSingleSupportedByList(kvp.Value);        // Update the supported by list on the beam

                }
            }

            // Add the beam to the support beams list
            CurrentRoofFramingLayout.AddBeamToLayout(beam);

            // Need to slow down the program otherwise it races through reading the data and goes straight to drawing.
            Thread.Sleep(1000);

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);
        }
    }
}
