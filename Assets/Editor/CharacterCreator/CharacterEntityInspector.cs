using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Game.CharacterCreator
{
    [CustomEditor(typeof(CharacterEntity))]
    public class CharacterEntityInspector : Editor
    {

        Color _activeColor = new Color(0.1F, 0.3F, 0.5F);
        Color _disableColor = new Color(0F, 0.1F, 0.3F);
        Color _actionColor = new Color(0F, 1F, 0.4F);
        Color _titleColor = new Color(0.3F, 0.5F, 1F);
        Color _buttonColor = new Color(0F, 0.8F, 0.3F);
        GUIStyle _titleButtonStyle;
        string _thePath;
        CharacterEntity myTarget;
        bool _showResult = false;
        List<string> _fileList = new List<string>();


        private void Awake()
        {
            var script = MonoScript.FromScriptableObject(this);
            _thePath = AssetDatabase.GetAssetPath(script);
            _thePath = _thePath.Replace("CharacterEntityInspector.cs", "");
            myTarget = (CharacterEntity)target;
            myTarget.EditorRootPath = _thePath.Replace("Editor/", "Resources/CharacterCreator/CustomBlueprints/").Replace("Assets", "");
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;
            bool _valueChanged = false;

            _titleButtonStyle = new GUIStyle(GUI.skin.button);
            _titleButtonStyle.alignment = TextAnchor.MiddleLeft;
            Color _backgroundColor = GUI.backgroundColor;
            Texture logoIcon = (Texture)AssetDatabase.LoadAssetAtPath(_thePath + "Logo.png", typeof(Texture));
            Texture warningIcon = (Texture)AssetDatabase.LoadAssetAtPath(_thePath + "warning.png", typeof(Texture));
            GUILayout.BeginHorizontal();
            GUILayout.Box(logoIcon);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _buttonColor;
            if (GUILayout.Button("User Guide", GUILayout.Width(100)))
            {
                Application.OpenURL(Application.dataPath.Replace("Assets", "") + _thePath.Replace("Editor/", "Documentation/UserGuide.pdf"));
            }
            GUI.backgroundColor = _titleColor;
            if (GUILayout.Button("Select Character Data Setting Prefab",GUILayout.Width(250)))
            {
                GameObject _data = Resources.Load<GameObject>("CharacterCreator/Core/CharacterData");
                Selection.activeObject = _data;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("uid:",GUILayout.Width(40));
            if (myTarget.mCharacterBoneControl != null)
            {
                GUI.backgroundColor = Color.black;
                GUILayout.Button(myTarget.uid);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                myTarget.uid = EditorGUILayout.TextField(myTarget.uid);
            }
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = myTarget.uid;
            }
            GUI.backgroundColor = myTarget.mCharacterBoneControl != null?Color.grey:_buttonColor;
            if (GUILayout.Button("Random",GUILayout.Width(60))) {
                if (myTarget.mCharacterBoneControl == null) myTarget.RandomUid();
            }
            GUI.backgroundColor = _backgroundColor;
            EditorGUILayout.EndHorizontal();

            if (myTarget.mCharacterBoneControl != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(warningIcon, GUILayout.Width(17), GUILayout.Height(17));
                GUI.color = Color.yellow;
                GUILayout.Label("uid is locked when preloaded character exists.");
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }


            if (myTarget.uid.Trim()=="") {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(warningIcon, GUILayout.Width(17), GUILayout.Height(17));
                GUI.color = Color.yellow;
                GUILayout.Label("A unique id is required in order to customize this character.");
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            if (myTarget.mCharacterBoneControl == null)
            {
                EditorGUILayout.BeginHorizontal();
                myTarget.LoadFromBlueprint = EditorGUILayout.Toggle(myTarget.LoadFromBlueprint, GUILayout.Width(20));
                GUILayout.Label("Load from preset file.");
                if (myTarget.LoadFromBlueprint)
                {
                    GUI.backgroundColor = _buttonColor;
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        _fileList.Clear();
                        _fileList.AddRange(Directory.GetFiles(_thePath.Replace("Editor", "Resources/CharacterCreator"), "*.bytes", SearchOption.AllDirectories));
                        _showResult = true;
                    }
                    GUI.backgroundColor = _backgroundColor;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (myTarget.LoadFromBlueprint)
            {
                if (myTarget.mCharacterBoneControl == null)
                {
                    EditorGUILayout.BeginHorizontal();
                    myTarget.BlueprintPath = GUILayout.TextArea(myTarget.BlueprintPath);
                    EditorGUILayout.EndHorizontal();

                    if (_showResult)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUI.color = _titleColor;
                        GUILayout.Label("Preset files in <Resources>:", GUILayout.Width(160));
                        GUI.color = Color.white;
                        GUI.backgroundColor = _buttonColor;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            _fileList.Clear();
                            _showResult = false;
                        }
                        GUI.backgroundColor = _backgroundColor;
                        EditorGUILayout.EndHorizontal();
                        for (int i = 0; i < _fileList.Count; i++)
                        {

                            string _path = _fileList[i].Replace(@"\", "/").Replace(_thePath.Replace("Editor/", "Resources/"), "").Replace(".bytes", "");
                            TextAsset bindata = Resources.Load(_path) as TextAsset;
                            BlurPrintType _bluePrintType = (BlurPrintType)bindata.bytes[0];
                            if (_bluePrintType == BlurPrintType.AllAppearance)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUI.backgroundColor = _titleColor;
                                if (GUILayout.Button(_path, _titleButtonStyle))
                                {
                                    myTarget.BlueprintPath = _path;
                                    _fileList.Clear();
                                    _showResult = false;
                                }
                                GUI.backgroundColor = _backgroundColor;
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                }

                EditorGUILayout.BeginHorizontal();
                
                if (myTarget.mCharacterBoneControl != null)
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Clear Preloaded Character", GUILayout.Width(200)))
                    {
                        AssetDatabase.DeleteAsset("Assets/CharacterCreator/UniqueCharacterAssets/" + myTarget.uid);
                        DestroyImmediate(myTarget.mCharacterBoneControl.gameObject);
                        myTarget.mCharacterBoneControl = null;
                        myTarget.GetComponent<Animator>().runtimeAnimatorController = null;
                        myTarget.GetComponent<Animator>().avatar = null;
                        myTarget.mCharacterAppearance = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Unique Assets Path:");
                    EditorGUILayout.EndHorizontal();
                    GUI.color = _buttonColor;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Assets/CharacterCreator/UniqueCharacterAssets/" +myTarget.uid+"/");
                }
                else
                {
                    GUI.backgroundColor = myTarget.uid.Trim() == ""?Color.grey: _titleColor;
                    if (GUILayout.Button("Preload Character", GUILayout.Width(200)))
                    {
                        if (myTarget.uid.Trim() != "") myTarget.LoadFromEditor(myTarget.BlueprintPath);
                    }
                    GUILayout.Box(warningIcon, GUILayout.Width(17),GUILayout.Height(17));
                    GUI.color = Color.yellow;
                    if (myTarget.uid.Trim() == "")
                    {
                        GUILayout.Label("Can not preload character without valid uid!");
                    }
                    else
                    {
                        GUILayout.Label("Read the notes below before click!");
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.color = Color.yellow;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Do not preload this character unless you plan to add unique components to it or your character controller requires reference of the renderer or bone transforms.\n" +
                    "Preload this character into the scene, this will create unique material assets in 'UniqueCharacterAssets' folder.\n" +
                    "The uid and gender of this character can not be changed after preload, but the other customizations are still avaliable.", MessageType.Info,true);
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                myTarget.AutoInitializeWithDefaultLooking = EditorGUILayout.Toggle(myTarget.AutoInitializeWithDefaultLooking, GUILayout.Width(20));
                GUILayout.Label("Auto Initialize with Default Appearance.");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("When [Load from preset file] is unchecked, you will have to manually call:\n" +
                    "Initialize(CharacterAppearance) or Initialize(Sex)\n" +
                    "to Initialize the character.", MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            if (myTarget.mCharacterBoneControl == null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Animation Controller(Male):", GUILayout.Width(200));
                myTarget.MaleController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(myTarget.MaleController, typeof(RuntimeAnimatorController), false, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Animation Controller(Female):", GUILayout.Width(200));
                myTarget.FemaleController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(myTarget.FemaleController, typeof(RuntimeAnimatorController), false, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();
            }

            EditorGUILayout.BeginHorizontal();
            GUI.color = _buttonColor;
            GUILayout.Label("Weapons:", GUILayout.Width(200));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            string[] WeaponTypes = new string[4] { "OneHanded", "TwoHanded", "Dual","Custom" };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Weapon Equip Type:",GUILayout.Width(150));
            EditorGUI.BeginChangeCheck();
            myTarget.WeaponEquipType = EditorGUILayout.Popup(myTarget.WeaponEquipType, WeaponTypes, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck() || myTarget.Weapons == null || myTarget.Weapons.Length < (myTarget.WeaponEquipType == 2 ? 2 : 1)) {
                myTarget.Weapons = new WeaponController[myTarget.WeaponEquipType==2?2:1];
                _valueChanged = true;
            }
            EditorGUILayout.EndHorizontal();

            for (int i=0;i< myTarget.Weapons.Length;i++) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (myTarget.WeaponEquipType < 2)
                {
                    GUILayout.Label(myTarget.WeaponEquipType == 0? "OneHanded Weapon:" : "TwoHanded Weapon:", GUILayout.Width(150));
                }
                else if (myTarget.WeaponEquipType==2) {
                    GUILayout.Label(i == 0 ? "Left Hand Weapon:" : "Right Hand Weapon:", GUILayout.Width(150));
                }
                else
                {
                    GUILayout.Label("Weapon #"+(i).ToString(), GUILayout.Width(150));
                }
                myTarget.Weapons[i] = (WeaponController)EditorGUILayout.ObjectField(myTarget.Weapons[i],typeof(WeaponController),false,GUILayout.Width(200));
                if (myTarget.WeaponEquipType == 3)
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X",GUILayout.Width(20))) {
                        List<WeaponController> _tempList = new List<WeaponController>();
                        _tempList.AddRange(myTarget.Weapons);
                        _tempList.RemoveAt(i);
                        myTarget.Weapons = _tempList.ToArray();
                        _valueChanged = true;
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();


                if (myTarget.Weapons[i]!=null) {

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    GUI.color = _buttonColor;
                    GUILayout.Label("> Type: [" + myTarget.Weapons[i].Type.ToString()+"]   Hold Bone: ["+ myTarget.Weapons[i].Data.HoldParentTransform+ "]   Carry Bone: [" + myTarget.Weapons[i].Data.CarryParentTransform + "]");
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    if ((myTarget.Weapons[i].Type == WeaponType.TwoHanded && myTarget.WeaponEquipType != 1)
                        || (myTarget.Weapons[i].Type != WeaponType.TwoHanded && myTarget.WeaponEquipType == 1)
                        || (myTarget.Weapons[i].Type != WeaponType.LeftHand && myTarget.WeaponEquipType == 2 && i==0)
                        || (myTarget.Weapons[i].Type != WeaponType.RightHand && myTarget.WeaponEquipType == 2 && i == 1)
                        )
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = Color.red;
                        GUILayout.Label("The weapon prefab does not match the required [Type]");
                        GUI.color = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }

                    string _error = "";
                    for (int u=0;u<myTarget.Weapons.Length;u++) {
                        if (u!=i && myTarget.Weapons[u]!=null ) {
                            if (myTarget.Weapons[u].Data.CarryParentTransform == myTarget.Weapons[i].Data.CarryParentTransform) _error = "<Duplicated Carry Bone>";
                            if (myTarget.Weapons[u].Data.HoldParentTransform == myTarget.Weapons[i].Data.HoldParentTransform && _error.Length<25) _error += "  <Duplicated Hold Bone>";
                        }
                    }
                    if (_error!="") {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.color = Color.red;
                        GUILayout.Label(_error);
                        GUI.color = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            if (myTarget.WeaponEquipType == 3)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUI.backgroundColor = _buttonColor;
                if (GUILayout.Button("Add Slot", GUILayout.Width(100)))
                {
                    List<WeaponController> _tempList = new List<WeaponController>();
                    _tempList.AddRange(myTarget.Weapons);
                    _tempList.Add(null);
                    myTarget.Weapons = _tempList.ToArray();
                    _valueChanged = true;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Default State:", GUILayout.Width(150));
            myTarget.DefaultWeaponState = (WeaponState)EditorGUILayout.EnumPopup(myTarget.DefaultWeaponState,GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();


            if ((_valueChanged || GUI.changed) && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(myTarget);
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("How to save this character?  Call:\n" +
                  "SaveByteFileToDisk()\n" +
                  "GetSaveBytes()\n" +
                  "SavePngFileToDisk()\n\n" +
                  "How to load saved data to this character?  Call:\n" +
                   "LoadFromByteFileFromDisk()\n" +
                  "LoadFromBytes()\n" +
                  "LoadFromPngFileFromDisk()", MessageType.Info);
            EditorGUILayout.Separator();
            base.OnInspectorGUI();
        }
    }
}
