namespace GitMind.ApplicationHandling
{
	internal interface IApplicationService
	{
		void SetIsStarted();

		void Start();

		bool IsActivatedOtherInstance();

		bool IsCommands();

		void HandleCommands();

		void TryDeleteTempFiles();
	}
}