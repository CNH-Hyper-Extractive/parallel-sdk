
using System;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	/// <summary>
	/// Common interface for generating a remoting IEngine? proxy
	/// </summary>
	public interface IRemotingConnection : IDisposable
	{
		/// <summary>
		/// Start the connection
		/// </summary>
		/// <param name="typeEngineInterface">Type of IEngine interface</param>
		/// <param name="urlClient">Asscoiated URL</param>
		/// <param name="ensureSecurity">Remoting security attribute</param>
		void Start(string urlClient, bool ensureSecurity, uint timeOut, Type iProxyType);

		IBase Base { get; }
	}
}

