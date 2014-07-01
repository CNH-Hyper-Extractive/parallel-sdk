
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	public class RemotingServerEngineTime : MarshalByRefObject, IEngineTime
	{
		static IEngine _engine;
		static Type _engineType;
		static Exception _lastException;
		static List<string> _args = new List<string>();
		static bool _finished = false;
		static bool _ensureSecurity = false;
		static bool _traceEngine = false;
		static XElement _initialisingXml;
		static RemotingProtocol _protocol;
		static string _objectUri;
		static string _portName = string.Empty;
		static int _port = -1;
		static int _sleep = 5000;
		static string _configFilename;
		static bool _configFilenameTmp;
		static IAssemblyLoader _assemblyLoader;
		static FileInfo _openMiStandard2Dll;
        static Stream _diagnosticsStream;

		/// <summary>
		/// Start server
		/// </summary>
		/// <param name="args">Server Args</param>
		public static void Main(string[] a)
		{
			try
			{
				Trace.Listeners.Clear();
				//Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
				//Trace.AutoFlush = true;

				var sdkUri = new Uri(Assembly.GetAssembly(typeof(RemotingServerEngineTime)).CodeBase);

				Console.WriteLine(
					"\r\n##########################################" +
					"\r\n FluidEarth2.Sdk.RemotingServerEngineTime" +
					"\r\n##########################################" +
					"\r\n\t" + DateTime.Now.ToString("u"));

				_args.AddRange(Environment.GetCommandLineArgs());

				foreach (string arg in _args)
					Trace.TraceInformation(string.Format("Arg: {0}", arg));

				if (_args.Count < 4)
				{
					Console.WriteLine("Minimum of 3 arguments required");
					Console.WriteLine();
					Console.Write(CommandLineArgs());
					return;
				}

				// First argument must be RemotingProtocol

				_protocol = (RemotingProtocol)Enum.Parse(typeof(RemotingProtocol), _args[1]);

				// second argument objectUri

				_objectUri = _args[2];

				if (!_objectUri.EndsWith(".rem") && !_objectUri.EndsWith(".soap"))
					throw new Exception("objectUri must end in either .rem or .soap");

				// third argument is either portName if IPC or port number

				switch (_protocol)
				{
					case RemotingProtocol.ipcAuto:
					case RemotingProtocol.ipc:
						_portName = _args[3];
						break;
					case RemotingProtocol.tcp:
					case RemotingProtocol.http:
						_port = int.Parse(_args[3]);
						break;
					default:
						throw new NotImplementedException(_protocol.ToString());
				}

				if (_args.Contains("launchDebugger"))
				{
					Trace.TraceWarning("Launching debugger");
					bool launched = Debugger.Launch();
					Trace.TraceInformation("Launched debugger " + launched.ToString());
				}

				_ensureSecurity = _args.Contains("ensureSecurity");
				_traceEngine = _args.Contains("traceEngine");

				Trace.TraceInformation(string.Format("\r\n" +
					"\tProtocol: {0}\r\n" +
					"\tObjectUri: {1} (With extension .rem or .soap)\r\n" + 
					"\tPortName: {2} (Only required if protocol is IPC)\r\n" +
					"\tPort: {3} (Only required if protocol not IPC)\r\n" +
					"\tEnsure Security: {4}\r\n" +
					"\tTrace Engine: {5}\r\n",
					_protocol, _objectUri, _portName, _port, _ensureSecurity, _traceEngine
					));

				ResolveDependancies();

				_assemblyLoader = AssemblyLoader.New("RemotingServerEngineTime");

				var sdkFile = new FileInfo(sdkUri.LocalPath);

				_configFilename = Path.Combine(sdkFile.DirectoryName, "FluidEarth2_Sdk_RemotingServerEngineTime.cfg");

				if (File.Exists(_configFilename))
				{
					Trace.TraceInformation(string.Format(
						"Using local remoting config \"{0}\"\r\n\r\n{1}",
						_configFilename, File.ReadAllText(_configFilename)));
				}
				else
				{
					_configFilenameTmp = true;
					_configFilename = Path.GetTempFileName();

					Console.WriteLine("_configFilename: " + _configFilename);

					var serverType = "FluidEarth2.Sdk.RemotingServerEngineTime, FluidEarth2_Sdk";

					var config = Utilities.Remoting.ServerConfigXml(
						_protocol, _objectUri, _portName, _port, serverType);

					config.Save(_configFilename);

					Trace.TraceInformation(string.Format(
						"Saved remoting config to \"{0}\"\r\n\r\n{1}",
						_configFilename, config.ToString()));
				}

				Trace.TraceInformation("Configuring server ...");
#if MONO
				RemotingConfiguration.Configure(_configFilename);
#else
				RemotingConfiguration.Configure(_configFilename, _ensureSecurity);
#endif
				Trace.TraceInformation(string.Format(
					"Configured server\r\n{0}\r\n{1}\r\n{2}",
					Utilities.Remoting.InfoRemotingConfiguration(),
					Utilities.Remoting.InfoRegisteredChannels(),
					"\r\n~~~~~~~~~~~~~~~~~~~~~~~~~~" +
					"\r\n Server awaiting requests" +
					"\r\n~~~~~~~~~~~~~~~~~~~~~~~~~~"
					));

				if (_protocol != RemotingProtocol.ipcAuto)
				{
					Trace.TraceInformation("Hit return to exit");
					Console.ReadLine();
				}
				else
				{
					while (!_finished)
						Thread.Sleep(_sleep);

					Trace.TraceWarning("Engine Finished");
				}
			}
			catch (System.Exception e)
			{
				_lastException = new Exception("Engine Server Initialisation Catch", e);

				Trace.TraceError(_lastException.ToString());
			}
			finally
			{
				if (_configFilenameTmp 
					&& _configFilename != null
					&& File.Exists(_configFilename))
				{
					File.Delete(_configFilename);

					Trace.TraceInformation("Deleted " + _configFilename);
				}

				Trace.TraceInformation(
					"\r\n#####################################" +
					"\r\n FLUID EARTH ENGINE SERVER SHUT DOWN" +
					"\r\n#####################################" +
					"\r\n\t" + DateTime.Now.ToString("u"));
			}
		}

		static string CommandLineArgs()
		{
			StringBuilder sb = new StringBuilder("Command Line Arguments:");
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat("\t{0} protocol objectUri port(name)",
				new FileInfo(Assembly.GetExecutingAssembly().Location).Name);
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("Where:");
			sb.AppendLine("\tProtocol: Either IPC,TCP or HTTP");
			sb.AppendLine("\tObjectUri: Message name (should end in either .rem or .soap)");
			sb.AppendLine("\tPort(name): If IPC name for channel, else port integer");
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("Optional additional keywords:");
			sb.AppendLine("\tlaunchDebugger: Allow for a debugger attach");
			sb.AppendLine("\tensureSecurity: Security not required");
			sb.AppendLine("\ttraceEngine: Additional trace output"); 
			sb.AppendLine();

			return sb.ToString();
		}

		static void ResolveDependancies()
		{
			var sdk = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var sdkFile = new FileInfo(sdk.LocalPath);

			_openMiStandard2Dll = new FileInfo(Path.Combine(sdkFile.DirectoryName, "OpenMI.Standard2.dll"));

			if (!_openMiStandard2Dll.Exists)
				throw new Exception("Cannot find OpenMI.Standard2.dll as " + _openMiStandard2Dll.FullName);
		}

		void EngineMethodCatch(System.Exception e)
		{
			_lastException = new Exception("Engine Server rethrow", e);

			Trace.TraceError(_lastException.ToString());
			Console.WriteLine(_lastException.ToString());

			Dispose();
		}

		void EngineMethodCall(string calling)
		{
			Trace.WriteIf(_traceEngine, string.Format("{0}: {1}\r\n", 
				DateTime.Now.ToString("u"), calling));

			if (_engine == null)
				throw new Exception("Server engine either not initialised or already disposed");
		}

		public string Ping()
		{
			try
			{
				Trace.WriteIf(_traceEngine, string.Format("{0}: {1}\r\n",
					DateTime.Now.ToString("u"), "Ping"));

                if (_lastException != null)
                    return Utilities.Xml.Persist(_lastException).ToString();

				if (_engine != null)
					return _engine.Ping();
				else
					return "Engine uninitialised or disposed";
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return _lastException != null
				? Utilities.Xml.Persist(_lastException).ToString()
				: "Engine Dead"; 
		}

		public virtual void Initialise(string initialisingXml)
		{
			Initialise(initialisingXml, null);
		}

		public void Initialise(string initialisingXml, IDocumentAccessor accessor)
		{
			try
			{
				Trace.WriteIf(_traceEngine, string.Format("{0}: {1}\r\n",
					DateTime.Now.ToString("u"), "Initialise:\r\n" + initialisingXml));

				if (_engine != null)
					throw new Exception("Engine already initialised on server");
				
				_initialisingXml = XElement.Parse(initialisingXml);

				var arguments = Persistence.Arguments
					.Parse(_initialisingXml, accessor);

                var diagnosticsIdentity = BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Diagnostics);

                var argDiagnostics = arguments
                    .Where(a => a.Id == diagnosticsIdentity.Id)
                    .Single()
                    as ArgumentParametersDiagnosticsEngine;

                var diagnostics = argDiagnostics.Parameters;

                if (diagnostics.LaunchDebugger)
                    Debugger.Launch();

				var assemblyLoaders = arguments
					.Where(a => a.Value is IAssemblyLoader)
					.Select(a => a.Value as IAssemblyLoader);

				foreach (var l in assemblyLoaders)
					_assemblyLoader.Add(l);

				var xmlType = _initialisingXml
					.Elements("ExternalType")
					.Single();

				var externalType = new ExternalType();
				externalType.Initialise(xmlType, accessor);

				object engine = externalType.CreateInstance(out _engineType);
				
				_engine = engine as IEngine;

				Contract.Requires(_engine != null,
					"\"{0}\" is not a \"{1}\"", engine.GetType(), typeof(IEngine));

                _engine = Utilities.Diagnostics.AddDiagnostics(
                    diagnostics, _engine,
                    out _diagnosticsStream, true);

				_engine.Initialise(initialisingXml);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetArgument(string key, string value)
		{
			try
			{
				EngineMethodCall("SetArgument: " + key);

				_engine.SetArgument(key, value);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetInput: " + engineVariable);

				_engine.SetInput(engineVariable, elementCount, elementValueCount, vectorLength);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetInput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetInput: " + engineVariable);

				_engine.SetInput(engineVariable, elementCount, elementValueCounts, vectorLength);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetOutput: " + engineVariable);

				_engine.SetOutput(engineVariable, elementCount, elementValueCount, vectorLength);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetOutput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetOutput: " + engineVariable);

				_engine.SetOutput(engineVariable, elementCount, elementValueCounts, vectorLength);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void Prepare()
		{
			try
			{
				EngineMethodCall("Prepare");

				_engine.Prepare();
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetStrings(string engineVariable, string missingValue, string[] values)
		{
			try
			{
				EngineMethodCall("SetStrings: " + engineVariable);

				_engine.SetStrings(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetInt32s(string engineVariable, int missingValue, int[] values)
		{
			try
			{
				EngineMethodCall("SetInt32s: " + engineVariable);

				_engine.SetInt32s(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetDoubles(string engineVariable, double missingValue, double[] values)
		{
			try
			{
				EngineMethodCall("SetDoubles: " + engineVariable);

				_engine.SetDoubles(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void SetBooleans(string engineVariable, bool missingValue, bool[] values)
		{
			try
			{
				EngineMethodCall("SetBooleans: " + engineVariable);

				_engine.SetBooleans(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public void Update()
		{
			try
			{
				EngineMethodCall("Update");

				_engine.Update();
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
		}

		public string[] GetStrings(string engineVariable, string missingValue)
		{
			try
			{
				EngineMethodCall("GetStrings: " + engineVariable);

				return _engine.GetStrings(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return null;
		}

		public int[] GetInt32s(string engineVariable, int missingValue)
		{
			try
			{
				EngineMethodCall("GetInt32s: " + engineVariable);

				return _engine.GetInt32s(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return null;
		}

		public double[] GetDoubles(string engineVariable, double missingValue)
		{
			try
			{
				EngineMethodCall("GetDoubles: " + engineVariable);

				return _engine.GetDoubles(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return null;
		}

		public bool[] GetBooleans(string engineVariable, bool missingValue)
		{
			try
			{
				EngineMethodCall("GetBooleans: " + engineVariable);

				return _engine.GetBooleans(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return null;
		}

		public double GetCurrentTime()
		{
			try
			{
				EngineMethodCall("CurrentTime");

				var engine = _engine as IEngineTime;

				if (engine == null)
					throw new Exception("Engine not IEngineTime: " + _engine.GetType().FullName);

				return engine.GetCurrentTime();
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}

			return -1;
		}


		public void Finish()
		{
			try
			{
				EngineMethodCall("Finishing ...");

				_engine.Finish();

                if (_diagnosticsStream != null)
                {
                    // Dispose does this too but this potentially makes file available earlier
                    _diagnosticsStream.Close();
                    _diagnosticsStream = null;
                }

				Dispose();

				EngineMethodCall("... finished");
			}
			catch (System.Exception e)
			{
				EngineMethodCatch(e);
			}
			finally
			{
				_finished = true;
			}
		}

		public void Dispose()
		{
			if (_engine != null)
			{
				_engine.Dispose();
				_engine = null;
			}

            if (_diagnosticsStream != null)
            {
                _diagnosticsStream.Close();
                _diagnosticsStream = null;
            }
		}
	}
}


