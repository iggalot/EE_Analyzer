﻿using System;

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

        // File information
        public const string DEFAULT_EE_BEAM_FILENAME = "beams._EE";  // filename that stores the rafter information.
        public const string DEFAULT_EE_CONNECTION_FILENAME = "connections._EE";  // filename that stores the support connection information.
        public const string DEFAULT_EE_LOAD_FILENAME = "loads._EE";  // filename that stores the support connection information.

        // Default layers
        public const string DEFAULT_ROOF_BOUNDINGBOX_LAYER = "_EE_ROOF_BOUNDINGBOX"; // Contains the bounding box 
        public const string DEFAULT_ROOF_RAFTERS_TRIMMED_LAYER = "_EE_ROOF_RAFTERS_TRIMMED"; // For the trimmed roof rafters
        public const string DEFAULT_ROOF_RAFTERS_UNTRIMMED_LAYER = "_EE_ROOF_RAFTERS_UNTRIMMED"; // For the untrimmed roof rafters

        public const string DEFAULT_SUPPORT_CONNECTION_POINT_LAYER = "_EE_SUPPORT_CONNECTIONS"; // For support connection points

        public const string DEFAULT_ROOF_RIDGE_SUPPORT_LAYER = "_EE_ROOF_RIDGE_SUPPORT"; // For ridge supports
        public const string DEFAULT_ROOF_HIPVALLEY_SUPPORT_LAYER = "_EE_ROOF_HIPVALLEY_SUPPORT"; // For the hip valley supports
        public const string DEFAULT_ROOF_WALL_SUPPORT_LAYER = "_EE_ROOF_WALL_SUPPORT"; // For the wall supports
        public const string DEFAULT_ROOF_STEEL_BEAM_SUPPORT_LAYER = "_EE_ROOF_STEEL_BEAM_SUPPORT"; // For the beam supports
        public const string DEFAULT_ROOF_WOOD_BEAM_SUPPORT_LAYER = "_EE_ROOF_STEEL_BEAM_SUPPORT"; // For the beam supports

        public const string DEFAULT_ROOF_PURLIN_SUPPORT_LAYER = "_EE_ROOF_PURLIN"; // For purlin supports

        public const string DEFAULT_LOAD_LAYER = "_EE_ROOF_LOADS"; // For roof loads.

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
