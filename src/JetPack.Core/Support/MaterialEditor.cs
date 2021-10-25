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
		public static void MEAddRange(this object self, object obj)
		{
			if (self is List<RendererProperty>)
				(self as List<RendererProperty>).AddRange(obj as List<RendererProperty>);
			else if (self is List<MaterialFloatProperty>)
				(self as List<MaterialFloatProperty>).AddRange(obj as List<MaterialFloatProperty>);
			else if (self is List<MaterialColorProperty>)
				(self as List<MaterialColorProperty>).AddRange(obj as List<MaterialColorProperty>);
			else if (self is List<MaterialTextureProperty>)
				(self as List<MaterialTextureProperty>).AddRange(obj as List<MaterialTextureProperty>);
			else if (self is List<MaterialShader>)
				(self as List<MaterialShader>).AddRange(obj as List<MaterialShader>);
			else if (self is List<MaterialCopy>)
				(self as List<MaterialCopy>).AddRange(obj as List<MaterialCopy>);
		}

		public static int MERemoveAll(this object self, Func<object, bool> match)
		{
			if (self is List<RendererProperty>)
				return (self as List<RendererProperty>).RemoveAll(new Predicate<RendererProperty>(match));
			else if (self is List<MaterialFloatProperty>)
				return (self as List<MaterialFloatProperty>).RemoveAll(new Predicate<MaterialFloatProperty>(match));
			else if (self is List<MaterialColorProperty>)
				return (self as List<MaterialColorProperty>).RemoveAll(new Predicate<MaterialColorProperty>(match));
			else if (self is List<MaterialTextureProperty>)
				return (self as List<MaterialTextureProperty>).RemoveAll(new Predicate<MaterialTextureProperty>(match));
			else if (self is List<MaterialShader>)
				return (self as List<MaterialShader>).RemoveAll(new Predicate<MaterialShader>(match));
			else if (self is List<MaterialCopy>)
				return (self as List<MaterialCopy>).RemoveAll(new Predicate<MaterialCopy>(match));
			return 0;
		}

		public static object MEWhere(this object self, Func<object, bool> match)
		{
			if (self is IEnumerable<RendererProperty>)
				return (self as IEnumerable<RendererProperty>).Where(new Func<RendererProperty, bool>(match));
			else if (self is IEnumerable<MaterialFloatProperty>)
				return (self as IEnumerable<MaterialFloatProperty>).Where(new Func<MaterialFloatProperty, bool>(match));
			else if (self is IEnumerable<MaterialColorProperty>)
				return (self as IEnumerable<MaterialColorProperty>).Where(new Func<MaterialColorProperty, bool>(match));
			else if (self is IEnumerable<MaterialTextureProperty>)
				return (self as IEnumerable<MaterialTextureProperty>).Where(new Func<MaterialTextureProperty, bool>(match));
			else if (self is IEnumerable<MaterialShader>)
				return (self as IEnumerable<MaterialShader>).Where(new Func<MaterialShader, bool>(match));
			else if (self is IEnumerable<MaterialCopy>)
				return (self as IEnumerable<MaterialCopy>).Where(new Func<MaterialCopy, bool>(match));
			return null;
		}

		public static void MEForEach(this object self, Action<object> action)
		{
			if (self is List<RendererProperty>)
				(self as List<RendererProperty>).ForEach(new Action<RendererProperty>(action));
			else if (self is List<MaterialFloatProperty>)
				(self as List<MaterialFloatProperty>).ForEach(new Action<MaterialFloatProperty>(action));
			else if (self is List<MaterialColorProperty>)
				(self as List<MaterialColorProperty>).ForEach(new Action<MaterialColorProperty>(action));
			else if (self is List<RendererProperty>)
				(self as List<RendererProperty>).ForEach(new Action<RendererProperty>(action));
			else if (self is List<MaterialTextureProperty>)
				(self as List<MaterialTextureProperty>).ForEach(new Action<MaterialTextureProperty>(action));
			else if (self is List<MaterialShader>)
				(self as List<MaterialShader>).ForEach(new Action<MaterialShader>(action));
			else if (self is List<MaterialCopy>)
				(self as List<MaterialCopy>).ForEach(new Action<MaterialCopy>(action));
		}
	}
}
