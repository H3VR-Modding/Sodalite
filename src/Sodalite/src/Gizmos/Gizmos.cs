using System;
using UnityEngine;
// ReSharper disable All
#pragma warning disable 1591

namespace Popcron
{
	public static class Gizmos
	{

		/// <summary>
		/// By default, it will always render to scene view camera and the main camera.
		/// Subscribing to this allows you to whitelist your custom cameras.
		/// </summary>
		public static readonly Func<Camera, bool> CameraFilter = cam => false;


		/// <summary>
		/// The size of the total gizmos buffer.
		/// Default is 4096.
		/// </summary>
		public static int BufferSize { get; set; } = 4096;

		/// <summary>
		/// Toggles whether the gizmos could be drawn or not.
		/// </summary>
		public static bool Enabled { get; set; } = true;

		/// <summary>
		/// The size of the gap when drawing dashed elements.
		/// Default gap size is 0.1
		/// </summary>
		public static float DashGap { get; set; } = 0.1f;

		/// <summary>
		/// Should the camera not draw elements that are not visible?
		/// </summary>
		public static bool FrustumCulling { get; set; } = true;

		/// <summary>
		/// The material being used to render.
		/// </summary>
		public static Material Material
		{
			get => GizmosInstance.Material;
			set => GizmosInstance.Material = value;
		}

		/// <summary>
		/// Rendering pass to activate.
		/// </summary>
		public static int Pass { get; set; } = 0;

		/// <summary>
		/// Global offset for all points. Default is (0, 0, 0).
		/// </summary>
		public static Vector3 Offset { get; set; } = Vector3.zero;

		private static Vector3[] _buffer = new Vector3[BufferSize];

		/// <summary>
		/// Draws an element onto the screen.
		/// </summary>
		public static void Draw<T>(Color? color, bool dashed, params object[] args) where T : Drawer
		{
			if (!Enabled) return;

			Drawer? drawer = Drawer.Get<T>();
			if (drawer != null)
			{
				int points = drawer.Draw(ref _buffer, args);

				//copy from buffer and add to the queue
				Vector3[] array = new Vector3[points];
				Array.Copy(_buffer, array, points);
				GizmosInstance.Submit(array, color, dashed);
			}
		}

		/// <summary>
		/// Draws an array of lines. Useful for things like paths.
		/// </summary>
		public static void Lines(Vector3[] lines, Color? color = null, bool dashed = false)
		{
			if (!Enabled) return;

			GizmosInstance.Submit(lines, color, dashed);
		}

		/// <summary>
		/// Draw line in world space.
		/// </summary>
		public static void Line(Vector3 a, Vector3 b, Color? color = null, bool dashed = false)
		{
			Draw<LineDrawer>(color, dashed, a, b);
		}

		/// <summary>
		/// Draw square in world space.
		/// </summary>
		public static void Square(Vector2 position, Vector2 size, Color? color = null, bool dashed = false)
		{
			Square(position, Quaternion.identity, size, color, dashed);
		}

		/// <summary>
		/// Draw square in world space with float diameter parameter.
		/// </summary>
		public static void Square(Vector2 position, float diameter, Color? color = null, bool dashed = false)
		{
			Square(position, Quaternion.identity, Vector2.one * diameter, color, dashed);
		}

		/// <summary>
		/// Draw square in world space with a rotation parameter.
		/// </summary>
		public static void Square(Vector2 position, Quaternion rotation, Vector2 size, Color? color = null, bool dashed = false)
		{
			Draw<SquareDrawer>(color, dashed, position, rotation, size);
		}

		/// <summary>
		/// Draws a cube in world space.
		/// </summary>
		public static void Cube(Vector3 position, Quaternion rotation, Vector3 size, Color? color = null, bool dashed = false)
		{
			Draw<CubeDrawer>(color, dashed, position, rotation, size);
		}

		/// <summary>
		/// Draws a rectangle in screen space.
		/// </summary>
		public static void Rect(Rect rect, Camera camera, Color? color = null, bool dashed = false)
		{
			rect.y = Screen.height - rect.y;
			Vector2 corner = camera.ScreenToWorldPoint(new Vector2(rect.x, rect.y - rect.height));
			Draw<SquareDrawer>(color, dashed, corner + rect.size * 0.5f, Quaternion.identity, rect.size);
		}

		/// <summary>
		/// Draws a representation of a bounding box.
		/// </summary>
		public static void Bounds(Bounds bounds, Color? color = null, bool dashed = false)
		{
			Draw<CubeDrawer>(color, dashed, bounds.center, Quaternion.identity, bounds.size);
		}

		/// <summary>
		/// Draws a cone similar to the one that spot lights draw.
		/// </summary>
		public static void Cone(Vector3 position, Quaternion rotation, float length, float angle, Color? color = null, bool dashed = false, int pointsCount = 16)
		{
			//draw the end of the cone
			float endAngle = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;
			Vector3 forward = rotation * Vector3.forward;
			Vector3 endPosition = position + forward * length;
			Draw<PolygonDrawer>(color, dashed, endPosition, pointsCount, endAngle, 0f, rotation);

			//draw the 4 lines
			for (int i = 0; i < 4; i++)
			{
				float a = i * 90f * Mathf.Deg2Rad;
				Vector3 point = rotation * new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * endAngle;
				Line(position, position + point + forward * length, color, dashed);
			}
		}

		/// <summary>
		/// Draws a sphere at position with specified radius.
		/// </summary>
		public static void Sphere(Vector3 position, float radius, Color? color = null, bool dashed = false, int pointsCount = 16)
		{
			Draw<PolygonDrawer>(color, dashed, position, pointsCount, radius, 0f, Quaternion.Euler(0f, 0f, 0f));
			Draw<PolygonDrawer>(color, dashed, position, pointsCount, radius, 0f, Quaternion.Euler(90f, 0f, 0f));
			Draw<PolygonDrawer>(color, dashed, position, pointsCount, radius, 0f, Quaternion.Euler(0f, 90f, 90f));
		}

		/// <summary>
		/// Draws a circle in world space and billboards towards the camera.
		/// </summary>
		public static void Circle(Vector3 position, float radius, Camera camera, Color? color = null, bool dashed = false, int pointsCount = 16)
		{
			Quaternion rotation = Quaternion.LookRotation(position - camera.transform.position);
			Draw<PolygonDrawer>(color, dashed, position, pointsCount, radius, 0f, rotation);
		}

		/// <summary>
		/// Draws a circle in world space with a specified rotation.
		/// </summary>
		public static void Circle(Vector3 position, float radius, Quaternion rotation, Color? color = null, bool dashed = false, int pointsCount = 16)
		{
			Draw<PolygonDrawer>(color, dashed, position, pointsCount, radius, 0f, rotation);
		}
	}
}
