using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;


namespace System
{
	internal static class FatalExceptionsExtensions
	{
		private static readonly IReadOnlyList<Type> FatalTypes = new[]
		{
			typeof(ArgumentException),
			typeof(AccessViolationException),
			typeof(AppDomainUnloadedException),
			typeof(ArithmeticException),
			typeof(ArrayTypeMismatchException),
			typeof(BadImageFormatException),
			typeof(CannotUnloadAppDomainException),
			typeof(ContextMarshalException),
			typeof(DataMisalignedException),
			typeof(IndexOutOfRangeException),
			typeof(InsufficientExecutionStackException),
			typeof(InvalidCastException),
			typeof(InvalidOperationException),
			typeof(InvalidProgramException),
			typeof(MemberAccessException),
			typeof(MulticastNotSupportedException),
			typeof(NotImplementedException),
			typeof(NotSupportedException),
			typeof(NullReferenceException),
			typeof(OutOfMemoryException),
			typeof(RankException),
			typeof(AmbiguousMatchException),
			typeof(InvalidComObjectException),
			typeof(InvalidOleVariantTypeException),
			typeof(MarshalDirectiveException),
			typeof(SafeArrayRankMismatchException),
			typeof(SafeArrayTypeMismatchException),
			typeof(StackOverflowException),
			typeof(TypeInitializationException),
		};


		public static event EventHandler<FatalExceptionEventArgs> FatalExeption;


		public static bool IsNotFatal(this Exception e)
		{
			Exception exception = e;
			Type exceptionType = e.GetType();
			
			if (FatalTypes.Contains(exceptionType))
			{
				StackTrace stackTrace = new StackTrace(1, true);
				string stackTraceText = stackTrace.ToString();
				string message = $"Exception type is fatal: {exceptionType}, {e}\n at \n{stackTraceText}";

				FatalExeption?.Invoke(null, new FatalExceptionEventArgs(message, exception));
				return false;
			}

			return true;
		}
	}
}