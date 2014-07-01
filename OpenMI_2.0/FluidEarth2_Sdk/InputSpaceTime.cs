
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class InputSpaceTime : BaseInputSpaceTime, IHasValueSetConvertor, IPersistence
    {
        IValueSetConverterTime _valueSetConverterTime = null;
        ITimeSpaceValueSet _valuesExplicitOverride = null;

        public InputSpaceTime()
        {}

        public InputSpaceTime(IValueDefinition valueDefinition, ISpatialDefinition spatialDefinition, IBaseLinkableComponent component, IValueSetConverterTime valueSetConverterTime)
            : base(valueDefinition, spatialDefinition, component)
        {
            _valueSetConverterTime = valueSetConverterTime;

            if (_valueSetConverterTime != null)
                _valueSetConverterTime.ExchangeItem = this;
        }

        public InputSpaceTime(IIdentifiable identity, IValueDefinition valueDefinition, ISpatialDefinition spatialDefinition, IBaseLinkableComponent component, IValueSetConverterTime valueSetConverterTime)
            : base(identity, valueDefinition, spatialDefinition, component)
        {
            _valueSetConverterTime = valueSetConverterTime;

            if (_valueSetConverterTime != null)
                _valueSetConverterTime.ExchangeItem = this;
        }

        public InputSpaceTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override void SetValuesTimeImplementation(ITimeSpaceValueSet values)
        {
            _valuesExplicitOverride = values;

            SendItemChangedEvent(string.Format("InputSpaceTime({0}).Values.set, explicit override {1}",
                Caption, values == null ? "deactivated" : "activated"));
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation()
        {
            if (_valuesExplicitOverride != null)
            {
                SendItemChangedEvent("InputSpaceTime({0}).Values.get, using explicit override");
                return _valuesExplicitOverride;
            }

            Contract.Requires(Provider != null, "Provider != null");

            return Provider.GetValues(this) as ITimeSpaceValueSet;
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            return GetValuesTimeImplementation();
        }

        protected override void SetValuesImplementation(IBaseValueSet values)
        {
            if (values is ITimeSpaceValueSet)
            {
                SetValuesTimeImplementation(values as ITimeSpaceValueSet);
                return;
            }

            throw new Exception("InputSpaceTime class temporal, call temporal Values methods");
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

        public const string XName = "InputSpaceTime";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);
            SpatialDefinition = Persistence.SpatialDefinition.Parse(xElement, accessor);
            TimeSet = Persistence.TimeSet.Parse(xElement, accessor);

            Component = null;
            Provider = null;

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

            if (!Utilities.IsValid("Provider", Provider, ValueDefinition.ValueType, out whyNot))
                return false;

            whyNot = string.Empty;
            return true;
        }
    }
}
