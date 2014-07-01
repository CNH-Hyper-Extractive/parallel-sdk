using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using ILinkableComponentVersion1 = OpenMI.Standard.ILinkableComponent;

namespace FluidEarth2.Sdk
{
    public class LinkableComponentV1OpenMI : LinkableComponentV1Base
    {
        int _nInputs = 0;
        int _nOutputs = 0;

        public LinkableComponentV1OpenMI()
            : base(new Identity(new Describes("OpenMIv1", "Converts a OpenMI V1 component V1 to OpenMI V2")),
                new ExternalType(typeof(LinkableComponentV1OpenMI)),
                new ExternalType(typeof(EngineProxy)),
                false)
        {
            Arguments.Add(new ArgumentTimeInterval(GetArgumentIdentity(ArgsOmi.UpdateTimeInterval),
                new TimeInterval()));
        }

        public override void Prepare()
        {
            var updateTimeInterval = ArgumentUpdateTimeInterval;

            var horizon = ArgumentTimeHorizon;

            if (double.IsNegativeInfinity(horizon.StampAsModifiedJulianDay)
                || double.IsPositiveInfinity(horizon.StampAsModifiedJulianDay))
                throw new Exception("Invalid value for argument "
                    + GetArgumentIdentity(BaseComponentTimeWithEngine.ArgsWithEngineTime.TimeHorizon));

            var activeInputs = Inputs
                .Where(i => i.Provider != null)
                .ToList();

            var activeOutputs = Outputs
                .Where(o => o.Consumers.Count > 0)
                .ToList();

            OpenMI.Standard.ILinkableComponent component1;
            var reports = new List<IReport>();

            if (!InstantiateComponent1(ArgumentV1Omi, out component1, reports))
                throw new Exception("Component V1 instantiation", null, reports);

            ((EngineProxy)Engine).PrePrepare(
                this,
                component1,
                horizon.StampAsModifiedJulianDay,
                updateTimeInterval.TotalDays,
                activeInputs, activeOutputs);

            base.Prepare();
        }

        public enum ArgsOmi
        {
            UpdateTimeInterval = 0,
        }

        public static IIdentifiable GetArgumentIdentity(ArgsOmi key)
        {
            switch (key)
            {
                case ArgsOmi.UpdateTimeInterval:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentV1OpenMI." + key.ToString(),
                        "UpdateTimeIncrement",
                        "To provide an automated convertor for a version 1 OpenMI component"
                        + " the user must provide a fixed update time increment."
                        + " The v1 component will update at these fixed time intervals"
                        + " interpolating/extrapolating as necessary. The v2 component wrapper will"
                        + " cache these returned values for providing to other v2 components,"
                        + " interpolating as required."
                        + " Value must be > 0 and finite.");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public TimeInterval ArgumentUpdateTimeInterval
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(ArgsOmi.UpdateTimeInterval))
                    .Value as ArgumentValueTimeInterval;

                return argValue.Value;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(ArgsOmi.UpdateTimeInterval))
                    .Value as ArgumentValueTimeInterval;

