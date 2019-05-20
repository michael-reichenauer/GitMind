namespace GitMind.GitModel
{
    public interface IGitSettings
    {
        bool IsRemoteDisabled { get; }
        string WorkingFolderPath { get; }
    }
}