using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk 
{
    /// <summary>
    /// A general ElementSet implementation which assumes no specific knowledge
    /// that might allow for more optimal storage. Useful for editing ElementSet's
    /// but not expected to be used where efficiency matters.
    /// </summary>
    public class ElementSetEditable : IElementSetProposed
    {
        public EditableElements ElementsEditable { get; set; }

        public ElementType ElementType { get { return ElementsEditable.ElementType; } }
        public int ElementCount { get { return ElementsEditable.Elements.Count; } }
        
        public string SpatialReferenceSystemWkt { get; set; }
        public int Version { get; set; }

        public string Caption
        {
            get { return ElementsEditable.Caption; }
            set { ElementsEditable.Caption = value; }
        }

        public string Description
        {
            get { return ElementsEditable.Description; }
            set { ElementsEditable.Description = value; }
        }

        public ElementSetEditable()
        {
            SpatialReferenceSystemWkt = string.Empty;
        }

        public ElementSetEditable(ElementType elementType, string spatialReferenceSystemWkt, EditableElements elements)
        {
            ElementsEditable = new EditableElements(elements);
            SpatialReferenceSystemWkt = spatialReferenceSystemWkt;
        }

        public ElementSetEditable(ElementSetEditable elementSet)
        {
            ElementsEditable = new EditableElements(elementSet.ElementsEditable);
            SpatialReferenceSystemWkt = elementSet.SpatialReferenceSystemWkt;
            Version = elementSet.Version;
        }

        public virtual object Clone()
        {
            return new ElementSetEditable(this);
        }

        public virtual bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return true;
        }

        public virtual void UpdateGeometry(IElementSet elementSetEdits)
        {
            ElementsEditable = new EditableElements(elementSetEdits);

            Version = elementSetEdits.Version;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
        }

        public virtual IIdentifiable GetElementId(int index)
        {
            return ElementsEditable
                .Elements[index]
                .Id;
        }

        public virtual int GetElementIndex(IIdentifiable elementId)
        {
            return ElementsEditable
                .Elements
                .FindIndex(i => i.Id.Id == elementId.Id);
        }

        public virtual int GetFaceCount(int elementIndex)
        {
            // Polyhedrons NOT implemented
            return 0;
        }

        public virtual int[] GetElementVertexIndices(int elementIndex, int faceIndex)
        {
            return ElementsEditable
                .Elements[elementIndex]
                .VerticeIndexs
                .ToArray();
        }

        public virtual int GetVertexCount(int elementIndex)
        {
            return ElementsEditable
                .Elements[elementIndex]
                .VerticeIndexs
                .Count;
        }

        public virtual double GetVertexMCoordinate(int elementIndex, int vertexIndex)
        {
            try
            {
                if (ElementsEditable.UniqueVertices)
                {
                    // ElementSetEditable.VerticeIndexs are NOT {0,1} if unique but say {5,6}
                    var nVertex = ElementsEditable.Elements[elementIndex].VerticeIndexs[vertexIndex];

                    return ElementsEditable
                        .Vertices
                        .Where(v => v.IndexVertex == nVertex)
                        .Select(v => v.Coords[HasZ ? 3 : 2])
                        .Single();
                }

                return ElementsEditable
                    .Vertices
                    .Where(v => v.IndexElement == elementIndex && v.IndexVertex == vertexIndex)
                    .Select(v => v.Coords[HasZ ? 3 : 2])
                    .Single();
            }
            catch (System.Exception)
            {
                return double.NaN;
            }
        }

        public virtual double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            try
            {
                if (ElementsEditable.UniqueVertices)
                {
                    // ElementSetEditable.VerticeIndexs are NOT {0,1} if unique but say {5,6}
                    var nVertex = ElementsEditable.Elements[elementIndex].VerticeIndexs[vertexIndex];

                    return ElementsEditable
                        .Vertices
                        .Where(v => v.IndexVertex == nVertex)
                        .Select(v => v.Coords[0])
                        .Single();
                }

                return ElementsEditable
                    .Vertices
                    .Where(v => v.IndexElement == elementIndex && v.IndexVertex == vertexIndex)
                    .Select(v => v.Coords[0])
                    .Single();
            }
            catch (System.Exception)
            {
                return double.NaN;
            }
        }

        public virtual double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            try
            {
                if (ElementsEditable.UniqueVertices)
                {
                    // ElementSetEditable.VerticeIndexs are NOT {0,1} if unique but say {5,6}
                    var nVertex = ElementsEditable.Elements[elementIndex].VerticeIndexs[vertexIndex];

                    return ElementsEditable
                        .Vertices
                        .Where(v => v.IndexVertex == nVertex)
                        .Select(v => v.Coords[1])
                        .Single();
                }

                return ElementsEditable
                    .Vertices
                    .Where(v => v.IndexElement == elementIndex && v.IndexVertex == vertexIndex)
                    .Select(v => v.Coords[1])
                    .Single();
            }
            catch (System.Exception)
            {
                return double.NaN;
            }
        }

        public virtual double GetVertexZCoordinate(int elementIndex, int vertexIndex)
        {
            try 
            {
                if (ElementsEditable.UniqueVertices)
                {
                    // ElementSetEditable.VerticeIndexs are NOT {0,1} if unique but say {5,6}
                    var nVertex = ElementsEditable.Elements[elementIndex].VerticeIndexs[vertexIndex];

                    return ElementsEditable
                        .Vertices
                        .Where(v => v.IndexVertex == nVertex)
                        .Select(v => v.Coords[2])
                        .Single();
                }

                return ElementsEditable
                    .Vertices
                    .Where(v => v.IndexElement == elementIndex && v.IndexVertex == vertexIndex)
                    .Select(v => v.Coords[2])
                    .Single();
            }
            catch (System.Exception)
            {
                return double.NaN;
            }
        }

        public bool HasM
        {
            get { return ElementsEditable.HasM; }
            set
            {
                if (ElementsEditable.HasM != value)
                {
                    if (value)
                        AddM();
                    else
                        RemoveM();
                }
            }
        }

        public bool HasZ
        {
            get { return ElementsEditable.HasZ; }
            set
            {
                if (ElementsEditable.HasZ != value)
                {
                    if (value)
                        AddZ();
                    else
                        RemoveZ();
                }
            }
        }

        void AddZ()
        {
            if (ElementsEditable.HasM)
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], 0.0, v.Coords[2] });
            else
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], 0.0, });

            ElementsEditable.HasZ = true;
        }

        void AddM()
        {
            if (ElementsEditable.HasZ)
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], v.Coords[2], 0.0 });
            else
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], 0.0, });

            ElementsEditable.HasM = true;
        }

        void RemoveZ()
        {
            if (ElementsEditable.HasM)
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], v.Coords[3] });
            else
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1] });

            ElementsEditable.HasZ = false;
        }

        void RemoveM()
        {
            if (ElementsEditable.HasZ)
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1], v.Coords[2] });
            else
                ElementsEditable.Vertices.ForEach(v => v.Coords = new List<double> { v.Coords[0], v.Coords[1] });

            ElementsEditable.HasM = false;
        }

        public class Element
        {
            public Identity Id { get; set; }
            public List<int> VerticeIndexs { get; set; }

            public Element(Element element)
            {
                Id = new Identity(element.Id);
                VerticeIndexs = new List<int>(element.VerticeIndexs);
            }

            public Element(IIdentifiable id)
            {
                Id = new Identity(id);
                VerticeIndexs = new List<int>();
            }

            public Element(IIdentifiable id, IEnumerable<int> verticeIndexs)
            {
                Id = new Identity(id);
                VerticeIndexs = new List<int>(verticeIndexs);
            }

            public Element(IIdentifiable id, string verticeIndexs)
            {
                Id = new Identity(id);
                VerticeIndexs = ParseVertices(verticeIndexs);
            }

            public string VerticesField 
            { 
                get
                {
                    return VerticeIndexs
                        .Aggregate(new StringBuilder(), (sb2, c) => sb2.Append(c.ToString() + " "))
                        .ToString()
                        .TrimEnd();          
                }

                set { VerticeIndexs = ParseVertices(value); }
            }

            public static List<int> ParseVertices(string values)
            {
                var vertices = new List<int>();

                int i;

                foreach (var s in values.Split(' '))
                    if (s.Trim() != string.Empty && int.TryParse(s, out i))
                        vertices.Add(i);

                return vertices;
            }
        }

        public class Vertex
        {
            public int IndexElement { get; set; }
            public int IndexVertex { get; set; }
            public int IndexFace { get; set; }

            public List<double> Coords { get; set; }

            public Vertex(Vertex vertex)
            {
                IndexElement = vertex.IndexElement;
                IndexVertex = vertex.IndexVertex;
                IndexFace = vertex.IndexFace;
                Coords = new List<double>(vertex.Coords);
            }

            public Vertex(IEnumerable<double> coords, int nElement, int nVertex = 0, int nFace = 0)
            {
                Coords = new List<double>(coords);
                IndexElement = nElement;
                IndexVertex = nVertex;
                IndexFace = nFace;
            }

            public Vertex(IElementSet elementSet, int nElement, int nVertex = 0, int nFace = 0)
            {
                IndexElement = nElement;
                IndexVertex = nVertex;
                IndexFace = nFace;

                var len = elementSet.HasZ ? 3 : 2;

                if (elementSet.HasM)
                    ++len;

                Coords = new List<double>(len);
                Coords.Add(elementSet.GetVertexXCoordinate(nElement, nVertex));
                Coords.Add(elementSet.GetVertexYCoordinate(nElement, nVertex));
                if (elementSet.HasZ)
                    Coords.Add(elementSet.GetVertexZCoordinate(nElement, nVertex));
                if (elementSet.HasM)
                    Coords.Add(elementSet.GetVertexMCoordinate(nElement, nVertex));
            }

            public Vertex(IList<string> fields)
                : this(New(fields))
            { }

            public static ElementSetEditable.Vertex New(IList<string> fields)
            {
                int nIndex;
                int nElement = -1;
                int nVertex = -1;
                double coord;

                if (fields.Count > 0 && int.TryParse(fields[0], out nIndex))
                    nElement = nIndex;

                if (fields.Count > 1 && int.TryParse(fields[1], out nIndex))
                    nVertex = nIndex;

                var coords = new List<double>();

                for (int n = 2; n < fields.Count; ++n)
                    coords.Add(double.TryParse(fields[n], out coord) ? coord : double.NaN);

                return new ElementSetEditable.Vertex(coords, nElement, nVertex);
            }

            public static ElementSetEditable.Vertex New(IList<string> fields, int nVertex, int nElement = -1, int nFace = -1)
            {
                double coord;

                var coords = new double[fields.Count];

                for (int n = 0; n < fields.Count; ++n)
                    coords[n] = double.TryParse(fields[n], out coord) ? coord : double.NaN;

                return new ElementSetEditable.Vertex(coords, nElement, nVertex, nFace);
            }

            public string CsvCoords
            {
                get
                {
                    return Coords
                        .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c + ","))
                        .ToString()
                        .Trim(',');
                }
            }

            public class CompareVertexIndexEquality : IEqualityComparer<Vertex>
            {
                public bool Equals(Vertex x, Vertex y)
                {
                    return x.IndexVertex == y.IndexVertex;
                }

                public int GetHashCode(Vertex obj)
                {
                    return obj.IndexVertex.GetHashCode();
                }
            }

            public class CompareVertexIndex : IComparer<Vertex>
            {
                public int Compare(Vertex x, Vertex y)
                {
                    return x.IndexVertex.CompareTo(y.IndexVertex);
                }
            }
        }

        public class EditableElements : IDescribable
        {
            public string Caption { get; set; }
            public string Description { get; set; }
            public List<Element> Elements { get; set; }
            public List<Vertex> Vertices { get; set; }

            public ElementType ElementType { get; set; }
            public bool HasZ { get; set; }
            public bool HasM { get; set; }
            public bool UniqueVertices { get; set; }

            public EditableElements(ElementType elementType, IEnumerable<Element> elements = null, IEnumerable<Vertex> vertices = null, bool hasZ = false, bool hasM = false, bool uniqueVertices = false)
            {
                Caption = elementType.ToString() + "s";
                Description = string.Empty;
                ElementType = elementType;
                Elements = elements == null
                    ? new List<Element>()
                    : new List<Element>(elements);
                Vertices = vertices == null
                    ? new List<Vertex>()
                    : new List<Vertex>(vertices);

                HasZ = hasZ;
                HasM = hasM;
                UniqueVertices = uniqueVertices;
            }

            /// <summary>
            /// Cloning constructor
            /// </summary>
            /// <param name="elements"></param>
            public EditableElements(EditableElements elements)
            {
                Caption = elements.Caption;
                Description = elements.Description;
                ElementType = elements.ElementType;
                Elements = elements.Elements.Select(e => new Element(e)).ToList();
                Vertices = elements.Vertices.Select(v => new Vertex(v)).ToList();
                HasZ = elements.HasZ;
                HasM = elements.HasM;
                UniqueVertices = elements.UniqueVertices;
            }

            public EditableElements(IElementSet elementSet)
            {
                Contract.Requires(elementSet != null, "elementSet != null");

                Caption = elementSet.Caption;
                Description = elementSet.Description;
                ElementType = elementSet.ElementType;

                switch (ElementType)
                {
                    case ElementType.IdBased:
                        InitialiseIdBased(elementSet);
                        break;
                    case ElementType.Point:
                        InitialisePoint(elementSet);
                        break;
                    case ElementType.PolyLine:
                    case ElementType.Polygon:
                        InitialisePoly(elementSet);
                        break;
                    default:
                        throw new NotImplementedException(ElementType.ToString());
                }
            }

            void InitialiseIdBased(IElementSet elementSet)
            {
                Elements = new List<Element>(elementSet.ElementCount);

                for (int nElement = 0; nElement < elementSet.ElementCount; ++nElement)
                    Elements.Add(new Element(new Identity(elementSet.GetElementId(nElement))));

                Vertices = new List<Vertex>();
            }

            void InitialisePoint(IElementSet elementSet)
            {
                Elements = new List<Element>(elementSet.ElementCount);
                Vertices = new List<Vertex>(elementSet.ElementCount);

                for (int nElement = 0; nElement < elementSet.ElementCount; ++nElement)
                {
                    Elements.Add(new Element(new Identity(elementSet.GetElementId(nElement))));
                    Vertices.Add(new Vertex(elementSet, nElement));
                }

                HasZ = elementSet.HasZ;
                HasM = elementSet.HasM;
            }

            void InitialisePoly(IElementSet elementSet)
            {
                HasZ = elementSet.HasZ;
                HasM = elementSet.HasM;

                var editableElementSet = elementSet as ElementSetEditable;

                if (editableElementSet != null)
                {
                    Elements = editableElementSet
                        .ElementsEditable
                        .Elements
                        .Select(e => new Element(e))
                        .ToList();

                    Vertices = editableElementSet
                        .ElementsEditable
                        .Vertices
                        .Select(v => new Vertex(v))
                        .ToList();

                    UniqueVertices = editableElementSet.ElementsEditable.UniqueVertices;

                    return;
                }

                var uniqueElements = elementSet as ElementSetVerticesUniqueBase;

                if (uniqueElements != null)
                {
                    Elements = uniqueElements.ElementSetEditableElements();
                    Vertices = uniqueElements.ElementSetEditableVertices();
                    UniqueVertices = true;

                    return;
                }

                Elements = new List<Element>(elementSet.ElementCount);
                Vertices = new List<Vertex>(elementSet.ElementCount);

                for (int nElement = 0; nElement < elementSet.ElementCount; ++nElement)
                {
                    var vertices = Enumerable.Range(0, elementSet.GetVertexCount(nElement));

                    Elements.Add(new Element(
                        new Identity(elementSet.GetElementId(nElement)),
                        vertices));

                    Vertices.AddRange(vertices.Select(nVertex =>
                        new Vertex(elementSet, nElement, nVertex)));
                }
            }

            public void VerticesAddMissing()
            {
                Vertices.AddRange(MissingVertices());
            }

            public EValidation Validate(out string whyNot)
            {
                var missing = MissingVertices();

                if (missing.Count > 0)
                {
                    whyNot = string.Format("Has {0} missing vertices", missing.Count);
                    return EValidation.Error;
                }

                if (Vertices.Any(v => v.Coords.Any(c => double.IsNaN(c))))
                {
                    whyNot = "Has at least one vertex coordinate of double.NaN";
                    return EValidation.Error;
                }

                if (Vertices.Any(v => v.Coords.Any(c => double.IsInfinity(c))))
                {
                    whyNot = "Has at least one vertex coordinate of +/- Infinity";
                    return EValidation.Warning;
                }

                whyNot = string.Empty;
                return EValidation.Valid;
            }

            public List<Vertex> MissingVertices()
            {
                int nCoords = HasZ ? 3 : 2;
                if (HasM)
                    ++nCoords;

                var coords = Enumerable
                    .Range(0, nCoords)
                    .Select(n => double.NaN);

                var toAdd = new List<Vertex>();

                if (UniqueVertices)
                {
                    var indexs = Elements
                        .SelectMany(e => e.VerticeIndexs)
                        .ToList();

                    indexs.Sort();

                    var indices = indexs
                        .Distinct();

                    foreach (var nVertex in indices)
                        if (!(Vertices.Any(v => v.IndexVertex == nVertex)))
                            toAdd.Add(new Vertex(coords, -1, nVertex));
                }
                else
                {
                    var indexs = new List<Tuple<int, int>>();

                    for (int nElement = 0; nElement < Elements.Count; ++nElement)
                        indexs.AddRange(Elements[nElement]
                            .VerticeIndexs
                            .Select(nVertex => Tuple.Create(nElement, nVertex)));

                    indexs.Sort();

                    var indices = indexs
                        .Distinct();
                 
                    foreach (var t in indices)
                        if (!(Vertices.Any(v => v.IndexElement == t.Item1 && v.IndexVertex == t.Item2)))
                            toAdd.Add(new Vertex(coords, t.Item1, t.Item2));
                }

                return toAdd;
            }
        }

        public int[] GetFaceVertexIndices(int elementIndex, int faceIndex)
        {
            throw new NotImplementedException();
        }

        public IList<IArgument> Arguments
        {
            get { return new IArgument[] {}; }
        }

        public void Initialise()
        { }
    }
}
