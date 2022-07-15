using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static EE_Analyzer.Utilities.LayerObjects;

namespace EE_Analyzer.Utilities
{
    public class DimensionObjects
    {
        /// <summary>
        /// Gets the layer list.
        /// </summary>
        /// <param name="db">Database instance this method applies to.</param>
        /// <returns>Layer names list.</returns>
        public static List<string> GetAllDimensionStyleNamesList()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                return ((DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead))
                    .Cast<ObjectId>()
                    .Select(id => ((DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name)
                    .ToList();
            }
        }

        /// <summary>
        /// Helper function to list the names of all dimension styles in the drawing
        /// </summary>
        /// <returns></returns>
        public static string ListAllDimensionStyles()
        {
            string str = "";
            foreach (var item in GetAllDimensionStyleNamesList())
            {
                str += "\n" + item.ToString();
            }

            return str;
        }

        /// <summary>
        /// Create the EE dimension style used by the program
        /// </summary>
        /// <param name="style_name"></param>
        public static void CreateEE_DimensionStyle(string style_name)
        {
            Database db = HostApplicationServices.WorkingDatabase;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Open our dimension style table to add our
                    // new dimension style

                    DimStyleTable dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForWrite);

                    // Create our new dimension style
                    using (DimStyleTableRecord dstr = new DimStyleTableRecord())
                    {

                        // Check if the dimstyle is already created.
                        if (GetAllDimensionStyleNamesList().Contains(style_name))
                        {
                            tr.Abort();
                            return;
                        }

                        // Otherwise create the new DIMSTYLE for this application

                        // Add DIMSTYLE settings here
                        //DIMADEC Controls the number of places of precision displayed for angular dimensions
                        //DIMALT Controls the display of alternate units in dimensions
                        //DIMALTD Controls the number of decimal places in alternate units
                        //DIMALTF Controls the multiplier for alternate units

                        //DIMALTRND Determines rounding of alternate units
                        //DIMALTTD Sets the number of decimal places for the tolerance values in the alternate units of a dimension
                        //DIMALTTZ Toggles suppression of zeros in tolerance values
                        //DIMALTU Sets the units format for alternate units of all dimension style family members except angular
                        //DIMALTZ Controls the suppression of zeros for alternate unit dimension values
                        //DIMAPOST Specifies a text prefix or suffix(or both) to the alternate dimension measurement for all types of dimensions except angular

                        //DIMASO Controls the associativity of dimension objects
                        //DIMASZ Controls the size of dimension line and leader line arrowheads
                        //DIMATFIT Determines how dimension text and arrows are arranged when space is not sufficient to place both within the extension lines
                        //DIMAUNIT Sets the units format for angular dimensions
                        //DIMAZIN Suppresses zeros for angular dimensions
                        //DIMBLK Sets the arrowhead block displayed at the ends of dimension lines or leader lines

                        //DIMBLK1 Sets the arrowhead for the first end of the dimension line when DIMSAH is on
                        //DIMBLK2 Sets the arrowhead for the second end of the dimension line when DIMSAH is on
                        //DIMCEN Controls drawing of circle or arc center marks and centerlines by DIMCENTER, DIMDIAMETER, and DIMRADIUS
                        //DIMCLRD Assigns colors to dimension lines, arrowheads, and dimension leader lines
                        //DIMCLRE Assigns colors to dimension extension lines

                        //DIMCLRT Assigns colors to dimension text
                        //DIMDEC Sets the number of decimal places displayed for the primary units of a dimension
                        //DIMDLE Sets the distance the dimension line extends beyond the extension line when oblique strokes are drawn instead of arrowheads
                        //DIMDLI Controls the spacing of dimension lines in baseline dimensions
                        //DIMDSEP Specifies a single character decimal separator to use when creating dimensions whose unit format is decimal

                        //DIMEXE Specifies how far to extend the extension line beyond the dimension line
                        //DIMEXO Specifies how far extension lines are offset from origin points
                        //DIMFIT Obsolete.Replaced by DIMATFIT and DIMTMOVE
                        //DIMFRAC Sets the fraction format when DIMLUNIT is set to 4 or 5
                        //DIMGAP Sets the distance around the dimension text when the dimension line breaks to accommodate dimension text
                        //DIMJUST Controls the horizontal positioning of dimension text

                        //DIMLDRBLK Specifies the arrow type for leaders
                        //DIMLFAC Sets a scale factor for linear dimension measurements
                        //DIMLIM Generates dimension limits as the default text
                        //DIMLUNIT Sets units for all dimension types except Angular
                        //DIMLWD Assigns lineweight to dimension lines
                        //DIMLWE Assigns lineweight to extension lines
                        //DIMPOST Specifies a text prefix or suffix(or both) to the dimension measurement

                        //DIMRND Rounds all dimensioning distances to the specified value
                        //DIMSAH Controls the display of dimension line arrowhead blocks
                        //DIMSCALE Sets the overall scale factor applied to dimensioning variables that specify sizes, distances, or offsets
                        //DIMSD1 Controls suppression of the first dimension line
                        //DIMSD2 Controls suppression of the second dimension line
                        //DIMSE1 Suppresses display of the first extension line
                        //DIMSE2 Suppresses display of the second extension line

                        //DIMSHO Controls redefinition of dimension objects while dragging
                        //DIMSOXD Suppresses drawing of dimension lines outside the extension lines
                        //DIMSTYLE Shows the current dimension style
                        //DIMTAD Controls the vertical position of text in relation to the dimension line
                        //DIMTDEC Sets the number of decimal places to display in tolerance values for the primary units in a dimension
                        //DIMTFAC Sets a scale factor used to calculate the height of text for dimension fractions and tolerances

                        //DIMTIH Controls the position of dimension text inside the extension lines for all dimension types except ordinate
                        //DIMTIX Draws text between extension lines
                        //DIMTM When DIMTOL or DIMLIM is on, sets the minimum(or lower) tolerance limit for dimension text
                        //DIMTMOVE Sets dimension text movement rules
                        //DIMTOFL Controls whether a dimension line is drawn between the extension lines even when the text is placed outside

                        //DIMTOH Controls the position of dimension text outside the extension lines
                        //DIMTOL Appends tolerances to dimension text
                        //DIMTOLJ Sets the vertical justification for tolerance values relative to the nominal dimension text
                        //DIMTP When DIMTOL or DIMLIM is on, sets the maximum(or upper) tolerance limit for dimension text
                        //DIMTSZ Specifies the size of oblique strokes drawn instead of arrowheads for linear, radius, and diameter dimensioning

                        //DIMTVP Controls the vertical position of dimension text above or below the dimension line
                        //DIMTXSTY Specifies the text style of the dimension
                        //DIMTXT Specifies the height of dimension text, unless the current text style has a fixed height
                        //DIMTZIN Controls the suppression of zeros in tolerance values
                        //DIMUNIT Obsolete. Replaced by DIMLUNIT and DIMFRAC
                        //DIMUPT Controls options for user - positioned text
                        //DIMZIN Controls the suppression of zeroes in the primary unit value

                        dstr.Dimasz = 5.75;   // arrow size
                        dstr.Dimtxt = 6.0;    // text sixe                
                        dstr.Dimexo = 4.0625; // dimension line offset (gap dim)
                        dstr.Dimtfac = 1.0;   // fraction text height scale
                        dstr.Dimdli = 0.382;  // baseline spacing
                        dstr.Dimexe = 4.67;   // dimline extension
                        dstr.Dimgap = 0.125;  // dimension break
                        dstr.Dimclrt = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 255, 0);    // make the text yellow
                        dstr.Dimclrd = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);    // make the dimension arrows, lines, etc. a cyan color
                        dstr.Dimclre = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);    // make the extension lines a cyan color                                                                                      
                        dstr.Dimtad = 0;
                        dstr.Name = style_name;

                        // Add it to the dimension style table
                        ObjectId dsId = dst.Add(dstr);
                        tr.AddNewlyCreatedDBObject(dstr, true);
                    }
                    tr.Commit();
                }
            } catch (System.Exception ex)
            {
                throw new InvalidOperationException("-- In CreateEE_DimensionStyle:  Attempt to create DIMSTYLE " + style_name + " failed with: " + ex.Message);
            }

        }

        /// <summary>
        /// Draws a vertical (y-) dimension measurement
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="dim_leader_pt1">first point of the measurement</param>
        /// <param name="dim_leader_pt2">second point of the measurement</param>
        /// <param name="dim_line_pt">location of the dimension text line</param>
        /// <param name="style_name"></param>
        public static void DrawVerticalDimension(Database db, Document doc, Point3d dim_leader_pt1, Point3d dim_leader_pt2, Point3d dim_line_pt, string style_name)
        {
            string curr_layer = GetCurrentLayerName();  // get the current layer and save it
            MakeLayerCurrent(EE_FDN_Settings.DEFAULT_FDN_DIMENSIONS_LAYER, doc, db); // switch to current layer

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // compute the 'dimensionLinePoint' (max X value + the current dimstyle text height X 5
                var dimstyle = (DimStyleTableRecord)tr.GetObject(db.Dimstyle, OpenMode.ForRead);

                // create a new RotatedDimension
                var cSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                using (var dim = new RotatedDimension(0.5 * Math.PI, dim_leader_pt1, dim_leader_pt2, dim_line_pt, "", db.Dimstyle))
                {
                    dim.TransformBy(doc.Editor.CurrentUserCoordinateSystem);
                    cSpace.AppendEntity(dim);
                    tr.AddNewlyCreatedDBObject(dim, true);
                }
                tr.Commit();
            }

            MakeLayerCurrent(curr_layer, doc, db);  // restore original setting
        }

        /// <summary>
        /// Draw a horizontal (x-) direction dimension
        /// </summary>
        /// <param name="db"></param>
        /// <param name="doc"></param>
        /// <param name="dim_leader_pt1">first point of the measurement</param>
        /// <param name="dim_leader_pt2">second point of the measurement</param>
        /// <param name="dim_line_pt">location of the dimension text line</param>
        public static void DrawHorizontalDimension(Database db, Document doc, Point3d dim_leader_pt1, Point3d dim_leader_pt2, Point3d dim_line_pt)
        {
            string curr_layer = GetCurrentLayerName();  // get the current layer and save it
            MakeLayerCurrent(EE_FDN_Settings.DEFAULT_FDN_DIMENSIONS_LAYER, doc, db); // switch to current layer

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // compute the 'dimensionLinePoint' (max X value + the current dimstyle text height X 5
                //var dimstyle = (DimStyleTableRecord)tr.GetObject(db.Dimstyle, OpenMode.ForRead);

                // create a new RotatedDimension
                var cSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                using (var dim = new RotatedDimension(0, dim_leader_pt1, dim_leader_pt2, dim_line_pt, "", db.Dimstyle))
                {
                    dim.TransformBy(doc.Editor.CurrentUserCoordinateSystem);
                    cSpace.AppendEntity(dim);
                    tr.AddNewlyCreatedDBObject(dim, true);
                }
                tr.Commit();
            }

            MakeLayerCurrent(curr_layer, doc, db);
        }
    }
}
