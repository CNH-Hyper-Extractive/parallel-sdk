
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ComponentWithEngine : BaseComponentWithEngine
    {
        public ComponentWithEngine()
            : base(new Identity("FluidEarth2.Sdk.ComponentWithEngine", "ComponentWithEngine"),
                new ExternalType(typeof(ComponentWithEngine)), new ExternalType())
        {
        }

        public ComponentWithEngine(ExternalType engineType)
            : base(new Identity("FluidEarth2.Sdk.ComponentWithEngine", "ComponentWithEngine"),
                new ExternalType(typeof(ComponentWithEngine)), engineType)
        {
        }

        public ComponentWithEngine(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }
    }
}
