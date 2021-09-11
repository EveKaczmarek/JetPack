using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using ChaCustom;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaMaker
	{
		public static Toggle CopyToggle(int _slotIndex)
		{
			if (_slotIndex < 0) return null;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return Traverse.Create(Instance.CvsAccessoryCopy).Field("tglKind").GetValue<Toggle[]>()[_slotIndex];

			IList _additionalCharaMakerSlots = Traverse.Create(MoreAccessories.Instance).Field("_additionalCharaMakerSlots").GetValue<IList>();
			if (_slotIndex - 20 >= _additionalCharaMakerSlots?.Count)
				return null;
			return Traverse.Create(_additionalCharaMakerSlots[_slotIndex - 20]).Field("copyToggle").GetValue<Toggle>();
#else
			return Instance.CvsAccessoryCopy.tglKind[_slotIndex];
#endif
		}

		public static GameObject GetObjAcsMove(int _slotIndex)
		{
			if (_slotIndex < 0) return null;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return CustomBase.Instance.chaCtrl.objAcsMove[_slotIndex, 1];

			List<GameObject[]> _objAcsMove = Traverse.Create(MoreAccessories.Instance).Field("_charaMakerData").Field("objAcsMove").GetValue<List<GameObject[]>>();
			if (_objAcsMove != null)
				return _objAcsMove.ElementAtOrDefault(_slotIndex - 20)?.ElementAtOrDefault(1);
			return null;
#else
			return CustomBase.Instance.chaCtrl.objAcsMove[_slotIndex, 1];
#endif
		}

		public static CvsAccessory GetCvsAccessory(int _slotIndex)
		{
			if (_slotIndex < 0) return null;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			return MoreAccessoriesKOI.MoreAccessories._self.GetCvsAccessory(_slotIndex);
#else
			if (_slotIndex >= AccListContainer.transform.childCount) return null;
			return AccListContainer.transform.GetChild(_slotIndex)?.GetComponentInChildren<CvsAccessory>(true);
#endif
		}
	}

	public partial class Accessory
	{
		public static bool GetAccessoryVisibility(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return false;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return false;
#endif
			List<bool> _parts = ListAccessoryVisibility(_chaCtrl);
			if (_slotIndex >= _parts.Count) return false;

			return _parts.ElementAtOrDefault(_slotIndex);
		}

		public static void SetAccessoryVisibility(ChaControl _chaCtrl, int _slotIndex, bool _show)
		{
			if (_slotIndex < 0) return;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return;
#endif
			List<bool> _parts = ListAccessoryVisibility(_chaCtrl);
			if (_slotIndex >= _parts.Count) return;

			_parts[_slotIndex] = _show;
		}

		public static List<bool> ListAccessoryVisibility(ChaControl _chaCtrl)
		{
			List<bool> _parts = _chaCtrl.fileStatus.showAccessory.ToList();
#if MoreAcc
			if (MoreAccessories.Installed)
				_parts.AddRange(MoreAccessories.ListShowAccessories(_chaCtrl));
#endif
			return _parts;
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _slotIndex) => GetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			if (_slotIndex < 0) return null;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
			{
				if (_chaCtrl.chaFile.coordinate.ElementAtOrDefault(_coordinateIndex) == null)
					return null;
				return _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ElementAtOrDefault(_slotIndex);
			}
			return MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex).ElementAtOrDefault(_slotIndex - 20);
#else
			return _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ElementAtOrDefault(_slotIndex);
#endif
		}

		public static void SetPartsInfo(ChaControl _chaCtrl, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo) => SetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex, _partInfo);
		public static void SetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo)
		{
			if (_slotIndex < 0) return;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return;

			if (_slotIndex < 20)
				_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts[_slotIndex] = _partInfo;
			else
			{
				MoreAccessories.CheckAndPadPartInfo(_chaCtrl, _coordinateIndex, _slotIndex - 20);
				MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex)[_slotIndex - 20] = _partInfo;
			}
#else
			_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts[_slotIndex] = _partInfo;
#endif
		}

		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(ChaControl _chaCtrl) => ListPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType);
		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(ChaControl _chaCtrl, int _coordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> _partInfo = _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ToList();
#if MoreAcc
			if (MoreAccessories.Installed)
				_partInfo.AddRange(MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex) ?? new List<ChaFileAccessory.PartsInfo>());
#endif
			return _partInfo;
		}

		public static List<ChaFileAccessory.PartsInfo> ListNowAccessories(ChaControl _chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> _partInfo = _chaCtrl.nowCoordinate.accessory.parts.ToList();
#if MoreAcc
			if (MoreAccessories.Installed)
				_partInfo.AddRange(MoreAccessories.ListNowAccessories(_chaCtrl) ?? new List<ChaFileAccessory.PartsInfo>());
#endif
			return _partInfo;
		}

		public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return null;
#if MoreAcc
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return _chaCtrl.cusAcsCmp.ElementAtOrDefault(_slotIndex);

			return MoreAccessoriesKOI.MoreAccessories._self.GetChaAccessoryComponent(_chaCtrl, _slotIndex);
#else
			return _chaCtrl.cusAcsCmp.ElementAtOrDefault(_slotIndex);
#endif
		}

		public static List<ChaAccessoryComponent> ListChaAccessoryComponent(ChaControl _chaCtrl)
		{
			List<ChaAccessoryComponent> _parts = _chaCtrl.cusAcsCmp.ToList();
#if MoreAcc
			if (!MoreAccessories.Installed) return _parts;
			_parts.AddRange(MoreAccessories.ListMoreChaAccessoryComponent(_chaCtrl));
#endif
			return _parts;
		}

		public static GameObject GetObjAccessory(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return null;
			return _chaCtrl.GetComponentsInChildren<ListInfoComponent>(true)?.FirstOrDefault(x => x != null && x.gameObject != null && x.gameObject.name == $"ca_slot{_slotIndex:00}")?.gameObject;
		}

		public static List<GameObject> ListObjAccessory(ChaControl _chaCtrl)
		{
			return _chaCtrl.GetComponentsInChildren<ListInfoComponent>(true)?.Where(x => x != null && x.gameObject != null && x.gameObject.name.StartsWith("ca_slot")).Select(x => x.gameObject).OrderBy(x => x.name).ToList() ?? new List<GameObject>();
		}

		public static List<GameObject> ListObjAccessory(GameObject _gameObject)
		{
			return _gameObject?.GetComponentsInChildren<ListInfoComponent>(true)?.Where(x => x != null && x.gameObject != null && x.gameObject.name.StartsWith("ca_slot")).Select(x => x.gameObject).ToList() ?? new List<GameObject>();
		}

		public static bool IsHairAccessory(ChaControl _chaCtrl, int _slotIndex)
		{
			GameObject _gameObject = GetObjAccessory(_chaCtrl, _slotIndex);
			if (_gameObject == null) return false;
			return _gameObject.GetComponent<ChaCustomHairComponent>() != null;
		}
	}
}
