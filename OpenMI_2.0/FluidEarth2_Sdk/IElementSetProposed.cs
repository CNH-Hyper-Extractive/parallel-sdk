using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;

namespace FluidEarth2.Sdk
{
    /// <summary>         
    /// As ElementSets can be large require an explicit implementation of IClonable and encourage
    /// implementation of UpdateGeometryAvailable for when edits can be allowed.
    /// </summary>
    public interface IElementSetProposed : IElementSet, ICloneable
    {
        /// <summary>
        /// If the ElementSet requires argument values to instantiate itself should return requires arguments here.
        /// </summary>
        IList<IArgument> Arguments { get; }

        /// <summary>
        /// If supports Arguments then Initialise will update state based on arguments currently set values.
        /// </summary>
        void Initialise();

        /// <summary>
        /// If true user can expect class to implement IClonable interface and bool UpdateGeometry(IElementSet) method
        /// for this class.
        /// </summary>
        /// <param name="elementSetEdits">Want to Update from</param>
        bool UpdateGeometryAvailable(IElementSet elementSetEdits);

        /// <summary>
        /// Update this using elementSetEdits. Typically elementSetEdits might have un-optimised 
        /// geometry storage suitable for editing but not for final implementation.
        /// Throw if update fails.
        /// </summary>
        /// <param name="elementSetEdits">Update from</param>
        void UpdateGeometry(IElementSet elementSetEdits);
    }
}
