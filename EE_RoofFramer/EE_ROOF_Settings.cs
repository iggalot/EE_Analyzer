using System;

namespace EE_RoofFramer
{
    public static class EE_ROOF_Settings
    {
        // VERSION INFO
        public const string CURRENT_VERSION_NUM = "Roof Ver. 1.0";
        public const string SIGNATURE_LABEL = "Copyright 2022-5-12 by Jay3";
        public const string LAST_UPDATED = "2022-07-06";
        public const int DAYS_UNTIL_EXPIRES = 25;
        public static DateTime APP_REGISTRATION_DATE = new DateTime(2022, 7, 6);

        // Default layers
        public const string DEFAULT_ROOF_BOUNDINGBOX_LAYER = "_EE_ROOF_BOUNDINGBOX"; // Contains the bounding box 
        public const string DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER = "_EE_ROOF_RAFTERS_TRIMMED"; // For the untrimmed ribs of the foundation
        public const string DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER = "_EE_ROOF_RAFTERS_UNTRIMMED"; // For the untrimmed ribs of the foundation

        public const string DEFAULT_ROOF_RIDGE_LAYER = "_EE_ROOF_RIDGE"; // For the untrimmed ribs of the foundation
        public const string DEFAULT_ROOF_HIPVALLEY_LAYER = "_EE_ROOF_HIPVALLEY"; // For the untrimmed ribs of the foundation

        public const string DEFAULT_ROOF_PURLIN_LAYER = "_EE_ROOF_PURLIN";
        public const string DEFAULT_ROOF_TEXTS_LAYER = "_EE_ROOF_TEXT";
        public const string DEFAULT_ROOF_DIMENSIONS_LAYER = "_EE_ROOF_DIMENSIONS";
        public const string DEFAULT_ROOF_ANNOTATION_LAYER = "_EE_ROOF_ANNOTATION_LAYER"; // for notes and markers
        public const string DEFAULT_TEMPORARY_GRAPHICS_LAYER = "_EE_TEMPORARY_GRAPHICS_LAYER";  // used for drawing objects that will be deleted later

        public const string DEFAULT_EE_DIMSTYLE_NAME = "EE_DIMSTYLE";

        // Default settings
        public const double DEFAULT_INTERSECTION_CIRCLE_RADIUS = 8; // diameter of the intersection marker circle
        public const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal

        public const double DEFAULT_ROOF_TEXT_SIZE = 5;  // the size of the text in the bill of materials.

        public const double DEFAULT_MAX_PURLIN_SPACING = 144;  // measured in inches -- typical max is 12ft
      
    }
}
