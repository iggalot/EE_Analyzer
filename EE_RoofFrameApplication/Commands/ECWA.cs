using Autodesk.AutoCAD.Geometry;
using EE_RoofFramer;
using EE_RoofFramer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.MathHelpers;
using static EE_Analyzer.Utilities.LineObjects;
using System.Threading;

namespace EE_RoofFrameApplication.Commands
{
    public class ECWA : acEE_Command
    {
        public static void CreateWallAssembly()
        {
            double wall_height = 120;

            RoofFramingLayout CurrentRoofFramingLayout = acEE_Command.OnCommndExecute();           // Create our framing layout, Set up layers and linetypes and AutoCAD drawings items

            // Prompt for line input
            Point3d[] points = PromptUserforLineEndPoints(db, doc);

            if ((points == null) || points.Length < 2)
            {
                doc.Editor.WriteMessage("\nInvalid input for endpoints in CreateNewSupportBeam");
                return;
            }

            Point3d start = points[0];
            Point3d end = points[1];

            // Create the wall object
            WallModel wall_model = new WallModel(CurrentRoofFramingLayout.GetNewId(), start, end, wall_height);

            // Create top plate (beam) of wall
            SupportModel_SS_Beam top_plate = new SupportModel_SS_Beam(CurrentRoofFramingLayout.GetNewId(), start, end);

            // Create bottom plate (beam) of wall
            SupportModel_SS_Beam bottom_plate = new SupportModel_SS_Beam(CurrentRoofFramingLayout.GetNewId(), start, end);

            // Add the plate (beam) references to the wall_model
            wall_model.AddTopBeam(top_plate.Id);
            wall_model.AddBottomBeam(bottom_plate.Id);

            // Create the connections between the top beam and the bottom beam
            // -- add connection at beginning of wall to top plate
            BaseConnectionModel tc_start = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), start, top_plate.Id, wall_model.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
            CurrentRoofFramingLayout.AddConnectionToLayout(tc_start);
            wall_model.AddConnection(tc_start);
            top_plate.AddConnection(tc_start);
            // -- add connection at beginning of wall to bottom plate
            BaseConnectionModel bc_start = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), start, wall_model.Id, bottom_plate.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
            CurrentRoofFramingLayout.AddConnectionToLayout(bc_start);
            wall_model.AddConnection(bc_start);
            bottom_plate.AddConnection(bc_start);
            // -- add connection at end of wall to top plate
            BaseConnectionModel tc_end = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), end, top_plate.Id, wall_model.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
            CurrentRoofFramingLayout.AddConnectionToLayout(tc_end);
            wall_model.AddConnection(tc_end);
            top_plate.AddConnection(tc_end);
            // -- add connection at end of wall to bottom plate
            BaseConnectionModel bc_end = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), end, wall_model.Id, bottom_plate.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
            CurrentRoofFramingLayout.AddConnectionToLayout(bc_end);
            wall_model.AddConnection(bc_end);
            bottom_plate.AddConnection(bc_end);

            // -- add connection at each stud spacing of wall
            double dist_from_start = wall_model.StudSpacing;
            while (dist_from_start < wall_model.Length)
            {
                Point3d stud_pt = Point3dFromVectorOffset(start, wall_model.vDir * dist_from_start);

                // top plate connections
                BaseConnectionModel top_stud = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), stud_pt, top_plate.Id, wall_model.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
                CurrentRoofFramingLayout.AddConnectionToLayout(top_stud);
                wall_model.AddConnection(top_stud);
                top_plate.AddConnection(top_stud);

                // bottom plate connections
                BaseConnectionModel bottom_stud = new BaseConnectionModel(CurrentRoofFramingLayout.GetNewId(), stud_pt, wall_model.Id, bottom_plate.Id, (int)ConnectionTypes.CONN_TYPE_MBR_TO_MBR);
                CurrentRoofFramingLayout.AddConnectionToLayout(bottom_stud);
                wall_model.AddConnection(bottom_stud);
                bottom_plate.AddConnection(bottom_stud);

                dist_from_start += wall_model.StudSpacing;
            }

            // Add the top and bottom plates (beams) to the layout
            CurrentRoofFramingLayout.AddBeamToLayout(top_plate);
            CurrentRoofFramingLayout.AddBeamToLayout(bottom_plate);

            // Add the wall to the layout dictionary
            CurrentRoofFramingLayout.AddWallToLayout(wall_model);

            // Need to slow down the program otherwise it races through reading the data and goes straight to drawing.
            Thread.Sleep(1000);

            acEE_Command.OnCommandTerminate(CurrentRoofFramingLayout);
        }
    }
}
