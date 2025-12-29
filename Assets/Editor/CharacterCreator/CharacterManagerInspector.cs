using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.CharacterCreator
{
    [CustomEditor(typeof(CharacterManager))]
    public class CharacterManagerInspector : Editor
    {

        Color _activeColor = new Color(0.1F, 0.7F, 1F,1F);
        Color _disableColor = new Color(0.5F, 0.8F, 1F,1F);
        Color _buttonColor = new Color(0F, 0.8F, 0.3F);
        int _selMorph = 0;
        GUIStyle _titleButtonStyle;
        private bool _emotionFold = false;
        private bool _settingFold = false;
        private bool _textureToolFold = false;
        private bool _modelToolFold = false;
        private CharacterDataSetting.CharacterTextureNames selectedTextureType;
        private OutfitSlots selectedSlot;
        private Sex selectedTextureSex;
        private Sex selectedOutfitSex;
        private Texture selectedTexture;
        private GameObject selectedPrefab;
        private CharacterDataSetting DataScript;
        private string _packResourcePath;
        private Texture selectedOutfitTexture;
        private GameObject selectedOutfitFbx;
        private Material selectedOutfitMaterial;
        private string[] OutfitFolderName = new string[7]{
            "Armor",
            "Helmet",
            "Glove",
            "Boot",
            "Armor",
            "Back",
            "Tail"
        };
        private string[] OutfitName = new string[7]{
            "Armor",
            "Helmet",
            "Glove",
            "Boot",
            "Pants",
            "Back",
            "Tail"
        };
        public override void OnInspectorGUI()
        {
            GUI.changed = false;
            bool _valueChanged = false;
            _titleButtonStyle = new GUIStyle(GUI.skin.button);
            _titleButtonStyle.alignment = TextAnchor.MiddleLeft;
            Color _backgroundColor = GUI.backgroundColor;
            var script = MonoScript.FromScriptableObject(this);
            CharacterManager myTarget = (CharacterManager)target;
            DataScript = myTarget.GetComponent<CharacterDataSetting>();

            string _thePath = AssetDatabase.GetAssetPath(script);
            _thePath = _thePath.Replace("CharacterManagerInspector.cs", "");
            _packResourcePath = _thePath.Replace("Editor/", "Resources/");
            Texture logoIcon = (Texture)AssetDatabase.LoadAssetAtPath(_thePath + "Logo.png", typeof(Texture));
            Texture warningIcon = (Texture)AssetDatabase.LoadAssetAtPath(_thePath + "warning.png", typeof(Texture));
            GUILayout.BeginHorizontal();
            GUILayout.Box(logoIcon);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _buttonColor;
            if (GUILayout.Button("User Guide", GUILayout.Width(100)))
            {
                 Application.OpenURL(Application.dataPath.Replace("Assets", "") + _thePath.Replace("Editor/", "Documentation/") + "UserGuide.pdf");
            }
            GUI.backgroundColor = _activeColor;
            if (GUILayout.Button("Character Creator Tool", GUILayout.Width(180)))
            {
                var _window = EditorWindow.GetWindow<CharacterCreatorTool>(false, "Character Creator", true);
                _window.maxSize = new Vector2(1600, 1024);
                _window.minSize = new Vector2(900, 600);
                _window.Show();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _activeColor;
            if (GUILayout.Button("Create Character Prefab", GUILayout.Width(210)))
            {
                var _window = EditorWindow.GetWindow<CharacterCreatorTool>(false, "Character Creator", true);
                _window.maxSize = new Vector2(1600, 1024);
                _window.minSize = new Vector2(900, 600);
                _window.Show();
                _window._npcFold = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _activeColor;
            if (GUILayout.Button("Outfit List (Preview)", GUILayout.Width(210)))
            {
                var _window = EditorWindow.GetWindow<CharacterCreatorTool>(false, "Character Creator", true);
                _window.maxSize = new Vector2(1600, 1024);
                _window.minSize = new Vector2(900, 600);
                _window.Show();
                _window._outfitList = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _activeColor;
            if (GUILayout.Button("Convert Saved Data", GUILayout.Width(210)))
            {
                var _window = EditorWindow.GetWindow<CharacterCreatorTool>(false, "Character Creator", true);
                _window.maxSize = new Vector2(1600, 1024);
                _window.minSize = new Vector2(900, 600);
                _window.Show();
                _window._dataFold = true;
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            GUI.color = _buttonColor;
            GUILayout.Label("Player's Blueprint Path in the [Character Customization] Interface:");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            myTarget.useRelativePathToGameInstallFolder = GUILayout.Toggle(myTarget.useRelativePathToGameInstallFolder, "Use the relative path of [Game Install Folder]");
            GUILayout.EndHorizontal();

            if (myTarget.useRelativePathToGameInstallFolder)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Relative Path:", GUILayout.Width(100));
                myTarget.CharacterBlueprintRootPath = GUILayout.TextArea(myTarget.CharacterBlueprintRootPath);
                GUILayout.EndHorizontal();
                GUI.color = _activeColor;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Editor: Assets/"+ myTarget.CharacterBlueprintRootPath+"/");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Build: [Game Install Folder]/" + myTarget.CharacterBlueprintRootPath + "/");
                GUILayout.EndHorizontal();
                GUI.color = Color.white;
            }
            else{
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Absolute Path:", GUILayout.Width(100));
                myTarget.CharacterBlueprintRootPath = GUILayout.TextArea(myTarget.CharacterBlueprintRootPath);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            myTarget.CacheMeshTexture = GUILayout.Toggle(myTarget.CacheMeshTexture, "Keep loaded meshes and textures in memory.");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("This will use more memory in exchange of significantly reduce the performance cost when loading character.\n" +
                "Call CharacterManager.ClearLoadedAssets() to free the used memory when necessary.", MessageType.Info, true);
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            GUI.color = _emotionFold ? _activeColor : _disableColor;
            _emotionFold = EditorGUILayout.Foldout(_emotionFold, "Character Emotion Settings");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            if (_emotionFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                EditorGUILayout.HelpBox("Use <CharacterEntity>().SetEmotion(string _uid, float _length) to play the emotion, use <CharacterEntity>().StopEmotion() to reset the emotion.", MessageType.Info,true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Emotions List ("+myTarget.EmotionSettings.Count.ToString()+"):",GUILayout.Width(120));
                GUI.backgroundColor = _buttonColor;
                if (GUILayout.Button("Add",GUILayout.Width(70))) {
                    EmotionSetting _newSetting = new EmotionSetting();
                    _newSetting.uid = "NewEmotion" + myTarget.EmotionSettings.Count.ToString();
                    _newSetting.HeadMorphs.Clear();
                    myTarget.EmotionSettings.Add(_newSetting);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

              
                for (int i=0;i<myTarget.EmotionSettings.Count;i++) {

                    for (int u = 0; u < myTarget.EmotionSettings.Count; u++){
                        if (myTarget.EmotionSettings[u].uid == myTarget.EmotionSettings[i].uid && i!=u)
                        {
                            myTarget.EmotionSettings[i].uid += "(Clone)";
                        }
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    myTarget.EmotionSettings[i].fold = EditorGUILayout.Foldout(myTarget.EmotionSettings[i].fold, myTarget.EmotionSettings[i].uid, true);
                    
                    GUILayout.EndHorizontal();

                    if (myTarget.EmotionSettings[i].fold) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.Label("UID:", GUILayout.Width(100));
                        myTarget.EmotionSettings[i].uid = GUILayout.TextField(myTarget.EmotionSettings[i].uid.Replace(" ",""), GUILayout.Width(150));
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("Delete", GUILayout.Width(50)))
                        {
                            myTarget.EmotionSettings.RemoveAt(i);
                            return;
                        }
                        GUI.backgroundColor = Color.white;
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.Label("Blend Shapes:",GUILayout.Width(100));
                        GUI.backgroundColor = _activeColor;
                        _selMorph = EditorGUILayout.Popup(_selMorph, EmotionSetting.MorphName, GUILayout.Width(150));
                        GUI.backgroundColor = _buttonColor;
                        if (GUILayout.Button("Active", GUILayout.Width(50)))
                        {
                            foreach (var obj in myTarget.EmotionSettings[i].HeadMorphs) {
                                if (Mathf.FloorToInt(obj.x) == _selMorph) return;
                            }
                            myTarget.EmotionSettings[i].HeadMorphs.Add(new Vector2( _selMorph,100F));
                        }
                        GUI.backgroundColor = Color.white;
                        GUILayout.EndHorizontal();

                        for (int u = 0; u < myTarget.EmotionSettings[i].HeadMorphs.Count; u++) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(70);
                            GUILayout.Label(EmotionSetting.GetEmotionName(Mathf.FloorToInt(myTarget.EmotionSettings[i].HeadMorphs[u].x)), GUILayout.Width(100));
                            myTarget.EmotionSettings[i].HeadMorphs[u] = new Vector2(myTarget.EmotionSettings[i].HeadMorphs[u].x, GUILayout.HorizontalSlider(myTarget.EmotionSettings[i].HeadMorphs[u].y, 0F, 100F, GUILayout.Width(100)));
                            GUILayout.Label(Mathf.FloorToInt(myTarget.EmotionSettings[i].HeadMorphs[u].y).ToString()+"%",GUILayout.Width(45));
                            GUI.backgroundColor = Color.red;
                            if (GUILayout.Button("X", GUILayout.Width(30)))
                            {
                                myTarget.EmotionSettings[i].HeadMorphs.RemoveAt(u);
                                return;
                            }
                            GUI.backgroundColor = Color.white;
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                
            }


            GUILayout.BeginHorizontal();
            GUI.color = _settingFold ? _activeColor : _disableColor;
            _settingFold = EditorGUILayout.Foldout(_settingFold, "Creation Interface Settings");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (_settingFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("General Setting:");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.RaceSettingVisible = GUILayout.Toggle(myTarget.RaceSettingVisible, "", GUILayout.Width(20));
                GUILayout.Label("Race setting visible.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.BackCategoryVisible = GUILayout.Toggle(myTarget.BackCategoryVisible, "", GUILayout.Width(20));
                GUILayout.Label("Back category visible.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.TailCategoryVisible = GUILayout.Toggle(myTarget.TailCategoryVisible, "", GUILayout.Width(20));
                GUILayout.Label("Tail category visible.");
                GUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("When player customize existing character:");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowChangeNameWhenCustomize = GUILayout.Toggle(myTarget.AllowChangeNameWhenCustomize, "", GUILayout.Width(20));
                GUILayout.Label("Allow change name.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowChangeSexWhenCustomize = GUILayout.Toggle(myTarget.AllowChangeSexWhenCustomize, "", GUILayout.Width(20));
                GUILayout.Label("Allow change sex.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowChangeRaceWhenCustomize = GUILayout.Toggle(myTarget.AllowChangeRaceWhenCustomize, "", GUILayout.Width(20));
                GUILayout.Label("Allow change race.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowOutfitsWhenCustomize = GUILayout.Toggle(myTarget.AllowOutfitsWhenCustomize, "", GUILayout.Width(20));
                GUILayout.Label("Allow change outfit.");
                GUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("When player create new character:");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowOutfitsWhenCreate = GUILayout.Toggle(myTarget.AllowOutfitsWhenCreate, "", GUILayout.Width(20));
                GUILayout.Label("Allow change outfit.");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                myTarget.AllowChangeSexWhenCreate = GUILayout.Toggle(myTarget.AllowChangeSexWhenCreate, "", GUILayout.Width(20));
                GUILayout.Label("Allow change sex.");
                GUILayout.EndHorizontal();
                EditorGUILayout.Separator();
            }
            if ((_valueChanged || GUI.changed) && !Application.isPlaying) EditorUtility.SetDirty(myTarget);

            if (DataScript == null) return;

         

            GUILayout.BeginHorizontal();
            GUI.color = _textureToolFold ? _activeColor : _disableColor;
            _textureToolFold = EditorGUILayout.Foldout(_textureToolFold, "Add new body customizations");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            if (_textureToolFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Type:", GUILayout.Width(60));
                selectedTextureType = (CharacterDataSetting.CharacterTextureNames)EditorGUILayout.EnumPopup(selectedTextureType, GUILayout.Width(150));

                GUI.backgroundColor = _buttonColor;
                if (GUILayout.Button("Help", GUILayout.Width(50)))
                {
                    if (selectedTextureType == CharacterDataSetting.CharacterTextureNames.HairID || selectedTextureType == CharacterDataSetting.CharacterTextureNames.BeardID)
                    {
                        Application.OpenURL(Application.dataPath.Replace("Assets", "") + _thePath.Replace("Editor/", "Documentation/") + "HowToCreateHeadAccessories.pdf");
                    }
                    else
                    {
                        Application.OpenURL(Application.dataPath.Replace("Assets", "") + _thePath.Replace("Editor/", "Documentation/") + "HowToCreateCustomizationTextures.pdf");
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                if (selectedTextureType == CharacterDataSetting.CharacterTextureNames.HairID || selectedTextureType == CharacterDataSetting.CharacterTextureNames.BeardID
                     || selectedTextureType == CharacterDataSetting.CharacterTextureNames.Back || selectedTextureType == CharacterDataSetting.CharacterTextureNames.Tail)
                {
                    PrefabTool(selectedTextureType);
                }
                else if (selectedTextureType != CharacterDataSetting.CharacterTextureNames.None)
                {
                    TextureTool(selectedTextureType);
                }
                EditorGUILayout.Separator();
            }


            GUILayout.BeginHorizontal();
            GUI.color = _modelToolFold ? _activeColor : _disableColor;
            _modelToolFold = EditorGUILayout.Foldout(_modelToolFold, "Add new outfits");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            if (_modelToolFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Type:", GUILayout.Width(60));
                string[] _slotPop = new string[5] { "Armor", "Helmet", "Gauntlet", "Boot", "Pants" };
                selectedSlot = (OutfitSlots)EditorGUILayout.Popup((int)selectedSlot, _slotPop, GUILayout.Width(150));
                GUI.backgroundColor = _buttonColor;
                if (GUILayout.Button("Help", GUILayout.Width(50)))
                {
                    Application.OpenURL(Application.dataPath.Replace("Assets", "") + _thePath.Replace("Editor/", "Documentation/") + "HowToCreateOutfits.pdf");
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Add [" + selectedSlot.ToString() + "] mesh for ", GUILayout.Width(145));
                selectedOutfitSex = (Sex)EditorGUILayout.EnumPopup(selectedOutfitSex, GUILayout.Width(65));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("FBX:", GUILayout.Width(60));
                selectedOutfitFbx = (GameObject)EditorGUILayout.ObjectField(selectedOutfitFbx, typeof(GameObject), false, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Material:", GUILayout.Width(60));
                selectedOutfitMaterial = (Material)EditorGUILayout.ObjectField(selectedOutfitMaterial, typeof(Material), false, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Thumb:", GUILayout.Width(60));
                selectedOutfitTexture = (Texture)EditorGUILayout.ObjectField(selectedOutfitTexture, typeof(Texture), false, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                bool _go = (selectedOutfitFbx != null  && selectedOutfitFbx.GetComponentInChildren<SkinnedMeshRenderer>(true) && selectedOutfitMaterial != null && selectedOutfitTexture != null);
                GUI.color = _go ? Color.white : Color.gray;
                GUI.backgroundColor = _go ? _buttonColor : Color.gray;
                if (GUILayout.Button("Add", GUILayout.Width(210)))
                {
                    if (_go)
                    {
                        int _newId = GetOutfitSetting().Length;
                        string _meshPath = _packResourcePath + "CharacterCreator/Player/" +
                            OutfitFolderName[(int)selectedSlot] + "/" + selectedOutfitSex.ToString() + OutfitName[(int)selectedSlot] + "_" + _newId.ToString()+".mesh";
                        string _matPath = _packResourcePath + "CharacterCreator/Player/" +
                            OutfitFolderName[(int)selectedSlot] + "/" + selectedOutfitSex.ToString() + OutfitFolderName[(int)selectedSlot] + "Mat_" + _newId.ToString() + ".mat";
                        string _thumbPath = _packResourcePath.Replace("Resources/", "Textures/Icons/") + selectedOutfitSex.ToString()+ selectedSlot.ToString()+"/"+
                          selectedOutfitSex.ToString() + selectedSlot.ToString() + _newId.ToString() + System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(selectedOutfitTexture));
                        try
                        {
                            Mesh _mesh = selectedOutfitFbx.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                            Mesh _newMesh = Object.Instantiate(_mesh) as Mesh;
                            AssetDatabase.CreateAsset(_newMesh, _meshPath);
                            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(selectedOutfitMaterial), _matPath);
                            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(selectedOutfitTexture), _thumbPath);
                            AssetDatabase.Refresh();
                            OutfitInfo _newInfo = new OutfitInfo();
                            _newInfo.DisplayName = selectedOutfitSex.ToString() + " " + OutfitName[(int)selectedSlot] + " " + _newId.ToString();
                            _newInfo.Icon = (Texture)AssetDatabase.LoadAssetAtPath(_thumbPath,typeof(Texture));
                            _newInfo.Slot = selectedSlot;
                            _newInfo.MeshPath = "CharacterCreator/Player/" +
                            OutfitFolderName[(int)selectedSlot] + "/" + selectedOutfitSex.ToString() + OutfitName[(int)selectedSlot] + "_" + _newId.ToString();
                            _newInfo.MaterialPath = "CharacterCreator/Player/" +
                            OutfitFolderName[(int)selectedSlot] + "/" + selectedOutfitSex.ToString() + OutfitFolderName[(int)selectedSlot] + "Mat_" + _newId.ToString();
                            _newInfo.ColorSetting = new OutfitColorSetting();
                            if (selectedOutfitMaterial.shader.name.Contains("CharacterSkin"))
                            {
                                if (selectedOutfitMaterial.GetTexture("_MaskMap1") != null)
                                {
                                    _newInfo.ColorSetting.ToggleCustomColor1 = true;
                                    _newInfo.ColorSetting.DefaultColor1 = selectedOutfitMaterial.GetColor("_Color1");
                                }
                                if (selectedOutfitMaterial.GetTexture("_MaskMap2") != null)
                                {
                                    _newInfo.ColorSetting.ToggleCustomColor2 = true;
                                    _newInfo.ColorSetting.DefaultColor2 = selectedOutfitMaterial.GetColor("_Color2");
                                }
                                if (selectedOutfitMaterial.GetTexture("_MaskMap3") != null)
                                {
                                    _newInfo.ColorSetting.ToggleCustomColor3 = true;
                                    _newInfo.ColorSetting.DefaultColor3 = selectedOutfitMaterial.GetColor("_Color3");
                                }
                            }
                            _newInfo.BoneNames = new string[selectedOutfitFbx.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length];
                            for (int i=0;i< _newInfo.BoneNames.Length;i++) {
                                _newInfo.BoneNames[i] = selectedOutfitFbx.GetComponentInChildren<SkinnedMeshRenderer>(true).bones[i].name;
                            }
                            List<OutfitInfo> _infoList = new List<OutfitInfo>();
                            _infoList.AddRange(GetOutfitSetting());
                            _infoList.Add(_newInfo);
                            switch (selectedSlot)
                            {
                                default:
                                    if(selectedOutfitSex == Sex.Male )
                                        DataScript.MaleArmorSetting= _infoList.ToArray();
                                    else
                                        DataScript.FemaleArmorSetting = _infoList.ToArray();
                                    break;
                                case OutfitSlots.Helmet:
                                    if (selectedOutfitSex == Sex.Male)
                                        DataScript.MaleHelmetSetting = _infoList.ToArray();
                                    else
                                        DataScript.FemaleHelmetSetting = _infoList.ToArray();
                                    break;
                                case OutfitSlots.Boot:
                                    if (selectedOutfitSex == Sex.Male)
                                        DataScript.MaleBootSetting = _infoList.ToArray();
                                    else
                                        DataScript.FemaleBootSetting = _infoList.ToArray();
                                    break;
                                case OutfitSlots.Gauntlet:
                                    if (selectedOutfitSex == Sex.Male)
                                        DataScript.MaleGloveSetting = _infoList.ToArray();
                                    else
                                        DataScript.FemaleGloveSetting = _infoList.ToArray();
                                    break;
                                case OutfitSlots.Pants:
                                    if (selectedOutfitSex == Sex.Male)
                                        DataScript.MalePantsSetting = _infoList.ToArray();
                                    else
                                        DataScript.FemalePantsSetting = _infoList.ToArray();
                                    break;
                            }


                            EditorUtility.DisplayDialog("Add new [" + selectedSlot.ToString() + "] mesh", "Successfully added (" + _newInfo.DisplayName + ") " , "OK");
                            EditorUtility.SetDirty(DataScript);
                            selectedOutfitFbx = null;
                            selectedOutfitMaterial = null;
                            selectedOutfitTexture = null;
                        }
                        catch
                        {
                            EditorUtility.DisplayDialog("Add new [" + selectedSlot.ToString()+ "] mesh", "Failed to add (" + selectedOutfitFbx.name + ")", "OK");
                        }

                    }
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Separator();
            }
        }

       
        

        private OutfitInfo[] GetOutfitSetting()
        {
            switch (selectedSlot)
            {
                default:
                    return selectedOutfitSex == Sex.Male ? DataScript.MaleArmorSetting : DataScript.FemaleArmorSetting;
                case OutfitSlots.Helmet:
                    return selectedOutfitSex == Sex.Male ? DataScript.MaleHelmetSetting : DataScript.FemaleHelmetSetting;
                case OutfitSlots.Boot:
                    return selectedOutfitSex == Sex.Male ? DataScript.MaleBootSetting : DataScript.FemaleBootSetting;
                case OutfitSlots.Gauntlet:
                    return selectedOutfitSex == Sex.Male ? DataScript.MaleGloveSetting : DataScript.FemaleGloveSetting;
                case OutfitSlots.Pants:
                    return selectedOutfitSex == Sex.Male ? DataScript.MalePantsSetting : DataScript.FemalePantsSetting;
            }
        }

        private void PrefabTool(CharacterDataSetting.CharacterTextureNames _type)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Add [" + _type.ToString().Replace("ID","") + "] prefab for ", GUILayout.Width(145));
            selectedTextureSex = (Sex)EditorGUILayout.EnumPopup(selectedTextureSex, GUILayout.Width(65));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Prefab:", GUILayout.Width(60));
            selectedPrefab = (GameObject)EditorGUILayout.ObjectField(selectedPrefab, typeof(GameObject), false, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Thumb:", GUILayout.Width(60));
            selectedTexture = (Texture)EditorGUILayout.ObjectField(selectedTexture, typeof(Texture), false, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUI.color = (selectedPrefab != null && selectedTexture!=null) ? Color.white : Color.gray;
            GUI.backgroundColor = (selectedPrefab != null && selectedTexture != null) ? _buttonColor : Color.gray;
            if (GUILayout.Button("Add", GUILayout.Width(210)))
            {
                if (selectedPrefab != null && selectedTexture != null)
                {
                    (selectedTextureSex == Sex.Male ? DataScript.MaleAccessorySettings : DataScript.FemaleAccessorySettings)[(int)_type - 8].MaxValue += 1;
                    int _newId = (selectedTextureSex == Sex.Male ? DataScript.MaleAccessorySettings : DataScript.FemaleAccessorySettings)[(int)_type - 8].MaxValue;
                    string _basePath = (selectedTextureSex == Sex.Male ? DataScript.MaleAccessorySettings : DataScript.FemaleAccessorySettings)[(int)_type - 8].PrefabPath;
                    string _thumbPath = (selectedTextureSex == Sex.Male ? DataScript.MaleAccessorySettings : DataScript.FemaleAccessorySettings)[(int)_type - 8].ThumbPath;
                    string _sourcePath = AssetDatabase.GetAssetPath(selectedPrefab);
                    string _targetPath = _packResourcePath + _basePath + _newId.ToString("00") +  System.IO.Path.GetExtension(_sourcePath);

                    string _sourceThumb = AssetDatabase.GetAssetPath(selectedTexture);
                    string _targetThumb = _packResourcePath + _thumbPath + _newId.ToString() +  System.IO.Path.GetExtension(_sourceThumb);

                    if (AssetDatabase.CopyAsset(_sourcePath, _targetPath) && AssetDatabase.CopyAsset(_sourceThumb, _targetThumb))
                    {
                        EditorUtility.DisplayDialog("Add new [" + _type.ToString().Replace("ID", "") + "] prefab", "Successfully added ("+ _type.ToString().Replace("ID", "") + _newId.ToString("00") + ") to " + selectedTextureSex.ToString() + " prefabs.", "OK");
                        EditorUtility.SetDirty(DataScript);
                        selectedPrefab = null;
                        selectedTexture = null;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Add new [" + _type.ToString().Replace("ID", "") + "] prefab", "Failed to add (" + selectedPrefab.name + ")", "OK");
                    }
                }

            }
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.HelpBox("Don't forget Apply this prefab after added textures.", MessageType.Info);
            GUILayout.EndHorizontal();


        }

        private void TextureTool(CharacterDataSetting.CharacterTextureNames _type)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Add ["+ _type.ToString()+"] texture for ",GUILayout.Width(145));
            selectedTextureSex = (Sex)EditorGUILayout.EnumPopup(selectedTextureSex, GUILayout.Width(65));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Texture:", GUILayout.Width(60));
            selectedTexture = (Texture)EditorGUILayout.ObjectField(selectedTexture, typeof(Texture), false, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUI.color= selectedTexture != null ? Color.white : Color.gray;
            GUI.backgroundColor = selectedTexture != null ? _buttonColor : Color.gray;
            if (GUILayout.Button("Add", GUILayout.Width(210)))
            {
                if (selectedTexture != null) {
                    (selectedTextureSex == Sex.Male ? DataScript.MaleMaterialSettings : DataScript.FemaleMaterialSettings)[(int)_type + 8].TextureIdMax += 1;
                    int _newId = (selectedTextureSex == Sex.Male ? DataScript.MaleMaterialSettings : DataScript.FemaleMaterialSettings)[(int)_type + 8].TextureIdMax;
                    string _basePath = (selectedTextureSex == Sex.Male ? DataScript.MaleMaterialSettings : DataScript.FemaleMaterialSettings)[(int)_type + 8].TextureBaseName;
                    string _sourcePath = AssetDatabase.GetAssetPath(selectedTexture);
                    string _targetPath = _packResourcePath + _basePath + _newId.ToString("00")+System.IO.Path.GetExtension(_sourcePath);
 
                    if (AssetDatabase.CopyAsset(_sourcePath, _targetPath))
                    {
                        EditorUtility.DisplayDialog("Add new [" + _type.ToString() + "] texture", "Successfully added (" + _type.ToString()+ _newId.ToString("00") + ") to " + selectedTextureSex.ToString() + " textures.", "OK");
                        EditorUtility.SetDirty(DataScript);
                        selectedTexture = null;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Add new [" + _type.ToString() + "] texture", "Failed to add (" + selectedTexture.name+")", "OK");
                    }
                }
            }
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.HelpBox("Don't forget Apply this prefab after added textures.", MessageType.Info);
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

        }

    }
}
