
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineDoubleVector2d : ValueSetConverterTimeEngineBase<Vector2d<double>>
    {
        public ValueSetConverterTimeEngineDoubleVector2d()
        {
            _vectorLength = 2;
        }

        public ValueSetConverterTimeEngineDoubleVector2d(string engineVariable, Vector2d<double> missingValue, int elementCount, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        {
            _vectorLength = 2;
        }

        public ValueSetConverterTimeEngineDoubleVector2d(string engineVariable, Vector2d<double> missingValue, int elementCount, int valuesPerElement, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        {
            _vectorLength = 2;
            _elementValueCount = valuesPerElement;
        }

        public ValueSetConverterTimeEngineDoubleVector2d(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IEnumerable<Vector2d<double>> LinearInterpolation(TimeRecord<Vector2d<double>> below, TimeRecord<Vector2d<double>> above, double factor)
        {
            var valuesAbove = above.Values.SelectMany(v => v.Values).ToArray();
            var valuesBelow = below.Values.SelectMany(v => v.Values);

            var doubles = valuesBelow
                .Select((b, n) => b + factor * (valuesAbove[n] - b))
                .ToArray();

            return Enumerable
                .Range(0, doubles.Count() / 2)
                .Select(n => new Vector2d<double>(doubles, 2 * n));
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<Vector2d<double>> record)
        {
            var values = record.Values.SelectMany(v => v.Values);

            iEngine.SetDoubles(EngineVariable, _missingValue.Value1, values.ToArray());       
        }

        public override TimeRecord<Vector2d<double>> GetEngineValues(IEngine iEngine, ITime time)
        {
            var doubles = iEngine.GetDoubles(EngineVariable, _missingValue.Value1);

            var values = Enumerable
                .Range(0, doubles.Length / 2)
                .Select(n => new Vector2d<double>(doubles, 2 * n));

            return new TimeRecord<Vector2d<double>>(time, values.ToArray());
        }

        public override string ToString(Vector2d<double> value)
        {
            return value.ToString();
        }

        public override Vector2d<double> ToValue(string value)
        {
            return new Vector2d<double>(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineDoubleVector2d(EngineVariable, _missingValue, _elementCount, _elementValueCount, _interpolation);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineDoubleVector2d";

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
