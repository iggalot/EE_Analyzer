using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_Analyzer.Utilities
{
    public static class HatchObjects
    {
        [CommandMethod("AddHatch")]
        public static void AddCircularHatch(Point3d pt, double diameter, string hatch_pattern, double hatch_scale)
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Create a circle object for the closed boundary to hatch
                using (Circle acCirc = new Circle())
                {
                    acCirc.Center = pt;
                    acCirc.Radius = diameter / 2.0;

                    // Add the new circle object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acCirc);
                    acTrans.AddNewlyCreatedDBObject(acCirc, true);

                    // Adds the circle to an object id array
                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                    acObjIdColl.Add(acCirc.ObjectId);

                    // Create the hatch object and append it to the block table record
                    using (Hatch acHatch = new Hatch())
                    {
                        acHatch.PatternScale = hatch_scale;
                        acHatch.SetHatchPattern(HatchPatternType.PreDefined, EE_Settings.DEFAULT_PIER_HATCH_TYPE);


                        acBlkTblRec.AppendEntity(acHatch);
                        acTrans.AddNewlyCreatedDBObject(acHatch, true);

                        // Set the properties of the hatch object
                        // Associative must be set after the hatch object is appended to the 
                        // block table record and before AppendLoop
                        acHatch.Associative = true;
                        acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                        acHatch.EvaluateHatch(true);
                    }
                }

                // Save the new object to the database
                acTrans.Commit();
            }
        }

        public static void AddRectangularHatch(Point3d pt, double width, double height, string hatch_pattern, double hatch_Scale)
        {
            throw new NotImplementedException();
            //// Get the current document and database
            //Document acDoc = Application.DocumentManager.MdiActiveDocument;
            //Database acCurDb = acDoc.Database;

            //// Start a transaction
            //using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            //{
            //    // Open the Block table for read
            //    BlockTable acBlkTbl;
            //    acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
            //                                    OpenMode.ForRead) as BlockTable;

            //    // Open the Block table record Model space for write
            //    BlockTableRecord acBlkTblRec;
            //    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
            //                                    OpenMode.ForWrite) as BlockTableRecord;

            //    // Create a circle object for the closed boundary to hatch
            //    using (Circle acCirc = new Circle())
            //    {
            //        acCirc.Center = pt;
            //        acCirc.Radius = width / 2.0;

            //        // Add the new circle object to the block table record and the transaction
            //        acBlkTblRec.AppendEntity(acCirc);
            //        acTrans.AddNewlyCreatedDBObject(acCirc, true);

            //        // Adds the circle to an object id array
            //        ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            //        acObjIdColl.Add(acCirc.ObjectId);

            //        // Create the hatch object and append it to the block table record
            //        using (Hatch acHatch = new Hatch())
            //        {
            //            acBlkTblRec.AppendEntity(acHatch);
            //            acTrans.AddNewlyCreatedDBObject(acHatch, true);

            //            // Set the properties of the hatch object
            //            // Associative must be set after the hatch object is appended to the 
            //            // block table record and before AppendLoop
            //            acHatch.SetHatchPattern(HatchPatternType.PreDefined, EE_Settings.DEFAULT_PIER_HATCH_TYPE);
            //            acHatch.PatternScale = 12;
            //            acHatch.Associative = true;
            //            acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
            //            acHatch.EvaluateHatch(true);
            //        }
            //    }

            //    // Save the new object to the database
            //    acTrans.Commit();
            //}
        }
    }
}
