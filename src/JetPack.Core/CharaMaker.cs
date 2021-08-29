using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChaCustom;

using HarmonyLib;
using Sideloader.AutoResolver;

namespace JetPack
{
	public partial class CharaMaker
	{
		public static bool Inside { get; internal set; }
		public static bool Loaded { get; internal set; }
		public static CustomBase CustomBase => CustomBase.Instance;
		public static ChaControl ChaControl => CustomBase?.chaCtrl;
		public static int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
		public static int CurrentAccssoryIndex = 0;

		private static Harmony _hookInstance;

		public static int CvsMainMenu = 0;
		public static Dictionary<int, Toggle> CvsMenuTree = new Dictionary<int, Toggle>();
		public static bool CvsScrollable = false;

		public static event EventHandler OnMakerStartLoading;
		public static event EventHandler OnMakerBaseLoaded;
		public static event EventHandler OnMakerFinishedLoading;
		public static event EventHandler OnMakerExiting;

		internal static void InvokeOnMakerStartLoading(object _sender, EventArgs _args) => OnMakerStartLoading?.Invoke(_sender, _args);
		internal static void InvokeOnMakerBaseLoaded(object _sender, EventArgs _args) => OnMakerBaseLoaded?.Invoke(_sender, _args);
		internal static void InvokeOnMakerFinishedLoading(object _sender, EventArgs _args) => OnMakerFinishedLoading?.Invoke(_sender, _args);
		internal static void InvokeOnSlotAdded(object _sender, SlotAddedEventArgs _args) => OnSlotAdded?.Invoke(_sender, _args);

		public static class Instance
		{
			public static CvsDrawCtrl CvsDrawCtrl => CustomBase.Instance.customCtrl.cmpDrawCtrl;
			public static CvsAccessoryCopy CvsAccessoryCopy => Singleton<CvsAccessoryCopy>.Instance;
		}

		internal static void Init()
		{
			OnMakerStartLoading += (_sender, _args) =>
			{
				Core.DebugLog($"[OnMakerStartLoading]");
				CurrentAccssoryIndex = 0;
				Inside = true;
				KKAPI.Hooks.OnMakerStartLoadingPatch();
			};

			OnMakerBaseLoaded += (_sender, _args) =>
			{
				Core.DebugLog($"[OnMakerBaseLoaded]");
				if (MoreAccessories.Installed)
					MoreAccessories.OnMakerBaseLoaded();
			};

			OnMakerFinishedLoading += (_sender, _args) =>
			{
				Core.DebugLog($"[OnMakerFinishedLoading]");
				Loaded = true;

				_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

				CvsScrollable = GameObject.Find("tglSlot01/Slot01Top/tglSlot01ScrollView") != null;

				int _onCustomSelectListClickCount = OnCustomSelectListClick?.GetInvocationList()?.Length ?? 0;
				if (_onCustomSelectListClickCount > 0)
				{
					Core.DebugLog($"[MakerFinishedLoading][(OnCustomSelectListClick: {_onCustomSelectListClickCount}]");
					_hookInstance.PatchAll(typeof(HooksCustomSelectListCtrl));
				}

				int _onPointerEnterCount = OnPointerEnter?.GetInvocationList()?.Length ?? 0;
				int _onPointerExitCount = OnPointerExit?.GetInvocationList()?.Length ?? 0;
				if (_onPointerEnterCount + _onPointerExitCount > 0)
				{
					Core.DebugLog($"[MakerFinishedLoading][(OnPointerEnter + OnPointerExit: {_onPointerEnterCount + _onPointerExitCount}]");
					_hookInstance.PatchAll(typeof(HooksSelectable));
				}

				CvsNavMenuInit(Singleton<CustomChangeMainMenu>.Instance);
				if (MoreAccessories.Installed)
					MoreAccessories.OnMakerFinishedLoading();
			};

			OnMakerExiting += (_sender, _args) =>
			{
				Core.DebugLog($"[OnMakerExiting]");

				CurrentAccssoryIndex = 0;
				Inside = false;
				Loaded = false;

				_hookInstance.UnpatchAll(_hookInstance.Id);
				_hookInstance = null;
			};

			OnSelectedMakerSlotChanged += (_sender, _args) =>
			{
				Core.DebugLog($"[OnSelectedMakerSlotChanged][{_args.OldSlotIndex}][{_args.NewSlotIndex}]");
			};

			OnSlotAdded += (_sender, _args) =>
			{
				Core.DebugLog($"[OnSlotAdded][{_args.SlotIndex}][{_args.SlotTemplate.name}]");
				//_args.SlotTemplate.GetComponent<CvsNavSideMenuEventHandler>().SlotIndex = _args.SlotIndex;
			};

			OnCvsNavMenuClick += (_sender, _args) =>
			{
				Core.DebugLog($"[OnCvsNavMenuClick][{_args.TopIndex}][{_args.SideToggle.name}][{_args.Changed}]");
			};

			OnClothesCopy += (_sender, _args) =>
			{
				Core.DebugLog($"[OnClothesCopy][{_args.SourceCoordinateIndex}][{_args.DestinationCoordinateIndex}][{_args.DestinationSlotIndex}]");
			};
		}

