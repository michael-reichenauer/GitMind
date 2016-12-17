using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitMind.Common;


namespace GitMind.GitModel.Private.Caching
{
	[DataContract, TypeConverter(typeof(CommitIntBySha))]
	internal class XCommitIntByShaSurrogate
	{
		[DataMember]
		public Dictionary<string, int> CommitIdToInt { get; set; }

		public static implicit operator XCommitIntByShaSurrogate(CommitIntBySha commitIntBySha)
		{
			if (commitIntBySha == null)
			{
				return null;
			}

			var intByShas = CommitIds.GetIntByShas();

			if (intByShas.Count <= 2)
			{
				return null;
			}

			return new XCommitIntByShaSurrogate { CommitIdToInt = intByShas };
		}

		public static implicit operator CommitIntBySha(XCommitIntByShaSurrogate commitIntBySha)
		{
			if (commitIntBySha == null)
			{
				return null;
			}

			foreach (var pair in commitIntBySha.CommitIdToInt)
			{
				CommitIds.Set(pair.Key, pair.Value);
			}

			return new CommitIntBySha();
		}
	}
}