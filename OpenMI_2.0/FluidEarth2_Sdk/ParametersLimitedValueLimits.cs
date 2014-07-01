
using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ParametersLimitedValueLimits<TType> : IPersistence
        where TType : IConvertible, IComparable<TType>
    {
        TType _minError = default(TType);
        TType _maxError = default(TType);
        TType _minWarning = default(TType);
        TType _maxWarning = default(TType);
        string _minErrorComment = string.Empty;
        string _maxErrorComment = string.Empty;
        string _minWarningComment = string.Empty;
        string _maxWarningComment = string.Empty;

        enum Limits { MinError = 1, MinWarning = 2, MaxWarning = 4, MaxError = 8 }
        Limits _limits = 0;

        public ParametersLimitedValueLimits()
        { }

        public ParametersLimitedValueLimits(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public const string XName = "LimitedValueLimits";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            var type = Utilities.Xml.GetAttribute(xElement, "type");

            if (type != typeof(TType).ToString())
                throw new Exception("Type missmatch");

            _limits = 0;

            string attr;
            Limits limit;

            foreach (var xLimit in xElement.Elements("Limit"))
            {
                attr = Utilities.Xml.GetAttribute(xLimit, "type");
                limit = (Limits)Enum.Parse(typeof(Limits), attr);
                _limits |= limit;

                attr = Utilities.Xml.GetAttribute(xLimit, "limit");

                switch (limit)
                {
                    case Limits.MinError:
                        _minError = (TType)Convert.ChangeType(attr, typeof(TType));
                        _minErrorComment = xLimit.Value;
                        break;
                    case Limits.MinWarning:
                        _minWarning = (TType)Convert.ChangeType(attr, typeof(TType));
                        _minWarningComment = xLimit.Value;
                        break;
                    case Limits.MaxWarning:
                        _maxWarning = (TType)Convert.ChangeType(attr, typeof(TType));
                        _maxWarningComment = xLimit.Value;
                        break;
                    case Limits.MaxError:
                        _maxError = (TType)Convert.ChangeType(attr, typeof(TType));
                        _maxErrorComment = xLimit.Value;
                        break;
                    default:
                        throw new NotImplementedException(limit.ToString());
                }          
            }              
        }   

        public XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                new XAttribute("type", typeof(TType).ToString()));

            if ((_limits & Limits.MinError) != 0)
                xml.Add(new XElement("Limit",
                    new XAttribute("type", Limits.MinError.ToString()),
                    new XAttribute("limit", (string)Convert.ChangeType(_minError, typeof(string))),
                    _minErrorComment));

            if ((_limits & Limits.MinWarning) != 0)
                xml.Add(new XElement("Limit",
                    new XAttribute("type", Limits.MinWarning.ToString()),
                    new XAttribute("limit", (string)Convert.ChangeType(_minWarning, typeof(string))),
                    _minWarningComment));

            if ((_limits & Limits.MaxWarning) != 0)
                xml.Add(new XElement("Limit",
                    new XAttribute("type", Limits.MaxWarning.ToString()),
                    new XAttribute("limit", (string)Convert.ChangeType(_maxWarning, typeof(string))),
                    _maxWarningComment));

            if ((_limits & Limits.MaxError) != 0)
                xml.Add(new XElement("Limit",
                    new XAttribute("type", Limits.MaxError.ToString()),
                    new XAttribute("limit", (string)Convert.ChangeType(_maxError, typeof(string))),
                    _maxErrorComment));

            return xml;
        }

        public void SetLimitErrorMin(TType limit)
        {
            SetLimitErrorMin(limit, null);
        }

        public void SetLimitErrorMin(TType limit, string comment)
        {
            _limits |= Limits.MinError;
            _minError = limit;
            _minErrorComment = comment == null ? string.Empty : comment;
        }

        public void SetLimitErrorMax(TType limit)
        {
            SetLimitErrorMax(limit, null);
        }

        public void SetLimitErrorMax(TType limit, string comment)
        {
            _limits |= Limits.MaxError;
            _maxError = limit;
            _maxErrorComment = comment == null ? string.Empty : comment;
        }

        public void SetLimitError(TType limitMin, TType limitMax)
        {
            SetLimitError(limitMin, limitMax, null, null);
        }

        public void SetLimitError(TType limitMin, TType limitMax, string commentMin, string commentMax)
        {
            _limits |= Limits.MinError;
            _limits |= Limits.MaxError;
            _minError = limitMin;
            _maxError = limitMax;
            _minErrorComment = commentMin == null ? string.Empty : commentMin;
            _maxErrorComment = commentMax == null ? string.Empty : commentMax;
        }

        public void SetLimitWarningMin(TType limit)
        {
            SetLimitWarningMin(limit, null);
        }

        public void SetLimitWarningMin(TType limit, string comment)
        {
            _limits |= Limits.MinWarning;
            _minWarning = limit;
            _minWarningComment = comment == null ? string.Empty : comment;
        }

        public void SetLimitWarningMax(TType limit)
        {
            SetLimitWarningMax(limit, null);
        }

        public void SetLimitWarningMax(TType limit, string comment)
        {
            _limits |= Limits.MaxWarning;
            _maxWarning = limit;
            _maxWarningComment = comment == null ? string.Empty : comment;
        }

        public void SetLimitWarning(TType limitMin, TType limitMax)
        {
            SetLimitWarning(limitMin, limitMax, null, null);
        }

        public void SetLimitWarning(TType limitMin, TType limitMax, string commentMin, string commentMax)
        {
            _limits |= Limits.MinWarning;
            _limits |= Limits.MaxWarning;
            _minWarning = limitMin;
            _maxWarning = limitMax;
            _minWarningComment = commentMin == null ? string.Empty : commentMin;
            _maxWarningComment = commentMax == null ? string.Empty : commentMax;
        }

        public EValidation Validate(TType value, out string message)
        {
            if ((_limits & Limits.MinError) != 0
                && _minError.CompareTo(value) > 0)
            {
                message = string.Format("value {0} < {1} limit, {2}",
                    value, _minError, _minErrorComment);
                return EValidation.Error;
            }

            if ((_limits & Limits.MaxError) != 0
                && _maxError.CompareTo(value) < 0)
            {
                message = string.Format("value {0} > {1} limit, {2}",
                    value, _maxError, _maxErrorComment);
                return EValidation.Error;
            }

            if ((_limits & Limits.MinWarning) != 0
                && _minWarning.CompareTo(value) > 0)
            {
                message = string.Format("value {0} < {1} limit, {2}",
                    value, _minWarning, _minWarningComment);
                return EValidation.Warning;
            }

            if ((_limits & Limits.MaxWarning) != 0
                && _maxWarning.CompareTo(value) < 0)
            {
                message = string.Format("value {0} > {1} limit, {2}",
                    value, _maxWarning, _maxWarningComment);
                return EValidation.Warning;
            }

            message = ToString();

            return EValidation.Valid;
        }
    }
}
