#region GNU GPL - Fluid Earth SDK
/*
    This file is part of 'Fluid Earth SDK'.

    'Fluid Earth SDK' is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    'Fluid Earth SDK' is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with 'Fluid Earth SDK'.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion GNU GPL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using OpenMI.Standard2;
using FluidEarth2.CoreStandard2;

namespace FluidEarth2.Sdk
{
    public abstract class BaseExchangeItem : FluidEarth2.CoreStandard2.BaseExchangeItem
    {
 //       string _componentId = string.Empty;
        IValueSetConverter _valueSetConverter = null;

        // Required for FluidEarth1 migration to 2
        Dictionary<string, string> _userVariables
            = new Dictionary<string, string>();

        public BaseExchangeItem()
        {}

        public BaseExchangeItem(IIdentifiable identity, IBaseLinkableComponent component, IValueDefinition iValueDefinition, IValueSetConverter iValueSetConverter)
            : base(identity, component, iValueDefinition)
        {
 //           _componentId = _component != null ? _component.Id : string.Empty;
            
            _valueSetConverter = iValueSetConverter;

            if (_valueSetConverter != null)
                _valueSetConverter.ExchangeItem = this;
        }

        public BaseExchangeItem(IBaseExchangeItem item)
            : base(item)
        {
//            _componentId = _component != null ? _component.Id : string.Empty;

            BaseExchangeItem e = item as BaseExchangeItem;

            if (e != null)
            {
                // Need to clone e._valueSetConverter?

                _valueSetConverter = e._valueSetConverter;

                if (_valueSetConverter != null)
                    _valueSetConverter.ExchangeItem = this;
            }
        }

        public const string XName = "BaseExchangeItem";

        public BaseExchangeItem(IBaseLinkableComponent component, XElement xElement, IDocumentAccessor accessor)
            : base(Persistence.Identity.Parse(xElement.Elements(Persistence.Identity.XName).Single(), accessor))
        {
            Contract35.Requires(xElement.Name == BaseExchangeItem.XName);

            Component = component;
//            _componentId = Utilities.Xml.GetAttribute(xElement, "componentId");

            if (xElement.Elements(Persistence.ValueDefinition.XName).SingleOrDefault() != null)
                ValueDefinition = Persistence.ValueDefinition.Parse(
                    xElement.Element(Persistence.ValueDefinition.XName), accessor);
 
            _valueSetConverter = null;

            foreach (XElement xUserVariable in xElement.Elements("UserVariable"))
                _userVariables.Add(
                    Utilities.Xml.GetAttribute(xUserVariable, "key"),
                    Utilities.Xml.GetAttribute(xUserVariable, "value"));
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement("BaseExchangeItem",
 //               new XAttribute("componentId", _componentId),
                Persistence.ValueDefinition.Persist(ValueDefinition, accessor),
                _userVariables.Keys.Select(k => 
                    new XElement("UserVariable", 
                        new XAttribute("key", k), 
                        new XAttribute("value", _userVariables[k]))),
                Persistence.Identity.Persist(this, accessor));
        }

        public void AddUserVariables(Dictionary<string, string> userVariables)
        {
            foreach (KeyValuePair<string, string> kv in userVariables)
                if (_userVariables.ContainsKey(kv.Key))
                    _userVariables[kv.Key] = kv.Value;
                else
                    _userVariables.Add(kv.Key, kv.Value);
        }

        public IValueSetConverter ValueSetConverter
        {
            get { return _valueSetConverter; }
            set
            {
                _valueSetConverter = value;

                if (_valueSetConverter != null)
                    _valueSetConverter.ExchangeItem = this;
            }
        }
    }
}
