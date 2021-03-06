using System;
using System.Linq;

using UnityEngine;
using ChaCustom;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public class KKAPI
	{
		internal static BaseUnityPlugin _instance = null;
		internal static Type _makerAPI = null;
		internal static Type _makerInterfaceCreator = null;
		internal static Type _accessoriesApi = null;
		internal static Harmony _hookInstance;

		public static bool DevelopmentBuild = false;

		internal static void Init()
		{
			_instance = Toolbox.GetPluginInstance("marco.kkapi");

			if (Toolbox.PluginVersionCompare("marco.kkapi", "1.26"))
			{
				DevelopmentBuild = true;
				Core._logger.LogError($"This version of KKAPI is built for development purpose instead of production use");
			}

			_makerAPI = _instance.GetType().Assembly.GetType("KKAPI.Maker.MakerAPI");
			_makerInterfaceCreator = _instance.GetType().Assembly.GetType("KKAPI.Maker.MakerInterfaceCreator");
			_accessoriesApi = _instance.GetType().Assembly.GetType("KKAPI.Maker.AccessoriesApi");
			Hooks.Init();
		}

		internal static void OnMakerExiting()
		{
			_hookInstance.Unpatch(_makerInterfaceCreator.GetMethod("OnMakerAccSlotAdded", AccessTools.all), HarmonyPatchType.Postfix, _hookInstance.Id);
		}

		internal class Hooks
		{
			internal static void Init()
			{
				_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
			}

			internal static void OnMakerStartLoadingPatch()
			{
				Core.DebugLog($"[KKAPI.Hooks.OnMakerStartLoadingPatch]");
				_hookInstance.Patch(_makerAPI.GetMethod("OnMakerBaseLoaded", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_MakerAPI_OnMakerBaseLoaded_Postfix)));
				_hookInstance.Patch(_makerAPI.GetMethod("OnMakerFinishedLoading", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_MakerAPI_OnMakerFinishedLoading_Postfix)));
				_hookInstance.Patch(_makerInterfaceCreator.GetMethod("OnMakerAccSlotAdded", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_MakerInterfaceCreator_OnMakerAccSlotAdded_Postfix)));
				_hookInstance.Patch(_accessoriesApi.GetMethod("GetAccessoryObjects", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_AccessoriesApi_GetAccessoryObjects_Prefix)));
				_hookInstance.Patch(_accessoriesApi.GetMethod("GetAccessoryObject", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_AccessoriesApi_GetAccessoryObject_Prefix)));
				_hookInstance.Patch(_accessoriesApi.GetMethod("GetAccessory", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_AccessoriesApi_GetAccessory_Prefix)));
			}

			private static void KKAPI_MakerAPI_OnMakerBaseLoaded_Postfix()
			{
				CharaMaker.InvokeOnMakerBaseLoaded(null, null);
				_hookInstance.Unpatch(_makerAPI.GetMethod("OnMakerBaseLoaded", AccessTools.all), HarmonyPatchType.Postfix, _hookInstance.Id);
			}

			private static void KKAPI_MakerAPI_OnMakerFinishedLoading_Postfix()
			{
				CharaMaker.InvokeOnMakerFinishedLoading(null, null);
				_hookInstance.Unpatch(_makerAPI.GetMethod("OnMakerFinishedLoading", AccessTools.all), HarmonyPatchType.Postfix, _hookInstance.Id);
			}

			private static void KKAPI_MakerInterfaceCreator_OnMakerAccSlotAdded_Postfix(Transform newSlotTransform)
			{
				CvsAccessory _cmp = newSlotTransform.GetComponentsInParent<CvsAccessory>(true)?.FirstOrDefault();
				if (_cmp == null) return;

				Transform _transform = newSlotTransform.GetComponentsInParent<CharaMaker.CvsNavSideMenuEventHandler>(true)?.FirstOrDefault()?.transform;
				int _slotIndex = _cmp.nSlotNo;
				CharaMaker.InvokeOnSlotAdded(MoreAccessoriesKOI.MoreAccessories._self, new CharaMaker.SlotAddedEventArgs(_slotIndex, _transform));
			}

			private static bool KKAPI_AccessoriesApi_GetAccessoryObjects_Prefix(ChaControl character, ref GameObject[] __result)
			{
				__result = Accessory.ListObjAccessory(character).ToArray();
				return false;
			}

			private static bool KKAPI_AccessoriesApi_GetAccessoryObject_Prefix(ChaControl character, int index, ref GameObject __result)
			{
				__result = Accessory.GetObjAccessory(character, index);
				return false;
			}

			private static bool KKAPI_AccessoriesApi_GetAccessory_Prefix(ChaControl character, int accessoryIndex, ref ChaAccessoryComponent __result)
			{
				__result = Accessory.GetChaAccessoryComponent(character, accessoryIndex);
				return false;
			}
		}
	}
}
