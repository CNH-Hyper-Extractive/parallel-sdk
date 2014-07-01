using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Diagnostics;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetSelafinElements : ElementSetSelafinBase
    {
        public ElementSetSelafinElements()
            : base(new SpatialDefinition(new Describes("Selafin Cells"), 0), ElementType.Polygon)
        { }

        public ElementSetSelafinElements(string selafinFile)
            : base(new SpatialDefinition(new Describes("Selafin Cells"), 0), ElementType.Polygon)
        { }

        public override IIdentifiable GetElementId(int index)
        {
            return new Identity(
                index.ToString(),
                index.ToString(),
                string.Empty);
        }

        public override int GetElementIndex(IIdentifiable elementId)
        {
            return int.Parse(elementId.Id);
        }

        public override int GetFaceCount(int elementIndex)
        {
            return 1;
        }

        public override int[] GetFaceVertexIndices(int elementIndex, int faceIndex)
        {
            if (!Initialised)
                Initialise();

            if (faceIndex != 0)
                throw new Exception("faceIndex must be 0, elements have only one face");

            int n = GetVertexCount(elementIndex);
            int[] a = new int[n];
            for (int m = 0; m < n; ++m)
                a[m] = Selafin.ConnectivityFtn1Based[elementIndex, m];

            return a;
        }

        public override int GetVertexCount(int elementIndex)
        {
            if (!Initialised)
                Initialise();

            return Selafin.PointsPerElementCount;
        }

        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            throw new Exception("No M Coordinates in Selafin file");
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            if (!Initialised)
                Initialise();

            Debug.Assert(vertexIndex == 0);
            return Selafin.CoordinatesFtn1Based[elementIndex + 1, 0];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            if (!Initialised)
                Initialise();

            Debug.Assert(vertexIndex == 0);
            return Selafin.CoordinatesFtn1Based[elementIndex + 1, 1];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            throw new Exception("No Z Coordinates in Selafin file");
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetSelafinElements(Selafin.FileInfo.FullName);
        }
    }
}
