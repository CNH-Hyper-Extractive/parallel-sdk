
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ComponentTimeWithEngine : BaseComponentTimeWithEngine
    {
        public ComponentTimeWithEngine()
            : base(new Identity("FluidEarth2.Sdk.ComponentTimeWithEngine", "ComponentTimeWithEngine"),
                new ExternalType(typeof(ComponentTimeWithEngine)), new ExternalType())
        {
        }

        public ComponentTimeWithEngine(ExternalType engineType)
            : base(new Identity("FluidEarth2.Sdk.ComponentTimeWithEngine", "ComponentTimeWithEngine"),
                new ExternalType(typeof(ComponentTimeWithEngine)), engineType)
        {
        }

        public ComponentTimeWithEngine(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }
    }
}
