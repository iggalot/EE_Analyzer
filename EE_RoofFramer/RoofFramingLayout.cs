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
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Windows;
using EE_Analyzer;
using EE_Analyzer.Utilities;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using EE_RoofFramer.Models;

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


        public Polyline ROOF_PERIMETER_POLYLINE { get; set; } = null;
        public Polyline ROOF_BOUNDARY_BOX { get; set; } = new Polyline();

        // Holds the basis point for the grade beam grid
        public Point3d ROOF_FRAMING_BASIS_POINT { get; set; } = new Point3d();

        List<RafterModel> lstRafters_Untrimmed { get; set; } = new List<RafterModel>();
        List<RafterModel> lstRafters_Trimmed { get; set; } = new List<RafterModel>();


        public RoofFramingLayout()
        {

        }

        public bool DrawRoofFramingDetails(bool preview_mode, bool should_close, int mode_number)
        {
            if(mode_number >= ROOF_PERIMETER_POLYLINE.NumberOfVertices)
            {
                CurrentDirectionMode = -2;
            } else
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

            // Get our AutoCAD API objects
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            Point3d start = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(0);
            Point3d end = ROOF_PERIMETER_POLYLINE.GetPoint3dAt(1);

            // If we are in preview mode we will just draw centerlines as a marker.
            // Clear our temporary layer prior to drawing temporary items

            if (PreviewMode is true)
            {
                // If we are in preview mode we will just draw centerlines as a marker.
                // Clear our temporary layer prior to drawing temporary items
                DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

                DoPreviewMode(doc, db, start, end);

                ModifyAutoCADGraphics.ForceRedraw(db, doc);

                IsComplete = false;
                return IsComplete;
            }

            //// Clear our temporary layer
 //           DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

            #region Trim Rafters
            //foreach(RafterModel model in lstRafters_Untrimmed)
            //{
            //    List<Point3d> intPt = EE_Helpers.FindPolylineIntersectionPoints(new Line(model.StartPt, model.EndPt), ROOF_BOUNDARY_BOX);

            //    foreach(Point3d pt in intPt)
            //    {
            //        DrawCircle(pt, 6, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);
            //    }

            //    RafterModel new_model = new RafterModel(model.StartPt, intPt[0]);
            //    lstRafters_Trimmed.Add(new_model);
            //}

            ////// Clear our temporary layer
            //DeleteAllObjectsOnLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db);

            //foreach (var item in lstRafters_Trimmed)
            //{
            //    MoveLineToLayer(OffsetLine(new Line(item.StartPt, item.EndPt), 0) as Line, EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER);  // Must create the centerline this way to have it added to the AutoCAD database
            //}

            //// Now force a redraw
            //using (Transaction trans = db.TransactionManager.StartTransaction())
            //{
            //    // Force a redraw of the screen?
            //    doc.TransactionManager.EnableGraphicsFlush(true);
            //    doc.TransactionManager.QueueForGraphicsFlush();
            //    Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            //    trans.Commit();
            //}
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








            // Do other stuff here to finalize drawing













            ModifyAutoCADGraphics.ForceRedraw(db, doc);
            // Indicate that the dialog should close.
            ShouldClose = true;
            IsComplete = true;

            return IsComplete;
        }

        private void DoPreviewMode(Document doc, Database db, Point3d start, Point3d end)
        {
            lstRafters_Untrimmed.Clear();

            switch (CurrentDirectionMode)
            {
                case -2:
                    {
                        CreateHorizontalRafters(db, doc, start, end);
                        break;
                    }

                case -1:
                    {
                        CreateVerticalRafters(db, doc, start, end);
                        break;
                    }

                default:
                    {
                        CreatePerpendicularRafters(db, doc, CurrentDirectionMode);
                        break;
                    }
            }

            foreach (var item in lstRafters_Untrimmed)
            {
                MoveLineToLayer(OffsetLine(new Line(item.StartPt, item.EndPt), 0) as Line, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER);  // Must create the centerline this way to have it added to the AutoCAD database
            }

            // Now force a redraw
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // Force a redraw of the screen?
                doc.TransactionManager.EnableGraphicsFlush(true);
                doc.TransactionManager.QueueForGraphicsFlush();
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
                trans.Commit();
            }

        }


        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreateVerticalRafters(Database db, Document doc, Point3d start, Point3d end)
        {
            Vector3d dir_unit_vec = new Vector3d(0, 1, 0);
            // get unit vect perpendicular to selected edge
            Vector3d perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_unit_vec, (-1.0) * new Vector3d(0, 0, 1)));

            int num_spaces_from_intpt_to_start = 0;
            int num_spaces_from_intpt_to_end = 0;

            // find furthest point on perpendicular line
            IntersectPointData intPt = null;
            Point3d current_vertex = new Point3d(0, 0, 0);
            double max_length = 0;

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

                RafterModel model = new RafterModel(start_pt, new_pt);

                lstRafters_Untrimmed.Add(model);
            }

            // Draw rafters from intersect point to end point -- start at index of 1 so we dont double draw the longest rafter
            for (int i = 1; i < num_spaces_from_intpt_to_end - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_end) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(0, 1, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(1).Y - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y));

                RafterModel model = new RafterModel(start_pt, new_pt);

                lstRafters_Untrimmed.Add(model);
            }
        }


        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreateHorizontalRafters(Database db, Document doc, Point3d start, Point3d end)
        {
            Vector3d dir_unit_vec = new Vector3d(1,0,0);
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
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(1,0,0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(3).X - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X));

                RafterModel model = new RafterModel(start_pt, new_pt);

                lstRafters_Untrimmed.Add(model);
            }

            // Draw rafters from intersect point to end point -- start at index of 1 so we dont double draw the longest rafter
            for (int i = 1; i < num_spaces_from_intpt_to_end - 1; i++)
            {
                Point3d start_pt = MathHelpers.Point3dFromVectorOffset(temp_vertex, MathHelpers.Normalize(dir_vec_intpt_to_end) * i * rafter_spacing);
                Point3d new_pt = MathHelpers.Point3dFromVectorOffset(start_pt, new Vector3d(1, 0, 0) * (ROOF_BOUNDARY_BOX.GetPoint3dAt(3).X - ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X));

                RafterModel model = new RafterModel(start_pt, new_pt);

                lstRafters_Untrimmed.Add(model);
            }


        }


        /// <summary>
        /// Creates rafters that are perpendicular to the edge between start and end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void CreatePerpendicularRafters(Database db, Document doc, int start_node_index)
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
                if(current_vertex == start || current_vertex == end)
                {
                    continue;
                }

                Point3d temp_pt = MathHelpers.Point3dFromVectorOffset(current_vertex, perp_unit_vec * 100);
                IntersectPointData current_intPt = EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start, end, current_vertex, temp_pt);

                double length = (MathHelpers.Magnitude(current_intPt.Point.GetVectorTo(current_vertex)));

                // Is this the longest?
                if (length > max_length){
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

            //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
            //DrawCircle(end_pt, 20, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
            DrawLine(start_pt, end_pt, EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, "HIDDEN2");
            
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
            IntersectPointData ipd_bb1= EE_Helpers.FindPointOfIntersectLines_FromPoint3d(start_pt, end_pt, bb1_a, bb1_b);
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
            if((v0_bb0.X * v1_bb1.X >= 0) && (v0_bb0.Y * v1_bb1.Y >= 0) && (v0_bb0.Z * v1_bb1.Z >= 0))
            {
                lst_v_side1.Add(v1_bb1);
            } else
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
            double  max_list1 = 0;
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

                RafterModel model = new RafterModel(start_temp_pt, end_temp_pt);
                //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
                lstRafters_Untrimmed.Add(model);
            }

            // Draw rafters from intersect point to start point -- start at index 1 so we dont duplicate the middle rafter
            for (int i = 1; i < num_rafters_side2; i++)
            {
                Point3d start_temp_pt = MathHelpers.Point3dFromVectorOffset(start_pt, uv_list2 * i * rafter_spacing);
                Point3d end_temp_pt = MathHelpers.Point3dFromVectorOffset(end_pt, uv_list2 * i * rafter_spacing);

                RafterModel model = new RafterModel(start_temp_pt, end_temp_pt);
                //DrawCircle(start_pt, 10, EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER);
                lstRafters_Untrimmed.Add(model);
            }
        }

        public void OnRoofFramingLayoutCreate()
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

            ROOF_PERIMETER_POLYLINE = ProcessFoundationPerimeter(db, edt, result);

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
            ROOF_BOUNDARY_BOX = CreateRoofBoundingBox(db, edt, lstVertices);
            doc.Editor.WriteMessage("\n-- Creating roof bounding box.");
            if (ROOF_BOUNDARY_BOX is null)
            {
                throw new System.Exception("Invalid roof boundary box created.");
            }
            #endregion

            #region Zoom Control to improve Autocad View
            // Zoom to the exents of the bounding box
            double zoom_factor = 0.02;
            Point3d zp1 = new Point3d(ROOF_BOUNDARY_BOX.GetPoint3dAt(0).X * (1 - zoom_factor), ROOF_BOUNDARY_BOX.GetPoint3dAt(0).Y * (1 - zoom_factor), 0);
            Point3d zp2 = new Point3d(ROOF_BOUNDARY_BOX.GetPoint3dAt(2).X * (1 + zoom_factor), ROOF_BOUNDARY_BOX.GetPoint3dAt(2).Y * (1 + zoom_factor), 0);

            ZoomWindow(db, doc, zp1, zp2);
            #endregion

            #region Find the Insert (Basis) Point the GradeBeams
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
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_BOUNDINGBOX_LAYER, doc, db, 4); // cyan
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER, doc, db, 3); // green
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER, doc, db, 2); // yellow

            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_HIPVALLEY_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_PURLIN_LAYER, doc, db, 140); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_RIDGE_LAYER, doc, db, 4); // blue
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_TEXTS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_DIMENSIONS_LAYER, doc, db, 2); // yellow
            CreateLayer(EE_ROOF_Settings.DEFAULT_ROOF_ANNOTATION_LAYER, doc, db, 1); // red
            CreateLayer(EE_ROOF_Settings.DEFAULT_TEMPORARY_GRAPHICS_LAYER, doc, db, 2);  // yellow

            //Create the EE dimension style
            CreateEE_DimensionStyle(EE_ROOF_Settings.DEFAULT_EE_DIMSTYLE_NAME);
        }


        private Polyline CreateRoofBoundingBox(Database db, Editor edt, List<Point2d> lstVertices)
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

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    trans.Commit();

                    MovePolylineToLayer(pl, EE_ROOF_Settings.DEFAULT_ROOF_BOUNDINGBOX_LAYER, bt, btr);

                    return pl;
                }
                catch (System.Exception ex)
                {
                    edt.WriteMessage("\nError encountered drawing roof boundary box line: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        private Polyline ProcessFoundationPerimeter(Database db, Editor edt, PromptEntityResult result)
        {
            Polyline roofPerimeterPolyline = new Polyline();
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
                            edt.WriteMessage("\nPolyline must have at least three sides.  The selected polygon only has " + lstVertices.Count);
                            trans.Abort();
                            return null;
                        }
                        else
                        {
                            // Check that the polyline is in a clockwise winding.  If not, then reverse the polyline direction
                            // -- necessary for the offset functions later to work correctly.
                            if (!PolylineIsWoundClockwise(roofPerimeterPolyline))
                            {
                                edt.WriteMessage("\nReversing polyline direction to make it Clockwise");
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
                        edt.WriteMessage("\nError encountered processing roof polyline winding direction: " + ex.Message);
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

        [CommandMethod("EER")]
        public void ShowModalWpfDialogCmd()
        {
            FirstLoad = true;

            // rudimentary copy protection based on current time 
            if (EE_ROOF_Settings.APP_REGISTRATION_DATE < DateTime.Now.AddDays(-1 * EE_ROOF_Settings.DAYS_UNTIL_EXPIRES))
            {
                // Update the expires 
                MessageBox.Show("Time has expired on this application. Contact the developer for a new licensed version.");
                return;
            }

            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            #region Application Setup
            // Set up layers and linetypes and AutoCAD drawings items
            EE_ApplicationSetup(doc, db);
            #endregion

            RoofFramingLayout CurrentFoundationLayout = new RoofFramingLayout();
            CurrentFoundationLayout.OnRoofFramingLayoutCreate();

            // Keep reloading the dialog box if we are in preview mode
            while (PreviewMode = true)
            {
                EE_ROOFInputDialog dialog;
                if (FirstLoad)
                {
                    // Use the default values
                    dialog = new EE_ROOFInputDialog(CurrentFoundationLayout, ShouldClose, CurrentDirectionMode);
                    FirstLoad = false;
                }
                else
                {
                    // Otherwise reload the previous iteration values
                    dialog = new EE_ROOFInputDialog(CurrentFoundationLayout, ShouldClose, CurrentDirectionMode);
                }

                ShouldClose = dialog.dialog_should_close;
                IsComplete = dialog.dialog_is_complete;

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
                        edt.WriteMessage("\nDialog displayed and successfully entered");
                    }
                }

                if (dialog.DialogResult == false)
                {
                    break;
                }

                CurrentDirectionMode = dialog.current_preview_mode_number;

            }
        }
    }
}
