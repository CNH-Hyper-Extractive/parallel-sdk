using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetVerticesUniqueIndexed : ElementSetVerticesUniqueBase
    {
        public int[][] IndexMap { get; private set; }

        public ElementSetVerticesUniqueIndexed(ElementType elementType)
            : base(elementType)
        {
            IndexMap = new int[0][];
        }

        public ElementSetVerticesUniqueIndexed(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            IEnumerable<IEnumerable<int>> elementVertices,
            ElementType elementType,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z = null,
            IEnumerable<double> m = null)
            : base(spatial, ids, elementType, x, y, z, m)
        {
            IndexMap = elementVertices
                .Select(a => a.ToArray())
                .ToArray();
        }

        public override int GetVertexCount(int elementIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);

            return IndexMap[elementIndex].Length;
        }

        protected override int IndexMapping(int elementIndex, int vertexIndex)
        {
            Contract.Requires(elementIndex > -1 && elementIndex < ElementCount,
                "elementIndex > -1 && elementIndex < ElementCount; -1 < {0} < {1}", elementIndex, ElementCount);
            Contract.Requires(vertexIndex > -1 && vertexIndex < IndexMap[elementIndex].Length,
                "vertexIndex > -1 && vertexIndex < VertexCount; -1 < {0} < {1}", vertexIndex, IndexMap[elementIndex].Length);

            return IndexMap[elementIndex][vertexIndex];
        }

        public new const string XName = "ElementSetVerticesUniqueIndexedBase";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            IndexMap = xElement
                .Elements("Indexes")
                .Select(x => Persistence.Values<int>.Parse(x, accessor))
                .ToArray();
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                base.Persist(accessor),
                IndexMap.Select(i => new XElement("Indexes", Persistence.Values<int>.Persist(i))));
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetVerticesUniqueIndexed(this, Ids, IndexMap, ElementType, X, Y, Z, M);
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSet)
        {
            return elementSet.ElementType == ElementType
                && (elementSet is ElementSetVerticesUniqueIndexed
                    || (elementSet is ElementSetEditable && ((ElementSetEditable)elementSet).ElementsEditable.UniqueVertices));
        }

        public override void UpdateGeometry(IElementSet elementSet)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSet), "updateGeometryAvailable(elementSet)");

            base.UpdateGeometry(elementSet);

            var es = elementSet as ElementSetVerticesUniqueIndexed;

            if (es != null)
            {
                IndexMap = es
                    .IndexMap
                    .Select(a => a.ToArray()).ToArray();

                return;
            }

            var es2 = elementSet as ElementSetEditable;

            Contract.Requires(es2 != null, "elementSet is ElementSetEditable");

            IndexMap = es2.ElementsEditable
                .Elements
                .Select(e => e.VerticeIndexs.ToArray())
                .ToArray();
        }

        protected override IList<IEnumerable<int>> ElementUniqueIndexMapping()
        {
            return IndexMap;
            /*
            var map = new List<List<int>>(ElementCount);

            for (int n = 0; n < ElementCount; ++n)
            {
                if (map[n] == null)              
                    map[n] = new List<int>();

                for (int m = 0; m <)



            }


                throw new NotImplementedException();
             */
        }
    }
}
