using System.Linq;
using System.Text;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptCallArguments : InterceptCallBase
    {
        public InterceptCallArguments(ParametersDiagnostics diagnostics)
            : base(diagnostics)
        { }

        public override void Start(string call, params object[] args)
        {
            Contract.Requires(call != null, "call != null");

            string line;

            if (args == null || args.Count() == 0)
                line = string.Format("{0}()", call);
            else
            {
                var csv = args
                    .Aggregate(new StringBuilder(), (sb, v) => sb.Append(v.ToString() + ","))
                    .ToString()
                    .TrimEnd(',');
                line = string.Format("{0}({1})", call, csv);
            }

            Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);
        }

        public override void Finally()
        { }
    }
}
