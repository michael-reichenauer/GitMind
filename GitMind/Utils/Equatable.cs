using System;


namespace GitMind.Utils
{
	/// <summary>
	/// Base class to help implement IEquatable'T' interface
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Equatable<T> : IEquatable<T>
	{
		// Sample class, which inherits Equatable<T>:
		// private class Id : Equatable<Id>
		// {
		//	 private readonly string id;
		//
		//	 public Id(string id)
		//	 {
		//	 	this.id = id;
		//	 }
		//
		//	 protected override bool IsEqual(Id other) => id == other.id;
		//	 protected override int GetHash() => id?.GetHashCode() ?? 0;
		// }
		protected abstract bool IsEqual(T other);

		protected abstract int GetHash();

		public bool Equals(T other) => (other != null) && IsEqual(other);

		public override bool Equals(object other) => other is T && Equals((T)other);

		public static bool operator ==(Equatable<T> obj1, Equatable<T> obj2) =>
			Equatable.IsEqual(obj1, obj2);

		public static bool operator !=(Equatable<T> obj1, Equatable<T> obj2) => !(obj1 == obj2);

		public override int GetHashCode() => GetHash();
	}


	/// <summary>
	/// Helper class to implement IEquatable'T'  interface
	/// </summary>
	public class Equatable
	{
		// Sample class, which implement Equatable:
		// private class Id : IEquatable<Id>
		// {
		//	 private readonly string id;
		//
		//	 public Id(string id)
		//	 {
		//	 	this.id = id;
		//	 }
		//
		//	 public bool Equals(Id other) => this == other;
		//   public override bool Equals(object other) => other is Id && Equals((Id)other);
		//	 public static bool operator ==(Id obj1, Id obj2) =>
		//	 	Equatable.IsEqual(obj1, obj2, (o1, o2) => o1.id == o2.id);
		//	 public static bool operator !=(Id obj1, Id obj2) => !(obj1 == obj2);
		//	 public override int GetHashCode() => id.GetHashCode();
		// }

		public static bool IsEqual<T>(T obj1, T obj2, Func<T, T, bool> predicate)
		{
			if (ReferenceEquals(obj1, null) && ReferenceEquals(obj2, null))
			{
				return true;
			}
			else if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
			{
				return false;
			}
			else
			{
				return predicate(obj1, obj2);
			}
		}

		public static bool IsEqual<T>(IEquatable<T> obj1, IEquatable<T> obj2)
		{
			if (ReferenceEquals(obj1, null) && ReferenceEquals(obj2, null))
			{
				return true;
			}
			else if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
			{
				return false;
			}
			else
			{
				return obj1.Equals(obj2);
			}
		}
	}
}