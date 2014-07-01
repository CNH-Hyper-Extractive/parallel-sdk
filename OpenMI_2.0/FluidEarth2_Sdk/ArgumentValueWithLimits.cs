
using System;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class ArgumentLimitedValue<TType> : Argument<ArgumentValueLimitedValue<TType>>
        where TType : IConvertible, IComparable<TType>
    {
        public ArgumentLimitedValue()
        { }

        public ArgumentLimitedValue(IIdentifiable identity, TType value, LimitedValueLimits<TType> limits)
            : base(identity, new ArgumentValueLimitedValue<TType>(value, limits), false, false)
        { }

        public ArgumentLimitedValue(IIdentifiable identity, TType value, LimitedValueLimits<TType> limits, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueLimitedValue<TType>(value, limits), isReadOnly, isOptional)
        {}

        public ArgumentLimitedValue(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public new const string XName = "ArgumentLimitedValue";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            var xValue = Persistence.ThisOrSingleChild(ArgumentValueLimitedValue<TType>.XName, xElement, true);

            if (xValue != null)
                Value = new ArgumentValueLimitedValue<TType>(xValue, accessor);

            base.Initialise(xElement, accessor);
        }   

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName, base.Persist(accessor));

            if (Value != null)
                xml.Add(((ArgumentValueLimitedValue<TType>)Value).Persist(accessor));

            return xml;
        }
    }
}