                argValue.Value = value;
            }
        }

        protected override ValidArgumentMessage ValidArgumentValue(IArgument arg, out string message)
        {
            try
            {
                message = string.Empty;

                if (arg.Id == GetArgumentIdentity(ArgsOmi.UpdateTimeInterval).Id)
                {
                    var inc = ArgumentUpdateTimeInterval;

                    double value = inc.TotalDays;

                    if (double.IsInfinity(value)
                        || double.IsNaN(value)
                        || value <= 0.0)
                        throw new Exception("Value must be > 0 and finite");

                    return ValidArgumentMessage.OK;
                }
            }
            catch (Exception e)
            {
                message = string.Format(
                    "# Argument \"{0}\" not set to a valid value: \"{1}\"" +
                    "\r\n* {2}",
                    arg.Caption, arg.ValueAsString, e.Message);

                return ValidArgumentMessage.Error;
            }

            return base.ValidArgumentValue(arg, out message);
        }

        protected override IBaseOutput Convert(OpenMI.Standard.IOutputExchangeItem item1)
        {
            var engineVariable = string.Format("Output{0}.{1}.{2}",
                ++_nOutputs, item1.Quantity.ID, item1.ElementSet.ID);

            var describes = new Describes(engineVariable,
                string.Format("{0}\r\n\r\n{1}", item1.Quantity.Description, item1.ElementSet.Description));

            var elementSet = new ElementSetUnoptimisedStorage(item1.ElementSet);

            return new OutputSpaceTimeComponent1(
                new Identity(engineVariable, describes),
                this,
                new Quantity(item1.Quantity, typeof(double), -999.999),
                elementSet,
                engineVariable, -999.999, elementSet.ElementCount);
        }

        protected override IBaseInput Convert(OpenMI.Standard.IInputExchangeItem item1)
        {
            var engineVariable = string.Format("Input{0}.{1}.{2}",
                ++_nInputs, item1.Quantity.ID, item1.ElementSet.ID);

            var describes = new Describes(engineVariable,
                string.Format("{0}\r\n\r\n{1}", item1.Quantity.Description, item1.ElementSet.Description));

            var elementSet = new ElementSetUnoptimisedStorage(item1.ElementSet);

            return new InputSpaceTimeComponent1(
                new Identity(engineVariable, describes),
                this,
                new Quantity(item1.Quantity, typeof(double), -999.999),
                elementSet,
                engineVariable, -999.999, elementSet.ElementCount);
        }

        internal class OutputSpaceTimeComponent1 : OutputSpaceTime
        {
            public OutputSpaceTimeComponent1()
            { }

            public OutputSpaceTimeComponent1(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleStandard1(engineVariable, missingValue, elementCount))
            { }

            public OutputSpaceTimeComponent1(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3dStandard1(engineVariable, missingValue, elementCount))
            { }

            /// <summary>
            /// Overridden to call convertor.GetValues(...) directly without any calls to Engine.Update()
            /// </summary>
            /// <param name="querySpecifier"></param>
            /// <returns></returns>
            protected override ITimeSpaceValueSet GetValuesTimeImplementation(IBaseExchangeItem querySpecifier)
            {
                var convertor = ValueSetConverter as IValueSetConverterTime;

                if (convertor == null)
                    throw new Exception("ValueSetConverter not a IValueSetConverterTime");

                // Non OpenMI special case, handy for checking current values without changing engine state

                if (querySpecifier == null)
                    convertor.GetValueSetLatest();

                if (querySpecifier is IBaseInput && !Consumers.Any(c => c.Id == querySpecifier.Id))
                    throw new Exception("GetValues request from an unregistered Input Item: " + querySpecifier.Id);
                else if (querySpecifier is IBaseOutput && !AdaptedOutputs.Any(c => c.Id == querySpecifier.Id))
                    throw new Exception("GetValues request from an unregistered Adapted Output Item: " + querySpecifier.Id);

                if (querySpecifier is IBaseOutput)
                    throw new NotImplementedException();

                if (!(querySpecifier is ITimeSpaceExchangeItem))
                    return convertor.GetValueSetLatest() as ITimeSpaceValueSet;

                ITimeSet requestTimeSet = ((ITimeSpaceExchangeItem)querySpecifier).TimeSet;

                return convertor.GetValueSetAt(requestTimeSet) as ITimeSpaceValueSet;
            }
        }

        internal class InputSpaceTimeComponent1 : InputSpaceTime
        {
            public InputSpaceTimeComponent1()
            { }

            public InputSpaceTimeComponent1(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleStandard1(engineVariable, missingValue, elementCount))
            { }

            public InputSpaceTimeComponent1(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3dStandard1(engineVariable, missingValue, elementCount))
            { }
        }

        internal class EngineProxy : IEngineTime
        {
            ILinkableComponentVersion1 _component1;
            double _currentTime;
            double _updateLinkTimeIncrement;

            List<OpenMI.Standard.ILink> _linksIn
                = new List<OpenMI.Standard.ILink>();
            List<OpenMI.Standard.ILink> _linksOut
                = new List<OpenMI.Standard.ILink>();

            public ILinkableComponentVersion1 Component1
            {
                get { return _component1; }
                set { _component1 = value; }
            }

            public double GetCurrentTime()
            {
                return _currentTime;
            }

            public string Ping()
            {
                return "42";
            }

            public virtual void Initialise(string initialisingXml)
            {
                Initialise(initialisingXml, null);
            }

            public void Initialise(string initialisingXml, IDocumentAccessor accessor)
            {
            }

            public void SetArgument(string key, string value)
            {
                Trace.TraceInformation("Unused Component1.Proxy.SetArgument({0},{1})",
                    key, value);
            }

            public void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
            {
                Trace.TraceInformation("Unused Component1.Proxy.SetInput({0},{1},{2},{3})",
                    engineVariable, elementCount, elementValueCount, vectorLength);
            }

            public void SetInput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
            {
                Trace.TraceInformation("Unused Component1.Proxy.SetInput({0},{1},...,{2})",
                    engineVariable, elementCount, vectorLength);
            }

            public void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
            {
                Trace.TraceInformation("Unused Component1.Proxy.SetOutput({0},{1},{2},{3})",
                    engineVariable, elementCount, elementValueCount, vectorLength);
            }

            public void SetOutput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
            {
                Trace.TraceInformation("Unused Component1.Proxy.SetOutput({0},{1},...,{2})",
                    engineVariable, elementCount, vectorLength);
            }

            public void PrePrepare(
                LinkableComponentV1OpenMI component2,
                OpenMI.Standard.ILinkableComponent component1,
                double startTime, double updateLinkTimeIncrement,
                List<IBaseInput> activeInputs, List<IBaseOutput> activeOutputs)
            {
                Contract.Requires(component1 != null, "component1 != null");
                Contract.Requires(component2 != null, "component2 != null");

                Component1 = component1;

                Component1.Initialize(component2.Arguments.Select(a => new Utilities.Standard1.Argument1(a)).ToArray());

                _currentTime = startTime;
                _updateLinkTimeIncrement = updateLinkTimeIncrement;

                // Create internal links between components 1 and 2
                // T2 --> T1
                // S1 --> S2

                Utilities.Standard1.TimeSpan timeHorizon1
                    = new Utilities.Standard1.TimeSpan(component2.TimeExtent.TimeHorizon);

                OpenMI.Standard.IInputExchangeItem input;
                OpenMI.Standard.IOutputExchangeItem output;
                OpenMI.Standard.ILink internalLink;

                ITimeSpaceExchangeItem itemTimeSpace;

                string idQuantity, idElementSet;

                activeInputs.ForEach(i =>
                {
                    idQuantity = i.ValueDefinition.Caption;

                    itemTimeSpace = i as ITimeSpaceExchangeItem;
                    idElementSet = itemTimeSpace == null
                        ? i.Caption // what else?
                        : itemTimeSpace.SpatialDefinition.Caption;

                    internalLink = null;

                    for (int n = 0; n < _component1.InputExchangeItemCount; ++n)
                    {
                        input = _component1.GetInputExchangeItem(n);

                        if (input.Quantity.ID != idQuantity
                            || input.ElementSet.ID != idElementSet)
                            continue;

                        var isVector = input.Quantity.ValueType == OpenMI.Standard.ValueType.Vector;

                        // Get providers convertor to use directly

                        Contract.Requires(i.Provider is IHasValueSetConvertor,
                            "i.Provider is IHasValueSetConvertor");

                        var convertorProvider = ((IHasValueSetConvertor)i.Provider).ValueSetConverter
                            as IValueSetConverterTime;

                        Contract.Requires(convertorProvider != null,
                            "i.Provider.ValueSetConverter is IValueSetConverterTime");

                        internalLink = new Utilities.Standard1.InternalLink(
                            _component1, input, timeHorizon1, convertorProvider, isVector);

                        _linksIn.Add(internalLink);

                        _component1.AddLink(internalLink);

                        break;
                    }

                    if (internalLink == null)
                        throw new Exception(string.Format(
                            "OpenMI.Standard.IOutputExchangeItem not found with Quality,ElementSet ids if \"{0}\",\"{1}\"",
                            idQuantity, idElementSet));
                });

                activeOutputs.ForEach(o =>
                {
                    idQuantity = o.ValueDefinition.Caption;

                    itemTimeSpace = o as ITimeSpaceExchangeItem;
                    idElementSet = itemTimeSpace == null
                        ? o.Caption // what else?
                        : itemTimeSpace.SpatialDefinition.Caption;

                    internalLink = null;

                    for (int n = 0; n < _component1.OutputExchangeItemCount; ++n)
                    {
                        output = _component1.GetOutputExchangeItem(n);

                        if (output.Quantity.ID != idQuantity
                            || output.ElementSet.ID != idElementSet)
                            continue;

                        internalLink = new Utilities.Standard1.InternalLink(_component1, output, timeHorizon1);

                        _linksOut.Add(internalLink);

                        _component1.AddLink(internalLink);

                        if (((IHasValueSetConvertor)o).ValueSetConverter is ValueSetConverterTimeEngineDoubleStandard1)
                        {
                            var convertor = (ValueSetConverterTimeEngineDoubleStandard1)((IHasValueSetConvertor)o).ValueSetConverter;

                            convertor.SetRuntime(this, internalLink);
                        }
                        else if (((IHasValueSetConvertor)o).ValueSetConverter is ValueSetConverterTimeEngineDoubleVector3dStandard1)
                        {
                            var convertor = (ValueSetConverterTimeEngineDoubleVector3dStandard1)((IHasValueSetConvertor)o).ValueSetConverter;

                            convertor.SetRuntime(this, internalLink);
                        }
                        else
                            throw new Exception("o.ValueSetConverter as ValueSetConverterTimeEngineDouble?");

                        break;
                    }

                    if (internalLink == null)
                        throw new Exception(string.Format(
                            "OpenMI.Standard.IInputExchangeItem not found with Quality,ElementSet ids if \"{0}\",\"{1}\"",
                            idQuantity, idElementSet));
                });

                if (_linksOut.Count < 1)
                    throw new Exception("At least one output link must be specified");
            }

            public void Prepare()
            {
                _component1.Prepare();
            }

            public void SetStrings(string engineVariable, string missingValue, string[] values)
            {
                throw new NotImplementedException();
            }

            public void SetInt32s(string engineVariable, int missingValue, int[] values)
            {
                throw new NotImplementedException();
            }

            public void SetDoubles(string engineVariable, double missingValue, double[] values)
            {
                throw new NotImplementedException();
            }

            public void SetBooleans(string engineVariable, bool missingValue, bool[] values)
            {
                throw new NotImplementedException();
            }

            public OpenMI.Standard.IValueSet GetComponent1Values(double at, string linkId)
            {
                OpenMI.Standard.IValueSet vs;

                if (_currentTime >= at)
                {
                    try
                    {
                        Trace.TraceInformation("Component1.GetValues(...), call into past",

                        vs = _component1.GetValues(
                            new Utilities.Standard1.TimeStamp(at), linkId));

                        Trace.TraceInformation("Component1.GetValues({0},{1}) =\r\n\t{2}",
                            at.ToString(), linkId,
                            Utilities.Standard1.ToString(vs));

                        return vs;
                    }
                    catch (System.Exception e)
                    {
                        throw new Exception(string.Format("Component1.GetValues({0},{1})",
                            at.ToString(), linkId), e);
                    }
                }

                if (_updateLinkTimeIncrement <= 0.0)
                    throw new Exception("_updateLinkTimeIncrement <= 0.0");

                try
                {
                    while (_currentTime < at)
                    {
                        _currentTime += _updateLinkTimeIncrement;

                        if (_currentTime > at)
                            break;

                        vs = _component1.GetValues(
                            new Utilities.Standard1.TimeStamp(_currentTime), linkId);

                        Trace.TraceInformation("SubStep Component1.GetValues({0},{1}) =\r\n\t{2}",
                            _currentTime.ToString(), linkId,
                            Utilities.Standard1.ToString(vs));
                    }

                    vs = _component1.GetValues(
                        new Utilities.Standard1.TimeStamp(at), linkId);

                    Trace.TraceInformation("return  Component1.GetValues({0},{1}) =\r\n\t{2}",
                        at.ToString(), linkId,
                        Utilities.Standard1.ToString(vs));

                    return vs;
                }
                catch (System.Exception e)
                {
                    throw new Exception(string.Format("Component1.GetValues({0},{1})",
                        _currentTime.ToString(), linkId), e);
                }
            }

            public void Update()
            {
                // Did you use OutputSpaceTimeComponent1 ?
                throw new NotImplementedException();
            }

            public string[] GetStrings(string engineVariable, string missingValue)
            {
                throw new NotImplementedException();
            }

            public int[] GetInt32s(string engineVariable, int missingValue)
            {
                throw new NotImplementedException();
            }

            public double[] GetDoubles(string engineVariable, double missingValue)
            {
                throw new NotImplementedException();
            }

            public bool[] GetBooleans(string engineVariable, bool missingValue)
            {
                throw new NotImplementedException();
            }

            public void Finish()
            {
                _component1.Finish();
            }

            public void Dispose()
            {
                _component1.Dispose();
            }
        }
    }
}