		internal static void UpdateAccssoryIndex()
		{
			CvsAccessory _cvsAccessory = GetCvsAccessory(CurrentAccssoryIndex);
			int _slotIndex = CurrentAccssoryIndex;
			if (_cvsAccessory == null || !_cvsAccessory.transform.parent.gameObject.activeSelf)
				_slotIndex = -1;
			else
				_slotIndex = _cvsAccessory.nSlotNo;
			if (_slotIndex != CurrentAccssoryIndex)
				OnSelectedMakerSlotChanged?.Invoke(null, new SelectedMakerSlotChangedEventArgs(CurrentAccssoryIndex, _slotIndex));
		}

		internal partial class Hooks
		{
			[HarmonyPrefix]
			[HarmonyPatch(typeof(CvsDrawCtrl), "UpdateAccessoryDraw")]
			private static bool CvsDrawCtrl_UpdateAccessoryDraw_Prefix()
			{
				return false;
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
			private static void CvsClothesCopy_CopyClothes_Postfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
			{
				for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
				{
					if (___tglKind[i].isOn)
						OnClothesCopy?.Invoke(null, new ClothesCopyEventArgs(___ddCoordeType[1].value, ___ddCoordeType[0].value, i));
				}
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(CustomAcsSelectKind), nameof(CustomAcsSelectKind.ChangeSlot), new[] { typeof(int), typeof(bool) })]
			private static void CustomAcsSelectKind_ChangeSlot_Postfix(CustomAcsSelectKind __instance, int _no)
			{
				if (CurrentAccssoryIndex != _no)
					OnSelectedMakerSlotChanged?.Invoke(__instance, new SelectedMakerSlotChangedEventArgs(CurrentAccssoryIndex, _no));
			}

