
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ElementSetCuboidBlock : ElementSetIds, IPersistence
    {
        double[] _x, _y, _z, _m;
        int[,] _elementVertices;

        public ElementSetCuboidBlock()
        { }

        public ElementSetCuboidBlock(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z,
            IEnumerable<double> m,
            int[,] elementVertices)
            : base(spatial, ids, ElementType.Polyhedron, z != null, m != null)
        {
            if (x.Count() != ElementCount)
                throw new Exception(string.Format("x.Count() {1} != {2}",
                    x.Count(), ElementCount));
            if (y.Count() != ElementCount)
                throw new Exception(string.Format("y.Count() {1} != {2}",
                    y.Count(), ElementCount));

            if (HasZ && z.Count() != ElementCount)
                throw new Exception(string.Format("z.Count() {1} != {2}",
                    z.Count(), ElementCount));

            if (HasM && m.Count() != ElementCount)
                throw new Exception(string.Format("m.Count() {1} != {2}",
                    m.Count(), ElementCount));

            if (elementVertices == null || elementVertices.GetLength(0) != ElementCount)
                throw new Exception(string.Format("elementVertices.GetLength(0) {1} != {2}",
                    elementVertices == null ? "null" : elementVertices.GetLength(0).ToString(), ElementCount));

            if (elementVertices.GetLength(1) != 8)
                throw new Exception(string.Format("elementVertices.GetLength(1) {0} != 8",
                    elementVertices.GetLength(1).ToString()));

            _x = x.ToArray();
            _y = y.ToArray();

            if (HasZ)
                _z = z.ToArray();

            if (HasM)
                _m = m.ToArray();

            _elementVertices = elementVertices;
        }

        public ElementSetCuboidBlock(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z,
            IEnumerable<double> m,
            int nX, int nY, int nZ)
            : base(spatial, ids, ElementType.Polyhedron, z != null, m != null)
        {
            int nTotal = nX * nY * nZ;

            if (nTotal != ElementCount)
                throw new Exception(string.Format("nX * nY * nZ = {1} != {2}",
                    nTotal, ElementCount));

            if (x.Count() != ElementCount)
                throw new Exception(string.Format("x.Count() {1} != {2}",
                    x.Count(), ElementCount));
            if (y.Count() != ElementCount)
                throw new Exception(string.Format("y.Count() {1} != {2}",
                    y.Count(), ElementCount));

            if (HasZ && z.Count() != ElementCount)
                throw new Exception(string.Format("z.Count() {1} != {2}",
                    z.Count(), ElementCount));

            if (HasM && m.Count() != ElementCount)
                throw new Exception(string.Format("m.Count() {1} != {2}",
                    m.Count(), ElementCount));

            int[][] elementVertices = new int[nTotal][];

            for (int i = 


            _x = x.ToArray();
            _y = y.ToArray();

            if (HasZ)
                _z = z.ToArray();

            if (HasM)
                _m = m.ToArray();

            _elementVertices = elementVertices;
        }

        public ElementSetCuboidBlock(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override int GetFaceCount(int elementIndex)
        {
            return 6;
        }

        public override int[] GetFaceVertexIndices(int elementIndex, int faceIndex)
        {
            switch(faceIndex)
            {
                case 0:
                    return new int[] { 0, 1, 2, 3 };
                case 1:
                    return new int[] { 4, 5, 6, 7 };
                case 2:
                    return new int[] { 0, 1, 5, 4 };
                case 3:
                    return new int[] { 3, 2, 6, 7 };
                case 4:
                    return new int[] { 0, 4, 7, 3 };
                case 5:
                    return new int[] { 1, 5, 6, 2 };
                default:
                    throw new Exception(faceIndex.ToString());
            }
        }

        public override int GetVertexCount(int elementIndex)
        {
            return 8;
        }

        int _nX, int nY, int nZ;

        int GlobalVertex(int elementIndex, int vertexIndex)
        {
            switch (vertexIndex)
            {
                case 0:
                    return elementIndex 
            
            }

        }


        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {

            return _m[_elementVertices[elementIndex, vertexIndex]];
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            return _x[_elementVertices[elementIndex, vertexIndex]];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            return _y[_elementVertices[elementIndex, vertexIndex]];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            return _z[_elementVertices[elementIndex, vertexIndex]];
        }

        public const string XName = "ElementSetCuboidBlock";
        public const string XVertices = "ElementVertices";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ISpatialDefinition spatial;
            bool hasZ, hasM;
            ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
            SetSpatial(spatial);
            HasZ = hasZ;
            HasM = hasM;

            _ids = xElement
                .Elements(Persistence.Identity.XName)
                .Select(i => Persistence.Identity.Parse(i, accessor))
                .ToList();

            _x = Persistence.Values<double>.Parse(xElement.Element("X"), accessor);
            _y = Persistence.Values<double>.Parse(xElement.Element("Y"), accessor);
            _z = null;
            _m = null;
            _elementVertices = null;

            if (HasZ)
                _z = Persistence.Values<double>.Parse(xElement.Element("Z"), accessor);
            if (HasM)
                _z = Persistence.Values<double>.Parse(xElement.Element("M"), accessor);

            var vertices = xElement
                .Elements(XVertices)
                .ToList();

            if (vertices.Count == 0)
                return;

            _elementVertices = new int[vertices.Count, 8];

            for (int n = 0; n < vertices.Count; ++n)
            {
                var v = Persistence.Values<int>.Parse(vertices[n], accessor);

                for (int m = 0; m < 8; ++m)
                    _elementVertices[n,m] = v[m];
            }
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.ElementSet.Persist(this, accessor),
                _ids.Select(i => Persistence.Identity.Persist(i, accessor)),
                new XElement("X", Persistence.Values<double>.Persist(_x)),
                new XElement("Y", Persistence.Values<double>.Persist(_y)),
                _elementVertices.Select(v => new XElement(XVertices, Persistence.Values<int>.Persist(v))));

            if (HasZ)
                xml.Add(new XElement("Z", Persistence.Values<double>.Persist(_z)));
            if (HasM)
                xml.Add(new XElement("M", Persistence.Values<double>.Persist(_m)));

            return xml;
        }
    }
}
