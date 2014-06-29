//  -----------------------------------------------------------------------
//   Copyright (c) 2014 Tom Bulatewicz, Kansas State University
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy
//   of this software and associated documentation files (the "Software"), to deal
//   in the Software without restriction, including without limitation the rights
//   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//   copies of the Software, and to permit persons to whom the Software is
//   furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all
//   copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//   SOFTWARE.
//  -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using Oatc.OpenMI.Sdk.Backbone;
using OpenMI.Standard;
using ValueType = OpenMI.Standard.ValueType;

namespace KState.Util
{
    public class ComponentProperties
    {
        // model info element
        private double mEndDateTime;
        private Dictionary<String, String> mExtras;

        // exchange items
        private List<InputExchangeItem> mInputExchangeItems;
        private String mModelDescription;
        private String mModelId;
        private List<OutputExchangeItem> mOutputExchangeItems;
        private double mStartDateTime;
        private double mTimeStepInSeconds;

        // any extra name-value pairs

        public String getModelId()
        {
            return mModelId;
        }

        public void setModelId(String value)
        {
            mModelId = value;
        }

        public String getModelDescription()
        {
            return mModelDescription;
        }

        public void setModelDescription(String value)
        {
            mModelDescription = value;
        }

        public double getStartDateTime()
        {
            return mStartDateTime;
        }

        public void setStartDateTime(double value)
        {
            mStartDateTime = value;
        }

        public double getEndDateTime()
        {
            return mEndDateTime;
        }

        public void setEndDateTime(double value)
        {
            mEndDateTime = value;
        }

        public double getTimeStepInSeconds()
        {
            return mTimeStepInSeconds;
        }

        public void setTimeStepInSeconds(double value)
        {
            mTimeStepInSeconds = value;
        }

        public List<InputExchangeItem> getInputExchangeItems()
        {
            return mInputExchangeItems;
        }

        public void setInputExchangeItems(List<InputExchangeItem> value)
        {
            mInputExchangeItems = value;
        }

        public List<OutputExchangeItem> getOutputExchangeItems()
        {
            return mOutputExchangeItems;
        }

        public void setOutputExchangeItems(List<OutputExchangeItem> value)
        {
            mOutputExchangeItems = value;
        }

        public void setExtra(String name, String value)
        {
            mExtras[name] = value;
        }

        public String getExtra(String name)
        {
            return mExtras[name];
        }

        public void setExtras(Dictionary<String, String> value)
        {
            mExtras = value;
        }

        public Dictionary<String, String> getExtras()
        {
            return mExtras;
        }

        public static ComponentProperties read
            (String filename, ILinkableComponent component,
                Dictionary<String, IElementSet> elementSets)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(filename);
            var properties = readConfiguration(xmlDocument, component, elementSets);
            return properties;
        }

        private static ComponentProperties readConfiguration
            (XmlDocument doc, ILinkableComponent component,
                Dictionary<String, IElementSet> elementSets)
        {
            var aNode = doc.ChildNodes[0];

            // ensure the configuration element is there
            Utils.forceNodeName(aNode, "Configuration");

            // create a new properties object
            var properties = new ComponentProperties();

            // read the element sets and create temporary objects that can be
            // assigned to the output exchange items as we read them, note that
            // we want to add them to our current set, since we may have read
            // other element sets from the element set file
            readElementSets(Utils.findChildNode(aNode, "ElementSets", false), elementSets);

            // read the name-value pair extras
            properties.setExtras(readExtras(Utils.findChildNode(aNode, "Extras", false)));

            // read the exchange items
            properties.setOutputExchangeItems(readOutputExchangeItems(Utils.findChildNode(aNode, "ExchangeItems"),
                component, elementSets));
            properties.setInputExchangeItems(readInputExchangeItems(Utils.findChildNode(aNode, "ExchangeItems"), component,
                elementSets));

            // read the time horizon
            var timeHorizonNode = Utils.findChildNode(aNode, "TimeHorizon");
            properties.setStartDateTime(Utils.readDateTimeString(Utils.findChildNodeValue(timeHorizonNode, "StartDateTime")));
            properties.setEndDateTime(Utils.readDateTimeString(Utils.findChildNodeValue(timeHorizonNode, "EndDateTime")));
            properties.setTimeStepInSeconds(Double.Parse(Utils
                .findChildNodeValue(timeHorizonNode, "TimeStepInSeconds")));

            // read the model info
            var modelInfoNode = Utils.findChildNode(aNode, "ModelInfo");
            properties.setModelId(Utils.findChildNodeValue(modelInfoNode, "ID"));
            properties.setModelDescription(Utils.findChildNodeValue(modelInfoNode, "Description"));
            if (Utils.findChildNode(modelInfoNode, "PrefetchEnabled", false) != null)
                properties.setExtra("prefetchEnabled", Utils.findChildNodeValue(modelInfoNode, "PrefetchEnabled"));
            if (Utils.findChildNode(modelInfoNode, "CacheEnabled", false) != null)
                properties.setExtra("cacheEnabled", Utils.findChildNodeValue(modelInfoNode, "CacheEnabled"));
            if (Utils.findChildNode(modelInfoNode, "CacheName", false) != null)
                properties.setExtra("cacheName", Utils.findChildNodeValue(modelInfoNode, "CacheName"));
            if (Utils.findChildNode(modelInfoNode, "ProcessingTime", false) != null)
                properties.setExtra("processingTime", Utils.findChildNodeValue(modelInfoNode, "ProcessingTime"));
            if (Utils.findChildNode(modelInfoNode, "EnableParallel", false) != null)
                properties.setExtra("enableParallel", Utils.findChildNodeValue(modelInfoNode, "EnableParallel"));
            if (Utils.findChildNode(modelInfoNode, "UsePersistence", false) != null)
                properties.setExtra("usePersistence", Utils.findChildNodeValue(modelInfoNode, "UsePersistence"));
            if (Utils.findChildNode(modelInfoNode, "StartupDelay", false) != null)
                properties.setExtra("startupDelay", Utils.findChildNodeValue(modelInfoNode, "StartupDelay"));
            if (Utils.findChildNode(modelInfoNode, "ShutdownDelay", false) != null)
                properties.setExtra("shutdownDelay", Utils.findChildNodeValue(modelInfoNode, "ShutdownDelay"));

            return properties;
        }

