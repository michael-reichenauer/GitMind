using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.GitModel;


namespace GitMind.ApplicationHandling.Private
{
    internal class GitSettings : IGitSettings
    {
        private readonly WorkingFolder workingFolder;


        public GitSettings(WorkingFolder workingFolder)
        {
            this.workingFolder = workingFolder;
        }

        public bool IsRemoteDisabled => Settings.Get<Options>().AutoRemoteCheckIntervalMin == 0;
        public string WorkingFolderPath => workingFolder.Path;
    }
}