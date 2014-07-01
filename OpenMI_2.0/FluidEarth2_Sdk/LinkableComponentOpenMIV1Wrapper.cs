
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using ILinkableComponentVersion1 = OpenMI.Standard.ILinkableComponent;

namespace FluidEarth2.Sdk
{
    public class LinkableComponentOpenMIV1Wrapper : BaseComponentTimeWithEngine
    {
        bool _convertingFromV1 = false;
        protected bool _includeUpdateTimeInterval = true;
        IDocumentAccessor _accessor;

        public enum ArgsV1Wrapper
        {
            Persistence = 0,
            UpdateTimeInterval,
            EngineExternalType,
        }

        public static IIdentifiable GetArgumentIdentity(ArgsV1Wrapper key)
        {
            switch (key)
            {
                case ArgsV1Wrapper.Persistence:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentOpenMIV1Wrapper." + key.ToString(),
                        "Persistence",
                        "Where components state is cached (XML)");
                case ArgsV1Wrapper.UpdateTimeInterval:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentOpenMIV1Wrapper." + key.ToString(),
                        "UpdateTimeIncrement",
                        "To provide an automated convertor for a version 1 OpenMI component"
                        + " the user must provide a fixed update time increment."
                        + " The v1 component will update at these fixed time intervals"
                        + " interpolating/extrapolating as necessary. The v2 component wrapper will"
                        + " cache these returned values for providing to other v2 components,"
                        + " interpolating as required."
                        + " Value must be > 0 and finite.");
                case ArgsV1Wrapper.EngineExternalType:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentOpenMIV1Wrapper." + key.ToString(),
                        "EngineExternalType",
                        "External type details for instantiating an original version 1 OpenMI.Standard.ILinkableComponent");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public IDocumentAccessor Accessor
        {
            set { _accessor = value; }
        }

        protected override ValidArgumentMessage ValidArgumentValue(IArgument arg, out string message)
        {
            try
            {
                message = string.Empty;

                if (_includeUpdateTimeInterval && arg.Id == GetArgumentIdentity(ArgsV1Wrapper.UpdateTimeInterval).Id)
                {
                    var inc = ArgumentUpdateTimeInterval;
                                              
                    double value = inc.TotalDays;

                    if (double.IsInfinity(value)
                        || double.IsNaN(value)
                        || value <= 0.0)
                        throw new Exception("Value must be > 0 and finite");

                    return ValidArgumentMessage.OK;
                }
                else if (arg.Id == GetArgumentIdentity(ArgsV1Wrapper.EngineExternalType).Id)
                {
                    var xType = ArgumentEngineExternalType;
                    var uri = new Uri(xType.AssemblyName.CodeBase);

                    string filename;

                    if (Utilities.UriIsFilePath(uri, out filename))
                    {
                        if (!File.Exists(filename))
                        {
                            message = string.Format("Cannot locate assembly {0} with codebase \"{1}\"",
                                xType.AssemblyName.FullName, filename);

                            return ValidArgumentMessage.Warning;
                        }
                    }

                    return ValidArgumentMessage.OK;
                }
                else if (arg.Id == GetArgumentIdentity(ArgsV1Wrapper.Persistence).Id)
                {
                    var uri = ArgumentPersistence;

                    string filename = uri.LocalPath;

                    if (!File.Exists(filename))
                        throw new FileNotFoundException(filename);

                    return ValidArgumentMessage.OK;
                }
            }
            catch (Exception e)
            {
                message = string.Format(
                    "# Argument \"{0}\" not set to a valid double, value = \"{1}\"" +
                    "\r\n* {2}",
                    arg.Caption, arg.ValueAsString, e.Message);

                return ValidArgumentMessage.Error;
            }

            return base.ValidArgumentValue(arg, out message);
        }

        public LinkableComponentOpenMIV1Wrapper()
            : base(new Identity("FluidEarth2.Sdk.LinkableComponentOpenMIV1Wrapper",
                    "OpenMI V1 Import", string.Empty),
                new ExternalType(typeof(LinkableComponentOpenMIV1Wrapper)),
                new ExternalType(typeof(EngineProxy)))
        {
            InitialiseArguments(null, null);
        }

