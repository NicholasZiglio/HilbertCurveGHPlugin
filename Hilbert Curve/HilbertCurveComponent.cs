using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Hilbert_Curve
{
    public class HilbertCurveComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public HilbertCurveComponent()
          : base("Hilbert Curve", "Hilbert Curve",
            "Generates a Hilbert Curve",
            "Hilbert Curve", "Hilbert Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Input Number: 0
            pManager.AddIntegerParameter("Hilbert Order", "Hilbert Order", "The number of iterations the hilbert curve will subdivide for.", GH_ParamAccess.item);
            // Input Number: 1
            pManager.AddNumberParameter("Edge Length", "Edge Length", "The length of each edge that the hilbert curve is filling.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Output Number: 0
            pManager.AddCurveParameter("Hilbert Curve", "Hilbert Curve", "The calculated hilbert curve.", GH_ParamAccess.item);
            // Output Number: 1
            pManager.AddIntegerParameter("Number of Rows", "Number Of Rows", "The number of rows will be connected by the hilbert curve.", GH_ParamAccess.item);
            // Output Number: 2
            pManager.AddIntegerParameter("Number of Columns", "Number Of Columns", "The number of columns will be connected by the hilbert curve.", GH_ParamAccess.item);
            // Output Number: 3
            pManager.AddIntegerParameter("Number of Points", "Number Of Points", "The number of points that will be connected by the hilbert curve.", GH_ParamAccess.item);
            // Output Number: 4
            pManager.AddIntegerParameter("Number of Segments", "Number Of Segments", "The number of segments that will make up the hilbert curve.", GH_ParamAccess.item);
            // Output Number: 5
            pManager.AddIntegerParameter("Segment Length", "Segment Length", "The length of each segment that will make up the hilbert curve.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare input variables
            int hilbertOrder = 1;
            double edgeLength = 1.0;

            // Get input variables
            DA.GetData<int>(0, ref hilbertOrder);
            DA.GetData<double>(1, ref edgeLength);

            // Input sanity checks
            // Ensure hilbert order is a positive integer
            if (hilbertOrder < 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You must provide a value higher or equal to 1 for the Hilbert Order input!\nThe value has been set to 1.");
                hilbertOrder = 1;
            }
            // Ensure hilbert order is not larger than 32 to avoid an integer overflow 
            if (hilbertOrder > 32)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You must provide a value lower or equal to than 32.\nThe value has been set to 32.");
                hilbertOrder = 32;
            }
            // Ensure hilbert edge length is a positive double
            if (edgeLength < double.Epsilon)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You must provide a value higher than 0 for the Edge Length input!\nThe value has been set to 1.0.");
                edgeLength = 1.0;
            }


            // Calculate main properties of the hilbert curve
            int numberOfRows = (int)Math.Pow(2, hilbertOrder);
            int numberOfColumns = numberOfRows;
            int numberOfPoints = (int)Math.Pow(2, 2 * hilbertOrder);
            int numberOfSegments = numberOfPoints - 1;
            double segmentLength = edgeLength / (Math.Pow(2, hilbertOrder - 1));


            // Declare output variables
            Polyline hilbertCurve = new Polyline(numberOfPoints);


            // Calculate hilbert curve coordinates for each point using uint index to avoid 
            for (uint pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                hilbertCurve.Add(calculateCoordinatesOnHilbertCurve(pointIndex, hilbertOrder, segmentLength));
            }


            // Outputs
            DA.SetData(0, hilbertCurve);
            DA.SetData(1, numberOfRows);
            DA.SetData(2, numberOfColumns);
            DA.SetData(3, numberOfPoints);
            DA.SetData(4, numberOfSegments);
            DA.SetData(4, segmentLength);
        }



        /// <summary>
        /// This is the method that calculates the coordinates of the given point on the hilbert curve.<br/>
        /// The algorithm used calculates the position based on the index of the point, therefore avoiding the need of recursion based on previous points or subdivisions.
        /// </summary>
        /// <param name="pointIndex">The index of the point for which the coordiates will be calculated.</param>
        /// <param name="hilbertOrder">The number of subdivisions within the hilbert curve. Also refered to as level, depth, iterations.</param>
        /// <param name="segmentLength">The distance between each point within the hilbert curve and therefore of each segment.</param>
        private Point3d calculateCoordinatesOnHilbertCurve(uint pointIndex, int hilbertOrder, double segmentLength)
        {
            // Point from which calculation begins.
            Point3d currentPoint = new Point3d(0, 0, 0);

            // This is used to multiply translation distances by the amount of subdivisions present in the current order.
            int subdivisionsToMove = 1;
            // Transform the point based on the two least siginificant bits which represent the containing quadrant of each subdivision.
            for (int currentOrder = 0; currentOrder < hilbertOrder; currentOrder++)
            {
                // Find which quadrant the current point is in and apply the appropriate transformation.
                TransformToCurrentQuadrant(pointIndex, subdivisionsToMove, ref currentPoint);
                
                // Right shift point index integer by 2 to check the next two least significant bits.
                pointIndex >>= 2;
                // Double move distance for the next order's subdivision.
                subdivisionsToMove *= 2;
            }

            // Offset point so it is in the middle of the first order subdivision.
            currentPoint += new Point3d(0.5, 0.5, 0);
            // Scale point from origin so the distance between points will be of segment length.
            currentPoint *= segmentLength;

            // Return a new point with the calculated coordinates.
            return currentPoint;
        }

        /// <summary>
        /// This is the method that calculates in which quadrant the point lies in for the current order.<br/>
        /// The quadrant is determined by the two least significant bits of the point index which indicate the quadrant it lies in.<br/>    
        /// The point will be transformed according to which quadrant it belongs in.<br/>
        /// NOTE:   The quadrants are in the order of the 1st order hilbert curve and NOT in the conventional order of cartesian plane!<br/>
        /// This is done to ensure that the hilbert curve starts from the bottom left (towards the origin) and expands in the +X and +Y direction.
        /// </summary>
        /// <param name="pointIndex">The index of the point for which the coordiates will be calculated.</param>
        /// <param name="subdivisionsToMove">The number of subdivisions that will be used to move the point during translations.</param>
        /// <param name="segmentLength">The distance between each point within the hilbert curve and therefore of each segment.</param>

        /*
         *      2nd Quadrant        |   3rd Quadrant
         *      Case: 1 -> 0b01     |   Case: 2 -> 0b10   
         *  ________________________|_____________________
         *                          |                     
         *      1st Quadrant        |   4th Quadrant      
         *      Case: 0 -> 0b00     |   Case: 3 -> 0b11    
         */
        private void TransformToCurrentQuadrant(uint pointIndex, double subdivisionsToMove, ref Point3d currentPoint)
        {
            // Check the first two least significant bits of point index to see in which quadrant the current point is in for the current order.
            switch (pointIndex & 3)
            {
                // 1st Quadrant contains the point in the current order. A flip tranformation is applied to the point's coordinates.
                case 0:
                    {
                        // Flip.
                        double temporaryX = currentPoint.X;
                        currentPoint.X = currentPoint.Y;
                        currentPoint.Y = temporaryX;
                        break;
                    }

                // 2nd Quadrant contains the point in the current order. A translation in the +Y (up) direction is applied to the point's coordinates.
                case 1:
                    {
                        // Move.
                        currentPoint.Y += 1 * subdivisionsToMove;
                        break;
                    }

                // 3rd Quadrant contains the point in the current order. A translation in the +X (right) and +Y (up) direction is applied to the point's coordinates.
                case 2:
                    {
                        // Move.
                        currentPoint.X += 1 * subdivisionsToMove;
                        currentPoint.Y += 1 * subdivisionsToMove;
                        break;
                    }

                // 4th Quadrant contains the point in the current order. A flip tranformation and then a translation in the +X (right) direction is applied to the point's coordinates.
                case 3:
                    {
                        // Flip.
                        double temporaryX = subdivisionsToMove - 1 - currentPoint.X;
                        currentPoint.X = subdivisionsToMove - 1 - currentPoint.Y;
                        currentPoint.Y = temporaryX;
                        // Move.
                        currentPoint.X += 1 * subdivisionsToMove;
                        break;
                    }

                // This line should never be hit as an & 3 operation will return a value between 0-3 for any integer.
                default: break;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.HilbertCurveIcon;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("97E7F849-CC87-403A-BA51-551A2A828E53");
    }
}