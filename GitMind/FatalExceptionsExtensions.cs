using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using GitMind;


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


		public static bool IsNotFatal(this Exception e)
		{
			Exception exception = e;
			Type exceptionType = e.GetType();
			AggregateException aggregateException = exception as AggregateException;

			if (aggregateException != null)
			{
				exception = aggregateException.InnerException;
				exceptionType = exception.GetType();
			}

			if (FatalTypes.Contains(exceptionType))
			{
				ExceptionHandling.Shutdown($"Exception type is fatal: {exceptionType}", exception);
				return false;
			}

			return true;
		}
	}
}