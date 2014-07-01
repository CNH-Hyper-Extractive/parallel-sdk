using System;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    [Serializable]
    public class Element : IPersistence
    {
        public IIdentifiable Identity { get; private set; }
        public double[] X { get; private set; }
        public double[] Y { get; private set; }
        public double[] Z { get; private set; }
        public double[] M { get; private set; }
        public int[][] Faces { get; private set; }

        public Element()
        {
            X = new double[] {};
            Y = new double[] { };
            Z = null;
            M = null;
            Faces = null;
        }

        public Element(IIdentifiable identity, 
            double[] x, double[] y)
            : this(identity, x, y, null, null, null)
        {}

        public Element(IIdentifiable identity, 
            double[] x, double[] y, 
            double[] z, double[] m, 
            int[][] faces)
        {
            Identity = identity;
            X = x;
            Y = y;
            Z = z;
            M = m;
            Faces = faces;
        }

        public Element(IElementSet set, int nElement)
        {
            Identity = new Identity(set.GetElementId(nElement));

            int nV = set.GetVertexCount(nElement);
            int nF = set.GetFaceCount(nElement);

            X = new double[nV];
            Y = new double[nV];

            if (set.HasZ)
                Z = new double[nV];
            if (set.HasM)
                M = new double[nV];

            for (int n = 0; n < nV; ++n)
            {
                X[n] = set.GetVertexXCoordinate(nElement, n);
                Y[n] = set.GetVertexYCoordinate(nElement, n);

                if (set.HasZ)
                    Z[n] = set.GetVertexZCoordinate(nElement, n);
                if (set.HasM)
                    M[n] = set.GetVertexMCoordinate(nElement, n);
            }

            if (nF > 0)
            {
                Faces = new int[nF][];

                for (int n = 0; n < nF; ++n)
                    Faces[n] = set.GetFaceVertexIndices(nElement, n);
            }
        }

        public Element(OpenMI.Standard.IElementSet set, int nElement)
        {
            Identity = new Identity(set.GetElementID(nElement));

            int nV = set.GetVertexCount(nElement);
            int nF = set.GetFaceCount(nElement);

            X = new double[nV];
            Y = new double[nV];

            bool hasZ = set.ElementType == OpenMI.Standard.ElementType.XYZPoint
                || set.ElementType == OpenMI.Standard.ElementType.XYZLine
                || set.ElementType == OpenMI.Standard.ElementType.XYZPolyLine
                || set.ElementType == OpenMI.Standard.ElementType.XYZPolygon
                || set.ElementType == OpenMI.Standard.ElementType.XYZPolyhedron;

            if (hasZ)
                Z = new double[nV];

            for (int n = 0; n < nV; ++n)
            {
                X[n] = set.GetXCoordinate(nElement, n);
                Y[n] = set.GetYCoordinate(nElement, n);

                if (hasZ)
                    Z[n] = set.GetZCoordinate(nElement, n);
            }

            if (nF > 0)
            {
                Faces = new int[nF][];

                for (int n = 0; n < nF; ++n)
                    Faces[n] = set.GetFaceVertexIndices(nElement, n);
            }
        }

        public const string XName = "Element";
        public const string XFace = "Face";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            X = Persistence.Values<double>.Parse(xElement.Element("X"), accessor);
            Y = Persistence.Values<double>.Parse(xElement.Element("Y"), accessor);
            Z = null;
            M = null;
            Faces = null;

            var z = xElement.Elements("Z").SingleOrDefault();
            if (z != null)
                Z = Persistence.Values<double>.Parse(z, accessor);

            var m = xElement.Elements("M").SingleOrDefault();
            if (m != null)
                M = Persistence.Values<double>.Parse(m, accessor);

            var faces = xElement
                .Elements(XFace)
                .ToList();

            if (faces.Count == 0)
                return;

            Faces = new int[faces.Count][];

            for (int n = 0; n < faces.Count; ++n)
                Faces[n] = Persistence.Values<int>.Parse(faces[n], accessor);
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.Identity.Persist(Identity, accessor),
                new XElement("X", Persistence.Values<double>.Persist(X)),
                new XElement("Y", Persistence.Values<double>.Persist(Y)));

            if (Z != null)
                xml.Add(new XElement("Z", Persistence.Values<double>.Persist(Z)));
            if (M != null)
                xml.Add(new XElement("M", Persistence.Values<double>.Persist(Z)));

            if (Faces != null)
                xml.Add(Faces.Select(f => new XElement(XFace, Persistence.Values<int>.Persist(f))));

            return xml;
        }
    }
}
