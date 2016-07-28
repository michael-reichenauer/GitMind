using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchItem : ViewModel
	{
		private readonly Lazy<IReadOnlyList<BranchItem>> subItems;
		private static readonly Lazy<IReadOnlyList<BranchItem>> NoSubItems
			= new Lazy<IReadOnlyList<BranchItem>>(() => new BranchItem[0]);


		public BranchItem(
			Branch branch, 
			Command<Branch> showBranchCommand, 
			Command<Branch> mergeBranchCommand)
		{
			Text = branch.Name;
			Branch = branch;
			ShowBranchCommand = showBranchCommand;
			MergeBranchCommand = mergeBranchCommand;
			subItems = NoSubItems;
		}


		private BranchItem(
			string prefix,
			string name,
			IEnumerable<Branch> branches,
			int level,
			Command<Branch> showBranchCommand,
			Command<Branch> mergeBranchCommand)
		{
			Text = name;
			ShowBranchCommand = showBranchCommand;
			MergeBranchCommand = mergeBranchCommand;
			subItems = new Lazy<IReadOnlyList<BranchItem>>(
				() => GetBranches(prefix, branches, level, showBranchCommand, mergeBranchCommand));
		}


		public IReadOnlyList<BranchItem> Children => subItems.Value;


		public string Text { get; }

		public Branch Branch { get; }

		public Command<Branch> ShowBranchCommand { get; }

		public Command<Branch> MergeBranchCommand { get; }



		public static IReadOnlyList<BranchItem> GetBranches(
			IEnumerable<Branch> branches, 
			Command<Branch> showBranchCommand, 
			Command<Branch> mergeBranchCommand)
		{
			if (branches.Count() < 20)
			{
				return branches.Select(b => new BranchItem(b, showBranchCommand, mergeBranchCommand)).ToList();
			}

			return GetBranches("", branches, 0, showBranchCommand, mergeBranchCommand);
		}


		private static IReadOnlyList<BranchItem> GetBranches(
			string prefix, 
			IEnumerable<Branch> branches,
			int level, 
			Command<Branch> showBranchCommand,
			Command<Branch> mergeBranchCommand)
		{
			List<BranchItem> list = new List<BranchItem>();

			foreach (Branch branch in branches.Where(b => b.Name.StartsWith(prefix)))
			{
				string[] nameParts = branch.Name.Split("/".ToCharArray());
				if (nameParts.Length == level + 1)
				{
					list.Add(new BranchItem(branch, showBranchCommand, mergeBranchCommand));
				}
				else if (!list.Any(n => n.Text == nameParts[level]))
				{
					list.Add(new BranchItem(
						prefix + nameParts[level] + "/",
						nameParts[level],
						branches,
						level + 1,
						showBranchCommand,
						mergeBranchCommand));
				}
			}

			return list
				.OrderBy(n => n.Branch != null)
				.ThenBy(b => b.Text)
				.ToList();
		}
	}
}