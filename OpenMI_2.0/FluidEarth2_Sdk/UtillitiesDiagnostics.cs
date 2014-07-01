using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public static partial class Utilities
    {
        public static class Diagnostics
        {
            public static IEngine AddDiagnostics(ParametersDiagnosticsNative diagnostics, IEngine engine, out Stream stream, bool isServer)
            {
                Contract.Requires(diagnostics != null, "diagnostics != null");
                Contract.Requires(engine != null, "engine != null");

                stream = null;

                if (diagnostics.To == WriteTo.None)
                    return engine;

                var log = diagnostics.Log;

                if (isServer && !diagnostics.LogServer)
                    log = null;

                if (log != null)
                {
                    try
                    {
                        if (!Path.IsPathRooted(log.FullName))
                            throw new Exception("Filename NOT rooted.");

                        if (isServer)
                        {
                            // Adapt from Client path

                            var n = log.FullName.Length - log.Extension.Length;
                            var s = log.FullName.Insert(n, "_Server");

                            log = new FileInfo(s);
                        }

                        if (File.Exists(log.FullName))
                            log.Delete();

                        stream = new BufferedStream(log.OpenWrite(), 20000);
                    }
                    catch (System.Exception e)
                    {
                        throw new Exception("Invalid Log fileName: " + log.FullName, e);
                    }
                }

                var intercepts = new List<IIntercept>();

                if (diagnostics.IncludeStatistics)
                    intercepts.Add(new InterceptCallReturnValues(diagnostics));

                if (diagnostics.IncludeCalls)
                    intercepts.Add(new InterceptCallArguments(diagnostics));

                if (diagnostics.IncludeTimings)
                    intercepts.Add(new InterceptCallTimer(diagnostics));

                if (stream != null)
                    foreach (var i in intercepts)
                        i.Streams.Add(stream);

                if (engine is IEngineTime)
                    return new InterceptionEngineTime(engine as IEngineTime, intercepts, true);

                if (engine is IEngine)
                    return new InterceptionEngine(engine as IEngine, intercepts, true);

                return engine;
            }

            public static IAdapterNativeLibraryDouble AddDiagnostics(ParametersDiagnosticsNative diagnostics, IAdapterNativeLibraryDouble engine, out Stream stream, bool isServer)
            {
                Contract.Requires(diagnostics != null, "diagnostics != null");
                Contract.Requires(engine != null, "engine != null");

                stream = null;

                if (diagnostics.To == WriteTo.None)
                    return engine;

                var log = diagnostics.Log;

                if (isServer && !diagnostics.LogServer)
                    log = null;

                if (log != null)
                {
                    try
                    {
                        if (!Path.IsPathRooted(log.FullName))
                            throw new Exception("Filename NOT rooted.");

                        if (isServer)
                        {
                            // Adapt from Client path

                            var n = log.FullName.Length - log.Extension.Length;
                            var s = log.FullName.Insert(n, "_Server");

                            log = new FileInfo(s);
                        }

                        if (File.Exists(log.FullName))
                            log.Delete();

                        stream = new BufferedStream(log.OpenWrite(), 20000);
                    }
                    catch (System.Exception e)
                    {
                        throw new Exception("Invalid Log fileName: " + log.FullName, e);
                    }
                }

                var intercepts = new List<IIntercept>();

                if (diagnostics.IncludeStatistics)
                    intercepts.Add(new InterceptCallReturnValues(diagnostics));

                if (diagnostics.IncludeCalls)
                    intercepts.Add(new InterceptCallArguments(diagnostics));

                if (diagnostics.IncludeTimings)
                    intercepts.Add(new InterceptCallTimer(diagnostics));

                if (stream != null)
                    foreach (var i in intercepts)
                        i.Streams.Add(stream);

                return new InterceptionAdapterNativeLibraryDouble(engine, intercepts, true);
            }

            public static string DatedLine(string caption, string line)
            {
                return string.Format("{0}[{1}]{2}",
                    DateTime.UtcNow.ToString("u"),
                    caption, line);
            }

            public static void WriteLine(string line, IIntercept intercept)
            {
                WriteLine(line, intercept.To, intercept == null ? null : intercept.Streams);
            }

            public static void WriteLine(string line, WriteTo to, IEnumerable<Stream> streams)
            {
                if (to == WriteTo.None)
                    return;

                Contract.Requires(line != null, "line != null");

                if ((to & WriteTo.Debug) != 0)
                    Debug.WriteLine(line);

                if ((to & WriteTo.Trace) != 0)
                    Trace.TraceInformation(line);

                if ((to & WriteTo.Console) != 0)
                    Console.WriteLine(line);

                if (streams != null)
                {
                    var uniqueStreams = new HashSet<Stream>(streams.Where(s => s != null));

                    if (uniqueStreams.Count > 0 && (to & WriteTo.Streams) != 0)
                    {
                        var bytes = System.Text.Encoding.Unicode.GetBytes(line + "\r\n");

                        foreach (var s in uniqueStreams)
                            if (s.CanWrite)
                                s.Write(bytes, 0, bytes.Count());
                    }
                }
            }        
        }
    }
}
