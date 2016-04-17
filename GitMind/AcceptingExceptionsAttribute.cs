using System;
using System.Runtime.CompilerServices;


namespace GitMind
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public sealed class AcceptingExceptionsAttribute : Attribute
	{
		public Type ExceptionType { get; }
		public string ExternalLocation { get; }
		public string MemberName { get; }
		public string SourceFilePath { get; }
		public int SourceLineNumber { get; }

		public AcceptingExceptionsAttribute(
			Type exceptionType,
			string externalLocation = null,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!exceptionType.IsSubclassOf(typeof(Exception)))
			{
				throw new ArgumentException($"Not an exception type: {nameof(exceptionType)}");
			}

			ExceptionType = exceptionType;
			ExternalLocation = externalLocation;
			MemberName = memberName;
			SourceFilePath = sourceFilePath;
			SourceLineNumber = sourceLineNumber;
		}	
	}
}