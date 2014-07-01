using System.Collections.Generic;
using System.Text;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public abstract class InterceptionBase
    {
        protected List<IIntercept> _intercepts;
        protected bool _active;

        public InterceptionBase(IEnumerable<IIntercept> intercepts, bool active)
        {
            Contract.Requires(intercepts != null, "intercepts != null");

            _intercepts = new List<IIntercept>(intercepts);
            _active = active;
        }

        public IEnumerable<IIntercept> Interceptors
        {
            get { return _intercepts; }
        }

        public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        #region Do for each intercept in turn

        protected void DoStart(string call, params object[] args)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Start(call, args);
        }

        protected void DoFinally()
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Finally();
        }

        protected void DoCatch(System.Exception exception)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Catch(exception);

            throw new Exception("Engine Exception", exception);
        }

        protected string DoFinalReport()
        {
            var sb = new StringBuilder();

            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    sb.AppendLine(i.FinalReport());

            return sb.ToString();
        }

        protected string DoValue(string value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected int DoValue(int value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected double DoValue(double value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected string[] DoValue(string[] value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected int[] DoValue(int[] value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected double[] DoValue(double[] value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        protected bool[] DoValue(bool[] value)
        {
            foreach (var i in _intercepts)
                if (i.To != WriteTo.None)
                    i.Value(value);

            return value;
        }

        #endregion
    }

}
