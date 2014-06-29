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
using System.IO;
using System.Net;
using System.Xml;
using Oatc.OpenMI.Sdk.Backbone;
using Oatc.OpenMI.Sdk.DevelopmentSupport;
using OpenMI.Standard;

namespace KState.Util
{
    public class Utils
    {
        public static char[] Delimiters
        {
            get
            {
                var d = new char[4];
                d = new char[3];
                d[0] = ' ';
                d[1] = '\t';
                d[2] = ',';
                return d;
            }
        }

        public static string GetHostAddress()
        {
            return Dns.GetHostName();
        }

        public static DateTime ConvertJavaMillisecondToDateTime(long javaMS)
        {
            return new DateTime(621355968000000000L + javaMS*10000);
            //DateTime UTCBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime dt = UTCBaseTime.Add(new System.TimeSpan(javaMS * System.TimeSpan.TicksPerMillisecond));
            //return dt;
        }

        public static long ConvertDateTimeToJavaMillisecond(DateTime dateTime)
        {
            return dateTime.ToFileTime(); // (dateTime.Ticks - 621355968000000000L) / 10000;
        }

        public static string FormatDateForXml(DateTime dt)
        {
            return string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        }

        public static void WriteArrayToFile(string path, string dataName, string header, DateTime dateTime, double[] data, IElementSet elementSet)
        {
            // don't write out empty data sets
            if (IsEmpty(data))
                return;

            var dt = string.Format("{0:0000}{1:00}{2:00}", dateTime.Year, dateTime.Month, dateTime.Day);
            dataName = dataName.Replace(' ', '_');
            var filename = dataName + dt + ".txt";
            var writer = new StreamWriter(Path.Combine(path, filename), false);
            writer.WriteLine(header);
            for (var i = 0; i < data.Length; i++)
            {
                var wellID = elementSet.GetElementID(i);
                writer.Write(wellID.PadLeft(8, ' '));

                var value = String.Format("{0:0.0000}", data[i]);
                writer.Write(value.PadLeft(12, ' '));

                writer.WriteLine();
            }
            writer.Close();
        }

        // remove empty strings from the string array
        public static string[] Compact(string[] inStrings)
        {
            // first see how many strings there are in the array
            var count = 0;
            for (var i = 0; i < inStrings.Length; i++)
            {
                if (inStrings[i] != "")
                    count++;
            }

            var outStrings = new string[count];
            var c = 0;
            for (var i = 0; i < inStrings.Length; i++)
            {
                if (inStrings[i] != "")
                {
                    outStrings[c] = inStrings[i];
                    c++;
                }
            }

            return outStrings;
        }

        public static bool IsEmpty(double[] data)
        {
            var isEmpty = true;
            for (var i = 0; i < data.Length; i++)
            {
                if ((int)data[i] != -999)
                {
                    isEmpty = false;
                    break;
                }
            }
            return isEmpty;
        }

        public static DateTime ITimeToDateTime(ITime iTime)
        {
            if (iTime is TimeStamp)
            {
                var ts = (TimeStamp)iTime;
                var d = ts.ModifiedJulianDay;
                var dt = CalendarConverter.ModifiedJulian2Gregorian(d);
                return dt;
            }

            throw new Exception("Unable to convert ITime to DateTime");
        }

        public static String AddTrailingSeparatorIfNecessary(String path)
        {
            if (path.EndsWith("" + Path.DirectorySeparatorChar) == false)
                path += Path.DirectorySeparatorChar;
            return path;
        }

        public static double readDateTimeString(String s)
        {
            var dt = DateTime.Parse(s);
            return CalendarConverter.Gregorian2ModifiedJulian(dt);
        }

        #region XML Helpers

        /**
	 * Checks if the specified node has the specified name (ignoring case) and
	 * throws an exception when it does not.
	 */

        public static void forceNodeName(XmlNode aNode, String aName)
        {
            if (aNode.Name != aName)
            {
                throw new Exception("INVALID_XML_STRUCTURE_EXPECTED_TAG_S_BUT_FOUND_TAG_S");
            }
        }

        /**
         * Finds the child node of the specified parent node that has a certain tag
         * name. If no matching node is found an exception will be thrown.
         */

        public static XmlNode findChildNode(XmlNode aParentNode, String aName)
        {
            return findChildNode(aParentNode, aName, true);
        }

        /**
         * Finds the child node of the specified parent node that has a certain tag
         * name. If no node is found and mustExist is false, null will be returned.
         * When mustExist is true and the node is not found the method will throw an
         * exception.
         */

        public static XmlNode findChildNode(XmlNode aParentNode, String aName, bool mustExist)
        {
            var children = aParentNode.ChildNodes;

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Name == aName)
                {
                    return children[i];
                }
            }

            if (!mustExist)
            {
                return null;
            }

            throw new Exception("INVALID_XML_STRUCTURE_REQUIRED_TAG_S_NOT_FOUND");
        }

        /**
         * Finds the child node of the specified parent node that has a certain tag
         * name and return its (text) value. If no matching node is found an
         * exception will be thrown.
         */

        public static String findChildNodeValue(XmlNode aParentNode, String aName)
        {
            return findChildNodeValue(aParentNode, aName, true);
        }

        /**
         * Finds the child node of the specified parent node that has a certain tag
         * name and return its (text) value. If no node is found and mustExist is
         * false, an empty string will be returned. When mustExist is true and the
         * node is not found the method will throw an exception.
         */

        public static String findChildNodeValue(XmlNode aParentNode, String aName, bool mustExist)
        {
            var n = findChildNode(aParentNode, aName, mustExist);
            return getNodeValue(n, aName);
        }

        public static String getNodeValue(XmlNode n, String aName)
        {
            if (n != null)
            {
                return n.InnerText;
                /*try
                {
                    return URLDecoder.decode(n.getFirstChild().getNodeValue(), "UTF-8");
                }
                catch (UnsupportedEncodingException e)
                {
                    throw new PersistenceException(INVALID_XML_STRUCTURE_UNSUPPORTED_ENCODING_OF_TAG_S_S_VALUE, aName);
                }*/
            }
            return "";
        }

        #endregion
    }
}