using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using static EE_Analyzer.Utilities.LinetypeObjects;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.PolylineObjects;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.DrawObject;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using EE_Analyzer.Models;
using EE_Analyzer.Utilities;
using System.Windows;

namespace EE_Analyzer
{
    public class FoundationLayout
    {
        private const double DEFAULT_MIN_PT_LENGTH = 120;  // Length (in inches) for which PT is not practical

        private int currentBeamNum = 0;

        private bool MODE_X_SELECTED = false;
        private bool MODE_Y_SELECTED = false;

        // A variable to handle previewing the grade beam locations
        public bool PreviewMode = true;
        public bool FirstLoad = true;
        public bool ShouldClose = false;

        private UIModes MODE_X_DIR { get; set; }
        private UIModes MODE_Y_DIR { get; set; }

        public double DEFAULT_DONT_DRAW_PT_LENGTH { get; set; }

        // Holds the primary foundation perimeter polyline object.
        public Polyline FDN_PERIMETER_POLYLINE { get; set; } = null;
        public Polyline FDN_PERIMETER_CENTERLINE_POLYLINE { get; set; } = new Polyline();

        public Polyline FDN_PERIMETER_INTERIOR_EDGE_POLYLINE { get; set; } = new Polyline();

        // Hold the bounding box for the foundation extents
        public Polyline FDN_BOUNDARY_BOX { get; set; } = new Polyline();

        // Holds the basis point for the grade beam grid
        public Point3d FDN_GRADE_BEAM_BASIS_POINT { get; set; } = new Point3d();

        // Data storage Entities
        private List<Line> BeamLines { get; set; } = new List<Line>();

        // Stores the untrimmed grade beams for the foundation
        private List<GradeBeamModel> lstInteriorGradeBeamsUntrimmed { get; set; } = new List<GradeBeamModel>();
        private List<GradeBeamModel> lstInteriorGradeBeamsTrimmed { get; set; } = new List<GradeBeamModel>();

        private List<StrandModel> lstSlabStrandsUntrimmed { get; set; } = new List<StrandModel>();
        private List<StrandModel> lstSlabStrandsTrimmed { get; set; } = new List<StrandModel>();

        private List<PierModel> lstPierModels { get; set; } = new List<PierModel>();

        public bool PiersSpecified { get; set; } = false;
        public PierShapes PierShape { get; set; } = PierShapes.PIER_UNDEFINED;
        public double PierWidth { get; set; } = 12;
        public double PierHeight { get; set; } = 12;

        #region PTI Slab Data Values

        public double[] Beam_X_Loc_Data;
        public double[] Beam_Y_Loc_Data;

        public int Beam_X_Qty { get; set; }
        public int Beam_X_Strand_Qty { get; set; }
        public int Beam_X_Slab_Strand_Qty { get; set; }

        public double Beam_X_Spacing { get; set; }
        public double Beam_X_Width { get; set; }
        public double Beam_X_Depth { get; set; }

        // TODO:  Start to install variable spacings here....

        public int Beam_Y_Qty { get; set; }
        public int Beam_Y_Strand_Qty { get; set; }
        public int Beam_Y_Slab_Strand_Qty { get; set; }

        public double Beam_Y_Spacing { get; set; }
        public double Beam_Y_Width { get; set; }
        public double Beam_Y_Depth { get; set; }


        public int Beam_X_DETAIL_QTY_1 {get; set;}
        public int Beam_X_DETAIL_QTY_2 {get; set;}
        public int Beam_X_DETAIL_QTY_3 {get; set;}
        public int Beam_X_DETAIL_QTY_4 {get; set;}
        public int Beam_X_DETAIL_QTY_5 { get; set; }
        public double Beam_X_DETAIL_SPA_1 {get; set;}
        public double Beam_X_DETAIL_SPA_2 {get; set;}
        public double Beam_X_DETAIL_SPA_3 {get; set;}
        public double Beam_X_DETAIL_SPA_4 {get; set;}
        public double Beam_X_DETAIL_SPA_5 { get; set; }
        public int Beam_Y_DETAIL_QTY_1 {get; set; }
        public int Beam_Y_DETAIL_QTY_2 {get; set; }
        public int Beam_Y_DETAIL_QTY_3 {get; set; }
        public int Beam_Y_DETAIL_QTY_4 {get; set; }
        public int Beam_Y_DETAIL_QTY_5 { get; set; }
        public double Beam_Y_DETAIL_SPA_1 {get; set; }
        public double Beam_Y_DETAIL_SPA_2 {get; set; }
        public double Beam_Y_DETAIL_SPA_3 {get; set; }
        public double Beam_Y_DETAIL_SPA_4 {get; set; }
        public double Beam_Y_DETAIL_SPA_5 { get; set; }


