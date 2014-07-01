using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptionEngineTime : InterceptionEngine, IEngineTime
    {
        public InterceptionEngineTime(IEngineTime engine, IEnumerable<IIntercept> intercepts, bool active)
            : base(engine, intercepts, active)
        { }

        public double GetCurrentTime()
        {
            if (!_active)
                return ((IEngineTime)_engine).GetCurrentTime();

            try
            {
                DoStart("GetCurrentTime");

                return DoValue(((IEngineTime)_engine).GetCurrentTime());
            }
            finally
            {
                DoFinally();
            }
        }
    }
}
