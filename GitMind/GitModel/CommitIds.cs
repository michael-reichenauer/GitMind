namespace GitMind.GitModel
{
	//internal static class CommitIds
	//{
	//	private static readonly Dictionary<string, CommitId> commitIdbySha 
	//		= new Dictionary<string, CommitId>();
	//	private static readonly Dictionary<int, CommitId> commitIdByInt
	//		= new Dictionary<int, CommitId>();


	//	public static CommitId Get(string commitIdSha)
	//	{
	//		CommitId commitId;
	//		if (!commitIdbySha.TryGetValue(commitIdSha, out commitId))
	//		{
	//			int id = commitIdbySha.Count;
	//			commitId = new CommitId(id, commitIdSha);
	//			commitIdbySha[commitIdSha] = commitId;
	//			commitIdByInt[id] = commitId;
	//		}

	//		return commitId;
	//	}


	//	public static void Set(string sha, int id)
	//	{
	//		CommitId commitId = new CommitId(id, sha);
	//		commitIdbySha[sha] = commitId;
	//		commitIdByInt[id] = commitId;
	//	}


	//	public static int GetId(string commitIdSha)
	//	{
	//		CommitId commitId = Get(commitIdSha);
			
	//		return commitId.Id;
	//	}

	//	public static CommitId Get(int id)
	//	{
	//		CommitId commitId;
	//		if (!commitIdByInt.TryGetValue(id, out commitId))
	//		{
	//			Asserter.FailFast($"Failed to get commit id {id}");
	//		}

	//		return commitId;
	//	}

	//	public static string GetSha(int id)
	//	{
	//		CommitId commitId;
	//		if (!commitIdByInt.TryGetValue(id, out commitId))
	//		{
	//			Asserter.FailFast($"Failed to get commit id {id}");
	//		}

	//		return commitId.Sha;
	//	}


	//	public static Dictionary<string, int> GetIntByShas()
	//	{
	//		Dictionary<string, int> intByShas = new Dictionary<string, int>();

	//		foreach (var pair in commitIdbySha)
	//		{
	//			intByShas[pair.Key] = pair.Value.Id;
	//		}

	//		return intByShas;
	//	}


	//	public static void Clear()
	//	{
	//		commitIdbySha.Clear();
	//		Set(CommitId.Uncommitted.Sha, CommitId.Uncommitted.Id);
	//		Set(CommitId.None.Sha, CommitId.None.Id);
	//	}
	//}
}