        /// <summary>
        /// A function to run calculations that only need to be selected or run one time per application
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        public void OnFoundationLayoutCreate()
        {
            // Get our AutoCAD API objects
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            #region Select Foundation Beam in AutoCAD
            // Selects the foundation polyline and corrects the winding order to be clockwise.

            var options = new PromptEntityOptions("\nSelect Foundation Polyline");
            options.SetRejectMessage("\nSelected object is not a polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            // Select the polyline for the foundation
            var result = edt.GetEntity(options);

            FDN_PERIMETER_POLYLINE = ProcessFoundationPerimeter(db, edt, result);

            if (FDN_PERIMETER_POLYLINE is null)
            {
                throw new System.Exception("\nInvalid foundation perimeter line selected.");
            }
            else
            {
                doc.Editor.WriteMessage("\nFoundation perimeter line selected");
            }
            #endregion

            #region Create and Draw Bounding Box
            var lstVertices = GetVertices(FDN_PERIMETER_POLYLINE);
            doc.Editor.WriteMessage("\n--Foundation perimeter has " + lstVertices.Count + " vertices.");
            FDN_BOUNDARY_BOX = CreateFoundationBoundingBox(db, edt, lstVertices);
            doc.Editor.WriteMessage("\n-- Creating foundation bounding box.");
            if (FDN_BOUNDARY_BOX is null)
            {
                throw new System.Exception("Invalid foundation boundary box created.");
            }
            #endregion

            #region Find the Insert (Basis) Point the GradeBeams
            // For the detailed spacings, use the lower left corner, otherwise use our algorithm for the optimized location
            doc.Editor.WriteMessage("\nGet grade beam insert point");
            //if (MODE_X_DIR == UIModes.MODE_X_DIR_DETAIL || MODE_X_DIR == UIModes.MODE_Y_DIR_DETAIL)
            //{
            //    // Use the algorithm to get the start point
            //    FDN_GRADE_BEAM_BASIS_POINT = FindGradeBeamInsertPoint(db, doc);
            //} else
            //{
            //    // use the lower left corner of the bounding box (index 0);
            //    FDN_GRADE_BEAM_BASIS_POINT = new Point3d(FDN_BOUNDARY_BOX.GetPoint3dAt(0).X + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Y + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Z); ;
            //}

            // use the lower left corner of the bounding box (index 0);
            FDN_GRADE_BEAM_BASIS_POINT = new Point3d(FDN_BOUNDARY_BOX.GetPoint3dAt(0).X + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Y + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Z); ;


            // Check that the basis point isn't outside of foundation polyline.  If it is, set it to the lower left corner of the boundary box.
            // TODO:  Figure out why this can happen sometime.  Possible the intersection point test is the cause?
            if ((FDN_GRADE_BEAM_BASIS_POINT.X < FDN_BOUNDARY_BOX.GetPoint2dAt(0).X) ||
                (FDN_GRADE_BEAM_BASIS_POINT.X > FDN_BOUNDARY_BOX.GetPoint2dAt(2).X) ||
                (FDN_GRADE_BEAM_BASIS_POINT.Y < FDN_BOUNDARY_BOX.GetPoint2dAt(0).Y) ||
                (FDN_GRADE_BEAM_BASIS_POINT.Y > FDN_BOUNDARY_BOX.GetPoint2dAt(2).Y))
            {
                // Set the basis point to the lower left and then offset by half the width of the perimeter beam
                FDN_GRADE_BEAM_BASIS_POINT = new Point3d(FDN_BOUNDARY_BOX.GetPoint3dAt(0).X + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Y + 0.5 * Beam_X_Width, FDN_BOUNDARY_BOX.GetPoint3dAt(0).Z);
                MessageBox.Show("Moving basis point to the lower left corner of the bounding box");
            }

            // Add a marker for this point.
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 20, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 25, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 30, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 35, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 40, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 45, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
            DrawCircle(FDN_GRADE_BEAM_BASIS_POINT, 50, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

            doc.Editor.WriteMessage("\n-Intersection of longest segments at :" + FDN_GRADE_BEAM_BASIS_POINT.X.ToString() + ", " + FDN_GRADE_BEAM_BASIS_POINT.Y.ToString() + ", " + FDN_GRADE_BEAM_BASIS_POINT.Z.ToString());

            doc.Editor.WriteMessage("\nGrade beam insert point computed succssfully");
            #endregion
        }

        #endregion

        /// <summary>
        /// Our default constructor
        /// </summary>
        public FoundationLayout()
        {
            // currently does nothing.
        }

        public void DrawFoundationDetails(
            int x_qty, double x_spa, double x_depth, double x_width,
            int y_qty, double y_spa, double y_depth, double y_width,
            int bx_strand_qty, int sx_strand_qty, int by_strand_qty, int sy_strand_qty,
            int x_spa_1_qty, int x_spa_2_qty, int x_spa_3_qty, int x_spa_4_qty, int x_spa_5_qty,
            double x_spa_1_spa, double x_spa_2_spa, double x_spa_3_spa, double x_spa_4_spa, double x_spa_5_spa,
            int y_spa_1_qty, int y_spa_2_qty, int y_spa_3_qty, int y_spa_4_qty, int y_spa_5_qty ,
            double y_spa_1_spa, double y_spa_2_spa, double y_spa_3_spa, double y_spa_4_spa, double y_spa_5_spa, 
            UIModes default_mode_x, UIModes default_mode_y,  
            bool piers_active, PierShapes pier_shape, double pier_width, double pier_height,
            bool preview_mode, bool should_close,
            double neglect_dimension = DEFAULT_MIN_PT_LENGTH)
        {
            ShouldClose = should_close;
            // If the window has been canceled dont bother doing anything else
            if (should_close)
            {
                return;
            }

            MODE_X_DIR = default_mode_x;
            MODE_Y_DIR = default_mode_y;

            PreviewMode = preview_mode;

            DEFAULT_DONT_DRAW_PT_LENGTH = neglect_dimension;

            PiersSpecified = piers_active;
            PierShape = pier_shape;
            PierWidth = pier_width;
            PierHeight = pier_height;

            Beam_X_Spacing = x_spa;  // spacing between horizontal beams
            Beam_X_Width = x_width;  // horizontal beam width
            Beam_X_Depth = x_depth;  // horizontal beam depth
            Beam_X_Qty = x_qty;         // horizontal beam qty
            Beam_X_Strand_Qty = bx_strand_qty;  // number of strands in each x-direction beam
            Beam_X_Slab_Strand_Qty = sx_strand_qty;  // number of strands in x-direction slab

            Beam_Y_Spacing = y_spa;  // spacing between vertical beams
            Beam_Y_Width = y_width;  // vertical beam width
            Beam_Y_Depth = y_depth;  // vertical beam depth
            Beam_Y_Qty = y_qty;         // vertical beam qty 
            Beam_Y_Strand_Qty = by_strand_qty;  // number of strands in each y-direction beam
            Beam_Y_Slab_Strand_Qty = sy_strand_qty;  // number of strands in y-direction slab

            Beam_X_DETAIL_QTY_1 = x_spa_1_qty;
            Beam_X_DETAIL_QTY_2 = x_spa_2_qty;
            Beam_X_DETAIL_QTY_3 = x_spa_3_qty;
            Beam_X_DETAIL_QTY_4 = x_spa_4_qty;
            Beam_X_DETAIL_QTY_5 = x_spa_5_qty;

            Beam_X_DETAIL_SPA_1 = x_spa_1_spa;
            Beam_X_DETAIL_SPA_2 = x_spa_2_spa;
            Beam_X_DETAIL_SPA_3 = x_spa_3_spa;
            Beam_X_DETAIL_SPA_4 = x_spa_4_spa;
            Beam_X_DETAIL_SPA_5 = x_spa_5_spa;

            Beam_Y_DETAIL_QTY_1 = y_spa_1_qty;
            Beam_Y_DETAIL_QTY_2 = y_spa_2_qty;
            Beam_Y_DETAIL_QTY_3 = y_spa_3_qty;
            Beam_Y_DETAIL_QTY_4 = y_spa_4_qty;
            Beam_Y_DETAIL_QTY_5 = y_spa_5_qty;

            Beam_Y_DETAIL_SPA_1 = y_spa_1_spa;
            Beam_Y_DETAIL_SPA_2 = y_spa_2_spa;
            Beam_Y_DETAIL_SPA_3 = y_spa_3_spa;
            Beam_Y_DETAIL_SPA_4 = y_spa_4_spa;
            Beam_Y_DETAIL_SPA_5 = y_spa_5_spa;


            double circle_radius = Beam_X_Spacing * 0.1; // for marking the intersections of beams and strands with the foundation polyline

            // Get our AutoCAD API objects
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;





            #region Determine Beam Spacings
            double min_x = FDN_BOUNDARY_BOX.GetPoint3dAt(0).X;
            double min_y = FDN_BOUNDARY_BOX.GetPoint3dAt(0).Y;
            double max_x = FDN_BOUNDARY_BOX.GetPoint3dAt(2).X;
            double max_y = FDN_BOUNDARY_BOX.GetPoint3dAt(2).Y;

            // Create the x-beam spacing list
            if (MODE_X_DIR == UIModes.MODE_X_DIR_QTY)
            {
                double x_dir_spa = (max_y - min_y - Beam_X_Width) / (x_qty - 1);
                Beam_X_Loc_Data = new double[x_qty];

                double curr_spa = 0;
                for (int i = 0; i < x_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_dir_spa;
                }
                MODE_X_SELECTED = true;
            }
            else if (MODE_X_DIR == UIModes.MODE_X_DIR_SPA)
            {
                int x_count = (int)Math.Ceiling(1 + (max_y - min_y - Beam_X_Width) / x_spa);
                double x_max_spa = (max_y - min_y) / x_count;
                Beam_X_Loc_Data = new double[x_count];

                double curr_spa = 0;
                for (int i = 0; i < x_count; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_max_spa;
                }
                MODE_X_SELECTED = true;

            }
            else if (MODE_X_DIR == UIModes.MODE_X_DIR_DETAIL)
            {
                int count = x_spa_1_qty + x_spa_2_qty + x_spa_3_qty + x_spa_4_qty + x_spa_5_qty;
                Beam_X_Loc_Data = new double[count];
                int temp_count = 0;

                double curr_spa = 0;
                for (int i = 0; i < x_spa_1_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_spa_1_spa;
                }
                temp_count += x_spa_1_qty;
                for (int i = temp_count; i < temp_count + x_spa_2_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_spa_2_spa;
                }
                temp_count += x_spa_2_qty;
                for (int i = temp_count; i < temp_count + x_spa_3_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_spa_3_spa;
                }
                temp_count += x_spa_3_qty;
                for (int i = temp_count; i < temp_count + x_spa_4_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_spa_4_spa;
                }
                temp_count += x_spa_4_qty;
                for (int i = temp_count; i < temp_count + x_spa_5_qty; i++)
                {
                    Beam_X_Loc_Data[i] = curr_spa;
                    curr_spa += x_spa_5_spa;
                }
                MODE_X_SELECTED = true;
            }

            // Create the y-beam spacing list
            if (MODE_Y_DIR == UIModes.MODE_Y_DIR_QTY)
            {
                double y_dir_spa = (max_x - min_x - Beam_Y_Width) / (y_qty - 1);
                Beam_Y_Loc_Data = new double[y_qty];

                double curr_spa = 0;
                for (int i = 0; i < y_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_dir_spa;
                }
                MODE_Y_SELECTED = true;
            }
            else if (MODE_Y_DIR == UIModes.MODE_Y_DIR_SPA)
            {
                int y_count = (int)Math.Ceiling(1 + (max_x - min_x - Beam_Y_Width) / y_spa);
                double y_max_spa = (max_x - min_x) / y_count;
                Beam_Y_Loc_Data = new double[y_count];

                double curr_spa = 0;
                for (int i = 0; i < y_count; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_max_spa;
                }
                MODE_Y_SELECTED = true;

            }
            else if (MODE_Y_DIR == UIModes.MODE_Y_DIR_DETAIL)
            {
                int count = y_spa_1_qty + y_spa_2_qty + y_spa_3_qty + y_spa_4_qty + y_spa_5_qty;
                Beam_Y_Loc_Data = new double[count];
                int temp_count = 0;

                double curr_spa = 0;
                for (int i = 0; i < y_spa_1_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_spa_1_spa;
                }
                temp_count += y_spa_1_qty;
                for (int i = temp_count; i < temp_count + y_spa_2_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_spa_2_spa;
                }
                temp_count += y_spa_2_qty;
                for (int i = temp_count; i < temp_count + y_spa_3_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_spa_3_spa;
                }
                temp_count += y_spa_3_qty;
                for (int i = temp_count; i < temp_count + y_spa_4_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_spa_4_spa;
                }
                temp_count += y_spa_4_qty;
                for (int i = temp_count; i < temp_count + y_spa_5_qty; i++)
                {
                    Beam_Y_Loc_Data[i] = curr_spa;
                    curr_spa += y_spa_5_spa;
                }
                MODE_Y_SELECTED = true;
            }

            if (MODE_X_SELECTED == false || MODE_Y_SELECTED == false)
            {
                doc.Editor.WriteMessage("X and Y directions must be selected.");
                return;
            }
            #endregion


            // If we are in preview mode we will just draw centerlines as a marker.
            if (PreviewMode is true)
            {
                DoPreviewMode(db, doc, true);
                DoPreviewMode(db, doc, false);


                //using (Transaction trans = db.TransactionManager.StartTransaction())
                //{
                //    // Force a redraw of the screen?
                //    doc.TransactionManager.EnableGraphicsFlush(true);
                //    doc.TransactionManager.QueueForGraphicsFlush();
                //    Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
                //    trans.Commit();
                //}
                return;
            }

            // Clear our temporary layer
            DeleteAllObjectsOnLayer(EE_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);


            #region Draw Parameter Data to Drawing
            WritefoundationData(x_qty, x_spa, x_depth, x_width, y_qty, y_spa, y_depth, y_width,
                x_spa_1_qty, x_spa_2_qty, x_spa_3_qty, x_spa_4_qty, x_spa_5_qty, x_spa_1_spa, x_spa_2_spa, x_spa_3_spa, x_spa_4_spa, x_spa_5_spa,
                y_spa_1_qty, y_spa_2_qty, y_spa_3_qty, y_spa_4_qty, y_spa_5_qty, y_spa_1_spa, y_spa_2_spa, y_spa_3_spa, y_spa_4_spa, y_spa_5_spa,
                default_mode_x, default_mode_y, neglect_dimension, doc, db);
            #endregion

            #region Draw Foundation Perimeter Beam
            // TODO:  Return a GradeBeam object for the foundation perimeter?
            //////////////////////////////////////////////////////
            // Draw the perimeter beam -- perimeter line will be continuous, inner edge line will be hidden
            // both will be assigned to the boundary perimeter layer
            //////////////////////////////////////////////////////
            doc.Editor.WriteMessage("\nDrawing foundation perimeter beam");
            DrawFoundationPerimeterBeam(db, doc, Beam_X_Width);
            doc.Editor.WriteMessage("\nDrawing foundation perimeter beam completed");
            #endregion

            #region Draw Untrimmed Grade Beams and Beam Strands
            doc.Editor.WriteMessage("\nDrawing untrimmed interior grade beams");

            // draw horizontal and vertical grade beams
            CreateUntrimmedGradeBeams(db, doc, true);  // for horizontal beams
            CreateUntrimmedGradeBeams(db, doc, false); // for vertical beams

            doc.Editor.WriteMessage("\n-- Completed drawing Interior grade beams. " + lstInteriorGradeBeamsUntrimmed.Count + " grade beams created.");

            #endregion

            #region Trim Grade Beam and Beam Strand Lines
            doc.Editor.WriteMessage("\nDrawing " + lstInteriorGradeBeamsUntrimmed.Count + " trimmed interior grade beams");

            // draw the trimmed horizontal and vertical grade beams
            CreateTrimmedGradeBeams(db, doc, lstInteriorGradeBeamsUntrimmed);
            doc.Editor.WriteMessage("\n-- Completed drawing trimmed grade beams. " + lstInteriorGradeBeamsTrimmed.Count + " grade beams created.");

            #endregion

            #region Draw Untrimmed Slab Strands

            doc.Editor.WriteMessage("\nDrawing untrimmed slab strands beams");
            CreateUntrimmedSlabStrands(db, doc, FDN_GRADE_BEAM_BASIS_POINT, true);  // for horizontal beams
            CreateUntrimmedSlabStrands(db, doc, FDN_GRADE_BEAM_BASIS_POINT, false); // for vertical beams
            doc.Editor.WriteMessage("\n-- Completed drawing untrimmed slab strands. " + lstSlabStrandsUntrimmed.Count + " untrimmed slab strands created.");

            #endregion

            #region Trim Slab Strands
            doc.Editor.WriteMessage("\nDrawing " + lstInteriorGradeBeamsUntrimmed.Count + " trimmed slab strands");
            CreateTrimmedSlabStrands(db, doc, lstSlabStrandsUntrimmed);
            doc.Editor.WriteMessage("\n-- Completed drawing trimmed slab strands. " + lstSlabStrandsTrimmed.Count + " trimmed slab strands created.");

            #endregion

            #region Draw Piers
            if (PiersSpecified)
            {
                doc.Editor.WriteMessage("\nDrawing piers");
                CreatePiers(db, doc, PierShape, PierWidth, PierHeight);
                doc.Editor.WriteMessage("\n-- Completed piers");

            }

            #endregion

            #region Bill of Materials
            // compute concrete volumes
            doc.Editor.WriteMessage("\nDrawing bill of materials");
            CreateBillOfMaterials(db, doc);
            doc.Editor.WriteMessage("\n-- Completed bill of materials");
            #endregion

            //// compute strand quantities
            //#endregion

            //#region Section Details
            //#endregion

            //#region Additional Steel
            //#endregion
        }

        /// <summary>
        /// Our preview mode functionality.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="isHorizontal"></param>
        /// <exception cref="System.Exception"></exception>
        private void DoPreviewMode(Database db, Document doc, bool isHorizontal)
        {
            // First delete all of the other preview lines from the temporary graphics layer.
            DeleteAllObjectsOnLayer(EE_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

            // Then create the new lines on the drawing.

            // retrieve the bounding box
            var bbox_points = GetVertices(FDN_BOUNDARY_BOX);

            if (bbox_points is null || (bbox_points.Count != 4))
            {
                throw new System.Exception("\nFoundation bounding box must have four points");
            }

            if (isHorizontal is true)
            {
                // For the horizontal beams
                // grade beams to the upper boundary box horizontal edge
                for (int i = 0; i < Beam_X_Loc_Data.Length; i++)
                {
                    double y_coord = FDN_GRADE_BEAM_BASIS_POINT.Y + Beam_X_Loc_Data[i];

                    // If our spacing pattern has gone beyond the beam extents
                    if (y_coord > bbox_points[1].Y - 0.5 * Beam_X_Width)
                    {
                        break;
                    }

                    Point3d p1 = new Point3d(bbox_points[0].X, y_coord, 0);
                    Point3d p2 = new Point3d(bbox_points[3].X, y_coord, 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping horizontal grade beam here.");
                        continue;
                    }
                    // reverse the points so the smallest X is on the left
                    if (p1.X > p2.X)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    // draw our line object
                    DrawCircle(p1, 40, EE_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

                    //DrawLine(p1, p2, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                }
            }
            else
            {
                // grade beams to the upper boundary box horizontal edge
                for (int i = 0; i < Beam_Y_Loc_Data.Length; i++)
                {
                    double x_coord = FDN_GRADE_BEAM_BASIS_POINT.X + Beam_Y_Loc_Data[i];

                    // If our spacing pattern has gone beyond the beam extents
                    if (x_coord > bbox_points[3].X - 0.5 * Beam_X_Width)
                    {
                        break;
                    }

                    Point3d p1 = new Point3d(x_coord, bbox_points[0].Y, 0);
                    Point3d p2 = new Point3d(x_coord, bbox_points[1].Y, 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping vertical grade beam here.");
                        continue;
                    }
                    // reverse the points so the smallest Y is on the bottom
                    if (p1.Y > p2.Y)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    // draw our line object
                    DrawCircle(p2, 40, EE_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

                    //DrawLine(p1, p2, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                }
            }


        }

        private void CreatePiers(Database db, Document doc,  PierShapes shape, double width, double height)
        {
            double tol = 0.001;
            int count = 0;
            for (int i = 0; i < lstInteriorGradeBeamsTrimmed.Count; i++)
            {
                for (int j = i; j < lstInteriorGradeBeamsTrimmed.Count; j++)
                {
                    IntersectPointData p1_data = FindPointOfIntersectLines_FromPoint3d(lstInteriorGradeBeamsTrimmed[i].CL_Pt_A,
                        lstInteriorGradeBeamsTrimmed[i].CL_Pt_B,
                        lstInteriorGradeBeamsTrimmed[j].CL_Pt_A,
                        lstInteriorGradeBeamsTrimmed[j].CL_Pt_B);

                    if(p1_data.Point.X == double.MaxValue || p1_data.Point.Y == double.MaxValue)
                    {
                        continue;
                    }

                    // TODO:  skip points that don't occur at physical intersection of grade beams
                    if(p1_data.isWithinSegment)
                    {
                        count++;
                        PierModel pm = new PierModel(p1_data.Point, shape, width, height, count);
                        lstPierModels.Add(pm);
                    }
                }
            }

            foreach (var item in lstPierModels)
            {
                item.AddToAutoCADDatabase(db, doc);
            }
        }

        /// <summary>
        /// Writes the design data to the drawing file.
        /// </summary>
        /// <returns></returns>
        private string WritefoundationData(int x_qty, double x_spa, double x_depth, double x_width, int y_qty, double y_spa, double y_depth, double y_width, 
            int x_spa_1_qty, int x_spa_2_qty, int x_spa_3_qty, int x_spa_4_qty, int x_spa_5_qty, double x_spa_1_spa, double x_spa_2_spa, double x_spa_3_spa, double x_spa_4_spa, double x_spa_5_spa, 
            int y_spa_1_qty, int y_spa_2_qty, int y_spa_3_qty, int y_spa_4_qty, int y_spa_5_qty, double y_spa_1_spa, double y_spa_2_spa, double y_spa_3_spa, double y_spa_4_spa, double y_spa_5_spa, 
            UIModes default_mode_x, UIModes default_mode_y, double neglect_dimension, Document doc, Database db)
        {
            string str = "";

            // X-dir beam data
            str += "\nX:   Depth: " + x_depth + "    Width: " + x_width + "   Beam Strands: " + Beam_X_Strand_Qty + "    Slab Strands: " + Beam_X_Slab_Strand_Qty;
            str += "\nUI Mode: ";
            switch (default_mode_x)
            {
                case UIModes.MODE_X_DIR_UNDEFINED:
                    str += "Undefined";
                    break;
                case UIModes.MODE_X_DIR_SPA:
                    str += "Max. Spacing: " + x_spa;
                    break;
                case UIModes.MODE_X_DIR_QTY:
                    str += "Quantity: " + x_qty;
                    break;
                case UIModes.MODE_X_DIR_DETAIL:
                    str += "   Manual:     1.  " + x_spa_1_qty + " @ " + x_spa_1_spa;
                    str += "   Manual:     2.  " + x_spa_2_qty + " @ " + x_spa_2_spa;
                    str += "   Manual:     3.  " + x_spa_3_qty + " @ " + x_spa_3_spa;
                    str += "   Manual:     4.  " + x_spa_4_qty + " @ " + x_spa_4_spa;
                    str += "   Manual:     5.  " + x_spa_5_qty + " @ " + x_spa_5_spa;
                    break;
                default:
                    str += "Unknown mode";
                    break;
            }

            // Y-dir beam data
            str += "\nY:   Depth: " + y_depth + "    Width: " + y_width + "   Beam Strands: " + Beam_Y_Strand_Qty + "    Slab Strands: " + Beam_Y_Slab_Strand_Qty;
            str += "\nUI Mode: ";
            switch (default_mode_y)
            {
                case UIModes.MODE_Y_DIR_UNDEFINED:
                    str += "Undefined";
                    break;
                case UIModes.MODE_Y_DIR_SPA:
                    str += "Max. Spacing: " + y_spa;
                    break;
                case UIModes.MODE_Y_DIR_QTY:
                    str += "Quantity: " + y_qty;
                    break;
                case UIModes.MODE_Y_DIR_DETAIL:
                    str += "  Manual:     1.  " + y_spa_1_qty + " @ " + y_spa_1_spa;
                    str += "  Manual:     2.  " + y_spa_2_qty + " @ " + y_spa_2_spa;
                    str += "  Manual:     3.  " + y_spa_3_qty + " @ " + y_spa_3_spa;
                    str += "  Manual:     4.  " + y_spa_4_qty + " @ " + y_spa_4_spa;
                    str += "  Manual:     5.  " + y_spa_5_qty + " @ " + y_spa_5_spa;
                    break;
                default:
                    str += "Unknown mode";
                    break;
            }
            str += "\nNeglect grade beam length: " + neglect_dimension;

            DrawMtext(db, doc, FDN_BOUNDARY_BOX.GetPoint3dAt(0), str, 5, EE_Settings.DEFAULT_FDN_TEXTS_LAYER, 0);
            return str;
        }

        /// <summary>
        /// Creates a crude bill of materials for the strand info.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        private void CreateBillOfMaterials(Database db, Document doc)
        {
            Point3d base_pt = FDN_BOUNDARY_BOX.GetPoint3dAt(2);

            // display slab strand info
            int count = 1;
            double total_length = 0.0;
            foreach (var item in lstSlabStrandsTrimmed)
            {
                // retrieve the strand label
                string str = item.Label;
                int index = str.IndexOf('x'); 
                string label_str = str.Substring(index + 1);  // display everything after the 'x'

                // Draw the line of text for the BOM
                // first write the label
                Point3d pt1 = new Point3d(base_pt.X + 10.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE,
                                            base_pt.Y - count * 2.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, 0);
                DrawObject.DrawMtext(db, doc, pt1, label_str, EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER);

                // Draw qty
                Point3d pt2 = new Point3d(pt1.X + 8 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, pt1.Y, 0);
                DrawObject.DrawMtext(db, doc, pt2, item.Qty.ToString(), EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER); ;

                // Draw length
                Point3d pt3 = new Point3d(pt2.X + 4 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, pt2.Y, 0);
                DrawObject.DrawMtext(db, doc, pt3, (Math.Ceiling(item.Length / 12.0)).ToString(), EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER);

                total_length += Math.Ceiling(item.Length) * item.Qty;
                count++;
            }

            // do the same for beam strands
            foreach (var item in lstInteriorGradeBeamsTrimmed)
            {
                // retrieve the strand label
                string str = item.StrandInfo.Label;
                int index = str.IndexOf('x');
                string label_str = str.Substring(index + 1);

                // Draw label
                Point3d pt1 = new Point3d(base_pt.X + 10.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE,
                                            base_pt.Y - count * 2.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, 0);
                DrawObject.DrawMtext(db, doc, pt1, label_str, EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER);

                // Draw qty
                Point3d pt2 = new Point3d(pt1.X + 8 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, pt1.Y, 0);
                DrawObject.DrawMtext(db, doc, pt2, item.StrandInfo.Qty.ToString(), EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER); ;

                // Draw length
                Point3d pt3 = new Point3d(pt2.X + 4 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, pt2.Y, 0);
                DrawObject.DrawMtext(db, doc, pt3, (Math.Ceiling(item.StrandInfo.Length / 12.0)).ToString(), EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER);

                total_length += Math.Ceiling(item.StrandInfo.Length) * item.StrandInfo.Qty;

                count++;
            }

            Point3d total_str_pt1 = new Point3d(base_pt.X + 10.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE,
                                            base_pt.Y - count * 2.0 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, 0);
            DrawObject.DrawMtext(db, doc, total_str_pt1, "TOTAL LENGTH", EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER);

            // Draw qty
            Point3d total_str_pt2 = new Point3d(total_str_pt1.X + 15 * EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, total_str_pt1.Y, 0);
            DrawObject.DrawMtext(db, doc, total_str_pt2, Math.Ceiling(total_length/12).ToString(), EE_Settings.DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE, EE_Settings.DEFAULT_FDN_TEXTS_LAYER); ;
        }

        /// <summary>
        /// Algorithm to trim untrimmed slab strands to the FDN_PERIMETER polyline.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="list">list of untrimmed <see cref="StrandModel"/> to be trimmed</param>
        private void CreateTrimmedSlabStrands(Database db, Document doc, List<StrandModel> list)
        {
            //int numVerts_outer = FDN_PERIMETER_POLYLINE.NumberOfVertices;

            /////////////////////////////////////////////////////////////////////
            /////  Get the trimmed grade beam intersection points with the FDN_PERIMETER_INTERIOR_EDGE_POLYLINE
            /////////////////////////////////////////////////////////////////////
            //// Create new models for each of the untrimmed grade beam models
            //foreach (StrandModel untr_strand in list)
            //{
            //    // Get the untrimmed end points of the beam centerline
            //    Point3d b1 = untr_strand.Centerline.StartPoint;
            //    Point3d b2 = untr_strand.Centerline.EndPoint;

            //    Point3d[] sorted_strand_points = null;

            //    try
            //    {
            //        // Get the intersection for the trimmed grade beam with the inner edge polyline
            //        sorted_strand_points = TrimAndSortIntersectionPoints(b1, b2, FDN_PERIMETER_POLYLINE);
            //        if (sorted_strand_points != null)
            //        {
            //            doc.Editor.WriteMessage("\n--Found " + sorted_strand_points.Length.ToString() + " strand intersection points.");
            //        }
            //        else
            //        {
            //            // Null sorted points list returned, so skip this beam and continue
            //            continue;
            //        }
            //    }
            //    catch
            //    {
            //        doc.Editor.WriteMessage("\n-Error finding trimmed slab strand points.");
            //    }

            //    for (int j = 0; j < sorted_strand_points.Length - 1; j = j + 2)
            //    {
            //        try
            //        {
            //            // Mark the intersection points for the beam centerline
            //            DrawCircle(sorted_strand_points[j], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_STRAND_ANNOTATION_LAYER);
            //            DrawCircle(sorted_strand_points[j + 1], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_STRAND_ANNOTATION_LAYER);

            //            // check if the grade beam is long enough for PT
            //            if (MathHelpers.Distance3DBetween(sorted_strand_points[j], sorted_strand_points[j + 1]) > DEFAULT_DONT_DRAW_PT_LENGTH)
            //            {
            //                StrandModel strand = new StrandModel(sorted_strand_points[j], sorted_strand_points[j + 1], 1, false, true);
            //                lstSlabStrandsTrimmed.Add(strand);
            //            }
            //        }
            //        catch (System.Exception e)
            //        {
            //            doc.Editor.WriteMessage("\nError creating trimmed slab strand at " + sorted_strand_points[j].X + ", " + sorted_strand_points[j + 1].Y);
            //            DrawCircle(sorted_strand_points[j], 40, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
            //            DrawCircle(sorted_strand_points[j], 50, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
            //            DrawCircle(sorted_strand_points[j], 60, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
            //        }
            //    }
            //}

            //// Now add the grade beam entities to the drawing
            //foreach (StrandModel strand in lstSlabStrandsTrimmed)
            //{
            //    try
            //    {
            //        strand.AddToAutoCADDatabase(db, doc);
            //    }
            //    catch (System.Exception e)
            //    {
            //        doc.Editor.WriteMessage("\nError adding trimmed slab strand to AutoCAD database: " + e.Message);
            //    }
            //}
        }

        /// <summary>
        /// Algorithm to create untrimmed slab strands to the FDN_BOUNDARY_BOX polyline
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="basis"></param>
        /// <param name="isHorizontal"></param>
        /// <exception cref="System.Exception"></exception>
        private void CreateUntrimmedSlabStrands(Database db, Document doc, Point3d basis, bool isHorizontal)
        {
            double width, spacing, depth;
            // retrieve the bounding box
            var bbox_points = GetVertices(FDN_BOUNDARY_BOX);

            if (bbox_points is null || (bbox_points.Count != 4))
            {
                throw new System.Exception("\nFoundation bounding box must have four points");
            }

            if (isHorizontal is true)
            {
                int num_strands = Beam_X_Slab_Strand_Qty + 1;  // one added since the first spacing is half a spacing from bottom edge of boundaing box

                spacing = (FDN_BOUNDARY_BOX.GetPoint3dAt(1).Y-FDN_BOUNDARY_BOX.GetPoint3dAt(0).Y) / num_strands;

                // grade beams to the upper boundary box horizontal edge
                int count = 0;
                while (bbox_points[0].Y + 0.5 * spacing + (count * spacing) < bbox_points[1].Y)
                {

                    Point3d p1 = new Point3d(bbox_points[0].X, bbox_points[0].Y + 0.5 * spacing + (count * spacing), 0);
                    Point3d p2 = new Point3d(bbox_points[3].X, bbox_points[0].Y + 0.5 * spacing + (count * spacing), 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nBeam line points are the same.  Skippingslab strand here.");
                        continue;
                    }
                    // reverse the points so the smallest X is on the left
                    if (p1.X > p2.X)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    StrandModel strand = new StrandModel(p1, p2, 1, false, false);
                    lstSlabStrandsUntrimmed.Add(strand);

                    count++;
                }
            }
            else
            {
                // for vertical beams
                int num_strands = Beam_Y_Slab_Strand_Qty + 1;

                spacing = (FDN_BOUNDARY_BOX.GetPoint3dAt(3).X - FDN_BOUNDARY_BOX.GetPoint3dAt(0).X) / num_strands;


                int count = 0;
                while (bbox_points[0].X + 0.5 * spacing + (count * spacing) < bbox_points[3].X)
                {
                    Point3d p1 = new Point3d(bbox_points[0].X + 0.5 * spacing + (count * spacing), bbox_points[0].Y, 0);
                    Point3d p2 = new Point3d(bbox_points[0].X + 0.5 * spacing + (count * spacing), bbox_points[1].Y, 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nStrand line end points are the same.  Skipping strand here.");
                        continue;
                    }
                    // reverse the points so the smallest Y is on the bottom
                    if (p1.Y > p2.Y)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    StrandModel strand = new StrandModel(p1, p2, 1, false, false);
                    lstSlabStrandsUntrimmed.Add(strand);

                    count++;
                }
            }

            // Now add the grade beam entities to the drawing
            foreach (StrandModel strand in lstSlabStrandsUntrimmed)
            {
                strand.AddToAutoCADDatabase(db, doc);
            }
        }

        /// <summary>
        /// Algorithm to find the insert point for drawing the grade beam grid.  
        /// Currently finds the intersection of the longest line segments. Default will be the lower left corner of the bounding box
        /// Other options can be added for different decision making here.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        private Point3d FindGradeBeamInsertPoint(Database db, Document doc)
        {
            if (FDN_BOUNDARY_BOX == null)
            {
                throw new System.Exception("\nInvalid boundary box polyline in FindGradeBeamInsertPoint()");
            }

            Point3d lower_left = FDN_BOUNDARY_BOX.GetPoint3dAt(0);
            Point3d basis_pt = new Point3d();

            // if something goes wrong...
            Point3d default_basis_pt = new Point3d(lower_left.X + 0.5 * Beam_X_Width, lower_left.Y + 0.5 * Beam_X_Width, lower_left.Z);

            try
            {
                // Find the intersection of the longest edges of the perimeter beam in both vertical and horizontal direction
                // and use that point as our basis point for drawing the interior gridlines.
                // Finds the longest horizontal segement on the polyline
                Point3d[] longestSegmentPoints = FindTwoLongestNonParallelSegmentsOnPolyline(FDN_PERIMETER_CENTERLINE_POLYLINE);

                // if there aren't at least two points, return the lower left of the bounding box (offset by the width of the beam).
                if(longestSegmentPoints.Length != 4)
                {
                    doc.Editor.WriteMessage("Invalid number of points for polyline segments."); 
                    basis_pt = default_basis_pt;
                }

                IntersectPointData intersectData = FindPointOfIntersectLines_FromPoint3d(
                    longestSegmentPoints[0], longestSegmentPoints[1],
                    longestSegmentPoints[2], longestSegmentPoints[3]);

                Point3d intPt = intersectData.Point;

                //DrawCircle(longestSegmentPoints[0], 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                //DrawCircle(longestSegmentPoints[1], 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                //DrawCircle(longestSegmentPoints[2], 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                //DrawCircle(longestSegmentPoints[3], 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

                if (intPt != null)
                {
                    //MessageBox.Show("Found a basis point");
                    basis_pt = intPt;
                }
                else
                {
                    //MessageBox.Show("Basis point was null.  Using default.");
                    basis_pt = default_basis_pt;
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\nError encountered finding the grade beam grid insertion point.  Using the lower left instead.: " + ex.Message);
                basis_pt = default_basis_pt;
            }

            return basis_pt;
        }

        /// <summary>
        /// Draws the interior grade beam based on the basis point and the foundation boundary box.
        /// These lines will be trimmed to the perimeter beam in a subsequent step.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="basis">The basis point for the grade beam grid as <see cref="Point3d"/></param>
        /// <param name="isHorizontal"></param>
        /// <exception cref="System.Exception"></exception>
        private void CreateUntrimmedGradeBeams(Database db, Document doc, bool isHorizontal)
        {
            // retrieve the bounding box
            var bbox_points = GetVertices(FDN_BOUNDARY_BOX);

            if (bbox_points is null || (bbox_points.Count != 4))
            {
                throw new System.Exception("\nFoundation bounding box must have four points");
            }

            if (isHorizontal is true)
            {
                // For the horizontal beams
                // grade beams to the upper boundary box horizontal edge
                for (int i = 0; i < Beam_X_Loc_Data.Length; i++)
                {
                    double y_coord = FDN_GRADE_BEAM_BASIS_POINT.Y + Beam_X_Loc_Data[i];

                    // If our spacing pattern has gone beyond the beam extents
                    if (y_coord > bbox_points[1].Y - 0.5 * Beam_X_Width)
                    {
                        break;
                    }

                    Point3d p1 = new Point3d(bbox_points[0].X, y_coord, 0);
                    Point3d p2 = new Point3d(bbox_points[3].X, y_coord, 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping horizontal grade beam here.");
                        continue;
                    }
                    // reverse the points so the smallest X is on the left
                    if (p1.X > p2.X)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    // Make our grade beam model object
                    GradeBeamModel beam = new GradeBeamModel(p1, p2, Beam_X_Strand_Qty, false, true, currentBeamNum, Beam_X_Width, Beam_X_Depth);
                    currentBeamNum++;
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                }
            } else
            {
                // grade beams to the upper boundary box horizontal edge
                for (int i = 0; i < Beam_Y_Loc_Data.Length; i++)
                {
                    double x_coord = FDN_GRADE_BEAM_BASIS_POINT.X + Beam_Y_Loc_Data[i];

                    // If our spacing pattern has gone beyond the beam extents
                    if (x_coord > bbox_points[3].X - 0.5 * Beam_X_Width)
                    {
                        break;
                    }

                    Point3d p1 = new Point3d(x_coord, bbox_points[0].Y, 0);
                    Point3d p2 = new Point3d(x_coord, bbox_points[1].Y, 0);

                    if (p1 == p2)
                    {
                        doc.Editor.WriteMessage("\nBeam line points are the same.  Skipping vertical grade beam here.");
                        continue;
                    }
                    // reverse the points so the smallest Y is on the bottom
                    if (p1.Y > p2.Y)
                    {
                        Point3d temp = p1;
                        p1 = p2;
                        p2 = temp;
                    }

                    // Make our grade beam model object
                    GradeBeamModel beam = new GradeBeamModel(p1, p2, Beam_Y_Strand_Qty, false, false, currentBeamNum, Beam_Y_Width, Beam_Y_Depth);
                    currentBeamNum++;
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                }
            }
          
            // Now add the grade beam entities to the drawing
            foreach (GradeBeamModel beam in lstInteriorGradeBeamsUntrimmed)
            {
                beam.AddToAutoCADDatabase(db, doc);
            }
        }

        /// <summary>
        /// Draws the interior grade beam based on the basis point and the foundation boundary box.
        /// These lines will be trimmed to the perimeter beam in a subsequent step.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <exception cref="System.Exception"></exception>
        private void CreateTrimmedGradeBeams(Database db, Document doc, List<GradeBeamModel> list)
        {
            foreach (GradeBeamModel untr_beam in list)
            {
                double width = untr_beam.Width;
                double depth = untr_beam.Depth;

                // Get the untrimmed end points of the beam centerline
                Point3d b1 = untr_beam.CL_Pt_A;
                Point3d b2 = untr_beam.CL_Pt_B;

                List<Point3d> lst_grade_beam_points = null;
                List<Point3d> lst_grade_beam_points_edge1 = null;
                List<Point3d> lst_grade_beam_points_edge2 = null;

                List<Point3d> lst_strand_points = null;

                Point3d[] sorted_grade_beam_points = null;
                Point3d[] sorted_grade_beam_points_edge1 = null;
                Point3d[] sorted_grade_beam_points_edge2 = null;

                Point3d[] sorted_strand_points = null;

                try
                {
                    // Get the intersection for the trimmed grade beam centerline and edges with the inner edge polyline
                    lst_grade_beam_points = FindPolylineIntersectionPoints(new Line(untr_beam.CL_Pt_A, untr_beam.CL_Pt_B), FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);
                    lst_grade_beam_points_edge1 = FindPolylineIntersectionPoints(untr_beam.Edge1, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);
                    lst_grade_beam_points_edge2 = FindPolylineIntersectionPoints(untr_beam.Edge2, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);

                    // if no points in the intersection list, skip to the next grade beam
                    if (lst_grade_beam_points == null || lst_grade_beam_points_edge1 == null || lst_grade_beam_points_edge2 == null)
                    {
                        continue;
                    }

                    // TODO::
                    // Figure out the logic here to draw a grade beam when one edge has more intersection points than the other -- results when close to boundary
                    // If there are less than two points on the intersection of the center line, skip the grade beam altogether
                    if (lst_grade_beam_points.Count < 2)
                    { 
                        doc.Editor.WriteMessage("\n--Only " + lst_grade_beam_points.Count + " intersection points found - no grade beam possible for grade beam between ("
                            + "\n" + b1.X.ToString() + "," + b1.Y.ToString() + ") and \n("
                            + b2.X.ToString() + "," + b2.Y.ToString() + ") -- skipping grade beam");
                        continue;
                    }

                    // Now determine how many grade beams we can make.
                    // since its possible that the lst_grade_beam points are unequal.
                    int num_cl_pts = lst_grade_beam_points.Count;
                    int num_e1_pts = lst_grade_beam_points_edge1.Count;
                    int num_e2_pts = lst_grade_beam_points_edge2.Count;

                    int smallest_pt_count = Math.Min(Math.Min(num_cl_pts, num_e1_pts), num_e2_pts);

                    sorted_grade_beam_points = new Point3d[lst_grade_beam_points.Count];
                    sorted_grade_beam_points_edge1 = new Point3d[lst_grade_beam_points_edge1.Count];
                    sorted_grade_beam_points_edge2 = new Point3d[lst_grade_beam_points_edge2.Count];

                    // Now sort the points
                    try
                    {
                        sorted_grade_beam_points = SortPointsHorizontallyOrVertically(lst_grade_beam_points);
                        sorted_grade_beam_points_edge1 = SortPointsHorizontallyOrVertically(lst_grade_beam_points_edge1);
                        sorted_grade_beam_points_edge2 = SortPointsHorizontallyOrVertically(lst_grade_beam_points_edge2);

                    }
                    catch (System.Exception e)
                    {
                        doc.Editor.WriteMessage("\n--Error sorting grade beam points");
                        continue;
                    }
                } catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\n-Error finding trimmed grade beam points from centerline data: " + ex.Message);
                }

                if (sorted_grade_beam_points == null)
                {
                    continue;
                }

                if(sorted_grade_beam_points.Length < 2)
                {
                    doc.Editor.WriteMessage("\n-- at least two points required to make a grade beam.");
                    continue;
                }

                for (int j = 0; j < sorted_grade_beam_points.Length - 1; j = j + 2)
                {
                    
                    Point3d p1 = sorted_grade_beam_points[j];
                    Point3d p2 = sorted_grade_beam_points[j + 1];

                    // check if the grade beam is long enough for PT
                    if (MathHelpers.Distance3DBetween(p1, p2) <= DEFAULT_DONT_DRAW_PT_LENGTH)
                    {
                        // beam is too short so skip it
                        continue;
                    }

                    try
                    {
                        // Mark the intersection points for the beam centerline
                        //DrawCircle(sorted_grade_beam_points[j], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        //DrawCircle(sorted_grade_beam_points[j + 1], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

                        GradeBeamModel beam = null;

                        // If it's an odd number of intersection points create a grade beam for the entire trim length
                        if (sorted_grade_beam_points.Length % 2 != 0)
                        {
                            // TODO:  Sort out the algorithm for TANGENT points and odd number of interesection points or if the number of points on edge lines does
                            // not match the number of points on the centerlines
                            doc.Editor.WriteMessage("\n--Odd number of points found " + lst_grade_beam_points.Count + " intersection points found"
                                + "\n" + b1.X.ToString() + "," + b1.Y.ToString() + ") and \n("
                                + b2.X.ToString() + "," + b2.Y.ToString() + ") -- skipping grade beam");

                            beam = new GradeBeamModel(sorted_grade_beam_points[0], sorted_grade_beam_points[sorted_grade_beam_points.Length - 1],
                               untr_beam.StrandInfo.Qty, true, untr_beam.IsHorizontal, currentBeamNum, width, depth);
                            currentBeamNum++;
                            break;
                        }

                        // Otherwise, continue with splitting it into groups of 2
                        else
                        {
                            beam = new GradeBeamModel(p1, p2, untr_beam.StrandInfo.Qty, true, untr_beam.IsHorizontal, currentBeamNum, width, depth);
                            currentBeamNum++;
                        }

                        lstInteriorGradeBeamsTrimmed.Add(beam);

                        //int num_trimmed = 0;

                        //if (intPoints_edge1 != null && intPoints_edge2 != null)
                        //{
                        //    //                               if (intPoints_edge1 == intPoints_edge2)
                        //    //                               {
                        //    // Both edges of the trimmed beam are fully contained within the inner perimeter line
                        //    // So its safe to adjust their end points
                        //    // first for edge 1
                        //    using (Transaction trans = db.TransactionManager.StartTransaction())
                        //    {
                        //        try
                        //        {
                        //            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        //            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        //            var obj = trans.GetObject(beam.Edge1.ObjectId, OpenMode.ForRead);
                        //            var pl = obj as Line;

                        //            // Now let's make sure we can edit the line
                        //            pl.UpgradeOpen();

                        //            pl.StartPoint = intPoints_edge1[j];
                        //            pl.EndPoint = intPoints_edge1[j + 1];
                        //            trans.Commit();
                        //        }
                        //        catch (System.Exception ex)
                        //        {
                        //            doc.Editor.WriteMessage("\nError encountered moving Edge1 end points on trimmed grade beam #" + beam.BeamNum + ": " + ex.Message);
                        //            trans.Abort();
                        //        }
                        //    }

                        //    // then for edge 2
                        //    using (Transaction trans = db.TransactionManager.StartTransaction())
                        //    {
                        //        try
                        //        {
                        //            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        //            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        //            var obj = trans.GetObject(beam.Edge2.ObjectId, OpenMode.ForRead);
                        //            var pl = obj as Line;

                        //            // Now let's make sure we can edit the line
                        //            pl.UpgradeOpen();

                        //            pl.StartPoint = intPoints_edge2[j];
                        //            pl.EndPoint = intPoints_edge2[j + 1];

                        //            trans.Commit();
                        //        }
                        //        catch (System.Exception ex)
                        //        {
                        //            doc.Editor.WriteMessage("\nError encountered moving Edge2 end points on trimmed grade beam #" + beam.BeamNum + ": " + ex.Message);
                        //            trans.Abort();
                        //        }
                        //    }
                        //    //}
                        //    //else
                        //    //{
                        //    //    // Otherwise, one of the edge lines is outside the perimeter line so we'll just keep the grade beam as untrimmed
                        //    //    // i.e. do Nothing for now
                        //    //    // TODO:  Fix this case where one edge line is outside of the bounday while the other is not
                        //    //    doc.Editor.WriteMessage("\n-- Grade beam edge is outside boundary polying for trimmed grade beam #" + beam.BeamNum);

                        //    //}


                        //}
                        //else
                        //{
                        //    doc.Editor.WriteMessage("\nGrade beam at " + sorted_grade_beam_points[j].X + " , " + sorted_grade_beam_points[j].Y +
                        //        "is less than minimum [" + DEFAULT_DONT_DRAW_PT_LENGTH + "] for post tensioning. Skipping grade beam here.");
                        //}

                    }
                    catch (System.Exception e)
                    {
                        doc.Editor.WriteMessage("\nError creating grade beam at " + sorted_grade_beam_points[j].X + ", " + sorted_grade_beam_points[j + 1].Y);
                        DrawCircle(p1, 40, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        DrawCircle(p1, 50, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        DrawCircle(p1, 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                    }
                }
            }

            // Now add the grade beam entities to the drawing
            foreach (GradeBeamModel beam in lstInteriorGradeBeamsTrimmed)
            {
                try
                {
                    beam.AddToAutoCADDatabase(db, doc);
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\nError adding trimmed grade beam to AutoCAD database: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Draws the foundation perimeter grade beam by using offset.  
        /// Draws the centerline and the interior edge
        /// </summary>
        /// <param name="db">AutoCAD database</param>
        /// <param name="doc">AutoCAD document</param>
        /// <param name="beam_x_width">Width of the grade beam</param>
        private void DrawFoundationPerimeterBeam(Database db, Document doc, double beam_x_width)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Redraw the perimeter beam
                    doc.Editor.WriteMessage("\nCreating perimeter beam outer edge line.");
                    MovePolylineToLayer(FDN_PERIMETER_POLYLINE, EE_Settings.DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(FDN_PERIMETER_POLYLINE, "CONTINUOUS", bt, btr);

                    // Draw the perimeter beam centerline
                    doc.Editor.WriteMessage("\nCreating perimeter beam center line.");
                    FDN_PERIMETER_CENTERLINE_POLYLINE = OffsetPolyline(FDN_PERIMETER_POLYLINE, beam_x_width * 0.5, bt, btr);
                    MovePolylineToLayer(FDN_PERIMETER_CENTERLINE_POLYLINE, EE_Settings.DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(FDN_PERIMETER_CENTERLINE_POLYLINE, "CENTER", bt, btr);

                    // Offset the perimeter polyline and move it to its appropriate layer
                    doc.Editor.WriteMessage("\nCreating perimeter beam inner edge line.");
                    FDN_PERIMETER_INTERIOR_EDGE_POLYLINE = OffsetPolyline(FDN_PERIMETER_POLYLINE, beam_x_width, bt, btr);
                    MovePolylineToLayer(FDN_PERIMETER_INTERIOR_EDGE_POLYLINE, EE_Settings.DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, bt, btr);
                    PolylineSetLinetype(FDN_PERIMETER_INTERIOR_EDGE_POLYLINE, "HIDDEN", bt, btr);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered processing foundation polyline winding direction: " + ex.Message);
                    trans.Abort();
                }

                return;
            }
        }

        /// <summary>
        /// Verifies the selection of the foundation beam and adjusts the winding order to be clockwise (if necessary)
        /// </summary>
        /// <param name="db">AutoCAD database</param>
        /// <param name="edt">AutoCAD Editor</param>
        /// <param name="result">result from the entity selection in AutoCAD</param>
        /// <param name="foundationPerimeterPolyline">stores the foundation perimeter polyline</param>
        /// <exception cref="System.Exception"></exception>
        private Polyline ProcessFoundationPerimeter(Database db, Editor edt, PromptEntityResult result)
        {
            Polyline foundationPerimeterPolyline = new Polyline();
            if (result.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        foundationPerimeterPolyline = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;

                        ///////////////////////////////////////////////////
                        /// Now start processing  the foundation polylines
                        /// ///////////////////////////////////////////////
                        int numVertices = foundationPerimeterPolyline.NumberOfVertices;
                        var lstVertices = GetVertices(foundationPerimeterPolyline);

                        if (lstVertices.Count < 3)
                        {
                            edt.WriteMessage("\nFoundation must have at least three sides.  The selected polygon only has " + lstVertices.Count);
                            trans.Abort();
                            return null;
                        }
                        else
                        {
                            // Check that the polyline is in a clockwise winding.  If not, then reverse the polyline direction
                            // -- necessary for the offset functions later to work correctly.
                            if (!PolylineIsWoundClockwise(foundationPerimeterPolyline))
                            {
                                edt.WriteMessage("\nReversing foundation polyline direction to make it Clockwise");
                                ReversePolylineDirection(foundationPerimeterPolyline);
                            }
                            else
                            {
                                // Do nothing since it's already clockwise.
                            }
                        }

                        trans.Commit();

                    } catch (System.Exception ex)
                    {
                        edt.WriteMessage("\nError encountered processing foundation polyline winding direction: " + ex.Message);
                        trans.Abort();
                        return null;
                    }

                    return foundationPerimeterPolyline;
                }
            }
            else
            {
                throw new System.Exception("Unknown error in selection the foundation Polyline.");
            }
        }

        /// <summary>
        /// Determines the rectangular boundaing box for a list of Point2D points.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="edt"></param>
        /// <param name="lstVertices"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private Polyline CreateFoundationBoundingBox(Database db, Editor edt, List<Point2d> lstVertices)
        {
            if(lstVertices is null)
            {
                throw new System.Exception("Error in creating bounding box from foundation perimeter beam");
            }

            var lx = lstVertices[0].X;
            var ly = lstVertices[0].Y;
            var rx = lstVertices[0].X;
            var ry = lstVertices[0].Y;
            var tx = lstVertices[0].X;
            var ty = lstVertices[0].Y;
            var bx = lstVertices[0].X;
            var by = lstVertices[0].Y;


            // Finds the extreme limits of the foundation
            foreach (var vert in lstVertices)
            {
                if (vert.X < lx)
                    lx = vert.X;

                if (vert.X > rx)
                    rx = vert.X;

                if (vert.Y < by)
                    by = vert.Y;

                if (vert.Y > ty)
                    ty = vert.Y;
            }

            // Draw the limits of the foundation
            Point2d boundP1 = new Point2d(lx, by);  // lower left
            Point2d boundP2 = new Point2d(lx, ty);  // upper left
            Point2d boundP3 = new Point2d(rx, ty);  // upper right
            Point2d boundP4 = new Point2d(rx, by);  // bottom right

            Polyline pl = new Polyline();

            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                try
                {
                    // Specify the polyline parameters 
                    pl.AddVertexAt(0, boundP1, 0, 0, 0);
                    pl.AddVertexAt(1, boundP2, 0, 0, 0);
                    pl.AddVertexAt(2, boundP3, 0, 0, 0);
                    pl.AddVertexAt(3, boundP4, 0, 0, 0);
                    pl.Closed = true;
                    pl.ColorIndex = 140; // cyan color

                    // assign the layer
//                    pl.Layer = EE_Settings.DEFAULT_FDN_BOUNDINGBOX_LAYER;

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("\nError encountered drawing foundation boundary line: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        private void EE_ApplicationSetup(Document doc, Database db)
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
            CreateLayer(EE_Settings.DEFAULT_FDN_BOUNDINGBOX_LAYER, doc, db, 4); // cyan
            CreateLayer(EE_Settings.DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER, doc, db, 3); // green
            CreateLayer(EE_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER, doc, db, 1); // red
            CreateLayer(EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER, doc, db, 140); // blue
            CreateLayer(EE_Settings.DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER, doc, db, 1);  // green
            CreateLayer(EE_Settings.DEFAULT_FDN_BEAM_STRANDS_TRIMMED_LAYER, doc, db, 3);  // green
            CreateLayer(EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_Settings.DEFAULT_FDN_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_Settings.DEFAULT_FDN_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_Settings.DEFAULT_FDN_STRAND_ANNOTATION_LAYER, doc, db, 2); // red
            CreateLayer(EE_Settings.DEFAULT_PIER_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_Settings.DEFAULT_PIER_TEXTS_LAYER, doc, db, 2);  // yellow
            CreateLayer(EE_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow
        }

        /// <summary>
        /// Command line to run the foundation detailing progam
        /// </summary>
        [CommandMethod("EEFDN")]
        public void ShowModalWpfDialogCmd()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            #region Application Setup
            // Set up layers and linetypes and AutoCAD drawings items
            EE_ApplicationSetup(doc, db);
            #endregion

            FoundationLayout CurrentFoundationLayout = new FoundationLayout();
            CurrentFoundationLayout.OnFoundationLayoutCreate();

            
            // Keep reloading the dialog box if we are in preview mode
            while (PreviewMode = true)
            {
                EE_FDNInputDialog dialog;
                if (FirstLoad)
                {
                    // Use the default values
                    dialog = new EE_FDNInputDialog(CurrentFoundationLayout);
                    FirstLoad = false;
                } else
                {
                    // Otherwise reload the previous iteration values
                    dialog = new EE_FDNInputDialog(CurrentFoundationLayout, Beam_X_Qty, Beam_X_Spacing, Beam_X_Width, Beam_X_Depth,
                    Beam_Y_Qty, Beam_Y_Spacing, Beam_Y_Width, Beam_Y_Depth, Beam_X_Strand_Qty,
                    Beam_X_Slab_Strand_Qty, Beam_Y_Strand_Qty, Beam_Y_Slab_Strand_Qty, DEFAULT_DONT_DRAW_PT_LENGTH,
                    Beam_X_DETAIL_QTY_1, Beam_X_DETAIL_QTY_2, Beam_X_DETAIL_QTY_3, Beam_X_DETAIL_QTY_4, Beam_X_DETAIL_QTY_5,
                    Beam_X_DETAIL_SPA_1, Beam_X_DETAIL_SPA_2, Beam_X_DETAIL_SPA_3, Beam_X_DETAIL_SPA_4, Beam_X_DETAIL_SPA_5,
                    Beam_Y_DETAIL_QTY_1, Beam_Y_DETAIL_QTY_2, Beam_Y_DETAIL_QTY_3, Beam_Y_DETAIL_QTY_4, Beam_Y_DETAIL_QTY_5,
                    Beam_Y_DETAIL_SPA_1, Beam_Y_DETAIL_SPA_2, Beam_Y_DETAIL_SPA_3, Beam_Y_DETAIL_SPA_4, Beam_Y_DETAIL_SPA_5,
                    PiersSpecified, PierShape, PierWidth, PierHeight, ShouldClose);
                }



                var result = AcAp.ShowModalWindow(dialog);
                if (result.Value)
                {
                    edt.WriteMessage("\nDialog displayed and successfully entered");
                    
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        // Force a redraw of the screen?
                        doc.TransactionManager.EnableGraphicsFlush(true);
                        doc.TransactionManager.QueueForGraphicsFlush();
                        Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
                        trans.Commit();
                    }
                }



                if (dialog.ShouldClose)
                {
                    break; // exit our loop
                }
            }


            LayerObjects.HideLayer(EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER, doc, db);
            LayerObjects.HideLayer(EE_Settings.DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER, doc, db);
            LayerObjects.HideLayer(EE_Settings.DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER, doc, db);
            LayerObjects.HideLayer(EE_Settings.DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER, doc, db);

        }

    }
}


