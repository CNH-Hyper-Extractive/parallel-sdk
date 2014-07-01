
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using FluidEarth2.Sdk.Interfaces;
using FluidEarth2.Sdk;
using ILinkableComponentVersion1 = OpenMI.Standard.ILinkableComponent;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using FluidEarth2.Sdk.CoreStandard2;

namespace FluidEarth2.Sdk
{
    public class LinkableComponentFluidEarthV1Wrapper : LinkableComponentOpenMIV1Wrapper
    {
        public void SetDefaultArguments()
        {
            // UnitTests use shadow copying, so have to go this route
            var uriExecuting = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var fileExecuting = new FileInfo(uriExecuting.LocalPath);

            var enginePath = Path.Combine(fileExecuting.DirectoryName, "FluidEarth2_Sdk_V1Wrappers.dll");

            var engineType = new ExternalType();
            engineType.Initialise(enginePath, "FluidEarth2.Sdk.V1Wrappers.Engine5");

            ArgumentParametersNativeDll = new ParametersNativeDll(engineType,
                ParametersNativeDll.Interface.FluidEarth2_Sdk_Interfaces_IEngineTime,
                null, false);

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

                ArgumentEngineExternalType = nativeEngineWrapperType;
            }
        }

        public LinkableComponentFluidEarthV1Wrapper()
            : base(new Identity("FluidEarth2.Sdk.LinkableComponentFluidEarthV1Wrapper",
                    "OpenMI Fluid Earth V1 Import", string.Empty),
                new ExternalType(typeof(LinkableComponentFluidEarthV1Wrapper)),
                new ExternalType(typeof(IProxyEngine5)), 
                true)
        {
            _includeUpdateTimeInterval = false;

            InitialiseArguments(null, null);

            SetDefaultArguments();
        }

        public LinkableComponentFluidEarthV1Wrapper(ILinkableComponentVersion1 component1, 
            IDocumentAccessor accessor,
            List<Utilities.Standard1.Argument1> args1)
            : base(new Identity(component1.ComponentID,
                    component1.ModelID + " [FluidEarthV1]", component1.ComponentDescription),
                new ExternalType(typeof(LinkableComponentOpenMIV1Wrapper)),
                new ExternalType(typeof(IProxyEngine5)),
                true)
        {
            _includeUpdateTimeInterval = false;

            Description += "\r\nUsing FluidEarth V1";

            DocumentAccessor = accessor;

            var uriPersistence = InitialiseArguments(component1, args1);

            SetDefaultArguments();

            ConstuctComponent(component1, uriPersistence, args1);
        }

        protected override BaseOutput NewOutputSpaceTime(string engineVariable, string description, OpenMI.Standard.IOutputExchangeItem output1, Utilities.Standard1.ExchangeItemV1ModelXml v1Model, int elementCount)
        {
            var output = new OutputSpaceTimeUserVariables(
                new Identity(engineVariable, engineVariable, description),
                this,
                new Quantity(output1.Quantity, typeof(double), -999.999),
                ConvertElementSet(output1.ElementSet, v1Model),
                engineVariable, -999.999, elementCount);

            //foreach (var kv in v1Model.UserVariables)
            //    output.UserVariables.Add(kv.Key, kv.Value);

            return output;
        }

        protected override BaseInput NewInputSpaceTime(string engineVariable, string description, OpenMI.Standard.IInputExchangeItem input1, Utilities.Standard1.ExchangeItemV1ModelXml v1Model, int elementCount)
        {
            var input = new InputSpaceTimeUserVariables(
                new Identity(engineVariable, engineVariable, description),
                this,
                new Quantity(input1.Quantity, typeof(double), -999.999),
                ConvertElementSet(input1.ElementSet, v1Model),
                engineVariable, -999.999, elementCount);

            //foreach (var kv in v1Model.UserVariables)
            //    input.UserVariables.Add(kv.Key, kv.Value);

            return input;
        }

        protected override void PrePrepare()
        {
            var component1Type = ArgumentEngineExternalType;

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

            var componentPath = new Uri(Assembly.GetAssembly(typeof(LinkableComponentFluidEarthV1Wrapper)).CodeBase);

            ISpatialDefinition iSpatial;

            var inputIds = new List<Ids>();

            foreach (var i in activeInputs)
            {   
                iSpatial = Utilities.AsSpatialDefinition(i);

                inputIds.Add(new Ids(
                    BaseComponentWithEngine.EngineConvertor(i).EngineVariable,
                    i.ValueDefinition.Caption,
                    iSpatial != null ? iSpatial.Caption : string.Empty,
                    new Dictionary<string, string>()));
            }

            var outputIds = new List<Ids>();

            foreach (var i in activeOutputs)
            {
                iSpatial = Utilities.AsSpatialDefinition(i);
                outputIds.Add(new Ids(
                    BaseComponentWithEngine.EngineConvertor(i).EngineVariable,
                    i.ValueDefinition.Caption,
                    iSpatial != null ? iSpatial.Caption : string.Empty,
                    new Dictionary<string, string>()));
            }

            var nativeEngineWrapperType = ArgumentEngineExternalType;

            ((IProxyEngine5)Engine).PrePrepare(
                componentPath.LocalPath, inputIds, outputIds, horizon,
                nativeEngineWrapperType.AssemblyName, nativeEngineWrapperType.TypeName);
        }

        internal class InputSpaceTimeUserVariables : InputSpaceTime
        {
            public InputSpaceTimeUserVariables()
            { }

            public InputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDouble(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear))
            { }

            public InputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3d(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<Vector3d<double>>.InterpolationTemporal.Linear))
            { }
        }

        internal class OutputSpaceTimeUserVariables : OutputSpaceTime
        {
            public OutputSpaceTimeUserVariables()
            { }

            public OutputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, double missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDouble(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear))
            { }

            public OutputSpaceTimeUserVariables(IIdentifiable identity, IBaseLinkableComponent component,
                IValueDefinition iValueDefinition, ISpatialDefinition iSpatialDefinition,
                string engineVariable, Vector3d<double> missingValue, int elementCount)
                : base(identity, iValueDefinition, iSpatialDefinition, component,
                    new ValueSetConverterTimeEngineDoubleVector3d(engineVariable, missingValue, elementCount, ValueSetConverterTimeRecordBase<Vector3d<double>>.InterpolationTemporal.Linear))
            { }
        }
    }
}

