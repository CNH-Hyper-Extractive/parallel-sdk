
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetTimeElementSingleValued<TType> : ValueSetTimeElementBase<TType>
    {
        public ValueSetTimeElementSingleValued(int elementCount)
        {
            base.Initialise(elementCount, 1, null);
        }

        public ValueSetTimeElementSingleValued(int elementCount, IEnumerable<TimeRecord<TType>> records)
        {
            base.Initialise(elementCount, 1, records);
        }
    }

    public class ValueSetTimeElementMultiValued<TType> : ValueSetTimeElementBase<TType>
    {
        public ValueSetTimeElementMultiValued(int elementCount, int elementValueCount)
        {
            base.Initialise(elementCount, elementValueCount, null);
        }

        public ValueSetTimeElementMultiValued(int elementCount, int elementValueCount, IEnumerable<TimeRecord<TType>> records)
        {
            base.Initialise(elementCount, elementValueCount, records);
        }
    }

    public class ValueSetTimeElementMultiMixedValued<TType> : ValueSetTimeElementBase<TType>
    {
        public ValueSetTimeElementMultiMixedValued(int[] elementValueCounts)
        {
            base.Initialise(elementValueCounts, null);
        }

        public ValueSetTimeElementMultiMixedValued(int[] elementValueCounts, IEnumerable<TimeRecord<TType>> records)
        {
            base.Initialise(elementValueCounts, records);
        }
    }

    /// <summary>
    /// ValueSet [Time][Element][ElementValues]
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public abstract class ValueSetTimeElementBase<TType> : IValueSetSpaceTime<TType>
    {
        int _elementCount;
        int[] _elementMultiValueCounts;
        int[] _offsets;
        List<TimeRecord<TType>> _cache = new List<TimeRecord<TType>>();

        public void Initialise(int elementCount, int elementMultiValueCount)
        {
            Initialise(elementCount, elementMultiValueCount, null);
        }

        public void Initialise(int elementCount, int elementMultiValueCount, IEnumerable<TimeRecord<TType>> records)
        {
            _elementCount = elementCount;

            _elementMultiValueCounts = new int[elementCount];
            for (int n = 0; n < _elementCount; ++n)
                _elementMultiValueCounts[n] = elementMultiValueCount;

            _offsets = new int[_elementCount];

            if (_elementCount > 0)
                _offsets[0] = 0;

            for (int o = 1; o < _elementCount; ++o)
                _offsets[o] = _offsets[o - 1] + _elementMultiValueCounts[o - 1];

            if (records != null)
                _cache.AddRange(records);
        }

        public void Initialise(int[] elementMultiValueCounts)
        {
            Initialise(elementMultiValueCounts, null);
        }

        public void Initialise(int[] elementMultiValueCounts, IEnumerable<TimeRecord<TType>> records)
        {
            _elementCount = elementMultiValueCounts.Length;

            _elementMultiValueCounts = elementMultiValueCounts;

            _offsets = new int[_elementCount];

            if (_elementCount > 0)
                _offsets[0] = 0;

            for (int o = 1; o < _elementCount; ++o)
                _offsets[o] = _offsets[o - 1] + _elementMultiValueCounts[o - 1];

            if (records != null)
                _cache.AddRange(records);
        }

        public ReadOnlyCollection<TimeRecord<TType>> Records
        {
            get { return _cache.AsReadOnly(); }
        }

        public IList GetElementValuesForTime(int timeIndex)
        {
            ValidTimeIndex(timeIndex);
            return _cache[timeIndex].Values.ToList();
        }

        public IList GetTimeSeriesValuesForElement(int elementIndex)
        {
            ValidElementIndex(elementIndex);

            List<TType[]> series = new List<TType[]>();

            int length = _elementMultiValueCounts[elementIndex];

            for (int t = 0; t < _cache.Count; ++t)
            {
                series.Add(new TType[length]);

                Array.Copy(
                    _cache[t].Values, _offsets[elementIndex],
                    series.Last(), 0,
                    length);
            }

            return series;
        }

        public object GetValue(int timeIndex, int elementIndex)
        {
            ValidTimeIndex(timeIndex);
            ValidElementIndex(elementIndex);

            int length = _elementMultiValueCounts[elementIndex];

            if (length == 1)
                return _cache[timeIndex].Values[_offsets[elementIndex]];
            else
            {
                TType[] values = new TType[length];

                Array.Copy(
                    _cache[timeIndex].Values, _offsets[elementIndex],
                    values, 0,
                    length);

                return values;
            }
        }

        public void SetValue(int timeIndex, int elementIndex, object value)
        {
            ValidTimeIndex(timeIndex);
            ValidElementIndex(elementIndex);

            TType[] valuesVectors = (TType[])value;

            int length = _elementMultiValueCounts[elementIndex];

            if (valuesVectors.Length != _elementMultiValueCounts[elementIndex])
                throw new Exception(string.Format(
                    "Invalid values vector length for time index {0}; expected {1}, received {2}",
                    timeIndex, _elementMultiValueCounts[elementIndex], valuesVectors.Length));

            if (length == 1)
                _cache[timeIndex].Values[_offsets[elementIndex]] = (TType)value;
            else
                Array.Copy(
                    valuesVectors, 0,
                    _cache[timeIndex].Values, _offsets[elementIndex],
                    length);
        }

        public void SetElementValuesForTime(int timeIndex, IList values)
        {
            ValidTimeIndex(timeIndex);

            if (values.Count != _offsets.Last())
                throw new Exception(string.Format(
                    "Invalid multi valued element vector list length, expected {0}, received {1}",
                    _offsets.Last(), values.Count));

            values.CopyTo(_cache[timeIndex].Values, 0);
        }

        public void SetTimeSeriesValuesForElement(int elementIndex, IList values)
        {
            ValidElementIndex(elementIndex);

            if (values.Count != _cache.Count)
                throw new Exception(string.Format("Invalid time values list length, expected {0}, received {1}",
                    _cache.Count, values.Count));

            for (int t = 0; t < _cache.Count; ++t)
                SetValue(t, elementIndex, values[t]);
        }

        public IList<IList> Values2D
        {
            get
            {
                List<IList> values = new List<IList>();

                object[] vs;

                for (int t = 0; t < _cache.Count; ++t)
                {
                    vs = new object[_elementCount];

                    for (int e = 0; e < _elementCount; ++e)
                        vs[e] = GetValue(t, e);

                    values.Add(vs);
                }

                return values.ToList();
            }

            set
            {
                // Its a shame that the we only have time indexs not times
                // so TimeRecord<TType>.Time == null for all items in cache

                _cache = new List<TimeRecord<TType>>();

                for (int t = 0; t < value.Count; ++t)
                {
                    _cache.Add(new TimeRecord<TType>());
                    _cache.Last().Values = new TType[_offsets.Last()];

                    for (int e = 0; e < _elementCount; ++e)
                        SetValue(t, e, value[t][e]);
                }
            }
        }

        public int GetIndexCount(int[] indices)
        {
            if (indices == null || indices.Length == 0)
                return _cache.Count();

            switch (indices.Length)
            {
                case Time:
                    return _cache.Count;
                case Element:
                    return _elementCount; // element count constant wrt time
                case ElementValues:
                    ValidElementIndex(indices[Element]);
                    return _elementMultiValueCounts[indices[Element]];
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public object GetValue(int[] indices)
        {
            ValidIndices(indices);
            return GetValue(indices[Time], indices[Element]);
        }

        /// <summary>
        /// ValueSet [Time][Element][ElementValues]
        /// </summary>
        public int NumberOfIndices
        {
            get { return 3; }
        }

        public void SetValue(int[] indices, object value)
        {
            ValidIndices(indices);
            SetValue(indices[Time], indices[Element], (TType)value);
        }

        public Type ValueType
        {
            get { return typeof(TType); }
        }

        const int Time = 0;
        const int Element = 1;
        const int ElementValues = 2;

        void ValidTimeIndex(int index)
        {
            if (index < 0 || index >= _cache.Count)
                throw new Exception(string.Format("Invalid time index {0}, range [0,{1})",
                    index, _cache.Count));
        }

        void ValidElementIndex(int index)
        {
            if (index < 0 || index >= _elementCount)
                throw new Exception(string.Format("Invalid element index {0}, range [0,{1})",
                    index, _elementCount));
        }

        void ValidIndices(int[] indices)
        {
            if (indices == null || indices.Length < NumberOfIndices)
                throw new Exception(string.Format("Invalid indices length {0}, range [0,{1})",
                    indices.Length, NumberOfIndices));
        }
    }
}

