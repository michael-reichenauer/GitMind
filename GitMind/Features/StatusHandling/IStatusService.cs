﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Features.StatusHandling.Private;


namespace GitMind.Features.StatusHandling
{
	internal interface IStatusService
	{
		event EventHandler<StatusChangedEventArgs> StatusChanged;

		event EventHandler<RepoChangedEventArgs> RepoChanged;

		void Monitor(string workingFolder);
		IDisposable PauseStatusNotifications(Refresh refresh = Refresh.None);
		Task<Status> GetStatusAsync();
		Status GetStatus();
		IReadOnlyList<string> GetRepoIds();
		Task<IReadOnlyList<string>> GetRepoIdsAsync();
	}
}