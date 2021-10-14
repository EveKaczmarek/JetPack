using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public partial class MoreOutfits
	{
		public static bool Installed = false;
		public static BaseUnityPlugin Instance = null;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.deathweasel.bepinex.moreoutfits");
			if (Instance == null) return;

			Installed = true;
		}

		internal static object GetController(ChaControl _chaCtrl)
		{
			if (!Installed) return null;
			return Traverse.Create(Instance).Method("GetController", new object[] { _chaCtrl }).GetValue();
		}

		public static string GetCoodinateName(ChaControl _chaCtrl, int _coordinateIndex)
		{
			if (!Installed) return "";
			return Traverse.Create(Instance).Method("GetCoodinateName", new object[] { _chaCtrl, _coordinateIndex }).GetValue<string>();
		}

		public static Dictionary<int, string> ListCoordinateNames(ChaControl _chaCtrl) => ListCoordinateNames(GetController(_chaCtrl));
		public static Dictionary<int, string> ListCoordinateNames(object _pluginCtrl)
		{
			if (!Installed) return new Dictionary<int, string>();

			return Traverse.Create(_pluginCtrl).Field("CoordinateNames").GetValue<Dictionary<int, string>>() ?? new Dictionary<int, string>();
		}
	}
}
