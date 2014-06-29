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

namespace KState.Util
{
    public class ElementSetReader
    {
        private XmlDocument xmlDocument;

        public static Dictionary<String, IElementSet> read(String filename)
        {
            var reader = new ElementSetReader();
            var sets = reader.readElementSets(filename);
            return sets;
        }

        private Dictionary<String, IElementSet> readElementSets(string filename)
        {
            try
            {
                xmlDocument = new XmlDocument();
                xmlDocument.Load(filename);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load xml document: " + e.Message);
            }

            var aNode = xmlDocument.ChildNodes[0];

            // ensure the configuration element is there
            Utils.forceNodeName(aNode, "ElementSets");

            return readElementSets(aNode);
        }

        private Dictionary<String, IElementSet> readElementSets(XmlNode aNode)
        {
            var elementSets = new Dictionary<String, IElementSet>();
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
            return elementSets;
        }

        private Element[] readElements(XmlNode aNode)
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

        private ElementType getElementType(String typeName)
        {
            if (typeName == "Point" == true)
                return ElementType.XYPoint;

            throw new Exception("Invalid element type");
        }
    }
}