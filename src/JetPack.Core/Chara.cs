using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

namespace JetPack
{
	public partial class Chara
	{
		internal static List<string> _cordNames = new List<string>();

		internal static void Init()
		{
			_cordNames = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).ToList();

			Core._hookInstance.PatchAll(typeof(Hooks));

			OnChangeCoordinateType += (_sender, _args) =>
			{
				Core.DebugLog($"[OnChangeCoordinateType][{_args.CoordinateType}][{_args.State}][{_args.DuringChange}]");
				CharaMaker.UpdateAccssoryIndex();
				CharaStudio.RefreshCharaStatePanel();
			};
		}

		public static string GetCoordinateName(ChaControl _chaCtrl, int _coordinateIndex)
		{
			if (_coordinateIndex < _cordNames.Count)
				return _cordNames[_coordinateIndex];

			return MoreOutfits.GetCoodinateName(_chaCtrl, _coordinateIndex);
		}

		public static List<string> ListCoordinateNames(ChaControl _chaCtrl)
		{
			List<string> _names = _cordNames.ToList();

			if (!MoreOutfits.Installed)
				return _names;

			_names.AddRange(MoreOutfits.ListCoordinateNames(_chaCtrl).Values?.ToList() ?? new List<string>());

			return _names;
		}

		public static event EventHandler<ChangeCoordinateTypeEventArgs> OnChangeCoordinateType;
		public class ChangeCoordinateTypeEventArgs : EventArgs
		{
			public ChangeCoordinateTypeEventArgs(ChaControl _chaCtrl, int _coordinateIndex, string _state)
			{
				ChaControl = _chaCtrl;
				CoordinateType = _coordinateIndex;
				State = _state;
				if (_state == "Coroutine")
					DuringChange = false;
				else
					DuringChange = true;
			}

			public ChaControl ChaControl { get; }
			public int CoordinateType { get; }
			public string State { get; }
			public bool DuringChange { get; } = false;
		}

		internal class Hooks
		{
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			private static void ChaControl_ChangeCoordinateType_Prefix(ChaControl __instance, ChaFileDefine.CoordinateType type)
			{
				OnChangeCoordinateType?.Invoke(null, new ChangeCoordinateTypeEventArgs(__instance, (int) type, "Prefix"));
			}

			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			private static void ChaControl_ChangeCoordinateType_Postfix(ChaControl __instance, ChaFileDefine.CoordinateType type)
			{
				OnChangeCoordinateType?.Invoke(null, new ChangeCoordinateTypeEventArgs(__instance, (int) type, "Postfix"));
				__instance.StartCoroutine(ChaControl_ChangeCoordinateType_Coroutine(__instance, type));
			}

			private static IEnumerator ChaControl_ChangeCoordinateType_Coroutine(ChaControl __instance, ChaFileDefine.CoordinateType type)
			{
				yield return Toolbox.WaitForEndOfFrame;
				yield return Toolbox.WaitForEndOfFrame;
				OnChangeCoordinateType?.Invoke(null, new ChangeCoordinateTypeEventArgs(__instance, (int) type, "Coroutine"));
			}
		}
	}
}
