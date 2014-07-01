
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class Output : BaseOutput, IHasValueSetConvertor, IPersistence
    {
        IValueSetConverter _valueSetConverter = null;

        // Required for FluidEarth1 migration to 2
        Dictionary<string, string> _userVariables
            = new Dictionary<string, string>();

        public Output()
        {}

        public Output(IValueDefinition valueDefinition, IBaseLinkableComponent component, IValueSetConverter valueSetConverter)
            : base(valueDefinition, component)
        {
            _valueSetConverter = valueSetConverter;

            if (_valueSetConverter != null)
                _valueSetConverter.ExchangeItem = this;
        }

        public Output(IIdentifiable identity, IValueDefinition valueDefinition, IBaseLinkableComponent component, IValueSetConverter valueSetConverter)
            : base(identity, valueDefinition, component)
        {
            _valueSetConverter = valueSetConverter;

            if (_valueSetConverter != null)
                _valueSetConverter.ExchangeItem = this;
        }

        public Output(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override IBaseValueSet GetValuesImplementation(IBaseExchangeItem querySpecifier)
        {
            // Here as non spatial and not temporal

            // Special case, handy for checking current values without changing engine state
            if (querySpecifier == null)
                ValueSetConverter.GetValueSetLatest();

            if (querySpecifier is IBaseInput && !Consumers.Any(c => c.Id == querySpecifier.Id))
                throw new Exception("GetValues request from an unregistered Input Item: " + querySpecifier.Id);
            else if (querySpecifier is IBaseOutput && !AdaptedOutputs.Any(c => c.Id == querySpecifier.Id))
                throw new Exception("GetValues request from an unregistered Adapted Output Item: " + querySpecifier.Id);

            if (querySpecifier is IBaseOutput)
                throw new NotImplementedException();

            // Not a time based request just update and return latest

            Component.Update(this);

            return ValueSetConverter.GetValueSetLatest();
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            // Here as non spatial and not temporal
            return ValueSetConverter.GetCache();
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

        public const string XName = "Output";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);

            Component = null;
            Consumers = new List<IBaseInput>();
            AdaptedOutputs = new List<IBaseAdaptedOutput>();  

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

            foreach (var consumer in Consumers)
                if (!Utilities.IsValid("Consumer", consumer, ValueDefinition.ValueType, out whyNot))
                    return false;

            whyNot = string.Empty;
            return true;
        }
    }
}



