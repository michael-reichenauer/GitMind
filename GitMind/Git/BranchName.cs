using System;
using System.Security.Cryptography;
using System.Windows.Markup;
using GitMind.Utils;


namespace GitMind.Git
{
	public class BranchName : Equatable<BranchName>, IComparable
	{
		private readonly string name;
		private readonly string id;
		private readonly int hashCode;

		public static BranchName Master = new BranchName("master");
		public static BranchName OriginHead = new BranchName("origin/HEAD");
		public static BranchName Head = new BranchName("HEAD");


		public BranchName(string name)
		{
			this.name = name;
			id = name.ToLower();
			hashCode = id.GetHashCode();
		}


		public string Name => name;
		

		public static BranchName From(string name) => name != null ? new BranchName(name) : null;

		protected override bool IsEqual(BranchName other) => id == other.id;

		protected override int GetHash() => hashCode;

		public bool StartsWith(string prefix) => 
			id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

		public BranchName Substring(int length) => BranchName.From(name.Substring(length));


		//public static implicit operator string(BranchName branchName) => branchName.name;


		//public static implicit operator BranchName(string branchName) => new BranchName(branchName);


		public override string ToString() => name;

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;

			BranchName otherTemperature = obj as BranchName;
			if (otherTemperature != null)
			{
				return string.Compare(id, otherTemperature.id, StringComparison.Ordinal);
			}
			else
			{
				throw new ArgumentException("Object is not a BranchName");
			}
		}
	}
}