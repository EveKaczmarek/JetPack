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
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return Traverse.Create(Instance.CvsAccessoryCopy).Field("tglKind").GetValue<Toggle[]>()[_slotIndex];

			IList _additionalCharaMakerSlots = Traverse.Create(MoreAccessories.Instance).Field("_additionalCharaMakerSlots").GetValue<IList>();
			if (_slotIndex - 20 >= _additionalCharaMakerSlots?.Count)
				return null;
			return Traverse.Create(_additionalCharaMakerSlots[_slotIndex - 20]).Field("copyToggle").GetValue<Toggle>();
		}

		public static GameObject GetObjAcsMove(int _slotIndex)
		{
			if (_slotIndex < 0) return null;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return CustomBase.Instance.chaCtrl.objAcsMove[_slotIndex, 1];

			List<GameObject[]> _objAcsMove = Traverse.Create(MoreAccessories.Instance).Field("_charaMakerData").Field("objAcsMove").GetValue<List<GameObject[]>>();
			if (_objAcsMove != null)
				return _objAcsMove.ElementAtOrDefault(_slotIndex - 20)?.ElementAtOrDefault(1);
			return null;
		}

		public static CvsAccessory GetCvsAccessory(int _slotIndex)
		{
			if (_slotIndex < 0) return null;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			return Traverse.Create(MoreAccessories.Instance).Method("GetCvsAccessory", new object[] { _slotIndex }).GetValue<CvsAccessory>();
		}
	}

	public partial class Accessory
	{
		public static bool GetAccessoryVisibility(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return false;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return false;

			if (_slotIndex < 20)
				return _chaCtrl.fileStatus.showAccessory[_slotIndex];
			object _charAdditionalData = MoreAccessories.GetCharAdditionalData(_chaCtrl);
			List<bool> _showAccessories = Traverse.Create(_charAdditionalData).Field("showAccessories").GetValue<List<bool>>();
			return _showAccessories.RefElementAt<bool>(_slotIndex - 20);
		}

		public static List<bool> ListAccessoryVisibility(ChaControl _chaCtrl)
		{
			List<bool> _parts = _chaCtrl.fileStatus.showAccessory.ToList();
			object _charAdditionalData = MoreAccessories.GetCharAdditionalData(_chaCtrl);
			_parts.AddRange(Traverse.Create(_charAdditionalData).Field("showAccessories").GetValue<List<bool>>() ?? new List<bool>());
			return _parts;
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _slotIndex) => GetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			if (_slotIndex < 0) return null;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
			{
				if (_chaCtrl.chaFile.coordinate.ElementAtOrDefault(_coordinateIndex) == null)
					return null;
				return _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ElementAtOrDefault(_slotIndex);
			}
			return MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex).ElementAtOrDefault(_slotIndex - 20);
		}

		public static void SetPartsInfo(ChaControl _chaCtrl, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo) => SetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex, _partInfo);
		public static void SetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo)
		{
			if (_slotIndex < 0) return;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return;

			if (_slotIndex < 20)
				_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts[_slotIndex] = _partInfo;
			else
			{
				MoreAccessories.CheckAndPadPartInfo(_chaCtrl, _coordinateIndex, _slotIndex - 20);
				MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex)[_slotIndex - 20] = _partInfo;
			}
		}

		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(ChaControl _chaCtrl) => ListPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType);
		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(ChaControl _chaCtrl, int _coordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> _partInfo = _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ToList();
			if (MoreAccessories.Installed)
				_partInfo.AddRange(MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex) ?? new List<ChaFileAccessory.PartsInfo>());
			return _partInfo;
		}

		public static List<ChaFileAccessory.PartsInfo> ListNowAccessories(ChaControl _chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> _partInfo = _chaCtrl.nowCoordinate.accessory.parts.ToList();
			if (MoreAccessories.Installed)
				_partInfo.AddRange(MoreAccessories.ListNowAccessories(_chaCtrl) ?? new List<ChaFileAccessory.PartsInfo>());
			return _partInfo;
		}

		public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return null;
			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			if (_slotIndex < 20)
				return _chaCtrl.cusAcsCmp.ElementAtOrDefault(_slotIndex);

			return Traverse.Create(MoreAccessories.Instance).Method("GetChaAccessoryComponent", new object[] { _chaCtrl, _slotIndex }).GetValue<ChaAccessoryComponent>();
		}

		public static List<ChaAccessoryComponent> ListChaAccessoryComponent(ChaControl _chaCtrl)
		{
			List<ChaAccessoryComponent> _parts = _chaCtrl.cusAcsCmp.ToList();
			if (!MoreAccessories.Installed) return _parts;
			_parts.AddRange(MoreAccessories.ListMoreChaAccessoryComponent(_chaCtrl));
			return _parts;
		}

		public static GameObject GetObjAccessory(ChaControl _chaCtrl, int _slotIndex)
		{
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
			ChaAccessoryComponent _cmp = GetChaAccessoryComponent(_chaCtrl, _slotIndex);
			if (_cmp == null)
				return false;
			return _cmp.gameObject.GetComponent<ChaCustomHairComponent>() != null;
		}
	}
}
