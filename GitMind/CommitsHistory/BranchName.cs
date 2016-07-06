using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchName : ViewModel
	{
		public BranchName(Branch branch, ICommand showBranchCommand)
		{
			Branch = branch;
			ShowBranchCommand = showBranchCommand;
		}


		public Branch Branch { get; }

		public ICommand ShowBranchCommand { get; }

		public string Text => Branch.Name;
	}


	internal class BranchName2 : ViewModel
	{
		private readonly Lazy<IReadOnlyList<BranchName2>> subItems;
		private static readonly Lazy<IReadOnlyList<BranchName2>> NoSubItems
			= new Lazy<IReadOnlyList<BranchName2>>(() => new BranchName2[0]);


		public BranchName2(
			string prefix,
			string name,
			IEnumerable<Branch> branches,
			int level,
			ICommand showBranchCommand)
		{
			Text = name;
			ShowBranchCommand = showBranchCommand;
			subItems = new Lazy<IReadOnlyList<BranchName2>>(
				() => GetBranches(prefix, branches, level, showBranchCommand));
		}

		public BranchName2(Branch branch, ICommand showBranchCommand)
		{
			Text = branch.Name;
			Branch = branch;
			ShowBranchCommand = showBranchCommand;
			subItems = NoSubItems;
		}


		public IReadOnlyList<BranchName2> Children => subItems.Value;


		public string Text { get; }

		public Branch Branch { get; }

		public ICommand ShowBranchCommand { get; }


		public static IReadOnlyList<BranchName2> GetBranches(
			IEnumerable<Branch> branches, ICommand showBranchCommand)
		{
			return GetBranches("", branches, 0, showBranchCommand);
		}


		private static IReadOnlyList<BranchName2> GetBranches(
			string prefix, IEnumerable<Branch> branches, int level, ICommand showBranchCommand)
		{
			Log.Warn($"Get for {prefix}");

			List<BranchName2> list = new List<BranchName2>();

			foreach (Branch branch in branches.Where(b => b.Name.StartsWith(prefix)))
			{
				string[] nameParts = branch.Name.Split("/".ToCharArray());
				if (nameParts.Length == level + 1)
				{
					list.Add(new BranchName2(branch, showBranchCommand));
				}
				else if (!list.Any(n => n.Text == nameParts[level]))
				{
					list.Add(new BranchName2(
						prefix + nameParts[level] + "/",
						nameParts[level],
						branches,
						level + 1,
						showBranchCommand));
				}
			}

			return list
				.OrderBy(n => n.Branch != null)
				.ThenBy(b => b.Text)
				.ToList();
		}
	}
}