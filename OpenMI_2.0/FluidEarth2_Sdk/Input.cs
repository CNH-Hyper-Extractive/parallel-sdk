
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class Input : BaseInput, IHasValueSetConvertor, IPersistence
    {
        IValueSetConverter _valueSetConverter = null;
        IBaseValueSet _valuesExplicitOverride = null;

        // Required for FluidEarth1 migration to 2
        Dictionary<string, string> _userVariables
            = new Dictionary<string, string>();

        public Input()
        {}

        public Input(IValueDefinition valueDefinition, IBaseLinkableComponent component, IValueSetConverter valueSetConverter)
            : base(valueDefinition, component)
        {
            _valueSetConverter = valueSetConverter;

            if (_valueSetConverter != null)
                _valueSetConverter.ExchangeItem = this;
        }

        public Input(IIdentifiable identity, IValueDefinition valueDefinition, IBaseLinkableComponent component, IValueSetConverter valueSetConverter)
            : base(identity, valueDefinition, component)
        {
            _valueSetConverter = valueSetConverter;

            if (_valueSetConverter != null)           
                _valueSetConverter.ExchangeItem = this;
        }

        public Input(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override void SetValuesImplementation(IBaseValueSet values)
        {
            _valuesExplicitOverride = values;

            SendItemChangedEvent(string.Format("Input({0}).Values.set, explicit override {1}", 
                Caption, values == null ? "deactivated" : "activated"));
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            if (_valuesExplicitOverride != null)
            {
                SendItemChangedEvent("Input({0}).Values.get, using explicit override");
                return _valuesExplicitOverride;
            }

            Contract.Requires(Provider != null, "Provider != null");

            return Provider.GetValues(this);
        }

        public void AddUserVariables(Dictionary<string, string> userVariables)
        {
            foreach (KeyValuePair<string, string> kv in userVariables)
                if (_userVariables.ContainsKey(kv.Key))
                    _userVariables[kv.Key] = kv.Value;
                else
                    _userVariables.Add(kv.Key, kv.Value);
        }

        public IValueSetConverter ValueSetConverter
        {
            get { return _valueSetConverter; }
            set
            {
                _valueSetConverter = value;

                if (_valueSetConverter != null)
                    _valueSetConverter.ExchangeItem = this;
            }
        }

        public const string XName = "Input";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);

            Component = null;
            Provider = null;

            _userVariables = xElement
                .Elements("User")
                .ToDictionary(k => Utilities.Xml.GetAttribute(k, "key"), v => Utilities.Xml.GetAttribute(v, "value"));

            _valueSetConverter = Persistence.Parse<IValueSetConverter>("ValueSetConverter", xElement, accessor);
            _valueSetConverter.ExchangeItem = this;
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                _userVariables.Select(v => new XElement("User", new XAttribute("key", v.Key), new XAttribute("value", v.Value))),
                Persistence.Persist<IValueSetConverter>("ValueSetConverter", _valueSetConverter, accessor));
        }

        public override void AddItemChangedEvent(EventHandler<ExchangeItemChangeEventArgs> onItemChangedEvent)
        {
            base.AddItemChangedEvent(onItemChangedEvent);

            if (_valueSetConverter != null)
                _valueSetConverter.ItemChanged += onItemChangedEvent;
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


