
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using FluidEarth2.Sdk.CoreStandard2;

namespace FluidEarth2.Sdk
{
    public static partial class Utilities
    {
        public static class Remoting
        {
            /*
            public static string PortName(int port)
            {
                return string.Format("FluidEarthEngineServer_{0}", port.ToString());
            }
            */

            public static XDocument ServerConfigXml(RemotingProtocol protocol, string objectUri, string portName, int port, string serverType)
            {
                XElement channel;

                switch (protocol)
                {
                    case RemotingProtocol.ipcAuto:
                    case RemotingProtocol.ipc:
                        channel =
                            new XElement("channel",
                                new XAttribute("ref", "ipc"),
                                new XAttribute("portName", portName));
                        break;
                    case RemotingProtocol.tcp:
                        channel =
                            new XElement("channel",
                                new XAttribute("ref", "tcp"),
                                new XAttribute("port", port));
                        break;
                    case RemotingProtocol.http:
                        channel =
                            new XElement("channel",
                                new XAttribute("ref", "http"),
                                new XAttribute("port", port));
                        break;
                    default:
                        throw new NotImplementedException(protocol.ToString());
                }

                XElement xml = new XElement("configuration",
                    new XElement("system.runtime.remoting",
                        new XElement("application",
                            new XAttribute("name", "FluidEarth2 Engine Server"),
                            new XElement("service",
                                new XElement("wellknown",
                                    new XAttribute("type", serverType),
                                    new XAttribute("objectUri", objectUri),
                                    new XAttribute("mode", "Singleton"))),
                            new XElement("channels", channel))));

                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    xml);
            }

            /// <summary>
            /// Get info about remoting configuration
            /// </summary>
            /// <returns>string info</returns>
            public static string InfoRemotingConfiguration()
            {
                string s = "Remoting Configuration:\r\n";
                s += string.Format("  ProcessId: {0}\r\n", RemotingConfiguration.ProcessId);
                s += string.Format("  ApplicationName: {0}\r\n", RemotingConfiguration.ApplicationName);
                s += string.Format("  ApplicationId: {0}\r\n", RemotingConfiguration.ApplicationId);

                s += "  RegisteredWellKnownClientTypes\r\n";
                foreach (WellKnownClientTypeEntry w in RemotingConfiguration.GetRegisteredWellKnownClientTypes())
                {
                    s += string.Format("    AssemblyName: {0}\r\n", w.AssemblyName);
                    s += string.Format("    ApplicationUrl: {0}\r\n", w.ApplicationUrl);
                    s += string.Format("    ObjectUrl: {0}\r\n", w.ObjectUrl);
                    s += string.Format("    ObjectType: {0}\r\n", w.ObjectType);
                    s += string.Format("    TypeName: {0}\r\n", w.TypeName);
                }

                s += "  RegisteredActivatedClientTypes\r\n";
                foreach (ActivatedClientTypeEntry w in RemotingConfiguration.GetRegisteredActivatedClientTypes())
                {
                    s += string.Format("    AssemblyName: {0}\r\n", w.AssemblyName);
                    s += string.Format("    ApplicationUrl: {0}\r\n", w.ApplicationUrl);
                    s += string.Format("    ObjectType: {0}\r\n", w.ObjectType);
                    s += string.Format("    TypeName: {0}\r\n", w.TypeName);
                }

                s += "  RegisteredWellKnownServiceTypes\r\n";
                foreach (WellKnownServiceTypeEntry w in RemotingConfiguration.GetRegisteredWellKnownServiceTypes())
                {
                    s += string.Format("    AssemblyName: {0}\r\n", w.AssemblyName);
                    s += string.Format("    ObjectUri: {0}\r\n", w.ObjectUri);
                    s += string.Format("    ObjectType: {0}\r\n", w.ObjectType);
                    s += string.Format("    TypeName: {0}\r\n", w.TypeName);
                    s += string.Format("    Mode: {0}\r\n", w.Mode);
                }

                s += "  RegisteredActivatedServiceTypes\r\n";
                foreach (ActivatedServiceTypeEntry w in RemotingConfiguration.GetRegisteredActivatedServiceTypes())
                {
                    s += string.Format("    AssemblyName: {0}\r\n", w.AssemblyName);
                    s += string.Format("    ObjectType: {0}\r\n", w.ObjectType);
                    s += string.Format("    TypeName: {0}\r\n", w.TypeName);
                }

                return s;
            }

            /// <summary>
            /// Get info about remoting configured channels
            /// </summary>
            /// <returns>string info</returns>
            public static string InfoRegisteredChannels()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Registered Channels:");

                ChannelDataStore store;

                foreach (IChannel channel in ChannelServices.RegisteredChannels)
                {
                    sb.AppendLine("  Channel: " + channel.ChannelName);
                    sb.AppendLine("    Priority: " + channel.ChannelPriority);

                    store = null;

                    if (channel is IpcChannel)
                        store = (ChannelDataStore)((IpcChannel)channel).ChannelData;
                    else if (channel is TcpChannel)
                        store = (ChannelDataStore)((TcpChannel)channel).ChannelData;
                    else if (channel is HttpChannel)
                        store = (ChannelDataStore)((HttpChannel)channel).ChannelData;

                    if (store != null)
                    {
                        foreach (string uri in store.ChannelUris)
                        {
                            sb.AppendFormat("    URI: {0}\r\n", uri);
                            sb.AppendLine();
                        }
                    }
                    else
                        sb.AppendLine("    No data store");
                }

                return sb.ToString();
            }

            public static SupportedPlatforms Platforms(RemotingProtocol protocol)
            {
                SupportedPlatforms platforms = 0;

                switch (protocol)
                {
                    case RemotingProtocol.inProcess:
                        platforms |= SupportedPlatforms.All;
                        break;
                    case RemotingProtocol.ipcAuto:
                        platforms |= SupportedPlatforms.Win;
                        break;
                    default:
                        throw new NotImplementedException(protocol.ToString());
                }

                return platforms;
            }
        }
    }


}
