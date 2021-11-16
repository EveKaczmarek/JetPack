using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BepInEx;
using HarmonyLib;

using KK_Plugins.MaterialEditor;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;

namespace JetPack
{
	public class MaterialEditor
	{
		public static bool Installed = false;
		public static BaseUnityPlugin Instance = null;
		public static Dictionary<string, Type> Type = new Dictionary<string, Type>();

		public static readonly List<string> ContainerKeys = new List<string>() { "RendererPropertyList", "MaterialShaderList", "MaterialFloatPropertyList", "MaterialColorPropertyList", "MaterialTexturePropertyList", "MaterialCopyList" };

		private static MethodInfo _loadData = null;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.deathweasel.bepinex.materialeditor");
			if (Instance == null) return;

			Installed = true;
			Type["MaterialAPI"] = Instance.GetType().Assembly.GetType("MaterialEditorAPI.MaterialAPI");
			Type["MaterialEditorCharaController"] = Instance.GetType().Assembly.GetType("KK_Plugins.MaterialEditor.MaterialEditorCharaController");

			_loadData = Type["MaterialEditorCharaController"].GetMethod("LoadData", AccessTools.all, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);

			foreach (string _key in ContainerKeys)
			{
				string _name = "KK_Plugins.MaterialEditor.MaterialEditorCharaController+" + _key.Replace("List", "");
				Type[_name] = Instance.GetType().Assembly.GetType(_name);
			}

			Core._hookInstance.PatchAll(typeof(Hooks));

			OnDataApply += (_sender, _args) =>
			{
				Core.DebugLog($"[OnDataApply][{_args.State}][{_args.DuringChange}]");
			};
		}

		public static object GetController(ChaControl _chaCtrl) => MaterialEditorPlugin.GetCharaController(_chaCtrl);

