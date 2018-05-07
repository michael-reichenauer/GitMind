using System;
using System.Reflection;


namespace GitMind.Utils
{
	public class Error : R
	{
		public Error(
			string message,
			string memberName,
			string sourceFilePath,
			int sourceLineNumber = 0)
			: this(new Exception(message), ToStackTrace(memberName, sourceFilePath, sourceLineNumber)) { }


		public Error(
			string message,
			Exception e,
			string memberName = "",
			string sourceFilePath = "",
			int sourceLineNumber = 0)
			: this(new Exception(message, e), ToStackTrace(memberName, sourceFilePath, sourceLineNumber)) { }


		public Error(
			Exception e,
			string memberName = "",
			string sourceFilePath = "",
			int sourceLineNumber = 0)
			: this(e, ToStackTrace(memberName, sourceFilePath, sourceLineNumber)) { }


		private Error(Exception e, string stackTrace) : base(AddStackTrace(e, stackTrace))
		{
			if (e != NoError && e != NoValueError)
			{
				Log.Warn($"{this}");
			}
		}


		private static string ToStackTrace(string memberName, string sourceFilePath, int sourceLineNumber) =>
			$"at {sourceFilePath}({sourceLineNumber}){memberName}";

		private static Exception AddStackTrace(Exception exception, string stackTrace)
		{
			if (stackTrace == null)
			{
				return exception;
			}

			FieldInfo field = typeof(Exception).GetField(
				"_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

			string stack = (string)field?.GetValue(exception);
			stackTrace = string.IsNullOrEmpty(stack) ? stackTrace : $"{stackTrace}\n{stack}";
			field?.SetValue(exception, stackTrace);
			return exception;
		}
	}
}