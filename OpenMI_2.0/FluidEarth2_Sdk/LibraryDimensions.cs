#region GNU GPL - Fluid Earth SDK
/*
    This file is part of 'Fluid Earth SDK'.

    'Fluid Earth SDK' is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    'Fluid Earth SDK' is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with 'Fluid Earth SDK'.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion GNU GPL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenMI.Standard2;
using FluidEarth2.Sdk.CoreStandard2;

namespace FluidEarth2.Sdk.Library
{
    class Dimensions
    {
        public static Dimension Length()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Length, 1.0);
            return d;
        }

        public static Dimension Area()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Length, 2.0);
            return d;
        }

        public static Dimension Volume()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Length, 3.0);
            return d;
        }
    
        public static Dimension Velocity()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Length, 1.0); 
            d.SetPower(DimensionBase.Time, -1.0);
            return d;
        }

        public static Dimension Pressure()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Mass, 1.0);
            d.SetPower(DimensionBase.Length, -1.0);
            d.SetPower(DimensionBase.Time, -2.0);
            return d;
        }

        public static Dimension Discharge()
        {
            Dimension d = new Dimension();
            d.SetPower(DimensionBase.Length, 3.0);
            d.SetPower(DimensionBase.Time, -1.0);
            return d;
        }
    }
}
