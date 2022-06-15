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
    public static class BlockObjects
    {
        public static void InsertBlock(Point3d insPt, string blockName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // check if the block table already has the 'blockName'" block
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    try
                    {
                        // search for a dwg file named 'blockName' in AutoCAD search paths
                        var filename = HostApplicationServices.Current.FindFile(blockName + ".dwg", db, FindFileHint.Default);
                        // add the dwg model space as 'blockName' block definition in the current database block table
                        using (var sourceDb = new Database(false, true))
                        {
                            sourceDb.ReadDwgFile(filename, FileOpenMode.OpenForReadAndAllShare, true, "");
                            db.Insert(blockName, sourceDb, true);
                        }
                    }
                    catch
                    {
                        ed.WriteMessage($"\nBlock '{blockName}' not found.");
                        return;
                    }
                }

                // create a new block reference
                using (var br = new BlockReference(insPt, bt[blockName]))
                {
                    var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    space.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                }
                tr.Commit();
            }
        }

        public static void CreateBlock(DBObjectCollection db_obj_coll, string block_name)
        {
            Document doc =
              Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr =
              db.TransactionManager.StartTransaction();

            using (tr)
            {
                // Get the block table from the drawing
                BlockTable bt =
                  (BlockTable)tr.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead
                  );

                // A variable for the block's name
                string blkName = block_name;

                // Create our new block table record...
                BlockTableRecord btr = new BlockTableRecord();

                // ... and set its properties
                btr.Name = blkName;

                // Add the new block to the block table
                bt.UpgradeOpen();
                ObjectId btrId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                // Add some lines to the block to form a square
                // (the entities belong directly to the block)
                int count = 0;
                foreach (Entity ent in db_obj_coll)
                {
                    count++;
                    try
                    {
                        btr.AppendEntity(ent);
                        tr.AddNewlyCreatedDBObject(ent, true);
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("obj " + count.ToString() + " -- " + ex.Message);
                    }

                }

                // Add a block reference to the model space
                BlockTableRecord ms =
                  (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite
                  );

                BlockReference br =
                  new BlockReference(Point3d.Origin, btrId);

                ms.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // Commit the transaction
                tr.Commit();

                // Report what we've done
                ed.WriteMessage(
                  "\nCreated block named \"{0}\" containing {1} entities.",
                  blkName, db_obj_coll.Count
                );
            }
        }

        private static DBObjectCollection SquareOfLines(double size)
        {
            // A function to generate a set of entities for our block

            DBObjectCollection ents = new DBObjectCollection();

            Point3d[] pts =
            {
                new Point3d(-size, -size, 0),
                new Point3d(size, -size, 0),
                new Point3d(size, size, 0),
                new Point3d(-size, size, 0)
              };

            int max = pts.GetUpperBound(0);

            for (int i = 0; i <= max; i++)
            {
                int j = (i == max ? 0 : i + 1);
                Line ln = new Line(pts[i], pts[j]);
                ents.Add(ln);
            }

            return ents;
        }
    }
}
