using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitNotesServiceTest : GitTestBase<IGitNotesService2>
	{
		[Test]
		public async Task TestNotesAsync()
		{
			string notesRef = "refs/notes/origin/GitMind.Branches";
			await git.InitRepoAsync();
			// Add some commits
			io.WriteFile("file1.txt", "text1");
			GitCommit root = await git.CommitAllChangesAsync("Message1");
			io.WriteFile("file1.txt", "text2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message2");

			// Get notes, should fail since no notes have been added
			R<string> note = await cmd.GetNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(false, note.IsOk);

			// Add note 1
			string notesText1 = "111 ref1\n222 ref2\n";
			R result = await cmd.AddNoteAsync(root.Sha.Sha, notesRef, notesText1, ct);
			Assert.AreEqual(true, result.IsOk);

			// Should get the same notes
			note = await cmd.GetNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(true, note.IsOk);
			Assert.AreEqual(notesText1, note.Value);

			// Add note 2
			string notesText2 = notesText1 + "333 ref3\n444 ref4\n";
			result = await cmd.AddNoteAsync(root.Sha.Sha, notesRef, notesText2, ct);
			Assert.AreEqual(true, result.IsOk);

			// And get current notes
			note = await cmd.GetNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(true, note.IsOk);
			Assert.AreEqual(notesText2, note.Value);

			// Add note 3
			string notesText3 = notesText2 + "555 ref5\n666 ref6\n";
			result = await cmd.AddNoteAsync(root.Sha.Sha, notesRef, notesText3, ct);
			Assert.AreEqual(true, result.IsOk);

			// And get current notes
			note = await cmd.GetNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(true, note.IsOk);
			Assert.AreEqual(notesText3, note.Value);

			// Remove note
			result = await cmd.RemoveNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(true, result.IsOk);

			// Should not get note
			note = await cmd.GetNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(false, note.IsOk);

			// Should succeed to remove second time (already removed)
			result = await cmd.RemoveNoteAsync(root.Sha.Sha, notesRef, ct);
			Assert.AreEqual(true, result.IsOk);
		}


		[Test, Explicit]
		public async Task TestGetGitMindNotesAsync()
		{
			var note1 = await cmd.GetNoteAsync("fae1e7", "refs/notes/GitMind.Branches", ct);
			var note2 = await cmd.GetNoteAsync("fae1e7", "refs/notes/origin/GitMind.Branches", ct);
			var note3 = await cmd.GetNoteAsync("fae1e7", "refs/notes/origin/GitMind.Branches.Manual", ct);
		}


	}
}