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
    public abstract class AdaptedOutputTimeBase<TTypeAdaptee, TTypeAdapted> 
        : BaseAdaptedOutputTime, IBaseAdaptedOutput, IPersistence
    {
        public abstract IEnumerable<TimeRecord<TTypeAdapted>> AdaptRecords(List<TimeRecord<TTypeAdaptee>> toAdapt, IBaseExchangeItem querySpecifier);

        /// <summary>
        /// Only available after PrepareIt() called i.e. for use in derived Adapt(...)
        /// </summary>
        protected IValueSetConverterTimeRecord<TTypeAdaptee> _converterAdaptee;
        protected Dictionary<IBaseExchangeItem, IValueSetConverterTimeRecord<TTypeAdapted>> _converterQuerySpecifiers
            = new Dictionary<IBaseExchangeItem, IValueSetConverterTimeRecord<TTypeAdapted>>();

        /// <summary>
        /// For use with persistence only, which ensures correct initialisation
        /// </summary>
        public AdaptedOutputTimeBase()
        { }

        /// <summary>
        /// Abstract base class for adapted output
        /// Derived classes must implement abstract ITimeSpaceValueSet Adapt(IBaseExchangeItem querySpecifier);
        /// </summary>
        /// <param name="identity">Identity of adapted output, must be non null</param>
        /// <param name="adaptee">Output item to adapt, must be non null</param>
        /// <param name="target">Input item to provide for, can be null (null in UI building)</param>
        public AdaptedOutputTimeBase(IIdentifiable identity, IBaseOutput adaptee, IBaseInput target, IAdaptedOutputFactory factory, object missingDataValue)
            : base(identity, adaptee, target, factory)
        {
            ValueDefinition = new ValueDefinition(
                new Describes(typeof(TTypeAdapted).ToString()),
                typeof(TTypeAdapted), missingDataValue);
        }

        protected override void PrepareIt()
        {
            string whyNot;

            if (!IsValid(out whyNot))
                throw new Exception("IsValid failure: " + whyNot);

            base.PrepareIt();

            var converter = Utilities.GetValueSetConverter(Adaptee);

            _converterAdaptee = converter as IValueSetConverterTimeRecord<TTypeAdaptee>;

            Contract.Requires(_converterAdaptee != null,
                "Adaptee IValueSetConverter is IValueSetConverterTimeRecord<TTypeAdaptee>");

            foreach (var consumer in Consumers)
            {
                converter = Utilities.GetValueSetConverter(consumer);

                var converterConsumer = converter as IValueSetConverterTimeRecord<TTypeAdapted>;

                Contract.Requires(converterConsumer != null,
                    "Consumer IValueSetConverter is IValueSetConverterTimeRecord<TTypeAdapted>");

                _converterQuerySpecifiers.Add(consumer, converterConsumer);
            }

            foreach (var adaptedOutput in AdaptedOutputs)
            {
                converter = Utilities.GetValueSetConverter(adaptedOutput);

                var converterConsumer = converter as IValueSetConverterTimeRecord<TTypeAdapted>;

                Contract.Requires(converterConsumer != null,
                    "AdaptedOutput IValueSetConverter is IValueSetConverterTimeRecord<TTypeAdapted>");

                _converterQuerySpecifiers.Add(adaptedOutput, converterConsumer);
            }
        }

        protected override ITimeSpaceValueSet Adapt(IBaseExchangeItem querySpecifier)
        {
            // Adaptee could be null for adapters that impose data rather than adapt.
            Contract.Requires(Adaptee == null || Adaptee is ITimeSpaceOutput,
                "Adaptee == null || Adaptee is ITimeSpaceOutput");
            Contract.Requires(Adaptee == null || _converterAdaptee != null,
                "Adaptee == null || _converterProvider != null");

            Contract.Requires(querySpecifier is ITimeSpaceExchangeItem,
                "querySpecifier is ITimeSpaceExchangeItem");

            Contract.Requires(_converterQuerySpecifiers.ContainsKey(querySpecifier),
                "_convertorConsumers.ContainsKey(querySpecifier)");
            Contract.Requires(_converterQuerySpecifiers[querySpecifier] is IValueSetConverterTime,
                "_convertorConsumers[querySpecifier] is IValueSetConverterTime");

            TimeSet = ((ITimeSpaceExchangeItem)querySpecifier).TimeSet;

            if (Adaptee != null)
            {
                var valueSet = ((ITimeSpaceOutput)Adaptee).GetValues(this);

                Contract.Requires(valueSet is ITimeSpaceValueSet,
                    "valueSet is ITimeSpaceValueSet");

                _converterAdaptee.SetCache(TimeSet, (ITimeSpaceValueSet)valueSet);
            }

            var eventArgMessages = new List<string>();

            var recordsAdaptee = new List<TimeRecord<TTypeAdaptee>>();

            if (_converterAdaptee != null)
                recordsAdaptee.AddRange(TimeSet
                    .Times
                    .Select(t => _converterAdaptee.GetRecordAt(t, eventArgMessages)));

            var consumerRecords = AdaptRecords(recordsAdaptee, querySpecifier);

            _converterQuerySpecifiers[querySpecifier].CacheRecords(consumerRecords);

            return ((IValueSetConverterTime)_converterQuerySpecifiers[querySpecifier]).GetValueSetAt(TimeSet);
        }

        public const string XName = "AdaptedOutputTime";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            Identity = Persistence.Identity.Parse(xElement, accessor);
            ValueDefinition = Persistence.ValueDefinition.Parse(xElement, accessor);
            SpatialDefinition = Persistence.SpatialDefinition.Parse(xElement, accessor);
            TimeSet = Persistence.TimeSet.Parse(xElement, accessor);
            ArgumentsAddRange(Persistence.Arguments.Parse(xElement, accessor));

            var factory = new ExternalType(xElement, accessor);
            Type type;
            _factory = factory.CreateInstance(out type) 
                as IAdaptedOutputFactory;

            Component = null;
            Consumers = new List<IBaseInput>();
            AdaptedOutputs = new List<IBaseAdaptedOutput>();
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                Persistence.SpatialDefinition.Persist(SpatialDefinition, accessor),
                Persistence.TimeSet.Persist(TimeSet, accessor),
                Persistence.Arguments.Persist(Arguments, accessor),
                new ExternalType(Factory).Persist(accessor));
        }

        public static bool CanAdapt(IBaseOutput adaptee, out string whyNot)
        {
            return Utilities.IsValid("Adaptee", 
                adaptee, typeof(TTypeAdaptee), out whyNot);
        }

        public override bool IsValid(out string whyNot)
        {
            if (!CanAdapt(Adaptee, out whyNot))
                return false;

            foreach (var consumer in Consumers)
                if (!Utilities.IsValid("Consumer", consumer, typeof(TTypeAdapted), out whyNot))
                    return false;

            return base.IsValid(out whyNot);
        }
        
        public override bool CanConnect(IBaseExchangeItem proposed, out string whyNot)
        {
            if (!CanAdapt(Adaptee, out whyNot))
                return false;

            if (!Utilities.IsValid("Proposed", proposed, typeof(TTypeAdapted), out whyNot))
                return false;

            return base.CanConnect(proposed, out whyNot);
        }
    }
}
