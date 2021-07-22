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
	[BepInDependency("marco.kkapi", "1.20")]
#elif KK
	[BepInDependency("marco.kkapi", "1.17")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.0")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.1.0")]
#endif
	public partial class Core : BaseUnityPlugin
	{
		public const string GUID = "madevil.JetPack";
		public const string Name = "JetPack";
		public const string Version = "2.0.8.0";

		internal static ManualLogSource _logger;
		internal static Harmony _hookInstance;

		private static ConfigEntry<bool> _cfgDebugMsg;

		private void Awake()
		{
			_logger = base.Logger;

			_cfgDebugMsg = Config.Bind("Debug", "Display debug message", false);

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				CharaStudio.Running = true;
			if (Application.dataPath.EndsWith("KoikatuVR_Data"))
			{
				CharaHscene.Inside = true;
				CharaHscene.VR = true;
			}
		}

		private void Start()
		{
			Game.HasDarkness = typeof(ChaControl).GetProperties(AccessTools.all).Any(x => x.Name == "exType");

			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			Chara.Init();
			KKAPI.Init();
			MoreAccessories.Init();
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
	}

	public partial class Storage
	{
		public static int _focusWindowID = -1;
	}
}
