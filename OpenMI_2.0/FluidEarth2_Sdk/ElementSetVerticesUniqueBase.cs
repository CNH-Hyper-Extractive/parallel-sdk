using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public abstract class ElementSetVerticesUniqueBase : ElementSetIds
    {
        protected abstract int IndexMapping(int elementIndex, int vertexIndex);
        protected abstract IList<IEnumerable<int>> ElementUniqueIndexMapping();

        public double[] X { get; private set; }
        public double[] Y { get; private set; }
        public double[] Z { get; private set; }
        public double[] M { get; private set; }

        public ElementSetVerticesUniqueBase(ElementType elementType)
        {
            ElementType = elementType;
            X = new double[] { };
            Y = new double[] { };
        }

        public ElementSetVerticesUniqueBase(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            ElementType elementType,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z = null,
            IEnumerable<double> m = null)
            : base(spatial, ids, elementType, z != null, m != null)
        {
            Contract.Requires(y.Count() == x.Count(), "y.Count() == x.Count(); {0} != {1}", y.Count(), x.Count());
            Contract.Requires(z == null || z.Count() == x.Count(), "z == null || z.Count() == x.Count(); z.Count() != {0}", x.Count());
            Contract.Requires(m == null || m.Count() == x.Count(), "m == null || m.Count() == x.Count(); m.Count() != {0}", x.Count());

            X = x.ToArray();
            Y = y.ToArray();
            Z = z != null ? z.ToArray() : null;
            M = m != null ? m.ToArray() : null;
        }

        public override int GetVertexCount(int elementIndex)
        {
            throw new NotImplementedException("Re-implement in derived class");
        }

        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(HasM, "HasM");
            return M[IndexMapping(elementIndex, vertexIndex)];
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            return X[IndexMapping(elementIndex, vertexIndex)];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            return Y[IndexMapping(elementIndex, vertexIndex)];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            Contract.Requires(HasZ, "HasZ");
            return Z[IndexMapping(elementIndex, vertexIndex)];
        }

        public new const string XName = "ElementSetPolygonsUniqueVertices";
        public const string XVertices = "ElementVertices";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            X = Persistence.Values<double>.Parse(xElement.Element("X"), accessor);
            Y = Persistence.Values<double>.Parse(xElement.Element("Y"), accessor);
            Z = null;
            M = null;

            if (HasZ)
                Z = Persistence.Values<double>.Parse(xElement.Element("Z"), accessor);
            if (HasM)
                Z = Persistence.Values<double>.Parse(xElement.Element("M"), accessor);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                base.Persist(accessor),
                new XElement("X", Persistence.Values<double>.Persist(X)),
                new XElement("Y", Persistence.Values<double>.Persist(Y)));
 
            if (HasZ)
                xml.Add(new XElement("Z", Persistence.Values<double>.Persist(Z)));
            if (HasM)
                xml.Add(new XElement("M", Persistence.Values<double>.Persist(M)));

            return xml;
        }

        public override void UpdateGeometry(IElementSet elementSet)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSet), "updateGeometryAvailable(elementSet)");

            base.UpdateGeometry(elementSet);

            var es = elementSet as ElementSetVerticesUniqueBase;

            if (es != null)
            {
                X = es.X.ToArray();
                Y = es.Y.ToArray();
                Z = es.Z == null ? null : es.Z.ToArray();
                M = es.M == null ? null : es.M.ToArray();

                return;
            }

            var es2 = elementSet as ElementSetEditable;

            Contract.Requires(es2 != null, "elementSet is ElementSetEditable");

            int nVertexMax = es2
                .ElementsEditable
                .Vertices
                .Select(v => v.IndexVertex).Max() + 1;

            X = new double[nVertexMax];
            Y = new double[nVertexMax];

            if (es2.ElementsEditable.HasZ)
               Z = new double[nVertexMax];

            if (es2.ElementsEditable.HasM)
                M = new double[nVertexMax];

            int nM = es2.ElementsEditable.HasZ ? 3 : 2;

            foreach (var v in es2.ElementsEditable.Vertices)
            {
                X[v.IndexVertex] = v.Coords[0];
                Y[v.IndexVertex] = v.Coords[1];

                if (es2.ElementsEditable.HasZ)
                    Z[v.IndexVertex] = v.Coords[2];

                if (es2.ElementsEditable.HasM)
                    M[v.IndexVertex] = v.Coords[nM];
            }
        }

        public List<ElementSetEditable.Element> ElementSetEditableElements()
        {
            var es = new List<ElementSetEditable.Element>(ElementCount);

            var elementUniqueVerticeIndexs = ElementUniqueIndexMapping();

            for (int nElement = 0; nElement < ElementCount; ++nElement)
                es.Add(new ElementSetEditable.Element(Ids[nElement], elementUniqueVerticeIndexs[nElement]));

            return es;
        }

        public List<ElementSetEditable.Vertex> ElementSetEditableVertices()
        {
            var vs = new List<ElementSetEditable.Vertex>(X.Length);

            for (int nUniqueVertex = 0; nUniqueVertex < X.Length; ++nUniqueVertex)
                vs.Add(new ElementSetEditable.Vertex(Coordinates(nUniqueVertex), -1, nUniqueVertex));

            return vs; 
        }

        public IEnumerable<double> Coordinates(int nVertex)
        {
            if (!HasZ && !HasM)
                return new double[] { X[nVertex], Y[nVertex] };
            if (HasZ && !HasM)
                return new double[] { X[nVertex], Y[nVertex], Z[nVertex] };
            if (!HasZ && HasM)
                return new double[] { X[nVertex], Y[nVertex], M[nVertex] };

            return new double[] { X[nVertex], Y[nVertex], Z[nVertex], M[nVertex] };
        }
    }
}
