using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EE_RoofFramer.Models
{
    public class LoadModel
    {
        // Dead load
        public double DL { get; set; }
        // Live load
        public double LL { get; set; }
        // Roof live load
        public double RLL { get; set; }

        public LoadModel(double dead, double live, double roof_live)
        {
            DL = dead;
            LL = live;
            RLL = roof_live;
        }

        public override string ToString()
        {
            return "DL: " + Math.Ceiling(DL) + "\nLL: " + Math.Ceiling(LL) + "\nRLL: " + Math.Ceiling(RLL) + " (lbs)";

        }
    }
}
