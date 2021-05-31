using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sodalite
{
	public static class Extensions
	{
		private static Random? random;

		/// <summary>
		/// Returns a random item from the list when called
		/// </summary>
		/// <returns>
		/// A random item from the list
		/// </returns>
		/// <exception cref="IndexOutOfRangeException">
		/// Thrown when the given list is empty
		/// </exception>
		public static T GetRandom<T>(this List<T> list)
		{
			if (list.Count < 1)
				throw new IndexOutOfRangeException("Cannot get random item from empty list!");

			if (random is null) random = new Random();

			return list[random.Next(list.Count)];
		}
	}
}
