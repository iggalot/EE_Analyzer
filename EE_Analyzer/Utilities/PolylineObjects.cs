using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_Analyzer.Utilities
{
    public static class PolylineObjects
    {
        // Return a list of vertices for a selected polyline
        public static List<Point2d> GetVertices(Polyline pl)
        {
            List<Point2d> vertices = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d pt = pl.GetPoint2dAt(i);
                vertices.Add(pt);
            }

            return vertices;
        }

        /// <summary>
        /// Offsets an Autocad Polyline by a specified distance
        /// </summary>
        /// <param name="pline">polyline object to offset</param>
        /// <param name="offset_dist">"+" makes the object bigger and "-" makes it smaller</param>
        public static Polyline OffsetPolyline(Polyline pline, double offset_dist, BlockTable bt, BlockTableRecord btr)
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

           // doc.Editor.WriteMessage("offsetting foundation line by: " + offset_dist);

            Polyline newPline = new Polyline();
            DBObjectCollection objCollection;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    objCollection = pline.GetOffsetCurves(offset_dist);

                    // Step through the new objects created
                    foreach (Entity ent in objCollection)
                    {
                        // Add each offset object
                        btr.AppendEntity(ent);
                        trans.AddNewlyCreatedDBObject(ent, true);

                        //// This time access the properties directly

                        //doc.Editor.WriteMessage("\nType:        " +

                        //  ent.GetType().ToString());

                        //doc.Editor.WriteMessage("\n  Handle:    " +

                        //  ent.Handle.ToString());

                        //doc.Editor.WriteMessage("\n  Layer:      " +

                        //  ent.Layer.ToString());

                        //doc.Editor.WriteMessage("\n  Linetype:  " +

                        //  ent.Linetype.ToString());

                        //doc.Editor.WriteMessage("\n  Lineweight: " +

                        //  ent.LineWeight.ToString());

                        //doc.Editor.WriteMessage("\n  ColorIndex: " +

                        //  ent.ColorIndex.ToString());

                        //doc.Editor.WriteMessage("\n  Color:      " +

                        //  ent.Color.ToString());


                    }

                    // capture the offset polyline and return
                    newPline = objCollection[0] as Polyline;

                    // Save the new objects to the database
                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("Error offseting polyline: " + ex.Message);
                    trans.Abort();
                    throw new System.Exception("Error offseting polyline object");
                }
            }

            return newPline ;
        }
        public static void MovePolylineToLayer(Polyline obj, string layer_name, BlockTable bt, BlockTableRecord btr)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(layer_name))
                    {
                        ed.WriteMessage("\nLayer [" + layer_name + " not found in MovePolylineToLayer");
                        throw new System.Exception("Layer [" + layer_name + "] not currently loaded");
                    }

                    // Get the layer's id and use it
                    ObjectId lid = lt[layer_name];

                    //obj.LayerId = lid;
                    Entity ent = trans.GetObject(obj.Id, OpenMode.ForWrite) as Entity;
                    ent.LayerId = lid;

                    trans.Commit();

                    //ed.WriteMessage("\n-- Polyline [" + obj.Handle.ToString() + "] successfully moved to layer [" + layer_name + "]");
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError moving polyline [" + obj.Handle.ToString() + "] to [" + layer_name + "]");
                    trans.Abort();
                }
            }
        }

        public static void PolylineSetLinetype(Polyline obj, string linetype_name, BlockTable bt, BlockTableRecord btr)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LinetypeTable lt = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                    if (!lt.Has(linetype_name))
                    {
                        ed.WriteMessage("\nLinetype [" + linetype_name + " not found in PolylineSetLinetype");
                        throw new System.Exception("Linetype [" + linetype_name + "] not currently loaded");
                    }

                    ObjectId ltid = lt[linetype_name];
                    //obj.LinetypeId = lt[linetype_name];

                    //obj.LayerId = lid;
                    Entity ent = trans.GetObject(obj.Id, OpenMode.ForWrite) as Entity;
                    ent.LinetypeId = ltid;

                    trans.Commit();

                    //ed.WriteMessage("\n-- Polyline [" + obj.Handle.ToString() + "] successfully changed to linetype [" + linetype_name + "]");

                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError changing linetypes for polyline [" + obj.Handle.ToString() + "] to [" + linetype_name + "]");
                    trans.Abort();
                }
            }
        }

        /// <summary>
        /// Structure for storing data used by the reversing winding order function for a polyline
        /// </summary>
        struct PerVertexData
        {
            public Point2d pt;
            public double bulge;
            public double startWidth;
            public double endWidth;
        }

        /// <summary>
        /// Determine if a polyline is wound clockwise
        /// </summary>
        /// <param name="pline"></param>
        /// <returns></returns>
        public static bool PolylineIsWoundClockwise(Polyline pline)
        {
            double sum = 0.0;
            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                Point2d pt1 = pline.GetPoint2dAt(i);
                Point2d pt2 = pline.GetPoint2dAt((i + 1) % pline.NumberOfVertices);
                sum += (pt2.X - pt1.X) * (pt2.Y + pt1.Y);
            }
            return sum > 0.0;
        }

        /// <summary>
        /// Utility function to reverse the direction of a polyline.
        /// </summary>
        /// <param name="pline"></param>
        public static Polyline ReversePolylineDirection(Polyline pline)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(pline.ObjectId, OpenMode.ForRead);
                var pl = obj as Polyline;

                try
                {
                    if (pl != null)
                    {
                        // Collect our per-vertex data
                        List<PerVertexData> vertData =
                          new List<PerVertexData>(pl.NumberOfVertices);

                        for (int i = 0; i < pl.NumberOfVertices; i++)
                        {
                            PerVertexData pvd = new PerVertexData();
                            pvd.bulge = (i > 0 ? pl.GetBulgeAt(i - 1) : 0);
                            pvd.startWidth = (i > 0 ? pl.GetStartWidthAt(i - 1) : 0);
                            pvd.endWidth = (i > 0 ? pl.GetEndWidthAt(i - 1) : 0);
                            pvd.pt = pl.GetPoint2dAt(i);

                            vertData.Add(pvd);
                        }

                        // Now let's make sure we can edit the polyline
                        pl.UpgradeOpen();

                        // Write the data back to the polyline, but in
                        // reverse order

                        for (int i = 0; i < pl.NumberOfVertices; i++)
                        {
                            PerVertexData pvd =
                            vertData[pl.NumberOfVertices - (i + 1)];

                            pl.SetPointAt(i, pvd.pt);
                            pl.SetBulgeAt(i, -pvd.bulge);
                            pl.SetStartWidthAt(i, pvd.endWidth);
                            pl.SetEndWidthAt(i, pvd.startWidth);
                        }
                    }
                    trans.Commit();
                }
                catch (System.Exception ex) {
                    doc.Editor.WriteMessage("\nError encountered while reversing polyline winding direction: " + ex.Message);
                    trans.Abort();
                    return null;
                }

                return pl;
            }
        }

        public static Point3d[] FindTwoLongestNonParallelSegmentsOnPolyline(Polyline pline)
        {
            // create array to store the four points  points[0] and points[1] contain the longest and points[2]
            // and points[3] contain the second longest.
            Point3d[] points = new Point3d[4];
            int numVertices = pline.NumberOfVertices;

            if(numVertices < 3)
            {
                throw new System.Exception("Polyline must have at least two segements (requires three vertices)");
            }

            double max_length_1 = 0.0; // the longest length segment
            double max_length_2 = 0.0; // the second longest length segment

            Point3d p1_longest_1 = pline.GetPoint3dAt(0);
            Point3d p2_longest_1 = pline.GetPoint3dAt(1);

            Point3d p1_longest_2 = pline.GetPoint3dAt(0);
            Point3d p2_longest_2 = pline.GetPoint3dAt(1);

            for (int i = 2; i < numVertices; i++)
            {
                Point3d p1 = pline.GetPoint3dAt(i % numVertices);
                Point3d p2 = pline.GetPoint3dAt((i + 1) % numVertices);
                double length = MathHelpers.Distance3DBetween(p1, p2);

                if(length >= max_length_1)
                {
                    // check if our test segment is not parallel to the current longest
                    if(EE_Helpers.GetSlopeOfPts(p1,p2) != EE_Helpers.GetSlopeOfPts(p1_longest_1, p2_longest_1))
                    {
                        // first move the first point to the second
                        p1_longest_2 = p1_longest_1;
                        p2_longest_2 = p2_longest_1;
                        max_length_2 = max_length_1;

                        // then store the new longest info in the first
                        p1_longest_1 = p1;
                        p2_longest_1 = p2;
                        max_length_1 = length;
                    }

                } else if (length >= max_length_2)
                {
                    // check if our test segment is not parallel to the current longest
                    if (EE_Helpers.GetSlopeOfPts(p1, p2) != EE_Helpers.GetSlopeOfPts(p1_longest_2, p2_longest_2))
                    {
                        // first point is unchanged but second point needs to be changed
                        p1_longest_2 = p1;
                        p2_longest_2 = p2;
                        max_length_2 = length;
                    }
                }
            }

            // Check to make sure we found a non-zero length
            if(max_length_1 == 0.0 || max_length_2 == 0.0) 
            {
                throw new System.Exception("Polyline segment lengths all returned a zero value");
            }

            points[0] = p1_longest_1;
            points[1] = p2_longest_1;
            points[2] = p1_longest_2;
            points[3] = p2_longest_2;

            return points;
        }

        /// <summary>
        /// Routine to determine if a linesegment of a polyline partially overlaps another line between two points A and B
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="A">Start point of our line object</param>
        /// <param name="B">End point of our line object</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private static int HorizontalTestLineOverLapPolyline(Polyline pline, Point2d A, Point2d B)
        {

            if (pline.NumberOfVertices < 2)
            {
                throw new System.Exception("Polyline must have at least 2 vertices");
            }

            int overlap_case = 0;

            Point2d temp;
            // swap A and B so that A is always on the left
            if (A.X > B.X)
            {
                temp = A;
                A = B;
                B = temp;
            }

            var C = pline.GetPoint2dAt(0);
            var D = pline.GetPoint2dAt(1);

            // swap C and D so that C is always on the left
            if (C.X > D.X)
            {
                temp = C;
                C = D;
                D = temp;
            }

            // Check endpoints
            // Case 1: C and D both within line AB
            // A =========== C ----------- D ========= B or
            //     -- create line segments AC and DB
            if ((C.X > A.X) && (C.X < B.X) && (D.X > A.X) && (D.X < B.X))
            {
                overlap_case = 1;
            }

            // Case 2: C within and D is not within AB
            // A =========== C ----------- B --------- D or
            //     -- create line segment AC and move B to D
            if ((C.X > A.X) && (C.X < B.X) && (D.X > A.X) && (D.X > B.X))
            {
                overlap_case = 2;
            }

            // Case 3: C is not within and D is within AB
            // A =========== D ----------- B --------- C or
            //     -- create line segment AD and move B to C
            if ((C.X > A.X) && (C.X > B.X) && (D.X > A.X) && (D.X < B.X))
            {
                overlap_case = 3;
            }

            // Case 4: Both C and D are outside AB
            // C ----------- D   A ========= B or
            // A =========== B   C ---------- D  
            //     -- create line AB
            if (((C.X < A.X) && (C.X < B.X) && (D.X < A.X) && (D.X < B.X)) ||
                ((C.X > A.X) && (C.X > B.X) && (D.X > A.X) && (D.X > B.X))
                )
            {
                overlap_case = 4;
            }

            return overlap_case;
        }
    }
}
