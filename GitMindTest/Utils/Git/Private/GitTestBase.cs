﻿using System.Collections.Generic;
using System.Threading;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel.Private;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	public class GitTestBase<TInterface>
	{
		protected readonly CancellationToken ct = CancellationToken.None;
		private AutoMock am;
		private AutoMock am2;

		protected TInterface cmd => am.Resolve<TInterface>();
		protected TInterface cmd2 => am2.Resolve<TInterface>();

		protected GitHelper git;
		protected GitHelper git2;

		protected IoHelper io;
		protected IoHelper io2;

		protected GitStatus status;
		protected IReadOnlyList<GitBranch> branches;
		protected IReadOnlyList<GitCommit> log;
		protected bool isCleanUp = true;

		[SetUp]
		public void Setup()
		{
			//io.CleanTempDirs();
			io = new IoHelper();
			io2 = new IoHelper();

			status = new GitStatus(0, 0, 0, 0, false, null, new GitFile[0]);
			branches = new GitBranch[0];

			am = new AutoMock()
				.RegisterNamespaceOf<IGitInfoService>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterType<IMessageService>()
				.RegisterSingleInstance(new WorkingFolder(io.WorkingFolder));

			am2 = new AutoMock()
				.RegisterNamespaceOf<IGitInfoService>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterType<IMessageService>()
				.RegisterSingleInstance(new WorkingFolder(io2.WorkingFolder));

			git = new GitHelper(am, io);
			git2 = new GitHelper(am2, io2);
		}


		[TearDown]
		public void Teardown()
		{
			am.Dispose();
			am2.Dispose();
			if (isCleanUp)
			{
				io.CleanTempDirs();
			}
		}
	}
}