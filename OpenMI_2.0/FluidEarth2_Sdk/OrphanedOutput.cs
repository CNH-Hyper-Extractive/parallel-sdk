using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class OrphanedOutput<TType> : BaseOutput, IOrphanedExchangeItem
    {
        SingleValue _value; 

        public OrphanedOutput()
        {
            _value = new SingleValue();

            var describes = new Describes("Source", "= Source");
            var describesVd = new Describes(typeof(TType).Name, "= " + typeof(TType).Name);

            SetIdentity(new Identity("FluidEarth2.Sdk.OrphanedOutput", describes));
            ValueDefinition = new ValueDefinition(describesVd, typeof(TType), default(TType));
            Component = null;
        }

        public OrphanedOutput(TType value)
            : this()
        {
            _value.Value = value;
        }

        public OrphanedOutput(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        protected override IBaseValueSet GetValuesImplementation(IBaseExchangeItem querySpecifier)
        {
            if (querySpecifier is IBaseInput && !Consumers.Any(c => c.Id == querySpecifier.Id))
                throw new Exception("GetValues request from an unregistered Input Item: " + querySpecifier.Id);
            else if (querySpecifier is IBaseOutput && !AdaptedOutputs.Any(c => c.Id == querySpecifier.Id))
                throw new Exception("GetValues request from an unregistered Adapted Output Item: " + querySpecifier.Id);

            if (querySpecifier is IBaseOutput)
                throw new NotImplementedException();

            return _value;
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            return _value;
        }

        public const string XName = "OrphanedOutput";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);

            // Default constructor only for value unless TType supports IPersistence or IConvertible

            TType value = default(TType);

            if (typeof(IPersistence).IsAssignableFrom(typeof(TType)))
            {
                var xValue = Persistence.ThisOrSingleChild("Value", xElement);
                ((IPersistence)value).Initialise(xValue, accessor);
            }
            else if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                value = (TType)Convert.ChangeType(Utilities.Xml.GetAttribute(xElement, "value"), typeof(TType));

            _value = new SingleValue(value);

            Component = null;
            Consumers = new List<IBaseInput>();
            AdaptedOutputs = new List<IBaseAdaptedOutput>();  
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor));

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

        public class SingleValue
            : IBaseValueSet
        {
            TType _value = default(TType);

            public SingleValue()
            { }

            public SingleValue(TType value)
            {
                _value = value;
            }

            public TType Value
            {
                get { return _value; }
                set { _value = value; }
            }

            public int GetIndexCount(int[] indices)
            {
                if (indices == null || indices.Length == 0)
                    return 1;

                throw new ArgumentException();
            }

            public object GetValue(int[] indices)
            {
                if (indices != null && indices.Length > 0)
                    throw new ArgumentException();

                return _value;
            }

            public int NumberOfIndices
            {
                get { return 1; }
            }

            public void SetValue(int[] indices, object value)
            {
                if (indices != null && indices.Length > 0)
                    throw new ArgumentException();

                _value = (TType)value;
            }

            public Type ValueType
            {
                get { return typeof(TType); }
            }
        }
    }
}




