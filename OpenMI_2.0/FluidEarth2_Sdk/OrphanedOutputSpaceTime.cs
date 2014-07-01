using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class OrphanedOutputSpaceTime<TType> : BaseOutputSpaceTime, IOrphanedExchangeItem
    {
        TType _value;

        public OrphanedOutputSpaceTime()
        {
            _value = default(TType);

            var describes = new Describes("Temporal Source", "= Temporal Source");
            var describesVd = new Describes(typeof(TType).Name, "= " + typeof(TType).Name);
            var describesSd = new Describes("Single Element", "= Single Element");

            SetIdentity(new Identity("FluidEarth2.Sdk.OrphanedOutputSpaceTime", describes));
            ValueDefinition = new ValueDefinition(describesVd, typeof(TType), default(TType));
            SpatialDefinition = new SpatialDefinition(describesSd, 1);
            Component = null;
        }

        public OrphanedOutputSpaceTime(TType value)
            : this()
        {
            _value = value;
        }

        public OrphanedOutputSpaceTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation(IBaseExchangeItem querySpecifier)
        {
            var item = querySpecifier as ITimeSpaceExchangeItem;

            Contract.Requires(item != null, "querySpecifier as ITimeSpaceExchangeItem");
            Contract.Requires(item.TimeSet != null, "item.TimeSet != null");
            Contract.Requires(item.TimeSet.Times != null, "item.TimeSet.Times != null");

            var records = new List<TimeRecord<TType>>();

            foreach (var time in item.TimeSet.Times)
                records.Add(new TimeRecord<TType>(time, new TType[] { _value }));

            return new ValueSetTimeRecord<TType>(records);
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation()
        {
            throw new NotImplementedException();
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            return GetValuesTimeImplementation();
        }

        protected override IBaseValueSet GetValuesImplementation(IBaseExchangeItem querySpecifier)
        {
            return GetValuesTimeImplementation(querySpecifier);
        }

        public const string XName = "OrphanedOutputSpaceTime";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);
            SpatialDefinition = Persistence.SpatialDefinition.Parse(xElement, accessor);
            TimeSet = Persistence.TimeSet.Parse(xElement, accessor);

            // Default constructor only for value unless TType supports IPersistence or IConvertible

            if (typeof(IPersistence).IsAssignableFrom(typeof(TType)))
            {
                _value = default(TType);
                var xValue = Persistence.ThisOrSingleChild("Value", xElement);
                ((IPersistence)_value).Initialise(xValue, accessor);
            }
            else if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                _value = (TType)Convert.ChangeType(Utilities.Xml.GetAttribute(xElement, "value"), typeof(TType));
            else
                _value = default(TType);

            Component = null;
            Consumers = new List<IBaseInput>();
            AdaptedOutputs = new List<IBaseAdaptedOutput>();
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                Persistence.SpatialDefinition.Persist(SpatialDefinition, accessor),
                Persistence.TimeSet.Persist(TimeSet, accessor));

            if (typeof(IPersistence).IsAssignableFrom(typeof(TType)))
                xml.Add(new XElement("Value", ((IPersistence)_value).Persist(accessor)));
            else if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                xml.Add(new XAttribute("value", _value.ToString()));

            return xml;
        }

        public override void AddItemChangedEvent(EventHandler<ExchangeItemChangeEventArgs> onItemChangedEvent)
        {
            base.AddItemChangedEvent(onItemChangedEvent);
        }

        public override bool IsValid(out string whyNot)
        {
            if (!base.IsValid(out whyNot))
                return false;

            foreach (var consumer in Consumers)
                if (!Utilities.IsValid("Consumer", consumer, ValueDefinition.ValueType, out whyNot))
                    return false;

            whyNot = string.Empty;
            return true;
        }
    }
}





