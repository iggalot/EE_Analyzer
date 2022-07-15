using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_Analyzer.Utilities
{
    public static class ModifyAutoCADGraphics
    {
        public static void ForceRedraw(Database db, Document doc)
        {
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
        /// Zooms to a window
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        static public void ZoomWindow(Database db, Document doc, Point3d min, Point3d max)
        {
            Editor ed = doc.Editor;

            // Call out helper function
            // [Change this to ZoomWin2 or WoomWin3 to
            // use different zoom techniques]
            ZoomWin(ed, min, max);

        }

        /// <summary>
        /// Helper function for zooming to a selected entity.
        /// </summary>
        static public void ZoomToEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Get the entity to which we'll zoom

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity:");

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return;

            // Extract its extents
            Extents3d ext;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                ext = ent.GeometricExtents;
                tr.Commit();
            }

            ext.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());

            // Call our helper function
            // [Change this to ZoomWin2 or WoomWin3 to
            // use different zoom techniques]
            ZoomWin(ed, ext.MinPoint, ext.MaxPoint);
        }

        private static void ZoomWin(Editor ed, Point3d min, Point3d max)
        {
            Point2d min2d = new Point2d(min.X, min.Y);
            Point2d max2d = new Point2d(max.X, max.Y);

            using (ViewTableRecord view = new ViewTableRecord())
            {

                view.CenterPoint = min2d + ((max2d - min2d) / 2.0);
                view.Height = max2d.Y - min2d.Y;
                view.Width = max2d.X - min2d.X;
                ed.SetCurrentView(view);
            }
        }
    }
}
