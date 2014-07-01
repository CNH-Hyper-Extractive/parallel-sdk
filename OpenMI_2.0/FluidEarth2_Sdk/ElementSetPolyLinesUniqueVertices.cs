using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Each element is a polyline with a single value along its length
    /// </summary>
    /// 
    public class ElementSetPolyLinesUniqueVertices : ElementSetVerticesUniqueIndexed
    {
        public ElementSetPolyLinesUniqueVertices()
            : base(ElementType.PolyLine)
        { }

        public ElementSetPolyLinesUniqueVertices(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            int[][] elementVertices,
            IEnumerable<double> x,
            IEnumerable<double> y,
            IEnumerable<double> z = null,
            IEnumerable<double> m = null)
            : base(spatial, ids, elementVertices, ElementType.PolyLine, x, y, z, m)
        { }

        public new const string XName = "ElementSetPolyLinesUniqueVertices";

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
            return new ElementSetPolyLinesUniqueVertices(this, Ids, IndexMap, X, Y, Z, M);
        }
    }
}
