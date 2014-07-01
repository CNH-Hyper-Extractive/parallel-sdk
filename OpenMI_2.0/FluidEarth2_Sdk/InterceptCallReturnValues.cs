using System.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptCallReturnValues : InterceptCallBase
    {
        string _currentCall;

        public InterceptCallReturnValues(ParametersDiagnostics diagnostics)
            : base(diagnostics)
        { }

        public override void Start(string call, params object[] args)
        {
            Contract.Requires(call != null, "call != null");

            _currentCall = call;
        }

        public override void Finally()
        {
            _currentCall = string.Empty;
        }

        public override string Value(string value)
        {
            var line = value == null
                ? string.Format("{0}() = string is null", _currentCall)
                : string.Format("{0}() = {1} characters", _currentCall, value.Length.ToString());

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }

        public override double Value(double value)
        {
            var line = string.Format("{0}() = {1}", _currentCall, value.ToString());

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }

        public override string[] Value(string[] value)
        {
            var line = value == null
                ? string.Format("{0}() = string[] is null", _currentCall)
                : string.Format("{0}() = string[{1}]", _currentCall, value.Length.ToString());

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }

        public override bool[] Value(bool[] value)
        {
            string line;

            if (value == null)
                line = string.Format("{0}() = bool[] is null", _currentCall);
            else
            {
                var trues = value.Count(b => b == true);
                line = string.Format("{0}() = bool[{1}], {2} on",
                    _currentCall, value.Length.ToString(), trues.ToString());
            }

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }

        public override double[] Value(double[] value)
        {
            string line;

            if (value == null)
                line = string.Format("{0}() = double[] is null",
                    _currentCall.ToString());
            else
                line = string.Format("{0}() = double[{1}], [{2} ... {3}], Ave = {4}",
                    _currentCall, value.Count().ToString(),
                    value.Min().ToString(), value.Max().ToString(), value.Average().ToString());

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }

        public override int[] Value(int[] value)
        {
            string line;

            if (value == null)
                line = string.Format("{0}() = int[] is null", _currentCall);
            else
                line = string.Format("{0}() = int[{1}], [{2} ... {3}], Ave = {4}",
                    _currentCall, value.Count().ToString(),
                    value.Min().ToString(), value.Max().ToString(), value.Average().ToString());

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);

            return base.Value(value);
        }
    }
}
