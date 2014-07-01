using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class Vector2d<TType> : VectorBase<TType>
        where TType : IConvertible
    {
        public Vector2d()
            : base(2)
        { }

        public Vector2d(string values)
            : base(2, values)
        { }

        public Vector2d(TType value)
            : this(value, value)
        { }

        public Vector2d(TType value1, TType value2)
            : base(2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public Vector2d(TType[] values)
            : base(2, values)
        { }

        public Vector2d(TType[] values, int offSet)
            : base(2, values, offSet)
        { }

        public Vector2d(Vector2d<TType> v)
            : base(2, v.Values)
        { }

        public Vector2d(XElement xElement, IDocumentAccessor accessor)
            : base(2)
        {
            Initialise(xElement, accessor);
        }

        public TType Value1
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public TType Value2
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public override IVector New(string values)
        {
            return new Vector2d<TType>(values);
        }
    }

    public class Vector3d<TType> : VectorBase<TType>
        where TType : IConvertible
    {
        public Vector3d()
            : base(3)
        { }

        public Vector3d(TType value1, TType value2, TType value3)
            : base(3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public Vector3d(TType value)
            : this(value, value, value)
        { }

        public Vector3d(string values)
            : base(3, values)
        { }

        public Vector3d(TType[] values)
            : base(3, values)
        { }

        public Vector3d(TType[] values, int offSet)
            : base(3, values, offSet)
        { }

        public Vector3d(Vector3d<TType> v)
            : base(3, v.Values)
        { }

        public Vector3d(XElement xElement, IDocumentAccessor accessor)
            : base(3)
        {
            Initialise(xElement, accessor);
        }

        public TType Value1
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public TType Value2
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public TType Value3
        {
            get { return Values[2]; }
            set { Values[2] = value; }
        }

        public override IVector New(string values)
        {
            return new Vector3d<TType>(values);
        }
    }
}
