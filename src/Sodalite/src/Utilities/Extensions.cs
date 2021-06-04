using System;
using System.Collections.Generic;

namespace Sodalite.Utilities
{
	public static class Extensions
	{
		private static Random? _random;

		/// <summary>
		/// Returns a random item from this list
		/// </summary>
		public static T GetRandom<T>(this IList<T> list)
		{
			// Make sure there is at least one item in the list
			if (list.Count < 1)
				throw new InvalidOperationException("Cannot get random item from empty list!");

			// Make sure our random is set and return a random item
			_random ??= new Random();
			return list[_random.Next(list.Count)];
		}
	}
}
