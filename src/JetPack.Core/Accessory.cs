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

			if (_slotIndex < 20 || MoreAccessories.BuggyBootleg)
				return Instance.CvsAccessoryCopy.tglKind.ElementAtOrDefault(_slotIndex);

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			return CopyMoreToggle(_slotIndex);
		}

		internal static Toggle CopyMoreToggle(int _slotIndex)
		{
			List<MoreAccessoriesKOI.MoreAccessories.CharaMakerSlotData> _additionalCharaMakerSlots = (MoreAccessories.Instance as MoreAccessoriesKOI.MoreAccessories)._additionalCharaMakerSlots;
			if (_slotIndex - 20 >= _additionalCharaMakerSlots?.Count) return null;

			return _additionalCharaMakerSlots.ElementAtOrDefault(_slotIndex - 20)?.copyToggle;
		}

		public static GameObject GetObjAcsMove(int _slotIndex)
		{
			if (_slotIndex < 0) return null;

			if (_slotIndex < 20 || MoreAccessories.BuggyBootleg)
			{
				if (_slotIndex >= CustomBase.Instance.chaCtrl.objAcsMove.Length) return null;
				return CustomBase.Instance.chaCtrl.objAcsMove[_slotIndex, 1];
			}

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			List<GameObject[]> _objAcsMove = Traverse.Create(MoreAccessories.Instance).Field("_charaMakerData").Field("objAcsMove").GetValue<List<GameObject[]>>();
			if (_objAcsMove != null)
				return _objAcsMove.ElementAtOrDefault(_slotIndex - 20)?.ElementAtOrDefault(1);
			return null;
		}

		public static CvsAccessory GetCvsAccessory(int _slotIndex)
		{
			if (_slotIndex < 0) return null;

			if (MoreAccessories.BuggyBootleg)
			{
				if (_slotIndex >= AccListContainer.transform.childCount) return null;
				return AccListContainer.transform.GetChild(_slotIndex)?.GetComponentInChildren<CvsAccessory>(true);
			}

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			return MoreAccessories.GetCvsAccessory(_slotIndex);
		}
	}

	public partial class Accessory
	{
		public static bool GetAccessoryVisibility(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return false;

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return false;

			List<bool> _parts = ListAccessoryVisibility(_chaCtrl);
			if (_slotIndex >= _parts.Count) return false;

			return _parts.ElementAtOrDefault(_slotIndex);
		}

		public static void SetAccessoryVisibility(ChaControl _chaCtrl, int _slotIndex, bool _show)
		{
			if (_slotIndex < 0) return;

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return;

			List<bool> _parts = ListAccessoryVisibility(_chaCtrl);
			if (_slotIndex >= _parts.Count) return;

			_parts[_slotIndex] = _show;
		}

		public static List<bool> ListAccessoryVisibility(ChaControl _chaCtrl)
		{
			List<bool> _parts = _chaCtrl.fileStatus.showAccessory.ToList();

			if (MoreAccessories.Installed && !MoreAccessories.BuggyBootleg)
			{
				_parts = _parts.Take(20).ToList();
				_parts.AddRange(MoreAccessories.ListShowAccessories(_chaCtrl));
			}

			return _parts;
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _slotIndex) => GetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			if (_slotIndex < 0) return null;

			if (_slotIndex < 20 || MoreAccessories.BuggyBootleg)
				return _chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.ElementAtOrDefault(_slotIndex);

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return null;

			return MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex).ElementAtOrDefault(_slotIndex - 20);
		}

		public static void SetPartsInfo(ChaControl _chaCtrl, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo) => SetPartsInfo(_chaCtrl, _chaCtrl.fileStatus.coordinateType, _slotIndex, _partInfo);
		public static void SetPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex, ChaFileAccessory.PartsInfo _partInfo)
		{
			if (_slotIndex < 0) return;

			if (_slotIndex >= 20 && !MoreAccessories.Installed) return;

			if (_slotIndex < 20 || MoreAccessories.BuggyBootleg)
			{
				if (_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts.Length <= _slotIndex)
				{
					List<ChaFileAccessory.PartsInfo> _parts = new List<ChaFileAccessory.PartsInfo>(_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts);
					while (_parts.Count < _slotIndex + 1)
						_parts.Add(new ChaFileAccessory.PartsInfo());
					_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts = _parts.ToArray();
				}
				_chaCtrl.chaFile.coordinate[_coordinateIndex].accessory.parts[_slotIndex] = _partInfo;
			}
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

			if (MoreAccessories.Installed && !MoreAccessories.BuggyBootleg)
			{
				_partInfo = _partInfo.Take(20).ToList();
				_partInfo.AddRange(MoreAccessories.ListMorePartsInfo(_chaCtrl, _coordinateIndex));
			}

			return _partInfo;
		}

		public static List<ChaFileAccessory.PartsInfo> ListNowAccessories(ChaControl _chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> _partInfo = _chaCtrl.nowCoordinate.accessory.parts.ToList();

			if (MoreAccessories.Installed && !MoreAccessories.BuggyBootleg)
			{
				_partInfo = _partInfo.Take(20).ToList();
				_partInfo.AddRange(MoreAccessories.ListNowAccessories(_chaCtrl));
			}

			return _partInfo;
		}

		public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl _chaCtrl, int _slotIndex)
		{
			if (_slotIndex < 0) return null;

			GameObject _ca_slot = GetObjAccessory(_chaCtrl, _slotIndex);
			return _ca_slot == null ? null : _ca_slot.GetComponent<ChaAccessoryComponent>();

			//return Traverse.Create(MoreAccessories.Instance).Method("GetChaAccessoryComponent", new object[] { _chaCtrl, _slotIndex }).GetValue<ChaAccessoryComponent>();
		}

		public static List<ChaAccessoryComponent> ListChaAccessoryComponent(ChaControl _chaCtrl)
		{
			List<ChaAccessoryComponent> _parts = new List<ChaAccessoryComponent>();
			List<GameObject> _objAccessory = ListObjAccessory(_chaCtrl);
			foreach (GameObject _ca_slot in _objAccessory)
			{
				ChaAccessoryComponent _cmp = _ca_slot.GetComponent<ChaAccessoryComponent>();
				if (_cmp != null)
					_parts.Add(_cmp);
			}
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
			if (_slotIndex < 0) return false;

			GameObject _ca_slot = GetObjAccessory(_chaCtrl, _slotIndex);
			return _ca_slot == null ? false : _ca_slot.GetComponent<ChaCustomHairComponent>() != null;
		}
	}
}
