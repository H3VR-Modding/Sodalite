using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Sodalite.Utilities;

/// <summary>
///     Some math helper functions because Unity 5.6 doesn't have a lot of advanced stuff
///     From: https://github.com/GregLukosek/3DMath/blob/master/Math3D.cs
/// </summary>
public static class Math3D
{
	/// <summary>
	///     This function returns a point which is a projection from a point to a line.
	///     The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
	/// </summary>
	public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
	{
		//get vector from point on line to point in space
		var linePointToPoint = point - linePoint;
		var t = Vector3.Dot(linePointToPoint, lineVec);
		return linePoint + lineVec * t;
	}


	/// <summary>
	///     This function returns a point which is a projection from a point to a line segment.
	///     If the projected point lies outside of the line segment, the projected point will
	///     be clamped to the appropriate line edge.
	///     If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
	/// </summary>
	public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
	{
		var vector = linePoint2 - linePoint1;

		var projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

		var side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

		return side switch
		{
			//The projected point is on the line segment
			0 => projectedPoint,
			1 => linePoint1,
			2 => linePoint2,

			//output is invalid
			_ => Vector3.zero
		};
	}

	/// <summary>
	///     This function finds out on which side of a line segment the point is located.
	///     The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
	///     the line segment, project it on the line using ProjectPointOnLine() first.
	///     Returns 0 if point is on the line segment.
	///     Returns 1 if point is outside of the line segment and located on the side of linePoint1.
	///     Returns 2 if point is outside of the line segment and located on the side of linePoint2.
	/// </summary>
	public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
	{
		var lineVec = linePoint2 - linePoint1;
		var pointVec = point - linePoint1;

		var dot = Vector3.Dot(pointVec, lineVec);

		//point is on side of linePoint2, compared to linePoint1
		if (dot > 0)
			//point is on the line segment
			return pointVec.magnitude <= lineVec.magnitude ? 0 : 2;

		//Point is not on side of linePoint2, compared to linePoint1.
		//Point is not on the line segment and it is on the side of linePoint1.
		return 1;
	}

	/// <summary>
	///     This function returns a point which is a projection from a point to a plane.
	/// </summary>
	public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
	{
		//First calculate the distance from the point to the plane:
		var distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

		//Reverse the sign of the distance
		distance *= -1;

		//Get a translation vector
		var translationVector = SetVectorLength(planeNormal, distance);

		//Translate the point to form a projection
		return point + translationVector;
	}

	/// <summary>
	///     Get the shortest distance between a point and a plane. The output is signed so it holds information
	///     as to which side of the plane normal the point is.
	/// </summary>
	public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
	{
		return Vector3.Dot(planeNormal, point - planePoint);
	}


	/// <summary>
	///     create a vector of direction "vector" with length "size"
	/// </summary>
	public static Vector3 SetVectorLength(Vector3 vector, float size)
	{
		return Vector3.Normalize(vector) * size;
	}

	/// <summary>
	///     Opposite of Vector3.Lerp
	/// </summary>
	public static float InverseLerpVector3(Vector3 a, Vector3 b, Vector3 value)
	{
		var ab = b - a;
		var av = value - a;
		return Vector3.Dot(av, ab) / Vector3.Dot(ab, ab);
	}
}
