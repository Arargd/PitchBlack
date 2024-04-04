using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using Unity.Mathematics;
using Zorro.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;


namespace PitchBlack
{
	[BepInPlugin("arargd.bepinex.pitchblack", "pitchblack", "1.1.0")]
	public class Main : BaseUnityPlugin
	{
		public static ConfigEntry<float> LightGain;

		public void Awake()
		{
			LightGain = Config.Bind("SETTINGS", "lightgain", -0.3f, "Adjusts the light gain, negative for darker lighting and positive for brighter. Generally stay in the -1 to 1 range.");
			Harmony harmony = new Harmony("arargdPitchBlack");
			harmony.PatchAll();
			Debug.Log("PitchBlack Loaded Successfully, I hope!");
		}

		public static void ApplyLightGain()
		{
			if (SceneManager.GetActiveScene().name == "HarborScene" || SceneManager.GetActiveScene().name == "FactoryScene")
			{
				PostVolumeHandler PVH = GameObject.FindObjectOfType<PostVolumeHandler>();
				PVH.m_lightGammaGain.gain.value = new Vector4(1, 1, 1, LightGain.Value);
				PVH.m_lightGammaGain.gamma.value = new Vector4(1, 1, 1, LightGain.Value);
			}
		}

		[HarmonyPatch(typeof(SettingsHandler), MethodType.Constructor)]
		public class SettingsSetup
		{

			public static void Postfix(SettingsHandler __instance)
			{
				Setting lightgain = new LightGainSetting();
				__instance.settings.Add(lightgain);
				lightgain.Load(__instance._settingsSaveLoad);
				lightgain.ApplyValue();
			}

		}

		[HarmonyPatch(typeof(LightToggler), "Check")]
		public class DarkPatch
		{

			public static void Postfix(LightToggler __instance)
			{
				try
				{
					if (__instance.light.transform.parent.name.Contains("Mesh") == false)
					{
						__instance.light.enabled = false;
						__instance.light.transform.parent.gameObject.SetActive(false);
					}
				}
				catch
				{
					__instance.light.enabled = false;
				}
			}

		}

		[HarmonyPatch(typeof(Level), "SetupFinished")]
		public class LightsAndGamma
		{
			public static void Postfix()
			{
				if (SceneManager.GetActiveScene().name == "HarborScene" || SceneManager.GetActiveScene().name == "FactoryScene")
				{
					ApplyLightGain();

					//Removes factory lights from factory
					FindObjectsOfType<Transform>()
					.Where(obj => obj.name.Contains("Factory_Light"))
					.ToList()
					.ForEach(obj => obj.gameObject.SetActive(false));
				}
				else
				{
					PostVolumeHandler PVH = GameObject.FindObjectOfType<PostVolumeHandler>();
					PVH.m_lightGammaGain.gain.value = new Vector4(1, 1, 1, 0);
					PVH.m_lightGammaGain.gamma.value = new Vector4(1, 1, 1, 0);
				}
			}

		}

		public class LightGainSetting : FloatSetting, IExposedSetting
		{
			public override void ApplyValue()
			{
				Debug.LogError("Applying light gain value!");
				LightGain.Value = this.Value;
				ApplyLightGain();
			}

			protected override float GetDefaultValue()
			{
				return -0.3f;
			}

			protected override float2 GetMinMaxValue()
			{
				return new float2(-1f, 1f);
			}

			public SettingCategory GetSettingCategory()
			{
				return SettingCategory.Graphics;
			}

			public string GetDisplayName()
			{
				return "Light Gain";
			}
		}


	}
}
