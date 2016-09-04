using System;
using GitMind.Utils;
using NUnit.Framework;


namespace GitMindTest.Utils
{
	/// <summary>
	///  Test class to test IEquatable
	/// </summary>
	[TestFixture]
	public class EquatableTest
	{
		[Test]
		public void TestEqualId()
		{
			Id idSome1 = new Id("some");
			Id idSome2 = new Id("some");
			Id idOther = new Id("other");
			Id idNull1 = null;
			Id idNull2 = null;

			Assert.AreEqual(idSome1, idSome1);
			Assert.AreEqual(idSome1, idSome2);
			Assert.AreEqual(idNull1, idNull2);
			Assert.AreEqual(new Id(null), new Id(null));

			Assert.IsTrue(idSome1 == idSome2);
			Assert.IsTrue(idNull1 == idNull2);
			Assert.IsTrue(new Id(null) == new Id(null));

			Assert.AreNotEqual(idSome1, idOther);
			Assert.AreNotEqual(idSome1, idNull1);
			Assert.IsTrue(idSome1 != idOther);
			Assert.IsTrue(idSome1 != idNull1);
		}


		[Test]
		public void TestEqualImpl()
		{
			IdImpl idSome1 = new IdImpl("some");
			IdImpl idSome2 = new IdImpl("some");
			IdImpl idOther = new IdImpl("other");
			IdImpl idNull1 = null;
			IdImpl idNull2 = null;

			Assert.AreEqual(idSome1, idSome1);
			Assert.AreEqual(idSome1, idSome2);
			Assert.AreEqual(idNull1, idNull2);
			Assert.AreEqual(new IdImpl(null), new IdImpl(null));

			Assert.IsTrue(idSome1 == idSome2);
			Assert.IsTrue(idNull1 == idNull2);
			Assert.IsTrue(new Id(null) == new Id(null));

			Assert.AreNotEqual(idSome1, idOther);
			Assert.AreNotEqual(idSome1, idNull1);

			Assert.IsTrue(idSome1 != idOther);
			Assert.IsTrue(idSome1 != idNull1);
		}


		[Test]
		public void TestHashId()
		{
			Assert.AreEqual("some".GetHashCode(), new Id("some").GetHashCode());
			Assert.AreEqual(0, new Id(null).GetHashCode());
		}


		[Test]
		public void TestHashImpl()
		{
			Assert.AreEqual("some".GetHashCode(), new IdImpl("some").GetHashCode());
			Assert.AreEqual(0, new Id(null).GetHashCode());
		}


		// Class, which inherits Equatable<T>
		private class Id : Equatable<Id>
		{
			private readonly string id;

			public Id(string id)
			{
				this.id = id;
			}

			protected override bool IsEqual(Id other) => id == other.id;
			protected override int GetHash() => id?.GetHashCode() ?? 0;
		}

		// Class, which implements IEquatable<T>, by using Equatable helper class
		private class IdImpl : IEquatable<IdImpl>
		{
			private readonly string id;

			public IdImpl(string id)
			{
				this.id = id;
			}

			public bool Equals(IdImpl other) => this == other;

			public override bool Equals(object obj) => obj is IdImpl && Equals((IdImpl)obj);

			public static bool operator ==(IdImpl obj1, IdImpl obj2) =>
				Equatable.IsEqual(obj1, obj2, (o1, o2) => o1.id == o2.id);

			public static bool operator !=(IdImpl obj1, IdImpl obj2) => !(obj1 == obj2);

			public override int GetHashCode() => id.GetHashCode();
		}
	}
}