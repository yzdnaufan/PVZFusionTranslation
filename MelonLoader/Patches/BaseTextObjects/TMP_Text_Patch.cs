using HarmonyLib;
using Il2CppTMPro;
using PvZ_Fusion_Translator.AssetStore;

namespace PvZ_Fusion_Translator.Patches.BaseTextObjects
{
	// The OnEnable patches only see text that is already set when an object is enabled.
	// Screens that build their UI in code and fill it in afterwards (e.g. the Gods Almanac)
	// were never translated, so also translate UI text at the moment it is assigned.
	[HarmonyPatch(typeof(TMP_Text))]
	public static class TMP_Text_Patch
	{
		// set_text runs every frame for counters and timers, so each source string is only translated once.
		private static readonly Dictionary<string, string> cache = new();
		private const int MaxCacheSize = 20000;

		internal static void ClearCache()
		{
			cache.Clear();
		}

		[HarmonyPatch(nameof(TMP_Text.text), MethodType.Setter)]
		[HarmonyPrefix]
		private static void SetText(TMP_Text __instance, ref string value)
		{
			if (string.IsNullOrEmpty(value) || !ContainsCJK(value)) return;

			if (!cache.TryGetValue(value, out string translated))
			{
				if (cache.Count >= MaxCacheSize) cache.Clear();
				translated = StringStore.TranslateText(value);
				cache[value] = translated;
			}

			if (translated == value) return;

			TextMeshProUGUI textUI = __instance.TryCast<TextMeshProUGUI>();
			if (textUI == null || IsExcluded(textUI)) return;

			textUI.font = FontStore.LoadTMPFont(Utils.Language.ToString());
			value = translated;
		}

		private static bool ContainsCJK(string text)
		{
			foreach (char c in text)
			{
				if (c >= '一' && c <= '鿿') return true;
			}
			return false;
		}

		// Same exclusions as TextMeshProUGUI_Patch.OnEnable
		private static bool IsExcluded(TextMeshProUGUI textUI)
		{
			var parent = textUI.transform.parent;
			if (parent == null) return false;
			if (parent.name.Contains("DrawCardsText")) return true;
			return parent.parent != null && parent.parent.name.Contains("AbyssBagMenu");
		}
	}
}
