using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// All face coordinates are stored separately, so could be inefficient for grids and meshes
    /// </summary>
    public class ElementSetPolygons : ElementSetVerticesNonUnique
    {
        public ElementSetPolygons()
            : base(ElementType.Polygon)
        { }

        public ElementSetPolygons(
            ISpatialDefinition spatial,
            IEnumerable<IIdentifiable> ids,
            IEnumerable<IEnumerable<double>> x,
            IEnumerable<IEnumerable<double>> y,
            IEnumerable<IEnumerable<double>> z = null,
            IEnumerable<IEnumerable<double>> m = null)
            : base(spatial, ids, ElementType.Polygon, x, y, z, m)
        { }

        public override int GetFaceCount(int elementIndex)
        {
            return 1;
        }

        public new const string XName = "ElementSetPolygons";

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
            return new ElementSetPolygons(this, Ids, X, Y, Z, M);
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits.ElementType == ElementType.Polygon;
        }
    }
}
