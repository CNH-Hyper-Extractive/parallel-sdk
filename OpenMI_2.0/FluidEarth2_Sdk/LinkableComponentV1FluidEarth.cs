using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Convert an FluidEarth V1 component into an OpenMI v2 component
    /// </summary>
    public class LinkableComponentV1FluidEarth : LinkableComponentV1Base
    {   
        XElement _v1ModelXml = null;
        Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> _v1ModelXmlInputs = null;
        Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> _v1ModelXmlOutputs = null;
        int _nInputs = 0;
        int _nOutputs = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public LinkableComponentV1FluidEarth()
            : base(new Identity(new Describes("FluidEarthV1", "Converts a OpenMI V1 component implemented using FluidEarth V1 to OpenMI V2")),
                new ExternalType(typeof(LinkableComponentV1FluidEarth)),
                new ExternalType(typeof(IProxyEngine5)),
                true)
        {
            var v1ServerType = new ArgumentExternalType(GetArgumentIdentity(ArgsFE.ExternalTypeV1EngineServer), new ExternalType());

            Arguments.Add(v1ServerType);

            // UnitTests use shadow copying, so have to go this route
            var uriExecuting = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var fileExecuting = new FileInfo(uriExecuting.LocalPath);

            var enginePath = Path.Combine(fileExecuting.DirectoryName, "FluidEarth2_Sdk_V1Wrappers.dll");

            var engineType = new ExternalType();
            engineType.Initialise(enginePath, "FluidEarth2.Sdk.V1Wrappers.Engine5");

            ArgumentParametersNativeDll = new ParametersNativeDll(engineType,
                ParametersNativeDll.Interface.FluidEarth2_Sdk_Interfaces_IEngineTime,
                null, false);
        }

        /// <summary>
        /// Intercept base initialise call to update references to IEngine server and native DLL.
        /// </summary>
        /// <param name="reinitialising">True if component has been initialised before</param>
        protected override void DoInitialise(bool reinitialising)
        {
            base.DoInitialise(reinitialising);

            var serverExe = Arguments
                .Where(a => a.Caption == "FluidEarth_SDK.ArgFile.ServerExe"
                    | a.Caption == "OpenWEB_SDK.ArgFile.ServerExe")
                .SingleOrDefault();

            if (serverExe != null)
            {
                var serverType = Arguments
                    .Where(a => a.Caption == "FluidEarth_SDK.Arg.ServerType"
                        || a.Caption == "OpenWEB_SDK.Arg.ServerType")
                    .SingleOrDefault();

                string typeName = string.Empty;

                // Guess that implemented followed recommended naming conventions and structure so Engine exists

                if (serverType != null)
                    typeName = serverType
                        .ValueAsString
                        .Substring(0, serverType.ValueAsString.LastIndexOf(".")) + ".Engine";

                var nativeEngineWrapperType = new ExternalType();
                nativeEngineWrapperType.Initialise(
                    serverExe.ValueAsString,
                    typeName);

                try
                {
                    Type type;
                    if (nativeEngineWrapperType.CreateInstance(out type) == null)
                        nativeEngineWrapperType.TypeName = string.Empty;
                }
                catch (Exception)
                {
                    nativeEngineWrapperType.TypeName = string.Empty;
                }

                if (nativeEngineWrapperType.TypeName == string.Empty)
                {
                    // try again with unmodified name
                    typeName = serverType.ValueAsString;
                    nativeEngineWrapperType.Initialise(serverExe.ValueAsString, typeName);

                    try
                    {
                        Type type;
                        if (nativeEngineWrapperType.CreateInstance(out type) == null)
                            nativeEngineWrapperType.TypeName = string.Empty;
                    }
                    catch (Exception)
                    {
                        nativeEngineWrapperType.TypeName = string.Empty;
                    }
                }

                ArgumentExternalTypeV1EngineServer = nativeEngineWrapperType;
            }
        }

        /// <summary>
        /// Arguments implemented by this class
        /// </summary>
        public enum ArgsFE
        {
            /// <summary>
            /// External type details of engine server
            /// </summary>
            ExternalTypeV1EngineServer = 0,
        }

        /// <summary>
        /// Identities of arguments implemented by this class
        /// </summary>
        /// <param name="key">Argument</param>
        /// <returns>Argument identity</returns>
        public static IIdentifiable GetArgumentIdentity(ArgsFE key)
        {
            switch (key)
            {
                case ArgsFE.ExternalTypeV1EngineServer:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentV1FluidEarth." + key.ToString(),
                        "Type of FluidEarth V1 Engine Server",
                        "Provides assembly location and type for the original FluidEarth V1 Engine Server");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        /// <summary>
        /// External type details of engine server
        /// </summary>
        public IExternalType ArgumentExternalTypeV1EngineServer
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(ArgsFE.ExternalTypeV1EngineServer))
                    .Value as ArgumentValueExternalType;

                return argValue.Value;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(ArgsFE.ExternalTypeV1EngineServer))
                    .Value as ArgumentValueExternalType;

                argValue.Value = value;
            }
        }

        /// <summary>
        /// Validate arguments implemented by this class
        /// </summary>
        /// <param name="arg">Argument to validate</param>
        /// <param name="message">Validation message</param>
        /// <returns>Validation status</returns>
        protected override ValidArgumentMessage ValidArgumentValue(IArgument arg, out string message)
        {
            try
            {
                message = string.Empty;

                if (arg.Id == GetArgumentIdentity(ArgsFE.ExternalTypeV1EngineServer).Id)
                {
                    var xType = ArgumentExternalTypeV1EngineServer;
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

        /// <summary>
        /// Prepare the engine for runtime usage
        /// </summary>
        public override void Prepare()
        {
            var horizon = ArgumentTimeHorizon;

            if (double.IsNegativeInfinity(horizon.StampAsModifiedJulianDay)
                || double.IsPositiveInfinity(horizon.StampAsModifiedJulianDay))
                throw new Exception("Invalid value for argument "
                    + GetArgumentIdentity(BaseComponentTimeWithEngine.ArgsWithEngineTime.TimeHorizon));

            IEnumerable<InputSpaceTimeUserVariables> activeInputs = Inputs
                .Where(i => i.Provider != null)
                .Cast<InputSpaceTimeUserVariables>();
            IEnumerable<OutputSpaceTimeUserVariables> activeOutputs = Outputs
                .Where(o => o.Consumers.Count > 0)
                .Cast<OutputSpaceTimeUserVariables>();

            var componentPath = new Uri(Assembly.GetAssembly(typeof(LinkableComponentV1FluidEarth)).CodeBase);

            ISpatialDefinition iSpatial;

            var inputIds = new List<Ids>();

            foreach (var i in activeInputs)
            {
                iSpatial = Utilities.AsSpatialDefinition(i);

                inputIds.Add(new Ids(
                    BaseComponentWithEngine.EngineConvertor(i).EngineVariable,
                    i.ValueDefinition.Caption,
                    iSpatial != null ? iSpatial.Caption : string.Empty,
                    i.UserVariables));
            }

            var outputIds = new List<Ids>();

            foreach (var i in activeOutputs)
            {
                iSpatial = Utilities.AsSpatialDefinition(i);
                outputIds.Add(new Ids(
                    BaseComponentWithEngine.EngineConvertor(i).EngineVariable,
                    i.ValueDefinition.Caption,
                    iSpatial != null ? iSpatial.Caption : string.Empty,
                    i.UserVariables));
            }

            var nativeEngineWrapperType = ArgumentExternalTypeV1EngineServer;

            ((IProxyEngine5)Engine).PrePrepare(
                componentPath.LocalPath, inputIds, outputIds, horizon,
                nativeEngineWrapperType.AssemblyName, nativeEngineWrapperType.TypeName);

            base.Prepare();
        }

       /// <summary>
       /// Get FluidEarth v1 XML model definitions from file
       /// </summary>
        XElement V1ModelXml
        {
            get
            {
                if (_v1ModelXml == null)
                {
                    var argModelResource = Arguments
                        .Where(a => a.Caption == "FluidEarth_SDK.ArgResource.Model")
                        .SingleOrDefault();

                    if (argModelResource != null)
                    {
                        // If necessary can automate this
                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine("Model file is an Embedded Resources, need to unpack");
                        sb.AppendLine("On command line, run component assembly with argument \"unpack\" e.g.");
                        sb.AppendLine(">FluidEarth_Example_TankCs.exe unpack");
                        sb.AppendLine("Then REMOVE OMI argument FluidEarth_SDK.ArgResource.Model and REPLACE with");
                        sb.AppendLine("FluidEarth_SDK.ArgFile.Model");
                        sb.AppendLine("with value set to the unpacked model file");

                        throw new Exception(sb.ToString());
                    }

                    var argModelFile = Arguments
                        .Where(a => a.Caption == "FluidEarth_SDK.ArgFile.Model")
                        .SingleOrDefault();

                    if (argModelFile == null) // further back in history?
                        argModelFile = Arguments
                        .Where(a => a.Caption == "OpenWEB_SDK.ArgFile.Model")
                        .SingleOrDefault();

                    var doc = XDocument.Load(argModelFile.ValueAsString);

                    _v1ModelXml = doc.Root;
                }

                return _v1ModelXml;
            }
        }

        /// <summary>
        /// Extract Model 1 input details from v1 XML model file
        /// </summary>
        Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> V1ModelInputs
        {
            get
            {
                if (_v1ModelXmlInputs == null && V1ModelXml != null)                
                    Utilities.Standard1.ReadModelFile(V1ModelXml, out _v1ModelXmlInputs, out _v1ModelXmlOutputs); 

                return _v1ModelXmlInputs;          
            }
        }

        /// <summary>
        /// Extract Model 1 output details from v1 XML model file
        /// </summary>
        Dictionary<string, Utilities.Standard1.ExchangeItemV1ModelXml> V1ModelOutputs
        {
            get
            {
                if (_v1ModelXmlOutputs == null && V1ModelXml != null)                
                    Utilities.Standard1.ReadModelFile(V1ModelXml, out _v1ModelXmlInputs, out _v1ModelXmlOutputs); 

                return _v1ModelXmlOutputs;          
            }
        }

        /// <summary>
        /// Extract specific Model 1 input details from v1 XML model file
        /// </summary>
        /// <param name="item">V1 input exchange item</param>
        /// <returns>V1 item details</returns>
        Utilities.Standard1.ExchangeItemV1ModelXml V1ModelXmlItem(OpenMI.Standard.IInputExchangeItem item)
        {
            return V1ModelInputs != null
                ? V1ModelInputs
                    .Values
                    .Where(v =>
                        v.Quantity.Id == item.Quantity.ID
                        && v.ElementSet.Id == item.ElementSet.ID)
                    .SingleOrDefault()
                : null;
        }

        /// <summary>
        /// Extract specific Model 1 output details from v1 XML model file
        /// </summary>
        /// <param name="item">V1 output exchange item</param>
        /// <returns>V1 item details</returns>
        Utilities.Standard1.ExchangeItemV1ModelXml V1ModelXmlItem(OpenMI.Standard.IOutputExchangeItem item)
        {
            return V1ModelOutputs != null
                ? V1ModelOutputs
                    .Values
                    .Where(v =>
                        v.Quantity.Id == item.Quantity.ID
                        && v.ElementSet.Id == item.ElementSet.ID)
                    .SingleOrDefault()
                : null;
        }

        /// <summary>
        /// Convert a OpenMI 1.4 input exchange item into corresponding OpenMI 2.0 base input.
        /// </summary>
        /// <param name="item1">OpenMI version 1.4 input exchange item</param>
        /// <returns>OpenMI 2.0 base input</returns>
        protected override IBaseInput Convert(OpenMI.Standard.IInputExchangeItem item1)
        {
            var engineVariable = string.Format("Input{0}.{1}.{2}",
                ++_nInputs, item1.Quantity.ID, item1.ElementSet.ID);

            var describes = new Describes(engineVariable,
                string.Format("{0}\r\n\r\n{1}", item1.Quantity.Description, item1.ElementSet.Description));

            var v1Xml = V1ModelXmlItem(item1);

            var elementSet = ConvertElementSet(item1.ElementSet, v1Xml);

            var input = new InputSpaceTimeUserVariables(
                new Identity(engineVariable, describes),
                this,
                new Quantity(item1.Quantity, typeof(double), -999.999),
                elementSet,
                engineVariable, -999.999, elementSet.ElementCount);

            if (v1Xml != null)
                foreach (var kv in v1Xml.UserVariables)
                    input.UserVariables.Add(kv.Key, kv.Value);

            return input;
        }

        /// <summary>
        /// Convert a OpenMI 1.4 output exchange item into corresponding OpenMI 2.0 base output.
        /// </summary>
        /// <param name="item1">OpenMI version 1.4 output exchange item</param>
        /// <returns>OpenMI 2.0 base output</returns>
        protected override IBaseOutput Convert(OpenMI.Standard.IOutputExchangeItem item1)
        {
            var engineVariable = string.Format("Output{0}.{1}.{2}",
                ++_nOutputs, item1.Quantity.ID, item1.ElementSet.ID);

            var describes = new Describes(engineVariable,
                string.Format("{0}\r\n\r\n{1}", item1.Quantity.Description, item1.ElementSet.Description));

            var v1Xml = V1ModelXmlItem(item1);

            var elementSet = ConvertElementSet(item1.ElementSet, v1Xml);

            var output = new OutputSpaceTimeUserVariables(
                new Identity(engineVariable, describes),
                this,
                new Quantity(item1.Quantity, typeof(double), -999.999),
                elementSet,
                engineVariable, -999.999, elementSet.ElementCount);

            if (v1Xml != null)
                foreach (var kv in v1Xml.UserVariables)
                    output.UserVariables.Add(kv.Key, kv.Value);

            return output;
        }

        /// <summary>
        /// Convert a OpenMI 1.4 elementSet into corresponding OpenMI 2.0 elementSet.
        /// </summary>
        /// <param name="elementSet1">OpenMI version 1.4 elementSet</param>
        /// <param name="v1Model">Model file details</param>
        /// <returns>OpenMI version 2.0 elementSet</returns>
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

        /// <summary>
        /// v2 IBaseInputTime implementation that uses IProxyEngine5 for runtime.
        /// </summary>
        public class InputSpaceTimeUserVariables : InputSpaceTime
        {
            public Dictionary<string, string> UserVariables { get; private set; }

            public InputSpaceTimeUserVariables()
            {
                UserVariables = new Dictionary<string, string>();
            }

            public InputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDouble(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear))
            {
                UserVariables = new Dictionary<string, string>();
            }

            public InputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3d(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<Vector3d<double>>.InterpolationTemporal.Linear))
            {
                UserVariables = new Dictionary<string, string>();
            }

            public override void Initialise(XElement xElement, IDocumentAccessor accessor)
            {
                base.Initialise(xElement, accessor);

                UserVariables = xElement
                    .Elements("User")
                    .ToDictionary(k => Utilities.Xml.GetAttribute(k, "key"), v => Utilities.Xml.GetAttribute(v, "value"));
            }

            public override XElement Persist(IDocumentAccessor accessor)
            {
                var xml =  base.Persist(accessor);
                xml.Add(UserVariables
                    .Select(v => new XElement("User", new XAttribute("key", v.Key), new XAttribute("value", v.Value))));
                return xml;
            }
        }

        /// <summary>
        /// v2 IBaseOutputTime implementation that uses IProxyEngine5 for runtime.
        /// </summary>
        public class OutputSpaceTimeUserVariables : OutputSpaceTime
        {
            public Dictionary<string, string> UserVariables { get; private set; }

            public OutputSpaceTimeUserVariables()
            {
                UserVariables = new Dictionary<string, string>();
            }

            public OutputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDouble(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear))
            {
                UserVariables = new Dictionary<string, string>();
            }

            public OutputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3d(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<Vector3d<double>>.InterpolationTemporal.Linear))
            {
                UserVariables = new Dictionary<string, string>();
            }

            public override void Initialise(XElement xElement, IDocumentAccessor accessor)
            {
                base.Initialise(xElement, accessor);

                UserVariables = xElement
                    .Elements("User")
                    .ToDictionary(k => Utilities.Xml.GetAttribute(k, "key"), v => Utilities.Xml.GetAttribute(v, "value"));
            }

            public override XElement Persist(IDocumentAccessor accessor)
            {
                var xml = base.Persist(accessor);
                xml.Add(UserVariables
                    .Select(v => new XElement("User", new XAttribute("key", v.Key), new XAttribute("value", v.Value))));
                return xml;
            }
        }
    }
}
