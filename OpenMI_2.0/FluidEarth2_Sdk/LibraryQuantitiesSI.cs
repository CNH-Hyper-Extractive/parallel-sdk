using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenMI.Standard2;
using FluidEarth2.Sdk.CoreStandard2;

namespace FluidEarth2.Sdk.Library
{
    public static class Quantities
    {
        public static Quantity DimensionlessDouble(string caption)
        {
            var describes = new Describes(caption, "Dimensionless Double (-Inf if unspecified)");

            return new Quantity(
                new ValueDefinition(describes, typeof(double), Double.NegativeInfinity),
                new Unit(new Describes("Dimensionless", "No specified dimensions")));
        }

        public static Quantity DimensionlessInt32Positive(string caption)
        {
            var describes = new Describes(caption, "Dimensionless Int32 (-1 if unspecified)");

            return new Quantity(
                new ValueDefinition(describes, typeof(Int32), -1),
                new Unit(new Describes("Dimensionless", "No specified dimensions")));
        }

        public static Quantity DimensionlessString(string caption)
        {
            var describes = new Describes(caption, "Dimensionless Double (UNSPECIFIED if unspecified)");

            return new Quantity(
                new ValueDefinition(describes, typeof(string), "UNSPECIFIED"),
                new Unit(new Describes("Dimensionless", "No specified dimensions")));
        }
    }

    public static class QuantitiesSI
    {
        public static IEnumerable<IDescribable> Repository
        {
            get
            {
                return new IDescribable[] { 
                    Length("SI Length (m)"),
                    Area("SI Area (m2)"),
                    Volume("SI Volume (m3)"),
                    Velocity("SI Velocity (m/s)"),
                    Velocity2d("SI Velocity 2d (m/s)"),
                    Velocity3d("SI Velocity 3d (m/s)"),
                    Discharge("SI Discharge (m3/s)"),
                    Pressure("SI Pressure (kg/m/s2)"),
                };
            }
        }

        static Quantity GetQuantity(IDescribable describes, IDimension iDimension)
        {
            return GetQuantity(describes, iDimension, 1);
        }

        static Quantity GetQuantity(IDescribable describes, IDimension iDimension, int dimension)
        {
            ValueDefinition vd;

            switch (dimension)
            {
                case 1:
                    vd = new ValueDefinition(describes,
                        typeof(double), 
                        Double.NegativeInfinity);
                    break;
                case 2:
                    vd = new ValueDefinition(describes,
                        typeof(Vector2d<double>), 
                        new Vector2d<double>(Double.NegativeInfinity));
                    break;
                case 3:
                    vd = new ValueDefinition(describes,
                        typeof(Vector3d<double>),
                        new Vector3d<double>(Double.NegativeInfinity));
                    break;
                default:
                    throw new NotImplementedException(dimension.ToString());
            }

            var unit = new Unit(describes, iDimension);

            return new Quantity(vd, unit);
        }

        public static Quantity Length(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Length[1], m");

            return GetQuantity(describes, Dimensions.Length());
        }

        public static Quantity Area(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Area[1], m2");

            return GetQuantity(describes, Dimensions.Area());
        }

        public static Quantity Volume(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Volume[1], m3");

            return GetQuantity(describes, Dimensions.Volume());
        }

        public static Quantity Velocity(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Velocity[1], m/s");

            return GetQuantity(describes, Dimensions.Velocity());
        }

        public static Quantity Velocity2d(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Velocity[2], m/s");

            return GetQuantity(describes, Dimensions.Velocity(), 2);
        }

        public static Quantity Velocity3d(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Velocity[3], m/s");

            return GetQuantity(describes, Dimensions.Velocity(), 3);
        }

        public static Quantity Pressure(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Pressure[1], kg·m−1·s−2");

            return GetQuantity(describes, Dimensions.Pressure());
        }

        public static Quantity Discharge(string caption)
        {
            Describes describes = new Describes(caption,
                "Système international d'unités: Discharge[1], m3·s−1");

            return GetQuantity(describes, Dimensions.Discharge());
        }
    }
}
