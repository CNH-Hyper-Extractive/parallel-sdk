
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class OutputSpaceTime : BaseOutputSpaceTime, IHasValueSetConvertor, IPersistence
    {
        IValueSetConverterTime _valueSetConverterTime = null;
        int _updatelimitBeforeExtrapolating = 1000;

        public OutputSpaceTime()
        {}

        public OutputSpaceTime(IValueDefinition valueDefinition, ISpatialDefinition spatialDefinition, IBaseLinkableComponent component, IValueSetConverterTime valueSetConverterTime)
            : base(valueDefinition, spatialDefinition, component)
        {
            _valueSetConverterTime = valueSetConverterTime;

            if (_valueSetConverterTime != null)
                _valueSetConverterTime.ExchangeItem = this;
        }

        public OutputSpaceTime(IIdentifiable identity, IValueDefinition valueDefinition, ISpatialDefinition spatialDefinition, IBaseLinkableComponent component, IValueSetConverterTime valueSetConverterTime)
            : base(identity, valueDefinition, spatialDefinition, component)
        {
            _valueSetConverterTime = valueSetConverterTime;

            if (_valueSetConverterTime != null)
                _valueSetConverterTime.ExchangeItem = this;
        }

        public OutputSpaceTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation(IBaseExchangeItem querySpecifier)
        {
            if (!(querySpecifier is ITimeSpaceExchangeItem))
                return _valueSetConverterTime.GetValueSetLatest() as ITimeSpaceValueSet;

            TimeSet = ((ITimeSpaceExchangeItem)querySpecifier).TimeSet;
            TimeSet queryTime = new TimeSet(((ITimeSpaceExchangeItem)querySpecifier).TimeSet);

            if (Component == null) // Orphaned Exchange item  
                return _valueSetConverterTime.GetValueSetAt(TimeSet) as ITimeSpaceValueSet;

            int updateCount = 0;

            while (!_valueSetConverterTime.CanGetValueSetWithoutExtrapolationAt(queryTime))
            {
                if (Component.Status == LinkableComponentStatus.Updating)
                {
                    // Bidirectional link and component is busy

                    string warning = string.Format("WARNING: Component \"{0}\" busy extrapolated for required values", Component.Caption);

                    Trace.TraceWarning(warning);
                    SendItemChangedEvent(warning);

                    return _valueSetConverterTime.GetValueSetAt(queryTime) as ITimeSpaceValueSet;
                }
                else if (updateCount > _updatelimitBeforeExtrapolating)
                {
                    string error = string.Format(
                        "ERROR: Component \"{0}\" reached update limit of {1}, aborted updates and extrapolated for required values",
                        Component.Caption, _updatelimitBeforeExtrapolating);

                    Trace.TraceError(error);
                    SendItemChangedEvent(error);

                    return _valueSetConverterTime.GetValueSetAt(queryTime) as ITimeSpaceValueSet;
                }
                else
                {
                    Component.Update(this);
                    ++updateCount;
                }
            }

            return _valueSetConverterTime.GetValueSetAt(queryTime) as ITimeSpaceValueSet;
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

        public IValueSetConverter ValueSetConverter
        {
            get { return _valueSetConverterTime; }
            set
            {
                _valueSetConverterTime = value as IValueSetConverterTime;

                if (_valueSetConverterTime != null)
                    _valueSetConverterTime.ExchangeItem = this;
            }
        }

        public const string XName = "OutputSpaceTime";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);
            SpatialDefinition = Persistence.SpatialDefinition.Parse(xElement, accessor);
            TimeSet = Persistence.TimeSet.Parse(xElement, accessor);

            Component = null;
            Consumers = new List<IBaseInput>();
            AdaptedOutputs = new List<IBaseAdaptedOutput>();

            _valueSetConverterTime = Persistence.Parse<IValueSetConverterTime>("ValueSetConverterTime", xElement, accessor);
            _valueSetConverterTime.ExchangeItem = this;
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                Persistence.SpatialDefinition.Persist(SpatialDefinition, accessor),
                Persistence.TimeSet.Persist(TimeSet, accessor),
                Persistence.Persist<IValueSetConverterTime>("ValueSetConverterTime", _valueSetConverterTime, accessor));
        }

        public override void AddItemChangedEvent(EventHandler<ExchangeItemChangeEventArgs> onItemChangedEvent)
        {
            base.AddItemChangedEvent(onItemChangedEvent);

            if (_valueSetConverterTime != null)
                _valueSetConverterTime.ItemChanged += onItemChangedEvent;
        }

        public override bool IsValid(out string whyNot)
        {
            if (!base.IsValid(out whyNot))
                return false;

            if (ValueSetConverter == null)
            {
                whyNot = "ValueSetConverter == null";
                return false;
            }

            foreach (var consumer in Consumers)
                if (!Utilities.IsValid("Consumer", consumer, ValueDefinition.ValueType, out whyNot))
                    return false;

            whyNot = string.Empty;
            return true;
        }
    }
}

