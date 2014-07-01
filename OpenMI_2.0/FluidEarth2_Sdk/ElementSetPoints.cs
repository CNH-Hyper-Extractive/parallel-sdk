
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetPoints : ElementSetIds
    {
        public double[] X { get; private set; }
        public double[] Y { get; private set; }
        public double[] Z { get; private set; }
        public double[] M { get; private set; }

        public ElementSetPoints()
        {
            ElementType = ElementType.Point;
            X = new double[] { };
            Y = new double[] { };
        }

        public ElementSetPoints(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z = null,
            IEnumerable<double> m = null)
            : base(spatial, ids, ElementType.Point, z != null, m != null)
        {
            if (x.Count() != ElementCount)
                throw new Exception(string.Format("x.Count() {0} != {1}",
                    x.Count(), ElementCount));
            if (y.Count() != ElementCount)
                throw new Exception(string.Format("y.Count() {0} != {1}",
                    y.Count(), ElementCount));

            if (HasZ && (z == null || z.Count() != ElementCount))
                throw new Exception(string.Format("z.Length = {0} != {1}",
                    z == null ? "null" : z.Count().ToString(), ElementCount));
            if (HasM && (m == null || m.Count() != ElementCount))
                throw new Exception(string.Format("m.Length = {0} != {1}",
                    m == null ? "null" : m.Count().ToString(), ElementCount));

            X = x.ToArray();
            Y = y.ToArray();

            if (HasZ)
                Z = z.ToArray();

            if (HasM)
                M = m.ToArray();
        }

        public override object Clone()
        {
            return new ElementSetPoints(this, Ids, X, Y, Z, M);
        }

        public override int GetVertexCount(int elementIndex)
        {
            return 1;
        }

        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            return M[elementIndex];
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            return X[elementIndex];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            return Y[elementIndex];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            return Z[elementIndex];
        }

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ISpatialDefinition spatial;
            bool hasZ, hasM;
            ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
            SetSpatial(spatial);
            HasZ = hasZ;
            HasM = hasM;

            SetIds(xElement
                .Elements(Persistence.Identity.XName)
                .Select(i => Persistence.Identity.Parse(i, accessor)));

            X = Persistence.Values<double>.Parse(xElement.Element("X"), accessor);
            Y = Persistence.Values<double>.Parse(xElement.Element("Y"), accessor);

            if (HasZ)
                Z = Persistence.Values<double>.Parse(xElement.Element("Z"), accessor);
            if (HasM)
                Z = Persistence.Values<double>.Parse(xElement.Element("M"), accessor);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.ElementSet.Persist(this, accessor),
                Ids.Select(i => Persistence.Identity.Persist(i, accessor)),
                new XElement("X", Persistence.Values<double>.Persist(X)),
                new XElement("Y", Persistence.Values<double>.Persist(Y)));

            if (HasZ)
                xml.Add(new XElement("Z", Persistence.Values<double>.Persist(Z)));
            if (HasM)
                xml.Add(new XElement("M", Persistence.Values<double>.Persist(M)));

            return xml;
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits.ElementType == ElementType.Point;
        }

        public override void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            base.UpdateGeometry(elementSetEdits);

            X = new double[elementSetEdits.ElementCount];
            Y = new double[elementSetEdits.ElementCount];

            Z = elementSetEdits.HasZ ? new double[elementSetEdits.ElementCount] : null;
            M = elementSetEdits.HasM ? new double[elementSetEdits.ElementCount] : null;

            int nVertex = 0;

            for (int nElement = 0; nElement < elementSetEdits.ElementCount; ++nElement)
            {
                X[nElement] = elementSetEdits.GetVertexXCoordinate(nElement, nVertex);
                Y[nElement] = elementSetEdits.GetVertexYCoordinate(nElement, nVertex);

                if (HasZ)
                    Z[nElement] = elementSetEdits.GetVertexZCoordinate(nElement, nVertex);
                if (HasM)
                    M[nElement] = elementSetEdits.GetVertexMCoordinate(nElement, nVertex);
            }
        }
    }
}
