
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	class RemotingClientEngine : IEngine, IDisposable
	{
		protected IRemotingConnection _connection;
		RemotingProcess _process;
		ParametersRemoting _remotingData;
        ParametersDiagnosticsNative _diagnostics;
		XElement _initialisingXml;
		bool _disposed = false;

		public virtual void Initialise(string initialisingXml)
		{
			Initialise(initialisingXml, null);
		}

		public void Initialise(string initialisingXml, IDocumentAccessor accessor)
		{
			try
			{
				_initialisingXml = XElement.Parse(initialisingXml);

				var arguments = Persistence.Arguments
					.Parse(_initialisingXml, accessor);

                var remotingId = BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Remoting).Id;
                var diagnosticsId = BaseComponentWithEngine.GetArgumentIdentity(
                    BaseComponentWithEngine.ArgsWithEngine.Diagnostics).Id;

				var remoting = arguments
                    .Where(a => a.Id == remotingId)
					.Single();

                var diagnostics = arguments
                    .Where(a => a.Id == diagnosticsId)
                    .Single();

				_remotingData = new ParametersRemoting();
				_remotingData.ValueAsString = remoting.ValueAsString;

                _diagnostics = new ParametersDiagnosticsNative();
                _diagnostics.ValueAsString = diagnostics.ValueAsString;

				if (_remotingData.Protocol == RemotingProtocol.ipcAuto)
				{
					string args = string.Format(
						"ipcAuto {0} {1}", _remotingData.ObjectUri, _remotingData.PortName);

					if (_remotingData.ServerLaunchDebugger)
						args += " launchDebugger"; 
					if (_remotingData.EnsureSecurity)
						args += " ensureSecurity";
					if (_remotingData.ServerTraceEngine)
						args += " traceEngine";

					var serverType = new ExternalType(typeof(RemotingServerEngineTime));

                    WriteLine("IPC Auto Process");
					WriteLine("\tServer: " + serverType.Url.LocalPath, false);
					WriteLine("\tArgs: " + args, false);
                    WriteLine("\tRedirect Standard Output (if true could be VERY slow): "
                        + _remotingData.IpcAutoRedirectStdOut.ToString(), false);

					_process = new RemotingProcess("RemotingServerEngineTime");
                    _process.Start(serverType.Url.LocalPath, args, _remotingData.IpcAutoRedirectStdOut);

                    WriteLine("Process started");
				}

                WriteLine(_remotingData.Details());

				switch (_remotingData.Protocol)
				{
					case RemotingProtocol.ipc:
					case RemotingProtocol.ipcAuto:
						WriteLine("IPC Connection");
						_connection = new RemotingConnectionIpc();
						break;
					case RemotingProtocol.tcp:
						WriteLine("TCP Connection");
						_connection = new RemotingConnectionTcp();
						break;
					case RemotingProtocol.http:
						WriteLine("HTTP Connection");
						_connection = new RemotingConnectionHttp();
						break;
					case RemotingProtocol.inProcess:
					default:
						throw new NotImplementedException(_remotingData.Protocol.ToString());
				}

				WriteLine("\tClient Uri: " + _remotingData.ClientUri, false);
				WriteLine("\tEnsure Security: " + _remotingData.EnsureSecurity, false);
				WriteLine("\tConnection TimeOut: " + _remotingData.ConnectionTimeOut, false);

				_connection.Start(_remotingData.ClientUri, _remotingData.EnsureSecurity, _remotingData.ConnectionTimeOut, typeof(IEngine));

				WriteLine("\tConnection started.", false);
				WriteLine(string.Format("... pause {0} seconds before pinging engine ...", _remotingData.ConnectionSleep / 1000.0));

				Thread.Sleep(_remotingData.ConnectionSleep);

				string ping = EngineProxy.Ping();

				WriteLine("Engine Ping: " + ping);

				WriteLine("Engine Initialising ...");
				EngineProxy.Initialise(_initialisingXml.ToString());
				WriteLine("\tInitialised.", false);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("Initialise", e);
			}
		}

        void WriteLine(string line, bool dated = true)
        {
            if (_diagnostics == null)
                Console.WriteLine(line);
            else if (dated)
                Utilities.Diagnostics.WriteLine(
                    Utilities.Diagnostics.DatedLine(_diagnostics.Caption, "Process started"),
                    _diagnostics.To, null);
            else
                Utilities.Diagnostics.WriteLine(line, _diagnostics.To, null);
        }

		IEngine EngineProxy
		{
			get { return _connection.Base as IEngine; }
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			WriteLine("Disposing engine client ...");

			if (_process != null)
			{
				_process.Dispose();
				_process = null;
			}

			if (_connection != null)
			{
				_connection.Dispose();
				_connection = null;
			}

            WriteLine("... disposed engine client");
		}

		protected void EngineMethodCall(string calling)
		{
			if (_disposed)
				throw new Exception("Engine already disposed");

			string ping = EngineProxy.Ping();

            if (ping == null || ping.ToLower().Contains("error") || ping.ToLower().Contains("exception"))
				throw new Exception(string.Format("Engine ping failure before {0} call: {1}", calling, ping));
		}

		protected Exception EngineMethodCatch(string calling, System.Exception e)
		{
			var err = new Exception("Engine Client call: " + calling, e);
			var s = Utilities.Xml.Persist(err).ToString();

			WriteLine(e.ToString());
			WriteLine(s);

			Trace.TraceError(e.ToString()); 
			Trace.TraceError(s);

			Dispose();

			return err;
		}

		public string Ping()
		{
			try
			{
				if (_disposed)
					throw new Exception("Engine already disposed");

				string ping = EngineProxy.Ping();

				if (ping == null || ping.ToLower().Contains("error"))
					throw new Exception(string.Format("Engine ping failure: {0}", ping));

				return ping;
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("Ping", e);
			}
		}

		public void SetArgument(string key, string value)
		{
			try
			{
				EngineMethodCall("SetArgument");

				EngineProxy.SetArgument(key, value);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetArgument", e);
			}
		}

		public void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetInput");

				EngineProxy.SetInput(engineVariable, elementCount, elementValueCount, vectorLength);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetInput", e);
			}
		}

		public void SetInput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetInput");

				EngineProxy.SetInput(engineVariable, elementCount, elementValueCounts, vectorLength);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetInput", e);
			}
		}

		public void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetOutput");

				EngineProxy.SetOutput(engineVariable, elementCount, elementValueCount, vectorLength);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetOutput", e);
			}
		}

		public void SetOutput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
		{
			try
			{
				EngineMethodCall("SetOutput");

				EngineProxy.SetOutput(engineVariable, elementCount, elementValueCounts, vectorLength);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetOutput", e);
			}
		}

		public void Prepare()
		{
			try
			{
				EngineMethodCall("Prepare");

				EngineProxy.Prepare();
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("Prepare", e);
			}
		}

		public void SetStrings(string engineVariable, string missingValue, string[] values)
		{
			try
			{
				EngineMethodCall("SetStrings");

				EngineProxy.SetStrings(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetStrings", e);
			}
		}

		public void SetInt32s(string engineVariable, int missingValue, int[] values)
		{
			try
			{
				EngineMethodCall("SetInt32s");

				EngineProxy.SetInt32s(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetInt32s", e);
			}
		}

		public void SetDoubles(string engineVariable, double missingValue, double[] values)
		{
			try
			{
				EngineMethodCall("SetDoubles");

				EngineProxy.SetDoubles(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetDoubles", e);
			}
		}

		public void SetBooleans(string engineVariable, bool missingValue, bool[] values)
		{
			try
			{
				EngineMethodCall("SetBooleans");

				EngineProxy.SetBooleans(engineVariable, missingValue, values);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("SetBooleans", e);
			}
		}

		public void Update()
		{
			try
			{
				EngineMethodCall("Update");

				EngineProxy.Update();
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("Update", e);
			}
		}

		public string[] GetStrings(string engineVariable, string missingValue)
		{
			string[] values = null;

			try
			{
				EngineMethodCall("GetStrings");

				values = EngineProxy.GetStrings(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("GetStrings", e);
			}

			return values != null 
				? values 
				: new string[] {};
		}

		public int[] GetInt32s(string engineVariable, int missingValue)
		{
			int[] values = null;

			try
			{
				EngineMethodCall("GetInt32s");

				values = EngineProxy.GetInt32s(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("GetInt32s", e);
			}

			return values != null
				? values
				: new int[] { };
		}

		public double[] GetDoubles(string engineVariable, double missingValue)
		{
			double[] values = null;

			try
			{
				EngineMethodCall("GetDoubles");

				values = EngineProxy.GetDoubles(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("GetDoubles", e);
			}

			return values != null
				? values
				: new double[] { };
		}

		public bool[] GetBooleans(string engineVariable, bool missingValue)
		{
			bool[] values = null;

			try
			{
				EngineMethodCall("GetBooleans");

				values = EngineProxy.GetBooleans(engineVariable, missingValue);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("GetBooleans", e);
			}

			return values != null
				? values
				: new bool[] { };
		}

		public void Finish()
		{
			try
			{
				EngineMethodCall("Finish");

                WriteLine("Calling Server Finish ...");
				EngineProxy.Finish();

				Thread.Sleep(5000);

				WriteLine("... Server Finished", false);
			}
			catch (System.Exception e)
			{
				throw EngineMethodCatch("Finish", e);
			}

			// Shut down server process
			Dispose();
		}
	}
}

