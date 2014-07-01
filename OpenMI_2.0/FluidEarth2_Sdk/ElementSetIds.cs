
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ElementSetIds : ElementSetProposed
    {
        public IIdentifiable[] Ids { get; private set; }

        public void SetIds(IEnumerable<IIdentifiable> ids)
        {
            Ids = ids.Select(i => new Identity(i as IDescribable)).ToArray();
            ElementCount = Ids.Length;
        }

        public ElementSetIds()
        {
            Ids = new IIdentifiable[] {};
        }

        public ElementSetIds(ISpatialDefinition spatial, IEnumerable<IIdentifiable> ids)
            : base(spatial, ElementType.IdBased)
        {
            Ids = ids.Select(i => new Identity(i as IDescribable)).ToArray();
            ElementCount = Ids.Length;
        }

        protected ElementSetIds(ISpatialDefinition spatial, IEnumerable<IIdentifiable> ids, ElementType elementType, bool hasZ, bool hasM)
            : base(spatial, elementType, hasZ, hasM)
        {
            Ids = ids.Select(i => new Identity(i as IDescribable)).ToArray();
            ElementCount = Ids.Length;
        }

        #region IElementSet Members

        public override IIdentifiable GetElementId(int index)
        {
            return Ids[index];
        }

        public override int GetElementIndex(IIdentifiable elementId)
        {
            return Ids
                .Select((v, n) => new {Id = v.Id, Index = n})
                .First(t => t.Id == elementId.Id).Index;
        }

        #endregion

        public new const string XName = "ElementSetIds";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            Ids = xElement
                .Elements(Persistence.Identity.XName)
                .Select(i => Persistence.Identity.Parse(i, accessor))
                .ToArray();

            ElementCount = Ids.Length;
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                base.Persist(accessor),
                Ids.Select(i => Persistence.Identity.Persist(i, accessor)));
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetIds(this, Ids);
        }

        public override bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits.ElementType == ElementType.IdBased;
        }

        public override void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            Ids = new IIdentifiable[elementSetEdits.ElementCount];

            for (int n = 0; n < elementSetEdits.ElementCount; ++n)
                Ids[n] = new Identity(elementSetEdits.GetElementId(n));

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
            ElementCount = elementSetEdits.ElementCount;
        }
    }
}
