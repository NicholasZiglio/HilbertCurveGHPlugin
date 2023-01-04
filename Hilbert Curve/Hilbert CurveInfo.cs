using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Hilbert_Curve
{
    public class Hilbert_CurveInfo : GH_AssemblyInfo
    {
        public override string Name => "Hilbert Curve";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.HilbertCurveIcon;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Generates a hilbert curve of a given order.";

        public override Guid Id => new Guid("A56B6EE4-A5FB-4060-B7E9-BBDD6B268A09");

        //Return a string identifying you or your company.
        public override string AuthorName => "Nicholas Ziglio";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "nicholas.ziglio@gmail.com";
    }
}