using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineDouble : ValueSetConverterTimeEngineBase<double>
    {
        public ValueSetConverterTimeEngineDouble()
        { }

        public ValueSetConverterTimeEngineDouble(string engineVariable, double missingValue, int elementCount, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        { }

        public ValueSetConverterTimeEngineDouble(string engineVariable, double missingValue, int elementCount, int valuesPerElement, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        {
            _elementValueCount = valuesPerElement;
        }

        public ValueSetConverterTimeEngineDouble(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IEnumerable<double> LinearInterpolation(TimeRecord<double> below, TimeRecord<double> above, double factor)
        {
            var a = above.Values.ToArray();

            return below.Values.Select((b, n) => 
                b + factor * (a[n] - b));
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<double> record)
        {
            iEngine.SetDoubles(EngineVariable, _missingValue, record.Values);       
        }

        public override TimeRecord<double> GetEngineValues(IEngine iEngine, ITime time)
        {
            var values = iEngine.GetDoubles(EngineVariable, _missingValue);

            return new TimeRecord<double>(time, values);
        }

        public override string ToString(double value)
        {
            return value.ToString();
        }

        public override double ToValue(string value)
        {
            return double.Parse(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineDouble(EngineVariable, _missingValue, _elementCount, _elementValueCount, _interpolation);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineDouble";

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
