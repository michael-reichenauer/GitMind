using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;


namespace GitMind.Utils
{
	internal class RemotingService : IDisposable
	{
		private string serverId;
		private Mutex channelMutex;
		private IpcServerChannel ipcServerChannel;
		private IpcClientChannel ipcClientChannel;


		public bool TryCreateServer(string id)
		{
			serverId = id;
			string mutexName = GetId(id);
			string channelName = GetChannelName(id);

			bool isMutexCreated;
			channelMutex = new Mutex(true, mutexName, out isMutexCreated);
			if (isMutexCreated)
			{
				CreateIpcServer(channelName);
			}

			return isMutexCreated;
		}


		public void PublishService(RemoteService remoteService)
		{
			Asserter.Requires(ipcServerChannel != null);

			// Publish the ipc service receiving the data
			string ipcServiceName = serverId + remoteService.GetType().FullName;
			RemotingServices.Marshal(remoteService, ipcServiceName);
		}


		public void CallService<TRemoteService>(
			string id, Action<TRemoteService> serviceAction)
		{
			TRemoteService ipcProxy = CreateClientProxy<TRemoteService>(id);

			serviceAction(ipcProxy);
		}


		public TResult CallService<TRemoteService, TResult>(
			string id, Func<TRemoteService, TResult> serviceFunction)
		{
			TRemoteService ipcProxy = CreateClientProxy<TRemoteService>(id);

			return serviceFunction(ipcProxy);
		}


		public void Dispose()
		{
			try
			{
				if (channelMutex != null)
				{
					channelMutex.Close();
					channelMutex = null;
				}

				if (ipcServerChannel != null)
				{
					ChannelServices.UnregisterChannel(ipcServerChannel);
					ipcServerChannel = null;
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn("Failed to close RPC remoting service");
			}
		}


		private void CreateIpcServer(string channelName)
		{
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
			IDictionary props = new Dictionary<string, string>();

			props["name"] = channelName;
			props["portName"] = channelName;
			props["exclusiveAddressUse"] = "false";

			ipcServerChannel = new IpcServerChannel(props, serverProvider);
			ChannelServices.RegisterChannel(ipcServerChannel, true);
		}


		private T CreateClientProxy<T>(string id)
		{
			string channelName = GetChannelName(id);

			if (ipcClientChannel == null)
			{
				ipcClientChannel = new IpcClientChannel();

				ChannelServices.RegisterChannel(ipcClientChannel, true);
			}

			string ipcServiceName = id + typeof(T).FullName;
			string ipcUrl = $"ipc://{channelName}/{ipcServiceName}";

			// Get proxy instance of the rpc service instance published by server in CreateRemotingServer()
			T ipcProxy = (T)RemotingServices.Connect(typeof(T), ipcUrl);

			if (ipcProxy == null)
			{
				Log.Error($"Failed to makeIPC call {channelName}");
			}
		
			return ipcProxy;
		}


		private static string GetChannelName(string uniqueName) => $"{GetId(uniqueName)}:rpc";

		private static string GetId(string uniqueName) => uniqueName + Environment.UserName;
	}
}