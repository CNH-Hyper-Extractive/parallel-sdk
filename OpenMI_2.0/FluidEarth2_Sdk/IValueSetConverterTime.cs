
using System;
using System.Collections.Generic;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public interface IValueSetConverterTime : IValueSetConverter
    {
        ITimeSet TimeSet { get; }

        bool CanGetValueSetWithoutExtrapolationAt(ITime at);
        bool CanGetValueSetWithoutExtrapolationAt(ITimeSet at);

        ITimeSpaceValueSet GetValueSetAt(ITime at);
        ITimeSpaceValueSet GetValueSetAt(ITimeSet at);

        void SetCache(ITime at, ITimeSpaceValueSet values);
        void SetCache(ITimeSet at, ITimeSpaceValueSet values);
    }

    public interface IValueSetConverterTimeEngine : IValueSetConverterPacking
    {
        string EngineVariable { get; }

        // IBaseValueSet comes from Input exchange item
        void ToEngine(IEngine iEngine, IBaseValueSet iValueSet);

        // Cache for Output exchange item
        void CacheEngineValues(IEngine iEngine);
    }

    public interface IValueSetConverterTimeRecord<TType> : IValueSetConverterTyped<TType>, IValueSetConverterTime
    {
        IEnumerable<TType> LinearInterpolation(TimeRecord<TType> below, TimeRecord<TType> above, double factor);

        void CacheRecords(IEnumerable<TimeRecord<TType>> values);
        IEnumerable<TimeRecord<TType>> CacheClone();

        TimeRecord<TType> GetRecordAt(ITime at, List<string> eventArgMessages);
    }
}
