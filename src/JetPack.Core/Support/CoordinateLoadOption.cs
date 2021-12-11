using System.Collections.Generic;
using System.Linq;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public partial class CoordinateLoadOption
	{
		public static bool Installed = false;
		public static BaseUnityPlugin Instance = null;
		public static bool Safe = true;

		internal static void Init()
		{
			if (Toolbox.GetPluginInstance("com.jim60105.kk.studiocoordinateloadoption") != null)
			{
				Installed = true;
				Instance = Toolbox.GetPluginInstance("com.jim60105.kk.studiocoordinateloadoption");
				Safe = false;
				return;
			}
			if (Toolbox.GetPluginInstance("com.jim60105.kk.coordinateloadoption") != null)
			{
				Installed = true;
				Instance = Toolbox.GetPluginInstance("com.jim60105.kk.coordinateloadoption");
				Safe = Toolbox.PluginVersionCompare("com.jim60105.kk.coordinateloadoption", "21.12.11");
				return;
			}
		}

		public static string[] GetBlackList()
		{
			return Traverse.Create(Instance).Field("pluginBoundAccessories").GetValue<string[]>();
		}

		public static void AddBlackList(string _guid)
		{
			List<string> _list = GetBlackList().Where(x => !x.IsNullOrEmpty()).ToList();
			_list.ForEach(x => x.Trim());
			if (_list.Contains(_guid.Trim())) return;

			_list.Add(_guid.Trim());
			Traverse.Create(Instance).Field("pluginBoundAccessories").SetValue(_list.ToArray());
		}

		public static void DelBlackList(string _guid)
		{
			List<string> _list = GetBlackList().Where(x => !x.IsNullOrEmpty()).ToList();
			_list.ForEach(x => x.Trim());
			if (!_list.Contains(_guid.Trim())) return;

			_list.RemoveAll(x => x == _guid.Trim());
			Traverse.Create(Instance).Field("pluginBoundAccessories").SetValue(_list.ToArray());
		}
	}
}
