using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sodalite
{


	/// <summary>
	/// A serializable version of Unity's Vector2
	/// </summary>
	public class Vector2Serializable
	{
		public float x;
		public float y;


		public Vector2Serializable() { }

		public Vector2Serializable(Vector2 v)
		{
			x = v.x;
			y = v.y;
		}

		public Vector2 GetVector2()
		{
			return new Vector2(x, y);
		}
	}


	/// <summary>
	/// A serializable version of Unity's Vector3
	/// </summary>
	public class Vector3Serializable
	{
		public float x;
		public float y;
		public float z;

		public Vector3Serializable() { }

		public Vector3Serializable(Vector3 v)
		{
			x = v.x;
			y = v.y;
			z = v.z;
		}

		public Vector3 GetVector3()
		{
			return new Vector3(x, y, z);
		}
	}
}
