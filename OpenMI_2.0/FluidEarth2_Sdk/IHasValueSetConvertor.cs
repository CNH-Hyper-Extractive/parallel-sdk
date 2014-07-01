using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluidEarth2.Sdk
{
    public interface IHasValueSetConvertor
    {
        IValueSetConverter ValueSetConverter { get; }
    }
}
