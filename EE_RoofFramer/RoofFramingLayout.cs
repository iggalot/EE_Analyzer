using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.LinetypeObjects;
using static EE_Analyzer.Utilities.ModifyAutoCADGraphics;
using static EE_Analyzer.Utilities.PolylineObjects;
using static EE_Analyzer.Utilities.DimensionObjects;
using static EE_RoofFramer.Utilities.FileObjects;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Windows;
using EE_Analyzer;
using EE_Analyzer.Utilities;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using EE_RoofFramer.Models;
using EE_RoofFramer.Utilities;
using System.IO;
using System.Threading;

namespace EE_RoofFramer
{
    public class RoofFramingLayout
    {
        private double rafter_spacing = 24;  // rafter spacing in inches

        public int CurrentDirectionMode = -2;  // -2 is the placeholder for horizontal, -1 for vertical, 0 through n is perpendicular to edge with start point at 0 through n

        // A variable to handle previewing the grade beam locations
        public bool PreviewMode = true;
        public bool FirstLoad = true;
        public bool ShouldClose = false;
        public bool IsComplete = false;

        // Holds the perimeter polyline object
        public Polyline ROOF_PERIMETER_POLYLINE { get; set; }

        //Holds the perimeter polyline object
        public Polyline ROOF_BOUNDARY_BOX { get; set; }

        // Holds the basis point for the grade beam grid
        public Point3d ROOF_FRAMING_BASIS_POINT { get; set; }

        // Get our AutoCAD API objects
        public static Document doc { get; } = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public static Database db { get; } = doc.Database;


        /// <summary>
        /// Dictionaries to hold our model database.  Indexed by the ID number of the element
        /// </summary>
        IDictionary<int, RafterModel> dctRafters_Untrimmed { get; set; } = new Dictionary<int, RafterModel>();
        IDictionary<int, RafterModel> dctRafters_Trimmed { get; set; } = new Dictionary<int, RafterModel>();
        IDictionary<int, SupportModel_SS_Beam> dctSupportBeams { get; set; } = new Dictionary<int, SupportModel_SS_Beam>();
        IDictionary<int, LoadModel> dctLoads { get; set; } = new Dictionary<int, LoadModel>();
        IDictionary<int, ConnectionModel> dctConnections { get; set; } = new Dictionary<int, ConnectionModel>();


        public RoofFramingLayout()
        {

        }

        /// <summary>
        /// Draws the roof framing, including a preview mode with temporary objects
        /// </summary>
        /// <param name="preview_mode"></param>
        /// <param name="should_close"></param>
        /// <param name="mode_number"></param>
        /// <returns></returns>
        public bool DrawRoofFramingDetails(bool preview_mode, bool should_close, int mode_number)
        {
            if (mode_number >= ROOF_PERIMETER_POLYLINE.NumberOfVertices)
            {
                CurrentDirectionMode = -2;
            }
            else
            {
                CurrentDirectionMode = mode_number;
            }

            ShouldClose = should_close;
            IsComplete = false;
            PreviewMode = preview_mode;

            // If the window has been canceled dont bother doing anything else
            if (should_close)
            {
                IsComplete = true;
                return IsComplete;
            }

            Point3d start = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(0);
            Point3d end = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(1);

            // If we are in preview mode we will just draw centerlines as a marker.
            // Clear our temporary layer prior to drawing temporary items
            #region Preview Mode
            if (PreviewMode is true)
            {
                // If we are in preview mode we will just draw centerlines as a marker.
                // Clear our temporary layer prior to drawing temporary items
                DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, doc, db);

                DoPreviewMode(start, end);

                foreach (KeyValuePair<int,RafterModel> kvp in dctRafters_Untrimmed)
                {
                    kvp.Value.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, dctConnections, dctLoads);

                }

                ModifyAutoCADGraphics.ForceRedraw(db, doc);

