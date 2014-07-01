using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class Coord2d : VectorBase<double>
    {
        public Coord2d()
            : base(2)
        { }

        public Coord2d(string values)
            : base(2, values)
        { }

        public Coord2d(double value)
            : this(value, value)
        { }

        public Coord2d(double value1, double value2)
            : base(2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public Coord2d(double[] values)
            : base(2, values)
        { }

        public Coord2d(double[] values, int offSet)
            : base(2, values, offSet)
        { }

        public Coord2d(XElement xElement, IDocumentAccessor accessor)
            : base(2)
        {
            Initialise(xElement, accessor);
        }

        public double Value1
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public double Value2
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public override IVector New(string values)
        {
            return new Coord2d(values);
        }
    }

    public class Coord3d : VectorBase<double>
    {
        public Coord3d()
            : base(3)
        { }

        public Coord3d(double value1, double value2, double value3)
            : base(3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public Coord3d(double value)
            : this(value, value, value)
        { }

        public Coord3d(string values)
            : base(3, values)
        { }

        public Coord3d(double[] values)
            : base(3, values)
        { }

        public Coord3d(double[] values, int offSet)
            : base(3, values, offSet)
        { }

        public Coord3d(XElement xElement, IDocumentAccessor accessor)
            : base(3)
        {
            Initialise(xElement, accessor);
        }

        public double Value1
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public double Value2
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public double Value3
        {
            get { return Values[2]; }
            set { Values[2] = value; }
        }

        public override IVector New(string values)
        {
            return new Coord3d(values);
        }
    }
}
