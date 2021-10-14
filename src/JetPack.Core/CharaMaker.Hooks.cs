using System;
using System.Linq;

using UnityEngine;
using ChaCustom;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaMaker
	{
		internal partial class HooksMoreAcc
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.SetControllerTransform), new Type[] { typeof(int) })]
			private static bool CvsAccessory_SetControllerTransform_Prefix(CvsAccessory __instance, int guidNo)
			{
				if (!Loaded) return true;
				if (__instance == null) return false;

				int _slotIndex = __instance.nSlotNo;
				GameObject _gameObject;
				if (_slotIndex < 20)
					_gameObject = CustomBase.Instance.chaCtrl.objAcsMove[_slotIndex, guidNo];
				else
					_gameObject = MoreAccessoriesKOI.MoreAccessories._self._charaMakerData?.objAcsMove?.ElementAtOrDefault(_slotIndex - 20)?.ElementAtOrDefault(guidNo);
				if (_gameObject == null)
				{
					return false;
				}
				CustomBase.Instance.customCtrl.cmpGuid[guidNo].amount.position = _gameObject.transform.position;
				CustomBase.Instance.customCtrl.cmpGuid[guidNo].amount.rotation = _gameObject.transform.eulerAngles;

				return false;
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.ChangeUseColorVisible))]
			private static bool CvsAccessory_ChangeUseColorVisible_Prefix(CvsAccessory __instance)
			{
				if (__instance == null) return false;

				bool[] _array = new bool[4];
				bool _active = false;
				if (__instance.ddAcsType.value != 0)
				{
					GameObject _ca_slot = Accessory.GetObjAccessory(CustomBase.Instance.chaCtrl, __instance.nSlotNo);
					ChaAccessoryComponent _cmp = _ca_slot?.GetComponent<ChaAccessoryComponent>();
					/*
					if (__instance.nSlotNo < 20)
						_cmp = CustomBase.Instance.chaCtrl.cusAcsCmp[__instance.nSlotNo];
					else
						_cmp = MoreAccessoriesKOI.MoreAccessories._self._accessoriesByChar.RefTryGetValue<MoreAccessoriesKOI.MoreAccessories.CharAdditionalData>(CustomBase.Instance.chaCtrl.chaFile).cusAcsCmp.ElementAtOrDefault(__instance.nSlotNo - 20);
					*/
					if (_ca_slot != null && _cmp != null)
					{
						if (_cmp.useColor01)
						{
							_array[0] = true;
							_active = true;
						}
						if (_cmp.useColor02)
						{
							_array[1] = true;
							_active = true;
						}
						if (_cmp.useColor03)
						{
							_array[2] = true;
							_active = true;
						}
						if (_cmp.rendAlpha != null && 0 < _cmp.rendAlpha.Length)
						{
							_array[3] = true;
							_active = true;
						}
					}
				}
				__instance.separateColor.SetActiveIfDifferent(_active);
				__instance.btnAcsColor01.transform.parent.gameObject.SetActiveIfDifferent(_array[0]);
				__instance.btnAcsColor02.transform.parent.gameObject.SetActiveIfDifferent(_array[1]);
				__instance.btnAcsColor03.transform.parent.gameObject.SetActiveIfDifferent(_array[2]);
				__instance.btnAcsColor04.transform.parent.gameObject.SetActiveIfDifferent(_array[3]);

				return false;
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateCustomUI))]
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
			[HarmonyPrefix, HarmonyPatch(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.ChangeColorWindow), new Type[] { typeof(int) })]
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
		}
	}
}
