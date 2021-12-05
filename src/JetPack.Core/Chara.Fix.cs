using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using Manager;
using MessagePack;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using Sideloader.AutoResolver;

namespace JetPack
{
	public partial class Core
	{
		internal static ConfigEntry<bool> _cfgFixChaCustomHairComponent;
		internal static ConfigEntry<bool> _cfgFixChaClothesComponent;
		internal static ConfigEntry<bool> _cfgFixChaAccessoryComponent;

		internal void Chara_Fix_Config_Init()
		{
			_cfgFixChaCustomHairComponent = Config.Bind("Fix", "ChaCustomHairComponent", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 10 }));
			_cfgFixChaClothesComponent = Config.Bind("Fix", "ChaClothesComponent", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 9 }));
			_cfgFixChaAccessoryComponent = Config.Bind("Fix", "ChaAccessoryComponent", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 8 }));
		}
	}

	public partial class Chara
	{
		public static event EventHandler<LoadCharaFbxDataEventArgs> OnLoadCharaFbxData;
		public class LoadCharaFbxDataEventArgs : EventArgs
		{
			public LoadCharaFbxDataEventArgs(ChaControl _chaCtrl, GameObject _gameObject)
			{
				ChaControl = _chaCtrl;
				GameObject = _gameObject;
			}

			public ChaControl ChaControl { get; }
			public GameObject GameObject { get; }
		}

		internal static partial class Hooks
		{
			internal static void Init()
			{
				Core._instance.Chara_Fix_Config_Init();

				{
					BaseUnityPlugin _instance = Toolbox.GetPluginInstance("com.bepis.bepinex.sideloader");
					Type _type = _instance.GetType().Assembly.GetType("Sideloader.AutoResolver.UniversalAutoResolver+Hooks");
					MethodInfo _method = _type.GetMethod("ExtendedCoordinateLoad", AccessTools.all, null, new[] { typeof(ChaFileCoordinate) }, null);
					Core._hookInstance.Patch(_method, postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.UniversalAutoResolver_Hooks_ExtendedCoordinateLoad_Postfix)));
				}
			}
