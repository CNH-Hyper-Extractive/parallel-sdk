using System;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ParametersNativeDll : IPersistence
    {
        IExternalType _nativeDllImplementingNetAssembly;
        Interface _interface = Interface.FluidEarth2_Sdk_Interfaces_IEngineTime;
        bool _debuggerLaunch;

        public enum Interface
        {
            FluidEarth2_Sdk_Interfaces_IEngineTime = 0,
            FluidEarth2_Sdk_Interfaces_IEngine,
            // FluidEarth_Sdk_Interfaces_IEngine5
        }

        public ParametersNativeDll()
        { }

        public ParametersNativeDll(IExternalType nativeDll, Interface inter, Uri dataFile, bool debuggerLaunch)
        {
            if (nativeDll == null)
                nativeDll = new ExternalType();

            _nativeDllImplementingNetAssembly = (ExternalType)nativeDll.Clone();
            _interface = inter;
            _debuggerLaunch = debuggerLaunch;
        }

        public const string XName = "NativeDllArgument";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            string inter = Utilities.Xml.GetAttribute(xElement, "interface");

            _interface = (Interface)Enum.Parse(typeof(Interface), inter);

            _nativeDllImplementingNetAssembly = null;

            XElement xdllDotNetStub = xElement
                .Elements("ExternalType")
                .SingleOrDefault();

            if (xdllDotNetStub != null)
            {
                _nativeDllImplementingNetAssembly = new ExternalType(accessor);
                _nativeDllImplementingNetAssembly.Initialise(xdllDotNetStub, accessor);
            }

            _debuggerLaunch = Utilities.Xml.GetAttribute(xElement, "debuggerLaunch", false);
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            XElement xml = new XElement(XName,
                new XAttribute("interface", _interface.ToString()));

            if (_nativeDllImplementingNetAssembly != null)
                xml.Add(_nativeDllImplementingNetAssembly.Persist(accessor));

            if (_debuggerLaunch)
                xml.Add(new XAttribute("debuggerLaunch", _debuggerLaunch.ToString()));

            return xml;
        }

        public Interface ImplementsInterface
        {
            get { return _interface; }
            set { _interface = value; }
        }

        public IExternalType NativeDll_ImplementingNetAssembly
        {
            get { return _nativeDllImplementingNetAssembly; }
            set { _nativeDllImplementingNetAssembly = value; }
        }

        public bool DebuggerLaunch
        {
            get { return _debuggerLaunch; }
            set { _debuggerLaunch = value; }
        }

        public override string ToString()
        {
            return _nativeDllImplementingNetAssembly != null
                ? _nativeDllImplementingNetAssembly.ToString()
                : base.ToString();
        }

        public virtual EValidation Validate(out string message)
        {
            if (_nativeDllImplementingNetAssembly != null)
                return _nativeDllImplementingNetAssembly.Validate(out message);

            message = "Native dll implementing .NET assembly unspecified";

            return EValidation.Error;
        }
    }
}
