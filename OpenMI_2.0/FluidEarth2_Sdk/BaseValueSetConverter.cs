
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public abstract class ValueSetConverterBase<TType> 
        : IValueSetConverterTyped<TType>
    {
        public abstract void EmptyCaches(ITime upto);
        public abstract IBaseValueSet GetCache();
        public abstract IBaseValueSet GetValueSetLatest();
        public abstract object Clone();

        public abstract void SetCache(IBaseValueSet values);
        public abstract void SetCache(ITime at, ITimeSpaceValueSet values);
        public abstract void SetCache(ITimeSet at, ITimeSpaceValueSet values);

        public abstract string ToString(TType value);
        public abstract TType ToValue(string value);

        public abstract IBaseValueSet ToValueSet(IEnumerable<TType> values);
        public abstract IEnumerable<TType> ToArray(IBaseValueSet values);

        IBaseExchangeItem _iBaseExchangeItem;

        public ValueSetConverterBase()
        { }

        public ValueSetConverterBase(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public IBaseExchangeItem ExchangeItem
        {
            get { return _iBaseExchangeItem; }
            set { _iBaseExchangeItem = value; } 
        }

        public event EventHandler<ExchangeItemChangeEventArgs> ItemChanged;

        public bool HasItemChangedEvents
        {
            get { return ItemChanged != null; }
        }

        public void SendItemChangedEvent(string message)
        {
            SendItemChangedEvent(new BaseExchangeItemChangeEventArgs(_iBaseExchangeItem, message));
        }

        public void SendItemChangedEvent(BaseExchangeItemChangeEventArgs args)
        {
            if (ItemChanged != null)
                ItemChanged(this, args);
        }

        public virtual string DiagnosticSummary()
        {
            return "Non diagnostics implemented for this convertor";
        }

        public const string XName = "ValueSetConverterBase";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _iBaseExchangeItem = null;
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName);
        }
    }
}