		internal static partial class Hooks
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorCharaController), "CorrectTongue")]
			private static void MaterialEditorCharaController_CorrectTongue_Prefix(MaterialEditorCharaController __instance)
			{
				OnDataApply?.Invoke(null, new ControllerEventArgs(__instance, "Prefix"));
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPostfix, HarmonyPatch(typeof(MaterialEditorCharaController), "CorrectTongue")]
			private static void MaterialEditorCharaController_CorrectTongue_Postfix(MaterialEditorCharaController __instance)
			{
				OnDataApply?.Invoke(null, new ControllerEventArgs(__instance, "Postfix"));
				Instance.StartCoroutine(MaterialEditorCharaController_CorrectTongue_Coroutine(__instance));
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorCharaController), "CoordinateChangedEvent")]
			private static bool MaterialEditorCharaController_CoordinateChangedEvent_Prefix(object __instance)
			{
				if (CharaMaker.Inside || CharaStudio.Running) return true;

				ChaControl _chaCtrl = Traverse.Create(__instance).Property("ChaControl").GetValue<ChaControl>();
				// https://stackoverflow.com/questions/66028312/c-sharp-find-a-method-by-name-and-run-it-ienumerator-method
				_chaCtrl.StartCoroutine((IEnumerator) _loadData.Invoke(__instance, new object[] { true, true, false }));
				return false;
			}
		}

		private static IEnumerator MaterialEditorCharaController_CorrectTongue_Coroutine(MaterialEditorCharaController _pluginCtrl)
		{
			yield return Toolbox.WaitForEndOfFrame;
			yield return Toolbox.WaitForEndOfFrame;
			OnDataApply?.Invoke(null, new ControllerEventArgs(_pluginCtrl, "Coroutine"));
		}

		public static event EventHandler<ControllerEventArgs> OnDataApply;
		public class ControllerEventArgs : EventArgs
		{
			public ControllerEventArgs(MaterialEditorCharaController _pluginCtrl, string _state)
			{
				Controller = _pluginCtrl;
				State = _state;
				if (_state == "Coroutine")
					DuringChange = false;
				else
					DuringChange = true;
			}

			public object Controller { get; }
			public string State { get; }
			public bool DuringChange { get; } = false;
		}
	}

	public static partial class MaterialEditorExtension
	{
		public static void MEAddRange(this object _self, object _object)
		{
			if (_self is List<RendererProperty>)
				(_self as List<RendererProperty>).AddRange(_object as List<RendererProperty>);
			else if (_self is List<MaterialFloatProperty>)
				(_self as List<MaterialFloatProperty>).AddRange(_object as List<MaterialFloatProperty>);
			else if (_self is List<MaterialColorProperty>)
				(_self as List<MaterialColorProperty>).AddRange(_object as List<MaterialColorProperty>);
			else if (_self is List<MaterialTextureProperty>)
				(_self as List<MaterialTextureProperty>).AddRange(_object as List<MaterialTextureProperty>);
			else if (_self is List<MaterialShader>)
				(_self as List<MaterialShader>).AddRange(_object as List<MaterialShader>);
			else if (_self is List<MaterialCopy>)
				(_self as List<MaterialCopy>).AddRange(_object as List<MaterialCopy>);
		}

		public static int MERemoveAll(this object _self, Func<object, bool> _match)
		{
			if (_self is List<RendererProperty>)
				return (_self as List<RendererProperty>).RemoveAll(new Predicate<RendererProperty>(_match));
			else if (_self is List<MaterialFloatProperty>)
				return (_self as List<MaterialFloatProperty>).RemoveAll(new Predicate<MaterialFloatProperty>(_match));
			else if (_self is List<MaterialColorProperty>)
				return (_self as List<MaterialColorProperty>).RemoveAll(new Predicate<MaterialColorProperty>(_match));
			else if (_self is List<MaterialTextureProperty>)
				return (_self as List<MaterialTextureProperty>).RemoveAll(new Predicate<MaterialTextureProperty>(_match));
			else if (_self is List<MaterialShader>)
				return (_self as List<MaterialShader>).RemoveAll(new Predicate<MaterialShader>(_match));
			else if (_self is List<MaterialCopy>)
				return (_self as List<MaterialCopy>).RemoveAll(new Predicate<MaterialCopy>(_match));
			return 0;
		}

		public static object MEWhere(this object _self, Func<object, bool> _match)
		{
			if (_self is IEnumerable<RendererProperty>)
				return (_self as IEnumerable<RendererProperty>).Where(new Func<RendererProperty, bool>(_match));
			else if (_self is IEnumerable<MaterialFloatProperty>)
				return (_self as IEnumerable<MaterialFloatProperty>).Where(new Func<MaterialFloatProperty, bool>(_match));
			else if (_self is IEnumerable<MaterialColorProperty>)
				return (_self as IEnumerable<MaterialColorProperty>).Where(new Func<MaterialColorProperty, bool>(_match));
			else if (_self is IEnumerable<MaterialTextureProperty>)
				return (_self as IEnumerable<MaterialTextureProperty>).Where(new Func<MaterialTextureProperty, bool>(_match));
			else if (_self is IEnumerable<MaterialShader>)
				return (_self as IEnumerable<MaterialShader>).Where(new Func<MaterialShader, bool>(_match));
			else if (_self is IEnumerable<MaterialCopy>)
				return (_self as IEnumerable<MaterialCopy>).Where(new Func<MaterialCopy, bool>(_match));
			return null;
		}

		public static void MEForEach(this object _self, Action<object> _action)
		{
			if (_self is List<RendererProperty>)
				(_self as List<RendererProperty>).ForEach(new Action<RendererProperty>(_action));
			else if (_self is List<MaterialFloatProperty>)
				(_self as List<MaterialFloatProperty>).ForEach(new Action<MaterialFloatProperty>(_action));
			else if (_self is List<MaterialColorProperty>)
				(_self as List<MaterialColorProperty>).ForEach(new Action<MaterialColorProperty>(_action));
			else if (_self is List<RendererProperty>)
				(_self as List<RendererProperty>).ForEach(new Action<RendererProperty>(_action));
			else if (_self is List<MaterialTextureProperty>)
				(_self as List<MaterialTextureProperty>).ForEach(new Action<MaterialTextureProperty>(_action));
			else if (_self is List<MaterialShader>)
				(_self as List<MaterialShader>).ForEach(new Action<MaterialShader>(_action));
			else if (_self is List<MaterialCopy>)
				(_self as List<MaterialCopy>).ForEach(new Action<MaterialCopy>(_action));
		}
	}
}
