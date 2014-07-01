using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineInt32 : ValueSetConverterTimeEngineBase<Int32>
    {
        public ValueSetConverterTimeEngineInt32()
        { }

        public ValueSetConverterTimeEngineInt32(string engineVariable, Int32 missingValue, int elementCount, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        { }

        public ValueSetConverterTimeEngineInt32(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IEnumerable<Int32> LinearInterpolation(TimeRecord<Int32> below, TimeRecord<Int32> above, double factor)
        {
            var a = above.Values.ToArray();

            return below.Values.Select((b, n) => Convert.ToInt32(b + factor * (a[n] - b)));
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<Int32> record)
        {
            iEngine.SetInt32s(EngineVariable, _missingValue, record.Values);       
        }

        public override TimeRecord<Int32> GetEngineValues(IEngine iEngine, ITime time)
        {
            var values = iEngine.GetInt32s(EngineVariable, _missingValue);

            return new TimeRecord<Int32>(time, values);
        }

        public override string ToString(System.Int32 value)
        {
            return value.ToString();
        }

        public override Int32 ToValue(string value)
        {
            return Int32.Parse(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineInt32(EngineVariable, _missingValue, _elementCount, _interpolation);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineInt32";

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
