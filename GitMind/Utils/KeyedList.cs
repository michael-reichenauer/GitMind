using System;
using System.Collections.ObjectModel;


namespace GitMind.Utils
{
	internal class KeyedList<TKey, TValue> : KeyedCollection<TKey, TValue>
	{
		private readonly Func<TValue, TKey> getKeyForItem;

		public KeyedList(Func<TValue, TKey> getKeyForItem)
		{
			this.getKeyForItem = getKeyForItem;
		}


		public bool TryGetValue(TKey key, out TValue value)
		{
			if (Contains(key))
			{
				value = this[key];
				return true;
			}

			value = default(TValue);
			return false;
		}


		protected override TKey GetKeyForItem(TValue item)
		{
			return getKeyForItem(item);
		}
	}
}