using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ComponentState : Identity, IPersistence
    {
        public IExternalType ComponentType { get; set; }
        public IExternalType EngineType { get; set; }
        public bool UseNativeEngine { get; set; }

        public List<IArgument> Arguments { get; private set; }
        public List<IBaseInput> Inputs { get; private set; }
        public List<IBaseOutput> Outputs { get; private set; }

        public ComponentState()
        {
            ComponentType = new ExternalType();
            EngineType = new ExternalType();

            Arguments = new List<IArgument>();
            Inputs = new List<IBaseInput>();
            Outputs = new List<IBaseOutput>();
        }    
   
        public IBaseLinkableComponent Instantiate()
        {
            var identity = new Identity(string.Empty);
            var instantiationType = new ExternalType(typeof(ComponentStateTimeWithEngine));

            var component = new ComponentStateTimeWithEngine();

            foreach (var arg in Arguments)
                component.Arguments.Add(arg);

            component.Initialize();

            return component;
        }

        public const string XName = "ComponentState";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            var identity = Persistence.Identity.Parse(xElement, accessor);

            SetIdentity(identity);

            var xComponentType = xElement
                .Elements("ComponentType")
                .SingleOrDefault();

            ComponentType.Initialise(xComponentType, accessor);

            var xEngineType = xElement
                .Elements("EngineType")
                .SingleOrDefault();

            EngineType.Initialise(xEngineType, accessor);

            UseNativeEngine = Utilities.Xml.GetAttribute(xElement, "useNativeEngine", false);

            Arguments = Persistence.Arguments
                .Parse(xElement, accessor)
                .ToList();

            Inputs = Persistence.Inputs
                .Parse(xElement, accessor)
                .ToList();

            Outputs = Persistence.Outputs
                .Parse(xElement, accessor)
                .ToList();
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                new XElement("ComponentType", ComponentType.Persist(accessor)),
                new XElement("EngineType", EngineType.Persist(accessor)),
                new XAttribute("useNativeEngine", UseNativeEngine),
                Persistence.Arguments.Persist(Arguments, accessor),
                Persistence.Inputs.Persist(Inputs, accessor),
                Persistence.Outputs.Persist(Outputs, accessor));
        }
    }
}
