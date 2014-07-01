using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineBoolean : ValueSetConverterTimeEngineBase<bool>
    {
        public ValueSetConverterTimeEngineBoolean()
        { }

        public ValueSetConverterTimeEngineBoolean(string engineVariable, bool missingValue, int elementCount)
            : base(engineVariable, missingValue, elementCount, InterpolationTemporal.NoneUseLast)
        { }

        public ValueSetConverterTimeEngineBoolean(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<bool> record)
        {
            iEngine.SetBooleans(EngineVariable, _missingValue, record.Values);
        }

        public override TimeRecord<bool> GetEngineValues(IEngine iEngine, ITime time)
        {
            var values = iEngine.GetBooleans(EngineVariable, _missingValue);

            return new TimeRecord<bool>(time, values);
        }

        public override string ToString(bool value)
        {
            return value.ToString();
        }

        public override bool ToValue(string value)
        {
            return bool.Parse(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineBoolean(EngineVariable, _missingValue, _elementCount);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineBoolean";

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
