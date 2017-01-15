using System.ComponentModel;
using System.Runtime.Serialization;
using GitMind.Git;


namespace GitMind.GitModel.Private.Caching
{
	[DataContract, TypeConverter(typeof(BranchName))]
	internal class BranchNameSurrogate
	{
		[DataMember]
		public string Name { get; set; }

		public static implicit operator BranchNameSurrogate(BranchName branchName) =>
			branchName != null ? new BranchNameSurrogate { Name = branchName.ToString() } : null;

		public static implicit operator BranchName(BranchNameSurrogate branchName)=>
			branchName != null ? new BranchName(branchName.Name) : null;
	}
}