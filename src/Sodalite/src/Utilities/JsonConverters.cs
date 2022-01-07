using System;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace Sodalite.Utilities;

/// <summary>
///     Because Unity's Vector3 classes are not serializable by default, use
///     this converter when trying to serialize things that use Vector3.
/// </summary>
// ReSharper disable once UnusedType.Global
public class Vector3Converter : JsonConverter
{
	/// <summary>
	///     Writes the Vector3 as a comma separated string, e.g. 1.5,2.5,-1.5
	/// </summary>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is not Vector3 vector) throw new JsonSerializationException("Expected Vector3 object value");
		writer.WriteValue($"{vector.x},{vector.y},{vector.z}");
	}

	/// <summary>
	///     Reads the serialized Vector3 value
	/// </summary>
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType != JsonToken.String) throw new JsonSerializationException("Expected value to be a string");

		var array = ((string) reader.Value).Split(',').Select(float.Parse).ToArray();
		if (array.Length != 3) throw new JsonSerializationException("Expected array to be of length 3");
		return new Vector3(array[0], array[1], array[2]);
	}

	/// <summary>
	///     We only convert Vector3s
	/// </summary>
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Vector3);
	}
}
