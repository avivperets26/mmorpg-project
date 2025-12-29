using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace Game.CharacterCreator
{
    public class Utils
    {
        private static readonly string CharacterCreatorDefineSymbol = "CHARACTER_CREATOR";
		private static bool Initialized = false;

		public static void SetDefineSymbolOnBuildTargetGroup(BuildTargetGroup targetGroup)
		{
#if UNITY_2021_2_OR_NEWER
			var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
			string currData = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
			string currData = PlayerSettings.GetScriptingDefineSymbolsForGroup( targetGroup );
#endif
			if (!currData.Contains(CharacterCreatorDefineSymbol))
			{
				if (string.IsNullOrEmpty(currData))
				{
#if UNITY_2021_2_OR_NEWER
					PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, CharacterCreatorDefineSymbol);
#else
					PlayerSettings.SetScriptingDefineSymbolsForGroup( targetGroup, CharacterCreatorDefineSymbol );
#endif
				}
				else
				{
					if (!currData[currData.Length - 1].Equals(';'))
					{
						currData += ';';
					}
					currData += CharacterCreatorDefineSymbol;

#if UNITY_2021_2_OR_NEWER
					PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currData);
#else
					PlayerSettings.SetScriptingDefineSymbolsForGroup( targetGroup, currData );
#endif
				}
			}
		}


		[InitializeOnLoadMethod]
		public static void Init()
		{
			if (!Initialized)
			{
				SetDefineSymbolOnBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
				Initialized = true;
			}
		}

	}
}
