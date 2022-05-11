namespace EE_Analyzer
{
    public static class EE_Settings
    {
        // Default layers
        public const string DEFAULT_FDN_BOUNDINGBOX_LAYER = "_EE_FDN_BOUNDINGBOX"; // Contains the bounding box 
        public const string DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER = "_EE_FDN_PERIMETER";  // Contains the polyline for the perimeter of the foundation
        public const string DEFAULT_FDN_BEAMS_UNTRIMMED_LAYER = "_EE_FDN_BEAMS_UNTRIMMED"; // For the untrimmed ribs of the foundation
        public const string DEFAULT_FDN_BEAMS_TRIMMED_LAYER = "_EE_FDN_BEAMS_TRIMMED";
        
        public const string DEFAULT_FDN_BEAM_STRANDS_UNTRIMMED_LAYER = "_EE_FDN_BEAM_STRANDS_UNTRIMMED";
        public const string DEFAULT_FDN_BEAM_STRANDS_TRIMMED_LAYER = "_EE_FDN_BEAM_STRANDS_TRIMMED";
        public const string DEFAULT_FDN_SLAB_STRANDS_LAYER = "_EE_FDN_SLAB_STRANDS";
        public const string DEFAULT_FDN_TEXTS_LAYER = "_EE_FDN_TEXT";
        public const string DEFAULT_FDN_DIMENSIONS_LAYER = "_EE_FDN_DIMENSIONS";
        public const string DEFAULT_FDN_ANNOTATION_LAYER = "_EE_FDN_ANNOTATION_LAYER"; // for notes and markers

        // Default settings
        public const double DEFAULT_INTERSECTION_CIRCLE_RADIUS = 8; // diameter of the intersection marker circle
        public const double DEFAULT_HORIZONTAL_TOLERANCE = 0.01;  // Sets the tolerance (difference between Y-coords) to determine if a line is horizontal

    }
}
