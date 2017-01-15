using System;


namespace GitMind.Utils
{
	/// <summary>
	/// Attribute used to mark types that should be registered as a single instance in
	/// dependency injection.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
	public sealed class SingleInstanceAttribute : Attribute
	{
	}
}