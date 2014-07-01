using System;
using System.Diagnostics;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptCallEngineServer : InterceptCallBase
    {
        System.Exception _lastException;

        public InterceptCallEngineServer(ParametersDiagnostics diagnostics)
            : base(diagnostics)
        { }

        public override void Catch(System.Exception exception)
        {
            Trace.TraceError(_lastException.ToString());
            Console.WriteLine(_lastException.ToString());

            _lastException = exception;
        }

        public System.Exception LastException
        {
            get { return _lastException; }
        }
    }
}
