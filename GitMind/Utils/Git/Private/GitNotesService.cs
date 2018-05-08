using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitNotesService : IGitNotesService
	{
		private readonly IGitCmdService gitCmdService;


		public GitNotesService(IGitCmdService gitCmdService)
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
					return R.NoValue;
				}

				return R.Error("Failed to get note", result.AsException());
			}

			string notes = result.Output;
			Log.Info($"Got note {notes.Length} length");
			return notes;
		}


		public async Task<R> AddNoteAsync(
			string sha, string notesRef, string note, CancellationToken ct)
		{
			Log.Debug($"Adding {note.Length}chars on {sha} {notesRef} ...");

			string filePath = Path.GetTempFileName();
			File.WriteAllText(filePath, note);
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"-c core.notesRef={notesRef} notes add -f --allow-empty -F \"{filePath}\" {sha}", ct);

			DeleteNotesFile(filePath);

			if (result.IsFaulted)
			{
				return R.Error("Failed to add note", result.AsException());
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
				return R.Error("Failed to add note", result.AsException());
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
				return R.Error("Failed to remove note", result.AsException());
			}

			Log.Info($"Removed note");
			return R.Ok;
		}


		private static void DeleteNotesFile(string filePath)
		{
			try
			{
				File.Delete(filePath);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete temp notes file {filePath}, {e.Message}");
			}
		}
	}
}