			[HarmonyBefore(new string[] { "com.joan6694.kkplugins.moreaccessories" })]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryKind))]
			private static void CvsAccessory_UpdateSelectAccessoryKind_Prefix(CvsAccessory __instance, ref int __state)
			{
				// Used to see if the kind actually changed
				__state = Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo).id;
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryKind))]
			private static void CvsAccessory_UpdateSelectAccessoryKind_Postfix(CvsAccessory __instance, ref int __state)
			{
				// Only send the event if the kind actually changed
				if (__state != Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo).id)
					OnAccessoryKindChanged?.Invoke(__instance, new AccessoryKindChangedEventArgs(__instance.nSlotNo));
			}

			[HarmonyBefore(new string[] { "com.joan6694.kkplugins.moreaccessories" })]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryType_Prefix(CvsAccessory __instance, ref int __state)
			{
				__state = Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo).type;
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryType_Postfix(CvsAccessory __instance, ref int __state)
			{
				ChaFileAccessory.PartsInfo _part = Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo);
				OnAccessoryTypeChanged?.Invoke(__instance, new AccessoryTypeChangedEventArgs(__instance.nSlotNo, __state, _part.type, _part));
			}

			[HarmonyBefore(new string[] { "com.joan6694.kkplugins.moreaccessories" })]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryParent), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryParent_Prefix(CvsAccessory __instance, ref string __state)
			{
				__state = Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo).parentKey;
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryParent), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryParent_Postfix(CvsAccessory __instance, ref string __state)
			{
				ChaFileAccessory.PartsInfo _part = Accessory.GetPartsInfo(ChaControl, __instance.nSlotNo);
				OnAccessoryParentChanged?.Invoke(__instance, new AccessoryParentChangedEventArgs(__instance.nSlotNo, __state, _part.parentKey, _part));
			}

			[HarmonyPrefix]
			[HarmonyPatch(typeof(CustomScene), "OnDestroy")]
			private static void CustomScene_OnDestroy_Prefix()
			{
				OnMakerExiting?.Invoke(null, null);
			}
		}

		public static event EventHandler<ClothesCopyEventArgs> OnClothesCopy;

		public class ClothesCopyEventArgs : EventArgs
		{
			public ClothesCopyEventArgs(int _sourceCoordinateIndex, int _destinationSlotIndex, int _slotIndex)
			{
				DestinationSlotIndex = _slotIndex;
				SourceCoordinateIndex = _sourceCoordinateIndex;
				DestinationCoordinateIndex = _destinationSlotIndex;
			}

			public int DestinationSlotIndex { get; }
			public int SourceCoordinateIndex { get; }
			public int DestinationCoordinateIndex { get; }
		}

		public static event EventHandler<CvsNavMenuEventArgs> OnCvsNavMenuClick;

		public class CvsNavMenuEventArgs : EventArgs
		{
			public CvsNavMenuEventArgs(int _topIndex, Toggle _sideToggle, bool _changed)
			{
				TopIndex = _topIndex;
				SideToggle = _sideToggle;
				Changed = _changed;
			}

			public int TopIndex { get; }
			public Toggle SideToggle { get; }
			public bool Changed { get; }
		}

		internal class CvsNavTopMenuEventHandler : MonoBehaviour, IPointerClickHandler
		{
			public int TopIndex;
			public void OnPointerClick(PointerEventData _pointerEventData)
			{
				bool _changed = CvsMainMenu != TopIndex;
				CvsMainMenu = TopIndex;
				OnCvsNavMenuClick?.Invoke(null, new CvsNavMenuEventArgs(TopIndex, CvsMenuTree[TopIndex], _changed));
			}
			internal void Init(int _topIndex)
			{
				TopIndex = _topIndex;
			}
		}

		internal class CvsNavSideMenuEventHandler : MonoBehaviour, IPointerClickHandler
		{
			public int TopIndex;
			public int SlotIndex
			{
				get
				{
					if (TopIndex == 4)
					{
						CvsAccessory _cvsAccessory = gameObject.GetComponentInChildren<CvsAccessory>(true);
						if (_cvsAccessory != null)
							return (int) _cvsAccessory.slotNo;
						else
							return -1;
					}
					else
						return -1;
				}
			}

			public void OnPointerClick(PointerEventData _pointerEventData)
			{
				Toggle _toggle = gameObject.GetComponent<Toggle>();
				bool _changed = CvsMenuTree[TopIndex] != _toggle;
				CvsMenuTree[TopIndex] = _toggle;

				if (TopIndex == 4)
					CurrentAccssoryIndex = SlotIndex;
				OnCvsNavMenuClick?.Invoke(null, new CvsNavMenuEventArgs(TopIndex, _toggle, _changed));
			}
			internal void Init(int _topIndex)
			{
				TopIndex = _topIndex;
			}
		}

		internal static void CvsNavMenuInit(CustomChangeMainMenu _instance)
		{
			for (int i = 0; i < _instance.items.Length; i++)
			{
				int _topIndex = i;
				Toggle _toggle = _instance.items[i].tglItem;
				if (_toggle != null)
					_toggle.gameObject.AddComponent<CvsNavTopMenuEventHandler>().Init(_topIndex);
			}

			foreach (Transform _child in GameObject.Find("CvsMenuTree").transform)
			{
				int _topIndex = _child.GetSiblingIndex();

				UI_ToggleGroupCtrl.ItemInfo[] _items = _child.GetComponent<UI_ToggleGroupCtrl>().items;
				CvsMenuTree[_topIndex] = _items[0].tglItem;
				for (int i = 0; i < _items.Length; i++)
				{
					Toggle _toggle = _items[i].tglItem;
					if (_toggle != null)
						_toggle.gameObject.AddComponent<CvsNavSideMenuEventHandler>().Init(_topIndex);
				}
			}
		}

		public static event EventHandler<CustomSelectListCtrlEventArgs> OnCustomSelectListClick;

		public class CustomSelectListCtrlEventArgs : EventArgs
		{
			public CustomSelectListCtrlEventArgs(GameObject _gameObject)
			{
				CustomSelectInfoComponent _cmp = _gameObject.GetComponent<CustomSelectInfoComponent>();
				if (_cmp == null || !_cmp.tgl.interactable) return;

				if (_cmp.info.index >= UniversalAutoResolver.BaseSlotID)
				{
					ResolveInfo _info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo) _cmp.info.category, _cmp.info.index);
					if (_info != null)
					{
						CategoryNo = (int) _info.CategoryNo;
						GUID = _info.GUID;
						ItemID = _info.LocalSlot;
						LocalItemID = _info.Slot;
					}
				}
				else
				{
					CategoryNo = _cmp.info.category;
					ItemID = _cmp.info.index;
					LocalItemID = -1;
				}
			}

			public int CategoryNo { get; }
			public string GUID { get; }
			public int ItemID { get; }
			public int LocalItemID { get; }
		}

		private class HooksCustomSelectListCtrl
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.OnPointerClick))]
			internal static void CustomSelectListCtrl_OnPointerClick_Postfix(CustomSelectListCtrl __instance, GameObject obj)
			{
				if (__instance.onChangeItemFunc == null || obj == null) return;
				OnCustomSelectListClick?.Invoke(__instance, new CustomSelectListCtrlEventArgs(obj));
			}
		}

		public static event EventHandler<PointerEventArgs> OnPointerEnter;
		public static event EventHandler<PointerEventArgs> OnPointerExit;

		public class PointerEventArgs : EventArgs
		{
			public PointerEventArgs(Selectable _selectable, PointerEventData _eventData)
			{
				Selectable = _selectable;
				EventData = _eventData;
			}

			public Selectable Selectable { get; }
			public PointerEventData EventData { get; }
		}

		private class HooksSelectable
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerEnter))]
			private static void Selectable_OnPointerEnter_Postfix(Selectable __instance, PointerEventData eventData)
			{
				OnPointerEnter?.Invoke(__instance, new PointerEventArgs(__instance, eventData));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerExit))]
			private static void Selectable_OnPointerExit_Postfix(Selectable __instance, PointerEventData eventData)
			{
				OnPointerExit?.Invoke(__instance, new PointerEventArgs(__instance, eventData));
			}
		}
	}
}
