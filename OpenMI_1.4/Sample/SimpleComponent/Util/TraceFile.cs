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
using System.Text;
using Oatc.OpenMI.Sdk.Backbone;
using OpenMI.Standard;

namespace KState.Util
{
    public class TraceFile
    {
        private readonly StreamWriter writer;

        // development options:
        private bool optionIncludeDateTime = true;
        private bool optionStdOut = true;

        public TraceFile(string prefix)
        {
            var filename = "Trace-" + prefix + "-" + Utils.GetHostAddress() + ".txt";
            writer = new StreamWriter(filename);
        }

        public void Append(String message)
        {
            try
            {
                var buffer = new StringBuilder();

                if (optionIncludeDateTime == true)
                {
                    buffer.Append(Utils.FormatDateForXml(DateTime.Now));
                    buffer.Append(" ");
                }

                buffer.Append(message);

                writer.WriteLine(buffer);
                writer.Flush();

                if (optionStdOut == true)
                    Console.WriteLine(message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to write to log: " + e.Message);
            }
        }

        public void Exception(Exception e)
        {
            Append("EXCEPTION: " + e.StackTrace);
        }

        public void AppendValueSet(String quantityId, IElementSet elementSet, ScalarSet valueSet)
        {
            Append(quantityId);
            for (var index = 0; index < elementSet.ElementCount; index++)
            {
                var id = elementSet.GetElementID(index);
                Append(id + ":" + valueSet.GetScalar(index));
            }
        }

        public void Stop()
        {
            writer.Close();
        }
    }
}