        private static void readElementSets(XmlNode aNode, Dictionary<String, IElementSet> elementSets)
        {
            if (aNode != null)
            {
                foreach (XmlNode node in aNode.ChildNodes)
                {
                    if (node.Name == "ElementSet")
                    {
                        var id = Utils.findChildNodeValue(node, "ID");
                        var description = Utils.findChildNodeValue(node, "Description");
                        var kind = Utils.findChildNodeValue(node, "Kind");

                        var elements = readElements(Utils.findChildNode(node, "Elements"));

                        var elementSet = new ElementSet();
                        elementSet.ID = id;
                        elementSet.Description = description;
                        elementSet.Elements = elements;
                        elementSet.ElementType = getElementType(kind);

                        elementSets[id] = elementSet;
                    }
                }
            }
        }

        private static Dictionary<String, String> readExtras(XmlNode aNode)
        {
            var properties = new Dictionary<String, String>();

            if (aNode != null)
            {
                foreach (XmlNode node in aNode.ChildNodes)
                {
                    if (node.Name == "Extra")
                    {
                        var name = node.Attributes["name"].InnerText;
                        var value = node.Attributes["value"].InnerText;

                        // save the element
                        properties[name] = value;
                    }
                }
            }

            return properties;
        }

        private static Element[] readElements(XmlNode aNode)
        {
            var elements = new List<Element>();
            foreach (XmlNode node in aNode.ChildNodes)
            {
                if (node.Name == "Element")
                {
                    var id = Utils.findChildNodeValue(node, "ID");
                    var x = Utils.findChildNodeValue(node, "X");
                    var y = Utils.findChildNodeValue(node, "Y");

                    // create a vertex
                    var vertex = new Vertex();
                    vertex.x = Double.Parse(x);
                    vertex.y = Double.Parse(y);

                    // create the vertices
                    var vertices = new Vertex[1];
                    vertices[0] = vertex;

                    // create an element
                    var element = new Element();
                    element.ID = id;
                    element.Vertices = vertices;

                    // save the element
                    elements.Add(element);
                }
            }

            var elementArray = new Element[elements.Count];
            for (var i = 0; i < elements.Count; i++)
                elementArray[i] = elements[i];

            return elementArray;
        }

        private static ElementType getElementType(String typeName)
        {
            if (typeName == "Point")
                return ElementType.XYPoint;

            throw new Exception("Invalid element type");
        }

        private static List<OutputExchangeItem> readOutputExchangeItems
            (XmlNode aNode, ILinkableComponent component,
                Dictionary<String, IElementSet> elementSets)
        {
            var items = new List<OutputExchangeItem>();

            foreach (XmlNode node in aNode.ChildNodes)
            {
                if (node.Name == "OutputExchangeItem")
                {
                    var item = new OutputExchangeItem();

                    // see what the element set id is and lookup the actual
                    // object and assign it to the exchange item
                    var elementSetId = Utils.findChildNodeValue(node, "ElementSetID");
                    item.ElementSet = elementSets[elementSetId];

                    // make sure we found the element set
                    if (item.ElementSet == null)
                        throw new Exception("Failed to find element set");

                    // read the quantity details
                    var quantity = readQuantity(Utils.findChildNode(node, "Quantity"));
                    item.Quantity = quantity;

                    items.Add(item);
                }
            }

            return items;
        }

