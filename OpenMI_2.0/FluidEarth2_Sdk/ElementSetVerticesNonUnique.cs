using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetVerticesNonUnique : ElementSetIds
    {
        public double[][] X { get; private set; }
        public double[][] Y { get; private set; }
        public double[][] Z { get; private set; }
        public double[][] M { get; private set; }

        public ElementSetVerticesNonUnique(ElementType elementType)
        {
            ElementType = elementType;
            X = new double[][] { };
            Y = new double[][] { };
        }

        public ElementSetVerticesNonUnique(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            ElementType elementType,
            IEnumerable<IEnumerable<double>> x,
            IEnumerable<IEnumerable<double>> y,
            IEnumerable<IEnumerable<double>> z = null,
            IEnumerable<IEnumerable<double>> m = null)
            : base(spatial, ids, elementType, z != null, m != null)
        {
            Contract.Requires(x.Count() == ids.Count(), "x.Count() == ids.Count(); {0} != {1}", x.Count(), ids.Count());
            Contract.Requires(y.Count() == ids.Count(), "y.Count() == ids.Count(); {0} != {1}", y.Count(), ids.Count());
            Contract.Requires(z == null || z.Count() == ids.Count(), "z == null || z.Count() == ids.Count(); z.Count() != {0}", ids.Count());
            Contract.Requires(m == null || m.Count() == ids.Count(), "m == null || m.Count() == ids.Count(); m.Count() != {0}", ids.Count());

            X = x.Select(a => a.ToArray()).ToArray();
            Y = y.Select(a => a.ToArray()).ToArray();
            Z = HasZ ? z.Select(a => a.ToArray()).ToArray() : null;
            M = HasM ? m.Select(a => a.ToArray()).ToArray() : null;
        }

        public override int GetVertexCount(int elementIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);

            return X[elementIndex].Length;
        }

        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);
            Contract.Requires(vertexIndex > -1 && vertexIndex < M[elementIndex].Length,
                "vertexIndex > -1 && vertexIndex < VertexCount; -1 < {0} < {1}", vertexIndex, M[elementIndex].Length);

            return M[elementIndex][vertexIndex];
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);
            Contract.Requires(vertexIndex > -1 && vertexIndex < X[elementIndex].Length,
                "vertexIndex > -1 && vertexIndex < VertexCount; -1 < {0} < {1}", vertexIndex, X[elementIndex].Length);

            return X[elementIndex][vertexIndex];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);
            Contract.Requires(vertexIndex > -1 && vertexIndex < Y[elementIndex].Length,
                "vertexIndex > -1 && vertexIndex < VertexCount; -1 < {0} < {1}", vertexIndex, Y[elementIndex].Length);

            return Y[elementIndex][vertexIndex];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);
            Contract.Requires(vertexIndex > -1 && vertexIndex < Z[elementIndex].Length,
                "vertexIndex > -1 && vertexIndex < VertexCount; -1 < {0} < {1}", vertexIndex, Z[elementIndex].Length);

            return Z[elementIndex][vertexIndex];
        }

        public new const string XName = "ElementSetVerticesNonUniqueBase";
        public const string XVertices = "ElementVertices";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            X = new double[ElementCount][];
            Y = new double[ElementCount][];

            if (HasZ)
                Z = new double[ElementCount][];
            if (HasM)
                M = new double[ElementCount][];

            int n = -1;

            foreach (var c in xElement.Elements("Coords"))
            {
                ++n;
                X[n] = Persistence.Values<double>.Parse(c.Element("X"), accessor);
                Y[n] = Persistence.Values<double>.Parse(c.Element("Y"), accessor);

                if (HasZ)
                    Z[n] = Persistence.Values<double>.Parse(c.Element("Z"), accessor);
                if (HasM)
                    M[n] = Persistence.Values<double>.Parse(c.Element("M"), accessor);
            }
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName, base.Persist(accessor));

            for (int n = 0; n < ElementCount; ++n)
            {
                var coords = new XElement("Coords",
                    new XElement("X", Persistence.Values<double>.Persist(X[n])),
                    new XElement("Y", Persistence.Values<double>.Persist(Y[n])));

                if (HasZ)
                    coords.Add(new XElement("Z", Persistence.Values<double>.Persist(Z[n])));
                if (HasM)
                    coords.Add(new XElement("M", Persistence.Values<double>.Persist(M[n])));

                xml.Add(coords);
            }

            return xml;
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetVerticesNonUnique(this, Ids, ElementType, X, Y, Z, M);
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits.ElementType == ElementType;
        }

        public override void UpdateGeometry(IElementSet elementSet)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSet), "updateGeometryAvailable(elementSet)");

            base.UpdateGeometry(elementSet);

            var es = elementSet as ElementSetVerticesNonUnique;

            if (es != null)
            {
                X = es.X.Select(a => a.ToArray()).ToArray();
                Y = es.Y.Select(a => a.ToArray()).ToArray();
                Z = es.Z == null ? null : es.Z.Select(a => a.ToArray()).ToArray();
                M = es.M == null ? null : es.M.Select(a => a.ToArray()).ToArray();

                return;
            }

            X = new double[elementSet.ElementCount][];
            Y = new double[elementSet.ElementCount][];
            Z = HasZ ? new double[elementSet.ElementCount][] : null;
            M = HasM ? new double[elementSet.ElementCount][] : null;

            for (int nElement = 0; nElement < elementSet.ElementCount; ++nElement)
            {
                var nVertexLength = elementSet.GetVertexCount(nElement);

                X[nElement] = new double[nVertexLength];
                Y[nElement] = new double[nVertexLength];

                if (HasZ)
                    Z[nElement] = new double[nVertexLength];

                if (HasM)
                    M[nElement] = new double[nVertexLength];

                var vertexIndexs = Enumerable.Range(0, nVertexLength).ToArray();

                foreach (var nVertex in vertexIndexs)
                {
                    X[nElement][nVertex] = elementSet.GetVertexXCoordinate(nElement, nVertex);
                    Y[nElement][nVertex] = elementSet.GetVertexYCoordinate(nElement, nVertex);

                    if (HasZ)
                        Z[nElement][nVertex] = elementSet.GetVertexZCoordinate(nElement, nVertex);
                    if (HasM)
                        M[nElement][nVertex] = elementSet.GetVertexMCoordinate(nElement, nVertex);
                }
            }
        }
    }
}
