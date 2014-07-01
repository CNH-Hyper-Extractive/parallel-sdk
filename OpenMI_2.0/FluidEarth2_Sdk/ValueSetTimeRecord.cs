using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetTimeRecord<TType> : ITimeSpaceValueSet
    {   
        List<TimeRecord<TType>> _records;

        public ValueSetTimeRecord()
        { }

        public ValueSetTimeRecord(ITimeSpaceValueSet values)
        {
            Values2D = values.Values2D;
        }

        public ValueSetTimeRecord(TimeRecord<TType> record)
        {
            _records = new List<TimeRecord<TType>>();
            _records.Add(record);
        }

        public ValueSetTimeRecord(IEnumerable<TimeRecord<TType>> records)
        {
            _records = new List<TimeRecord<TType>>(records);
        }

        public int TimeCount
        {
            get { return _records.Count; }
        }

        public int ElementCount
        {
            get { return _records.Count > 0 ? _records[0].Values.Count() : 0; }
        }

        public Type ValueType
        {
            get { return typeof(TType); }
        }

        public List<TimeRecord<TType>> Records
        {
            get { return _records; }
            set { _records = value; }
        }

        public IList<IList> Values2D
        {
            get
            {
                var l = new List<IList>();

                foreach (var r in _records)
                    l.Add(r.Values.ToList());

                return l;
            }

            set
            {
                _records = new List<TimeRecord<TType>>(value.Count);

                foreach (var vs in value)
                    _records.Add(new TimeRecord<TType>(new Time(),
                        vs.AsQueryable().Cast<TType>().ToArray()));
            }
        }

        public IList GetElementValuesForTime(int timeIndex)
        {
            return _records[timeIndex].Values;
        }

        public void SetElementValuesForTime(int timeIndex, IList values)
        {
            _records[timeIndex].Values = values
                .Cast<TType>()
                .ToArray();
        }

        public IList GetTimeSeriesValuesForElement(int elementIndex)
        {
            return Records
                .Select(r => r.Values[elementIndex])
                .ToList();
        }

        public object GetValue(int timeIndex, int elementIndex)
        {
            return Records[timeIndex].Values[elementIndex];
        }

        public void SetTimeSeriesValuesForElement(int elementIndex, IList values)
        {
            for (int n = 0; n < Records.Count; ++n)
                SetValue(n, elementIndex, values[elementIndex]);
            foreach (var r in Records)
                r.Values[elementIndex] = (TType)values[elementIndex];
        }

        public void SetValue(int timeIndex, int elementIndex, object value)
        {
            Records[timeIndex].Values[elementIndex] = (TType)value;
        }

        protected const int ElementIndex = 0;

        public int GetIndexCount(int[] indices)
        {
            if (indices == null || indices.Length == 0)
                return ElementCount;

            if (indices.Length == ElementIndex)
                return ElementCount;

            throw new IndexOutOfRangeException();
        }

        public object GetValue(int[] indices)
        {
            return Records
                .Last()
                .Values[indices[indices[ElementIndex]]];
        }

        public void SetValue(int[] indices, object value)
        {
            Records
                .Last()
                .Values[indices[indices[ElementIndex]]] = (TType)value;
        }

        public int NumberOfIndices
        {
            get { return ElementIndex + 1; }
        }
    }
}
