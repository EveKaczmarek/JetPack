using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using BepInEx;
using HarmonyLib;

using static MoreAccessoriesKOI.MoreAccessories;

namespace JetPack
{
	public partial class MoreAccessories
	{
		public static bool Installed = false;
		public static BaseUnityPlugin Instance = null;
		public static bool NewVer = true;

		private static Type _type = null;
		private static object _accessoriesByChar = null;
		private static Harmony _hookInstance = null;

		public static bool BuggyBootleg = false;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.joan6694.illusionplugins.moreaccessories");
			if (Instance == null) return;

			Installed = true;
			_type = Instance.GetType();

			//if (_type.Assembly.GetType("MoreAccessoriesKOI.MoreAccessories+CharAdditionalData") == null)
			if (Toolbox.PluginVersionCompare("com.joan6694.illusionplugins.moreaccessories", "2.0"))
			{
				BuggyBootleg = true;
				Core._logger.LogError($"This vsersion of MoreAccessories is still under development, use at your own risk");
#if KK
				// amazing!! even the Backward Compatibility support is imcomplete
				if (!_type.GetFields(AccessTools.all).Any(x => x.Name == "BackwardCompatibility") || !Traverse.Create(_type).Field("BackwardCompatibility").GetValue<bool>())
				{
					Installed = false;
					Instance = null;
					Core._logger.LogError("Backward compatibility in MoreAccessories is disabled");
					return;
				}
#endif
			}

			if (!BuggyBootleg)
				_accessoriesByChar = Traverse.Create(Instance).Field("_accessoriesByChar").GetValue();
		}

		internal static void OnMakerBaseLoaded()
		{
			if (!Installed) return;

			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
		}

		internal static void OnMakerFinishedLoading()
		{
			if (!Installed || BuggyBootleg) return;

			_hookInstance.Patch(GetCvsPatchType("CvsAccessory_UpdateCustomUI").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));
			//_hookInstance.Unpatch(typeof(CvsAccessory).GetMethod("UpdateCustomUI"), HarmonyPatchType.Prefix, "com.joan6694.kkplugins.moreaccessories");
			_hookInstance.Patch(GetCvsPatchType("CvsAccessory_ChangeUseColorVisible").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));
			_hookInstance.Patch(GetCvsPatchType("CvsAccessory_SetControllerTransform").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));

			_hookInstance.Patch(_type.GetMethod("UpdateMakerUI", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MoreAccessories_UpdateMakerUI_Postfix)));
			_hookInstance.Patch(_type.Assembly.GetType($"MoreAccessoriesKOI.CustomAcsChangeSlot_ChangeColorWindow_Patches").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));

			_hookInstance.Patch(_type.GetMethod("GetChaAccessoryComponent", AccessTools.all, null, new[] { typeof(ChaControl), typeof(int) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MoreAccessories_GetChaAccessoryComponent_Prefix)));
		}

		internal static void OnMakerExiting()
		{
			if (!Installed || BuggyBootleg) return;

			_hookInstance.UnpatchAll(_hookInstance.Id);
			_hookInstance = null;
		}

		public static Type GetCvsPatchType(string _methodName) => _type.Assembly.GetType($"MoreAccessoriesKOI.CvsAccessory_Patches+{_methodName}_Patches");
		internal static partial class Hooks
		{
			internal static bool ReturnFalse() => false;

			internal static void MoreAccessories_UpdateMakerUI_Postfix()
			{
				if (!Installed || BuggyBootleg) return;

				CharaMaker.UpdateAccssoryIndex();
			}

			internal static bool MoreAccessories_GetChaAccessoryComponent_Prefix(ChaControl character, int index, ref ChaAccessoryComponent __result)
			{
				if (index < 0)
					__result = null;
				else
				{
					GameObject _ca_slot = Accessory.GetObjAccessory(character, index);
					__result = _ca_slot == null ? null : _ca_slot.GetComponent<ChaAccessoryComponent>();
				}

				return false;
			}
		}

		public static void CheckAndPadPartInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			if (!Installed || BuggyBootleg) return;

			List<ChaFileAccessory.PartsInfo> _parts = ListMorePartsInfo(_chaCtrl, _coordinateIndex);
			if (_parts == null) return;

			for (int i = _parts.Count; i < _slotIndex + 1; i++)
			{
				if (_parts.ElementAtOrDefault(i) == null)
					_parts.Add(new ChaFileAccessory.PartsInfo());
			}
		}

		public static List<ChaFileAccessory.PartsInfo> ListMorePartsInfo(ChaControl _chaCtrl, int _coordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> _parts = new List<ChaFileAccessory.PartsInfo>();
			if (!Installed || BuggyBootleg) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			Dictionary<int, List<ChaFileAccessory.PartsInfo>> _rawAccessoriesInfos = _charAdditionalData.rawAccessoriesInfos;
			if (_rawAccessoriesInfos == null) return _parts;
			_rawAccessoriesInfos.TryGetValue(_coordinateIndex, out _parts);
			return _parts ?? new List<ChaFileAccessory.PartsInfo>();
		}

		public static List<bool> ListShowAccessories(ChaControl _chaCtrl)
		{
			List<bool> _parts = new List<bool>();
			if (!Installed || BuggyBootleg) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			return _charAdditionalData.showAccessories ?? _parts;
		}

		public static List<ChaFileAccessory.PartsInfo> ListNowAccessories(ChaControl _chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> _parts = new List<ChaFileAccessory.PartsInfo>();
			if (!Installed || BuggyBootleg) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			return _charAdditionalData.nowAccessories ?? _parts;
		}

		public static List<ChaAccessoryComponent> ListMoreChaAccessoryComponent(ChaControl _chaCtrl)
		{
			List<ChaAccessoryComponent> _parts = new List<ChaAccessoryComponent>();
			if (!Installed || BuggyBootleg) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			_parts = _charAdditionalData.cusAcsCmp;
			return _charAdditionalData.cusAcsCmp ?? _parts;
		}

		public static CharAdditionalData GetCharAdditionalData(ChaControl _chaCtrl)
		{
			if (!Installed || BuggyBootleg) return null;

			return _accessoriesByChar.RefTryGetValue<CharAdditionalData>(_chaCtrl.chaFile);
		}
	}
}
