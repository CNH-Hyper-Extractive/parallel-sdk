
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public interface IValueSetSpaceTime<TType> : ITimeSpaceValueSet
    {
        ReadOnlyCollection<TimeRecord<TType>> Records { get; }
    }
}