        public LinkableComponentOpenMIV1Wrapper(ILinkableComponentVersion1 component1, 
            IDocumentAccessor accessor,
            List<Utilities.Standard1.Argument1> args1)
            : base(new Identity(component1.ComponentID,
                    component1.ModelID + " [OpenMIv1]", component1.ComponentDescription),
                new ExternalType(typeof(LinkableComponentOpenMIV1Wrapper)),
                new ExternalType(typeof(EngineProxy)))
        {
            Description += "\r\nConverted from OpenMI Standard 1";

            DocumentAccessor = accessor;

            var uriPersistence = InitialiseArguments(component1, args1);

            ConstuctComponent(component1, uriPersistence, args1);
        }

        protected LinkableComponentOpenMIV1Wrapper(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType, bool useNativeDllArgument)
            : base (identity, derivedComponentType, engineType, useNativeDllArgument)
        {     
        }

        protected virtual ArgumentUri InitialiseArguments(ILinkableComponentVersion1 component1, List<Utilities.Standard1.Argument1> args1)
        {
            if (component1 == null)
            {
                var uri = new Uri(@"http://sourceforge.net/projects/fluidearth/");
                var persistence = new ArgumentUri(GetArgumentIdentity(ArgsV1Wrapper.Persistence), uri);

                // For Uri's have to use a valid Url to initialise, then change to relevant value
                Arguments.Add(persistence);
                Arguments.Add(new ArgumentExternalType(GetArgumentIdentity(ArgsV1Wrapper.EngineExternalType),
                    new ExternalType(DocumentAccessor)));

                if (_includeUpdateTimeInterval)
                    Arguments.Add(new ArgumentTimeInterval(GetArgumentIdentity(ArgsV1Wrapper.UpdateTimeInterval),
                        new TimeInterval()));

                if (args1 != null)
                    foreach (var a1 in args1)
                        Arguments.Add(new ArgumentStandard1(a1));

                return persistence;
            }

            ArgumentTimeHorizon = new Time(
                component1.TimeHorizon.Start.ModifiedJulianDay,
                component1.TimeHorizon.End.ModifiedJulianDay);

            ExternalType component1Type = new ExternalType(DocumentAccessor);
            component1Type.Initialise(component1.GetType());

            var uriP = new Uri(
                DocumentAccessor.Uri
                    .LocalPath
                    .Substring(0, DocumentAccessor.Uri.LocalPath.LastIndexOf('.')) + Caption + "Persist.xml");

            var uriPersistence = new ArgumentUri(GetArgumentIdentity(ArgsV1Wrapper.Persistence), uriP);

            uriPersistence.ValueAsString = 
                new Uri(
                    DocumentAccessor.Uri
                        .LocalPath
                        .Substring(0, DocumentAccessor.Uri.LocalPath.LastIndexOf('.')) + Caption + "Persist.xml")
                .AbsoluteUri;

            Arguments.Add(uriPersistence);
            Arguments.Add(new ArgumentExternalType(GetArgumentIdentity(ArgsV1Wrapper.EngineExternalType),
                component1Type));

            if (_includeUpdateTimeInterval)
                Arguments.Add(new ArgumentTimeInterval(GetArgumentIdentity(ArgsV1Wrapper.UpdateTimeInterval),
                    new TimeInterval()));

            foreach (var a1 in args1)
                Arguments.Add(new ArgumentStandard1(a1));

            return uriPersistence;
        }