                IsComplete = false;
                return IsComplete;
            }
            // Clear our temporary layer now that preview mode is over
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, doc, db);
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

            #endregion

            #region Finalize Mode -- Trim Rafters
            try
            {

                foreach (KeyValuePair<int, RafterModel> kvp in dctRafters_Untrimmed)
                {
                    List<Point3d> lst_intPt = EE_Helpers.FindPolylineIntersectionPoints(new Line(kvp.Value.StartPt, kvp.Value.EndPt), ROOF_PERIMETER_POLYLINE);

                    // is it a valid list of intersection points
                    if (lst_intPt == null)
                    {
                        continue;
                    }

                    // need two points to make a rafter
                    if (lst_intPt.Count < 2)
                    {
                        continue;
                    }

                    foreach (Point3d pt in lst_intPt)
                    {
                        DrawCircle(pt, 6, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER);
                    }

                    RafterModel new_model = new RafterModel(lst_intPt[0], lst_intPt[1], rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER);

                    //// Add a uniform load
                    //LoadModel uniform_load_model = new LoadModel(10, 20, 20, new_model.StartPt, new_model.EndPt, LoadTypes.LOAD_TYPE_FULL_UNIFORM_LOAD);
                    //AddLoadToLayout(uniform_load_model);
                    //new_model.AddUniformLoads(uniform_load_model, dctLoads);

                    // finally add the rafter to the list
                    AddTrimmedRafterToLayout(new_model);
                }
            } catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("Error finalizing trimmed rafters: " + ex.Message);
            }

            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

            #endregion

            #region Draw Purlins

            //bool exceed_length = true;
            //int count = 1;
            //while(exceed_length is true)
            //{
            //    exceed_length = false;

            //    foreach (var item in lstRafters_Trimmed)
            //    {
            //        if (item.Length > EE_ROOF_Settings.DEFAULT_MAX_PURLIN_SPACING * count)
            //        {
            //            Point3d purlin_pt = MathHelpers.Point3dFromVectorOffset(item.StartPt, item.vDir * EE_ROOF_Settings.DEFAULT_MAX_PURLIN_SPACING * count);
            //            DrawCircle(purlin_pt, 6, EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_LAYER);
            //            exceed_length = true;
            //        }
            //    }

            //    count++;
            //}


            #endregion




            ModifyAutoCADGraphics.ForceRedraw(db, doc);
            // Indicate that the dialog should close.
            ShouldClose = true;
            IsComplete = true;

            // Draw all the framing
            DrawAllRoofFraming();

            // Write the data models to their respective files
            WriteAllDataToFiles();

            return IsComplete;
        }


        /// <summary>
        /// Preview mode functionality
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void DoPreviewMode(Point3d start, Point3d end)
        {
            dctRafters_Untrimmed.Clear();

            switch (CurrentDirectionMode)
            {
                case -2:
                    {
                        CreateHorizontalRafters(start, end);
                        break;
                    }

                case -1:
                    {
                        CreateVerticalRafters(start, end);
                        break;
                    }

                default:
                    {
                        CreatePerpendicularRafters(CurrentDirectionMode);
                        break;
                    }
            }
        }


        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreateVerticalRafters(Point3d start, Point3d end)
        {
            Vector3d dir_unit_vec = new Vector3d(0, 1, 0);
            // get unit vect perpendicular to selected edge
            Vector3d perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_unit_vec, (-1.0) * new Vector3d(0, 0, 1)));

            int num_spaces_from_intpt_to_start = 0;
            int num_spaces_from_intpt_to_end = 0;

            // find furthest point on perpendicular line
            Point3d current_vertex = new Point3d(0, 0, 0);

            // Base point is at the mid-height of bounding box
            Point3d temp_vertex = MathHelpers.GetMidpoint(ROOF_BOUNDARY_BOX.GetPoint3dAt(0), ROOF_BOUNDARY_BOX.GetPoint3dAt(3));

            Vector3d dir_vec_intpt_to_start = new Vector3d(start.X - temp_vertex.X, 0, 0);
            Vector3d dir_vec_intpt_to_end = new Vector3d(end.X - temp_vertex.X, 0, 0);

            num_spaces_from_intpt_to_start = (int)Math.Ceiling(MathHelpers.Magnitude(dir_vec_intpt_to_start) / rafter_spacing) + 1;
            num_spaces_from_intpt_to_end = (int)Math.Ceiling(MathHelpers.Magnitude(dir_vec_intpt_to_end) / rafter_spacing) + 1;

            DrawCircle(temp_vertex, 10, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

            // Draw rafters from intersect point to start point
            for (int i = 0; i < num_spaces_from_intpt_to_start - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_start) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(0, 1, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(1).Y - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y));

                RafterModel new_model = new RafterModel(start_pt, new_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);
            }

            // Draw rafters from intersect point to end point -- start at index of 1 so we dont double draw the longest rafter
            for (int i = 1; i < num_spaces_from_intpt_to_end - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_end) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(0, 1, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(1).Y - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y));

                RafterModel new_model = new RafterModel(start_pt, new_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);
            }
        }


        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreateHorizontalRafters(Point3d start, Point3d end)
        {
            Vector3d dir_unit_vec = new Vector3d(1, 0, 0);
            // get unit vect perpendicular to selected edge
            Vector3d perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_unit_vec, new Vector3d(0, 0, 1)));

            int num_spaces_from_intpt_to_start = 0;
            int num_spaces_from_intpt_to_end = 0;

            // Base point is at the mid-height of bounding box
            Point3d temp_vertex = MathHelpers.GetMidpoint(ROOF_BOUNDARY_BOX.GetPoint3dAt(0), ROOF_BOUNDARY_BOX.GetPoint3dAt(1));

            Vector3d dir_vec_intpt_to_start = new Vector3d(0, start.Y - temp_vertex.Y, 0);
            Vector3d dir_vec_intpt_to_end = new Vector3d(0, end.Y - temp_vertex.Y, 0);

            num_spaces_from_intpt_to_start = (int)Math.Ceiling(MathHelpers.Magnitude(dir_vec_intpt_to_start) / rafter_spacing) + 1;
            num_spaces_from_intpt_to_end = (int)Math.Ceiling(MathHelpers.Magnitude(dir_vec_intpt_to_end) / rafter_spacing) + 1;

            DrawCircle(temp_vertex, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);

            // Draw rafters from intersect point to start point
            for (int i = 0; i < num_spaces_from_intpt_to_start - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_start) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(1, 0, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(3).X - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X));

                RafterModel new_model = new RafterModel(start_pt, new_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);
            }

            // Draw rafters from intersect point to end point -- start at index of 1 so we dont double draw the longest rafter
            for (int i = 1; i < num_spaces_from_intpt_to_end - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_end) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(1, 0, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(3).X - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X));

                RafterModel new_model = new RafterModel(start_pt, new_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);
            }


        }


        /// <summary>
        /// Creates rafters that are perpendicular to the edge between start and end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreatePerpendicularRafters(int start_node_index)
        {
            Point3d start = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(start_node_index);
            Point3d end = ROOF_PERIMETER_POLYLINE.GetPoint3dAt((start_node_index + 1) % ROOF_PERIMETER_POLYLINE.NumberOfVertices);

            Vector3d dir_vec = start.GetVectorTo(end);
            Vector3d dir_unit_vec = MathHelpers.Normalize(dir_vec);
            // get unit vect perpendicular to selected edge
            Vector3d perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_vec, new Vector3d(0, 0, 1)));

            // find furthest point on perpendicular line
            IntersectPointData intPt = null;
            double max_length = 0;
            Point3d longest_vertex = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(0);
            Vector3d v1 = new Vector3d();

            for (int i = 0; i < ROOF_PERIMETER_POLYLINE.NumberOfVertices; i++)
            {
                Point3d current_vertex = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(i);

                // if our test vertex is either the start or end, we can't drw rafters
                if (current_vertex == start || current_vertex == end)
                {
                    continue;
                }

                Point3d temp_pt = MathHelpers.Point3dFromVectorOffset(current_vertex, perp_unit_vec * 100);
                IntersectPointData current_intPt = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start, end, current_vertex, temp_pt);

                double length = (MathHelpers.Magnitude(current_intPt.Point.GetVectorTo(current_vertex)));

                // Is this the longest?
                if (length > max_length)
                {
                    max_length = length;
                    intPt = current_intPt;
                    longest_vertex = temp_pt;
                    v1 = MathHelpers.Normalize(longest_vertex.GetVectorTo(current_intPt.Point));
                }
            }

            // Compute the vectors from our vertex to each of the four corners of the bounding box.
            Vector3d vert_toBB0 = longest_vertex.GetVectorTo(ROOF_BOUNDARY_BOX.GetPoint3dAt(0));
            Vector3d vert_toBB1 = longest_vertex.GetVectorTo(ROOF_BOUNDARY_BOX.GetPoint3dAt(1));
            Vector3d vert_toBB2 = longest_vertex.GetVectorTo(ROOF_BOUNDARY_BOX.GetPoint3dAt(2));
            Vector3d vert_toBB3 = longest_vertex.GetVectorTo(ROOF_BOUNDARY_BOX.GetPoint3dAt(3));

            // Project each vector onto a unit direction vector for the rafter
            double d0_proj = MathHelpers.DotProduct(vert_toBB0, perp_unit_vec);
            double d1_proj = MathHelpers.DotProduct(vert_toBB1, perp_unit_vec);
            double d2_proj = MathHelpers.DotProduct(vert_toBB2, perp_unit_vec);
            double d3_proj = MathHelpers.DotProduct(vert_toBB3, perp_unit_vec);

            // max distance
            double max_dist = Math.Max(d0_proj, Math.Max(d1_proj, Math.Max(d2_proj, d3_proj)));
            double min_dist = Math.Min(d0_proj, Math.Min(d1_proj, Math.Min(d2_proj, d3_proj)));

            doc.Editor.WriteMessage("\nd0: " + d0_proj + "   d1: " + d1_proj + "   d2: " + d2_proj + "   d3: " + d3_proj);
            doc.Editor.WriteMessage("Max: " + max_dist + "   Min: " + min_dist);

            Point3d start_pt = MathHelpers.Point3dFromVectorOffset(longest_vertex, perp_unit_vec * max_dist);
            Point3d end_pt = MathHelpers.Point3dFromVectorOffset(longest_vertex, perp_unit_vec * min_dist);
            double length_of_untrimmed = MathHelpers.Magnitude(start_pt.GetVectorTo(end_pt));

            //// MArkers for the longest lines
            //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
            //DrawCircle(end_pt, 20, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
            //DrawLine(start_pt, end_pt, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");

            // Create a line from each corner of bounding box to arbitrary point along the dir_unit_vec for the side
            Point3d bb0_a = ROOF_BOUNDARY_BOX.GetPoint3dAt(0);
            Point3d bb1_a = ROOF_BOUNDARY_BOX.GetPoint3dAt(1);
            Point3d bb2_a = ROOF_BOUNDARY_BOX.GetPoint3dAt(2);
            Point3d bb3_a = ROOF_BOUNDARY_BOX.GetPoint3dAt(3);

            // The other end of the arbitrary line
            Point3d bb0_b = MathHelpers.Point3dFromVectorOffset(ROOF_BOUNDARY_BOX.GetPoint3dAt(0), dir_unit_vec * 1000);
            Point3d bb1_b = MathHelpers.Point3dFromVectorOffset(ROOF_BOUNDARY_BOX.GetPoint3dAt(1), dir_unit_vec * 1000);
            Point3d bb2_b = MathHelpers.Point3dFromVectorOffset(ROOF_BOUNDARY_BOX.GetPoint3dAt(2), dir_unit_vec * 1000);
            Point3d bb3_b = MathHelpers.Point3dFromVectorOffset(ROOF_BOUNDARY_BOX.GetPoint3dAt(3), dir_unit_vec * 1000);


            //DrawLine(bb0_a, bb0_b, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");
            //DrawLine(bb1_a, bb1_b, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");
            //DrawLine(bb2_a, bb2_b, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");
            //DrawLine(bb3_a, bb3_b, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");


            // Find intersection points of these line segments with our longest rafter
            // BB0
            IntersectPointData ipd_bb0 = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start_pt, end_pt, bb0_a, bb0_b);
            Point3d intpt_bb0 = ipd_bb0.Point;
            Vector3d v0_bb0 = intpt_bb0.GetVectorTo(bb0_a);
            double len0 = MathHelpers.Magnitude(v0_bb0);
            Vector3d uv0_bb0 = v0_bb0 / len0;  // unit vector
            DrawCircle(intpt_bb0, 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

            // BB1
            IntersectPointData ipd_bb1 = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start_pt, end_pt, bb1_a, bb1_b);
            Point3d intpt_bb1 = ipd_bb1.Point;
            Vector3d v1_bb1 = intpt_bb1.GetVectorTo(bb1_a);
            double len1 = MathHelpers.Magnitude(v1_bb1);
            Vector3d uv1_bb1 = v1_bb1 / len1;  // unit vector
            DrawCircle(intpt_bb1, 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

            // BB2
            IntersectPointData ipd_bb2 = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start_pt, end_pt, bb2_a, bb2_b);
            Point3d intpt_bb2 = ipd_bb2.Point;
            Vector3d v2_bb2 = intpt_bb2.GetVectorTo(bb2_a);
            double len2 = MathHelpers.Magnitude(v2_bb2);
            Vector3d uv2_bb2 = v2_bb2 / len2;  // unit vector
            DrawCircle(intpt_bb2, 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

            // BB3
            IntersectPointData ipd_bb3 = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start_pt, end_pt, bb3_a, bb3_b);
            Point3d intpt_bb3 = ipd_bb3.Point;
            Vector3d v3_bb3 = intpt_bb3.GetVectorTo(bb3_a);
            double len3 = MathHelpers.Magnitude(v3_bb3);
            Vector3d uv3_bb3 = v3_bb3 / len3;  // unit vector
            DrawCircle(intpt_bb3, 5, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);

            //Find the two unit vectors  away from this main rafter
            // One of the vectors has to be the first one.
            List<Vector3d> lst_v_side1 = new List<Vector3d>();
            List<Vector3d> lst_v_side2 = new List<Vector3d>();

            lst_v_side1.Add(v0_bb0);

            // if the vector coefficients have the same sign (products are greater than 0) for all components, we know they are in the same direction.
            if ((v0_bb0.X * v1_bb1.X >= 0) && (v0_bb0.Y * v1_bb1.Y >= 0) && (v0_bb0.Z * v1_bb1.Z >= 0))
            {
                lst_v_side1.Add(v1_bb1);
            }
            else
            {
                lst_v_side2.Add(v1_bb1);
            }

            if ((v0_bb0.X * v2_bb2.X >= 0) && (v0_bb0.Y * v2_bb2.Y >= 0) && (v0_bb0.Z * v2_bb2.Z >= 0))
            {
                lst_v_side1.Add(v2_bb2);
            }
            else
            {
                lst_v_side2.Add(v2_bb2);
            }

            if ((v0_bb0.X * v3_bb3.X >= 0) && (v0_bb0.Y * v3_bb3.Y >= 0) && (v0_bb0.Z * v3_bb3.Z >= 0))
            {
                lst_v_side1.Add(v3_bb3);
            }
            else
            {
                lst_v_side2.Add(v3_bb3);
            }

            // Now find the max magnitude of the vectors in each of these two lists
            double max_list1 = 0;
            Vector3d uv_list1 = new Vector3d(); // unit vector

            foreach (var item in lst_v_side1)
            {
                double len = MathHelpers.Magnitude(item);
                if (len > max_list1)
                {
                    max_list1 = len;
                    uv_list1 = item / len;
                }
            }

            double max_list2 = 0;
            Vector3d uv_list2 = new Vector3d(); // unit vector
            foreach (var item in lst_v_side2)
            {
                double len = MathHelpers.Magnitude(item);
                if (len > max_list2)
                {
                    max_list2 = len;
                    uv_list2 = item / len;
                }
            }

            // now compute the number of rafters for each side
            int num_rafters_side1 = (int)(Math.Ceiling(max_list1 / rafter_spacing));
            int num_rafters_side2 = (int)(Math.Ceiling(max_list2 / rafter_spacing));

            // Draw rafters from our longest rafter to the farthest corner on the bounding box
            for (int i = 0; i < num_rafters_side1; i++)
            {
                Point3d start_temp_pt = MathHelpers.Point3dFromVectorOffset(start_pt, uv_list1 * i * rafter_spacing);
                Point3d end_temp_pt = MathHelpers.Point3dFromVectorOffset(end_pt, uv_list1 * i * rafter_spacing);

                RafterModel new_model = new RafterModel(start_temp_pt, end_temp_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);

            }

            // Draw rafters from intersect point to start point -- start at index 1 so we dont duplicate the middle rafter
            for (int i = 1; i < num_rafters_side2; i++)
            {
                Point3d start_temp_pt = MathHelpers.Point3dFromVectorOffset(start_pt, uv_list2 * i * rafter_spacing);
                Point3d end_temp_pt = MathHelpers.Point3dFromVectorOffset(end_pt, uv_list2 * i * rafter_spacing);

                RafterModel new_model = new RafterModel(start_temp_pt, end_temp_pt, rafter_spacing, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER);

                //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
                // finally add the rafter to the list
                AddUntrimmedRafterToLayout(new_model);
            }
        }

        /// <summary>
        /// Fired when the roof framing layout is initially created, to establish basic common parameters. 
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        public bool OnRoofFramingLayoutCreate()
        {
            // Get our AutoCAD API objects
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            #region Select Roof Region in AutoCAD
            // Selects the foundation polyline and corrects the winding order to be clockwise.

            var options = new PromptEntityOptions("\nSelect Roof Section Perimeter Polyline");
            options.SetRejectMessage("\nSelected object is not a polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            // Select the polyline for the foundation
            var result = edt.GetEntity(options);
            ROOF_PERIMETER_POLYLINE = ProcessRoofLayoutPerimeter(result);

            if (ROOF_PERIMETER_POLYLINE is null)
            {
                throw new System.Exception("\nInvalid roof perimeter line selected.");
            }
            else
            {
                doc.Editor.WriteMessage("\nRoof perimeter line selected");
            }
            #endregion

            #region Create and Draw Bounding Box
            var lstVertices = GetVertices(ROOF_PERIMETER_POLYLINE);
            doc.Editor.WriteMessage("\n--Roof perimeter has " + lstVertices.Count + " vertices.");

            // Create the roof bounding box
            doc.Editor.WriteMessage("\n-- Creating roof bounding box.");
            ObjectId oid = CreateRoofBoundingBox(lstVertices);
            if(oid == ObjectId.Null)
            {
                doc.Editor.WriteMessage("Invalid roof boundary box created.");
                return false;
            } else
            {
                ROOF_BOUNDARY_BOX = GetPolylineByObjectId(db, oid);
            }

            #endregion

            #region Zoom Control to improve Autocad View
            // Zoom to the exents of the bounding box
            double zoom_factor = 0.02;
            Point3d zp1 = new Point3d(ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X * (1 - zoom_factor), ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y * (1 - zoom_factor), 0);
            Point3d zp2 = new Point3d(ROOF_BOUNDARY_BOX.GetPoint3dAt(2).X * (1 + zoom_factor), ROOF_BOUNDARY_BOX.GetPoint3dAt(2).Y * (1 + zoom_factor), 0);

            ZoomWindow(db, doc, zp1, zp2);
            #endregion

            #region Find the Insert (Basis) Point for the Layout
            // For the detailed spacings, use the lower left corner, otherwise use our algorithm for the optimized location
            doc.Editor.WriteMessage("\nGet roof framing insert point");

            // use the lower left corner of the bounding box (index 0);
            ROOF_FRAMING_BASIS_POINT = new Point3d(ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X, ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y,
                ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Z); ;

            // Add a marker for this point.
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 20, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 25, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 30, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 35, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 40, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 45, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);
            DrawCircle(ROOF_FRAMING_BASIS_POINT, 50, EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER);

            doc.Editor.WriteMessage("\n-Intersection of longest segments at :" + ROOF_FRAMING_BASIS_POINT.X.ToString() + ", " + ROOF_FRAMING_BASIS_POINT.Y.ToString() + ", " +
                ROOF_FRAMING_BASIS_POINT.Z.ToString());

            doc.Editor.WriteMessage("\nRoof basis insert point computed succssfully");
            #endregion

            return true;
        }


        /// <summary>
        /// Sets up the AutoCAD linetypes and the layers for the application
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        private void EE_ApplicationSetup()
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
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WALL_SUPPORT_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 2); // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER, doc, db, 140); // blue


            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_SUPPORT_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, doc, db, 140);  // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db, 140);  // yellow
                        
            //Create the EE dimension style
            CreateEE_DimensionStyle(EE_ROOF_Settings.DEFAULT_EE_DIMSTYLE_NAME);
        }

        /// <summary>
        /// Creates the bounding box for the roof region
        /// </summary>
        /// <param name="db"></param>
        /// <param name="edt"></param>
        /// <param name="lstVertices"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private ObjectId CreateRoofBoundingBox(List<Point2d> lstVertices)
        {
            if (lstVertices is null)
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

            ObjectId oid;

            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                try
                {
                    // Specify the polyline parameters 
                    using (Polyline pl = new Polyline())
                    {
                        pl.AddVertexAt(0, boundP1, 0, 0, 0);
                        pl.AddVertexAt(1, boundP2, 0, 0, 0);
                        pl.AddVertexAt(2, boundP3, 0, 0, 0);
                        pl.AddVertexAt(3, boundP4, 0, 0, 0);
                        pl.Closed = true;
                        pl.ColorIndex = 140; // cyan color
                        pl.Layer = EE_ROOF_Settings.DEFAULT_ROOF_BOUNDINGBOX_LAYER;

                        // Set the default properties
                        pl.SetDatabaseDefaults();
                        oid = btr.AppendEntity(pl);
                        trans.AddNewlyCreatedDBObject(pl, true);

                        trans.Commit();

                        return oid;
                    }
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered drawing roof boundary box line: " + ex.Message);
                    trans.Abort();
                    return ObjectId.Null;
                }
            }

        }

        /// <summary>
        /// Processes the selected roof polyline, making adjustments and correcting the winding order if necessary.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="edt"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private Polyline ProcessRoofLayoutPerimeter(PromptEntityResult result)
        {
            Polyline roofPerimeterPolyline;
            if (result.Status == PromptStatus.OK)
            {
                // at this point we know an entity has been selected and it is a Polyline
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        roofPerimeterPolyline = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;

                        ///////////////////////////////////////////////////
                        /// Now start processing  the foundation polylines
                        /// ///////////////////////////////////////////////
                        int numVertices = roofPerimeterPolyline.NumberOfVertices;
                        var lstVertices = GetVertices(roofPerimeterPolyline);

                        if (lstVertices.Count < 3)
                        {
                            doc.Editor.WriteMessage("\nPolyline must have at least three sides.  The selected polygon only has " + lstVertices.Count);
                            trans.Abort();
                            return null;
                        }
                        else
                        {
                            // Check that the polyline is in a clockwise winding.  If not, then reverse the polyline direction
                            // -- necessary for the offset functions later to work correctly.
                            if (!PolylineIsWoundClockwise(roofPerimeterPolyline))
                            {
                                doc.Editor.WriteMessage("\nReversing polyline direction to make it Clockwise");
                                ReversePolylineDirection(roofPerimeterPolyline);
                            }
                            else
                            {
                                // Do nothing since it's already clockwise.
                            }
                        }

                        trans.Commit();

                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError encountered processing roof polyline winding direction: " + ex.Message);
                        trans.Abort();
                        return null;
                    }

                    return roofPerimeterPolyline;
                }
            }
            else
            {
                throw new System.Exception("Unknown error in selection of the roof Polyline.");
            }
        }

        public void DrawAllRoofFraming()
        {
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER, doc, db);

            // Now draw the current rafter file contents
            foreach (KeyValuePair<int, RafterModel> kvp in this.dctRafters_Trimmed)
            {
                kvp.Value.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER, dctConnections, dctLoads);
            }

            // Now draw the support beam file contents
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, doc, db);

            foreach (KeyValuePair<int, SupportModel_SS_Beam> kvp in this.dctSupportBeams)
            {
                kvp.Value.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER, dctConnections, dctLoads);
            }

            // Now draw the connections file contents
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, doc, db);

            foreach (KeyValuePair<int, ConnectionModel> kvp in this.dctConnections)
            {
                kvp.Value.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER, dctConnections, dctLoads);
            }

            // Now draw the loads file contents
            DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db);

            foreach (KeyValuePair<int, LoadModel> kvp in this.dctLoads)
            {
                kvp.Value.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_LOAD_LAYER, dctConnections, dctLoads);
            }



            //// Delete drawing objects on trimmed rafter layer
            //DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_LOAD_LAYER, doc, db);
            //// Draw the support beams
            //foreach (LoadModel item in this.lstLoads)
            //{
            //    item.AddToAutoCADDatabase(db, doc, EE_ROOF_Settings.DEFAULT_LOAD_LAYER);
            //}

            // Now force a redraw
            ModifyAutoCADGraphics.ForceRedraw(db, doc);

        }


        #region Add Elements to ROOF LAYOUT
        private void AddTrimmedRafterToLayout(RafterModel model)
        {

            if(dctRafters_Trimmed is null)
            {
                dctRafters_Trimmed = new Dictionary<int, RafterModel>();
            }

            if(model == null)
            {
                return;
            }

            dctRafters_Trimmed.Add(model.Id, model);
        }

        private void AddUntrimmedRafterToLayout(RafterModel model)
        {

            if (dctRafters_Untrimmed is null)
            {
                dctRafters_Untrimmed = new Dictionary<int, RafterModel>();
            }

            if (model == null)
            {
                return;
            }

            dctRafters_Untrimmed.Add(model.Id, model);
        }

        private void AddBeamToLayout(SupportModel_SS_Beam model)
        {
            if (dctSupportBeams is null)
            {
                dctSupportBeams = new Dictionary<int, SupportModel_SS_Beam>();
            }

            if (model == null)
            {
                return;
            }

            dctSupportBeams.Add(model.Id, model);
        }

        private void AddLoadToLayout(LoadModel model)
        {
            if (dctLoads is null)
            {
                dctLoads = new Dictionary<int, LoadModel>();
            }

            if (model == null)
            {
                return;
            }

            dctLoads.Add(model.Id, model);
        }

        private void AddConnectionToLayout(ConnectionModel model)
        {
            if (dctConnections is null)
            {
                dctConnections = new Dictionary<int, ConnectionModel>();
            }

            if (model == null)
            {
                return;
            }

            dctConnections.Add(model.Id, model);
        }
        #endregion


        #region File Read and Write
        /// <summary>
        /// Reads all the data files for the application
        /// </summary>
        private async Task ReadAllDataFromFiles()
        {
            // read the beams file
            ReadRaftersFile();
            ReadBeamsFile();
            ReadSupportConnectionsFile();
            ReadLoadsFile();
        }

        private IDictionary<int, RafterModel> ReadRaftersFile()
        {
            Dictionary<int, RafterModel> new_dict = new Dictionary<int, RafterModel>();

            // Read the support connections file
            if (File.Exists(EE_ROOF_Settings.DEFAULT_EE_RAFTER_FILENAME))
            {
                // Read the entire beams file
                string[] lines = FileObjects.ReadFromFile(EE_ROOF_Settings.DEFAULT_EE_RAFTER_FILENAME);

                // Parse the individual lines
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Equals("$"))
                        break;
                    else
                    {
                        RafterModel model = new RafterModel(line, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER);
                        this.AddTrimmedRafterToLayout(model);

                        new_dict.Add(model.Id, model);
                    }
                }
            }
            return new_dict;
        }

        private IDictionary<int, SupportModel_SS_Beam> ReadBeamsFile()
        {
            Dictionary<int, SupportModel_SS_Beam> new_dict = new Dictionary<int, SupportModel_SS_Beam>();

            // Read the support connections file
            if (File.Exists(EE_ROOF_Settings.DEFAULT_EE_BEAM_FILENAME))
            {
                // Read the entire beams file
                string[] lines = FileObjects.ReadFromFile(EE_ROOF_Settings.DEFAULT_EE_BEAM_FILENAME);

                // Parse the individual lines
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Equals("$"))
                        break;
                    else
                    {
                        SupportModel_SS_Beam model = new SupportModel_SS_Beam(line, EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);
                        this.AddBeamToLayout(model);

                        new_dict.Add(model.Id, model);
                    }
                }
            }
            return new_dict;
        }
        private IDictionary<int, ConnectionModel> ReadSupportConnectionsFile()
        {
            Dictionary<int, ConnectionModel> new_dict = new Dictionary<int, ConnectionModel>();

            // Read the support connections file
            if (File.Exists(EE_ROOF_Settings.DEFAULT_EE_CONNECTION_FILENAME))
            {
                // Read the entire beams file
                string[] lines = FileObjects.ReadFromFile(EE_ROOF_Settings.DEFAULT_EE_CONNECTION_FILENAME);

                // Parse the individual lines
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Equals("$"))
                        break;
                    else
                    {
                        ConnectionModel model = new ConnectionModel(line, EE_ROOF_Settings.DEFAULT_SUPPORT_CONNECTION_POINT_LAYER);
                        this.AddConnectionToLayout(model);

                        new_dict.Add(model.Id, model);
                    }
                }
            }
            return new_dict;
        }

        private IDictionary<int, LoadModel> ReadLoadsFile()
        {
            Dictionary<int, LoadModel> new_dict = new Dictionary<int, LoadModel>();

            // Read the support connections file
            if (File.Exists(EE_ROOF_Settings.DEFAULT_EE_LOAD_FILENAME))
            {
                // Read the entire beams file
                string[] lines = FileObjects.ReadFromFile(EE_ROOF_Settings.DEFAULT_EE_LOAD_FILENAME);

                // Parse the individual lines
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Equals("$"))
                        break;
                    else
                    {
                        LoadModel model = new LoadModel(line);
                        this.AddLoadToLayout(model);

                        new_dict.Add(model.Id, model);
                    }
                }
            }
            return new_dict;
        }


        /// <summary>
        /// Write all the data objects to file
        /// </summary>
        private async Task WriteAllDataToFiles()
        {
            await Task.Run(() => WriteRaftersToFile());
            await Task.Run(() => WriteBeamsToFile());
            await Task.Run(() => WriteConnectionsToFile());
            await Task.Run(() => WriteLoadsToFile());
        }

        /// <summary>
        /// Write all rafters to file
        /// </summary>
        private void WriteRaftersToFile()
        {
            File.Delete(EE_ROOF_Settings.DEFAULT_EE_RAFTER_FILENAME);

            if (dctRafters_Trimmed.Count > 0)
            {
                foreach (KeyValuePair<int, RafterModel> kvp in dctRafters_Trimmed)
                {
                    FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_RAFTER_FILENAME, kvp.Value.ToFile());
                }
            }

            if(dctRafters_Trimmed.Count == 0)
            {
                FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_RAFTER_FILENAME, "$");  // create an empty file
            }
        }

        private void WriteBeamsToFile()
        {
            File.Delete(EE_ROOF_Settings.DEFAULT_EE_BEAM_FILENAME);

            if (dctSupportBeams.Count > 0)
            {
                foreach (KeyValuePair<int, SupportModel_SS_Beam> kvp in dctSupportBeams)
                {
                    FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_BEAM_FILENAME, kvp.Value.ToFile());
                }
            }

            if (dctSupportBeams.Count == 0)
            {
                FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_BEAM_FILENAME, "$");  // create an empty file
            }
        }

        private void WriteConnectionsToFile()
        {
            File.Delete(EE_ROOF_Settings.DEFAULT_EE_CONNECTION_FILENAME);

            if (dctConnections.Count > 0)
            {
                foreach (KeyValuePair<int, ConnectionModel> kvp in dctConnections)
                {
                    FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_CONNECTION_FILENAME, kvp.Value.ToFile());
                }
            } 
            
            if (dctConnections.Count == 0)
            {
                FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_CONNECTION_FILENAME, "$");  // create an empty file
            }

        }

        private void WriteLoadsToFile() 
        {
            File.Delete(EE_ROOF_Settings.DEFAULT_EE_LOAD_FILENAME);

            if (dctLoads.Count > 0)
            {
                foreach (KeyValuePair<int, LoadModel> kvp in dctLoads)
                {
                    FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_LOAD_FILENAME, kvp.Value.ToFile());
                }
            }

            if (dctLoads.Count == 0)
            {
                FileObjects.AppendStringToFile(EE_ROOF_Settings.DEFAULT_EE_LOAD_FILENAME, "$");  // create an empty file
            }
        }
        #endregion

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


        /// <summary>
        /// Compute the support reactions for the rafters
        /// </summary>
        private void ComputeRafterSupportReactions()
        {
            
            //foreach (KeyValuePair<int, RafterModel> item in dctRafters_Trimmed)
            //{
            //    // For support A reactions
            //    double RA_DL = 0;
            //    double RA_LL = 0;
            //    double RA_RLL = 0;

            //    // For support B reactions
            //    double RB_DL = 0;
            //    double RB_LL = 0;
            //    double RB_RLL = 0;

            //    LoadModel lm1 = new LoadModel(1000, 1000, 1000, item.Value.StartPt, item.Value.StartPt, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);
            //    LoadModel lm2 = new LoadModel(1000, 1000, 1000, item.Value.EndPt, item.Value.EndPt, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);

            //    AddLoadToLayout(lm1);
            //    AddLoadToLayout(lm2);

            //}

            //foreach (KeyValuePair<int, RafterModel> kvp in dctRafters_Trimmed)
            //{
            //    // For support A reactions
            //    double RA_DL = 0;
            //    double RA_LL = 0;
            //    double RA_RLL = 0;

            //    // For support B reactions
            //    double RB_DL = 0;
            //    double RB_LL = 0;
            //    double RB_RLL = 0;

            //    // Then are we determinate?
            //    if (kvp.Value.lst_SupportedBy.Count > 2)
            //    {
            //        // Grab the second connection model in the list
            //        Point3d first_support, second_support;
            //        SupportModel_SS_Beam start_model, end_model;

            //        // If we have two supports
            //        if (kvp.Value.lst_SupportedBy.Count == 2)
            //        {
            //            ConnectionModel conn_start = dctConnections[kvp.Value.lst_SupportedBy[0]];
            //            start_model = dctSupportBeams[conn_start.BelowConn];

            //            ConnectionModel conn_end = dctConnections[kvp.Value.lst_SupportedBy[1]];
            //            end_model = dctSupportBeams[conn_end.BelowConn];

            //            double L = MathHelpers.Magnitude((conn_start.ConnectionPoint).GetVectorTo(conn_end.ConnectionPoint)); // main span length
            //            double a = MathHelpers.Magnitude(kvp.Value.StartPt.GetVectorTo(conn_start.ConnectionPoint));  // left cantilever
            //            double b = MathHelpers.Magnitude((conn_end.ConnectionPoint).GetVectorTo(kvp.Value.EndPt));   // right cantilever

            //            foreach (var item in kvp.Value.lst_UniformLoadModels)
            //            {
            //                if (dctLoads.ContainsKey(kvp.Value.lst_UniformLoadModels[item]))
            //                {
            //                    LoadModel uni_load = dctLoads[kvp.Value.lst_UniformLoadModels[item]];

            //                    double w_dl = uni_load.DL;
            //                    double w_ll = uni_load.LL;
            //                    double w_rll = uni_load.RLL;

            //                    // compute the loads on the cantilevers
            //                    double Ma_DL = 0.5 * w_dl * a * a;
            //                    double Mb_DL = 0.5 * w_dl * b * b;

            //                    double Ma_LL = 0.5 * w_ll * a * a;
            //                    double Mb_LL = 0.5 * w_ll * b * b;

            //                    double Ma_RLL = 0.5 * w_rll * a * a;
            //                    double Mb_RLL = 0.5 * w_rll * b * b;

            //                    // if Ma > Mb then RA = wL/2 + 0.5*(Ma - Mb) / L
            //                    //                 RB = wL/2 - 0.5*(Ma - Mb) / L
            //                    if (Ma_DL > Mb_DL)
            //                    {
            //                        RA_DL += 0.5 * w_dl * L + 0.5 * (Ma_DL - Mb_DL) / L;
            //                        RA_LL += 0.5 * w_ll * L + 0.5 * (Ma_LL - Mb_LL) / L;
            //                        RA_RLL += 0.5 * w_rll * L + 0.5 * (Ma_RLL - Mb_RLL) / L;

            //                        RB_DL += 0.5 * w_dl * L - 0.5 * (Ma_DL - Mb_DL) / L;
            //                        RB_LL += 0.5 * w_ll * L - 0.5 * (Ma_LL - Mb_LL) / L;
            //                        RB_RLL += 0.5 * w_rll * L - 0.5 * (Ma_RLL - Mb_RLL) / L;
            //                    }
            //                    // otherwise
            //                    // if Ma > Mb then RA = wL/2 - 0.5*(Ma - Mb) / L
            //                    //                 RB = wL/2 + 0.5*(Ma - Mb) / L
            //                    else
            //                    {
            //                        RA_DL += 0.5 * w_dl * L - 0.5 * (Mb_DL - Ma_DL) / L;
            //                        RA_LL += 0.5 * w_ll * L - 0.5 * (Mb_LL - Ma_LL) / L;
            //                        RA_RLL += 0.5 * w_rll * L - 0.5 * (Mb_RLL - Ma_RLL) / L;

            //                        RB_DL += 0.5 * w_dl * L + 0.5 * (Mb_DL - Ma_DL) / L;
            //                        RB_LL += 0.5 * w_ll * L + 0.5 * (Mb_LL - Ma_LL) / L;
            //                        RB_RLL += 0.5 * w_rll * L + 0.5 * (Mb_RLL - Ma_RLL) / L;
            //                    }
            //                }

            //                kvp.Value.ReactionsCalculatedCorrectly = true;
            //                LoadModel lm1 = new LoadModel(RA_DL, RA_LL, RA_RLL, conn_start.ConnectionPoint, conn_start.ConnectionPoint, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);
            //                LoadModel lm2 = new LoadModel(RB_DL, RB_LL, RB_RLL, conn_end.ConnectionPoint, conn_end.ConnectionPoint, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);

            //                // Add the reaction to the rafter
            //                kvp.Value.Reaction_SupportA = lm1;
            //                kvp.Value.Reaction_SupportB = lm2;

            //                // Add the loads to the drawing layout
            //                this.AddLoadToLayout(lm1);
            //                this.AddLoadToLayout(lm2);
            //            }
            //        }
            //        else
            //        {

            //        }
            //    }
            //    else
            //    {

            //    }


            //}
        }




        [CommandMethod("EER")]
        public void ShowModalWpfDialogCmd()
        {
            if (ValidateUser() is false)
                return;

            RoofFramingLayout CurrentFoundationLayout = new RoofFramingLayout();
            
            CurrentFoundationLayout.EE_ApplicationSetup();      // Set up layers and linetypes and AutoCAD drawings items
            CurrentFoundationLayout.ReadAllDataFromFiles();     // read existing data from the text files
            Thread.Sleep(1000);
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read


            // Set up our initial work.  If false, end the program.
            if (CurrentFoundationLayout.OnRoofFramingLayoutCreate() is false)
                return;

            CurrentFoundationLayout.FirstLoad = true;

            // Keep reloading the dialog box if we are in preview mode
            while (CurrentFoundationLayout.PreviewMode == true)
            {
                EE_ROOFInputDialog dialog;
                if (CurrentFoundationLayout.FirstLoad)
                {
                    // Use the default values
                    dialog = new EE_ROOFInputDialog(CurrentFoundationLayout, CurrentFoundationLayout.ShouldClose, CurrentFoundationLayout.CurrentDirectionMode);
                    CurrentFoundationLayout.FirstLoad = false;
                }
                else
                {
                    // Otherwise reload the previous iteration values
                    dialog = new EE_ROOFInputDialog(CurrentFoundationLayout, CurrentFoundationLayout.ShouldClose, CurrentFoundationLayout.CurrentDirectionMode);
                }

                CurrentFoundationLayout.ShouldClose = dialog.dialog_should_close;
                CurrentFoundationLayout.IsComplete = dialog.dialog_is_complete;

                if (dialog.dialog_should_close || dialog.dialog_is_complete)
                {
                    dialog.DialogResult = false;
                    break; // exit our loop
                }
                else
                {
                    var result = AcAp.ShowModalWindow(dialog);
                    if (result.Value)
                    {
                        doc.Editor.WriteMessage("\nDialog displayed and successfully entered");
                    }
                }

                if (dialog.DialogResult == false)
                {
                    break;
                }

                CurrentFoundationLayout.CurrentDirectionMode = dialog.current_preview_mode_number;
            }

            CurrentFoundationLayout.ComputeRafterSupportReactions(); // compute the support reactions

            // Need to slow down the program otherwise it races through reading the data and goes straight to drawing.
            Thread.Sleep(1000);
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read
            CurrentFoundationLayout.WriteAllDataToFiles();
        }   


        /// <summary>
        /// The command to add a new beam to the screen.
        /// </summary>
        [CommandMethod("ENB")]
        public void CreateNewSupportBeam()
        {
            if (ValidateUser() is false)
                return;

            RoofFramingLayout CurrentFoundationLayout = new RoofFramingLayout();

            #region Application Setup

            // Set up layers and linetypes and AutoCAD drawings items
            CurrentFoundationLayout.EE_ApplicationSetup();

            #endregion

            // Recreate our data models from file storage and puase slightly
            CurrentFoundationLayout.ReadAllDataFromFiles();
            Thread.Sleep(1000);
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read

            Point3d[] pt = PromptUserforLineEndPoints(db, doc);

            if ((pt == null) || pt.Length < 2)
            {
                doc.Editor.WriteMessage("\nInvalid input for endpoints in CreateNewSupportBeam");
                return;
            }

            // Create the new beam
            SupportModel_SS_Beam beam = new SupportModel_SS_Beam(pt[0], pt[1], EE_ROOF_Settings.DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER);

            // Now check if the support intersects the rafter
            foreach (KeyValuePair<int, RafterModel> kvp in CurrentFoundationLayout.dctRafters_Trimmed)
            {
                IntersectPointData intPt = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(beam.StartPt, beam.EndPt, kvp.Value.StartPt, kvp.Value.EndPt);
                if (intPt is null)
                {
                    continue;
                }

                // if its a valid intersection, make a new connection, add it to the support and the rafter,
                if (intPt.isWithinSegment is true)
                {
                    ConnectionModel support_conn = new ConnectionModel(intPt.Point, kvp.Value.Id, beam.Id);
                    // add the connection to our list
                    CurrentFoundationLayout.AddConnectionToLayout(support_conn);

                    // Update the beam object to indicate the support connection
                    beam.AddConnection(support_conn, CurrentFoundationLayout.dctConnections);

                    // update the rafter object to indicate the support in it
                    kvp.Value.AddConnection(support_conn, CurrentFoundationLayout.dctConnections);


                }
            }

            // Add the beam to the support beams list
            CurrentFoundationLayout.AddBeamToLayout(beam);

            // Finalize computations
            CurrentFoundationLayout.ComputeRafterSupportReactions();

            // Need to slow down the program otherwise it races through reading the data and goes straight to drawing.
            Thread.Sleep(1000);

            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read
            CurrentFoundationLayout.UpdateCurrentHandles();     // then update the handles in the tables
            CurrentFoundationLayout.WriteAllDataToFiles();  // save the work
        }

        /// <summary>
        /// Function to test reading and storing of raftermodel information.  Can we retrieve the model info between commands?
        /// </summary>
        [CommandMethod("ERD")]
        public void ReloadDrawingFromFile()
        {
            if (ValidateUser() is false)
                return;

            RoofFramingLayout CurrentFoundationLayout = new RoofFramingLayout();

            // Set up layers and linetypes and AutoCAD drawings items
            CurrentFoundationLayout.EE_ApplicationSetup();

            // Recreate our data models from file storage and puase slightly
            CurrentFoundationLayout.ReadAllDataFromFiles();
            Thread.Sleep(1000);
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read
            CurrentFoundationLayout.WriteAllDataToFiles();  // save the work

        }

        /// <summary>
        /// Updates all of the handles in the table objects since they change with a drawing is reloaded.  Need to search all of the dictionaries for references 
        /// </summary>
        private void UpdateCurrentHandles()
        {
        //    // Check all records for our Handle references

        //    // first do rafters
        //    if (dctRafters_Trimmed != null)
        //    {
        //        foreach (KeyValuePair<int, RafterModel> item in dctRafters_Trimmed)
        //        {
        //            // get the current handle and the old handle
        //            Handle old_handle = item.Value.OldHandle;
        //            Handle current_handle = item.Value.CurrentHandle;

        //            // search connections, and loads for references to the old handle
        //            foreach (KeyValuePair<Handle, ConnectionModel> conn in dctConnections)
        //            {
        //                // check the above handle for the connection
        //                if (conn.Value.AboveConn == old_handle)
        //                {
        //                    dctConnections[conn.Key].AboveConn = current_handle;
        //                }
        //                // check the below handle for the connection
        //                if (conn.Value.BelowConn == old_handle)
        //                {
        //                    dctConnections[conn.Key].BelowConn = current_handle;
        //                }
        //            }

        //            // then set the old handle on the rafter itself
        //            dctRafters_Trimmed[item.Key].OldHandle = current_handle;
        //        }
        //    }
            

        //    // second do beams
        //    if(dctSupportBeams != null)
        //    {
        //        foreach (KeyValuePair<Handle, SupportModel_SS_Beam> item in dctSupportBeams)
        //        {
        //            // get the current handle and the old handle
        //            Handle old_handle = item.Value.OldHandle;
        //            Handle current_handle = item.Value.CurrentHandle;

        //            // search connections, and loads for references to the old handle
        //            foreach (KeyValuePair<Handle, ConnectionModel> conn in dctConnections)
        //            {
        //                // check the above handle for the connection
        //                if (conn.Value.AboveConn == old_handle)
        //                {
        //                    dctConnections[conn.Key].AboveConn = current_handle;
        //                }
        //                // check the below handle for the connection
        //                if (conn.Value.BelowConn == old_handle)
        //                {
        //                    dctConnections[conn.Key].BelowConn = current_handle;
        //                }
        //            }
        //            dctSupportBeams[item.Key].OldHandle = current_handle;
        //        }

        //    }


        //    // third do connections
        //    if (dctConnections != null)
        //    {
        //        foreach (KeyValuePair<Handle, ConnectionModel> item in dctConnections)
        //        {
        //            // get the current handle and the old handle
        //            Handle old_handle = item.Value.OldHandle;
        //            Handle current_handle = item.Value.CurrentHandle;

        //            // search rafters and beams for references to the old handle
        //            foreach (KeyValuePair<Handle, RafterModel> rafter in dctRafters_Trimmed)
        //            {
        //                if (rafter.Value.lst_SupportConnections == null)
        //                    continue;

        //                for (int i = 0; i < rafter.Value.lst_SupportConnections.Count; i++)
        //                {
        //                    if (rafter.Value.lst_SupportConnections[i] == old_handle)
        //                    {
        //                        dctRafters_Trimmed[rafter.Key].lst_SupportConnections[i] = current_handle;
        //                    }
        //                }
        //            }

        //            // search rafters and beams for references to the old handle
        //            foreach (KeyValuePair<Handle, SupportModel_SS_Beam> beam in dctSupportBeams)
        //            {
        //                if (beam.Value.lst_SupportConnections == null)
        //                    continue;

        //                for (int i = 0; i < beam.Value.lst_SupportConnections.Count; i++)
        //                {
        //                    if (beam.Value.lst_SupportConnections[i] == old_handle)
        //                    {
        //                        dctSupportBeams[beam.Key].lst_SupportConnections[i] = current_handle;
        //                    }
        //                }
        //            }
        //            dctConnections[item.Key].OldHandle = current_handle;
        //        }
        //    }
        }


        [CommandMethod("EEC")]
        public void PerformRafterReactionCalculations()
        {
            if (ValidateUser() is false)
                return;

            RoofFramingLayout CurrentFoundationLayout = new RoofFramingLayout();

            #region Application Setup

            // Set up layers and linetypes and AutoCAD drawings items
            CurrentFoundationLayout.EE_ApplicationSetup();

            #endregion

            // Recreate our data models from file storage and puase slightly
            CurrentFoundationLayout.ReadAllDataFromFiles();
            Thread.Sleep(1000);
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read

            // Do our calculations
            foreach (KeyValuePair<int,RafterModel> item in CurrentFoundationLayout.dctRafters_Trimmed)
            {
                // Get a rafter
                RafterModel rafter = item.Value;

                // check if the rafter is determinant
                if (rafter.IsDeterminate)
                {
                    // compute the reaction values
                    if(rafter.lst_SupportedBy.Count == 2)
                    {
                        ConnectionModel first_conn = CurrentFoundationLayout.dctConnections[rafter.lst_SupportedBy[0]];
                        ConnectionModel second_conn = CurrentFoundationLayout.dctConnections[rafter.lst_SupportedBy[0]];

                        Point3d first_support_point = first_conn.ConnectionPoint;
                        Point3d second_support_point = second_conn.ConnectionPoint;

                        Point3d start_beam_point = rafter.StartPt;
                        Point3d end_beam_point = rafter.EndPt;

                        // Sort the points baed on distance from StartPt
                        double dist1 = MathHelpers.Magnitude(start_beam_point.GetVectorTo(first_support_point)); // dist from start of beam to first support connection
                        double dist2 = MathHelpers.Magnitude(start_beam_point.GetVectorTo(second_support_point)); // dist from start of beam to second support connection

                        Point3d temp_point;
                        ConnectionModel temp_conn;
                        if(dist1 > dist2)
                        {
                            // Swap the points
                            temp_point = first_support_point;
                            first_support_point = second_support_point;
                            second_support_point = temp_point;

                            // Swap the connections
                            temp_conn = first_conn;
                            first_conn = second_conn;
                            second_conn = temp_conn;
                        }

                        double L = MathHelpers.Magnitude(first_support_point.GetVectorTo(second_support_point)); // main span length
                        double a = MathHelpers.Magnitude(start_beam_point.GetVectorTo(first_support_point));  // left cantilever
                        double b = MathHelpers.Magnitude(second_support_point.GetVectorTo(end_beam_point));   // right cantilever

                        // For support A reactions
                        double RA_DL = 0;
                        double RA_LL = 0;
                        double RA_RLL = 0;

                        // For support B reactions
                        double RB_DL = 0;
                        double RB_LL = 0;
                        double RB_RLL = 0;

                        // Test with the first uniform load value
 //                      if (dctLoads.ContainsKey(rafter.lst_UniformLoadModels[0]))
 //                       {
                            LoadModel uni_load = dctLoads[rafter.lst_UniformLoadModels[0]];

                            double w_dl = uni_load.DL;
                            double w_ll = uni_load.LL;
                            double w_rll = uni_load.RLL;

                            // compute the loads on the cantilevers
                            double Ma_DL = 0.5 * w_dl * a * a;
                            double Mb_DL = 0.5 * w_dl * b * b;

                            double Ma_LL = 0.5 * w_ll * a * a;
                            double Mb_LL = 0.5 * w_ll * b * b;

                            double Ma_RLL = 0.5 * w_rll * a * a;
                            double Mb_RLL = 0.5 * w_rll * b * b;

                            // if Ma > Mb then RA = wL/2 + 0.5*(Ma - Mb) / L
                            //                 RB = wL/2 - 0.5*(Ma - Mb) / L
                            if (Ma_DL > Mb_DL)
                            {
                                RA_DL += 0.5 * w_dl * L + 0.5 * (Ma_DL - Mb_DL) / L;
                                RA_LL += 0.5 * w_ll * L + 0.5 * (Ma_LL - Mb_LL) / L;
                                RA_RLL += 0.5 * w_rll * L + 0.5 * (Ma_RLL - Mb_RLL) / L;

                                RB_DL += 0.5 * w_dl * L - 0.5 * (Ma_DL - Mb_DL) / L;
                                RB_LL += 0.5 * w_ll * L - 0.5 * (Ma_LL - Mb_LL) / L;
                                RB_RLL += 0.5 * w_rll * L - 0.5 * (Ma_RLL - Mb_RLL) / L;
                            }
                            // otherwise
                            // if Ma > Mb then RA = wL/2 - 0.5*(Ma - Mb) / L
                            //                 RB = wL/2 + 0.5*(Ma - Mb) / L
                            else
                            {
                                RA_DL += 0.5 * w_dl * L - 0.5 * (Mb_DL - Ma_DL) / L;
                                RA_LL += 0.5 * w_ll * L - 0.5 * (Mb_LL - Ma_LL) / L;
                                RA_RLL += 0.5 * w_rll * L - 0.5 * (Mb_RLL - Ma_RLL) / L;

                                RB_DL += 0.5 * w_dl * L + 0.5 * (Mb_DL - Ma_DL) / L;
                                RB_LL += 0.5 * w_ll * L + 0.5 * (Mb_LL - Ma_LL) / L;
                                RB_RLL += 0.5 * w_rll * L + 0.5 * (Mb_RLL - Ma_RLL) / L;
                            }

                            LoadModel lmA = new LoadModel(RA_DL, RA_LL, RA_RLL, first_support_point, first_support_point, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);
                            LoadModel lmB = new LoadModel(RB_DL, RB_LL, RB_RLL, second_support_point, second_support_point, LoadTypes.LOAD_TYPE_CONCENTRATED_LOAD);

                            DrawMtext(db, doc, first_support_point, lmA.ToString(), 4, EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER);
                            DrawMtext(db, doc, first_support_point, lmB.ToString(), 4, EE_ROOF_Settings.DEFAULT_ROOF_CALCULATIONS_LAYER);
 //                       }
                    }
                }
            }
            // draw reaction values on the drawing
            CurrentFoundationLayout.DrawAllRoofFraming();       // redraw the data now that it's read
            CurrentFoundationLayout.WriteAllDataToFiles();      // save the work

        }

    }
}

 