#if KK
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
#elif KKS
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataNoAsync))]
#endif
			internal static void ChaControl_LoadCharaFbxDataAsync_Prefix(ChaControl __instance, ref Action<GameObject> actObj)
			{
				Action<GameObject> _oldAct = actObj;
				actObj = delegate (GameObject _gameObject)
				{
					_oldAct(_gameObject);
					if (_gameObject == null) return;

					if (Core._cfgFixChaCustomHairComponent.Value)
					{
						ChaCustomHairComponent[] _cmps = _gameObject.GetComponentsInChildren<ChaCustomHairComponent>(true);
						if (_cmps?.Length > 0)
						{
							foreach (ChaCustomHairComponent _cmp in _cmps)
								ChaCustomHairComponent_Constructor_Postfix(__instance, _cmp);
						}
					}

					if (Core._cfgFixChaClothesComponent.Value)
					{
						ChaClothesComponent[] _cmps = _gameObject.GetComponentsInChildren<ChaClothesComponent>(true);
						if (_cmps?.Length > 0)
						{
							foreach (ChaClothesComponent _cmp in _cmps)
								ChaClothesComponent_Constructor_Postfix(__instance, _cmp);
						}
					}

					if (Core._cfgFixChaAccessoryComponent.Value)
					{
						ChaAccessoryComponent[] _cmps = _gameObject.GetComponentsInChildren<ChaAccessoryComponent>(true);
						if (_cmps?.Length > 0)
						{
							foreach (ChaAccessoryComponent _cmp in _cmps)
								ChaAccessoryComponent_Constructor_Postfix(__instance, _cmp);
						}
					}

					if (_gameObject.GetComponent<ComponentLookupTable>() == null)
						_gameObject.AddComponent<ComponentLookupTable>().Init(_gameObject);

					OnLoadCharaFbxData?.Invoke(null, new LoadCharaFbxDataEventArgs(__instance, _gameObject));
				};
			}

			private static string ComponentErrorMsg(Component _cmp, string _name, ListInfoBase _data)
			{
				string _guid = "(hardmod)";
				int _itemID = -1;

				if (_data.Id >= UniversalAutoResolver.BaseSlotID)
				{
					_guid = "(missing)";
					ResolveInfo _info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo) _data.Category, _data.Id);
					if (_info != null)
					{
						_guid = _info.GUID;
						_itemID = _info.Slot;
					}
				}

				return $"[{_cmp.GetType().Name}] bad {_name} setting(s) found in [{_cmp.gameObject.name}][{_guid}][{(ChaListDefine.CategoryNo) _data.Category} ({_data.Category})][{_itemID}][{_data.Name}]";
			}

			internal static void ChaCustomHairComponent_Constructor_Postfix(ChaControl _chaCtrl, ChaCustomHairComponent __instance)
			{
				_chaCtrl.StartCoroutine(Wait());
				//__instance.enabled = false;

				IEnumerator Wait()
				{
					while (__instance != null && __instance?.gameObject == null)
						yield return null;

					if (__instance == null)
						yield break;

					ListInfoBase _data = __instance.gameObject.GetComponent<ListInfoComponent>()?.data;
					if (_data == null)
						yield break;

					if (__instance.rendHair?.Length > 0)
					{
						int n = __instance.rendHair.Length;
						__instance.rendHair = __instance.rendHair?.Where(x => x != null).ToArray();
						if (__instance.rendHair.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendHair", _data));
					}
					if (__instance.rendAccessory?.Length > 0)
					{
						int n = __instance.rendAccessory.Length;
						__instance.rendAccessory = __instance.rendAccessory?.Where(x => x != null).ToArray();
						if (__instance.rendAccessory.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendAccessory", _data));
					}
					if (__instance.trfLength?.Length > 0)
					{
						int n = __instance.trfLength.Length;
						__instance.trfLength = __instance.trfLength?.Where(x => x != null).ToArray();
						if (__instance.trfLength.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "trfLength", _data));
					}

					//__instance.enabled = true;
				}
			}

			internal static void ChaClothesComponent_Constructor_Postfix(ChaControl _chaCtrl, ChaClothesComponent __instance)
			{
				_chaCtrl.StartCoroutine(Wait());
				//__instance.enabled = false;

				IEnumerator Wait()
				{
					while (__instance != null && __instance?.gameObject == null)
						yield return null;

					if (__instance == null)
						yield break;

					ListInfoBase _data = __instance.gameObject.GetComponent<ListInfoComponent>()?.data;
					if (_data == null)
						yield break;

					if (__instance.rendNormal01?.Length > 0)
					{
						int n = __instance.rendNormal01.Length;
						__instance.rendNormal01 = __instance.rendNormal01?.Where(x => x != null).ToArray();
						if (__instance.rendNormal01.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendNormal01", _data));
					}
					if (__instance.rendNormal02?.Length > 0)
					{
						int n = __instance.rendNormal02.Length;
						__instance.rendNormal02 = __instance.rendNormal02?.Where(x => x != null).ToArray();
						if (__instance.rendNormal02.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendNormal02", _data));
					}
					/*
					if (__instance.rendNormal03?.Length > 0)
					{
						int n = __instance.rendNormal03.Length;
						__instance.rendNormal03 = __instance.rendNormal03?.Where(x => x != null).ToArray();
						if (__instance.rendNormal03.Length < n)
							Core._logger.LogError($"[ChaClothesComponent] bad rendNormal03 setting(s) found in {__instance.gameObject.name}");
					}
					*/
					if (Game.HasDarkness)
						ComponentArrayFix<Renderer>(__instance, "rendNormal03", _data);

					//__instance.enabled = true;
				}
			}

			internal static void ComponentArrayFix<T>(Component _cmp, string _name, ListInfoBase _data)
			{
				Traverse _traverse = Traverse.Create(_cmp);
				T[] _array = _traverse.Field(_name).GetValue<T[]>();
				if (_array == null || _array?.Length == 0) return;

				int n = _array.Length;
				//if (n > 0)
				{
					List<T> _list = new List<T>();
					for (int i = 0; i < n; i++)
					{
						T x = _array[i];
						if (x != null)
							_list.Add(x);
					}

					if (_list.Count == n) return;

					_traverse.Field(_name).SetValue(_list.ToArray());
					Core.DebugLog(LogLevel.Error, ComponentErrorMsg(_cmp, _name, _data));
				}
			}

			internal static void ChaAccessoryComponent_Constructor_Postfix(ChaControl _chaCtrl, ChaAccessoryComponent __instance)
			{
				_chaCtrl.StartCoroutine(Wait());
				//__instance.enabled = false;

				IEnumerator Wait()
				{
					while (__instance != null && __instance?.gameObject == null)
						yield return null;

					if (__instance == null)
						yield break;

					ListInfoBase _data = __instance.gameObject.GetComponent<ListInfoComponent>()?.data;
					if (_data == null)
						yield break;

					if (__instance.rendNormal?.Length > 0)
					{
						int n = __instance.rendNormal.Length;
						__instance.rendNormal = __instance.rendNormal?.Where(x => x != null).ToArray();
						if (__instance.rendNormal.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendNormal", _data));
					}
					if (__instance.rendAlpha?.Length > 0)
					{
						int n = __instance.rendAlpha.Length;
						__instance.rendAlpha = __instance.rendAlpha?.Where(x => x != null).ToArray();
						if (__instance.rendAlpha.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendAlpha", _data));
					}
					if (__instance.rendHair?.Length > 0)
					{
						int n = __instance.rendHair.Length;
						__instance.rendHair = __instance.rendHair?.Where(x => x != null).ToArray();
						if (__instance.rendHair.Length < n)
							Core.DebugLog(LogLevel.Error, ComponentErrorMsg(__instance, "rendHair", _data));
					}

					//__instance.enabled = true;
				}
			}

			internal static void UniversalAutoResolver_Hooks_ExtendedCoordinateLoad_Postfix(ChaFileCoordinate file)
			{
				if (CharaMaker.Inside || CharaStudio.Running) return;

				ChaControl _chaCtrl = null;
				ChaFile _chaFile = null;
#if KK
				foreach (KeyValuePair<int, ChaControl> x in Character.Instance.dictEntryChara)
#else
				foreach (KeyValuePair<int, ChaControl> x in Character.dictEntryChara)
#endif
				{
					if (x.Value.nowCoordinate == file)
					{
						_chaCtrl = x.Value;
						_chaFile = x.Value.chaFile;
						break;
					}
				}
				if (_chaFile == null) return;

				int _currentCoordinateIndex = _chaFile.status.coordinateType;
				{
					byte[] _byte = MessagePackSerializer.Serialize(file.clothes.parts);
					_chaCtrl.chaFile.coordinate[_currentCoordinateIndex].clothes.parts = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo[]>(_byte);
				}
				{
					byte[] _byte = MessagePackSerializer.Serialize(file.clothes.subPartsId);
					_chaCtrl.chaFile.coordinate[_currentCoordinateIndex].clothes.subPartsId = MessagePackSerializer.Deserialize<int[]>(_byte);
				}
				{
					byte[] _byte = MessagePackSerializer.Serialize(file.accessory.parts);
					_chaCtrl.chaFile.coordinate[_currentCoordinateIndex].accessory.parts = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo[]>(_byte);
				}
			}
		}
	}
}
