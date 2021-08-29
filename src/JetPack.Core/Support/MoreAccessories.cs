using System;
using System.Collections.Generic;
using System.Linq;

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
		private static object _accessoriesByChar;
		private static Harmony _hookInstance = null;

		internal static void Init()
		{
#if KK
			Instance = Toolbox.GetPluginInstance("com.joan6694.illusionplugins.moreaccessories");
			if (Instance == null) return;
			Installed = true;
			_type = Instance.GetType();
			_accessoriesByChar = _self._accessoriesByChar;
#endif
		}

		internal static void OnMakerBaseLoaded()
		{
			if (!Installed) return;

			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
		}

		internal static void OnMakerFinishedLoading()
		{
			if (!Installed) return;

			_hookInstance.Patch(GetCvsPatchType("CvsAccessory_UpdateCustomUI").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));
			//_hookInstance.Unpatch(typeof(CvsAccessory).GetMethod("UpdateCustomUI"), HarmonyPatchType.Prefix, "com.joan6694.kkplugins.moreaccessories");

			_hookInstance.Patch(_type.GetMethod("UpdateMakerUI", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MoreAccessories_UpdateMakerUI_Postfix)));
		}

		internal static void OnMakerExiting()
		{
			if (!Installed) return;

			_hookInstance.UnpatchAll(_hookInstance.Id);
			_hookInstance = null;
		}

		public static Type GetCvsPatchType(string _methodName) => _type.Assembly.GetType($"MoreAccessoriesKOI.CvsAccessory_Patches+{_methodName}_Patches");
		internal static partial class Hooks
		{
			internal static bool ReturnFalse() => false;

			internal static void MoreAccessories_UpdateMakerUI_Postfix()
			{
				CharaMaker.UpdateAccssoryIndex();
			}
		}

		public static void CheckAndPadPartInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			if (!Installed) return;

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
			if (!Installed) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			Dictionary<int, List<ChaFileAccessory.PartsInfo>> _rawAccessoriesInfos = _charAdditionalData.rawAccessoriesInfos;
			if (_rawAccessoriesInfos == null) return _parts;
			_rawAccessoriesInfos.TryGetValue(_coordinateIndex, out _parts);
			return _parts ?? new List<ChaFileAccessory.PartsInfo>();
		}

		public static List<ChaFileAccessory.PartsInfo> ListNowAccessories(ChaControl _chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> _parts = new List<ChaFileAccessory.PartsInfo>();
			if (!Installed) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			return _charAdditionalData.nowAccessories ?? new List<ChaFileAccessory.PartsInfo>();
		}

		public static List<ChaAccessoryComponent> ListMoreChaAccessoryComponent(ChaControl _chaCtrl)
		{
			List<ChaAccessoryComponent> _parts = new List<ChaAccessoryComponent>();
			if (!Installed) return _parts;

			CharAdditionalData _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			_parts = _charAdditionalData.cusAcsCmp;
			return _parts ?? new List<ChaAccessoryComponent>();
		}

		public static CharAdditionalData GetCharAdditionalData(ChaControl _chaCtrl)
		{
			return _accessoriesByChar.RefTryGetValue<CharAdditionalData>(_chaCtrl.chaFile);
		}
	}
}
