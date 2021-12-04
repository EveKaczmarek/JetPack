using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace JetPack
{
	[BepInPlugin(GUID, Name, Version)]
#if KKS
	[BepInDependency("marco.kkapi", "1.26")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.1.2")]
#elif KK
	[BepInDependency("marco.kkapi", "1.17")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.1.1")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.1.0")]
#endif
	public partial class Core : BaseUnityPlugin
	{
		public const string GUID = "madevil.JetPack";
#if DEBUG
		public const string Name = "JetPack (Debug Build)";
#else
		public const string Name = "JetPack";
#endif
		public const string Version = "2.2.3.0";

		internal static ManualLogSource _logger;
		internal static Core _instance;
		internal static Harmony _hookInstance;

		private static ConfigEntry<bool> _cfgDebugMsg;
		internal static ConfigEntry<bool> _cfgMigrationOnLoad;
		internal static ConfigEntry<bool> _cfgMigrationCordBrowse;

		private void Awake()
		{
			_logger = base.Logger;
			_instance = this;

			_cfgDebugMsg = Config.Bind("Debug", "Display debug message", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 20 }));

			if (Application.productName == Constants.StudioProcessName)
				CharaStudio.Running = true;
			if (Application.productName == Constants.VRProcessName
#if KK
				|| Application.productName == Constants.VRProcessNameSteam
#endif
			)
			{
				CharaHscene.Inside = true;
				CharaHscene.VR = true;
			}

			_cfgMigrationOnLoad = Config.Bind("Migration", "Enable OnLoad", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 20 }));
			_cfgMigrationCordBrowse = Config.Bind("Migration", "Coordinate Browse", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 10 }));
		}

		private void Start()
		{
			Game.HasDarkness = typeof(ChaControl).GetProperties(AccessTools.all).Any(x => x.Name == "exType");
			Game.ConsoleActive = Traverse.Create(typeof(BepInEx.Bootstrap.Chainloader).Assembly.GetType("BepInEx.ConsoleManager")).Property("ConsoleActive").GetValue<bool>();

			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			MoreAccessories.Init();
			Chara.Init();
			KKAPI.Init();
			Migration.Init();
			MoreOutfits.Init();
			MaterialEditor.Init();

			if (CharaStudio.Running)
			{
				CharaStudio.OnStudioLoaded += CharaStudio.RegisterControls;
				SceneManager.sceneLoaded += CharaStudio.SceneLoaded;
			}
			else
			{
				SceneManager.sceneLoaded += SceneLoaded;
				CharaMaker.Init();
				CharaHscene.Init();
			}
		}

		private static void SceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
		{
			DebugLog($"[SceneLoaded][name: {_scene.name}][mode: {_loadSceneMode}]");
			if (_scene.name == "CustomScene")
				CharaMaker.InvokeOnMakerStartLoading(null, null);
			else if (_scene.name == "HProc" || _scene.name == "VRHScene")
			{
				CharaHscene.Inside = true;
				CharaHscene.Hooks.Init();
				CharaHscene.InvokeOnHSceneStartLoading(null, null);
			}
		}

		private static class Hooks { }

		internal static void DebugLog(object _msg) => DebugLog(LogLevel.Warning, _msg);
		internal static void DebugLog(LogLevel _level, object _msg)
		{
			if (_cfgDebugMsg.Value)
				_logger.Log(_level, _msg);
			else
				_logger.Log(LogLevel.Debug, _msg);
		}
	}

	public class Game
	{
		public static bool HasDarkness = false;
		public static bool ConsoleActive = false;
	}

	public partial class Storage
	{
		public static int _focusWindowID = -1;
	}

	public partial class Constants
	{
#if KK
		public const string Prefix = "KK";
		public const string GameName = "Koikatsu";
		public const string StudioProcessName = "CharaStudio";
		public const string MainGameProcessName = "Koikatu";
		public const string MainGameProcessNameSteam = "Koikatsu Party";
		public const string VRProcessName = "KoikatuVR";
		public const string VRProcessNameSteam = "Koikatsu Party VR";
#elif KKS
		public const string Prefix = "KKS";
		public const string GameName = "Koikatsu Sunshine";
		public const string StudioProcessName = "CharaStudio";
		public const string MainGameProcessName = "KoikatsuSunshine";
		public const string VRProcessName = "KoikatsuSunshine_VR";
#endif
	}
}
