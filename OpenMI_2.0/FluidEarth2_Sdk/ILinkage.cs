using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public interface ILinkage : IIdentifiable
    {
        IBaseLinkableComponent Source { get; }
        IBaseLinkableComponent Target { get; }

        IBaseOutput OutputFirst { get; }
        IBaseOutput OutputLast { get; }
        IBaseInput Input { get; }

        /// <summary>
        // Remove connection with composition input/output
        /// </summary>
        void Detach(IComposition composition);

        void RemoveOrphanedAdapters(IComposition composition);

        /// <summary>
        /// In order IBaseOutput, IBaseAdaptedOutput, ..., IBaseInput
        /// </summary>
        /// <returns></returns>
        List<IBaseExchangeItem> Chainage { get; }

        /// <summary>
        /// In order Target ... Source
        /// </summary>
        IEnumerable<IBaseAdaptedOutput> Adapters { get; }

        IBaseOutput AdapteeLastValid { get; }

        string DetailsAsWikiText();
        bool IsValid(out string whyNot);
    }
}
