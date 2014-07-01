using System.Collections.Generic;

namespace FluidEarth2.Sdk
{
    public class ValueSetElementMultiValues<TType>
    {
        List<TType> _elementMultiValues = new List<TType>();

        public ValueSetElementMultiValues()
        { }

        public ValueSetElementMultiValues(IEnumerable<TType> elementMultiValues)
        { 
            _elementMultiValues = new List<TType>(elementMultiValues);
        }

        List<TType> MultiValues
        {
            get { return _elementMultiValues; }
            set { _elementMultiValues = new List<TType>(value); }
        }
    }
}
