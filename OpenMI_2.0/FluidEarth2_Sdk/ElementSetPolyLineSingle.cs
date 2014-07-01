using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Each element is a polyline with a single value along its length
    /// </summary>
    /// 
    public class ElementSetPolyLineSingle : ElementSetVerticesUniqueBase
    {
        int[][] _indexMap;

        public ElementSetPolyLineSingle()
            : base(ElementType.PolyLine)
        { }

        /// <summary>
        /// ElementSet for a single polyline, each segment will be an element.
        /// </summary>
        /// <param name="spatial">Spatial definition</param>
        /// <param name="polyLineX">PolyLine X coords #segments + 1</param>
        /// <param name="polyLineY">PolyLine Y coords #segments + 1</param>
        /// <param name="polyLineZ">Optional polyline Z coords #segments + 1</param>
        /// <param name="polyLineM">Optional polyline M coords #segments + 1</param>
        public ElementSetPolyLineSingle(
            ISpatialDefinition spatial,
            double[] polyLineX,
            double[] polyLineY,
            double[] polyLineZ = null,
            double[] polyLineM = null)
            : base(spatial, SegmentIds(spatial), ElementType.PolyLine, polyLineX, polyLineY, polyLineZ, polyLineM)
        { }

        public int[][] IndexMap
        {
            get
            {
                if (_indexMap == null)
                    _indexMap = GetElementVertexMapping(this);

                return _indexMap;
            }
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

        public new const string XName = "ElementSetSinglePolyLine";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, base.Persist(accessor));
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetPolyLineSingle(this, X, Y, Z, M);
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            string whyNot;
            return IsSinglePolyLine(elementSetEdits, out whyNot);
        }

        public override void UpdateGeometry(IElementSet elementSet)
        {
            base.UpdateGeometry(elementSet);

            _indexMap = null;
        }

        public static bool IsSinglePolyLine(IElementSet elementSet, out string whyNot)
        {
            whyNot = string.Empty;

            if (elementSet is ElementSetPolyLineSingle)
                return true;

            if (elementSet.ElementType != ElementType.PolyLine)
            {
                whyNot = "elementSet.ElementType != ElementType.PolyLine";
                return false;
            }

            for (int nElement = 0; nElement < elementSet.ElementCount; ++nElement)
            {
                if (elementSet.GetVertexCount(nElement) != 2)
                {
                    whyNot = "For at least one nElement elementSet.GetVertexCount(nElement) != 2";
                    return false;
                }
            }

            for (int nElement = 1; nElement < elementSet.ElementCount; ++nElement)
            {
                var len = Distance(elementSet, nElement, 0, nElement - 1, 1);

                if (len > 10.0 * double.Epsilon)
                {
                    whyNot = "adjacent element vertices > 10.0 * double.Epsilon apart";
                    return false;
                }
            }

            return true;
        }

        public static double Distance(IElementSet elementSet, int nElement1, int nVertex1, int nElement2, int nVertex2)
        {
            var dx = elementSet.GetVertexXCoordinate(nElement2, nVertex2) - elementSet.GetVertexXCoordinate(nElement1, nVertex1);
            var dy = elementSet.GetVertexYCoordinate(nElement2, nVertex2) - elementSet.GetVertexYCoordinate(nElement1, nVertex1);
            var dz = elementSet.HasZ
                ? elementSet.GetVertexZCoordinate(nElement2, nVertex2) - elementSet.GetVertexZCoordinate(nElement1, nVertex1)
                : 0.0;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static IIdentifiable[] SegmentIds(ISpatialDefinition spatial)
        {
            var ids = new Identity[spatial.ElementCount];

            for (int n = 0; n < spatial.ElementCount; ++n)
                ids[n] = new Identity(string.Format("{0}[{1}]", spatial.Caption, n.ToString()));

            return ids;
        }

        static int[][] GetElementVertexMapping(ISpatialDefinition spatial)
        {
            var mapping = new int[spatial.ElementCount][];

            for (int n = 0; n < spatial.ElementCount; ++n)
                mapping[n] = new int[] { n, n + 1 };

            return mapping;
        }

        protected override IList<IEnumerable<int>> ElementUniqueIndexMapping()
        {
            var map = new int[ElementCount][];

            int next = -1;

            for (int n = 0; n < ElementCount; ++n)
                map[n] = new int[] { ++next, next + 1 };

            return map;
        }
    }
}
