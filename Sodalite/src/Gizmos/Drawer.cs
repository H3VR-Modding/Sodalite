using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Popcron
{
	public abstract class Drawer
	{
		private static Dictionary<Type, Drawer>? _typeToDrawer;

		public abstract int Draw(ref Vector3[] buffer, params object[] args);

		public static Drawer? Get<T>() where T : class
		{
			//find all drawers
			if (_typeToDrawer == null)
			{
				_typeToDrawer = new Dictionary<Type, Drawer>
				{
					{typeof(CubeDrawer), new CubeDrawer()},
					{typeof(LineDrawer), new LineDrawer()},
					{typeof(PolygonDrawer), new PolygonDrawer()},
					{typeof(SquareDrawer), new SquareDrawer()}
				};

				//add defaults

				//find extras
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					Type[] types = assembly.GetTypes();
					foreach (Type type in types)
					{
						if (type.IsAbstract) continue;

						if (type.IsSubclassOf(typeof(Drawer)) && !_typeToDrawer.ContainsKey(type))
							try
							{
								Drawer value = (Drawer) Activator.CreateInstance(type);
								_typeToDrawer[type] = value;
							}
							catch (Exception e)
							{
								// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
								Debug.LogError($"Couldn't register drawer of type {type} because {e.Message}");
							}
					}
				}
			}

			return _typeToDrawer.TryGetValue(typeof(T), out Drawer drawer) ? drawer : null;
		}
	}
}
