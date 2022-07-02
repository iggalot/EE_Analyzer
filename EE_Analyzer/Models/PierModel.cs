using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.PolylineObjects;
using static EE_Analyzer.Utilities.HatchObjects;




namespace EE_Analyzer.Models
{
    public enum PierShapes
    {
        PIER_UNDEFINED = 0,
        PIER_CIRCLE = 1,
        PIER_SQUARE = 2,
        PIER_RECTANGLE = 3,
    }
    public class PierModel
    {
        public int Id { get; set; }
        public Point3d Location { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public PierShapes PierShape { get; set; }

        public PierModel(Point3d pt, PierShapes shape, double width, double height, int id)
        {
            Id = id;
            PierShape = shape;
            Width = width;
            Height = height;
            Location = pt;
        }

        /// <summary>
        /// Creates the grade beam object in AutoCAD and creates our GradeBeamModel object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        public void AddToAutoCADDatabase(Database db, Document doc)
        {

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Add a label
                    DrawMtext(db, doc, 
                        new Point3d(Location.X + Width / 2.0 * Math.Cos(-0.785), Location.Y + Width / 2.0 * Math.Sin(-0.785), 0),
                        "P" + Id.ToString(), 5, EE_Settings.DEFAULT_PIER_TEXTS_LAYER);

                    if (PierShape == PierShapes.PIER_CIRCLE)
                    {
                        DrawCircle(Location, Width / 2.0, EE_Settings.DEFAULT_PIER_LAYER, "HIDDEN2");  // outer pier diameter
                        //DrawCircle(Location, (Width * 0.9) / 2.0, EE_Settings.DEFAULT_PIER_LAYER, "HIDDEN2"); // inner pier circle
                        AddCircularHatch(Location, Width * 0.9, EE_Settings.DEFAULT_PIER_HATCH_TYPE, EE_Settings.DEFAULT_HATCH_PATTERNSCALE);
                    }
                    else if (PierShape == PierShapes.PIER_RECTANGLE)
                    {
                        Polyline pline = new Polyline();
                        pline.AddVertexAt(0, new Point2d(Location.X - Width / 2.0, Location.Y - Height / 2.0), 0, 0, 0);
                        pline.AddVertexAt(1, new Point2d(Location.X - Width / 2.0, Location.Y + Height / 2.0), 0, 0, 0);
                        pline.AddVertexAt(2, new Point2d(Location.X + Width / 2.0, Location.Y + Height / 2.0), 0, 0, 0);
                        pline.AddVertexAt(3, new Point2d(Location.X + Width / 2.0, Location.Y - Height / 2.0), 0, 0, 0);

                        pline.SetDatabaseDefaults();
                        pline.Closed = true;
                        pline.Layer = EE_Settings.DEFAULT_PIER_LAYER;
                        pline.Linetype = "HIDDEN2";
                        //pline.SetDatabaseDefaults();
                        ObjectId plineId = btr.AppendEntity(pline);
                        trans.AddNewlyCreatedDBObject(pline, true);

                        // Now create the inner rectangle (offset by 10%)
                        Polyline inner_pline = new Polyline();
                        inner_pline = OffsetPolyline(pline, 0.1 * Width, bt, btr);
                        MovePolylineToLayer(inner_pline, EE_Settings.DEFAULT_PIER_LAYER, bt, btr);
                        PolylineSetLinetype(inner_pline, "HIDDEN2", bt, btr);
                        ObjectId plineId2 = inner_pline.ObjectId;

                        // Add the associative hatch
                        AddRectangularHatch(Location, plineId2, EE_Settings.DEFAULT_PIER_HATCH_TYPE, EE_Settings.DEFAULT_HATCH_PATTERNSCALE);
                    }

                    trans.Commit();

                }
                catch (System.Exception e)
                {
                    doc.Editor.WriteMessage("\n Error drawing pier object for pier at " + Location.X + " , " + Location.Y);
                    trans.Abort();
                }
            }


        }
    }
}
