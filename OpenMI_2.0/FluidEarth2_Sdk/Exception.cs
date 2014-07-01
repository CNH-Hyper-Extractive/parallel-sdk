using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Exception which can only be thrown from inside this assembly
    /// </summary>
    public class Exception : ExceptionBase
    {
        /// <summary>
        /// Only constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="inner">Inner Exception</param>
        /// <param name="reports">Additional data as IReport's</param>
        internal Exception(string message = "", System.Exception inner = null, IEnumerable<IReport> reports = null)
            : base(typeof(Exception), message, inner, reports)
        { }
    }

    /// <summary>
    /// Contracts which can only be expressed from inside this assembly
    /// </summary>
    public static class Contract
    {
        /// <summary>
        /// Contract that developer expects previous code to have been already honoured 
        /// </summary>
        /// <param name="condition">True if contract has been honoured</param>
        /// <param name="format">string Format expression for message to display if contract failed</param>
        /// <param name="values">Arguments for Format expression</param>
        internal static void Requires(bool condition, string format, params object[] values)
        {
            ContractBase.Requires(typeof(Contract), condition, format, values);
        }

        /// <summary>
        /// Contract that developer expects previous code to have been already honoured 
        /// </summary>
        /// <param name="condition">True if contract has been honoured</param>
        /// <param name="conditionAsString">Message to display if contract failed</param>
        internal static void Requires(IExternalType derivedClass, bool condition, string conditionAsString)
        {
            ContractBase.Requires(typeof(Contract), condition, conditionAsString);
        }
    }
}