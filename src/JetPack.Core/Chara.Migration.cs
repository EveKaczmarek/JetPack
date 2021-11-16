using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using MessagePack;

using BepInEx;
using HarmonyLib;

using ExtensibleSaveFormat;
using Sideloader.AutoResolver;

namespace JetPack
{
	public partial class Migration
	{
		public static BaseUnityPlugin Instance = null;
		public static string ExtDataGUID = "MigrationHelper";
		public static int ExtDataVer = 1;
		public static string ExtDataKey = "Info";
		private static Dictionary<string, Type> _type = new Dictionary<string, Type>();
		private static Dictionary<string, Traverse> _traverse = new Dictionary<string, Traverse>();
		private static bool _patchSuccess = true;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.bepis.bepinex.sideloader");
			if (Instance == null) return;

			_type["ManifestVersionComparer"] = Instance.GetType().Assembly.GetType("Sideloader.ManifestVersionComparer");
			_traverse["ManifestVersionComparer"] = Traverse.Create(Activator.CreateInstance(_type["ManifestVersionComparer"]));
			_type["UniversalAutoResolver+Hooks"] = typeof(UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
			Core._hookInstance.Patch(_type["UniversalAutoResolver+Hooks"].GetMethod("ExtendedCardLoad", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_Hooks_ExtendedCardLoad_Postfix)));
			Core._hookInstance.Patch(_type["UniversalAutoResolver+Hooks"].GetMethod("ExtendedCardSave", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_Hooks_ExtendedCardSave_Postfix)));
			Core._hookInstance.Patch(_type["UniversalAutoResolver+Hooks"].GetMethod("ExtendedCoordinateLoad", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_Hooks_ExtendedCoordinateLoad_Postfix)));
			Core._hookInstance.Patch(_type["UniversalAutoResolver+Hooks"].GetMethod("ExtendedCoordinateSave", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_Hooks_ExtendedCoordinateSave_Postfix)));

			MethodBase _iterateCardPrefixes = typeof(UniversalAutoResolver).GetMethods(AccessTools.all).Single(x => x.Name == "IterateCardPrefixes");
			Core._hookInstance.Patch(_iterateCardPrefixes, prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_IterateCardPrefixes_Prefix)));

			MethodBase _iterateCoordinatePrefixes = typeof(UniversalAutoResolver).GetMethods(AccessTools.all).Single(x => x.Name == "IterateCoordinatePrefixes");
			Core._hookInstance.Patch(_iterateCoordinatePrefixes, prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_IterateCoordinatePrefixes_Prefix)));

			MethodBase _resolveStructure = typeof(UniversalAutoResolver).GetMethods(AccessTools.all).Single(x => x.Name == "ResolveStructure");
			Core._hookInstance.Patch(_resolveStructure, transpiler: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_ResolveStructure_Transpiler)));
		}

		internal static int ManifestVersionComparer(string x, string y)
		{
			return _traverse["ManifestVersionComparer"].Method("Compare", new object[] { x, y }).GetValue<int>();
		}

		internal partial class Hooks
		{
			private static Dictionary<ICollection<ResolveInfo>, object> _pool = new Dictionary<ICollection<ResolveInfo>, object>();
			private static Dictionary<ICollection<ResolveInfo>, Dictionary<string, bool>> _poolResult = new Dictionary<ICollection<ResolveInfo>, Dictionary<string, bool>>();

			internal static void UniversalAutoResolver_IterateCardPrefixes_Prefix(ChaFile file, ICollection<ResolveInfo> extInfo)
			{
				if (!_patchSuccess || !Core._cfgMigrationOnLoad.Value) return;
				if (file == null || extInfo == null || extInfo?.Count == 0) return;
				Core.DebugLog($"[UniversalAutoResolver_IterateCardPrefixes_Prefix]");
				_pool[extInfo] = file;
				_poolResult[extInfo] = new Dictionary<string, bool>();
			}

			internal static void UniversalAutoResolver_IterateCoordinatePrefixes_Prefix(ChaFileCoordinate coordinate, ICollection<ResolveInfo> extInfo)
			{
				if (!_patchSuccess || !Core._cfgMigrationOnLoad.Value) return;
				if (coordinate == null || extInfo == null || extInfo?.Count == 0) return;
				if (_pool.ContainsKey(extInfo)) return; // called by IterateCardPrefixes

				if (!Core._cfgMigrationCordBrowse.Value)
				{
#if KK
					if (!Manager.Character.Instance.dictEntryChara.Any(x => x.Value.nowCoordinate == coordinate))
#else
					if (!Manager.Character.dictEntryChara.Any(x => x.Value.nowCoordinate == coordinate))
#endif
						return;
				}

				Core.DebugLog($"[UniversalAutoResolver_IterateCoordinatePrefixes_Prefix]");
				_pool[extInfo] = coordinate;
				_poolResult[extInfo] = new Dictionary<string, bool>();
			}

			internal static IEnumerable<CodeInstruction> UniversalAutoResolver_ResolveStructure_Transpiler(IEnumerable<CodeInstruction> _instructions)
			{
				MethodInfo _get_GUID = AccessTools.Method(typeof(ResolveInfo), "get_GUID");
				MethodInfo _ShowGUIDError = AccessTools.Method(typeof(UniversalAutoResolver), "ShowGUIDError");

				CodeMatcher _codeMatcher = new CodeMatcher(_instructions)
					.MatchForward(useEnd: false,
						new CodeMatch(OpCodes.Call, _ShowGUIDError))
					.Advance(1)
					.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, _get_GUID))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_ResolveStructure_Method))))
					.MatchForward(useEnd: false,
						new CodeMatch(OpCodes.Call, _ShowGUIDError))
					.Advance(1)
					.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, _get_GUID))
					.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_ResolveStructure_Method))));

				_codeMatcher.ReportFailure(MethodBase.GetCurrentMethod(), _error =>
				{
					_patchSuccess = false;
					Core._logger.LogError(_error);
				});
				//System.IO.File.WriteAllLines("UniversalAutoResolver_ResolveStructure_Method.txt", _codeMatcher.Instructions().Select(x => x.ToString()).ToArray());
				return _codeMatcher.Instructions();
			}

			internal static void UniversalAutoResolver_ResolveStructure_Method(ICollection<ResolveInfo> _info, string _guid)
			{
				if (!_patchSuccess || !Core._cfgMigrationOnLoad.Value) return;
				if (_info == null || _info?.Count == 0) return;
				if (!_poolResult.ContainsKey(_info))
				{
					//Core._logger.LogError($"[UniversalAutoResolver_ResolveStructure_Method] Key not found for ResolveInfo");
					return;
				}

				if (!_poolResult[_info].ContainsKey(_guid))
					_poolResult[_info][_guid] = UniversalAutoResolver.LoadedResolutionInfo.Any(x => x.GUID == _guid);
			}

			internal static void UniversalAutoResolver_Hooks_ExtendedCardLoad_Postfix(ChaFile file) => PluginDataLoad(file);
			internal static void UniversalAutoResolver_Hooks_ExtendedCardSave_Postfix(ChaFile file) => PluginDataSave(file);
			internal static void UniversalAutoResolver_Hooks_ExtendedCoordinateLoad_Postfix(ChaFileCoordinate file) => PluginDataLoad(file);
			internal static void UniversalAutoResolver_Hooks_ExtendedCoordinateSave_Postfix(ChaFileCoordinate file) => PluginDataSave(file);

			private static void PluginDataLoad(object _file)
			{
				if (!_patchSuccess || !Core._cfgMigrationOnLoad.Value) return;
				if (_file == null) return;

				PluginData _pluginData = null;
				if (_file is ChaFile)
					_pluginData = ExtendedSave.GetExtendedDataById(_file as ChaFile, ExtDataGUID);
				if (_file is ChaFileCoordinate)
					_pluginData = ExtendedSave.GetExtendedDataById(_file as ChaFileCoordinate, ExtDataGUID);

				Dictionary<string, string> _data = null;
				if (_pluginData != null && _pluginData.data.ContainsKey(ExtDataKey))
					_data = MessagePackSerializer.Deserialize<Dictionary<string, string>>((byte[]) _pluginData.data[ExtDataKey]);
				if (_data == null)
					_data = new Dictionary<string, string>();

				ICollection<ResolveInfo> _info = _pool.Where(x => x.Value == _file).Select(x => x.Key).FirstOrDefault();
				if (_info == null) return;

				if (_poolResult.ContainsKey(_info) && _poolResult[_info].Count > 0)
				{
					foreach (KeyValuePair<string, bool> x in _poolResult[_info])
					{
						string _verSave = "unknown";
						if (_data.ContainsKey(x.Key))
							_verSave = _data[x.Key];
						string _verGame = "missing";
						if (Sideloader.Sideloader.Manifests.ContainsKey(x.Key))
							_verGame = Sideloader.Sideloader.Manifests[x.Key].Version.Trim();

						Core.DebugLog($"[PluginDataLoad][{x.Key}][{x.Value}][{_verSave}][{_verGame}]");
					}
				}
				_pool.Remove(_info);
				_poolResult.Remove(_info);
			}

			private static void PluginDataSave(object _file)
			{
				PluginData _pluginData = null;
				if (_file is ChaFile)
					_pluginData = ExtendedSave.GetExtendedDataById(_file as ChaFile, UniversalAutoResolver.UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(_file as ChaFile, UniversalAutoResolver.UARExtID);
				if (_file is ChaFileCoordinate)
					_pluginData = ExtendedSave.GetExtendedDataById(_file as ChaFileCoordinate, UniversalAutoResolver.UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(_file as ChaFileCoordinate, UniversalAutoResolver.UARExtID);

				if (_pluginData == null || !_pluginData.data.ContainsKey("info")) return;

				IList _tmpExtInfo = _pluginData.data["info"] as IList;
				Dictionary<string, string> _data = new Dictionary<string, string>();
				for (int i = 0; i < _tmpExtInfo.Count; i++)
				{
					ResolveInfo _info = MessagePackSerializer.Deserialize<ResolveInfo>((byte[]) _tmpExtInfo[i]);
					if (!Sideloader.Sideloader.Manifests.ContainsKey(_info.GUID)) continue;

					_data[_info.GUID] = Sideloader.Sideloader.Manifests[_info.GUID].Version.Trim();
				}

				_data = _data.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

				if (_file is ChaFile)
					ExtendedSave.SetExtendedDataById(_file as ChaFile, ExtDataGUID, new PluginData { version = ExtDataVer, data = new Dictionary<string, object> { [ExtDataKey] = MessagePackSerializer.Serialize(_data) } });
				if (_file is ChaFileCoordinate)
					ExtendedSave.SetExtendedDataById(_file as ChaFileCoordinate, ExtDataGUID, new PluginData { version = ExtDataVer, data = new Dictionary<string, object> { [ExtDataKey] = MessagePackSerializer.Serialize(_data) } });
			}
		}
	}
}
