﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitMind.Utils;


namespace GitMind.Common
{
	internal class CommitId : Equatable<CommitId>
	{
		private static readonly string UncommittedId = new string('0', 40);
		public static readonly CommitId Uncommitted = new CommitId(UncommittedId);


		public CommitId(string commitIdSha)
		{		
			Id = CommitIds.GetId(commitIdSha);
			Sha = commitIdSha;
		}


		public CommitId(int id)
		{
			Id = id;
			Sha = CommitIds.GetSha(id);
		}


		public CommitId(int id, string sha)
		{
			Id = id;
			Sha = sha;
		}

		public int Id { get; }

		public string Sha { get; }

		//public static implicit operator string(CommitId commitId) => commitId.Sha;

		protected override bool IsEqual(CommitId other) => Id == other.Id;

		protected override int GetHash() => Id;
	}
}