        private static List<InputExchangeItem> readInputExchangeItems
            (XmlNode aNode, ILinkableComponent component,
                Dictionary<String, IElementSet> elementSets)
        {
            var items = new List<InputExchangeItem>();

            foreach (XmlNode node in aNode.ChildNodes)
            {
                if (node.Name == "InputExchangeItem")
                {
                    var item = new InputExchangeItem();

                    // see what the element set id is and lookup the actual
                    // object and assign it to the exchange item
                    var elementSetId = Utils.findChildNodeValue(node, "ElementSetID");
                    item.ElementSet = elementSets[elementSetId];

                    // make sure we found the element set
                    if (item.ElementSet == null)
                        throw new Exception("Failed to find element set");

                    // read the quantity details
                    var quantity = readQuantity(Utils.findChildNode(node, "Quantity"));
                    item.Quantity = quantity;

                    items.Add(item);
                }
            }

            return items;
        }

        private static Quantity readQuantity(XmlNode aNode)
        {
            var quantity = new Quantity();

            // required
            quantity.ID = Utils.findChildNodeValue(aNode, "ID");

            // optional
            if (Utils.findChildNode(aNode, "Description", false) != null)
                quantity.Description = Utils.findChildNodeValue(aNode, "Description");
            else
                quantity.Description = "";

            if (Utils.findChildNode(aNode, "ValueType", false) != null)
                quantity.ValueType = getValueType(Utils.findChildNodeValue(aNode, "ValueType"));
            else
                quantity.ValueType = ValueType.Scalar;

            if (Utils.findChildNode(aNode, "Dimensions", false) != null)
                quantity.Dimension = readDimension(Utils.findChildNode(aNode, "Dimensions"));
            else
            {
                var dimension = new Dimension();
                dimension.SetPower(DimensionBase.Length, 1);
                quantity.Dimension = dimension;
            }

            if (Utils.findChildNode(aNode, "Unit", false) != null)
                quantity.Unit = readUnit(Utils.findChildNode(aNode, "Unit"));
            else
            {
                var unit = new Unit();
                unit.ID = "DefaultUnit";
                unit.Description = "Default Unit";
                unit.ConversionFactorToSI = 1.0;
                unit.OffSetToSI = 0.0;
                quantity.Unit = unit;
            }

            return quantity;
        }

        private static Dimension readDimension(XmlNode aNode)
        {
            var dimension = new Dimension();
            foreach (XmlNode node in aNode.ChildNodes)
            {
                if (node.Name == "Dimension")
                {
                    // note that this just returns the first one right now
                    var b = Utils.findChildNodeValue(node, "Base");
                    var power = Utils.findChildNodeValue(node, "Power");
                    dimension.SetPower(getDimensionBase(b), Int32.Parse(power));
                    break;
                }
            }
            return dimension;
        }

        private static Unit readUnit(XmlNode aNode)
        {
            var unit = new Unit();
            unit.ID = Utils.findChildNodeValue(aNode, "ID");

            if (Utils.findChildNode(aNode, "Description", false) != null)
                unit.Description = Utils.findChildNodeValue(aNode, "Description");

            if (Utils.findChildNode(aNode, "ConversionFactorToSI", false) != null)
                unit.ConversionFactorToSI = Double.Parse(Utils.findChildNodeValue(aNode, "ConversionFactorToSI"));

            if (Utils.findChildNode(aNode, "OffSetToSI", false) != null)
                unit.OffSetToSI = Double.Parse(Utils.findChildNodeValue(aNode, "OffSetToSI"));
            return unit;
        }

        private static DimensionBase getDimensionBase(String b)
        {
            switch (b)
            {
                case "Length":
                    return DimensionBase.Length;
                case "Time":
                    return DimensionBase.Time;
                case "AmountOfSubstance":
                    return DimensionBase.AmountOfSubstance;
                case "Currency":
                    return DimensionBase.Currency;
                case "ElectricCurrent":
                    return DimensionBase.ElectricCurrent;
                case "LuminousIntensity":
                    return DimensionBase.LuminousIntensity;
                case "Mass":
                    return DimensionBase.Mass;
                case "Temperature":
                    return DimensionBase.Temperature;
                default:
                    throw new Exception("Invalid dimension base");
            }
        }

        private static ValueType getValueType(String valueType)
        {
            switch (valueType)
            {
                case "Scalar":
                    return ValueType.Scalar;
                case "Vector":
                    return ValueType.Vector;
                default:
                    throw new Exception("Invalid value type");
            }
        }
    }
}