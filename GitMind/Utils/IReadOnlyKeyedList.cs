using System.Collections;
using System.Collections.Generic;


namespace GitMind.Utils
{
	internal interface IReadOnlyKeyedList<in TKey, TValue> : IReadOnlyList<TValue>
	{
		TValue this[TKey key] { get; }
		int IndexOf(TValue item);
		bool TryGetValue(TKey key, out TValue value);
		bool Contains(TKey key);
		bool Contains(TValue item);
	}
}