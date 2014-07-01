using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public abstract class ValidationBase : Identity, IValidation
    {
        public abstract bool DoValidation(ITime getValuesAt);

        public ValidationBase(IIdentifiable identity)
            : base(identity)
        { }

        protected List<string> _errors = new List<string>();
        protected List<string> _warnings = new List<string>();
        protected List<string> _details = new List<string>();

        public IList<string> Errors
        {
            get { return _errors; }
        }

        public IList<string> Warnings
        {
            get { return _warnings; }
        }

        public IList<string> Details
        {
            get { return _details; }
        }

        public string AddError
        {
            set { _errors.Add(value); }
        }

        public string AddWarning
        {
            set { _warnings.Add(value); }
        }

        public string AddDetail
        {
            set { _details.Add(value); }
        }

        public bool Validate(ITime getValuesAt)
        {

            _errors.Clear();
            _warnings.Clear();
            _details.Clear();

            return DoValidation(getValuesAt);
        }

        public void ProcessValidateMessage(string message)
        {
            try
            {
                var xml = XElement.Parse(message);

                switch (xml.Name.LocalName.ToLower())
                {
                    case "detail":
                        AddDetail = xml.Value;
                        break;
                    case "warning":
                        AddWarning = xml.Value;
                        break;
                    case "error":
                        AddError = xml.Value;
                        break;
                    default:
                        AddError = message;
                        break;
                }
            }
            catch (System.Exception)
            {
                AddError = message;
            }
        }

        public string Report()
        {
            var sb = new StringBuilder();
            AddWikiText(sb);
            return sb.ToString();
        }

        public string AddWikiText(StringBuilder sb)
        {
            sb.AppendLine("== Validation: " + Caption);
            sb.AppendLine("* " + DateTime.Now.ToString("u"));
            if (_errors.Count == 0)
                sb.AppendLine("* Valid");
            else
                sb.AppendLine("* INVALID");

            if (_errors.Count > 0)
            {
                sb.AppendLine("=== Errors");

                foreach (var e in _errors)
                    sb.AppendLine(Sdk.Utilities.IsProbablyWikiText(e)
                        ? e : "# " + e);
            }

            if (_warnings.Count > 0)
            {
                sb.AppendLine("=== Warnings");

                foreach (var w in _warnings)
                    sb.AppendLine(Sdk.Utilities.IsProbablyWikiText(w)
                        ? w : "# " + w);
            }

            if (_details.Count > 0)
            {
                sb.AppendLine("=== Further details");

                foreach (var d in _details)
                    sb.AppendLine(Sdk.Utilities.IsProbablyWikiText(d)
                        ? d : "# " + d);
            }

            return sb.ToString();
        }

        public bool Succeeded
        {
            get { return _errors.Count == 0; }
        }

        public bool Failed
        {
            get { return !Succeeded; }
        }
    }
}
