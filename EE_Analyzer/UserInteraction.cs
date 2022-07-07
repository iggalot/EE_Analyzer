using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

namespace EE_Analyzer
{
    public class UserInteraction
    {
        [CommandMethod("DrawObjectUsingGetKeywords")]
        public void DrawObjectUsingGetKeywords()
        {
            // Get the document object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Create a PromptKeyWordOptions
            PromptKeywordOptions pko = new PromptKeywordOptions("");
            pko.Message = "\nWhat would you like to draw?";
            pko.Keywords.Add("Line");
            pko.Keywords.Add("Circle");
            pko.Keywords.Add("Mtext");
            pko.AllowNone = false;

            PromptResult res = doc.Editor.GetKeywords(pko);
            string answer = res.StringResult;
            if (answer != null)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    switch (answer)
                    {
                        case "Line":
                            // Draw the line
                            Point3d pt1 = new Point3d(0, 0, 0);
                            Point3d pt2 = new Point3d(100, 100, 0);
                            Line ln = new Line(pt1, pt2);
                            btr.AppendEntity(ln);
                            trans.AddNewlyCreatedDBObject(ln, true);
                            break;
                        case "Circle":
                            // Draw the circle
                            Point3d cenPt = new Point3d(0, 0, 0);
                            Circle cir = new Circle();
                            cir.Center = cenPt;
                            cir.Radius = 10;
                            cir.ColorIndex = 1;
                            btr.AppendEntity(cir);
                            trans.AddNewlyCreatedDBObject(cir, true);
                            break;
                        case "Mtext":
                            // Draw the mtext
                            Point3d insPt = new Point3d(0, 0, 0);
                            MText mtx = new MText();
                            mtx.Contents = "Hello World!";
                            mtx.Location = insPt;
                            mtx.TextHeight = 10;
                            mtx.ColorIndex = 2;
                            btr.AppendEntity(mtx);
                            trans.AddNewlyCreatedDBObject(mtx, true);
                            break;
                        default:
                            doc.Editor.WriteMessage("No option selected");
                            break;
                    }

                    // Commit the transaction
                    trans.Commit();
                }
            }
        }
        [CommandMethod("GetDistancebetweenTwoPoints")]
        public void GetDistanceBetweenTwoPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor edt = doc.Editor;

            PromptDoubleResult pdr;
            pdr = edt.GetDistance("Pick two points to get the distance: ");

            Application.ShowAlertDialog("\nDistance between points: " + pdr.Value.ToString());

        }

        [CommandMethod("CreateLineUsingGetPoint")]
        public void CreateLineUsingGetPoint()
        {
            // Get the document object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            // Prompt for the starting point
            PromptPointOptions ppo = new PromptPointOptions("Pick starting point: ");
            PromptPointResult ppr = edt.GetPoint(ppo);
            Point3d startPt = ppr.Value;

            // Prompt for the end point and specify the start point as the basepoint
            ppo = new PromptPointOptions("Pick end point: ");
            ppo.UseBasePoint = true;
            ppo.BasePoint = startPt;
            ppr = edt.GetPoint(ppo);
            Point3d endPt = ppr.Value;

            if (startPt == null || endPt == null)
            {
                edt.WriteMessage("Invalid points.");
            }

            // start the transaction
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                // Get the Block Table
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Construct the line based on the two points above
                Line ln = new Line(startPt, endPt);
                ln.SetDatabaseDefaults();

                // Add the line to the drawing
                btr.AppendEntity(ln);
                trans.AddNewlyCreatedDBObject(ln, true);

                // Commit the transaction
                trans.Commit();
            }


        }

        [CommandMethod("SetLayerUsinGetString")]
        public void SetLayerUsingGetString()
        {
            // Get the document object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                LayerTable lyTab = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                PromptStringOptions prompt = new PromptStringOptions("Enter layer to make current: ");
                prompt.AllowSpaces = false;

                // Get the result of the user input using a PromptResult
                PromptResult result = edt.GetString(prompt);

                if (result.Status == PromptStatus.OK)
                {
                    string layerName = result.StringResult;

                    // Check if the entered layer name exists in the layer database
                    if (lyTab.Has(layerName) == true)
                    {
                        // Set the layer current
                        db.Clayer = lyTab[layerName];

                        // Commit the transaction
                        trans.Commit();
                    }
                    else
                    {
                        Application.ShowAlertDialog("The layer " + layerName + " you entered does not exist.");
                    }

                }
                else
                {
                    Application.ShowAlertDialog("No layer entered.");
                }
            }

        }

        [CommandMethod("GetName")]
        public void GetNameUsingGetString()
        {
            // Get the document object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor edt = doc.Editor;

            // Prompt the user using PromptStringOptions
            PromptStringOptions prompt = new PromptStringOptions("Enter your name: ");
            prompt.AllowSpaces = true;

            // Get the results of the user input using a PromptResult
            PromptResult result = edt.GetString(prompt);
            if (result.Status == PromptStatus.OK)
            {
                string name = result.StringResult;
                edt.WriteMessage("Hello there: " + name);
                Application.ShowAlertDialog("Your name is: " + name);
            }
            else
            {
                edt.WriteMessage("No name entered.");
                Application.ShowAlertDialog("No name entered.");

            }
        }
    }
}
