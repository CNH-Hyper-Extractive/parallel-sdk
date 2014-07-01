
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class ValueSetElementSingleValued<TType> : ValueSetElementBase<TType>
    {
        public ValueSetElementSingleValued(int elementCount)
        {
            base.Initialise(elementCount, 1, null);
        }

        public ValueSetElementSingleValued(int elementCount, IEnumerable<TType> values)
        {
            base.Initialise(elementCount, 1, values);
        }
    }

    public class ValueSetElementMultiValued<TType> : ValueSetElementBase<TType>
    {
        public ValueSetElementMultiValued(int elementCount, int elementValueCount)
        {
            base.Initialise(elementCount, elementValueCount, null);
        }

        public ValueSetElementMultiValued(int elementCount, int elementValueCount, IEnumerable<TType> values)
        {
            base.Initialise(elementCount, elementValueCount, values);
        }
    }

    public class ValueSetElementMultiMixedValued<TType> : ValueSetElementBase<TType>
    {
        public ValueSetElementMultiMixedValued(int[] elementValueCounts)
        {
            base.Initialise(elementValueCounts, null);
        }

        public ValueSetElementMultiMixedValued(int[] elementValueCounts, IEnumerable<TType> values)
        {
            base.Initialise(elementValueCounts, values);
        }
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public abstract class ValueSetElementBase<TType> : IBaseValueSet
    {
        int _elementCount;
        int[] _elementMultiValueCounts;
        int[] _offsets;
        TType[] _values;

        public void Initialise(int elementCount, int elementMultiValueCount)
        {
            Initialise(elementCount, elementMultiValueCount, null);
        }

        public void Initialise(int elementCount, int elementMultiValueCount, IEnumerable<TType> values)
        {
            _elementCount = elementCount;

            _elementMultiValueCounts = new int[elementCount];

            for (int n = 0; n < _elementCount; ++n)
                _elementMultiValueCounts[n] = elementMultiValueCount;

            _offsets = new int[_elementCount + 1];
            _offsets[0] = 0;

            for (int o = 0; o < _elementCount; ++o)
                _offsets[o + 1] = _offsets[o] + _elementMultiValueCounts[o];

            _values = new TType[_offsets.Last()];

            if (values != null)
            {
                if (values.Count() != _offsets.Last())
                    throw new Exception(string.Format(
                        "Invalid values vector length; expected {0}, received {1}",
                        _offsets.Last(), values.Count()));

                Array.Copy(values.ToArray(), _values, _offsets.Last());
            }
        }

        public void Initialise(int[] elementMultiValueCounts)
        {
            Initialise(elementMultiValueCounts, null);
        }

        public void Initialise(int[] elementMultiValueCounts, IEnumerable<TType> values)
        {
            _elementCount = elementMultiValueCounts.Length;

            _elementMultiValueCounts = elementMultiValueCounts;

            _offsets = new int[_elementCount + 1];
            _offsets[0] = 0;

            for (int o = 0; o < _elementCount; ++o)
                _offsets[o + 1] = _offsets[o] + _elementMultiValueCounts[o];

            _values = new TType[_offsets.Last()];

            if (values != null)
            {
                if (values.Count() != _offsets.Last())
                    throw new Exception(string.Format(
                        "Invalid values vector length; expected {0}, received {1}",
                        _offsets.Last(), values.Count()));

                Array.Copy(values.ToArray(), _values, _offsets.Last());
            }
        }

        public int GetIndexCount(int[] indices)
        {
            if (indices == null || indices.Length == 0)
                return _elementCount;

            switch (indices.Length)
            {
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
            ValidElementIndex(indices[Element]);

            int length = _elementMultiValueCounts[indices[Element]];

            if (length == 1)
                return _offsets[indices[Element]];
            else
            {
                TType[] values = new TType[length];

                Array.Copy(
                    _values, _offsets[indices[Element]],
                    values, 0,
                    length);

                return values;
            }
        }

        public void SetValue(int[] indices, object value)
        {
            ValidIndices(indices);
            ValidElementIndex(indices[Element]);

            TType[] valuesVectors = (TType[])value;

            int length = _elementMultiValueCounts[indices[Element]];

            if (valuesVectors.Length != _elementMultiValueCounts[indices[Element]])
                throw new Exception(string.Format(
                    "Invalid values vector length; expected {0} received {1}",
                    _elementMultiValueCounts[indices[Element]], 
                    valuesVectors.Length));

            if (length == 1)
                _values[_offsets[indices[Element]]] = (TType)value;
            else
                Array.Copy(
                    valuesVectors, 0,
                    _values, _offsets[indices[Element]],
                    length);
        }

        public Type ValueType
        {
            get { return typeof(TType); }
        }

        public TType[] Values
        {
            get { return _values; }
        }

        /// <summary>
        /// ValueSet [Element][ElementMultiValue]
        /// </summary>
        public int NumberOfIndices
        {
            get { return ElementValues + 1; }
        }

        const int Element = 0;
        const int ElementValues = 1;

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

#if WIP
    /// <summary>
    /// 2D ValueSet [Element, ElementOffSet]
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class BaseValueSetValueElementCountConstant<TType> : IBaseValueSet
    {
        int _elementCount;
        int _valueCountPerElement;

        TType[] _values;

        public void Initialise(int elementCount, TType missingValue, int valueCountPerElement, IEnumerable<TType> values = null)
        {
            _elementCount = elementCount;
            _valueCountPerElement = valueCountPerElement;

            _values = new TType[_elementCount * _valueCountPerElement];

            if (values != null)
            {
                if (values.Count() != _values.Count())
                    throw new Exception(string.Format(
                        "Invalid values vector length; expected {0}, received {1}",
                        _values.Count(), values.Count()));

                Array.Copy(values.ToArray(), _values, _values.Count());
            }
            else
                for (int n = 0; n < _elementCount; ++n)
                    _values[n] = missingValue;
        }

        public int GetIndexCount(int[] indices)
        {
            if (indices == null || indices.Length == 0)
                return _elementCount;
            else if (indices.Length == 1)
                return _valueCountPerElement;

            throw new IndexOutOfRangeException();
        }

        public object GetValue(int[] indices)
        {
            return _values[ValueIndex(indices)];
        }

        public void SetValue(int[] indices, object value)
        {
            _values[ValueIndex(indices)] = (TType)value;
        }

        public Type ValueType
        {
            get { return typeof(TType); }
        }

        public TType[] Values
        {
            get { return _values; }
        }

        /// <summary>
        /// 2D ValueSet [Element, ElementOffSet]
        /// </summary>
        public int NumberOfIndices
        {
            get { return 2; }
        }

        void ValidElementIndex(int index)
        {
            if (index < 0 || index >= _elementCount)
                throw new Exception(string.Format("Invalid element index {0}, range [0,{1})",
                    index, _elementCount));
        }

        void ValidOffsetIndex(int index)
        {
            if (index < 0 || index >= _valueCountPerElement)
                throw new Exception(string.Format("Invalid offset index {0}, range [0,{1})",
                    index, _valueCountPerElement));
        }

        void ValidIndices(int[] indices)
        {
            if (indices == null || indices.Length < NumberOfIndices)
                throw new Exception(string.Format("Invalid indices length {0}, range [0,{1})",
                    indices.Length, NumberOfIndices));

            if (indices.Length > 0)
                ValidElementIndex(indices[0]);
            if (indices.Length > 1)
                ValidOffsetIndex(indices[1]);
        }

        int ValueIndex(int[] indices)
        {
            ValidIndices(indices);

            return indices[0] * _valueCountPerElement + indices[1];
        }
    }
#endif
}
