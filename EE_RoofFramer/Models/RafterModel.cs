using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using EE_Analyzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EE_Analyzer.Utilities.DrawObject;
using static EE_Analyzer.Utilities.EE_Helpers;
using static EE_Analyzer.Utilities.LineObjects;
using static EE_Analyzer.Utilities.LayerObjects;
using static EE_Analyzer.Utilities.XData;


namespace EE_RoofFramer.Models
{
    public class RafterModel : acStructuralObject
    {        
        private double Spacing = 24; // tributary width (or rafter spacing)

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
        public RafterModel(int id, Point3d start, Point3d end, double spacing) : base(id)
        {
            StartPt = start;
            EndPt = end;

            Spacing = spacing;  // spacing between adjacent rafters

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
        public RafterModel(string line) : base()
        {
            string[] split_line = line.Split(',');
            // Check that this line is a "RAFTER" designation "R"
            int index = 0;

            if (split_line.Length >= 8)
            {
                if (split_line[index].Substring(0, 1).Equals("R"))
                {
                    // read the previous information that was stored in the file
                    Id = Int32.Parse(split_line[index].Substring(1, split_line[index].Length - 1));

                    // Read spacing
                    Spacing = Double.Parse(split_line[index + 1]);
                    // 0, 1, 2 -- First three values are the start point coord
                    StartPt = new Point3d(Double.Parse(split_line[index + 2]), Double.Parse(split_line[index + 3]), Double.Parse(split_line[index + 4]));
                    // 3, 4, 5 == Next three values are the end point coord
                    EndPt = new Point3d(Double.Parse(split_line[index + 5]), Double.Parse(split_line[index + 6]), Double.Parse(split_line[index + 7]));

                    //// Centerline object
                    //Line ln = new Line(StartPt, EndPt);
                    //Centerline = OffsetLine(ln, 0) as Line;
                    //MoveLineToLayer(Centerline, layer_name);

                    index = index + 8;  // start index of the first L: marker

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
                    HighlightStatus();

                    // mark the end supports of the rafter
//                    DrawCircle(StartPt, 2, layer_name);
//                    DrawCircle(EndPt, 2, layer_name);

                    // Add the rafter ID label
                    DrawMtext(db, doc, MidPt, "#"+Id.ToString(), 3, layer_name);

                    trans.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage("\nError adding rafter [" + CurrentHandle.ToString() + "] information to RafterModel entities to AutoCAD DB: " + ex.Message);
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
            data += "R" + Id.ToString();                // Rafter ID
            data += ","+ Spacing.ToString() + ",";      // Trib width
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

        /// <summary>
        /// Add a load model object
        /// </summary>
        /// <param name="load_model"></param>
        public override void AddUniformLoads(BaseLoadModel load_model)
        {

        }

        public override void AddConcentratedLoads(BaseLoadModel load_model)
        {

        }

        /// <summary>
        /// Add connection information for beams that are supporting this rafter
        /// </summary>
        /// <param name="conn"></param>
        public override void AddConnection(BaseConnectionModel conn)
        {
            lst_SupportConnections.Add(conn.Id);

            // if the connecition ABOVE value is the same as this member id, this connection is supporting this member, so add it to the supported by lst.
            if(conn.AboveConn == this.Id)
            {
                lst_SupportedBy.Add(conn.Id);
            }

            UpdateCalculations();
        }

        public bool IsDeterminate()
        {
            bool is_determinate = true;
            if ((lst_SupportedBy is null) || (lst_SupportedBy.Count < 2))
            {
                is_determinate = false;
            }

            return is_determinate;
        }

        public override bool ValidateSupports()
        {
            return IsDeterminate();
        }

        /// <summary>
        /// Change the color of the rafter line based on its support status
        /// </summary>
        public override void HighlightStatus()
        {
            if (IsDeterminate() == true)
            {
                ChangeLineColor(Centerline, EE_ROOF_Settings.RAFTER_DETERMINATE_PASS_COLOR);
            }
            else
            {
                ChangeLineColor(Centerline, EE_ROOF_Settings.RAFTER_DETERMINATE_FAIL_COLOR);
            }
        }

        //TODO: Include external point loads for this calculation
        public override void CalculateReactions(RoofFramingLayout layout)
        {
            IDictionary<int, BaseConnectionModel> conn_dict = layout.dctConnections;
            IDictionary<int, BaseLoadModel> load_dict = layout.dctLoads;
            IDictionary<int, int> applied_loads_dict = layout.dctAppliedLoads;

            // check if the rafter is determinant
            if (this.IsDeterminate())
            {
                // compute the reaction values
                if (this.lst_SupportedBy.Count == 2)
                {
                    List<BaseConnectionModel> connection_models_above = new List<BaseConnectionModel>();

                    foreach (int conn_id in this.lst_SupportConnections)
                    {
                        if (conn_dict.ContainsKey(conn_id))
                        {
                            if (conn_dict[conn_id].AboveConn == this.Id)
                            {
                                // Add our connection model to the list to be investigated
                                connection_models_above.Add(conn_dict[conn_id]);
                            }
                        }
                    }

                    BaseConnectionModel first_conn = connection_models_above[0];
                    BaseConnectionModel second_conn = connection_models_above[1];

                    // coordinate data of supports and limits of beam as measured from origin
                    Point3d pA = first_conn.ConnectionPoint;        // (in.) Support A location
                    Point3d pB = second_conn.ConnectionPoint;       // (in.) Support B location
                    Point3d pStart = this.StartPt;                // (in.) Start point of beam
                    Point3d pEnd = this.EndPt;                    // (in.) End point of beam
                    Point3d pW = pStart + 0.5 * (pEnd - pStart);    // (in.) vector from start to center of distributed load

                    // vectors on structure
                    Vector3d vSA = pA - pStart;    // (in.) vector from start to 1st support
                    Vector3d vSB = pB - pStart; ;   // (in.) vector from start to 2nd support
                    Vector3d vSE = pEnd - pStart;  // (in.) vector from start to end of beam

                    // load vector
                    // (lb)                           psf *     ft                       * ft
                    Vector3d vW_DL = new Vector3d(0, 0, -10 * (24.0 / 12.0) * MathHelpers.Magnitude(vSE) / 12.0);
                    Vector3d vW_LL = new Vector3d(0, 0, -20 * (24.0 / 12.0) * MathHelpers.Magnitude(vSE) / 12.0);
                    Vector3d vW_RLL = new Vector3d(0, 0, -20 * (24.0 / 12.0) * MathHelpers.Magnitude(vSE) / 12.0);

                    // position vectors
                    // (ft)    =     (in.)  / 12.0            --> (ft.)
                    Vector3d r_AB = pA.GetVectorTo(pB) / 12.0;
                    Vector3d r_AW = pA.GetVectorTo(pW) / 12.0;

                    // Moment vectors
                    Vector3d MA_DL = MathHelpers.CrossProduct(r_AW, vW_DL);   // (lb ft)
                    Vector3d MA_LL = MathHelpers.CrossProduct(r_AW, vW_LL);   // (lb ft) 
                    Vector3d MA_RLL = MathHelpers.CrossProduct(r_AW, vW_RLL); // (lb ft)

                    // Reactions for support B
                    //        lb   =     lb-ft  / ft
                    double RB_Z_DL = (MA_DL.Y / (r_AB.X));     // (lb)
                    double RB_Z_LL = (MA_LL.Y / (r_AB.X));     // (lb)
                    double RB_Z_RLL = (MA_RLL.Y / (r_AB.X));   // (lb)

                    // Reactions for support A = RA = VW + RB === as vector equations
                    //        lb   =     lb-ft  / ft
                    double RA_Z_DL = -vW_DL.Z - RB_Z_DL;      // (lb)
                    double RA_Z_LL = -vW_LL.Z - RB_Z_LL;      // (lb)
                    double RA_Z_RLL = -vW_RLL.Z - RB_Z_RLL;   // (lb)

                    BaseLoadModel lmA = new LoadModelConcentrated(layout.GetNewId(), pA, RA_Z_DL, RA_Z_LL, RA_Z_RLL);
                    BaseLoadModel lmB = new LoadModelConcentrated(layout.GetNewId(), pB, RB_Z_DL, RB_Z_LL, RB_Z_RLL);

                    Reactions.Add(lmA);     // save the A reaction
                    Reactions.Add(lmB);     // save the B reaction
                }
            }
        }
    }
}
