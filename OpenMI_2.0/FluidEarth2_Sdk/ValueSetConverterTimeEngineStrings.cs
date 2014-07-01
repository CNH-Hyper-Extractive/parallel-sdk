using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineString : ValueSetConverterTimeEngineBase<string>
    {
        public ValueSetConverterTimeEngineString()
        { }

        public ValueSetConverterTimeEngineString(string engineVariable, int elementCount, string missingValue)
            : base(engineVariable, missingValue, elementCount, InterpolationTemporal.NoneUseLast)
        { }

        public ValueSetConverterTimeEngineString(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<string> record)
        {
            iEngine.SetStrings(EngineVariable, _missingValue, record.Values);
        }

        public override TimeRecord<string> GetEngineValues(IEngine iEngine, ITime time)
        {
            var values = iEngine.GetStrings(EngineVariable, _missingValue);

            return new TimeRecord<string>(time, values);
        }

        public override string ToString(string value)
        {
            return value;
        }

        public override string ToValue(string value)
        {
            return value;
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineString(EngineVariable, _elementCount, _missingValue);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineString";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                base.Persist(accessor));
        }
    }
}
