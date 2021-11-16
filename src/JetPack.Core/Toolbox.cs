using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using UnityEngine;
using ParadoxNotion.Serialization;
using MessagePack;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public static partial class Toolbox
	{
		public static T MessagePackClone<T>(T _object)
		{
			byte[] _byte = MessagePackSerializer.Serialize(_object);
			return MessagePackSerializer.Deserialize<T>(_byte);
		}

		public static object JsonClone(this object _self)
		{
			if (_self == null)
				return null;
			string _json = JSONSerializer.Serialize(_self.GetType(), _self);
			return JSONSerializer.Deserialize(_self.GetType(), _json);
		}

		public static T JsonClone<T>(this object _self)
		{
			if (_self == null)
				return default(T);
			string _json = JSONSerializer.Serialize(typeof(T), _self);
			return (T) JSONSerializer.Deserialize(typeof(T), _json);
		}

		public static T MakeDeepCopy<T>(this object _self) where T : class
		{
			if (_self == null)
				return default(T);
			return AccessTools.MakeDeepCopy<T>(_self);
		}

		public static BaseUnityPlugin GetPluginInstance(string _guid)
		{
			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(_guid, out PluginInfo _pluginInfo);
			return _pluginInfo?.Instance;
		}

		public static Version GetPluginVersion(string _guid)
		{
			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(_guid, out PluginInfo _pluginInfo);
			return _pluginInfo?.Metadata?.Version;
		}

		public static bool PluginVersionCompare(string _guid, string _version)
		{
			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(_guid, out PluginInfo _pluginInfo);
			if (_pluginInfo == null) return false;
			return _pluginInfo.Metadata.Version.CompareTo(new Version(_version)) > -1;
		}

		public static bool PluginVersionCompare(BaseUnityPlugin _instance, string _version)
		{
			return _instance.Info.Metadata.Version.CompareTo(new Version(_version)) > -1;
		}

		public static T[] Add<T>(this T[] _self, T _item)
		{
			List<T> _list = _self.ToList();
			_list.Add(_item);
			return _list.ToArray();
		}

		public static Texture2D LoadTexture(byte[] _byte)
		{
			Texture2D _texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			_texture.LoadImage(_byte);
			return _texture;
		}

		public static byte[] ReadAllBytes(this Stream _self)
		{
			byte[] _byte = new byte[16 * 1024];
			using (var ms = new MemoryStream())
			{
				int _pointer;
				while ((_pointer = _self.Read(_byte, 0, _byte.Length)) > 0)
					ms.Write(_byte, 0, _pointer);
				return ms.ToArray();
			}
		}

		public static string SceneAddSceneName()
		{
#if KK
			return Manager.Scene.Instance.AddSceneName;
#else
			return Manager.Scene.AddSceneName;
#endif
		}

		public static bool SceneIsOverlap()
		{
#if KK
			return Manager.Scene.Instance.IsOverlap;
#else
			return Manager.Scene.IsOverlap;
#endif
		}

		// https://stackoverflow.com/questions/8477664/how-can-i-generate-uuid-in-c-sharp
		// https://stackoverflow.com/questions/1700361/how-to-convert-a-guid-to-a-string-in-c
		public static string GUID(string _format = "D")
		{
			return Guid.NewGuid().ToString(_format).ToUpper();
		}

		public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();

		public static string DisplayObjectInfo(object _object)
		{
			StringBuilder _stringBuilder = new StringBuilder();

			// Include the type of the object
			Type _type = _object.GetType();
			_stringBuilder.Append("Type: " + _type.Name);

			// Include information for each Field
			_stringBuilder.Append("\r\n\r\nFields:");
			FieldInfo[] _fieldInfos = _type.GetFields();
			if (_fieldInfos.Length > 0)
			{
				foreach (FieldInfo _fieldInfo in _fieldInfos)
					_stringBuilder.Append("\r\n " + _fieldInfo.ToString() + " = " + _fieldInfo.GetValue(_object));
			}
			else
				_stringBuilder.Append("\r\n None");

			// Include information for each Property
			_stringBuilder.Append("\r\n\r\nProperties:");
			PropertyInfo[] _propertyInfos = _type.GetProperties();
			if (_propertyInfos.Length > 0)
			{
				foreach (PropertyInfo _propertyInfo in _propertyInfos)
					_stringBuilder.Append("\r\n " + _propertyInfo.ToString() + " = " + _propertyInfo.GetValue(_object, null));
			}
			else
				_stringBuilder.Append("\r\n None");

			return _stringBuilder.ToString();
		}

		public static string GetPath(this GameObject _self, GameObject _top) => GetGameObjectPath(_self, _top);
		public static string GetGameObjectPath(GameObject _gameObject, GameObject _top)
		{
			if (_gameObject == null)
				return "";

			string _fullPath = _gameObject.name;
			Transform _current = _gameObject.transform;
			while (_current.parent != null && _current != _top?.transform)
			{
				_fullPath = _current.parent.name + "/" + _fullPath;
				_current = _current.parent;
			};
			return _fullPath;
		}

		public static string NameFormatted(this Material _material) => _material == null ? "" : _material.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
		public static string NameFormatted(this string _name) => _name.Replace("(Instance)", "").Replace(" Instance", "").Trim();

		public static object MessagePackGenericSerializer(Type _type, object _object) => MessagePackGenericSerializer(_type, (byte[]) _object);
		public static object MessagePackGenericSerializer(Type _type, byte[] _bytes)
		{
			//MethodInfo _method = typeof(MessagePackSerializer).GetMethod("Deserialize", AccessTools.all, null, new Type[] { typeof(byte[]) }, null);
			//MethodInfo _generic = _method.MakeGenericMethod(_type);

			MethodInfo _generic = Cache.MethodInfos["MessagePackSerializer.Deserialize"].CheckOrMakeGenericMethod(_type);

			return _generic.Invoke(null, new object[] { _bytes });
		}
	}

	public static class Cache
	{
		public static Dictionary<string, MethodInfo> MethodInfos = new Dictionary<string, MethodInfo>();

		static Cache()
		{
			MethodInfos["MessagePackSerializer.Deserialize"] = typeof(MessagePackSerializer).GetMethod("Deserialize", AccessTools.all, null, new Type[] { typeof(byte[]) }, null);
		}

		public static MethodInfo CheckOrMakeGenericMethod(this MethodInfo _self, Type _type)
		{
			string _key = $"Generic.{_self.DeclaringType.FullName}.{_type.FullName}";
			if (!MethodInfos.ContainsKey(_key))
			{
				MethodInfos[_key] = _self.MakeGenericMethod(_type);
				Core.DebugLog($"[CheckOrMakeGenericMethod][{_key}] cached");
			}
			return MethodInfos[_key];
		}
	}
}
