﻿using System;
using System.Runtime.Remoting;
using GitMind.Utils.Ipc;
using NUnit.Framework;


namespace GitMindTest.Utils.Ipc
{
	[TestFixture]
	public class IpcServiceTest
	{
		[Test]
		public void Test()
		{
			string id = Guid.NewGuid().ToString();

			using (IpcRemotingService ipcServerService = new IpcRemotingService())
			{
				Assert.IsTrue(ipcServerService.TryCreateServer(id));

				ipcServerService.PublishService(new ServerSide());

				using (IpcRemotingService ipcClientService = new IpcRemotingService())
				{
					string request = "Some request text";
					string response = ipcClientService.CallService<ServerSide, string>(
						id, service => service.DoubleText(request));

					Assert.AreEqual(request + request, response);
				}
			}
		}

		[Test]
		public void TestIfNoServer()
		{
			string id = Guid.NewGuid().ToString();

			using (IpcRemotingService ipcClientService = new IpcRemotingService())
			{
				string request = "Some request text";

				Assert.Throws<RemotingException>(() => ipcClientService.CallService<ServerSide, string>(
					id, service => service.DoubleText(request)));
			}

		}


		private class ServerSide : IpcService
		{
			public string DoubleText(string request) => request + request;
		}
	}
}