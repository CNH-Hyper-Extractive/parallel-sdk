
using System;
using System.Collections;
using System.Diagnostics;

using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	/// <summary>
	/// Implementation of IConnection for IPC remoting
	/// </summary>
	public class RemotingConnectionIpc : IRemotingConnection
	{
		IpcClientChannel _channel;
		IBase _base;
		IClientChannelSinkProvider _sinkProvider = null;
		bool _ensureSecurity = false;

		#region IConnection Members

		/// <summary>
		/// Start connection with specified engine interface
		/// </summary>
		/// <param name="typeEngineInterface">Type of engine interface</param>
		/// <param name="urlClient">Asscoiated URL</param>
		/// <param name="ensureSecurity">Remoting security attribute</param>
		public void Start(string urlClient, bool ensureSecurity, uint timeOut, Type iProxyType)
		{
			Trace.TraceInformation("Configuring client connection");

			_ensureSecurity = ensureSecurity;

#if MONO
			_sinkProvider = new BinaryClientFormatterSinkProvider();
#endif

			IDictionary t = new Hashtable();
			t.Add("timeout", timeOut);
			t.Add("name", urlClient);

			Console.WriteLine("New IPC channel");

			// need to make ChannelNames unique so need to use this
			// constructor even though we dont care about the sink provider		
			_channel = new IpcClientChannel(urlClient, _sinkProvider);

			Console.WriteLine("\tRegister");

			ChannelServices.RegisterChannel(_channel, _ensureSecurity);

			Console.WriteLine("\tActivate object proxy to IEngine");

			_base = (IBase)Activator.GetObject(iProxyType, urlClient);

			Console.WriteLine("\tActivated.");
		}

		public IBase Base
		{
			get { return _base; }
		}

		/// <summary>
		/// Clear up connection resources
		/// </summary>
		public void Dispose()
		{
			if (_channel != null)
			{
				Trace.TraceInformation("Unregistering Channel: " + _channel.ChannelName);
				ChannelServices.UnregisterChannel(_channel);
				Trace.TraceInformation("Unregistered");
			}
		}

		#endregion
	}
}

