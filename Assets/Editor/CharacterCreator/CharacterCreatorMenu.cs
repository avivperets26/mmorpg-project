using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Game.CharacterCreator
{
	public class CharacterCreatorMenu : ScriptableWizard
	{

		[MenuItem("Window/Character Creator/Tools")]

		public static void CreateWizard()
		{
		    var _window=	EditorWindow.GetWindow<CharacterCreatorTool>(false, "Character Creator", true);
			_window.maxSize = new Vector2(1600,1024);
			_window.minSize = new Vector2(900,600);
			_window.Show();
		}

	}
}
