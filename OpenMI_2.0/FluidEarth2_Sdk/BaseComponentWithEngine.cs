
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public abstract class BaseComponentWithEngine : CoreStandard2.BaseComponent, IPersistence
    {
        IExternalType _derivedComponentType;
        IExternalType _engineType;
        bool _useNativeDll;
        Argument<string> _argCaption;

        WriteTo _writeTo = WriteTo.None;
        Stream _diagnosticsStream = null;

        IEngine _engine;

        /// <summary>
        /// Initialised at run time Prepare, active means connected to a 
        /// valid external provider or consumer 
        /// </summary>
        protected List<IBaseInput> _activeInputs =
            new List<IBaseInput>();
        protected List<IBaseOutput> _activeOutputs =
            new List<IBaseOutput>();

        public enum ArgsWithEngine { 
            Caption = 0, Diagnostics, Remoting, NativeDll,
            LaunchDebugger,
        }

        public static IIdentifiable GetArgumentIdentity(ArgsWithEngine key)
        {
            switch (key)
            {
                case ArgsWithEngine.Caption:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "Caption", "Component caption");
                case ArgsWithEngine.Diagnostics:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "Diagnostics", "Monitoring and Debugging options");
                case ArgsWithEngine.Remoting:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "Remoting", "Out of process 'remoting' settings");
                case ArgsWithEngine.NativeDll:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "NativeDll", "Use External C dll to access engine");
                case ArgsWithEngine.LaunchDebugger:
                    return new Identity("FluidEarth2.Sdk.BaseComponentWithEngine." + key.ToString(),
                        "Launch Debugger", "Launch a Debugger (unless one already attached)");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public BaseComponentWithEngine()
        { }

        public BaseComponentWithEngine(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType)
            : this(identity, derivedComponentType, engineType, false)
        { }

        void SetArgCaption()
        {
            _argCaption = new Argument<string>(GetArgumentIdentity(ArgsWithEngine.Caption), base.Caption);
            _argCaption.ValueChanged += new EventHandler<ArgumentBase.ValueChangedEventArgs>(OnCaptionArgumentChanged);
        }

        public BaseComponentWithEngine(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType, bool useNativeDllArgument)
            : base(identity, true, null, null, null, null)
        {
            _derivedComponentType = derivedComponentType;
            _engineType = engineType;

            _useNativeDll = useNativeDllArgument;

            SetArgCaption();

            ArgumentsAddRange(new IArgument[] 
            { 
                _argCaption,
                new ArgumentParametersDiagnosticsEngine(GetArgumentIdentity(ArgsWithEngine.Diagnostics),
                    new ParametersDiagnosticsNative()),          
                new ArgumentParametersRemoting(GetArgumentIdentity(ArgsWithEngine.Remoting), 
                    new ParametersRemoting()),  
            });

            if (_useNativeDll)
                Arguments.Add(new ArgumentNativeDll(GetArgumentIdentity(ArgsWithEngine.NativeDll),
                    new ParametersNativeDll()));
        }

        public bool UseNativeDll
        {
            get { return _useNativeDll; }
            set
            {
                if (_useNativeDll != value)
                {
                    if (_useNativeDll)                 
                    {
                        var arg = Arguments
                            .Where(z => z.GetType() == typeof(ArgumentNativeDll))
                            .Single();

                        Arguments.Remove(arg);             
                    }
                    else
                        Arguments.Add(new ArgumentNativeDll(GetArgumentIdentity(ArgsWithEngine.NativeDll),
                            new ParametersNativeDll()));

                    _useNativeDll = value;
                }
            }
        }


        void OnCaptionArgumentChanged(object sender, ArgumentBase.ValueChangedEventArgs e)
        {
            base.Caption = e.ValueAsStringNew;
        }

        public new string Caption
        {
            get { return _argCaption.ValueAsString; }
            set 
            { 
                _argCaption.ValueAsString = value;
                base.Caption = value;
            }
        }

        public BaseComponentWithEngine(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public ParametersDiagnosticsNative Diagnostics
        {
            get
            {
                return ((ArgumentParametersDiagnosticsEngine)Argument(GetArgumentIdentity(ArgsWithEngine.Diagnostics))).Parameters;
            }

            set
            {
                ((ArgumentParametersDiagnosticsEngine)Argument(GetArgumentIdentity(ArgsWithEngine.Diagnostics))).Parameters = value;
            }
        }

        public ParametersRemoting ArgumentParametersRemoting
        {
            get
            {
                return ((ArgumentParametersRemoting)Argument(GetArgumentIdentity(ArgsWithEngine.Remoting))).ParametersRemoting;
            }

            set
            {
                ((ArgumentParametersRemoting)Argument(GetArgumentIdentity(ArgsWithEngine.Remoting))).ParametersRemoting = value;
            }
        }

        public ParametersNativeDll ArgumentParametersNativeDll
        {
            get
            {
                if (!_useNativeDll)
                    throw new Exception("Invalid usage, component has no native dll");

                return ((ArgumentNativeDll)Argument(GetArgumentIdentity(ArgsWithEngine.NativeDll))).ParametersNativeDll;
            }

            set
            {
                if (!_useNativeDll)
                    throw new Exception("Invalid usage, component has no native dll");

                ((ArgumentNativeDll)Argument(GetArgumentIdentity(ArgsWithEngine.NativeDll))).ParametersNativeDll = value;
            }
        }

        public IDocumentAccessor DocumentAccessor
        {
            get { return _derivedComponentType.DocumentAccessor; }
            set
            {
                _derivedComponentType.DocumentAccessor = value;

                if (_engineType != null)
                    _engineType.DocumentAccessor = value;
            }
        }

        public IExternalType EngineType
        {
            get { return _engineType; }
            set { _engineType = value; }
        }

        public XElement OmiElement
        {
            get
            {
                if (Status == LinkableComponentStatus.Created)
                    throw new Exception(
                        "Component must be initialised before creation of OMI file so that Arguments have meaningful values");

                var remoteData = ArgumentParametersRemoting;

                return Omi.Component.Persist(
                    this,
                    Utilities.Remoting.Platforms(remoteData.Protocol),
                    _derivedComponentType.DocumentAccessor);
            }
        }

        public XDocument OmiDocument
        {
            get
            {
                return new XDocument(OmiElement);
            }
        }

        /// <summary>
        /// Component/Engine specific validations
        /// </summary>
        /// <param name="errors">Errors</param>
        /// <param name="warnings">Warnings</param>
        /// <param name="details">Useful information</param>
        /// <returns></returns>
        public virtual bool Validate(List<string> errors, List<string> warnings, List<string> details)
        {
            /* Was
             *  SortedSet<IIdentifiable> addMoreInfo = new SortedSet<IIdentifiable>(
                    new Utilities.FuncComparer<IIdentifiable>((a,b) => a.Id.CompareTo(b.Id)));
             * 
             * in .NET 4.0, but now needs to be .NET 3.5 so more verbose
             */

            List<IIdentifiable> addMoreInfo = new List<IIdentifiable>();
       
            string message;

            foreach (IArgument arg in Arguments)
                switch (ValidArgumentValue(arg, out message))
                {
                    case ValidArgumentMessage.Error:
                        errors.Add(message);
                        addMoreInfo.Add(arg);
                        break;
                    case ValidArgumentMessage.Warning:
                        warnings.Add(message);
                        addMoreInfo.Add(arg);
                        break;
                    case ValidArgumentMessage.OK:
                        break;
                    default:
                        throw new NotImplementedException();
                }
                    
            if (addMoreInfo.Count > 0)
            {

                addMoreInfo.Sort(new Utilities.FuncComparer<IIdentifiable>((a, b) => a.Id.CompareTo(b.Id)));
                addMoreInfo.Distinct(new Identity.CompareIds());

                StringBuilder sb = new StringBuilder();

                foreach (IIdentifiable identity in addMoreInfo)
                {
                    sb.AppendLine(string.Format("* About Argument \"{0}\"", identity.Caption));
                    sb.AppendLine("** Id");
                    sb.AppendLine("*** " + identity.Id);
                    sb.AppendLine("** Description");
                    sb.AppendLine("*** " + identity.Description);
                }

                details.Add(sb.ToString());
            }

            return errors.Count == 0;
        }

        protected enum ValidArgumentMessage { OK = 0, Warning, Error, }

        protected virtual ValidArgumentMessage ValidArgumentValue(IArgument arg, out string message)
        {
            message = string.Empty;
            return ValidArgumentMessage.OK;
        }

        protected virtual IEngine NewRemotingEngine()
        {
            return new RemotingClientEngine();
        }

        protected FluidEarth2.Sdk.Interfaces.IEngine Engine
        {
            get
            {
                if (Status == LinkableComponentStatus.Finishing
                    || Status == LinkableComponentStatus.Finished)
                    throw new Exception("Attempt to access remote engine after entering Finishing state");

                if (_engine == null)
                {
                    if (_useNativeDll)
                    {
                        var nativeDll = ArgumentParametersNativeDll;

                        if (nativeDll.NativeDll_ImplementingNetAssembly != null)
                            _engineType = nativeDll.NativeDll_ImplementingNetAssembly;
                    }

                    var remoteData = ArgumentParametersRemoting;

                    if (remoteData.Protocol != RemotingProtocol.inProcess)
                        _engine = NewRemotingEngine();
                    else
                    {
                        Type type;
                        object engine = _engineType.CreateInstance(out type);

                        _engine = engine as IEngine;

                        Contract.Requires(_engine != null,
                            "{0} is not derived from IEngine or IEngineTime",
                            type.ToString());
                    }
                }

                return _engine;
            }
        }

        protected override void DoInitialise(bool reinitialising)
        {
            if (Diagnostics.TraceStatus)
                StatusChanged += new EventHandler<LinkableComponentStatusChangeEventArgs>(OnComponentStatusChangedEvent);

            if (Diagnostics.TraceExchangeItems)
                SetOnItemChangedEvent = OnItemChangedEvent;
        }

        protected override bool DoValidate(out string[] messages)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            List<string> details = new List<string>();

            Status = Validate(errors, warnings, details)
                ? LinkableComponentStatus.Valid
                : LinkableComponentStatus.Invalid;

            var m = new List<string>();

            m.AddRange(errors.Select(e => new XElement("Error", e).ToString()));
            m.AddRange(warnings.Select(w => new XElement("Warning", w).ToString()));
            m.AddRange(details.Select(i => new XElement("Detail", i).ToString()));

            messages = m.ToArray();

            return errors.Count == 0;
        }

        protected override void DoPrepare()
        {
            // Use Engine to force initialisation if null
            _engine = Utilities.Diagnostics.AddDiagnostics(
                Diagnostics, Engine,
                out _diagnosticsStream, false);

            Engine.Initialise(EngineInitialisingXml(DocumentAccessor).ToString());

            ArgumentStandard1 arg1;

            foreach (var arg in Arguments)
            {
                arg1 = arg as ArgumentStandard1;

                if (arg1 != null)
                    Engine.SetArgument(arg1.Key, arg1.Value);
                else
                    Engine.SetArgument(arg.Id, arg.ValueAsString);

                if (arg.Id.Contains(":SplitOn"))
                {
                    // FORTRAN struggles on its string manipulations
                    // so split up here and also pass individually

                    int n = arg.Id.IndexOf(":SplitOn") + 8;
                    if (n >= arg.Id.Length)
                        throw new Exception("Invalid :SplitOn in key " + arg.Id);

                    var parts = arg.ValueAsString.Split(arg.Id[n]);

                    for (int i = 0; i < parts.Length; ++i)
                        Engine.SetArgument(
                            string.Format("{0}.Part{1}",
                            arg.Id.Substring(0, arg.Id.LastIndexOf(':')), i + 1),
                            parts[i]);
                }
            }

            if (Inputs.Any(i => !(i is IHasValueSetConvertor)))
                throw new Exception("Has IBaseInput that does not implement IValueSetConvertor!");
            if (Outputs.Any(i => !(i is IHasValueSetConvertor)))
                throw new Exception("Has IBaseOutput that does not implement IValueSetConvertor!");

            _activeInputs = Inputs
                .Where(i => i.Provider != null)
                .ToList();

            _activeOutputs = Outputs
                .Where(o => o.Consumers.Count > 0 || o.AdaptedOutputs.Count > 0)
                .ToList();

            Contract.Requires(_activeOutputs.Count > 0, 
                "Component must have at least one output with a consumer." 
                + " Has Prepare() been called before connecting the consumers?");

            if (_onItemChangedEvent != null)
            {
                foreach (var active in _activeInputs)
                    active.ItemChanged += _onItemChangedEvent;
                foreach (var active in _activeOutputs)
                    active.ItemChanged += _onItemChangedEvent;
            }

            IValueSetConverter convertor;

            _activeInputs.ForEach(i =>
            {
                convertor = ((IHasValueSetConvertor)i).ValueSetConverter;

                if (_onItemChangedEvent != null)
                    convertor.ItemChanged += _onItemChangedEvent;

                var engineConvertor = convertor as IValueSetConverterTimeEngine;

                if (engineConvertor != null)
                {
                    if (engineConvertor.ElementValueCountConstant)
                        Engine.SetInput(
                            engineConvertor.EngineVariable,
                            engineConvertor.ElementCount,
                            engineConvertor.ElementValueCount,
                            engineConvertor.VectorLength);
                    else
                        Engine.SetInput(
                            engineConvertor.EngineVariable,
                            engineConvertor.ElementCount,
                            engineConvertor.ElementValueCounts,
                            engineConvertor.VectorLength);
                }
            });

            _activeOutputs.ForEach(o =>
            {
                convertor = ((IHasValueSetConvertor)o).ValueSetConverter;

                if (_onItemChangedEvent != null)
                    convertor.ItemChanged += _onItemChangedEvent;

                var engineConvertor = convertor as IValueSetConverterTimeEngine;

                if (engineConvertor != null)
                {
                    if (engineConvertor.ElementValueCountConstant)
                        Engine.SetOutput(
                            engineConvertor.EngineVariable,
                            engineConvertor.ElementCount,
                            engineConvertor.ElementValueCount,
                            engineConvertor.VectorLength);
                    else
                        Engine.SetOutput(
                            engineConvertor.EngineVariable,
                            engineConvertor.ElementCount,
                            engineConvertor.ElementValueCounts,
                            engineConvertor.VectorLength);
                }

                foreach (var y in Outputs.Where(x => x.Consumers.Count > 0 || x.AdaptedOutputs.Count > 0))
                    foreach (IBaseAdaptedOutput a in y.AdaptedOutputs)
                        a.Initialize();
            });

            if (Diagnostics.LaunchDebugger)
                Debugger.Launch();

            Engine.Prepare();

            /*
             * Populate all active output caches with values for engine start time
             * These values are needed to smooth the start up of compositions
             * with bidirectional links. Cant extrapolate with no values in cache.
             */

            _activeOutputs.ForEach(o => 
                EngineConvertor(o).CacheEngineValues(Engine));
        }

        public static IValueSetConverter Convertor(IBaseExchangeItem item)
        {
            var has = item as IHasValueSetConvertor;

            if (has == null || has.ValueSetConverter == null)
                throw new Exception(item.Caption + " does not have a IValueSetConverter");

            return ((IHasValueSetConvertor)item).ValueSetConverter;
        }

        public static IValueSetConverterTimeEngine EngineConvertor(IBaseExchangeItem item)
        {
            var convertor = Convertor(item) as IValueSetConverterTimeEngine;

            if (convertor == null)
                throw new Exception(item.Caption + " does not have a IValueSetConverterEngine");
            
            return convertor;          
        }

        protected override void DoUpdate(IEnumerable<IBaseOutput> requiredOutput)
        {
            // Not time base component so force 1 and 1 only update

            // Update engine with values from input exchange item 

            if (_activeInputs.Count > 0)
                throw new Exception("Engine does not implement time base inputs");

            /* How, see above exception
             * 
            _activeInputs.ForEach(i =>
                EngineConvertor(i).ToEngine(Engine, 
                    i.Values as OpenMI.Standard2.TimeSpace.ITimeSpaceValueSet));
             */

            Engine.Update();

            // Update output cache with values from engine
            // Update all not just required, as could improve interpolations

            _activeOutputs.ForEach(o =>
                EngineConvertor(o).CacheEngineValues(Engine));
        }

        protected override void DoFinish()
        {
            if (_engine != null)
            {
                _engine.Finish();
                _engine.Dispose();
                _engine = null;

                if (_diagnosticsStream != null)
                {
                    _diagnosticsStream.Close();
                    _diagnosticsStream = null;
                }
            }
        }

        protected override void DoCatchTidy(System.Exception e)
        {
            if (_engine != null)
            {
                _engine.Dispose();
                _engine = null;

                if (_diagnosticsStream != null)
                {
                    _diagnosticsStream.Close();
                    _diagnosticsStream = null;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_engine != null)
            {
                _engine.Finish();
                _engine.Dispose();
                _engine = null;
            }

            if (_diagnosticsStream != null)
            {
                _diagnosticsStream.Close();
                _diagnosticsStream = null;
            }
        }

        #region Add Exchange items

        public void Add(BaseInput input)
        {
            if (_inputs.Any(e => e.Id == input.Id))
                throw new Exception("Already specified input: " + input.Id);

            // use protected member directly to avoid component status check on Inputs
            _inputs.Add(input);
        }

        public void Add(BaseOutput output)
        {
            if (_outputs.Any(e => e.Id == output.Id))
                throw new Exception("Already specified output: " + output.Id);

            // use protected member directly to avoid component status check on Outputs
            _outputs.Add(output);
        }

        public void AddRange(IEnumerable<BaseInput> inputs)
        {
            foreach (BaseInput input in inputs)
                Add(input);
        }

        public void AddRange(IEnumerable<BaseOutput> outputs)
        {
            foreach (BaseOutput output in outputs)
                Add(output);
        }

        #endregion Add Exchange items


        public virtual XElement EngineInitialisingXml(IDocumentAccessor accessor)
        {
            return new XElement("Engine",
                _engineType.Persist(accessor),
                Persistence.Arguments.Persist(Arguments, accessor));
        }

        public virtual void OnComponentStatusChangedEvent(object sender, LinkableComponentStatusChangeEventArgs e)
        {
            if (Diagnostics.To == WriteTo.None)
                return;

            var line = string.Format(
                "Event Status {0} => {1}",
                e.OldStatus.ToString(), e.NewStatus.ToString());

            Utilities.Diagnostics.WriteLine(
                Utilities.Diagnostics.DatedLine(Diagnostics.Caption, line), 
                Diagnostics.To, 
                new Stream[] {_diagnosticsStream});
        }

        public virtual void OnItemChangedEvent(object sender, ExchangeItemChangeEventArgs e)
        {
            if (Diagnostics.To == WriteTo.None)
                return;

            string component = e.ExchangeItem.Component != null ? e.ExchangeItem.Component.Caption : "Orphan";

            var sb = new StringBuilder(
                string.Format("Event Item: {0}", e.Message));

            var e2 = e as BaseExchangeItemChangeEventArgs;

            if (e2 != null)
            {
                for (int n = 0; n < e2.Messages.Count; ++n)
                {
                    sb.AppendLine();
                    sb.Append(n == 0 ? "\t" + e2.Messages[n] : "\t\t" + e2.Messages[n]);
                }
            }

            Utilities.Diagnostics.WriteLine(
                Utilities.Diagnostics.DatedLine(Diagnostics.Caption, sb.ToString()),
                Diagnostics.To,
                new Stream[] { _diagnosticsStream });
        }

        public const string XName = "BaseComponentWithEngine";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            IEnumerable<IArgument> arguments;
            IEnumerable<IBaseInput> inputs;
            IEnumerable<IBaseOutput> outputs;

            Identity = Persistence.BaseComponent.Parse(xElement, accessor, out arguments, out inputs, out outputs);

            Arguments = arguments.ToList();

            SetArgCaption();

            Inputs = inputs.ToList();
            Outputs = outputs.ToList();

            foreach (var i in Inputs)
                if (i is BaseExchangeItem)
                    ((BaseExchangeItem)i).Component = this;

            foreach (var i in Outputs)
                if (i is BaseExchangeItem)
                    ((BaseExchangeItem)i).Component = this;

            _useNativeDll = Utilities.Xml.GetAttribute(xElement, "useNativeDllArgument", false);

            int traceTo = int.Parse(Utilities.Xml.GetAttribute(xElement, "traceTo"));

            _writeTo = (WriteTo)traceTo;
                           
            var derivedComponentType = xElement
                .Elements("DerivedComponentType")
                .SingleOrDefault();

            _derivedComponentType = derivedComponentType != null 
                ? new ExternalType(derivedComponentType, accessor) 
                : null;

            var engineType = xElement
                .Elements("EngineType")
                .SingleOrDefault();

            _engineType = engineType != null
                ? new ExternalType(engineType, accessor)
                : null;

            _engine = null;

            _activeInputs = new List<IBaseInput>();
            _activeOutputs = new List<IBaseOutput>();
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            int traceTo = (int)_writeTo;

            var xml = new XElement(XName,              
                Persistence.BaseComponent.Persist(this, accessor),
                new XAttribute("useNativeDllArgument", _useNativeDll),
                new XAttribute("traceTo", traceTo.ToString()));

            if (_derivedComponentType != null)
                xml.Add(new XElement("DerivedComponentType", _derivedComponentType.Persist(accessor)));
            if (_engineType != null)
                xml.Add(new XElement("EngineType", _engineType.Persist(accessor)));

            return xml;
        }
    }
}