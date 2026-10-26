using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace RandomForeseer.RandomForeseerCode.Data.Migrations;

internal static class JsonObjectExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_00247555D8ACA8F6CF104E36EEBCCB93B19A
	{
		[SpecialName]
		public static class _003CM_003E_0024CC1A43B1BE66C8794A1E83C731224E7E
		{
		}

		[ExtensionMarker("<M>$CC1A43B1BE66C8794A1E83C731224E7E")]
		public bool? GetBoolean(string propertyName)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$CC1A43B1BE66C8794A1E83C731224E7E")]
		public void MoveProperty(string legacyName, string currentName)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$CC1A43B1BE66C8794A1E83C731224E7E")]
		public void SetIfMissing(string propertyName, JsonNode? value)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$CC1A43B1BE66C8794A1E83C731224E7E")]
		public void SetIfMissing(string propertyName, bool value)
		{
			throw new NotSupportedException();
		}
	}

	public static bool? GetBoolean(this JsonObject data, string propertyName)
	{
		if (!data.TryGetPropertyValue(propertyName, out JsonNode jsonNode))
		{
			return null;
		}
		return jsonNode?.GetValue<bool>();
	}

	public static void MoveProperty(this JsonObject data, string legacyName, string currentName)
	{
		if (data.TryGetPropertyValue(legacyName, out JsonNode jsonNode))
		{
			data.SetIfMissing(currentName, jsonNode);
			data.Remove(legacyName);
		}
	}

	public static void SetIfMissing(this JsonObject data, string propertyName, JsonNode? value)
	{
		if (!data.ContainsKey(propertyName))
		{
			data[propertyName] = value?.DeepClone();
		}
	}

	public static void SetIfMissing(this JsonObject data, string propertyName, bool value)
	{
		if (!data.ContainsKey(propertyName))
		{
			data[propertyName] = value;
		}
	}
}
