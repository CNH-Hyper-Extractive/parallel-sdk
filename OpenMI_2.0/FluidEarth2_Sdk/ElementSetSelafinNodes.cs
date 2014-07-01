using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Diagnostics;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetSelafinNodes : ElementSetSelafinBase
    {
        public ElementSetSelafinNodes()
            : base(new SpatialDefinition(new Describes("Selafin Nodes"), 0), ElementType.Point)
        { }

        public ElementSetSelafinNodes(string selafinFile)
            : base(new SpatialDefinition(new Describes("Selafin Nodes"), 0), ElementType.Point)
        { }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetSelafinNodes(Selafin.FileInfo.FullName);
        }

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
            return 0;
        }

        public override int[] GetFaceVertexIndices(int elementIndex, int faceIndex)
        {
            throw new Exception("No face vertices for point based elementSet");
        }

        public override int GetVertexCount(int elementIndex)
        {
            return 0;
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
    }
}
