
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeEngineDoubleVector3d : ValueSetConverterTimeEngineBase<Vector3d<double>>
    {
        public ValueSetConverterTimeEngineDoubleVector3d()
        {
            _vectorLength = 3;
        }

        public ValueSetConverterTimeEngineDoubleVector3d(string engineVariable, Vector3d<double> missingValue, int elementCount, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        {
            _vectorLength = 3;
        }

        public ValueSetConverterTimeEngineDoubleVector3d(string engineVariable, Vector3d<double> missingValue, int elementCount, int valuesPerElement, InterpolationTemporal interpolation)
            : base(engineVariable, missingValue, elementCount, interpolation)
        {
            _vectorLength = 3;
            _elementValueCount = valuesPerElement;
        }

        public ValueSetConverterTimeEngineDoubleVector3d(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IEnumerable<Vector3d<double>> LinearInterpolation(TimeRecord<Vector3d<double>> below, TimeRecord<Vector3d<double>> above, double factor)
        {
            var valuesAbove = above.Values.SelectMany(v => v.Values).ToArray();
            var valuesBelow = below.Values.SelectMany(v => v.Values);

            var doubles = valuesBelow
                .Select((b, n) => b + factor * (valuesAbove[n] - b))
                .ToArray();

            return Enumerable
                .Range(0, doubles.Count() / 3)
                .Select(n => new Vector3d<double>(doubles, 3 * n));
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<Vector3d<double>> record)
        {
            var values = record.Values.SelectMany(v => v.Values);

            iEngine.SetDoubles(EngineVariable, _missingValue.Value1, values.ToArray());       
        }

        public override TimeRecord<Vector3d<double>> GetEngineValues(IEngine iEngine, ITime time)
        {
            var doubles = iEngine.GetDoubles(EngineVariable, _missingValue.Value1);

            var values = Enumerable
                .Range(0, doubles.Length / 3)
                .Select(n => new Vector3d<double>(doubles, 3 * n));

            return new TimeRecord<Vector3d<double>>(time, values.ToArray());
        }

        public override string ToString(Vector3d<double> value)
        {
            return value.ToString();
        }

        public override Vector3d<double> ToValue(string value)
        {
            return new Vector3d<double>(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineDoubleVector3d(EngineVariable, _missingValue, _elementCount, _elementValueCount, _interpolation);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineDoubleVector3d";

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

