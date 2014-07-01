
using OpenMI.Standard2.TimeSpace;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    class RemotingClientEngineTime : RemotingClientEngine, IEngineTime
    {
        public double GetCurrentTime()
        {
            try
            {
                EngineMethodCall("GetCurrentTime");

                return ((IEngineTime)_connection.Base).GetCurrentTime();
            }
            catch (System.Exception e)
            {
                throw EngineMethodCatch("GetCurrentTime", e);
            }
        }
    }
}
