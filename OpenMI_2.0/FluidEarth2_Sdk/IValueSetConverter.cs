
using System;
using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public interface IValueSetConverter : ICloneable, IPersistence
    {
        // TODO
        // split up into IValueSetConverterBase and IValueSetConverterNonTemporal

        IBaseExchangeItem ExchangeItem { set; get; }

        void EmptyCaches(ITime upto);
        IBaseValueSet GetCache();
        void SetCache(IBaseValueSet values);

        IBaseValueSet GetValueSetLatest();

        string DiagnosticSummary();

        event EventHandler<ExchangeItemChangeEventArgs> ItemChanged;
    }

    public interface IValueSetConverterTyped<TType> : IValueSetConverter
    {
        IBaseValueSet ToValueSet(IEnumerable<TType> values);
        IEnumerable<TType> ToArray(IBaseValueSet values);

        // Used in CSV so must NOT contain any commas
        string ToString(TType value);
        TType ToValue(string value);
    }

    public interface IValueSetConverterPacking
    {
        int ElementCount { get; }
        int VectorLength { get; }

        bool ElementValueCountConstant { get; }
        int ElementValueCount { get; }
        int[] ElementValueCounts { get; }

        int ValueArrayLength { get; }
    }
}
