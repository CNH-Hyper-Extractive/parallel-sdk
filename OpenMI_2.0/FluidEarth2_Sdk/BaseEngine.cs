
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A base implementation of the  FluidEarth2.Sdk.Interfaces.IEngine 
    /// 
    /// Provides default implementations for many of the IEngine interfaces except for
    /// Update() which is abstract and so must be implemented by inherited class.
    /// </summary>
    public abstract class BaseEngine : IEngine
    {
        /// <summary>
        /// Arguments that will be passed to engine at runtime.
        /// </summary>
        List<IArgument> _arguments;
        /// <summary>
        /// Text supplied via Initialise(string) call
        /// </summary>
        XElement _initialisingXml;

        /// <summary>
        /// Simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Ping()
        /// </summary>
        /// <returns>The world is all ok for this class</returns>
        public virtual string Ping()
        {
            return "42";
        }

        /// <summary>
        /// Simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Initialise()
        /// 
        /// Override if specific additional functionality required.
        /// </summary>
        /// <param name="initialisingXml">Text required by engine to initialise itself.
        /// Currently assumes text is XML that can be passed to initialise IBaseEngine::Arguments</param>
        /// <param name="accessor">Provides context for the supplied XML, i.e. what URL did it have
        /// which can be used to resolve relative paths. Can legitimately be null, i.e. no context available.</param>
        public virtual void Initialise(string initialisingXml, IDocumentAccessor accessor)
        {
            _initialisingXml = XElement.Parse(initialisingXml);

            _arguments = Persistence.Arguments
                .Parse(_initialisingXml, accessor)
                .ToList();
        }

        /// <summary>
        /// Simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Initialise()
        /// 
        /// Override if specific additional functionality required.
        /// </summary>
        /// <param name="initialisingText">Text required by engine to initialise itself.
        /// Currently assumes text is XML that can be passed to initialise IBaseEngine::Arguments</param>
        public virtual void Initialise(string initialisingText)
        {
            Initialise(initialisingText, null);
        }

        /// <summary>
        /// Override to free engine resources on engine death.
        /// </summary>
        public virtual void Dispose()
        {
            Trace.TraceInformation("Dispose Engine Base");
        }

        /// <summary>
        /// Get Argument that matches supplied IIdentifiable.Id.
        /// Throws if not found.
        /// </summary>
        /// <param name="id">Identity to match Id against.</param>
        /// <returns>IArgument that matches IIdentifiable.Id</returns>
        public IArgument Argument(IIdentifiable id)
        {
            IArgument arg = _arguments
                .Where(a => a.Id == id.Id)
                .SingleOrDefault();

            if (arg == null)
                throw new Exception(string.Format("Cannot find argument \"{0}\" in argument list", id.Id));

            return arg;
        }

        /// <summary>
        /// The caption can be changed by the user in the UI or by localization.
        ///         
        /// Value managed by a IArgument stored in BaseEngine::Arguments
        /// </summary>
        public string ArgumentCaption
        {
            get
            {
                return (string)Argument(BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Caption)).Value;
            }
        }

        /// <summary>
        /// Any engine diagnostics required.
        /// 
        /// Value managed by a IArgument stored in BaseEngine::Arguments
        /// </summary>
        public ParametersDiagnosticsNative ArgumentDiagnostics
        {
            get
            {
                return ((ArgumentParametersDiagnosticsEngine)Argument(BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Diagnostics))).Parameters;
            }
        }

        /// <summary>
        /// What form of Remoting, if any, is engine to run under.
        /// 
        /// Value managed by a IArgument stored in BaseEngine::Arguments
        /// </summary>
        public ParametersRemoting ArgumentRemoting
        {
            get
            {
                return ((ArgumentParametersRemoting)Argument(BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Remoting))).ParametersRemoting;
            }
        }

        /// <summary>
        /// Get engine NativeDll value as specified by this managed NativeDll IArgument 
        /// 
        /// Value managed by a IArgument stored in BaseEngine::Arguments
        /// </summary>
        public ParametersNativeDll ArgumentNativeDll
        {
            get
            {
                return ((ArgumentNativeDll)Argument(BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.NativeDll))).ParametersNativeDll;
            }
        }

        #region New base methods

        /// <summary>
        /// Read only Argument collection used/required by the engine.
        /// </summary>
        public ReadOnlyCollection<IArgument> Arguments
        {
            get { return _arguments.AsReadOnly(); }
        }

        /// <summary>
        /// Text/XML that was provided by call to BaseEngine::Initialise(string)
        /// </summary>
        public XElement InitialisingXml
        {
            get { return _initialisingXml; }
        }

        /// <summary>
        /// Utility function to copy between generic flat arrays
        /// 
        /// Throws if arrays null or mismatched in size.
        /// </summary>
        /// <typeparam name="TType">base variable type for arrays</typeparam>
        /// <param name="engineVariable">Engine name for array, used in exception messages to aide debugging</param>
        /// <param name="from">Array to copy from.</param>
        /// <param name="to">array to copy too.</param>
        protected void CopyArray<TType>(string engineVariable, TType[] from, TType[] to)
        {
            if (from == null)
                throw new Exception(string.Format("Input values {0} are NULL", engineVariable));

            if (from.Length != to.Length)
                throw new Exception(string.Format(
                    "Input values {0} length mismatch: {1} != {2} (expected)",
                    engineVariable, from.Length, to.Length));

            from.CopyTo(to, 0);
        }

        /// <summary>
        /// Utility function to check generic flat array is expected size
        /// </summary>
        /// <typeparam name="TType">base variable type for arrays</typeparam>
        /// <param name="engineVariable">Engine name for array, used in exception messages to aide debugging</param>
        /// <param name="elementCount">Expected element count</param>
        /// <param name="vectorLength">Expected element length</param>
        /// <param name="values">Array to check</param>
        protected void IsCompatableArray<TType>(string engineVariable, int elementCount, int vectorLength, TType[] values)
        {
            if (values == null)
                throw new Exception(string.Format("Output values {0} are NULL", engineVariable));

            if (values.Length != elementCount * vectorLength)
                throw new Exception(string.Format(
                    "Output values {0} length mismatch: {1} != {2} == {3} * {4}",
                    engineVariable, values.Length, elementCount * vectorLength, elementCount, vectorLength));
        }

        #endregion New base methods

        /// <summary>
        /// Abstract implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Update()
        /// Must be defined by inherited class.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Finish()
        /// 
        /// Override if specific additional functionality required.
        /// </summary>
        public virtual void Finish()
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetArgument()
        /// 
        /// If using a .NET engine this is redundant as can use Arguments directly
        /// This argument is used to pass argument values to Native DLL implementations
        /// of the engine.
        /// 
        /// Override if specific additional functionality required.
        /// </summary>
        /// <param name="key">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="value">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetArgument(string key, string value)
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetInput(string,int,int,int)
        /// 
        /// MUST be overridden if engine supports IBaseInput's of fixed element length, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementValueCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="vectorLength">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetInput(string,int,int[],int)
        /// 
        /// MUST be overridden if engine supports IBaseInput's of variable element length, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementValueCounts">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="vectorLength">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetInput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetOutput(string,int,int,int)
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of fixed element length, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementValueCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="vectorLength">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetOutput(string,int,int[],int)
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of variable element length, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementCount">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="elementValueCounts">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="vectorLength">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetOutput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
        {
        }

        /// <summary>
        /// Do nothing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::Prepare()
        /// 
        /// Override if specific additional functionality required.
        /// </summary>
        public virtual void Prepare()
        {
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetStrings()
        /// 
        /// MUST be overridden if engine supports IBaseInput's of ValueType string, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="values">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetStrings(string engineVariable, string missingValue, string[] values)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetInt32s()
        /// 
        /// MUST be overridden if engine supports IBaseInput's of ValueType int, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="values">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetInt32s(string engineVariable, int missingValue, int[] values)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetDoubles()
        /// 
        /// MUST be overridden if engine supports IBaseInput's of ValueType double, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="values">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetDoubles(string engineVariable, double missingValue, double[] values)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::SetBooleans()
        /// 
        /// MUST be overridden if engine supports IBaseInput's of ValueType bool, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="values">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        public virtual void SetBooleans(string engineVariable, bool missingValue, bool[] values)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::GetStrings()
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of ValueType string, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <returns>See FluidEarth2.Sdk.Interfaces.IEngine</returns>
        public virtual string[] GetStrings(string engineVariable, string missingValue)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::GetInt32s()
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of ValueType int, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <returns>See FluidEarth2.Sdk.Interfaces.IEngine</returns>
        public virtual int[] GetInt32s(string engineVariable, int missingValue)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::GetDoubles()
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of ValueType double, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <returns>See FluidEarth2.Sdk.Interfaces.IEngine</returns>
        public virtual double[] GetDoubles(string engineVariable, double missingValue)
        {
            throw new NotImplementedException("Method not overridden");
        }

        /// <summary>
        /// Throwing simple implementation of interface FluidEarth2.Sdk.Interfaces.IEngine::GetBooleans()
        /// 
        /// MUST be overridden if engine supports IBaseOutput's of ValueType bool, else engine will throw at runtime.
        /// </summary>
        /// <param name="engineVariable">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <param name="missingValue">See FluidEarth2.Sdk.Interfaces.IEngine</param>
        /// <returns>See FluidEarth2.Sdk.Interfaces.IEngine</returns>
        public virtual bool[] GetBooleans(string engineVariable, bool missingValue)
        {
            throw new NotImplementedException("Method not overridden");
        }
    }
}
