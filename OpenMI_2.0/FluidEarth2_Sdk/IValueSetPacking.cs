
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public interface IValueSetPacking : IPersistence
    {
        int ElementCount { get; }
        int VectorLength { get; }

        /// <summary>
        /// Elements in a ElementSet can vary, if so this is false
        /// e.g. An ElementSet of Polylines of different segment lengths and a value for each segment
        /// </summary>
        bool ElementValueCountConstant { get; }
        int ElementValueCount { get; }
        int[] ElementValueCounts { get; }

        int ValueArrayLength { get; }

        object MissingValue { get; }
    }
}
