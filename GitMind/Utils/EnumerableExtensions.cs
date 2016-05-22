﻿using System;
using System.Collections.Generic;



namespace System.Linq
{
	public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
		{
			foreach (T item in enumeration)
			{
				action(item);
			}
		}

		public static IReadOnlyList<TSource> AsReadOnlyList<TSource>(this IReadOnlyList<TSource> source)
		{
			return source;
		}
	}
}