using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace Game.CharacterCreator
{
    public class CharacterCreatorTool : EditorWindow
    {
        private int _id = 0;
        private int _boneNameId = 0;
        private OutfitSlots _boneNameSlot;
        private Sex _boneNameSex;
        private CharacterDataSetting _dataScript;
        public bool _npcFold = false;
        public bool _dataFold = false;
        public bool _outfitList = false;
        private bool _maleFold = false;
        private bool _femaleFold = false;
        private bool[] _maleSlotFold=new bool[1];
        private bool[] _femaleSlotFold = new bool[1];
        private Vector2 _scroll;
        private TextAsset ByteText;
        private string _characterPath;
        private string _characterAssetPath="MyCharacter";
        private string _characterName = "NewCharacter";
        private bool _characterWithCC = false;
        private GameObject _characterPrefab;
        private string _generatedPrefabName="";
 
        Color _titleColor = new Color(0.3F, 0.5F, 1F);
        Color _buttonColor = new Color(0F, 0.8F, 0.3F);
        


        private Transform FindAllChild(Transform _mother, string _name)
        {
            Transform result = null;
            Transform[] allMyChild = _mother.GetComponentsInChildren<Transform>(true);
            foreach (Transform obj in allMyChild)
            {
                if (obj.name == _name)
                {
                    result = obj.transform;
                }
            }
            return result;
        }

        void GetDataScript()
        {
            GameObject _data = Resources.Load<GameObject>("CharacterCreator/Core/CharacterData");
            _dataScript = _data.GetComponent<CharacterDataSetting>();
        }

        private void LoadSkinnedMesh(Transform _root, SkinnedMeshRenderer _renderer, string _meshPath, string _matPath, string[] boneNames)
        {
            Dictionary<string, Transform> BoneDictionary = new Dictionary<string, Transform>();
            BoneDictionary.Clear();
            foreach (Transform trans in _root.GetComponentsInChildren<Transform>())
            {
                if (!BoneDictionary.ContainsKey(trans.name))
                {
                    BoneDictionary.Add(trans.name, trans);
                }
            }

            if (_meshPath.Length > 0 && _matPath.Length > 0)
            {
                _renderer.enabled = false;
                _renderer.sharedMesh = Resources.Load<Mesh>(_meshPath);
                _renderer.material = Resources.Load<Material>(_matPath);
                Transform[] bones = new Transform[boneNames.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    bones[i] = BoneDictionary[boneNames[i]];
                }
                _renderer.bones = bones;
                _renderer.enabled = true;
                Bounds _bound = _renderer.localBounds;
                _bound.center = Vector3.zero;
                _bound.extents = new Vector3(0.8F, 1.8F, 0.8F);
                _renderer.localBounds = _bound;
            }
            else
            {
                _renderer.enabled = false;
            }
        }

        private void DevFunctions()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Bones", GUILayout.Width(150))) {
                List<Transform> _bones = new List<Transform>();
                foreach (Transform trans in Selection.activeGameObject.GetComponentsInChildren<Transform>())
                {
                    if (trans.tag == "Bone")
                    {
                        _bones.Add(trans);
                    }
                }
                Selection.activeGameObject.GetComponent<CharacterBoneControl>().Bones = _bones.ToArray();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Bone Map", GUILayout.Width(150)))
            {
                SkinnedMeshRenderer _target = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
                Dictionary<string, Transform> Bones = new Dictionary<string, Transform>();
                foreach (var obj in Selection.activeGameObject.transform.parent.GetComponentsInChildren<Transform>())
                {
                    if (!Bones.ContainsKey(obj.name)) Bones.Add(obj.name, obj);
                }
                Transform[] _newBones = new Transform[_target.bones.Length];
                for (int i = 0; i < _newBones.Length; i++)
                {
                    if (Bones.ContainsKey(_target.bones[i].name))
                    {
                        _newBones[i] = Bones[_target.bones[i].name];
                    }
                    else
                    {
                        Debug.LogError("Could not find bone ["+_target.bones[i].name+"] !");
                        return;
                    }
                }
                _target.bones = _newBones;
                _target.rootBone = Bones[_target.rootBone.name];
                Debug.Log(Selection.activeGameObject.name+" > Bone map copied!");
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            DataObj = (CharacterDataSetting)EditorGUILayout.ObjectField(DataObj, typeof(CharacterDataSetting), true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _boneNameSex = (Sex)EditorGUILayout.EnumPopup(_boneNameSex, GUILayout.Width(70));
            _boneNameSlot = (OutfitSlots)EditorGUILayout.EnumPopup(_boneNameSlot, GUILayout.Width(80));
            GUILayout.Label("ID:", GUILayout.Width(30));
            _boneNameId = EditorGUILayout.IntField(_boneNameId, GUILayout.Width(50));
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Setup Bone Names"))
            {
                SkinnedMeshRenderer _renderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
                string[] _boneNames = new string[_renderer.bones.Length];
                for (int u = 0; u < _boneNames.Length; u++)
                {
                    _boneNames[u] = _renderer.bones[u].name;
                }
                if (_boneNameSex == Sex.Male)
                {
                    switch (_boneNameSlot)
                    {
                        case OutfitSlots.Armor:
                            DataObj.MaleArmorSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Pants:
                            DataObj.MalePantsSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Gauntlet:
                            DataObj.MaleGloveSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Boot:
                            DataObj.MaleBootSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                    }
                }
                else
                {
                    switch (_boneNameSlot)
                    {
                        case OutfitSlots.Armor:
                            DataObj.FemaleArmorSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Pants:
                            DataObj.FemalePantsSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Gauntlet:
                            DataObj.FemaleGloveSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                        case OutfitSlots.Boot:
                            DataObj.FemaleBootSetting[_boneNameId].BoneNames = _boneNames;
                            break;
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();
        }

        CharacterDataSetting DataObj;
        void OnGUI()
        {
  
            GUIStyle box = GUI.skin.button;
            GUIStyle header = new GUIStyle();
            header.fontSize = 20;
            header.alignment = TextAnchor.MiddleCenter;

            if (_dataScript == null)GetDataScript();

            if (_maleSlotFold.Length != System.Enum.GetValues(typeof(OutfitSlots)).Length)
            {
                _maleSlotFold = new bool[System.Enum.GetValues(typeof(OutfitSlots)).Length];
                for (int i=0;i<_maleSlotFold.Length;i++) _maleSlotFold[i] = false;
            }

            if (_femaleSlotFold.Length != System.Enum.GetValues(typeof(OutfitSlots)).Length)
            {
                _femaleSlotFold = new bool[System.Enum.GetValues(typeof(OutfitSlots)).Length];
                for (int i = 0; i < _femaleSlotFold.Length; i++) _femaleSlotFold[i] = false;
            }

            _scroll=GUILayout.BeginScrollView(_scroll);


            //DevFunctions(); //For package developer
            //GUILayout.BeginHorizontal();
            //if (GUILayout.Button("Assign Body Normal"))
            //{
            //    string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/CharacterCreator" });
            //    foreach (string guid2 in guids)
            //    {
            //        string _path = AssetDatabase.GUIDToAssetPath(guid2);
            //        Material _mat = (Material)AssetDatabase.LoadAssetAtPath(_path, typeof(Material));
            //        if (_mat.shader.name.Contains("CharacterSkin"))
            //        {
            //            if (_mat.GetTexture("_NormalMap") != null && _mat.GetTexture("_BodyNormal") == null)
            //            {
            //                Debug.Log(_path);
            //                _mat.SetTexture("_BodyNormal", _mat.GetTexture("_NormalMap"));
            //            }
            //        }
            //    }
            //}
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _titleColor;
            if (GUILayout.Button("Select Character Data Setting Prefab"))
            {
                Selection.activeObject = _dataScript.gameObject;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            GUI.color = _buttonColor;
            _npcFold = EditorGUILayout.Foldout(_npcFold, "[Create Character Prefab]");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            if (_npcFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("Generate a character prefab by selecting a preset file (*.bytes) generated from the character customization interface in <Developer> mode," +
                    " this will be useful if you only want to directly create characters without using the customization feature in game.", MessageType.Info,true);
                GUILayout.EndHorizontal();

                if (_generatedPrefabName == "")
                {

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUI.color = _titleColor;
                    GUILayout.Label("Select a preset file (*.bytes):");
                    GUI.color = Color.white;
                    GUI.backgroundColor = _buttonColor;
                    if (GUILayout.Button("Select", GUILayout.Width(80)))
                    {
                        string _folderPath = File.Exists(_characterPath) ? Path.GetDirectoryName(_characterPath).Replace(@"\", "/") : Application.dataPath;
                        string _selPath = EditorUtility.OpenFilePanelWithFilters("Select a preset file (*.bytes)", _folderPath, new string[2] { "Preset file (*.bytes)", "bytes" });
                        if (!string.IsNullOrEmpty(_selPath))
                        {
                            _characterPath = _selPath;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    _characterPath = GUILayout.TextArea(_characterPath);
                    GUILayout.EndHorizontal();

                    if (File.Exists(_characterPath))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = _buttonColor;
                        GUILayout.Box("[" + Path.GetFileName(_characterPath) + "]", GUILayout.Width(300));
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();

                        EditorGUILayout.Separator();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = _titleColor;
                        _characterWithCC = GUILayout.Toggle(_characterWithCC, "Prefab with character customization feature.");
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();

                        EditorGUILayout.Separator();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = _titleColor;
                        GUILayout.Label("Materials Path (Unique materials of this character will be generated in this folder):");
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUILayout.Label("Assets/CharacterCreator/UniqueCharacterAssets/", GUILayout.Width(390));
                        _characterAssetPath = GUILayout.TextField(_characterAssetPath, GUILayout.Width(100));
                        GUILayout.Label("/", GUILayout.Width(30));
                        GUILayout.EndHorizontal();

                        string _matPath = Application.dataPath + "/CharacterCreator/UniqueCharacterAssets/" + _characterAssetPath;
                        bool _goodToGo = true;
                        if (string.IsNullOrEmpty(_characterAssetPath))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUI.color = Color.red;
                            EditorGUILayout.HelpBox("[Materials Path] can not be empty, please input the name of the folder.", MessageType.Warning, true);
                            GUI.color = Color.white;
                            GUILayout.EndHorizontal();
                            _goodToGo = false;
                        }
                        else if (Directory.Exists(_matPath))
                        {
                            if (Directory.GetFiles(_matPath).Length > 0)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUI.color = Color.red;
                                EditorGUILayout.HelpBox("The [Materials Path] you selected is not empty, please pick another one.", MessageType.Warning, true);
                                GUI.color = Color.white;
                                GUILayout.EndHorizontal();
                                _goodToGo = false;
                            }
                        }

                        EditorGUILayout.Separator();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = _titleColor;
                        GUILayout.Label("Prefab Name:", GUILayout.Width(90));
                        GUI.color = Color.white;
                        _characterName = GUILayout.TextField(_characterName, GUILayout.Width(200));
                        GUILayout.EndHorizontal();
                        if (string.IsNullOrEmpty(_characterName))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUI.color = Color.red;
                            EditorGUILayout.HelpBox("[Prefab Name] can not be empty, please input the name of the prefab.", MessageType.Warning, true);
                            GUI.color = Color.white;
                            GUILayout.EndHorizontal();
                            _goodToGo = false;
                        }


                        EditorGUILayout.Separator();

                        if (_goodToGo)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUI.backgroundColor = _buttonColor;
                            if (GUILayout.Button("Generate Character Prefab"))
                            {
                                GameObject _newEntityObj = new GameObject();
                                CharacterEntity _entity = _newEntityObj.AddComponent<CharacterEntity>();
                                Animator _ani = _newEntityObj.AddComponent<Animator>();
                                _ani.applyRootMotion = true;
                                _entity.uid = _characterAssetPath;
                                _entity.LoadFromBlueprint = true;
                                CharacterAppearance _appearance = CharacterManager.LoadCharacterDataFromFile(null, _characterPath);
                                _entity.Initialize(_appearance);
                                _characterPrefab = _entity.mCharacterBoneControl.gameObject;
                                _characterPrefab.name = _characterName;
                                Animator cAni = _characterPrefab.AddComponent<Animator>();
                                cAni.avatar = _ani.avatar;
                                cAni.applyRootMotion = true;
                                if (!_characterWithCC)
                                {
                                    CharacterBoneControlLite _boneLite = _characterPrefab.AddComponent<CharacterBoneControlLite>();
                                    Dictionary<string, Transform> _boneDictionary = new Dictionary<string, Transform>();
                                    foreach (Transform trans in _entity.mCharacterBoneControl.Bones)
                                    {
                                        if (trans.tag == "Bone")
                                        {
                                            _boneDictionary.Add(trans.name, trans);
                                        }
                                    }
                                    foreach (CharacterBoneDeformSetting obj in _appearance._CharacterData.Sex == 0 ? _dataScript.MaleBoneSettings : _dataScript.FemaleBoneSettings)
                                    {
                                        if (_boneDictionary.ContainsKey(obj.BoneNmae))
                                        {
                                            _boneLite.AddData(_boneDictionary[obj.BoneNmae], obj);
                                        }
                                    }
                                    DestroyImmediate(_entity.mCharacterBoneControl);
                                }
                                _characterPrefab.transform.SetParent(null);
                                _characterPrefab.transform.position = Vector3.zero;
                                _characterPrefab.transform.localEulerAngles = Vector3.zero;
                                bool _prefabCreated= PrefabUtility.SaveAsPrefabAsset(_characterPrefab, "Assets/CharacterCreator/UniqueCharacterAssets/" + _characterAssetPath+"/"+_characterName+".prefab");
                                DestroyImmediate(_newEntityObj);
                                _generatedPrefabName = _characterName + ".prefab";
                                if (_prefabCreated) DestroyImmediate(_characterPrefab);
                                GameObject _createdObj = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/UniqueCharacterAssets/" + _characterAssetPath + "/" + _generatedPrefabName, typeof(GameObject));
                                Selection.activeObject = _createdObj;
                                EditorGUIUtility.PingObject(_createdObj);
                            }
                            GUI.backgroundColor = Color.white;
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            EditorGUILayout.HelpBox("When creating the character prefab, unique materials will be generated into the [Materials Path] folder, the prefab itself will be " +
                                "placed in the Hierarchy.", MessageType.Info, true);
                            GUILayout.EndHorizontal();
                        }

                        EditorGUILayout.Separator();

                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = Color.red;
                        EditorGUILayout.HelpBox("Invalid preset file path.", MessageType.Warning, true);
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUI.color = _buttonColor;
                    EditorGUILayout.HelpBox("Successfully generated the character prefab!", MessageType.Info, true);
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUI.backgroundColor = _titleColor;
                    if (GUILayout.Button("Select", GUILayout.Width(100)))
                    {
                        GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/UniqueCharacterAssets/" + _characterAssetPath+"/"+ _generatedPrefabName, typeof(GameObject));
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.Label(" Generated Prefab: [" + _generatedPrefabName + "]");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUI.backgroundColor = _titleColor;
                    if (GUILayout.Button("Select", GUILayout.Width(100)))
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/UniqueCharacterAssets/"+_characterAssetPath, typeof(UnityEngine.Object));
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.Label(" Materials folder: Assets/CharacterCreator/UniqueCharacterAssets/" + _characterAssetPath);
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUI.backgroundColor = _buttonColor;
                    if (GUILayout.Button("Done"))
                    {
                        _generatedPrefabName = "";
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            _outfitList = EditorGUILayout.Foldout(_outfitList, "[Outfits List]");
            GUILayout.EndHorizontal();

            if (_outfitList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                _maleFold = EditorGUILayout.Foldout(_maleFold, "Male");
                GUILayout.EndHorizontal();
                if (_maleFold)
                {
                    for (int i = 0; i < _maleSlotFold.Length; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(40);
                        _maleSlotFold[i] = EditorGUILayout.Foldout(_maleSlotFold[i], ((OutfitSlots)i).ToString());
                        GUILayout.EndHorizontal();
                        if (_maleSlotFold[i])
                        {
                            OutfitInfo[] _infos = _dataScript.GetOutfitSettings(Sex.Male, (OutfitSlots)i);
                            for (int u = 0; u < _infos.Length; u++)
                            {
                                OutfitInfo _info = _infos[u];
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(60);
                                GUILayout.Box(_info.Icon, new GUILayoutOption[2] { GUILayout.Width(32), GUILayout.Height(32) });
                                GUILayout.Label(_info.DisplayName, new GUILayoutOption[2] { GUILayout.Width(150), GUILayout.Height(32) });
                                GUI.backgroundColor = _buttonColor;
                                if (GUILayout.Button("Preview", new GUILayoutOption[2] { GUILayout.Width(70), GUILayout.Height(32) }))
                                {
                                    if (MeshPreview.instance != null) MeshPreview.instance.Close();
                                    if (i == 5 || i == 6)
                                    {
                                        MeshPreview.ShowPreview("", "", "Assets/CharacterCreator/Resources/" + _info.MeshPath + ".prefab");
                                    }
                                    else
                                    {
                                        string _path = AssetDatabase.GetAssetPath(_dataScript.gameObject).Replace("CharacterData", "PreviewModel");
                                        MeshPreview.ShowPreview(_info.MeshPath, _info.MaterialPath, _path);
                                    }
                                }
                                GUI.backgroundColor = Color.white;
                                GUILayout.EndHorizontal();
                            }
                        }

                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                _femaleFold = EditorGUILayout.Foldout(_femaleFold, "Female");
                GUILayout.EndHorizontal();
                if (_femaleFold)
                {
                    for (int i = 0; i < _femaleSlotFold.Length; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(40);
                        _femaleSlotFold[i] = EditorGUILayout.Foldout(_femaleSlotFold[i], ((OutfitSlots)i).ToString());
                        GUILayout.EndHorizontal();
                        if (_femaleSlotFold[i])
                        {
                            OutfitInfo[] _infos = _dataScript.GetOutfitSettings(Sex.Female, (OutfitSlots)i);
                            for (int u = 0; u < _infos.Length; u++)
                            {
                                OutfitInfo _info = _infos[u];
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(60);
                                GUILayout.Box(_info.Icon, new GUILayoutOption[2] { GUILayout.Width(32), GUILayout.Height(32) });
                                GUILayout.Label(_info.DisplayName, new GUILayoutOption[2] { GUILayout.Width(150), GUILayout.Height(32) });
                                GUI.backgroundColor = _buttonColor;
                                if (GUILayout.Button("Preview", new GUILayoutOption[2] { GUILayout.Width(70), GUILayout.Height(32) }))
                                {
                                    if (MeshPreview.instance != null) MeshPreview.instance.Close();
                                    if (i == 5 || i == 6)
                                    {
                                        MeshPreview.ShowPreview("", "", "Assets/CharacterCreator/Resources/" + _info.MeshPath + ".prefab");
                                    }
                                    else
                                    {
                                        string _path = AssetDatabase.GetAssetPath(_dataScript.gameObject).Replace("CharacterData", "PreviewModel");
                                        MeshPreview.ShowPreview(_info.MeshPath, _info.MaterialPath, _path);
                                    }
                                }
                                GUI.backgroundColor = Color.white;
                                GUILayout.EndHorizontal();
                            }
                        }

                    }
                }
            }

           



           EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            _dataFold = EditorGUILayout.Foldout(_dataFold, "[v1.5+ Save Data Update Tool]");
            GUILayout.EndHorizontal();

            if (_dataFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("[Convert old data to v1.5+]", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("Select the old save:", GUILayout.Width(120));
                ByteText = (TextAsset)EditorGUILayout.ObjectField(ByteText, typeof(TextAsset), false, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUI.backgroundColor = ByteText!=null? new Color(0.2F, 0.5F, 1F, 1F):Color.gray;
                if (GUILayout.Button("Convert Save Data from (v1.0~v1.2) to v1.5+", GUILayout.Width(270)))
                {
                    try
                    {
                        string _path = AssetDatabase.GetAssetPath(ByteText);
                        _path = _path.Substring(6, _path.Length - 6);
                        BlurPrintType _type = (BlurPrintType)ByteText.bytes[0];
                        CharacterAppearance _data = new CharacterAppearance(ByteText.bytes, CharacterAppearance.DataVersions.Before_v1_3);
                        File.WriteAllBytes(Application.dataPath + _path, _data.ToBytes(_type));
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Save Data","Successfully converted the save data.","OK");
                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Save Data", "Invalid save data", "OK");
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUI.backgroundColor = ByteText != null ? new Color(0.2F, 0.5F, 1F, 1F) : Color.gray;
                if (GUILayout.Button("Convert Save Data from (v1.3~v.1.4) to v1.5+", GUILayout.Width(270)))
                {
                    try
                    {
                        string _path = AssetDatabase.GetAssetPath(ByteText);
                        _path = _path.Substring(6, _path.Length - 6);
                        BlurPrintType _type = (BlurPrintType)ByteText.bytes[0];
                        CharacterAppearance _data = new CharacterAppearance(ByteText.bytes, CharacterAppearance.DataVersions.Before_v1_5);
                        File.WriteAllBytes(Application.dataPath + _path, _data.ToBytes(_type));
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Save Data", "Successfully converted the save data.", "OK");
                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Save Data", "Invalid save data", "OK");
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("DO NOT use this for save data from v1.5+", MessageType.Warning);
                GUILayout.EndHorizontal();
            }

           

            GUILayout.EndScrollView();
        }


       

    }
}
