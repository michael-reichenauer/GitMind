using System;
using System.Threading;
using GitMind.ApplicationHandling;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	public class GitTestBase<TInterface>
	{
		private Lazy<TInterface> resolved;
		protected readonly CancellationToken ct = CancellationToken.None;
		protected AutoMock am;
		protected TInterface gitCmd => resolved.Value;


		[SetUp]
		public void Setup()
		{
			am = new AutoMock()
				.RegisterNamespaceOf<IGitVersion>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterSingleInstance(new WorkingFolderPath(@"C:\Work Files\GitMind"));
			resolved = new Lazy<TInterface>(() => am.Resolve<TInterface>());
		}


		[TearDown]
		public void Teardown() => am.Dispose();
	}
}