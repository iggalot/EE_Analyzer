using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.DrawObject;



namespace EE_RoofFramer.Models
{
    public class WallModel : acStructuralObject
    {
        public double StudSpacing { get; set; } = 16;
        public double Height { get; set; } = 120;
        public int TopBeam { get; set; } = -1;
        public int BottomBeam { get; set; } = -1;

        public Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }

        private Point3d MidPt { get => MathHelpers.GetMidpoint(StartPt, EndPt); }

        public List<BaseLoadModel> Reactions { get; set; } = new List<BaseLoadModel>();

        public Line Centerline { get; set; }

        // unit vector for direction of the rafter
        public Vector3d vDir { get; set; }

        public Double Length { get; set; }

        // public int Id { get => _id; set { _id = value; next_id++; } }

        public bool ReactionsCalculatedCorrectly = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="start">start point</param>
        /// <param name="end">end point</param>
        /// <param name="spacing">tributary width or rafter spacing</param>
        public WallModel(int id, Point3d start, Point3d end, double height) : base(id)
        {
            StartPt = start;
            EndPt = end;

            Height = height;  // spacing between adjacent rafters

            //// Centerline object
            //Line ln = new Line(StartPt, EndPt);
            //Centerline = OffsetLine(ln, 0) as Line;
            //MoveLineToLayer(Centerline, layer_name);

            UpdateCalculations();
        }

        /// <summary>
        /// constructor to create our object from a line of text -- used when parsing the string file
        /// </summary>
        /// <param name="line"></param>
        public WallModel(string line) : base()
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            if (split_line.Length >= 10)
            {
                if (split_line[index].Substring(0, 1).Equals("W"))
                {
                    // read the previous information that was stored in the file
                    Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));

                    // Read spacing
                    Height = Double.Parse(split_line[index + 1]);

                    // Read top and bottom beam ids
                    TopBeam = Int32.Parse(split_line[index + 2]);
                    BottomBeam = Int32.Parse(split_line[index + 3]);

                    // 0, 1, 2 -- First three values are the start point coord
                    StartPt = new Point3d(Double.Parse(split_line[index + 4]), Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]));
                    // 3, 4, 5 == Next three values are the end point coord
                    EndPt = new Point3d(Double.Parse(split_line[index + 7]), Double.Parse(split_line[index + 8]), Double.Parse(split_line[index + 9]));

                    //// Centerline object
                    //Line ln = new Line(StartPt, EndPt);
                    //Centerline = OffsetLine(ln, 0) as Line;
                    //MoveLineToLayer(Centerline, layer_name);

                    index = index + 10;  // start index of the first L: marker

                    bool should_continue = true;
                    while (should_continue)
                    {
                        if (split_line[index].Equals("$"))
                        {
                            should_continue = false;
                            continue;
                        }
                        if (split_line[index].Length < 2)
                        {
                            should_continue = false;
                            continue;
                        }

                        if (split_line[index].Substring(0, 2).Equals("SC"))
                        {
                            lst_SupportConnections.Add(Int32.Parse(split_line[index].Substring(2, split_line[index].Length - 2)));
                            index++;
                        }
                        else
                        {
                            should_continue = false;
                        }
                    }

                    UpdateCalculations();
                    return;
                }
            }
        }

        /// <summary>
        /// Updates calculations for the rafter model
        /// </summary>
        protected override void UpdateCalculations()
        {
            vDir = MathHelpers.Normalize(StartPt.GetVectorTo(EndPt));
            Length = MathHelpers.Magnitude(StartPt.GetVectorTo(EndPt));
        }

        public override void AddToAutoCADDatabase(Database db, Document doc, string layer_name)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Line ln = new Line(StartPt, EndPt);
                    Centerline = OffsetLine(ln, 0) as Line;
                    MoveLineToLayer(Centerline, layer_name);

                    // indicate if the rafters are adequately supported.
                    UpdateCalculations();

                    // mark the end supports of the rafter
                    //                    DrawCircle(StartPt, 2, layer_name);
                    //                    DrawCircle(EndPt, 2, layer_name);

                    // Add the rafter ID label
                    DrawMtext(db, doc, MidPt, "#" + Id.ToString(), 3, layer_name);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter [" + Id.ToString() + "] information to RafterModel entities to AutoCAD DB: " + ex.Message);
                    trans.Abort();
                }
            }
        }

        /// <summary>
        /// Contains the information format to record this object in a text file
        /// </summary>
        /// <returns></returns>
        public override string ToFile()
        {
            string data = "";
            //RT prefix indicates its a trimmed rafter
            data += "W" + Id.ToString() + ",";                // Rafter ID
            data += Height.ToString() + ",";      // Trib width
            data += TopBeam.ToString() + "," + BottomBeam.ToString() + ",";      // Top and bottom beam associated with this wall object
            data += StartPt.X.ToString() + "," + StartPt.Y.ToString() + "," + StartPt.Z.ToString() + ",";   // Start pt
            data += EndPt.X.ToString() + "," + EndPt.Y.ToString() + "," + EndPt.Z.ToString() + ",";         // End pt

            // add supported by connections
            foreach (int item in lst_SupportConnections)
            {
                data += "SC" + item + ",";
            }

            // End of record
            data += "$";

            return data;
        }

        public void AddTopBeam(int top_beam_id)
        {
            TopBeam = top_beam_id;

            UpdateCalculations();
        }

        public void AddBottomBeam(int bot_beam_id)
        {
            BottomBeam = bot_beam_id;

            UpdateCalculations();
        }

        public override void AddConnection(BaseConnectionModel conn)
        {
            lst_SupportConnections.Add(conn.Id);

            // if the connecition ABOVE value is the same as this member id, this connection is supporting this member, so add it to the supported by lst.
            if (conn.AboveConn == this.Id)
            {
                lst_SupportedBy.Add(conn.Id);
            }

            UpdateCalculations();
        }

        public override void AddUniformLoads(BaseLoadModel load_model)
        {

        }

        public override void AddConcentratedLoads(BaseLoadModel load_model)
        {

        }

        public override void HighlightStatus()
        {
        }

        public override bool ValidateSupports()
        {
            throw new NotImplementedException();
        }

        public override void CalculateReactions(RoofFramingLayout layout)
        {
            throw new NotImplementedException();
        }
    }
}
