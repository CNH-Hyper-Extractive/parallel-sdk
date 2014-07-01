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

using OpenMI.Standard2;
using System.Linq;
using System.Xml.Linq;
using System;

namespace FluidEarth2.Sdk
{
    public class Category : Describes, ICategory
    {
        object _value;

        public Category(ICategory iCategory)
            : base(iCategory)
        {
            _value = iCategory.Value;
        }

        public Category(object obj, IDescribable describes)
            : base(describes)
        {
            _value = obj;
        }

        public Category(XElement xElement, IDocumentAccessor accessor)
            : base(xElement.Elements("Describes").Single(), accessor)
        {
            Utilities.Xml.ValidElement(xElement, "Category");

            Type type = Type.GetType(Utilities.Xml.GetAttribute(xElement, "type"));
            _value = Convert.ChangeType(Utilities.Xml.GetAttribute(xElement, "value"), type);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement("Category",
                new XAttribute("type", _value.GetType().ToString()),
                new XAttribute("value", _value.ToString()),
                base.Persist(accessor));
        }

        #region ICategory Members

        public object Value
        {
            get { return _value; }
        }

        #endregion
    }
}
