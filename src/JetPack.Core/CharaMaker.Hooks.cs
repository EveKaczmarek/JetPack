using UnityEngine;
using ChaCustom;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaMaker
	{
		internal partial class Hooks
		{
#if MoreAcc
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), "UpdateCustomUI")]
			private static bool CvsAccessory_UpdateCustomUI_Prefix(CvsAccessory __instance)
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

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.ChangeColorWindow), new[] { typeof(int) })]
			private static bool CustomAcsChangeSlot_ChangeColorWindow_Prefix(CustomAcsChangeSlot __instance, int no)
			{
				if (__instance.cvsColor == null)
					return false;
				if (!__instance.cvsColor.isOpen)
					return false;

				CvsAccessory _cmp = GetCvsAccessory(no);
				if (_cmp != null)
					_cmp.SetDefaultColorWindow(no);

				return false;
			}
#endif
		}
	}
}
