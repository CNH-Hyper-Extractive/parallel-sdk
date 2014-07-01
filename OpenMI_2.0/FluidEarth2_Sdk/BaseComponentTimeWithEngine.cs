
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public abstract class BaseComponentTimeWithEngine : BaseComponentWithEngine, ITimeSpaceComponent, IPersistence
    {
        TimeSet _timeExtent;
        protected bool _engineDone;

        public enum ArgsWithEngineTime { TimeHorizon = 0, }

        public static IIdentifiable GetArgumentIdentity(ArgsWithEngineTime key)
        {
            switch(key)
            {
                case ArgsWithEngineTime.TimeHorizon:
                    return new Identity("FluidEarth2.Sdk.BaseComponentTimeWithEngine." + key.ToString(),
                        "Time Horizon", 
                        "* Time range for which engine can run,"
                        + " \"Unbounded\" indicates no restraint."
                        + " If unbounded user must set start time before running so engine"
                        + " knows from when to initialise itself from."
                        + "\r\n* Date/Time's are displayed in 'Universal Sortable (\"u\")' format"
                        + " \"yyyy-MM-dd HH:mm:ssZ\" where Z indicates Zulu time (GMT).");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public Time ArgumentTimeHorizon
        {
            get
            {
                return ((ArgumentTime)Argument(GetArgumentIdentity(ArgsWithEngineTime.TimeHorizon))).Time;
            }

            set
            {
                ((ArgumentTime)Argument(GetArgumentIdentity(ArgsWithEngineTime.TimeHorizon))).Time = value;
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public BaseComponentTimeWithEngine()
        { }

        /// <summary>
        /// Constructor for .NET coded IEngine
        /// </summary>
        /// <param name="identity">Identity for the component</param>
        /// <param name="derivedComponentType">Type of derived class encapsulated as an ExternalType</param>
        /// <param name="engineType">Type of IEngine derived class encapsulated as an ExternalType</param>
        public BaseComponentTimeWithEngine(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType)
            : this(identity, derivedComponentType, engineType, false)
        { }

        /// <summary>
        /// Constructor for native coded IEngine
        /// </summary>
        /// <param name="identity">Identity for the component</param>
        /// <param name="derivedComponentType">Type of derived class encapsulated as an ExternalType</param>
        /// <param name="engineType">Type of IEngine derived class encapsulated as an ExternalType</param>
        /// <param name="useNativeDllArgument">True if engineType is actually a .NEt wrapper for
        /// a native implementation of IEngine, i.e. uses C interface to talk to engine which
        /// might be in some non .NET language e.g. C++/FORTRAN/Python etc</param>
        public BaseComponentTimeWithEngine(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType, bool useNativeDllArgument)
            : base(identity, derivedComponentType, engineType, useNativeDllArgument)
        {
            _timeExtent = new TimeSet();
            _timeExtent.SetTimeHorizon(new Time());

            var argTime = new ArgumentTime(GetArgumentIdentity(ArgsWithEngineTime.TimeHorizon),
                new Time(_timeExtent.TimeHorizon));

            argTime.ValueChanged += new EventHandler<Argument<ArgumentValueTime>.ValueChangedEventArgs>(OnArgumentChangedTimeHorizon);

            Arguments.Add(argTime);

            // Base class sets typeof(RemotingServerEngineTime) not typeof(RemotingServerEngine)
            // Dont have a RemotingServerEngine as RemotingServerEngineTime only has one additional time method
            // which we can jsut ensure not to call!
            //((ArgumentParametersRemoting)Argument(GetArgumentIdentity(ArgsWithEngine.Remoting))).ParametersRemoting 
            //    = new ParametersRemoting(typeof(RemotingServerEngineTime);
        }

        void OnArgumentChangedTimeHorizon(object sender, Argument<ArgumentValueTime>.ValueChangedEventArgs e)
        {
            var argTime = (ArgumentTime)sender;

            _timeExtent.SetTimeHorizon(argTime.Time);
        }

        public BaseComponentTimeWithEngine(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override string StatusChangeEventMessage(LinkableComponentStatus oldStatus, LinkableComponentStatus newStatus)
        {
            if (newStatus == LinkableComponentStatus.Updated
                || newStatus == LinkableComponentStatus.Updating
                || newStatus == LinkableComponentStatus.WaitingForData)
            {
                // This accesses engine so newStatus has to appropriate for the engine to still be available

                string engineTime = new Time(((IEngineTime)Engine).GetCurrentTime()).ToString();

                return string.Format("{0} at engine time {1}",
                    base.StatusChangeEventMessage(oldStatus, newStatus),
                    engineTime);
            }
            else
                return base.StatusChangeEventMessage(oldStatus, newStatus);
        }

        public override bool Validate(List<string> errors, List<string> warnings, List<string> details)
        {
            bool ok = base.Validate(errors, warnings, details);

            if (TimeExtent.TimeHorizon.StampAsModifiedJulianDay == double.NegativeInfinity)
            {
                var horizon = ArgumentTimeHorizon;

                if (double.IsInfinity(horizon.StampAsModifiedJulianDay))
                {
                    ok = false;
                    errors.Add(
                        "Time horizon start is unbounded so engine can not determine its initialisation time");
                }
            }

            return ok;
        }

        public override XElement EngineInitialisingXml(IDocumentAccessor accessor)
        {
            XElement xml = base.EngineInitialisingXml(accessor);

            xml.Add(new XElement("TimeExtent",
                Persistence.TimeSet.Persist(_timeExtent, accessor)));

            return xml;
        }

        protected override void DoInitialise(bool reinitialising)
        {
            base.DoInitialise(reinitialising);

            _timeExtent.Times.Clear();
        }

        protected override void DoPrepare()
        {
            ITime horizon = TimeExtent.TimeHorizon;

            double start = horizon.StampAsModifiedJulianDay;
            double end = double.IsInfinity(start) || double.IsInfinity(horizon.DurationInDays)
                ? double.PositiveInfinity
                : start + horizon.DurationInDays;

            string startDate = double.IsInfinity(start)
                ? "()" : Time.ToDateTimeString(start);
            string endDate = double.IsInfinity(end)
                ? "()" : Time.ToDateTimeString(end);

            Arguments.Add(new Argument<string>(
                new Identity("TimeExtent.TimeHorizon.ModifiedJulianStart", "TimeExtent.TimeHorizon.ModifiedJulianStart"),
                start.ToString()));

            Arguments.Add(new Argument<string>(
                new Identity("TimeExtent.TimeHorizon.ModifiedJulianEnd", "TimeExtent.TimeHorizon.ModifiedJulianEnd"),
                end.ToString()));

            Arguments.Add(new Argument<string>(
                new Identity("TimeExtent.TimeHorizon.DateTimeStart", "TimeExtent.TimeHorizon.DateTimeStart"),
                startDate));

            Arguments.Add(new Argument<string>(
                new Identity("TimeExtent.TimeHorizon.DateTimeEnd", "TimeExtent.TimeHorizon.DateTimeEnd"),
                endDate));

            base.DoPrepare();

            _engineDone = false;
        }

        List<IBaseInput> Consumers(IBaseOutput source)
        {
            List<IBaseInput> consumers = new List<IBaseInput>(source.Consumers);

            AddConsumersRecursive(source, consumers);

            return consumers;
        }

        void AddConsumersRecursive(IBaseOutput source, List<IBaseInput> consumers)
        {
            if (source == null)
                return;

            consumers.AddRange(source.Consumers);

            foreach (IBaseAdaptedOutput adapted in source.AdaptedOutputs)
                AddConsumersRecursive(adapted, consumers);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        protected override void DoUpdate(IEnumerable<IBaseOutput> requiredOutput)
        {
            double earliestConsumerTimeRequestStart = double.PositiveInfinity;
            double earliestConsumerTimeRequestEnd = double.NegativeInfinity;

            bool hasNonTemporalConsumers = false;

            ITime firstTime, lastTime;
            double startTime, endTime;
            int consumerTimeCount = 0;

            foreach (BaseOutput output in requiredOutput)
            {
                foreach (IBaseInput consumer in Consumers(output))
                {
                    if (consumer is ITimeSpaceExchangeItem)
                    {
                        ++consumerTimeCount;

                        firstTime = ((ITimeSpaceExchangeItem)consumer).TimeSet.Times.FirstOrDefault();

                        if (firstTime != null)
                        {
                            startTime = firstTime.StampAsModifiedJulianDay;

                            if (startTime < earliestConsumerTimeRequestStart)
                                earliestConsumerTimeRequestStart = startTime;
                        }

                        lastTime = ((ITimeSpaceExchangeItem)consumer).TimeSet.Times.LastOrDefault();

                        if (lastTime != null)
                        {
                            endTime = Time.EndTime(lastTime);

                            if (endTime > earliestConsumerTimeRequestEnd)
                                earliestConsumerTimeRequestEnd = endTime;
                        }
                    }
                    else
                        hasNonTemporalConsumers = true;
                }
            }

            Trace.TraceInformation(
                "Component Update \"{0}\" {1} to time {2}",
                Caption, TraceOutputs(requiredOutput), earliestConsumerTimeRequestEnd.ToString());

            // Non temporal consumers might want to update after every engine update
            bool updateOnceOnly = hasNonTemporalConsumers 
                || double.IsPositiveInfinity(earliestConsumerTimeRequestEnd)
                || consumerTimeCount == 0;

            double engineTime = ((IEngineTime)Engine).GetCurrentTime();

            if (updateOnceOnly)
            {
                Trace.TraceInformation("Single engine update only");
                
                UpdateEngine();

                engineTime = ((IEngineTime)Engine).GetCurrentTime();
            }
            else
            {
                // Update until first consumer can use a new values

                while (earliestConsumerTimeRequestEnd > engineTime)
                {
                    UpdateEngine();

                    engineTime = ((IEngineTime)Engine).GetCurrentTime();
                }
            }

            ITime upto = new Time(earliestConsumerTimeRequestStart);

            if (upto != null)
                _activeOutputs.ForEach(o => 
                    ((IHasValueSetConvertor)o).ValueSetConverter.EmptyCaches(upto));
        }

        string TraceOutputs(IEnumerable<IBaseOutput> requiredOutput)
        {
            return requiredOutput
                .Aggregate(new StringBuilder(), (sb, d) => sb.Append(string.Format("\"{0}\",", d.Caption)))
                .ToString()
                .TrimEnd(',');
        }

        //private readonly object syncLock = new object();
        protected virtual void UpdateEngine()
        {
            // synchronize access to this method to ensure serial execution
            // in case it is called in parallel
            //lock (syncLock)
            {

                // Update engine with values from input exchange item 

                var engineTime = new Time(((IEngineTime)Engine).GetCurrentTime());

                // For active time based inputs set required time and get values
                
                // parallel
                Dictionary<ITimeSpaceExchangeItem, IBaseValueSet> valueSets = new Dictionary<ITimeSpaceExchangeItem, IBaseValueSet>();
                List<System.Threading.Thread> threadList = new List<System.Threading.Thread>();
                _activeInputs.ForEach(a =>
                {
                    var thread = new System.Threading.Thread(delegate()
                    {
                        var timeInput = a as ITimeSpaceExchangeItem;

                        // If not a time input just get next set of values

                        if (timeInput != null)
                        {
                            // change to request inputs at timestep being calculated
                            timeInput.TimeSet.Times.Clear();
                            //timeInput.TimeSet.Times.Add(engineTime);
                            timeInput.TimeSet.Times.Add(new Time(engineTime.StampAsModifiedJulianDay + 1));
                        }

                        var values = a.Values as IBaseValueSet;

                        valueSets[timeInput] = values;

                    });
                    thread.IsBackground = true;
                    thread.Start();
                    threadList.Add(thread);
                });
                foreach (System.Threading.Thread thread in threadList)
                {
                    thread.Join();
                }
                foreach (ITimeSpaceExchangeItem nextExchangeItem in valueSets.Keys)
                {
                    IBaseValueSet nextValueSet = valueSets[nextExchangeItem];
                    EngineConvertor(nextExchangeItem).ToEngine(Engine, nextValueSet);
                }

                /*
                // serial
                _activeInputs.ForEach(a =>
                    {
                        var timeInput = a as ITimeSpaceExchangeItem;

                        // If not a time input just get next set of values

                        if (timeInput != null)
                        {
                            // change to request inputs at timestep being calculated
                            timeInput.TimeSet.Times.Clear();
                            //timeInput.TimeSet.Times.Add(engineTime);
                            timeInput.TimeSet.Times.Add(new Time(engineTime.StampAsModifiedJulianDay + 1));
                        }

                        var values = a.Values as IBaseValueSet;

                        EngineConvertor(a).ToEngine(Engine, values);
                    });
                */

                Engine.Update();

                var updatedEngineTime = new Time(((IEngineTime)Engine).GetCurrentTime());

                if (updatedEngineTime == null)
                    throw new Exception("Engine current time is null");

                if (updatedEngineTime.StampAsModifiedJulianDay <= engineTime.StampAsModifiedJulianDay)
                    throw new Exception("Engine update failed to progress in time. Failed at " + updatedEngineTime.ToString());

                // Update output cache with values from engine
                // Update all not just required, as could improve interpolations

                _activeOutputs.ForEach(o =>
                    EngineConvertor(o).CacheEngineValues(Engine));

                _timeExtent.Times.Add(updatedEngineTime);

                _engineDone = updatedEngineTime.StampAsModifiedJulianDay >=
                    Time.EndTime(_timeExtent.TimeHorizon);

                Trace.TraceInformation(string.Format("Completed \"{0}\" engine update: {1} => {2}, [{3}]",
                    Caption,
                    Time.ToDateTimeString(engineTime.StampAsModifiedJulianDay),
                    Time.ToDateTimeString(updatedEngineTime.StampAsModifiedJulianDay),
                    new TimeInterval(updatedEngineTime.StampAsModifiedJulianDay - engineTime.StampAsModifiedJulianDay).ToString()));
            }
        }

        protected override bool IsDone()
        {
            return _engineDone;
        }

        #region ITimeExtension Members

        public ITimeSet TimeExtent
        {
            get { return _timeExtent; }
        }

        #endregion

        protected override IEngine NewRemotingEngine()
        {
            return new RemotingClientEngineTime();
        }

        public new const string XName = "BaseComponentTimeWithEngine";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _timeExtent = Persistence.TimeSet.Parse(xElement, accessor);

            _engineDone = false;
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.TimeSet.Persist(_timeExtent, accessor),
                base.Persist(accessor));
        }
    }
}
