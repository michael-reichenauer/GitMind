using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;


namespace GitMind.Utils.UI.Ipc
{
	internal class IpcRemotingService : IDisposable
	{
		private string uniqueId;
		private Mutex channelMutex;
		private IpcServerChannel ipcServerChannel;
		private IpcClientChannel ipcClientChannel;


		public bool TryCreateServer(string serverId)
		{
			uniqueId = serverId;
			string mutexName = GetId(serverId);
			string channelName = GetChannelName(serverId);

			bool isMutexCreated;
			channelMutex = new Mutex(true, mutexName, out isMutexCreated);
			if (isMutexCreated)
			{
				CreateIpcServer(channelName);
			}

			return isMutexCreated;
		}


		public void PublishService(IpcService ipcService)
		{
			Asserter.Requires(ipcServerChannel != null);

			// Publish the ipc service receiving the data
			string ipcServiceName = uniqueId + ipcService.GetType().FullName;
			RemotingServices.Marshal(ipcService, ipcServiceName);
		}


		public void CallService<TRemoteService>(
			string serverId, Action<TRemoteService> serviceAction)
		{
			TRemoteService ipcProxy = CreateClientProxy<TRemoteService>(serverId);

			serviceAction(ipcProxy);
		}


		public TResult CallService<TRemoteService, TResult>(
			string serverId, Func<TRemoteService, TResult> serviceFunction)
		{
			TRemoteService ipcProxy = CreateClientProxy<TRemoteService>(serverId);

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
				Log.Exception(e, "Failed to close RPC remoting service");
			}
		}


		private void CreateIpcServer(string channelName)
		{
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

			IDictionary properties = new Dictionary<string, string>();
			properties["name"] = channelName;
			properties["portName"] = channelName;
			properties["exclusiveAddressUse"] = "false";

			ipcServerChannel = new IpcServerChannel(properties, serverProvider);
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

			// Get proxy instance of rpc service instance published by server in PublishService()
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