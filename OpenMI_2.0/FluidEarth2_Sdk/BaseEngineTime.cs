
using FluidEarth2.Sdk.Interfaces;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A base implementation of the FluidEarth2.Sdk.Interfaces.IEngine and FluidEarth2.Sdk.Interfaces.IEngineTime
    /// 
    /// Provides default implementations for many of the IEngine interfaces except for
    /// Update(double timeCurrent) which is abstract and so must be implemented by inherited class.
    /// </summary>
    public abstract class BaseEngineTime : BaseEngine, IEngineTime
    {
        /// <summary>
        /// During runtime, current time horizon for the engine
        /// </summary>
        Time _horizon;
        /// <summary>
        /// During runtime, what time does the engine believe it is currently at.
        /// </summary>
        double _timeCurrent;


        /// <summary>
        /// Simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Initialise()
        /// 
        /// Overide if specific additional functionality required.
        /// </summary>
        /// <param name="initialisingXml">See FluidEarth2.Sdk.BaseEngine</param>
        /// <param name="accessor">See FluidEarth2.Sdk.BaseEngine</param>
        public override void Initialise(string initialisingXml, IDocumentAccessor accessor)
        {
            base.Initialise(initialisingXml, accessor);

            _horizon = new Time(ArgumentTimeHorizon);   
            _timeCurrent = _horizon.StampAsModifiedJulianDay;

            if (double.IsNegativeInfinity(_timeCurrent))
                throw new Exception("Time horizon start is unbounded");
        }

        /// <summary>
        /// During runtime, current time horizon for the engine
        /// </summary>
        public ITime TimeHorizon
        {
            get { return _horizon; }
        }

        /// <summary>
        /// Time horizon for which engine will be valid to run
        /// 
        /// Value manged by a IArgument stored in BaseEngine::Arguments
        /// </summary>
        public Time ArgumentTimeHorizon
        {
            get
            {
                return ((ArgumentTime)Argument(BaseComponentTimeWithEngine.GetArgumentIdentity(
                    BaseComponentTimeWithEngine.ArgsWithEngineTime.TimeHorizon))).Time;
            }
        }

        /// <summary>
        /// During runtime, what time does the engine believe it is currently at.
        /// </summary>
        /// <returns>The time the engine is currently at</returns>
        public double GetCurrentTime()
        {
            return _timeCurrent;
        }

        /// <summary>
        /// Implements interface FluidEarth2.Sdk.Interfaces.IEngine::Prepare()
        /// using BaseEngine::Prepare()
        /// 
        /// Overide if specific additional functionality required.
        /// </summary>
        public override void Prepare()
        {
            base.Prepare();
        }

        /// <summary>
        /// Move the engine forward in time
        /// </summary>
        /// <param name="timeCurrent">Time engine is currently on</param>
        /// <returns>New engine time, must be >= timeCurrent</returns>
        public abstract double Update(double timeCurrent);

        /// <summary>
        /// Implements BaseEngine::Update() using BaseEngineTime::Update(double)
        /// </summary>
        public override void Update()
        {
            if (!double.IsPositiveInfinity(_horizon.DurationInDays) && _timeCurrent >= _horizon.StampAsModifiedJulianDay + _horizon.DurationInDays)
                throw new Exception(string.Format("Trying to progress engine beyond its time horizon " + _horizon.ToString()));

            double newTime = Update(_timeCurrent);

            if (newTime < _timeCurrent)
                throw new Exception(string.Format("New engine time is less than previous time; {0} < {1}",
                    new Time(newTime).ToString(), new Time(_timeCurrent).ToString()));

            _timeCurrent = newTime;
        }
    }
}
