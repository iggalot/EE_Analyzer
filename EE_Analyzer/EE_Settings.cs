using System;

namespace EE_Analyzer
{
    public static class EE_Settings
    {
        // VERSION INFO
        public const string CURRENT_VERSION_NUM = "Foundation Detailer Ver. 0.9";
        public const string SIGNATURE_LABEL = "Copyright 2022-5-12 by Jay3";
        public const string LAST_UPDATED = "2022-07-06";
        public const int DAYS_UNTIL_EXPIRES = 25;
        public static DateTime APP_REGISTRATION_DATE = new DateTime(2022, 7, 6);


        // Default layers
        public const string DEFAULT_FDN_BOUNDINGBOX_LAYER = "_EE_FDN_BOUNDINGBOX"; // Contains the bounding box 
        public const string DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER = "_EE_FDN_PERIMETER";  // Contains the polyline for the perimeter of the foundation
        public const string DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER = "_EE_FDN_BEAMS_UNTRIMMED"; // For the untrimmed ribs of the foundation
        public const string DEFAULT_FDN_BEAMS_TRIMMED_LAYER = "_EE_FDN_BEAMS_TRIMMED";

        public const string DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER = "_EE_FDN_BEAM_STRANDS_UNTRIMMED";
        public const string DEFAULT_FDN_BEAM_STRANDS_TRIMMED_LAYER = "_EE_FDN_BEAM_STRANDS_TRIMMED";
        public const string DEFAULT_FDN_SLAB_STRANDS_UNTRIMMED_LAYER = "_EE_FDN_SLAB_STRANDS_UNTRIMMED";
        public const string DEFAULT_FDN_SLAB_STRANDS_TRIMMED_LAYER = "_EE_FDN_SLAB_STRANDS_TRIMMED";
        public const string DEFAULT_FDN_STRAND_ANNOTATION_LAYER = "_EE_FDN_STRAND_ANNOTATION_LAYER";

        public const string DEFAULT_FDN_TEXTS_LAYER = "_EE_FDN_TEXT";
        public const string DEFAULT_FDN_DIMENSIONS_LAYER = "_EE_FDN_DIMENSIONS";
        public const string DEFAULT_FDN_ANNOTATION_LAYER = "_EE_FDN_ANNOTATION_LAYER"; // for notes and markers
        public const string DEFAULT_PIER_LAYER = "_EE_PIER_LAYER";
        public const string DEFAULT_PIER_TEXTS_LAYER = "_EE_PIER_TEXT";
        public const string DEFAULT_TEMPORARY_GRAPHICS_LAYER = "_EE_TEMPORARY_GRAPHICS_LAYER";  // used for drawing objects that will be deleted later

        public const string DEFAULT_EE_DIMSTYLE_NAME = "EE_DIMSTYLE";


        // Default settings
        public const double DEFAULT_INTERSECTION_CIRCLE_RADIUS = 8; // diameter of the intersection marker circle
        public const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal

        public const double DEFAULT_BILL_OF_MATERIALS_TEXT_SIZE = 5;  // the size of the text in the bill of materials.
        public const double DEFAULT_GRADE_BEAM_INFO_TEXT_SIZE = 5; // size of grade beam information text

        public const double DEFAULT_STRAND_ARROW_LENGTH = 8; // length of each strand arrow
        public const double DEFAULT_STRAND_ARROW_WIDTH = 3;   // width of the strand arrow -- about 30% of arrow width
        public const double DEFAULT_STRAND_INFO_TEXT_SIZE = 5; // about 150% of arrow width seens about right

        public static double DEFAULT_MIN_PT_LENGTH = 240;  // Length (in inches) for which PT is not practical -- so no grade beams
        public const double DEFAULT_PT_LENGTH_EXCESS_CONSTRUCTION = 36;  // amount (in inches) to add to both dead and live ends for stressing operations


        // Pier information
        public const string DEFAULT_PIER_HATCH_TYPE = "ANSI31";
        public const double DEFAULT_HATCH_PATTERNSCALE = 30;
    }
}
