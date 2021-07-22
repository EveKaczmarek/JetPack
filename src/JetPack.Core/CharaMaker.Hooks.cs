using UnityEngine;
using ChaCustom;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaMaker
	{
		internal partial class Hooks
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), "CalculateUI")]
			private static bool CvsAccessory_CalculateUI_Prefix(CvsAccessory __instance)
			{
				__instance.tglTakeOverParent.isOn = CustomBase.Instance.customSettingSave.acsTakeOverParent;
				__instance.tglTakeOverColor.isOn = CustomBase.Instance.customSettingSave.acsTakeOverColor;
				ChaFileAccessory.PartsInfo _part = Accessory.GetPartsInfo(CustomBase.instance.chaCtrl, __instance.nSlotNo);
				if (Game.HasDarkness)
					__instance.tglNoShake.isOn = _part.noShake;
				__instance.imgAcsColor01.color = _part.color[0];
				__instance.imgAcsColor02.color = _part.color[1];
				__instance.imgAcsColor03.color = _part.color[2];
				__instance.imgAcsColor04.color = _part.color[3];
				for (int i = 0; i < __instance.tglAcsGroup.Length; i++)
					__instance.tglAcsGroup[i].isOn = i == _part.hideCategory;
				for (int j = 0; j < 2; j++)
					__instance.UpdateDrawControllerState(j);
				return false;
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), "UpdateCustomUI")]
			internal static bool CvsAccessory_UpdateCustomUI_Prefix(CvsAccessory __instance)
			{
				if (!Loaded) return true;

				int _slotIndex = __instance.nSlotNo;
				if (_slotIndex < 0)
					return false;

				ChaFileAccessory.PartsInfo _part = CustomBase.Instance.chaCtrl.GetPartsInfo(_slotIndex);

				__instance.CalculateUI();
				//__instance.Field<CvsDrawCtrl>("cmpDrawCtrl").UpdateAccessoryDraw();
				int _value = 0;
				if (_part != null)
					_value = _part.type - 120;
				__instance.ddAcsType.value = _value;

				__instance.UpdateAccessoryKindInfo();
				__instance.UpdateAccessoryParentInfo();
				__instance.UpdateAccessoryMoveInfo();
				__instance.ChangeSettingVisible(_value != 0);

				__instance.separateColor.SetActiveIfDifferent(false);
				__instance.separateCorrect.SetActiveIfDifferent(false);
				Transform _parent = CvsScrollable ? __instance.transform.GetChild(0).GetChild(0).GetChild(0) : __instance.transform;
				_parent.Find("objController01/Controller/imgSeparete").gameObject.SetActiveIfDifferent(__instance.objControllerTop02.activeSelf);

				return false;
			}
		}
	}
}
