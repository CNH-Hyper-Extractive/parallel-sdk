
using System;
using System.Collections;
using System.Diagnostics;

using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	/// <summary>
	/// Implementation of IConnection for HTTP remoting
	/// </summary>
    public class RemotingConnectionHttp : IRemotingConnection
	{
		HttpClientChannel _channel;
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

			IDictionary t = new Hashtable();
			t.Add("timeout", timeOut);
			t.Add("name", urlClient);

#if MONO
			_sinkProvider = new BinaryClientFormatterSinkProvider();
#endif

#if MONO
/* MoMA moans about this line, but not clear why?
Message	10	MonoTodo: void HttpClientChannel..ctor(IDictionary, IClientChannelSinkProvider)
Reason: Handle the machineName, proxyName, proxyPort, servicePrincipalName, useAuthenticatedConnectionSharing properties	D:\Source\Projects\FluidEarth_Trunk\FluidEarth\SDK\src\ConnectionHttp.cs	148	14	PluginInterfaces
*/
			_channel = new HttpClientChannel(t, _sinkProvider);
#else
			// need to make ChannelNames unique so need to use this
			// constructor even though we dont care about the sink provider
			_channel = new HttpClientChannel(t, _sinkProvider);
#endif

			ChannelServices.RegisterChannel(_channel, _ensureSecurity);

			Trace.TraceInformation("Configured client connection");

			_base = (IBase)Activator.GetObject(iProxyType, urlClient);

			Trace.TraceInformation("Acquired proxy");
		}

		/// <summary>
		/// Get base engine proxy
		/// </summary>
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


