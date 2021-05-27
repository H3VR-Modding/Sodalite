using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

#nullable disable
public struct ScriptableRenderContext
{
}

public static class RenderPipelineManager
{
#pragma warning disable 67
	public static event Action<ScriptableRenderContext, Camera> EndCameraRendering;
#pragma warning restore 67
}

namespace Popcron
{
	[ExecuteInEditMode]
	[AddComponentMenu("")]
	public class GizmosInstance : MonoBehaviour
	{
		private const int DefaultQueueSize = 4096;

		private static GizmosInstance _instance;
		private static bool _hotReloaded = true;
		private static Material _defaultMaterial;

		private Material _overrideMaterial;
		private int _queueIndex;
		private int _lastFrame;
		private Element[] _queue = new Element[DefaultQueueSize];

		/// <summary>
		/// The material being used to render
		/// </summary>
		public static Material Material
		{
			get
			{
				GizmosInstance inst = GetOrCreate();
				return inst._overrideMaterial ? inst._overrideMaterial : DefaultMaterial;
			}
			set
			{
				GizmosInstance inst = GetOrCreate();
				inst._overrideMaterial = value;
			}
		}

		/// <summary>
		/// The default line renderer material
		/// </summary>
		private static Material DefaultMaterial
		{
			get
			{
				if (!_defaultMaterial)
				{
					// Unity has a built-in shader that is useful for drawing
					// simple colored things.
					Shader shader = Shader.Find("Hidden/Internal-Colored");
					_defaultMaterial = new Material(shader)
					{
						hideFlags = HideFlags.HideAndDontSave
					};

					// Turn on alpha blending
					// ReSharper disable Unity.PreferAddressByIdToGraphicsParams
					_defaultMaterial.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
					_defaultMaterial.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
					_defaultMaterial.SetInt("_Cull", (int) CullMode.Off);
					_defaultMaterial.SetInt("_ZWrite", 0);
					// ReSharper restore Unity.PreferAddressByIdToGraphicsParams
				}

				return _defaultMaterial;
			}
		}

		private static GizmosInstance GetOrCreate()
		{
			if (_hotReloaded || !_instance)
			{
				// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
				var gizmosInstances = FindObjectsOfType<GizmosInstance>();
				for (int i = 0; i < gizmosInstances.Length; i++)
				{
					_instance = gizmosInstances[i];

					//destroy any extra gizmo instances
					if (i > 0)
					{
						if (Application.isPlaying)
							Destroy(gizmosInstances[i]);
						else
							DestroyImmediate(gizmosInstances[i]);
					}
				}

				//none were found, create a new one
				if (!_instance)
				{
					// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
					_instance = new GameObject(typeof(GizmosInstance).FullName).AddComponent<GizmosInstance>();
					_instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
				}

				_hotReloaded = false;
			}

			return _instance;
		}

		/// <summary>
		/// Submits an array of points to draw into the queue.
		/// </summary>
		internal static void Submit(Vector3[] points, Color? color, bool dashed)
		{
			// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
			GizmosInstance inst = GetOrCreate();

			//if new frame, reset index
			if (inst._lastFrame != Time.frameCount)
			{
				inst._lastFrame = Time.frameCount;
				inst._queueIndex = 0;
			}

			//exceeded the length, so make it even bigger
			if (inst._queueIndex >= inst._queue.Length)
			{
				var bigger = new Element[inst._queue.Length + DefaultQueueSize];
				for (int i = inst._queue.Length; i < bigger.Length; i++) bigger[i] = new Element();

				Array.Copy(inst._queue, 0, bigger, 0, inst._queue.Length);
				inst._queue = bigger;
			}

			inst._queue[inst._queueIndex].Color = color ?? Color.white;
			inst._queue[inst._queueIndex].Points = points;
			inst._queue[inst._queueIndex].Dashed = dashed;

			inst._queueIndex++;
		}

		private void OnEnable()
		{
			//populate queue with empty elements
			_queue = new Element[DefaultQueueSize];
			for (int i = 0; i < DefaultQueueSize; i++) _queue[i] = new Element();

			if (GraphicsSettings.renderPipelineAsset == null)
				Camera.onPostRender += OnRendered;
			else
				RenderPipelineManager.EndCameraRendering += OnRendered;
		}

		private void OnDisable()
		{
			if (GraphicsSettings.renderPipelineAsset == null)
				Camera.onPostRender -= OnRendered;
			else
				RenderPipelineManager.EndCameraRendering -= OnRendered;
		}

		private void OnRendered(ScriptableRenderContext context, Camera camera)
		{
			OnRendered(camera);
		}

		private static bool ShouldRenderCamera(Camera camera)
		{
			if (!camera) return false;
			return camera.CompareTag("MainCamera") || Gizmos.CameraFilter.Invoke(camera);
		}

		private static bool IsVisibleByCamera(Element points, Camera camera)
		{
			//essentially check if at least 1 point is visible by the camera
			return camera && points.Points.Select(camera.WorldToViewportPoint).Any(vp => vp.x is >= 0 and <= 1 && vp.y is >= 0 and <= 1);
		}

		private void Update()
		{
			//always render something
			// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
			Gizmos.Line(default, default);
		}

		private void OnRendered(Camera camera)
		{
			Material.SetPass(Gizmos.Pass);

			//shouldn't be rendering
			if (!Gizmos.Enabled) _queueIndex = 0;

			//check if this camera is ok to render with
			if (!ShouldRenderCamera(camera))
			{
				GL.PushMatrix();
				GL.Begin(GL.LINES);

				//bla bla bla

				GL.End();
				GL.PopMatrix();
				return;
			}

			Vector3 offset = Gizmos.Offset;

			GL.PushMatrix();
			GL.MultMatrix(Matrix4x4.identity);
			GL.Begin(GL.LINES);

			bool alt = Time.time % 1 > 0.5f;
			float dashGap = Mathf.Clamp(Gizmos.DashGap, 0.01f, 32f);
			bool frustumCull = Gizmos.FrustumCulling;
			var points = new List<Vector3>();

			//draw le elements
			for (int e = 0; e < _queueIndex; e++)
			{
				//just in case
				if (_queue.Length <= e) break;

				Element element = _queue[e];

				//don't render this thingy if its not inside the frustum
				if (frustumCull)
					if (!IsVisibleByCamera(element, camera))
						continue;

				points.Clear();
				if (element.Dashed)
					//subdivide
					for (int i = 0; i < element.Points.Length - 1; i++)
					{
						Vector3 pointA = element.Points[i];
						Vector3 pointB = element.Points[i + 1];
						Vector3 direction = pointB - pointA;
						if (direction.sqrMagnitude > dashGap * dashGap * 2f)
						{
							float magnitude = direction.magnitude;
							int amount = Mathf.RoundToInt(magnitude / dashGap);

							for (int p = 0; p < amount - 1; p++)
								if (p % 2 == (alt ? 1 : 0))
								{
									float startLerp = p / (amount - 1f);
									float endLerp = (p + 1) / (amount - 1f);
									Vector3 start = Vector3.Lerp(pointA, pointB, startLerp);
									Vector3 end = Vector3.Lerp(pointA, pointB, endLerp);
									points.Add(start);
									points.Add(end);
								}
						}
						else
						{
							points.Add(pointA);
							points.Add(pointB);
						}
					}
				else
					points.AddRange(element.Points);

				GL.Color(element.Color);
				foreach (Vector3 t in points)
					GL.Vertex(t + offset);
			}

			GL.End();
			GL.PopMatrix();
		}
	}
}
