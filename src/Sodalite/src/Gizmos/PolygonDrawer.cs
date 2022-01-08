﻿using UnityEngine;

#pragma warning disable 1591
namespace Popcron;

public class PolygonDrawer : Drawer
{
	public override int Draw(ref Vector3[] buffer, params object[] values)
	{
		var position = (Vector3) values[0];
		var points = (int) values[1];
		var radius = (float) values[2];
		var offset = (float) values[3];
		var rotation = (Quaternion) values[4];

		var step = 360f / points;
		offset *= Mathf.Deg2Rad;

		for (var i = 0; i < points; i++)
		{
			var cx = Mathf.Cos(Mathf.Deg2Rad * step * i + offset) * radius;
			var cy = Mathf.Sin(Mathf.Deg2Rad * step * i + offset) * radius;
			Vector3 current = new(cx, cy);

			var nx = Mathf.Cos(Mathf.Deg2Rad * step * (i + 1) + offset) * radius;
			var ny = Mathf.Sin(Mathf.Deg2Rad * step * (i + 1) + offset) * radius;
			Vector3 next = new(nx, ny);

			buffer[i * 2] = position + rotation * current;
			buffer[i * 2 + 1] = position + rotation * next;
		}

		return points * 2;
	}
}