        public TimeInterval ArgumentUpdateTimeInterval
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.UpdateTimeInterval))
                    .Value as ArgumentValueTimeInterval;

                return argValue.Value;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.UpdateTimeInterval))
                    .Value as ArgumentValueTimeInterval;

                argValue.Value = value;
            }
        }

        public IExternalType ArgumentEngineExternalType
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.EngineExternalType))
                    .Value as ArgumentValueExternalType;

                return argValue.Value;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.EngineExternalType))
                    .Value as ArgumentValueExternalType;

                argValue.Value = value;
            }
        }

        public Uri ArgumentPersistence
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.Persistence))
                    .Value as ArgumentValueUri;

                return argValue.Value;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(ArgsV1Wrapper.Persistence))
                    .Value as ArgumentValueUri;

                argValue.Value = value;
            }
        }

        protected void ConstuctComponent(ILinkableComponentVersion1 component1,
            ArgumentUri uriPersistence,
            List<Utilities.Standard1.Argument1> args1)
        {
            try
            {
                _convertingFromV1 = true;

                Construct(component1, args1);

                Initialize();

                var uri = (uriPersistence.Value as ArgumentValueUri).Value;

                var xPersistence = new XDocument(Persist(DocumentAccessor));
                xPersistence.Save(uri.LocalPath);
            }
            finally
            {
                _convertingFromV1 = false;
            }
        }

        void Construct(ILinkableComponentVersion1 component1, List<Utilities.Standard1.Argument1> args1)
        {
            Utilities.Standard1.Argument1 argModelResource = args1
                .Where(a => a.Key == "FluidEarth_SDK.ArgResource.Model")
                .SingleOrDefault();

            if (argModelResource != null)
            {
                // If necessary can automate this
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Model file is an Embedded Resources, need to unpack");
                sb.AppendLine("On command line, run component assembly with argument \"unpack\" e.g.");
                sb.AppendLine(">FluidEarth_Example_TankCs.exe unpack");
                sb.AppendLine("Then REMOVE omi argument FluidEarth_SDK.ArgResource.Model and REPLACE with");
                sb.AppendLine("FluidEarth_SDK.ArgFile.Model");
                sb.AppendLine("with value set to the unpacked model file");

                throw new Exception(sb.ToString());
            }

            Utilities.Standard1.Argument1 argModelFile = args1
                .Where(a => a.Key == "FluidEarth_SDK.ArgFile.Model")
                .SingleOrDefault();

            if (argModelFile == null) // further back in history?
                argModelFile = args1
                .Where(a => a.Key == "OpenWEB_SDK.ArgFile.Model")
                .SingleOrDefault();

            Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> v1ModelXmlInputs = null;
            Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> v1ModelXmlOutputs = null;

            if (argModelFile != null)
            {
                XDocument doc = XDocument.Load(argModelFile.Value);
                XElement xModel = doc.Root;

                Utilities.Standard1.ReadModelFile(xModel, out v1ModelXmlInputs, out v1ModelXmlOutputs);
            }

            ConstructExchangeItems(component1, v1ModelXmlInputs, v1ModelXmlOutputs);

            string captionSpatialDefintion, captionValueDefintion;
            ITimeSpaceExchangeItem spatial;

            if (v1ModelXmlInputs != null)
            {
                foreach (Utilities.Standard1.ExchangeItemV1ModelXml e in v1ModelXmlInputs.Values)
                {
                    captionValueDefintion = e.Quantity.Id;
                    captionSpatialDefintion = e.ElementSet.Id;

                    foreach (IBaseInput input in _inputs)
                    {
                        if (input.ValueDefinition.Caption != captionValueDefintion)
                            continue;

                        spatial = input as ITimeSpaceExchangeItem;

                        if (spatial == null)
                            continue;

                        //if (spatial.SpatialDefinition.Caption == captionSpatialDefintion)
                        //    ((InputSpaceTime)input).AddUserVariables(e.UserVariables);
                    }
                }
            }

            if (v1ModelXmlOutputs != null)
            {
                foreach (Utilities.Standard1.ExchangeItemV1ModelXml e in v1ModelXmlOutputs.Values)
                {
                    captionValueDefintion = e.Quantity.Id;
                    captionSpatialDefintion = e.ElementSet.Id;

                    foreach (IBaseOutput output in _outputs)
                    {
                        if (output.ValueDefinition.Caption != captionValueDefintion)
                            continue;

                        spatial = output as ITimeSpaceExchangeItem;

                        if (spatial == null)
                            continue;

                        //if (spatial.SpatialDefinition.Caption == captionSpatialDefintion)
                        //    ((OutputSpaceTime)output).AddUserVariables(e.UserVariables);
                    }
                }
            }
        }

        IEnumerable<OpenMI.Standard.IArgument> Arguments1
        {
            get
            {
                return Arguments
                    .Where(a => a is OpenMI.Standard.IArgument)
                    .Cast<OpenMI.Standard.IArgument>();
            }
        }

        protected override void DoInitialise(bool reinitialising)
        {
            base.DoInitialise(reinitialising);

            var uri = ArgumentPersistence;

            if (uri == null)
                throw new Exception(string.Format(
                    "Must set argument {0} to a valid URI for persisting component prior to calling Initialize()",
                    ArgsV1Wrapper.Persistence.ToString()));

            if (!File.Exists(uri.LocalPath) && !_convertingFromV1)
                throw new FileNotFoundException(uri.LocalPath);
            else if (!_convertingFromV1 && !Reinitialising)
            {
                var xDocument = XDocument.Load(uri.LocalPath);
                Initialise(xDocument.Root, _accessor);
            }
        }

        public static string EngineVariable(string linkId, string quantityId, string elementSetId)
        {
            return string.Format("{0}.{1}.{2}", linkId, quantityId, elementSetId);
        }

        void ConstructExchangeItems(ILinkableComponentVersion1 component1, 
            Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> v1ModelXmlInputs,
            Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> v1ModelXmlOutputs)
        {
            List<BaseInput> convertedInputs = new List<BaseInput>();
            List<BaseOutput> convertedOutputs = new List<BaseOutput>();

            OpenMI.Standard.IInputExchangeItem input1;
            string engineVariable, description;
            Utilities.Standard1.ExchangeItemV1ModelXml v1Model;

            for (int n = 0; n < component1.InputExchangeItemCount; ++n)
            {
                input1 = component1.GetInputExchangeItem(n);

                v1Model = v1ModelXmlInputs
                    .Values
                    .Where(v => 
                        v.Quantity.Id == input1.Quantity.ID
                        && v.ElementSet.Id == input1.ElementSet.ID)
                    .SingleOrDefault();

                engineVariable = EngineVariable(
                    string.Format("Input{0}", n), 
                    input1.Quantity.ID, input1.ElementSet.ID);

                description = string.Format("{0}\r\n{1}",
                    input1.Quantity.Description, input1.ElementSet.Description);

                convertedInputs.Add(
                    NewInputSpaceTime(engineVariable, description,
                        input1, v1Model, input1.ElementSet.ElementCount));
            }

            OpenMI.Standard.IOutputExchangeItem output1;

            for (int n = 0; n < component1.OutputExchangeItemCount; ++n)
            {
                output1 = component1.GetOutputExchangeItem(n);

                v1Model = v1ModelXmlOutputs
                    .Values
                    .Where(v =>
                        v.Quantity.Id == output1.Quantity.ID
                        && v.ElementSet.Id == output1.ElementSet.ID)
                    .SingleOrDefault();

                engineVariable = EngineVariable(
                    string.Format("Output{0}", n),
                    output1.Quantity.ID, output1.ElementSet.ID);

                description = string.Format("{0}\r\n{1}",
                    output1.Quantity.Description, output1.ElementSet.Description);

                convertedOutputs.Add(
                    NewOutputSpaceTime(engineVariable, description,
                        output1, v1Model, output1.ElementSet.ElementCount));
            }

            AddRange(convertedInputs);
            AddRange(convertedOutputs);
        }

        protected virtual BaseOutput NewOutputSpaceTime(string engineVariable, string description,
            OpenMI.Standard.IOutputExchangeItem output1, 
            Utilities.Standard1.ExchangeItemV1ModelXml v1Model, int elementCount)
        {
            return new OutputSpaceTimeComponent1(
                new Identity(engineVariable, engineVariable, description),
                this,
                new Quantity(output1.Quantity, typeof(double), -999.999),
                ConvertElementSet(output1.ElementSet, v1Model),
                engineVariable, -999.999, elementCount);
        }

        protected virtual BaseInput NewInputSpaceTime(string engineVariable, string description, 
            OpenMI.Standard.IInputExchangeItem input1,
            Utilities.Standard1.ExchangeItemV1ModelXml v1Model, int elementCount)
        {
            return new InputSpaceTimeComponent1(
                new Identity(engineVariable, engineVariable, description),
                this,
                new Quantity(input1.Quantity, typeof(double), -999.999),
                ConvertElementSet(input1.ElementSet, v1Model),
                engineVariable, -999.999, elementCount);
        }

        protected IElementSet ConvertElementSet(OpenMI.Standard.IElementSet elementSet1,
            Utilities.Standard1.ExchangeItemV1ModelXml v1Model)
        {
            if (v1Model != null)
            {
                if (elementSet1.GetType().ToString() == "FluidEarth.Sdk.ElementSetGridRegular"
                    && v1Model.UserVariables["ElementType"] == "XYPoint"
                    && v1Model.UserVariables["Storage"] == "RegularGrid")
                {
                    var cellCountX = int.Parse(v1Model.UserVariables["N"]) - 1;
                    var cellCountY = int.Parse(v1Model.UserVariables["M"]) - 1;
                    var originX = double.Parse(v1Model.UserVariables["OriginX"]);
                    var originY = double.Parse(v1Model.UserVariables["OriginY"]);
                    var deltaX = double.Parse(v1Model.UserVariables["dX"]);
                    var deltaY = double.Parse(v1Model.UserVariables["dY"]);

                    var parameters = new ParametersGridRegular(
                        cellCountX, cellCountY, 
                        new Coord2d(originX, originY), 
                        deltaX, deltaY);

                    bool fastN = v1Model.UserVariables["Packing"] == "FastN";

                    return new ElementSetGridRegularPoints(parameters, ElementSetGridRegularPoints.Located.Node, fastN);
                }
            }

            return new ElementSetUnoptimisedStorage(elementSet1);
        }

        protected virtual void PrePrepare()
        {
            var updateTimeInterval = ArgumentUpdateTimeInterval;
            var component1Type = ArgumentEngineExternalType;

            var horizon = ArgumentTimeHorizon;

            if (double.IsNegativeInfinity(horizon.StampAsModifiedJulianDay)
                || double.IsPositiveInfinity(horizon.StampAsModifiedJulianDay))
                throw new Exception("Invalid value for argument "
                    + GetArgumentIdentity(BaseComponentTimeWithEngine.ArgsWithEngineTime.TimeHorizon));

            List<IBaseInput> activeInputs = Inputs
                .Where(i => i.Provider != null)
                .ToList();
            List<IBaseOutput> activeOutputs = Outputs
                .Where(o => o.Consumers.Count > 0)
                .ToList();

            ((EngineProxy)Engine).PrePrepare(
                this,
                component1Type,
                horizon.StampAsModifiedJulianDay,
                updateTimeInterval.TotalDays,
                activeInputs, activeOutputs);       
        }

        public override void Prepare()
        {
            PrePrepare();

            base.Prepare();
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
               LinkableComponentOpenMIV1Wrapper component2,
               IExternalType component1ExternalType, 
               double startTime, double updateLinkTimeIncrement,
               List<IBaseInput> activeInputs, List<IBaseOutput> activeOutputs)
            {
                Type tComponentV1;

                Component1
                    = component1ExternalType.CreateInstance(out tComponentV1)
                    as OpenMI.Standard.ILinkableComponent;

                if (Component1 == null)
                    throw new Exception("Cannot instantiate " + component1ExternalType.ToString());

                Component1.Initialize(component2.Arguments1.ToArray());

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

                            // Get providers convertot to use directly

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
                                "OpenMI.Standard.IOutputExchangeItem not found with Qualtity,ElementSet ids if \"{0}\",\"{1}\"",
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

                                //convertor.SetRuntime(this, internalLink);
                            }
                            else if (((IHasValueSetConvertor)o).ValueSetConverter is ValueSetConverterTimeEngineDoubleVector3dStandard1)
                            {
                                var convertor = (ValueSetConverterTimeEngineDoubleVector3dStandard1)((IHasValueSetConvertor)o).ValueSetConverter;

                                //convertor.SetRuntime(this, internalLink);
                            }
                            else
                                throw new Exception("o.ValueSetConverter as ValueSetConverterTimeEngineDouble?");

                            break;
                        }

                        if (internalLink == null)
                            throw new Exception(string.Format(
                                "OpenMI.Standard.IInputExchangeItem not found with Qualtity,ElementSet ids if \"{0}\",\"{1}\"",
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

                        Trace.TraceInformation("substep Component1.GetValues({0},{1}) =\r\n\t{2}",
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
    }
}
