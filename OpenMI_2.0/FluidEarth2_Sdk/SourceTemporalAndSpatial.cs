using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class SourceTemporalAndSpatial<TType> : BaseOutputSpaceTime, IOrphanedExchangeItem
    //    where TType : IPersistence || IConvertible
    {
        TType[] _values;

        public SourceTemporalAndSpatial()
        { }

        public SourceTemporalAndSpatial(TType value, int elementCount, TType missingValue)
        {
            var values = new TType[elementCount];

            values.Initialize();

            for (int n = 0; n < elementCount; ++n)
                values[n] = value;

            Initialise(values, missingValue);
        }

        public SourceTemporalAndSpatial(TType[] values, TType missingValue)
        {
            Initialise(values, missingValue);
        }

        public SourceTemporalAndSpatial(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        void Initialise(TType[] values, TType missingValue)
        {
            Contract.Requires(values != null, "values != null");

            _values = values;

            var describes = new Describes("Source: Temporal & Spatial", "= Source: Temporal & Spatial");
            var describesVd = new Describes(typeof(TType).Name, "= " + typeof(TType).Name);
            var describesSd = new Describes("Spatial Definition", "= Spatial Definition");

            SetIdentity(new Identity("FluidEarth2.Sdk.SourceTemporalAndSpatial", describes));
            ValueDefinition = new ValueDefinition(describesVd, typeof(TType), missingValue);
            SpatialDefinition = new SpatialDefinition(describesSd, values.Length);
            Component = null;
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation(IBaseExchangeItem querySpecifier)
        {
            var item = querySpecifier as ITimeSpaceExchangeItem;

            Contract.Requires(item != null, "querySpecifier as ITimeSpaceExchangeItem");
            Contract.Requires(item.TimeSet != null, "item.TimeSet != null");
            Contract.Requires(item.TimeSet.Times != null, "item.TimeSet.Times != null");

            var records = new List<TimeRecord<TType>>();

            foreach (var time in item.TimeSet.Times)
                records.Add(new TimeRecord<TType>(time, _values));

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

            var xValues = Persistence.ThisOrSingleChild("Values", xElement);

            if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                _values = xValues
                    .Value
                    .Split(',')
                    .Select(s => (TType)Convert.ChangeType(s, typeof(TType)))
                    .ToArray();
            else if (typeof(IVector).IsAssignableFrom(typeof(TType)))
                _values = xValues
                    .Value
                    .Split(',')
                    .Select(s => (TType)((IVector)ValueDefinition.MissingDataValue).New(s))
                    .ToArray();
            else if (typeof(IPersistence).IsAssignableFrom(typeof(TType)))
            {
                var values = new List<TType>();

                foreach (var xValue in xValues.Elements("Value"))
                {
                    var value = default(TType);

                    ((IPersistence)value).Initialise(xValue, accessor);

                    values.Add(value);
                }

                _values = values.ToArray();
            }
            else
                Contract.Requires(false, "TType must implement either IPersistence, IConvertible or IVector");

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

            if (typeof(IConvertible).IsAssignableFrom(typeof(TType)) 
                || typeof(IVector).IsAssignableFrom(typeof(TType)))
            {
                var csv = _values
                    .Aggregate(new StringBuilder(), (sb, v) => sb.Append(string.Format("{0},", v.ToString())))
                    .ToString()
                    .TrimEnd(',');

                xml.Add(new XElement("Values", csv));
            }
            else if (typeof(IPersistence).IsAssignableFrom(typeof(TType)))
            {
                var xValues = _values
                    .Select(v => Persistence.Persist<IPersistence>("Value", v as IPersistence, accessor));
                     
                xml.Add(new XElement("Values", xValues));
            }
            else
                Contract.Requires(false, "TType must implement either IPersistence od IConvertible");

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
