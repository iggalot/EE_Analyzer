using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.MathHelpers;


namespace EE_Analyzer.Models
{
    public class StrandModel
    {
        private static int _id = 0;
        private const double icon_size = 12;
        private const double icon_thickness = 8;
        public int Id { get; set; }

        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }
        public string Label { get; set; }
        public double Length { get; set; }

        public Line Centerline { get; set; }

        public Point3d LiveEnd { get; set; }
        public int Qty { get; set; } = 1;

        public Polyline LiveEndIcon;
        public Polyline DeadEndIcon;

        public bool IsBeamStrand { get; set; } = false;

        public StrandModel(Point3d start, Point3d end, int qty, bool isBeamStrand)
        {
            Id = _id;
            _id++;

            StartPt = start;
            EndPt = end;
            IsBeamStrand = isBeamStrand;
            Qty= qty;

            Centerline = OffsetLine(new Line(start, end), 0) as Line;  // Must create the centerline this way to have it added to the AutoCAD database

            Length = MathHelpers.Distance3DBetween(start, end);

            string str = Qty + "x ";
            if (isBeamStrand)
            {
                str += "B";
            }

            Label = str + "S" + (Math.Ceiling(Length * 10 / 12).ToString());
        }

        public void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            doc.Editor.WriteMessage("Drawing LiveEnd");
            DrawLiveEndIcon(db, doc, layer_name);
            doc.Editor.WriteMessage("Drawing DeadEnd");
            DrawDeadEndIcon(db, doc, layer_name);
            doc.Editor.WriteMessage("Drawing StrandLabel");
            DrawStrandLabel(db, doc, layer_name);
        }

        public Polyline DrawLiveEndIcon(Database db, Document doc, string layer_name)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline pl = new Polyline();

                try
                {
                    // Get the angle of the polyline
                    var angle = Math.Atan((EndPt.Y - StartPt.Y) / (EndPt.X - StartPt.X));

                    Point2d pt1 = new Point2d(StartPt.X, StartPt.Y);

                    pl.AddVertexAt(0, pt1, 0, icon_thickness, 0);


                    // Specify the polyline parameters 
                    for (int i = 0; i < Qty; i++)
                    {
                        Point2d pt2 = new Point2d(StartPt.X + Math.Cos(angle)*(i + 1) * icon_size, StartPt.Y+ Math.Sin(angle) * (i + 1) * icon_size);
                        pl.AddVertexAt(i + 1, pt2, 0, icon_thickness, 0);
                    }

                    //pl.Closed = true;

                    // assign the layer
                    pl.Layer = layer_name;

                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered drawing foundation boundary line: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        public Polyline DrawDeadEndIcon(Database db, Document doc, string layer_name)
        {
            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline pl = new Polyline();

                try
                {
                    // Get the angle of the polyline
                    var angle = Math.Atan((EndPt.Y - StartPt.Y) / (EndPt.X - StartPt.X));

                    // And add 180degrees to reverse it.
                    var angle_reverse = angle + Math.PI;

                    Point2d pt1 = new Point2d(EndPt.X, EndPt.Y);

                    pl.AddVertexAt(0, pt1, 0, icon_thickness, icon_thickness);

                    // Specify the polyline parameters 
                    for (int i = 0; i < 2 * Qty; i++)
                    {
                        Point2d pt2 = new Point2d(EndPt.X + (i + 1) * (0.8 * icon_size) * Math.Cos(angle_reverse), EndPt.Y + (i + 1) * (0.8 * icon_size) * Math.Sin(angle_reverse));
                        if (i % 2 != 0)
                        {
                            pl.AddVertexAt(i + 1, pt2, 0, icon_thickness, icon_thickness);
                        }
                        else
                        {
                            pl.AddVertexAt(i + 1, pt2, 0, 0, 0);
                        }
                    }

                    // assign the layer
                    pl.Layer = layer_name;
                    
                    // Set the default properties
                    pl.SetDatabaseDefaults();
                    btr.AppendEntity(pl);
                    trans.AddNewlyCreatedDBObject(pl, true);

                    trans.Commit();

                    return pl;
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered drawing strand dead end icon: " + ex.Message);
                    trans.Abort();
                    return null;
                }
            }
        }

        private void DrawStrandLabel(Database db, Document doc, string layer_name)
        {
            Vector3d vector = StartPt.GetVectorTo(EndPt);

            var length = vector.Length;

            // at this point we know an entity has been selected and it is a Polyline
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Point3d insPt = StartPt;

                // Get the angle of the polyline
                var angle = Math.Atan((EndPt.Y - StartPt.Y) / (EndPt.X - StartPt.X));

                MText mtx = new MText();
                try
                {
                    mtx.Contents = Label;
                    mtx.Location = new Point3d(insPt.X + Math.Sin(angle) * 0.8 * icon_size, insPt.Y - Math.Cos(angle) * 0.8 * icon_size, 0);
                    mtx.TextHeight = 0.75 * icon_size;

                    mtx.Layer = layer_name;

                    mtx.Rotation = angle;

                    btr.AppendEntity(mtx);
                    trans.AddNewlyCreatedDBObject(mtx, true);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError encountered while adding beam label objects: " + ex.Message);
                    trans.Abort();
                    return;
                }

            }
        }
    }
}
