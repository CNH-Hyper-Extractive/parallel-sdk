using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public interface IOrphanedExchangeItem : IPersistence
    { }

    public class OrphanedInputSpaceTime : BaseInputSpaceTime, IOrphanedExchangeItem
    {
        public OrphanedInputSpaceTime()
        {
            var describes = new Describes("Temporal Target", "= Temporal Target");
            SetIdentity(new Identity("FluidEarth2.Sdk.OrphanedInputSpaceTime", describes));

            Component = null;

            ValueDefinition = new ValueDefinition();
            SpatialDefinition = new SpatialDefinition();

            TimeSet = new TimeSet();
        }

        public OrphanedInputSpaceTime(ITimeSpaceOutput output)
        {
            var describes = new Describes("Temporal Target", "= Temporal Target");
            SetIdentity(new Identity("FluidEarth2.Sdk.OrphanedInputSpaceTime", describes));

            Component = null;

            ValueDefinition = output.ValueDefinition;
            SpatialDefinition = output.SpatialDefinition;

            TimeSet = new TimeSet();
        }

        public OrphanedInputSpaceTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public const string XName = "OrphanedInputSpaceTime";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);
            SpatialDefinition = Persistence.SpatialDefinition.Parse(xElement, accessor);
            TimeSet = Persistence.TimeSet.Parse(xElement, accessor);

            Component = null;
            Provider = null;
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                Persistence.SpatialDefinition.Persist(SpatialDefinition, accessor),
                Persistence.TimeSet.Persist(TimeSet, accessor));
        }

        public override void AddItemChangedEvent(EventHandler<ExchangeItemChangeEventArgs> onItemChangedEvent)
        {
            base.AddItemChangedEvent(onItemChangedEvent);
        }

        public override bool IsValid(out string whyNot)
        {
            if (!base.IsValid(out whyNot))
                return false;

            if (!Utilities.IsValid("Provider", Provider, ValueDefinition.ValueType, out whyNot))
                return false;

            whyNot = string.Empty;
            return true;
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation()
        {
            Contract.Requires(false,
                "Orphaned Input, does not implement get values calls");
            return null;
        }

        protected override void SetValuesTimeImplementation(ITimeSpaceValueSet values)
        {
            Contract.Requires(false, 
                "Orphaned Input, does not implement get values calls");
        }
    }
}






