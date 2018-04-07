using System.Collections.Generic;
using System.Threading;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	public class GitTestBase<TInterface>
	{
		private AutoMock am;

		protected readonly CancellationToken ct = CancellationToken.None;
		protected TInterface cmd => am.Resolve<TInterface>();
		protected GitHelper git;
		protected IoHelper io;

		protected Status2 status;
		protected IReadOnlyList<GitBranch2> branches;


		[SetUp]
		public void Setup()
		{
			// CleanTempDirs();
			io = new IoHelper();

			status = new Status2(0, 0, 0, 0, new GitFile2[0]);
			branches = new GitBranch2[0];

			am = new AutoMock()
				.RegisterNamespaceOf<IGitInfoService>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterType<IMessageService>()
				.RegisterSingleInstance(new WorkingFolderPath(io.WorkingFolder));

			git = new GitHelper(am, io.WorkingFolder, io);

		}


		[TearDown]
		public void Teardown()
		{
			am.Dispose();
			// CleanTempDirs();
		}
	}
}