using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitNotesService
	{
		Task<R<string>> GetNoteAsync(string sha, string notesRef, CancellationToken ct);


		Task<R> AddNoteAsync(
			string sha, string notesRef, string note, CancellationToken ct);


		Task<R> RemoveNoteAsync(string sha, string notesRef, CancellationToken ct);


		Task<R> AppendNoteAsync(
			string sha, string notesRef, string note, CancellationToken ct);
	}
}