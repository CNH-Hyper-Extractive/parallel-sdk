
using System;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	public enum RemotingProtocol
	{
		inProcess = 0,
		ipcAuto, // Automatically start IPC server in new process, windows only
		ipc,
		tcp,
		http,
	}

	public class ParametersRemoting
	{
		RemotingProtocol _protocol = RemotingProtocol.inProcess;

		string _host = System.Environment.MachineName;
		string _objectUri = "MyComposition.rem";
		string _portName = "MyComposition";
		string _portGuid = Guid.NewGuid().ToString();
		int _port = 6666;

		string _clientUri = string.Empty;

		int _connectionSleep = 5000;
		uint _connectionTimeOut = 5000U;
		bool _ensureSecurity = false;
		bool _serverLaunchDebugger = false;
		bool _serverTraceEngine = false;
        bool _ipcAutoRedirectStdOut = false;

		public RemotingProtocol Protocol { get { return _protocol; } }

		public string Host { get { return _host; } }
		public string ObjectUri { get { return _objectUri; } }
		public string PortName { get { return _portName; } }
		public int Port { get { return _port; } }
		public string ClientUri { get { return _clientUri; } }

		public int ConnectionSleep 
		{ 
			get { return _connectionSleep; }
			set { _connectionSleep = value; }
		}

		public uint ConnectionTimeOut 
		{ 
			get { return _connectionTimeOut; }
			set { _connectionTimeOut = value; }
		}

		public bool EnsureSecurity 
		{ 
			get { return _ensureSecurity; }
			set { _ensureSecurity = value; }
		}

		public bool ServerLaunchDebugger 
		{ 
			get { return _serverLaunchDebugger; }
			set { _serverLaunchDebugger = value; }
		}

		public bool ServerTraceEngine 
		{ 
			get { return _serverTraceEngine; }
			set { _serverTraceEngine = value; }
		}

        public bool IpcAutoRedirectStdOut
        {
            get { return _ipcAutoRedirectStdOut; }
            set { _ipcAutoRedirectStdOut = value; }
        }

		public ParametersRemoting()
		{
			UpdateValues();
		}

		public ParametersRemoting(RemotingProtocol protocol)
		{
			_protocol = protocol;

			UpdateValues();
		}

		public void SetInProcess()
		{
			_protocol = RemotingProtocol.inProcess;

			UpdateValues();
		}

		public void SetIpcAuto(bool redirectStdOut = false)
		{
			_protocol = RemotingProtocol.ipcAuto;
            _ipcAutoRedirectStdOut = redirectStdOut;

			UpdateValues();
		}

		public void SetIpc(string objectUri, string portName)
		{
			_protocol = RemotingProtocol.ipc;
			_objectUri = objectUri;
			_portName = portName;

			UpdateValues();
		}

		public void SetTcp(string objectUri, string host, int port)
		{
			_protocol = RemotingProtocol.tcp;
			_objectUri = objectUri;
			_host = host;
			_port = port;

			UpdateValues();
		}

		public void SetHttp(string objectUri, string host, int port)
		{
			_protocol = RemotingProtocol.http;
			_objectUri = objectUri;
			_host = host;
			_port = port;

			UpdateValues();
		}

		public override string ToString()
		{
			return Protocol.ToString();
		}

		void UpdateValues()
		{
			if (Protocol == RemotingProtocol.inProcess)
				return;

#if MONO
			if (_protocol == RemotingProtocol.ipcAuto)
			{
				throw new Exception("MONO does not support ipcAuto");
			}
#endif			

			if (Protocol == RemotingProtocol.inProcess)
			{
				_portName = string.Empty;
				_objectUri = string.Empty;
				_clientUri = string.Empty;
			}
			else if (Protocol == RemotingProtocol.ipcAuto)
			{
				_portName = _portGuid;
				_objectUri = PortName + ".rem";
				_clientUri = string.Format("ipc://{0}/{1}", PortName, ObjectUri);
			}
			else
			{
				if (ObjectUri == null)
					throw new Exception("Missing ObjectUri");

				if (!ObjectUri.EndsWith(".rem") && !ObjectUri.EndsWith(".soap"))
					throw new Exception("ObjectUri must end in either .rem or .soap");

				if (Protocol == RemotingProtocol.ipc)
				{
					if (PortName == null)
						throw new Exception("Missing PortName");

					_clientUri = string.Format("ipc://{0}/{1}", PortName, ObjectUri);
				}
				else
				{
					if (Host == null)
						throw new Exception("Missing Host");
					if (Port < 0)
						throw new Exception("Missing Port");

					if (Protocol == RemotingProtocol.tcp)
						_clientUri = string.Format("tcp://{0}:{1}/{2}", Host, Port, ObjectUri);
					else if (Protocol == RemotingProtocol.http)
						_clientUri = string.Format("http://{0}:{1}/{2}.rem", Host, Port, ObjectUri);
					else
						throw new NotImplementedException(Protocol.ToString());
				}
			}
		}

		public string Details()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Remoting Details:");
			sb.AppendLine("\tClient Uri: " + ClientUri.ToString());
			sb.AppendLine("\tProtocol: " + Protocol.ToString());

			if (Protocol == RemotingProtocol.ipcAuto)
			{
				sb.AppendLine("\tPortName: " + _portName.ToString());
				sb.AppendLine("\tObjectUri: " + _objectUri.ToString());
				sb.AppendLine("\tClientUri: " + _clientUri.ToString());
                sb.AppendLine("\tRedirectStdOut: " + _ipcAutoRedirectStdOut.ToString());
			}

			return sb.ToString();
		}

		public void Initialise(XElement xInitialise, IDocumentAccessor accessor)
		{
		}

		public XElement Persist(IDocumentAccessor accessor)
		{
			return new XElement("RemoteData");
		}

		public string ValueAsString
		{
			get
			{
				StringBuilder sb
					= new StringBuilder(Protocol.ToString());

				if (_protocol == RemotingProtocol.inProcess)
					return sb.ToString();

				sb.Append("^" + ConnectionSleep.ToString());
				sb.Append("^" + ConnectionTimeOut.ToString());
				sb.Append("^" + EnsureSecurity.ToString());
				sb.Append("^" + ServerLaunchDebugger.ToString()); ;
				sb.Append("^" + ServerTraceEngine.ToString());

				if (_protocol != RemotingProtocol.ipcAuto)
				{
					sb.Append("^" + ObjectUri);

					if (_protocol == RemotingProtocol.ipc)
						sb.Append("^" + PortName);
					else
					{
						sb.Append("^" + Host);
						sb.Append("^" + Port.ToString());
					}
				}
                else
                    sb.Append("^" + IpcAutoRedirectStdOut.ToString());

				return sb.ToString();
			}

			set
			{
				string[] parts = value.Split('^');

				_protocol = (RemotingProtocol)Enum.Parse(typeof(RemotingProtocol), parts[0]);

				if (_protocol != RemotingProtocol.inProcess)
				{
					_connectionSleep = int.Parse(parts[1]);
					_connectionTimeOut = uint.Parse(parts[2]);
					_ensureSecurity = bool.Parse(parts[3]);
					_serverLaunchDebugger = bool.Parse(parts[4]);
					_serverTraceEngine = bool.Parse(parts[5]);

                    if (_protocol != RemotingProtocol.ipcAuto)
                    {
                        _objectUri = parts[6];

                        if (_protocol == RemotingProtocol.ipc)
                            _portName = parts[7];
                        else
                        {
                            _host = parts[7];
                            _port = int.Parse(parts[8]);
                        }
                    }
                    else
                        _ipcAutoRedirectStdOut = 
                            parts.Length < 7 || bool.Parse(parts[6]);
				}

				UpdateValues();
			}
		}
	};
}
