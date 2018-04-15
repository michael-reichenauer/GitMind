using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitNotesService2 : IGitNotesService2
	{
		private readonly IGitCmdService gitCmdService;


		public GitNotesService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<string>> GetNoteAsync(string sha, string notesRef, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"-c core.notesRef={notesRef} notes show {sha}", ct);

			if (result.IsFaulted)
			{
				if (result.ExitCode == 1 && result.Error.StartsWith($"error: no note found for object {sha}"))
				{
					return Error.NoValue;
				}

				return Error.From("Failed to get note", result.AsError());
			}

			string notes = result.Output;
			Log.Info($"Got note {notes.Length} length");
			return notes;
		}


		public async Task<R> AddNoteAsync(
			string sha, string notesRef, string note, CancellationToken ct)
		{
			Log.Debug($"Adding {note.Length}chars on {sha} {notesRef} ...");

			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"-c core.notesRef={notesRef} notes add -f --allow-empty -m\"{note}\" {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to add note", result.AsError());
			}


			Log.Info($"Added note {note.Length} length");
			return R.Ok;
		}

		public async Task<R> AppendNoteAsync(
			string sha, string notesRef, string note, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"-c core.notesRef={notesRef} notes append --allow-empty -m\"{note}\" {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to add note", result.AsError());
			}


			Log.Info($"Added note {note.Length} length");
			return R.Ok;
		}

		public async Task<R> RemoveNoteAsync(string sha, string notesRef, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"-c core.notesRef={notesRef} notes remove --ignore-missing {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to remove note", result.AsError());
			}

			Log.Info($"Removed note");
			return R.Ok;
		}
	}
}