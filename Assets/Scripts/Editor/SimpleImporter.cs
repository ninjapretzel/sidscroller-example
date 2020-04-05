using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SuperTiled2Unity;
using SuperTiled2Unity.Editor;
using SuperTiled2Unity.Dictionaries;
using SuperTiled2Unity.ThirdParty;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine.Tilemaps;

public class SimpleImporter : CustomTmxImporter {

	public override void TmxAssetImported(TmxAssetImportedArgs args) {
		// This is the imported prefab
		SuperMap map = args.ImportedSuperMap;
		// var importer = args.AssetImporter;

		// Find all objects
		var objects = map.GetComponentsInChildren<SuperObject>();
		foreach (var obj in objects) {

			CheckTrigger(obj);
			CheckType(obj);

		}

		var layers = map.GetComponentsInChildren<SuperTileLayer>();
		foreach (var layer in layers) {
			CheckTrigger(layer);
		}

	}

	private static void CheckTrigger(SuperTileLayer layer) {
		var props = layer.GetComponentInParent<SuperCustomProperties>();


		CustomProperty prop;
		if (props.m_Properties.TryGetProperty("isTrigger", out prop)) {
			var colliders = layer.GetComponentsInChildren<Collider2D>();
			foreach (var collider in colliders) { 
				collider.isTrigger = prop.GetValueAsBool(); 
			}
		}
		if (props.m_Properties.TryGetProperty("unity:isTrigger", out prop)) {
			var colliders = layer.GetComponentsInChildren<Collider2D>();
			foreach (var collider in colliders) { 
				collider.isTrigger = prop.GetValueAsBool(); 
			}
		}
		if (props.m_Properties.TryGetProperty("order", out prop)) {
			var renderer = layer.GetComponent<TilemapRenderer>();
			renderer.sortingOrder = prop.GetValueAsInt();
		}
		if (props.m_Properties.TryGetProperty("unity:order", out prop)) {
			var renderer = layer.GetComponent<TilemapRenderer>();
			renderer.sortingOrder = prop.GetValueAsInt();
		}
	}

	void CheckTrigger(SuperObject obj) {
		var props = obj.GetComponent<SuperCustomProperties>();
		CustomProperty prop;

		if (props.TryGetCustomProperty("isTrigger", out prop)) {
			if (prop.m_Value.Equals("true")) {
				obj.GetComponent<Collider2D>().isTrigger = true;
			}
		}
		if (props.TryGetCustomProperty("unity:isTrigger", out prop)) {
			if (prop.m_Value.Equals("true")) {
				obj.GetComponent<Collider2D>().isTrigger = true;
			}
		}
	}

	void CheckType(SuperObject obj) {
		string typeName = obj.m_Type;
		
		Type type = Helpers.GetTypeInUnityAssemblies(typeName);
		if (type != null && typeof(Component).IsAssignableFrom(type)) {
			Component component = obj.gameObject.AddComponent(type);

			ApplyProperties(obj.GetComponent<SuperCustomProperties>(), component);

		}

	}

	const BindingFlags ANY_INSTANCE = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
	void ApplyProperties(SuperCustomProperties props, Component comp) {
		var type = comp.GetType();

		foreach (var prop in props.m_Properties) {
			var field = type.GetField(prop.m_Name, ANY_INSTANCE);
			if (field != null) {

				try {
					if (field.FieldType == typeof(bool)) { field.SetValue(comp, prop.m_Value == "true"); }
					else if (field.FieldType == typeof(string)) { field.SetValue(comp, prop.m_Value); } 
					else if (field.FieldType == typeof(float)) { 
						float val = 0; 
						float.TryParse(prop.m_Value, out val);
						field.SetValue(comp, val);
					} else if (field.FieldType == typeof(int)) {
						int val = 0;
						int.TryParse(prop.m_Value, out val);
						field.SetValue(comp, val);
					}
					if (field.FieldType == typeof(Color)) { 
						Color val = Color.white;
						Helpers.ParseColor(prop.m_Value, out val);
						field.SetValue(comp, val);
					}
				} catch (Exception e) { Debug.LogWarning($"Something went wrong when importing {props},\n{e}"); }
			}
			var property = type.GetProperty(prop.m_Name, ANY_INSTANCE);
			if (property != null) {

			}
		}
	}

	
	
	public static class Helpers {
		public static Type GetTypeInUnityAssemblies(string targetTypeName) {
			if (targetTypeName == null) { throw new ArgumentNullException(); }
			foreach (string assembly in assemblies) {
				Type targetClass = assembly.Length > 0 ? Type.GetType(targetTypeName + assembly) : Type.GetType(targetTypeName);
				if (targetClass != null) {
					return targetClass;
				}
			}
			return null;
		}
		public static IEnumerable<string> assemblies {
			get {
				yield return "";
				yield return ",UnityEngine";
	#if UNITY_EDITOR
				yield return ",UnityEditor";
	#endif
				yield return ",Assembly-UnityScript";
				yield return ",Assembly-CSharp";
	#if UNITY_EDITOR
				yield return ",Assembly-UnityScript-Editor";
				yield return ",Assembly-CSharp-Editor";
	#endif
				yield return ",Assembly-UnityScript-firstpass";
				yield return ",Assembly-CSharp-firstpass";
	#if UNITY_EDITOR
				yield return ",Assembly-UnityScript-Editor-firstpass";
				yield return ",Assembly-CSharp-Editor-firstpass";
	#endif
			}
		}

		public static byte ParseByte(string s) { return byte.Parse(s, NumberStyles.HexNumber); }
		/// <summary>
		/// Parses a hex string into a Color object
		/// # is optional
		/// 'FF' is used for alpha if not present
		/// FF0000 = #FF0000 = #FF0000FF
		/// </summary>
		public static Color ToColorFromHex(string s) { return (Color)ParseHex32(s); }

		/// <summary>
		/// Parses a hex string into a Color object
		/// # is optional
		/// 'FF' is used for alpha if not present
		/// FF0000 = #FF0000 = #FF0000FF
		/// </summary>
		public static Color ParseHex(string s) { return (Color)ParseHex32(s); }

		/// <summary> 
		/// Parses a hex string into a Color32 object.
		/// # is optional
		/// 'FF' is used for alpha if not present.
		/// </summary>
		public static Color32 ParseHex32(string s) {
			Color32 c = new Color32(0, 0, 0, 0);
			try {
				int pos = s.StartsWith("#") ? 1 : 0;

				string r = s.Substring(pos + 0, 2);
				string g = s.Substring(pos + 2, 2);
				string b = s.Substring(pos + 4, 2);
				string a = (s.Length > (pos + 6)) ? s.Substring(pos + 6, 2) : "FF";

				c = new Color32(ParseByte(r), ParseByte(g), ParseByte(g), ParseByte(a));
			} catch { }

			return c;
		}

		/// <summary> Wraps the parse in a try...catch block and writes to <paramref name="col"/> Writes <see cref="Color.white"/> if parse fails. </summary>
		/// <param name="s"> String to parse </param>
		/// <param name="col"> Color to write to </param>
		/// <returns> true if parse successful, false if parse fails. </returns>
		public static bool ParseColor(string s, out Color col) {
			try {
				col = ParseHex(s);
				return true;
			} catch (System.Exception) {
				col = Color.white;
				return false;
			}
		}

	}

}
