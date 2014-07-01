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
    public abstract class AdaptedOutputTimeNativeDouble
        : AdaptedOutputTimeNativeBase<double>
    {
        public AdaptedOutputTimeNativeDouble()
        { }

        /// <summary>
        /// Class for adapted output using native library to implement base.Adapt(...)
        /// </summary>
        /// <param name="identity">Identity of adapted output, must be non null</param>
        /// <param name="nativeLibrary">Native library</param>
        /// <param name="adaptee">Output item to adapt, must be non null</param>
        /// <param name="target">Input item to provide for, can be null (null in UI building)</param>
        public AdaptedOutputTimeNativeDouble(IIdentifiable identity, ExternalType nativeLibrary, IBaseOutput adaptee, IBaseInput target, IAdaptedOutputFactory factory)
            : base(identity, nativeLibrary, adaptee, target, factory)
        { }

        public override IEnumerable<double> ToDoubles(IEnumerable<double> values)
        {
            return values;
        }

        public override IEnumerable<double> FromDoubles(IEnumerable<double> values)
        {
            return values;
        }
    }

    public abstract class AdaptedOutputTimeNativeVector2dDouble
        : AdaptedOutputTimeNativeBase<Vector2d<double>>
    {
        public AdaptedOutputTimeNativeVector2dDouble()
        { }

        /// <summary>
        /// Class for adapted output using native library to implement base.Adapt(...)
        /// </summary>
        /// <param name="identity">Identity of adapted output, must be non null</param>
        /// <param name="nativeLibrary">Native library</param>
        /// <param name="adaptee">Output item to adapt, must be non null</param>
        /// <param name="target">Input item to provide for, can be null (null in UI building)</param>
        public AdaptedOutputTimeNativeVector2dDouble(IIdentifiable identity, ExternalType nativeLibrary, IBaseOutput adaptee, IBaseInput target, IAdaptedOutputFactory factory)
            : base(identity, nativeLibrary, adaptee, target, factory)
        { }

        public override IEnumerable<double> ToDoubles(IEnumerable<Vector2d<double>> values)
        {
            return values
                .SelectMany(v => v.Values);
        }

        public override IEnumerable<Vector2d<double>> FromDoubles(IEnumerable<double> values)
        {
            var v = values.ToArray();

            return Enumerable
                .Range(0, v.Length / 2)
                .Select(n => new Vector2d<double>(v, 2 * n));
        }
    }

    public abstract class AdaptedOutputTimeNativeVector3dDouble
        : AdaptedOutputTimeNativeBase<Vector3d<double>>
    {
        public AdaptedOutputTimeNativeVector3dDouble()
        { }

        /// <summary>
        /// Class for adapted output using native library to implement base.Adapt(...)
        /// </summary>
        /// <param name="identity">Identity of adapted output, must be non null</param>
        /// <param name="nativeLibrary">Native library</param>
        /// <param name="adaptee">Output item to adapt, must be non null</param>
        /// <param name="target">Input item to provide for, can be null (null in UI building)</param>
        public AdaptedOutputTimeNativeVector3dDouble(IIdentifiable identity, ExternalType nativeLibrary, IBaseOutput adaptee, IBaseInput target, IAdaptedOutputFactory factory)
            : base(identity, nativeLibrary, adaptee, target, factory)
        { }

        public override IEnumerable<double> ToDoubles(IEnumerable<Vector3d<double>> values)
        {
            return values
                .SelectMany(v => v.Values);
        }

        public override IEnumerable<Vector3d<double>> FromDoubles(IEnumerable<double> values)
        {
            var v = values.ToArray();

            return Enumerable
                .Range(0, v.Length / 3)
                .Select(n => new Vector3d<double>(v, 3 * n));
        }
    }

    public abstract class AdaptedOutputTimeNativeBase<TType>
        : AdaptedOutputTimeBase<TType, TType>
    {
        public abstract IEnumerable<double> ToDoubles(IEnumerable<TType> values);
        public abstract IEnumerable<TType> FromDoubles(IEnumerable<double> values);

        Stream _diagnosticsStream = null;

        public AdaptedOutputTimeNativeBase()
        { }

        /// <summary>
        /// Class for adapted output using native library to implement base.Adapt(...)
        /// </summary>
        /// <param name="identity">Identity of adapted output, must be non null</param>
        /// <param name="nativeLibrary">Native library</param>
        /// <param name="adaptee">Output item to adapt, must be non null</param>
        /// <param name="target">Input item to provide for, can be null (null in UI building)</param>
        public AdaptedOutputTimeNativeBase(IIdentifiable identity, ExternalType nativeLibrary, IBaseOutput adaptee, IBaseInput target, IAdaptedOutputFactory factory)
            : base(identity, adaptee, target, factory, MissingDataValue(adaptee))
        {
            ArgumentsAddRange(new IArgument[] 
            { 
                new ArgumentExternalType(GetArgumentIdentity(Args.NativeLibrary), nativeLibrary),
                new ArgumentParametersDiagnosticsEngine(GetArgumentIdentity(Args.Diagnostics),
                    new ParametersDiagnosticsNative()),               
                new ArgumentParametersRemoting(GetArgumentIdentity(Args.Remoting), 
                    new ParametersRemoting()),  
            });

            if (target != null)
            {
                AddConsumer(target);
                adaptee.AddAdaptedOutput(this);
            }
        }

        static object MissingDataValue(IBaseOutput adaptee)
        {
            Contract.Requires(adaptee != null, "Adaptee != null");
            Contract.Requires(adaptee.ValueDefinition != null, "adaptee.ValueDefinition != null");

            return adaptee.ValueDefinition.MissingDataValue;
        }

        public override bool IsValid(out string whyNot)
        {
            if (!base.IsValid(out whyNot))
                return false;

            var adapteeElementSet = Utilities.AsElementSet(Adaptee);

            if (adapteeElementSet == null)
            {
                whyNot = "Adaptee does not implement ElementSet";
                return false;
            }

            foreach (var consumer in Consumers)
            {
                var prefix = "Consumer " + consumer.Caption;

                var elementSet = Utilities.AsElementSet(consumer);

                if (!Utilities.IsValid(prefix, elementSet, adapteeElementSet.ElementType, out whyNot))
                    return false;
            }

            whyNot = string.Empty;
            return true;
        }

        public override bool CanConnect(IBaseExchangeItem proposed, out string whyNot)
        {
            if (!base.CanConnect(proposed, out whyNot))
                return false;

            var adapteeElementSet = Utilities.AsElementSet(Adaptee);

            var prefix = "Proposed" + proposed.Caption;

            var elementSet = Utilities.AsElementSet(proposed);

            if (!Utilities.IsValid(prefix, elementSet, adapteeElementSet.ElementType, out whyNot))
                return false;

            whyNot = string.Empty;
            return true;
        }

        public enum Args { NativeLibrary = 0, Remoting, Diagnostics, }

        public static IIdentifiable GetArgumentIdentity(Args key)
        {
            switch (key)
            {
                case Args.NativeLibrary:
                    return new Identity("FluidEarth2.Sdk.AdaptedOutputTimeNative." + key.ToString(),
                        "NativeLibrary", "Native library implementing adapter");
                case Args.Remoting:
                    return new Identity("FluidEarth2.Sdk.AdaptedOutputTimeNative." + key.ToString(),
                        "Remoting", "Out of process 'remoting' settings");
                case Args.Diagnostics:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "Diagnostics", "Monitoring and Debugging options");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public ParametersDiagnosticsNative Diagnostics
        {
            get
            {
                return ((ArgumentParametersDiagnosticsEngine)Argument(GetArgumentIdentity(Args.Diagnostics))).Parameters;
            }

            set
            {
                ((ArgumentParametersDiagnosticsEngine)Argument(GetArgumentIdentity(Args.Diagnostics))).Parameters = value;
            }
        }

        public ParametersRemoting ArgumentParametersRemoting
        {
            get
            {
                return ((ArgumentParametersRemoting)Argument(GetArgumentIdentity(Args.Remoting))).ParametersRemoting;
            }

            set
            {
                ((ArgumentParametersRemoting)Argument(GetArgumentIdentity(Args.Remoting))).ParametersRemoting = value;
            }
        }

        public IExternalType NativeLibrary
        {
            get
            {
                return ((ArgumentExternalType)Argument(GetArgumentIdentity(Args.NativeLibrary))).ExternalType;
            }

            set
            {
                ((ArgumentExternalType)Argument(GetArgumentIdentity(Args.NativeLibrary))).ExternalType = value;
            }
        }

        IAdapterNativeLibraryDouble _client;

        protected IAdapterNativeLibraryDouble Client
        {
            get
            {
                if (_client == null)
                {
                    var remoteData = ArgumentParametersRemoting;

                    if (remoteData.Protocol == RemotingProtocol.inProcess)
                    {
                        Type type;
                        var obj = NativeLibrary.CreateInstance(out type);

                        if (obj is IAdapterNativeLibraryDouble)
                            _client = obj as IAdapterNativeLibraryDouble;
                        else
                        {
                            Contract.Requires(false,
                                "{0} is not derived from IAdapterNativeLibraryDouble",
                                type.ToString());
                        }
                    }
                    else
                    {
                        var uriThis = new Uri(Assembly.GetAssembly(typeof(AdaptedOutputTimeNativeBase<TType>)).CodeBase);
                        var fileThis = new FileInfo(uriThis.LocalPath);

                        var path = Path.Combine(fileThis.DirectoryName, "FluidEarth2_Sdk_RemotingAdapterTimeDouble.exe");
                        var xtype = new ExternalType(path, "FluidEarth2.Sdk.RemotingAdapterTimeDouble.Client");

                        Type type;
                        var remotingClient = xtype.CreateInstance(out type) as IClient;

                        remotingClient.ServerConnection(Arguments);

                        if (remotingClient is IAdapterNativeLibraryDouble)
                            _client = remotingClient as IAdapterNativeLibraryDouble;
                        else
                        {
                            Contract.Requires(false,
                                "{0} is not derived from IAdapterNativeLibraryDouble",
                                type.ToString());
                        }
                    }
                }

                return _client;
            }
        }

        public virtual XElement ServerInitialisingXml(IDocumentAccessor accessor)
        {
            return new XElement("Server",
                NativeLibrary.Persist(accessor),
                Persistence.Arguments.Persist(Arguments, accessor));
        }

        protected GeometryPassingOptions _geometryPassingOptionsAdaptee = GeometryPassingOptions.None;
        protected GeometryPassingOptions _geometryPassingOptionsAdapted = GeometryPassingOptions.None;

        Dictionary<IBaseExchangeItem, string> _adaptedKeys 
            = new Dictionary<IBaseExchangeItem, string>();

        protected override void PrepareIt()
        {
            if (Client == null)
                throw new Exception("NativeLibrary not specified");

            base.PrepareIt();

            // Use Client to force initialisation if null
            _client = Utilities.Diagnostics.AddDiagnostics(
                Diagnostics, Client,
                out _diagnosticsStream, false);

            if (Client.Ping() < 0)
                throw new Exception("_nativeLibrary.Ping < 0");

            int successCode;
            var xml = Persist(null).ToString();

            Client.Initialise(xml, out successCode);
            Utilities2.Native.ProcessSuccessCode("Initialise", successCode, Client.GetSuccessMessage);

            foreach (var iArg in Arguments)
            {
                Client.SetArgument(iArg.Id, iArg.ValueAsString, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }

            var missingDataValue = Adaptee.ValueDefinition.MissingDataValue.ToString();

            SetArgument("Adaptee", _converterAdaptee as IValueSetConverterTimeEngine, missingDataValue);
            SetArgument("Adaptee", Adaptee);

            if (_geometryPassingOptionsAdaptee != GeometryPassingOptions.None)
                PassGeometry(WhichWay.Adaptee, _geometryPassingOptionsAdaptee, Adaptee);

            int n = 0;
            foreach (var item in _converterQuerySpecifiers)
            {
                _adaptedKeys.Add(item.Key, string.Format("Adapted.{0}", ++n));

                missingDataValue = item.Key.ValueDefinition.MissingDataValue.ToString();

                SetArgument(_adaptedKeys[item.Key], item.Key);
                SetArgument(_adaptedKeys[item.Key], item.Value as IValueSetConverterTimeEngine, missingDataValue);

                if (_geometryPassingOptionsAdapted != GeometryPassingOptions.None)
                    PassGeometry(WhichWay.Adapted, _geometryPassingOptionsAdaptee, item.Key);
            }

            Client.Prepare(out successCode);
            Utilities2.Native.ProcessSuccessCode("Prepare", successCode, Client.GetSuccessMessage);
        }

        void PassGeometry(WhichWay whichWay, GeometryPassingOptions options, IBaseExchangeItem item)
        {
            IElementSet elementSet = Utilities.AsElementSet(item);

            if (elementSet == null)
                throw new Exception("No element set found");

            int successCode;
            double[] coords;

            int count = 0;

            for (int n = 0; n < elementSet.ElementCount; ++n)
                count += elementSet.GetVertexCount(n);

            if ((_geometryPassingOptionsAdaptee & GeometryPassingOptions.X) != 0)
            {
                coords = new double[count];

                int i = -1;
                for (int n = 0; n < elementSet.ElementCount; ++n)
                    for (int m = 0; m < elementSet.GetVertexCount(n); ++m)
                        coords[++i] = elementSet.GetVertexXCoordinate(n, m);

                Client.SetGeometryCoords(whichWay, GeometryPassingOptions.X, coords, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetGeometryCoords", successCode, Client.GetSuccessMessage);
            }

            if ((_geometryPassingOptionsAdaptee & GeometryPassingOptions.Y) != 0)
            {
                coords = new double[count];

                int i = -1;
                for (int n = 0; n < elementSet.ElementCount; ++n)
                    for (int m = 0; m < elementSet.GetVertexCount(n); ++m)
                        coords[++i] = elementSet.GetVertexYCoordinate(n, m);

                Client.SetGeometryCoords(whichWay, GeometryPassingOptions.Y, coords, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetGeometryCoords", successCode, Client.GetSuccessMessage);
            }

            if ((_geometryPassingOptionsAdaptee & GeometryPassingOptions.Z) != 0)
            {
                coords = new double[count];

                int i = -1;
                for (int n = 0; n < elementSet.ElementCount; ++n)
                    for (int m = 0; m < elementSet.GetVertexCount(n); ++m)
                        coords[++i] = elementSet.GetVertexZCoordinate(n, m);

                Client.SetGeometryCoords(whichWay, GeometryPassingOptions.Z, coords, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetGeometryCoords", successCode, Client.GetSuccessMessage);
            }

            if ((_geometryPassingOptionsAdaptee & GeometryPassingOptions.M) != 0)
            {
                coords = new double[count];

                int i = -1;
                for (int n = 0; n < elementSet.ElementCount; ++n)
                    for (int m = 0; m < elementSet.GetVertexCount(n); ++m)
                        coords[++i] = elementSet.GetVertexMCoordinate(n, m);

                Client.SetGeometryCoords(whichWay, GeometryPassingOptions.M, coords, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetGeometryCoords", successCode, Client.GetSuccessMessage);
            }

            if ((_geometryPassingOptionsAdaptee & GeometryPassingOptions.VertexCounts) != 0)
            {
                int[] counts = new int[elementSet.ElementCount];

                for (int n = 0; n < elementSet.ElementCount; ++n)
                    counts[n] = elementSet.GetVertexCount(n);

                Client.SetGeometryVertexCounts(whichWay, counts, out successCode);
                Utilities2.Native.ProcessSuccessCode("SetGeometryVertexCounts", successCode, Client.GetSuccessMessage);
            }
        }

        void SetArgument(string prefix, IBaseExchangeItem item)
        {
            if (item == null)
                return;

            int successCode;

            Client.SetArgument(prefix + ".IBaseExchangeItem.Id", item.Id, out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

            Client.SetArgument(prefix + ".IBaseExchangeItem.Caption", item.Caption, out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

            if (item.ValueDefinition != null)
            {
                Client.SetArgument(prefix + ".IBaseExchangeItem.ValueDefinition.ValueType", 
                    item.ValueDefinition.ValueType.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

                Client.SetArgument(prefix + ".IBaseExchangeItem.ValueDefinition.MissingDataValue",
                    item.ValueDefinition.MissingDataValue.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }

            var spatial = Utilities.AsSpatialDefinition(item);

            if (spatial != null)
            {
                Client.SetArgument(prefix + ".IBaseExchangeItem.SpatialDefinition.ElementCount",
                    spatial.ElementCount.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

                Client.SetArgument(prefix + ".IBaseExchangeItem.SpatialDefinition.Version",
                    spatial.Version.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

                Client.SetArgument(prefix + ".IBaseExchangeItem.SpatialDefinition.SpatialReferenceSystemWkt",
                    spatial.SpatialReferenceSystemWkt.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }

            IElementSet elementSet = Utilities.AsElementSet(item);

            if (elementSet != null)
            {
                Client.SetArgument(prefix + ".IBaseExchangeItem.ElementSet.ElementType",
                    elementSet.ElementType.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }
        }

        void SetArgument(string prefix, IValueSetConverterTimeEngine packing, string missingDataValue)
        {
            if (packing == null)
                throw new Exception("!IValueSetConverterTimeEngine");

            int successCode;

            Client.SetArgument(prefix + ".Packing.ElementCount",
                packing.ElementCount.ToString(), out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

            if (packing.ElementValueCountConstant)
            {
                Client.SetArgument(prefix + ".Packing.ElementValueCount",
                    packing.ElementValueCount.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }
            else
            {
                string counts = packing
                    .ElementValueCounts
                    .Aggregate(new StringBuilder(), (sb, n) => sb.AppendFormat("{0} ", n.ToString()))
                    .ToString()
                    .Trim();

                Client.SetArgument(prefix + ".Packing.ElementValueCounts",
                    counts.ToString(), out successCode);
                Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
            }

            Client.SetArgument(prefix + ".Packing.VectorLength",
                packing.VectorLength.ToString(), out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

            Client.SetArgument(prefix + ".Packing.ValueArrayLength",
                packing.ValueArrayLength.ToString(), out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);

            Client.SetArgument(prefix + ".Packing.MissingValue",
                missingDataValue, out successCode);
            Utilities2.Native.ProcessSuccessCode("SetArgument", successCode, Client.GetSuccessMessage);
        }

        public override void Finish()
        {
            if (_finished)
                return;

            try
            {
                int successCode;

                Client.Finish(out successCode);
                Utilities2.Native.ProcessSuccessCode("Finish", successCode, Client.GetSuccessMessage);

                base.Finish();

                if (_diagnosticsStream != null)
                {
                    _diagnosticsStream.Close();
                    _diagnosticsStream = null;
                }
            }
            finally
            {
                _finished = true;
            }
        }

        public override IEnumerable<TimeRecord<TType>> AdaptRecords(List<TimeRecord<TType>> toAdapt, IBaseExchangeItem querySpecifier)
        {
            Contract.Requires(toAdapt != null, "toAdapt != null");
            Contract.Requires(querySpecifier != null, "querySpecifier != null");

            var convertorConsumer = _converterQuerySpecifiers[querySpecifier]
                as IValueSetConverterTimeEngine;

            Contract.Requires(convertorConsumer != null, "convertorConsumer != null");

            var adaptedRecords = new List<TimeRecord<TType>>();

            foreach (var record in toAdapt)
            {
                var time = record.Time.StampAsModifiedJulianDay;

                int successCode;
                var adapted = Client.AdaptDoubles(time, ToDoubles(record.Values).ToArray(), convertorConsumer.ValueArrayLength, out successCode);
                Utilities2.Native.ProcessSuccessCode("Adapt", successCode, Client.GetSuccessMessage);

                adaptedRecords.Add(new TimeRecord<TType>(record.Time, FromDoubles(adapted)));
            }

            return adaptedRecords;
        }
    }
}


