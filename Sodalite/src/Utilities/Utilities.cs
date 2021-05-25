using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Sodalite
{
	public static class Utilities
	{
		/// <summary>
		///		Helper method for loading a texture from a path
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static Texture2D LoadTextureFromFile(string file)
		{
			byte[] fileData = File.ReadAllBytes(file);
			Texture2D tex = new(0, 0);
			tex.LoadImage(fileData);
			return tex;
		}

		/// <summary>
		///		Extension method for catching TypeReflectionLoadExceptions when using Assembly.GetTypes()
		/// </summary>
		/// <param name="asm">The assembly to get the types from</param>
		/// <returns>The types from the assembly that were able to load properly</returns>
		public static IEnumerable<Type> GetTypesSafe(this Assembly asm)
		{
			try
			{
				return asm.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t is not null);
			}
		}

		/// <summary>
		///		Extension method to check if the value has a given flag
		/// </summary>
		/// <param name="value">The value to check on</param>
		/// <param name="flag">The flag to check for</param>
		/// <typeparam name="TEnum">The type of the enum</typeparam>
		/// <returns>True if the value has a given flag set</returns>
		public static bool HasFlag<TEnum>(this TEnum value, TEnum flag) where TEnum : Enum
		{
			return (Convert.ToInt32(value) & Convert.ToInt32(flag)) != 0;
		}
	}
}
