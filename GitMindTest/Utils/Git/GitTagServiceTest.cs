using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitTagServiceTest : GitTestBase<IGitTagService2>
	{
		[Test]
		public async Task TestTagsAsync()
		{
			await git.InitRepoAsync();

			// Add some commits
			io.WriteFile("file1.txt", "text1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message1");
			io.WriteFile("file2.txt", "text2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message12");

			// Get list of tag should be empty
			R<IReadOnlyList<GitTag>> tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(true, tags.IsOk);
			Assert.AreEqual(0, tags.Value.Count);

			// Add tag 1
			R<GitTag> tag1 = await cmd.AddTagAsync(commit1.Sha.Sha, "tag1", CancellationToken.None);
			Assert.AreEqual(true, tag1.IsOk);

			// Get a list of 1 tag
			tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(true, tags.IsOk);
			Assert.AreEqual(1, tags.Value.Count);
			Assert.AreEqual(commit1.Sha.Sha, tags.Value[0].CommitId);
			Assert.AreEqual("tag1", tags.Value[0].TagName);

			// Add tag 2
			R<GitTag> tag2 = await cmd.AddTagAsync(commit2.Sha.Sha, "tag2", CancellationToken.None);
			Assert.AreEqual(true, tag2.IsOk);

			// Get a list of 2 tags
			tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(2, tags.Value.Count);
			Assert.IsNotNull(tags.Value.FirstOrDefault(t => t.CommitId == commit1.Sha.Sha));
			Assert.IsNotNull(tags.Value.FirstOrDefault(t => t.CommitId == commit2.Sha.Sha));

			// Cannot add tag2 again
			R<GitTag> tag22 = await cmd.AddTagAsync(commit2.Sha.Sha, "tag2", CancellationToken.None);
			Assert.AreEqual(false, tag22.IsOk);
			tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(2, tags.Value.Count);

			// Delete tag2
			R delete = await cmd.DeleteTagAsync("tag2", CancellationToken.None);
			Assert.AreEqual(true, delete.IsOk);
			tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(1, tags.Value.Count);
			Assert.IsNotNull(tags.Value.FirstOrDefault(t => t.CommitId == commit1.Sha.Sha));

			// Now it is possible to add tag2 again since it was deleted
			tag22 = await cmd.AddTagAsync(commit2.Sha.Sha, "tag2", CancellationToken.None);
			Assert.AreEqual(true, tag22.IsOk);
			tags = await cmd.GetAllTagsAsync(CancellationToken.None);
			Assert.AreEqual(2, tags.Value.Count);

			// can nor delete unknown tag
			R delete2 = await cmd.DeleteTagAsync("tag_unknown", CancellationToken.None);
			Assert.AreEqual(false, delete2.IsOk);
		}
	}
}