using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchItem : ViewModel
	{
		private readonly Lazy<ObservableCollection<BranchItem>> subItems;
		private static readonly Lazy<ObservableCollection<BranchItem>> NoSubItems
			= new Lazy<ObservableCollection<BranchItem>>(() => new ObservableCollection<BranchItem>());

		public BranchItem(
			Branch branch,
			Command<Branch> branchCommand)
			: this(branch, branchCommand, null)
		{
		}

		public BranchItem(
			Branch branch,
			Command<Branch> showBranchCommand,
			Command<Branch> mergeBranchCommand)
		{
			Text = branch.Name;
			Branch = branch;
			BranchCommand = showBranchCommand;
			MergeBranchCommand = mergeBranchCommand;
			subItems = NoSubItems;
		}

		public BranchItem(
			Branch branch,
			string menuItemText,
			Command<Branch> branchCommand)
			: this(branch, branchCommand, null)
		{
			Text = menuItemText;
		}

		private BranchItem(
			string prefix,
			string name,
			IEnumerable<Branch> branches,
			int level,
			Command<Branch> branchCommand)
		{
			Text = name;
			BranchCommand = branchCommand;
			subItems = new Lazy<ObservableCollection<BranchItem>>(
				() =>
				{
					ObservableCollection<BranchItem> list = new ObservableCollection<BranchItem>();
					GetBranches(prefix, branches, level, branchCommand)
						.ForEach(b => list.Add(b));
					return list;
				});
		}


		public ObservableCollection<BranchItem> Children => subItems.Value;


		public string Text { get; }

		public Branch Branch { get; }

		public Command<Branch> BranchCommand { get; }

		public Command<Branch> MergeBranchCommand { get; }



		public static IReadOnlyList<BranchItem> GetBranches(
			IEnumerable<Branch> branches,
			Command<Branch> branchCommand)
		{
			if (branches.Count() < 21)
			{
				return branches
					.Select(b => new BranchItem(b, branchCommand))
					.ToList();
			}

			return GetBranches("", branches, 0, branchCommand);
		}


		private static IReadOnlyList<BranchItem> GetBranches(
			string prefix,
			IEnumerable<Branch> branches,
			int level,
			Command<Branch> showBranchCommand)
		{
			List<BranchItem> localItems = level != 0
				? Enumerable.Empty<BranchItem>().ToList()
				: branches
					.Where(b => b.IsLocal)
					.Take(10)
					.Select(b => new BranchItem(b, showBranchCommand))
					.ToList();

			List<BranchItem> allItems = new List<BranchItem>();

			foreach (Branch branch in branches.Where(b => b.Name.StartsWith(prefix)))
			{
				string[] nameParts = branch.Name.ToString().Split("/".ToCharArray());
				if (nameParts.Length == level + 1)
				{
					if (level == 0 && !localItems.Any(b => b.Branch == branch))
					{
						localItems.Add(new BranchItem(branch, showBranchCommand));
					}
					else if (level != 0)
					{
						allItems.Add(new BranchItem(branch, showBranchCommand));
					}
				}
				else if (!allItems.Any(n => n.Text == nameParts[level]))
				{
					allItems.Add(new BranchItem(
						prefix + nameParts[level] + "/",
						nameParts[level],
						branches,
						level + 1,
						showBranchCommand));
				}
			}

			allItems = allItems
				.OrderBy(n => n.Branch != null)
				.ThenBy(b => b.Text)
				.ToList();

			return
				localItems
				.OrderBy(b => b.Text)
				.Concat(allItems)
				.ToList();
		}
	}
}