﻿using Autodesk.AutoCAD.ApplicationServices;
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


        // Holds the primary foundation perimeter polyline object.
        public static Polyline FDN_PERIMETER_POLYLINE { get; set; } = new Polyline();
        public static Polyline FDN_PERIMETER_CENTERLINE_POLYLINE { get; set; } = new Polyline();

        public static Polyline FDN_PERIMETER_INTERIOR_EDGE_POLYLINE { get; set; } = new Polyline();

        // Hold the bounding box for the foundation extents
        public static Polyline FDN_BOUNDARY_BOX { get; set; } = new Polyline();

        // Holds the basis point for the grade beam grid
        public static Point3d FDN_GRADE_BEAM_BASIS_POINT { get; set; } = new Point3d();

        // Data storage Entities
        private static List<Line> BeamLines { get; set; } = new List<Line>();

        // Stores the untrimmed grade beams for the foundation
        private static List<GradeBeamModel> lstInteriorGradeBeamsUntrimmed { get; set; } = new List<GradeBeamModel>();
        private static List<GradeBeamModel> lstInteriorGradeBeamsTrimmed { get; set; } = new List<GradeBeamModel>();

        private static List<StrandModel> lstSlabStrandsUntrimmed { get; set; } = new List<StrandModel>();
        private static List<StrandModel> lstSlabStrandsTrimmed { get; set; } = new List<StrandModel>();


        #region PTI Slab Data Values
        public static int Beam_X_Qty { get; set; }
        public static int Beam_X_Strand_Qty { get; set; }
        public static int Beam_X_Slab_Strand_Qty { get; set; }

        public static double Beam_X_Spacing { get; set; }
        public static double Beam_X_Width { get; set; }
        public static double Beam_X_Depth { get; set; }

        public static int Beam_Y_Qty { get; set; }
        public static int Beam_Y_Strand_Qty { get; set; }
        public static int Beam_Y_Slab_Strand_Qty { get; set; }

        public static double Beam_Y_Spacing { get; set; }
        public static double Beam_Y_Width { get; set; }
        public static double Beam_Y_Depth { get; set; }
        #endregion


        [CommandMethod("EE_FDN")]
        public static void DrawFoundationDetails(
            int x_qty, double x_spa, double x_depth, double x_width,
            int y_qty, double y_spa, double y_depth, double y_width,
            int bx_strand_qty, int sx_strand_qty, int by_strand_qty, int sy_strand_qty)
        {
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

            double circle_radius = Beam_X_Spacing * 0.1; // for marking the intersections of beams and strands with the foundation polyline

            int max_beams = 75;  // define the maximum number of beams in a given direction -- in case we get into an infinite loop situation.

            // Get our AutoCAD API objects
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;
            
            #region Application Setup
            // Set up layers and linetypes and AutoCAD drawings items
            EE_ApplicationSetup(doc, db);
            #endregion

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

            #region Find the Insert (Basis) Point the GradeBeams
            doc.Editor.WriteMessage("\nGet grade beam insert point");
            FDN_GRADE_BEAM_BASIS_POINT = FindGradeBeamInsertPoint(db, doc);

            // Check that the basis point isn't outside of foundation polyline.  If it is, set it to the lower left corner of the boundary box.
            // TODO:  Figure out why this can happen sometime.  Possible the intersection point test is the cause?
            if((FDN_GRADE_BEAM_BASIS_POINT.X < FDN_BOUNDARY_BOX.GetPoint2dAt(0).X) ||
                (FDN_GRADE_BEAM_BASIS_POINT.X > FDN_BOUNDARY_BOX.GetPoint2dAt(2).X) ||
                (FDN_GRADE_BEAM_BASIS_POINT.Y < FDN_BOUNDARY_BOX.GetPoint2dAt(0).Y) ||
                (FDN_GRADE_BEAM_BASIS_POINT.Y > FDN_BOUNDARY_BOX.GetPoint2dAt(2).Y))
            {
                // Set the basis point to the lower left and then offset by half the width of the perimeter beam
                Point3d lower_left = FDN_BOUNDARY_BOX.GetPoint3dAt(0);
                FDN_GRADE_BEAM_BASIS_POINT = new Point3d(lower_left.X + 0.5 * Beam_X_Width, lower_left.Y + 0.5 * Beam_X_Width, lower_left.Z);
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
            
            #region Draw Untrimmed Grade Beams and Beam Strands
            doc.Editor.WriteMessage("\nDrawing untrimmed interior grade beams");

            // draw horizontal and vertical grade beams
            CreateUntrimmedGradeBeams(db, doc, FDN_GRADE_BEAM_BASIS_POINT, true);  // for horizontal beams
            CreateUntrimmedGradeBeams(db, doc, FDN_GRADE_BEAM_BASIS_POINT, false); // for vertical beams
            
            doc.Editor.WriteMessage("\n-- Completed drawing Interior grade beams. " + lstInteriorGradeBeamsUntrimmed.Count + " grade beams created.");

            #endregion

            #region Trim Grade Beam and Beam Strand Lines
            doc.Editor.WriteMessage("\nDrawing " + lstInteriorGradeBeamsUntrimmed.Count + " trimmed interior grade beams");

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

            #region Bill of Materials
            // compute concrete volumes

            // compute strand quantities
            #endregion

            #region Section Details
            #endregion

            #region Additional Steel
            #endregion
        }

        private static void CreateTrimmedSlabStrands(Database db, Document doc, List<StrandModel> list)
        {
            int numVerts_outer = FDN_PERIMETER_POLYLINE.NumberOfVertices;

            ///////////////////////////////////////////////////////////////////
            ///  Get the trimmed grade beam intersection points with the FDN_PERIMETER_INTERIOR_EDGE_POLYLINE
            ///////////////////////////////////////////////////////////////////
            // Create new models for each of the untrimmed grade beam models
            foreach (StrandModel untr_strand in list)
            {
                // Get the untrimmed end points of the beam centerline
                Point3d b1 = untr_strand.Centerline.StartPoint;
                Point3d b2 = untr_strand.Centerline.EndPoint;

                Point3d[] sorted_strand_points = null;

                try
                {
                    // Get the intersection for the trimmed grade beam with the inner edge polyline
                    sorted_strand_points = TrimAndSortIntersectionPoints(b1, b2, FDN_PERIMETER_POLYLINE);
                    if (sorted_strand_points != null)
                    {
                        doc.Editor.WriteMessage("\n--Found " + sorted_strand_points.Length.ToString() + " strand intersection points.");
                    }
                    else
                    {
                        // Null sorted points list returned, so skip this beam and continue
                        continue;
                    }
                }
                catch
                {
                    doc.Editor.WriteMessage("\n-Error finding trimmed slab strand points.");
                }

                for (int j = 0; j < sorted_strand_points.Length - 1; j = j + 2)
                {
                    try
                    {
                        // Mark the intersection points for the beam centerline
                        DrawCircle(sorted_strand_points[j], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_STRAND_ANNOTATION_LAYER);
                        DrawCircle(sorted_strand_points[j + 1], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_STRAND_ANNOTATION_LAYER);

                        // check if the grade beam is long enough for PT
                        if (MathHelpers.Distance3DBetween(sorted_strand_points[j], sorted_strand_points[j + 1]) > EE_Settings.DEFAULT_DONT_DRAW_PT_LENGTH)
                        {
                            StrandModel strand = new StrandModel(sorted_strand_points[j], sorted_strand_points[j + 1], 1, false, true);
                            lstSlabStrandsTrimmed.Add(strand);
                        }
                    }
                    catch (System.Exception e)
                    {
                        doc.Editor.WriteMessage("\nError creating trimmed slab strand at " + sorted_strand_points[j].X + ", " + sorted_strand_points[j + 1].Y);
                        DrawCircle(sorted_strand_points[j], 40, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
                        DrawCircle(sorted_strand_points[j], 50, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
                        DrawCircle(sorted_strand_points[j], 60, EE_Settings.DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER);
                    }
                }
            }

            // Now add the grade beam entities to the drawing
            foreach (StrandModel strand in lstSlabStrandsTrimmed)
            {
                try
                {
                    strand.AddToAutoCADDatabase(db, doc);
                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\nError adding trimmed slab strand to AutoCAD database: " + e.Message);
                }
            }
        }

        private static void CreateUntrimmedSlabStrands(Database db, Document doc, Point3d basis, bool isHorizontal)
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
        private static Point3d FindGradeBeamInsertPoint(Database db, Document doc)
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

                Point3d intPt = FindPointOfIntersectLines_FromPoint3d(
                    longestSegmentPoints[0], longestSegmentPoints[1],
                    longestSegmentPoints[2], longestSegmentPoints[3]);

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
        private static void CreateUntrimmedGradeBeams(Database db, Document doc, Point3d basis, bool isHorizontal)
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
                width = Beam_X_Width;
                spacing = Beam_X_Spacing;
                depth = Beam_X_Depth;

                // grade beams to the upper boundary box horizontal edge
                int count = 0;
                while (basis.Y + (count * spacing) < bbox_points[1].Y)
                {
                    int num_strands = Beam_X_Strand_Qty;

                    Point3d p1 = new Point3d(bbox_points[0].X, basis.Y + (count * spacing), 0);
                    Point3d p2 = new Point3d(bbox_points[3].X, basis.Y + (count * spacing), 0);

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

                    GradeBeamModel beam = new GradeBeamModel(p1, p2, FDN_PERIMETER_CENTERLINE_POLYLINE, Beam_X_Strand_Qty, false, width, depth);
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                    //StrandModel strand = new StrandModel(p1, p2, Beam_X_Strand_Qty, true);
                    //beam.StrandInfo = strand;
                    //BeamStrandLines.Add(strand);

                    count++;
                }

                count = 1;  // start at 1 here to avoid double drawing the first beam
                while (basis.Y - (count * spacing) > bbox_points[0].Y)
                {
                    Point3d p1 = new Point3d(bbox_points[0].X, basis.Y - (count * spacing), 0);
                    Point3d p2 = new Point3d(bbox_points[3].X, basis.Y - (count * spacing), 0);

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

                    GradeBeamModel beam = new GradeBeamModel(p1, p2, FDN_PERIMETER_CENTERLINE_POLYLINE, Beam_X_Strand_Qty, false, width, depth);
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                    //StrandModel strand = new StrandModel(p1, p2, Beam_X_Strand_Qty, true);
                    //beam.StrandInfo = strand;
                    //BeamStrandLines.Add(strand);

                    count++;
                }
            } 
            else
            {
                // for vertical beams
                width = Beam_Y_Width;
                spacing = Beam_Y_Spacing;
                depth = Beam_Y_Depth;

                int count = 0;
                while (basis.X + (count * spacing) < bbox_points[3].X)
                {
                    Point3d p1 = new Point3d(basis.X + (count * spacing), bbox_points[0].Y, 0);
                    Point3d p2 = new Point3d(basis.X + (count * spacing), bbox_points[1].Y, 0);

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

                    GradeBeamModel beam = new GradeBeamModel(p1, p2, FDN_PERIMETER_CENTERLINE_POLYLINE, Beam_Y_Strand_Qty, false, width, depth);
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                    //StrandModel strand = new StrandModel(p1, p2, Beam_Y_Strand_Qty, true);
                    //beam.StrandInfo = strand;
                    //BeamStrandLines.Add(strand);

                    count++;
                }

                count = 1;
                while (basis.X - (count * spacing) > bbox_points[0].X)
                {
                    Point3d p1 = new Point3d(basis.X - (count * spacing), bbox_points[0].Y, 0);
                    Point3d p2 = new Point3d(basis.X - (count * spacing), bbox_points[1].Y, 0);

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

                    GradeBeamModel beam = new GradeBeamModel(p1, p2, FDN_PERIMETER_CENTERLINE_POLYLINE, Beam_Y_Strand_Qty, false, width, depth);
                    lstInteriorGradeBeamsUntrimmed.Add(beam);
                   // StrandModel strand = new StrandModel(p1, p2, Beam_Y_Strand_Qty, true);
                   // beam.StrandInfo = strand;
                   // BeamStrandLines.Add(strand);

                    count++;
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
        private static void CreateTrimmedGradeBeams(Database db, Document doc, List<GradeBeamModel> list)
        {
            int numVerts_inner = FDN_PERIMETER_INTERIOR_EDGE_POLYLINE.NumberOfVertices;
            int numVerts_outer = FDN_PERIMETER_POLYLINE.NumberOfVertices;

            ///////////////////////////////////////////////////////////////////
            ///  Get the trimmed grade beam intersection points with the FDN_PERIMETER_INTERIOR_EDGE_POLYLINE
            ///////////////////////////////////////////////////////////////////
            // Create new models for each of the untrimmed grade beam models
            foreach (GradeBeamModel untr_beam in list)
            {
                double width = untr_beam.Width;
                double depth = untr_beam.Depth;

                // Get the untrimmed end points of the beam centerline
                Point3d b1 = untr_beam.Centerline.StartPoint;
                Point3d b2 = untr_beam.Centerline.EndPoint;

                Point3d[] sorted_grade_beam_points = null;
                Point3d[] sorted_strand_points = null;

                try
                {
                    // Get the intersection for the trimmed grade beam with the inner edge polyline
                    sorted_grade_beam_points = TrimAndSortIntersectionPoints(b1, b2, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);
                    if(sorted_grade_beam_points != null)
                    {
                        doc.Editor.WriteMessage("\n--Found " + sorted_grade_beam_points.Length.ToString() + " grade beam intersection points.");
                    }
                    else
                    {
                        // Null sorted points list returned, so skip this beam and continue
                        continue;
                    }
                } catch
                {
                    doc.Editor.WriteMessage("\n-Error finding trimmed grade beam points.");
                }

                try
                {
                    // Get the intersection for the trimmed strand with the outer edge polyline
                    sorted_strand_points = TrimAndSortIntersectionPoints(b1, b2, FDN_PERIMETER_POLYLINE);
                    if (sorted_grade_beam_points != null)
                    {
                        doc.Editor.WriteMessage("\n--Found " + sorted_strand_points.Length.ToString() + " strand intersection points.");
                    }
                    else
                    {
                        // Null sorted points list returned, so skip this beam and continue
                        continue;
                    }

                }
                catch
                {
                    doc.Editor.WriteMessage("\n-Error finding trimmed strand points.");
                    doc.Editor.WriteMessage("\n" + sorted_grade_beam_points.Length.ToString() + " sorted beam points and " + sorted_strand_points.Length.ToString() + " sorted strand points.");

                }

                doc.Editor.WriteMessage("\n" + sorted_grade_beam_points.Length.ToString() + " sorted beam points and " + sorted_strand_points.Length.ToString() + " sorted strand points.");

                for (int j = 0; j < sorted_grade_beam_points.Length - 1; j = j + 2)
                {
                    try
                    {
                        // Mark the intersection points for the beam centerline
                        DrawCircle(sorted_grade_beam_points[j], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        DrawCircle(sorted_grade_beam_points[j + 1], EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

                        // check if the grade beam is long enough for PT
                        if (MathHelpers.Distance3DBetween(sorted_grade_beam_points[j], sorted_grade_beam_points[j + 1]) > EE_Settings.DEFAULT_DONT_DRAW_PT_LENGTH)
                        {
                            GradeBeamModel beam = new GradeBeamModel(sorted_grade_beam_points[j], sorted_grade_beam_points[j + 1], FDN_PERIMETER_INTERIOR_EDGE_POLYLINE, untr_beam.StrandInfo.Qty, true, width, depth);

                            // Modify the trimmed edge lines of the new grade beam
                            Point3d[] intPoints_edge1 = TrimAndSortIntersectionPoints(beam.Edge1.StartPoint, beam.Edge1.EndPoint, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);
                            Point3d[] intPoints_edge2 = TrimAndSortIntersectionPoints(beam.Edge2.StartPoint, beam.Edge2.EndPoint, FDN_PERIMETER_INTERIOR_EDGE_POLYLINE);

                            if (intPoints_edge1 != null && intPoints_edge2 != null)
                            {
                                if (intPoints_edge1 == intPoints_edge2)
                                {
                                    // Both edges of the trimmed beam are fully contained within the inner perimeter line
                                    // So its safe to adjust their end points
                                    // first for edge 1
                                    using (Transaction trans = db.TransactionManager.StartTransaction())
                                    {
                                        try
                                        {
                                            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                                            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                                            var obj = trans.GetObject(beam.Edge1.ObjectId, OpenMode.ForRead);
                                            var pl = obj as Line;

                                            // Now let's make sure we can edit the line
                                            pl.UpgradeOpen();

                                            pl.StartPoint = intPoints_edge1[j];
                                            pl.EndPoint = intPoints_edge1[j + 1];

                                            trans.Commit();
                                        }
                                        catch (System.Exception ex)
                                        {
                                            doc.Editor.WriteMessage("\nError encountered moving Edge1 end points on trimmed grade beam #" + beam.BeamNum + ": " + ex.Message);
                                            trans.Abort();
                                        }
                                    }

                                    // then for edge 2
                                    using (Transaction trans = db.TransactionManager.StartTransaction())
                                    {
                                        try
                                        {
                                            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                                            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                                            var obj = trans.GetObject(beam.Edge2.ObjectId, OpenMode.ForRead);
                                            var pl = obj as Line;

                                            // Now let's make sure we can edit the line
                                            pl.UpgradeOpen();

                                            pl.StartPoint = intPoints_edge2[j];
                                            pl.EndPoint = intPoints_edge2[j + 1];

                                            trans.Commit();
                                        }
                                        catch (System.Exception ex)
                                        {
                                            doc.Editor.WriteMessage("\nError encountered moving Edge2 end points on trimmed grade beam #" + beam.BeamNum + ": " + ex.Message);
                                            trans.Abort();
                                        }
                                    }
                                }
                                else
                                {
                                    // Otherwise, one of the edge lines is outside the perimeter line so we'll just keep the grade beam as untrimmed
                                    // i.e. do Nothing for now
                                    // TODO:  Fix this case where one edge line is outside of the bounday while the other is not
                                }

                                lstInteriorGradeBeamsTrimmed.Add(beam);
                            }
                            else
                            {
                                doc.Editor.WriteMessage("\nGrade beam at " + sorted_grade_beam_points[j].X + " , " + sorted_grade_beam_points[j].Y + "is less than minimum [" + EE_Settings.DEFAULT_DONT_DRAW_PT_LENGTH + "] for post tensioning. Skipping grade beam here.");
                            }
                        }

                    }
                    catch (System.Exception e)
                    {
                        doc.Editor.WriteMessage("\nError creating grade beam at " + sorted_grade_beam_points[j].X + ", " + sorted_grade_beam_points[j + 1].Y);
                        DrawCircle(sorted_grade_beam_points[j], 40, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        DrawCircle(sorted_grade_beam_points[j], 50, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
                        DrawCircle(sorted_grade_beam_points[j], 60, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);
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
        private static void DrawFoundationPerimeterBeam(Database db, Document doc, double beam_x_width)
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
        private static Polyline ProcessFoundationPerimeter(Database db, Editor edt, PromptEntityResult result)
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

                        if (lstVertices.Count < 4)
                        {
                            edt.WriteMessage("\nFoundation must have at least four sides.  The selected polygon only has " + lstVertices.Count);
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
        private static Polyline CreateFoundationBoundingBox(Database db, Editor edt, List<Point2d> lstVertices)
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
                    pl.Layer = EE_Settings.DEFAULT_FDN_BOUNDINGBOX_LAYER;

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
        private static void EE_ApplicationSetup(Document doc, Database db)
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

        }









        [CommandMethod("JIM")]
        public void ShowModalWpfDialogCmd()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;
            //double radius; // radius default value
            //string layer;  // layer default value

            // private fields initialization (initial default values)
            // initial default values
           // layer = (string)AcAp.GetSystemVariable("clayer");
            //radius = 10.0;

            //var layers = GetAllLayerNamesList();
            //if (!layers.Contains(layer))
            //{
            //    layer = (string)AcAp.GetSystemVariable("clayer");
            //}

            // shows the dialog box
            //var dialog = new EE_FDNInputDialog(layers, layer, radius);
            var dialog = new EE_FDNInputDialog();

            var result = AcAp.ShowModalWindow(dialog);
            if (result.Value)
            {
                edt.WriteMessage("\nDialog displayed and successfully entered");
            }   

            //if (result.Value)
            //{
            //    // fields update
            //    //layer = dialog.Layer;
            //    //radius = dialog.Radius;

            //    //// circle drawing
            //    //var ppr = ed.GetPoint("\nSpecify the center: ");
            //    //if (ppr.Status == PromptStatus.OK)
            //    //{
            //    //    // drawing the circle in current space
            //    //    using (var tr = db.TransactionManager.StartTransaction())
            //    //    {
            //    //        //var curSpace =
            //    //        //    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            //    //        //using (var circle = new Circle(ppr.Value, Vector3d.ZAxis, radius))
            //    //        //{
            //    //        //    try
            //    //        //    {
            //    //        //        circle.TransformBy(ed.CurrentUserCoordinateSystem);
            //    //        //        circle.Layer = layer;
            //    //        //        curSpace.AppendEntity(circle);
            //    //        //        tr.AddNewlyCreatedDBObject(circle, true);
            //    //        //    } catch (System.Exception ex)
            //    //        //    {
                                
            //    //        //    }
            //    //        //}
            //    //        tr.Commit();
            //    //    }
            //    //}
            //}
        }

        
    }
}













        //                #region GradeBeams
        //                ///////////////////////////////////////////////////////////////////////////  
        //                // For each grade beam, find intersection points of the grade beams with
        //                // the foundation border using the entities of the BeamList
        //                //
        //                // Trim the line to the physical edge of the slab (not the limits rectangle)
        //                ///////////////////////////////////////////////////////////////////////////
        //                // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

        //                Point3dCollection points = null;
        //                if (foundationPerimeterPolyline != null && BeamLines.Count > 0)
        //                {
        //                    foreach (var beamline in BeamLines)
        //                    {
        //                        // Get the collection of intersection points and sort them
        //                        //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
        //                        points = IntersectionPointsOnPolyline(beamline, foundationPerimeterPolyline);
        //                        Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, points);

        //                        // We have the intersection points for each of the beam center lines, now mark them with circles
        //                        if (sorted_points != null)
        //                        {
        //                            // Mark the circles for intersection
        //                            for (int i = 0; i < sorted_points.Length; i++)
        //                            {
        //                                double radius = circle_radius;

        //                                try
        //                                {
        //                                    var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
        //                                    circle.ColorIndex = 1;
        //                                    circle.Layer = DEFAULT_FDN_ANNOTATION_LAYER;

        //                                    modelSpace.AppendEntity(circle);
        //                                    trans.AddNewlyCreatedDBObject(circle, true);
        //                                }
        //                                catch (System.Exception ex)
        //                                {
        //                                    edt.WriteMessage("\nError encountered while drawing intersection points: " + ex.Message);
        //                                    trans.Abort();
        //                                    return;
        //                                }
        //                            }

        //                            // Draw the trimmed beam center lines
        //                            try
        //                            {
        //                                TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, foundationPerimeterPolyline, true);
        //                            }
        //                            catch (System.Exception ex)
        //                            {
        //                                edt.WriteMessage("\nError encountered while trimming beam centerline to foundation extents: " + ex.Message);
        //                                trans.Abort();
        //                                return;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    edt.WriteMessage("\nError with creating beam objects");
        //                    trans.Abort();
        //                    return;
        //                }

        //                #endregion


        //                #region GradeBeam Strands
        //                ///////////////////////////////////////////////////////////////////////////  
        //                // For each grade beam, find intersection points of the grade beams with
        //                // the foundation border using the entities of the BeamList
        //                //
        //                // Trim the line to the physical edge of the slab (not the limits rectangle)
        //                ///////////////////////////////////////////////////////////////////////////
        //                // edt.WriteMessage("\n" + BeamLines.Count + " lines in BeamLines list");

        //                Point3dCollection strand_points = null;
        //                if (foundationPerimeterPolyline != null && BeamLines.Count > 0)
        //                {
        //                    foreach (var beamline in BeamLines)
        //                    {
        //                        // Get the collection of intersection points and sort them
        //                        //points = IntersectionPointsOnPolyline(beamline, innerPerimeterBeamPolyline);
        //                        strand_points = IntersectionPointsOnPolyline(beamline, foundationPerimeterPolyline);
        //                        Point3d[] sorted_points = SortIntersectionPoint3DArray(edt, strand_points);

        //                        // We have the intersection points for each of the beam center lines, now mark them with circles
        //                        if (sorted_points != null)
        //                        {
        //                            // Mark the circles for intersection
        //                            for (int i = 0; i < sorted_points.Length; i++)
        //                            {
        //                                double radius = circle_radius * 0.25;

        //                                try
        //                                {
        //                                    // Draw the strand markers
        //                                    var circle = new Circle(sorted_points[i], Vector3d.ZAxis, radius);
        //                                    circle.Layer = DEFAULT_FDN_BEAM_STRANDS_LAYER;

        //                                    modelSpace.AppendEntity(circle);
        //                                    trans.AddNewlyCreatedDBObject(circle, true);

        //                                    // Add strand labels -- stopping for the last point
        //                                    if (i != sorted_points.Length - 1)
        //                                    {
        //                                        // Only display at the start end
        //                                        if (i % 2 == 0)
        //                                        {
        //                                            AddStrandLabel(edt, trans, btr, sorted_points[i], sorted_points[i + 1]);
        //                                        }
        //                                    }
        //                                }
        //                                catch (System.Exception ex)
        //                                {
        //                                    edt.WriteMessage("\nError encountered while drawing strand intersection points: " + ex.Message);
        //                                    trans.Abort();
        //                                    return;
        //                                }
        //                            }

        //                            // Draw the trimmed strand center lines
        //                            try
        //                            {
        //                                TrimLinesToPolylineIntersection(edt, trans, btr, sorted_points, foundationPerimeterPolyline, false);
        //                            }
        //                            catch (System.Exception ex)
        //                            {
        //                                edt.WriteMessage("\nError encountered while trimming strand line to foundation extents: " + ex.Message);
        //                                trans.Abort();
        //                                return;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    edt.WriteMessage("\nError with creating beam objects");
        //                    trans.Abort();
        //                    return;
        //                }

        //                #endregion 

        //                trans.Commit();
        //            }
        //            catch (System.Exception ex)
        //            {
        //                edt.WriteMessage("\nError encountered while drawing beam objects: " + ex.Message);
        //                trans.Abort();
        //                return;
        //            }
        //        }
        //    }
        //}



        //private static void AddStrandLabel(Editor edt, Transaction trans, BlockTableRecord btr, Point3d pt1, Point3d pt2)
        //{
        //    Vector3d vector = pt1.GetVectorTo(pt2);
        //    var length = vector.Length;

        //    // TODO:  CHANGE STRAND LABEL TO HANDLE DOUBLE AND TRIPLE STRANDS (DS, TS)
        //    string txt = "S" + Math.Ceiling((length / 12.0) *10).ToString();
        //    Point3d insPt = pt1;

        //    // Get the angle of the polyline
        //    var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //    using (MText mtx = new MText())
        //    {
        //        try
        //        {
        //            mtx.Contents = txt;
        //            mtx.Location = insPt;
        //            mtx.TextHeight = 2;
        //            mtx.ColorIndex = 3;

        //            mtx.Layer = DEFAULT_FDN_TEXTS_LAYER;

        //            mtx.Rotation = angle;

        //            btr.AppendEntity(mtx);
        //            trans.AddNewlyCreatedDBObject(mtx, true);
        //        }
        //        catch (System.Exception ex)
        //        {
        //            edt.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
        //            trans.Abort();
        //            return;
        //        }

        //    }
        //}

        ///// <summary>
        ///// Takes a Point3DCollection and sorts the points from left to right and bottom to top and returns
        ///// an Point3d[]
        ///// </summary>
        ///// <param name="edt">AutoCAD editor object (for messaging)</param>
        ///// <param name="points">A <see cref="Points3dCollection"/> of points </param>
        ///// <returns>An array of Points3d[]</returns>
        //private static Point3d[] SortIntersectionPoint3DArray(Editor edt, Point3dCollection points)
        //{
        //    // Sort the collection of points into an array sorted from descending to ascending
        //    Point3d[] sorted_points = new Point3d[points.Count];

        //    // If the point is horizontal
        //    if (Math.Abs(points[1].Y - points[0].Y) < DEFAULT_HORIZONTAL_TOLERANCE)
        //    {
        //        //edt.WriteMessage("\nBeam " + beamCount + " is horizontal");
        //        sorted_points = sortPoint3dByHorizontally(points);
        //    }
        //    // Otherwise it is vertical
        //    else
        //    {
        //        //edt.WriteMessage("\nBeam " + beamCount + " is vertical");
        //        sorted_points = sortPoint3dByVertically(points);
        //    }

        //    if (sorted_points != null)
        //        edt.WriteMessage("\n" + sorted_points.Length + " points are intersecting the polyline");
        //    else
        //        edt.WriteMessage("\nNo points of intersection found");
        //    return sorted_points;
        //}

        ///// <summary>
        ///// Trim the beam lines to the foundation polyline.
        ///// Beam lines are drawn from the fondation extens polyline so they always start and end outside of an edge.
        ///// For beamlines that cross multiple edges at 'X' location
        ///// ------X----------------X-------------------X---------------X------------------X----------------X-----
        /////       0                1                   2               3                  4                5
        /////
        ///// should be broken into adjacent pairs
        /////       X----------------X                   X---------------X                  X----------------X
        /////       0                1                   2               3                  4                5
        ///// 
        ///// This function will draw them as a polyline so that we can vary the width and eventually cause a strand stagger
        ///// </summary>
        ///// <param name="edt">Autocad Editor object</param>
        ///// <param name="trans">The current database transaction</param>
        ///// <param name="btr">The AutoCAD model space block table record</param>
        ///// <param name="points">The points to trim</param>

        //private static void TrimLinesToPolylineIntersection(Editor edt, Transaction trans, BlockTableRecord btr, Point3d[] points, Polyline pline, bool shouldAddLabel=true)
        //{

        //    // Must be an even number of points currently
        //    // TODO:  Determine logic to hand odd number of intersection points -- which could occur for a tangent point to a corner.
        //    if(points.Length % 2 == 0)
        //    {
        //        for (int i = 0; i < points.Length; i = i + 2)
        //        {
        //            // Send a message to the user
        //            //edt.WriteMessage("\nDrawing a shortened Line object: ");
        //            Polyline pl = new Polyline();
        //            Point2d pt1 = new Point2d(points[i].X, points[i].Y);
        //            Point2d pt2 = new Point2d(points[i + 1].X, points[i + 1].Y);

        //            try
        //            {
        //                pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                pl.Closed = false;
        //                pl.ColorIndex = 150; // blue color
        //                pl.ConstantWidth = 1;
        //                pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                pl.Linetype = "CENTER";

        //                // Set the default properties
        //                pl.SetDatabaseDefaults();
        //                btr.AppendEntity(pl);
        //                trans.AddNewlyCreatedDBObject(pl, true);
        //            } catch (System.Exception ex)
        //            {
        //                edt.WriteMessage("\nError encountered while drawing trimmed beam line object from Pt1: (" 
        //                    + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                trans.Abort();
        //                return;
        //            }

        //            // Get the angle of the polyline
        //            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //            // Add number labels for each line segment
        //            if(shouldAddLabel)
        //                AddBeamLabels(edt, trans, btr, points, i, angle);

        //            beamCount++;
        //        }
        //    } else
        //    {
        //        for (int i = 0; i < points.Length; i = i + 2)
        //        {
        //            // Send a message to the user
        //            //edt.WriteMessage("\nDrawing a shortened Line object: ");

        //            Polyline pl = new Polyline();
        //            Point2d pt1 = new Point2d(points[i].X, points[i].Y);
        //            Point2d pt2 = new Point2d(points[points.Length-1].X, points[points.Length - 1].Y);

        //            // Assume Line AB and Polyline segment CD
        //            var overlap_case = HorizontalTestLineOverLapPolyline(pline, pt1, pt2);

        //            switch (overlap_case)
        //            {
        //                case 0:
        //                    {
        //                        try
        //                        {
        //                            pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                            pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                            pl.Closed = false;
        //                            pl.ColorIndex = 2; // yellow color
        //                            pl.ConstantWidth = 8;
        //                            pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                            pl.Linetype = "CENTER";

        //                            // Set the default properties
        //                            pl.SetDatabaseDefaults();
        //                            btr.AppendEntity(pl);
        //                            trans.AddNewlyCreatedDBObject(pl, true);

        //                            // Get the angle of the polyline
        //                            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //                            // Add number labels for each line segment
        //                            AddBeamLabels(edt, trans, btr, points, i, angle);

        //                        } catch (System.Exception ex)
        //                        {
        //                            edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
        //                                + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                            trans.Abort();
        //                            return;
        //                        }

        //                        break;
        //                    }

        //                default:
        //                    {
        //                        try
        //                        {
        //                            pl.AddVertexAt(0, pt1, 0, 0, 0);
        //                            pl.AddVertexAt(1, pt2, 0, 0, 0);
        //                            pl.Closed = false;
        //                            pl.ColorIndex = 1; // red color
        //                            pl.ConstantWidth = 8;

        //                            // Set the default properties
        //                            pl.SetDatabaseDefaults();
        //                            btr.AppendEntity(pl);
        //                            trans.AddNewlyCreatedDBObject(pl, true);
        //                            pl.Layer = DEFAULT_FDN_BEAMS_TRIMMED_LAYER;
        //                            pl.Linetype = "CENTER";

        //                            // Get the angle of the polyline
        //                            var angle = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));

        //                            // Add number labels for each line segment
        //                            AddBeamLabels(edt, trans, btr, points, i, angle);

        //                        }
        //                        catch (System.Exception ex)
        //                        {
        //                            edt.WriteMessage("\nError encountered while drawing modified beam line object from Pt1: ("
        //                                + pt1.X + "," + pt1.Y + ") to Pt2: (" + pt2.X + "," + pt2.Y + "):  " + ex.Message);
        //                            trans.Abort();
        //                            return;
        //                        }

        //                        break;
        //                    }
        //            }
        //        }

        //        edt.WriteMessage("\nError: The points list should have an even number of points");
        //    }
        //}






