using System;
using System.Collections.Generic;

using BepInEx;

using KK_Plugins.MaterialEditor;

namespace JetPack
{
	public class MaterialEditor
	{
		public static bool Installed = false;
		public static BaseUnityPlugin Instance = null;
		public static Dictionary<string, Type> Type = new Dictionary<string, Type>();

		public static readonly List<string> ContainerKeys = new List<string>() { "RendererPropertyList", "MaterialShaderList", "MaterialFloatPropertyList", "MaterialColorPropertyList", "MaterialTexturePropertyList", "MaterialCopyList" };

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.deathweasel.bepinex.materialeditor");
			if (Instance == null) return;

			Installed = true;
			Type["MaterialAPI"] = Instance.GetType().Assembly.GetType("MaterialEditorAPI.MaterialAPI");
			Type["MaterialEditorCharaController"] = Instance.GetType().Assembly.GetType("KK_Plugins.MaterialEditor.MaterialEditorCharaController");

			foreach (string _key in ContainerKeys)
			{
				string _name = "KK_Plugins.MaterialEditor.MaterialEditorCharaController+" + _key.Replace("List", "");
				Type[_name] = Instance.GetType().Assembly.GetType(_name);
			}
		}

		public static object GetController(ChaControl _chaCtrl)
		{
			return MaterialEditorPlugin.GetCharaController(_chaCtrl);
		}
	}
}
