using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class LinkableComponentTimeWithEngine : BaseComponentTimeWithEngine
    {
        public LinkableComponentTimeWithEngine(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType, bool useNativeDllArgument)
            : base(identity, derivedComponentType, engineType, useNativeDllArgument)
        {
        }
    }
}
