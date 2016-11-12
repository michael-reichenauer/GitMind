using System;
using System.Threading.Tasks;


namespace GitMind.Common.ProgressHandling
{
	internal interface IProgressService
	{
		//void Show(string text, Func<Task> progressAction);
		//void Show(Func<ProgressState, Task> progressAction);
		//void Show(string text, Func<ProgressState, Task> progressAction);
		//T Show<T>(string text, Func<Task<T>> progressAction);
		//T Show<T>(Func<ProgressState, Task<T>> progressAction);
		//T Show<T>(string text, Func<ProgressState, Task<T>> progressAction);
		Progress ShowDialog(string text = "");
		void SetText(string text);
	}
}