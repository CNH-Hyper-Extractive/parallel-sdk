using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ParametersDiagnostics : IPersistence
    {
        bool _traceStatus;
        bool _traceExchangeItems;
        WriteTo _writeTo = WriteTo.None;
        string _caption = string.Empty;

        public string Caption
        {
            get { return _caption; }
            set 
            {
                // strip conflicting XML characters
                _caption = Regex.Replace(value, @"[^\w\.@-]", ""); 
            }
        }

        public bool TraceStatus
        {
            get { return _traceStatus; }
            set { _traceStatus = value; }
        }

        public bool TraceExchangeItems
        {
            get { return _traceExchangeItems; }
            set { _traceExchangeItems = value; }
        }

        public WriteTo To
        {
            get { return _writeTo; }
            set { _writeTo = value; }
        }

        public override string ToString()
        {
            return Caption;
        }

        public string ValueAsString
        {
            get
            {
                var sb = new StringBuilder();

                if (Caption != string.Empty)
                    sb.Append("~Caption=" + Caption.ToString()); 
                if (TraceStatus)
                    sb.Append("~Status");
                if (TraceExchangeItems)
                    sb.Append("~Items");
                if (To != WriteTo.None)
                    sb.Append("~To=" + To.ToString().Replace(',','|'));

                return sb
                    .ToString()
                    .TrimStart('~');
            }

            set
            {
                TraceStatus = false;
                TraceExchangeItems = false;
                To = WriteTo.None;
                Caption = string.Empty;

                var options = value.Split('~');

                TraceStatus = options.Contains("Status");
                TraceExchangeItems = options.Contains("Items");

                var to = options
                    .Where(o => o.Trim().StartsWith("To="))
                    .SingleOrDefault();

                if (to != null)
                {
                    var t = to.Substring(3).Replace('|',',');
                    To = (WriteTo)Enum.Parse(
                        typeof(WriteTo), t);
                }

                var caption = options
                    .Where(o => o.Trim().StartsWith("Caption="))
                    .SingleOrDefault();

                if (caption != null)
                    Caption = caption.Substring(8);
            }
        }

        public ParametersDiagnostics()
        { }

        public ParametersDiagnostics(bool traceStatus = false, bool traceExchangeItems = false)
        {
            TraceStatus = traceStatus;
            TraceExchangeItems = traceExchangeItems;
        }

        public ParametersDiagnostics(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public const string XName = "ParametersDiagnostics";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ValueAsString = xElement.Value;
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, ValueAsString);
        }
    }
}
