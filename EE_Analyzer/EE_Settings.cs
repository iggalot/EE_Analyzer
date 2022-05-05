namespace EE_Analyzer
{
    public static class EE_Settings
    {
        // Default layers
        public const string DEFAULT_FDN_BOUNDINGBOX_LAYER = "_EE_FDN_BOUNDINGBOX"; // Contains the bounding box 
        public const string DEFAULT_FDN_BOUNDARY_PERIMENTER_LAYER = "_EE_FDN_PERIMETER";  // Contains the polyline for the perimeter of the foundation
        public const string DEFAULT_FDN_BEAMS_LAYER = "_EE_FDN_BEAMS"; // For the untrimmed ribs of the foundation
        public const string DEFAULT_FDN_BEAMS_TRIMMED_LAYER = "_EE_FDN_BEAMS_TRIMMED";
        public const string DEFAULT_FDN_BEAM_STRANDS_LAYER = "_EE_FDN_BEAM_STRANDS";
        public const string DEFAULT_FDN_SLAB_STRANDS_LAYER = "_EE_FDN_SLAB_STRANDS";
        public const string DEFAULT_FDN_TEXTS_LAYER = "_EE_FDN_TEXT";
        public const string DEFAULT_FDN_DIMENSIONS_LAYER = "_EE_FDN_DIMENSIONS";
        public const string DEFAULT_FDN_ANNOTATION_LAYER = "_EE_FDN_ANNOTATION_LAYER"; // for notes and markers

    }
}
