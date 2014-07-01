using System.Collections.Generic;
using System.IO;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public abstract class InterceptCallBase : IIntercept
    {
        ParametersDiagnostics _diagnostics;
        List<Stream> _streams;

        public InterceptCallBase(ParametersDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
            _streams = new List<Stream>();
        }

        public string Caption
        {
            get { return _diagnostics.Caption; }
            set { _diagnostics.Caption = value; }
        }

        public WriteTo To
        {
            get { return _diagnostics.To; }
            set { _diagnostics.To = value; }
        }

        public IList<Stream> Streams
        {
            get { return _streams; }
        }

        public virtual void Start(string call, params object[] args)
        { }

        public virtual void Finally()
        { }

        public virtual string FinalReport()
        {
            return string.Empty;
        }

        public virtual string Value(string value)
        {
            return value;
        }

        public virtual int Value(int value)
        {
            return value;
        }

        public virtual double Value(double value)
        {
            return value;
        }

        public virtual string[] Value(string[] value)
        {
            return value;
        }

        public virtual int[] Value(int[] value)
        {
            return value;
        }

        public virtual double[] Value(double[] value)
        {
            return value;
        }

        public virtual bool[] Value(bool[] value)
        {
            return value;
        }

        public virtual void Catch(System.Exception exception)
        { }

        public void Dispose()
        { }
    }
}
