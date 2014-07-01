#if DEPRECATED

using System;
using System.Xml.Linq;
using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeSpaceAdapter<TType> : ValueSetConverterTime<TType>
        where TType : EngineValueTypes.IEngineType
    {
        public delegate List<TimeRecord<TType>> Adapt(ITimeSet timeSet, ITimeSpaceValueSet adaptee);

        Adapt _adapter;

        public ValueSetConverterTimeSpaceAdapter()
        {
        }

        public ValueSetConverterTimeSpaceAdapter(string engineVariable, IValueSetPacking consumerPacking, Interpolation interpolation, Adapt adapter)
            : base(engineVariable, consumerPacking, interpolation)
        {
            _adapter = adapter;
        }

        public ValueSetConverterTimeSpaceAdapter(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public void Refresh(ITimeSet timeSet, ITimeSpaceValueSet adaptee)
        {
            List<TimeRecord<TType>> values = _adapter(timeSet, adaptee);

            _cache.Clear();
            _cache.AddRange(values);
        }

        public override void CacheEngineValues(IEngine iEngine)
        {
            throw new Exception("ValueSetConverterTimeSpaceAdapter<TType> should NOT call engine, adapter only");
        }

        public override void ToEngine(IEngine iEngine, IBaseValueSet iValueSet)
        {
            throw new Exception("ValueSetConverterTimeSpaceAdapter<TType> should NOT call engine, adapter only");
        }

        public override object Clone()
        {
            return new ValueSetConverterTimeSpaceAdapter<TType>(EngineVariable, PackingInformation, _interpolation, _adapter);
        }
    }
}

#endif
