using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;
using System.Xml.Linq;
using System.Collections.Generic;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class ElementSetProposed : ElementSet, IElementSetProposed, IPersistence
    {
        public ElementSetProposed()
        { }

        public ElementSetProposed(IElementSet iElementSet)
            : base(iElementSet)
        { }

        public ElementSetProposed(OpenMI.Standard.IElementSet iElementSet1)
            : base(iElementSet1)
        { }

        public ElementSetProposed(ISpatialDefinition spatial, ElementType elementType)
            : base(spatial, elementType)
        { }

        public ElementSetProposed(ISpatialDefinition spatial, ElementType elementType, bool hasZ, bool hasM)
            : base(spatial, elementType, hasZ, hasM)
        { }

        public const string XName = "ElementSetProposed";
        public const string XAttribute = "elementTypeExtended";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ISpatialDefinition spatial;
            bool hasZ, hasM;
            ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
            SetSpatial(spatial);
            HasZ = hasZ;
            HasM = hasM;
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.ElementSet.Persist(this, accessor));
        }

        public virtual bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return false;
        }

        public virtual void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
            ElementCount = elementSetEdits.ElementCount;
        }

        public IList<IArgument> Arguments
        {
            get { return GetArguments(); }
        }

        public virtual void Initialise()
        { }

        public virtual List<IArgument> GetArguments()
        {
            return new List<IArgument>();
        }
    }
}
