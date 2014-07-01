using System.Collections.Generic;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public interface IValidation : IIdentifiable
    {
        bool Succeeded { get; }
        bool Failed { get; }

        IList<string> Errors { get; }
        IList<string> Warnings { get; }
        IList<string> Details { get; }

        bool Validate(ITime getValuesAt);

        string Report();
    }
}
