
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ElementSetUnoptimisedStorage : ElementSet, IElementSetProposed, IPersistence
    {
        public List<Element> Elements { get; private set; }

        int _binaryPersistenceLimit = 20;

        public ElementSetUnoptimisedStorage()
        {
            Elements = new List<Element>();
        }

        public ElementSetUnoptimisedStorage(ISpatialDefinition iSpatialDefinition, ElementType elementType, IEnumerable<Element> elements)
            : base(iSpatialDefinition, elementType, false, false)
        {
            Elements = new List<Element>(elements);
            ElementCount = Elements.Count;
        }

        public ElementSetUnoptimisedStorage(ISpatialDefinition iSpatialDefinition, ElementType elementType, IEnumerable<Element> elements, bool hasZ, bool hasM)
            : base(iSpatialDefinition, elementType, hasZ, hasM)
        {
            Elements = new List<Element>(elements);
            ElementCount = Elements.Count;
        }

        public ElementSetUnoptimisedStorage(IElementSet es2)
            : base(es2)
        {
            UpdateGeometry(es2);
        }

        public ElementSetUnoptimisedStorage(OpenMI.Standard.IElementSet es1)
            : base(es1)
        {
            Elements = new List<Element>(ElementCount);

            for (int n = 0; n < ElementCount; ++n)
                Elements.Add(new Element(es1, n));

            ElementCount = Elements.Count;
        }

        public override IIdentifiable GetElementId(int index)
        {
            return Elements[index].Identity;
        }

        public override int GetElementIndex(IIdentifiable elementId)
        {
            return Elements.FindIndex(i => i.Identity.Id == elementId.Id);
        }

        public override int GetFaceCount(int elementIndex)
        {
            return Elements[elementIndex].Faces.Count();
        }

        public override int[] GetFaceVertexIndices(int elementIndex, int faceIndex)
        {
            return Elements[elementIndex].Faces[faceIndex];
        }

        public override int GetVertexCount(int elementIndex)
        {
            return Elements[elementIndex].X.Length;
        }

        public override double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            if (!HasM)
                throw new Exception("ElementSet has no M coord");

            return Elements[elementIndex].M[vertexIndex];
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            return Elements[elementIndex].X[vertexIndex];
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            return Elements[elementIndex].Y[vertexIndex];
        }

        public override double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            if (!HasZ)
                throw new Exception("ElementSet has no Z coord");

            return Elements[elementIndex].Z[vertexIndex];
        }

        public const string XName = "ElementSetUnoptimisedStorage";
        public const string XFile = "BinaryFile";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ISpatialDefinition spatial;
            bool hasZ, hasM;
            ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
            SetSpatial(spatial);
            HasZ = hasZ;
            HasM = hasM;

            var binaryFile = xElement.Elements(XFile).SingleOrDefault();

            if (binaryFile == null)
            {
                Elements = xElement
                    .Elements(Element.XName)
                    .Select(x => 
                    {
                        var d = new Element();
                        d.Initialise(x, accessor);
                        return d;
                    })
                    .ToList();
            }
            else
            {
                string relative = binaryFile.Value;

                var uri = new Uri(accessor.Uri, relative);

                using (FileStream fs = File.OpenRead(uri.LocalPath))
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    Elements = (List<Element>)bf.Deserialize(fs);
                    ElementCount = Elements.Count;
                }
            }
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            if (ElementCount < _binaryPersistenceLimit)
                return new XElement(XName,
                    Persistence.ElementSet.Persist(this, accessor),
                    Elements.Select(e => e.Persist(accessor)));

            // Binary

            var uri = Utilities.NewUniqueUri(accessor.Uri, Caption, ".bin");
            var uriRelative = accessor.Uri.MakeRelativeUri(uri).ToString();

            using (FileStream fs = File.Create(uri.LocalPath))
            {
                BinaryFormatter bf = new BinaryFormatter();

                bf.Serialize(fs, Elements);
            }

            return new XElement(XName,
                new XElement(XFile, uriRelative),
                Persistence.ElementSet.Persist(this, accessor));
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetUnoptimisedStorage(this);
        }

        public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return true;
        }

        public void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            Elements = new List<Element>(elementSetEdits.ElementCount);

            for (int n = 0; n < elementSetEdits.ElementCount; ++n)
                Elements.Add(new Element(elementSetEdits, n));

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt; 
            ElementCount = elementSetEdits.ElementCount;
        }

        public IList<IArgument> Arguments
        {
            get { return new IArgument[] { }; }
        }

        public void Initialise()
        { }
    }
}


