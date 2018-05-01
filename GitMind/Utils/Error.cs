using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace GitMind.Utils
{
	//public class Error : Equatable<Error>
	//{
	//	private static readonly string NoErrorText = "No error";
	//	private static readonly string NoValueText = "No value";


	//	private readonly string stackTrace;


	//	private Error(string message, string details, string stackTrace, Error inner = null)
	//	{
	//		this.stackTrace = stackTrace;
	//		Message = message;
	//		Exception = new Exception(message);
	//		Inner = inner;

	//		if (message != NoErrorText && message != NoValueText)
	//		{
	//			Log.Warn($"{this}");
	//		}
	//	}


	//	private Error(Exception e)
	//	{
	//		stackTrace = e.StackTrace;
	//		Message = "";
	//		Exception = e;
	//	}

		

	//	public static Error None { get; } = new Error(NoErrorText, "", "");
	//	public static Error NoValue { get; } = new Error(NoValueText, "", "");

	//	public Exception Exception { get; }

	//	public string Message { get; }
	//	public string StackTrace => Inner != null ? $"{stackTrace}\n--- Inner:\n{Inner.StackTrace}" : stackTrace;


	//	public IEnumerable<string> AllMessages()
	//	{
	//		yield return Message;
	//		if (Inner != null)
	//		{
	//			foreach (string innerMessage in Inner.AllMessages().Where(m => !string.IsNullOrEmpty(m)))
	//			{
	//				yield return innerMessage;
	//			}
	//		}
	//	}



	//	public Error Inner { get; }


	//	//public string StackTrace => Exception.StackTrace != null ? $"at:\n{Exception.StackTrace}" : null;


	//	public static Error From(
	//		string message,
	//		[CallerMemberName] string memberName = "",
	//		[CallerFilePath] string sourceFilePath = "",
	//		[CallerLineNumber] int sourceLineNumber = 0) =>
	//		new Error(message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber));


	//	public static Error From(
	//		string message,
	//		Error error,
	//		[CallerMemberName] string memberName = "",
	//		[CallerFilePath] string sourceFilePath = "",
	//		[CallerLineNumber] int sourceLineNumber = 0) =>
	//		new Error(message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber), error);


	//	public static Error From(
	//		Exception e,
	//		[CallerMemberName] string memberName = "",
	//		[CallerFilePath] string sourceFilePath = "",
	//		[CallerLineNumber] int sourceLineNumber = 0) =>
	//		new Error(e.Message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber), new Error(e));


	//	public static Error From(
	//		string message,
	//		Exception e,
	//		[CallerMemberName] string memberName = "",
	//		[CallerFilePath] string sourceFilePath = "",
	//		[CallerLineNumber] int sourceLineNumber = 0) =>
	//		new Error(message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber), 
	//			new Error(e.Message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber), new Error(e)));


	//	public static Error From(
	//		string message, 
	//		R result,
	//		[CallerMemberName] string memberName = "",
	//		[CallerFilePath] string sourceFilePath = "",
	//		[CallerLineNumber] int sourceLineNumber = 0) =>
	//		new Error(message, ToStackTrace(memberName, sourceFilePath, sourceLineNumber), result.Error);



	//	private static string ToStackTrace(string memberName, string sourceFilePath, int sourceLineNumber) =>
	//		$"at {sourceFilePath}({sourceLineNumber}){memberName}";


	//	//public static implicit operator Error(Exception e) => From(e);


	//	public bool Is<T>()
	//	{
	//		return this is T || Exception is T;
	//	}


	//	protected override bool IsEqual(Error other)
	//	{
	//		if ((ReferenceEquals(this, None) && !ReferenceEquals(other, None))
	//				|| !ReferenceEquals(this, None) && ReferenceEquals(other, None))
	//		{
	//			return false;
	//		}

	//		return
	//			(Exception == null && other.Exception == null && GetType() == other.GetType())
	//			|| other.GetType().IsInstanceOfType(this)
	//			|| (GetType() == other.GetType() && Exception != null && other.Exception != null
	//				&& other.Exception.GetType().IsInstanceOfType(this));
	//	}

	//	protected override int GetHash() => 0;


	//	public override string ToString() => string.Join(",\n", AllMessages()) + $"\n{StackTrace}";
	//
	//}
}