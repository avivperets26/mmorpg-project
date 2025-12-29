using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.CharacterCreator
{
    [CustomEditor(typeof(WeaponController))]
    public class WeaponControllerInspector : Editor
    {
        Color _activeColor = new Color(0.1F, 0.3F, 0.5F);
        Color _disableColor = new Color(0F, 0.1F, 0.3F);
        Color _actionColor = new Color(0F, 1F, 0.4F);
        Color _titleColor = new Color(0.3F, 0.5F, 1F);
        Color _buttonColor = new Color(0F, 0.8F, 0.3F);
        GUIStyle _titleButtonStyle;
        string _thePath;
        WeaponController myTarget;
        List<string> CarryBoneNames = new List<string>(){
            "Custom",
            "CarryPointHipL",
            "CarryPointHipR",
            "CarryPointHipBack",
            "CarryPointThighL",
            "CarryPointThighR",
            "CarryPointBack",
            "CarryPointArmL",
            "CarryPointArmR"
        };
        List<string> HoldBoneNames = new List<string>(){
            "Custom",
            "WeaponBoneL",
            "WeaponBoneR",
            "Bip001 L UpperArm",
            "Bip001 L Forearm",
            "Bip001 R UpperArm",
            "Bip001 R Forearm"
        };

        List<string> HoldBoneLNames = new List<string>(){
            "Custom",
            "WeaponBoneL",
        };

        List<string> HoldBoneRNames = new List<string>(){
            "Custom",
            "WeaponBoneR",
        };

        List<string> HoldBoneTwoHandedNames = new List<string>(){
            "Custom",
            "WeaponBoneL",
            "WeaponBoneR"
        };




        bool SyncScale = true;

        private void Awake()
        {
            var script = MonoScript.FromScriptableObject(this);
            _thePath = AssetDatabase.GetAssetPath(script);
            _thePath = _thePath.Replace("WeaponControllerInspector.cs", "");
            myTarget = (WeaponController)target;
           
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
            EditorGUILayout.HelpBox("Add this script to an empty GameObject,\n" +
                "then put your weapon model as a child of this GameObject,\n" +
                "assign it to <Weapon Model> slot.", MessageType.Info, true);
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("uid:", GUILayout.Width(40));
            myTarget.uid = EditorGUILayout.TextField(myTarget.uid);
            GUI.backgroundColor = _buttonColor;
            if (GUILayout.Button("Random", GUILayout.Width(60)))
            {
                myTarget.RandomUid();
            }
            GUI.backgroundColor = _backgroundColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Weapon Model:",GUILayout.Width(150));
            myTarget.WeaponModel = (Transform)EditorGUILayout.ObjectField(myTarget.WeaponModel, typeof(Transform), true, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            if (myTarget.WeaponModel != null)
            {
                if (myTarget.WeaponModel.IsChildOf(myTarget.transform))
                {
                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    GUI.color = _buttonColor;
                    GUILayout.Label("Type:", GUILayout.Width(150));
                    GUI.color = Color.white;
                    GUI.backgroundColor = _buttonColor;
                    myTarget.Type = (WeaponType)EditorGUILayout.EnumPopup(myTarget.Type, GUILayout.Width(150));
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    GUI.color = _buttonColor;
                    GUILayout.Label("Parent Bone when holding the weapon:");
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = _buttonColor;
                    myTarget.Data.HoldParentTransform = GUILayout.TextField(myTarget.Data.HoldParentTransform, GUILayout.Width(150));
                    int _sel = GetHoldBoneList(myTarget.Type).IndexOf(myTarget.Data.HoldParentTransform);
                    if (_sel < 0) _sel = 0;
                    EditorGUI.BeginChangeCheck();
                    _sel = EditorGUILayout.Popup(_sel, GetHoldBoneList(myTarget.Type).ToArray(), GUILayout.Width(150));
                    if (EditorGUI.EndChangeCheck() && _sel > 0)
                    {
                        myTarget.Data.HoldParentTransform = GetHoldBoneList(myTarget.Type)[_sel];
                        _valueChanged = true;
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    if (myTarget.Data.HoldParentTransform == "")
                    {
                        GUILayout.BeginHorizontal();
                        GUI.color = Color.red;
                        GUILayout.Label("The parent bone name can not be empty!");
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();
                    }


                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    myTarget.Data.VisibleWhenCarry = GUILayout.Toggle(myTarget.Data.VisibleWhenCarry, "Visible when carrying");
                    GUILayout.EndHorizontal();

                    if (myTarget.Data.VisibleWhenCarry)
                    {
                        GUILayout.BeginHorizontal();
                        GUI.color = _buttonColor;
                        GUILayout.Label("Parent Bone when carry the weapon:");
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUI.backgroundColor = _buttonColor;
                        myTarget.Data.CarryParentTransform = GUILayout.TextField(myTarget.Data.CarryParentTransform, GUILayout.Width(150));
                        _sel = CarryBoneNames.IndexOf(myTarget.Data.CarryParentTransform);
                        if (_sel < 0) _sel = 0;
                        EditorGUI.BeginChangeCheck();
                        _sel = EditorGUILayout.Popup(_sel, CarryBoneNames.ToArray(), GUILayout.Width(150));
                        if (EditorGUI.EndChangeCheck() && _sel > 0)
                        {
                            myTarget.Data.CarryParentTransform = CarryBoneNames[_sel];
                            _valueChanged = true;
                        }
                        GUI.backgroundColor = Color.white;
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Sheath Model:", GUILayout.Width(150));
                        myTarget.SheathModel = (Transform)EditorGUILayout.ObjectField(myTarget.SheathModel, typeof(Transform), true, GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        if (myTarget.SheathModel != null && !myTarget.SheathModel.IsChildOf(myTarget.WeaponModel))
                        {
                            GUILayout.BeginHorizontal();
                            GUI.color = Color.red;
                            GUILayout.Label("The <Sheath Model> must be a child of the Weapon Model!");
                            GUI.color = Color.white;
                            GUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.Separator();
                    EditorGUILayout.Separator();

                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = myTarget.PositionMode ? _actionColor : _buttonColor;
                    GUI.color = myTarget.PositionMode ? _actionColor : Color.white * 0.7F;
                    if (GUILayout.Button("Position Mode", GUILayout.Width(100)))
                    {
                        myTarget.PositionMode = !myTarget.PositionMode;
                        EditorGUI.FocusTextInControl(null);
                    }
                    GUI.color = Color.white;
                    GUI.backgroundColor = _activeColor;
                    if (GUILayout.Button("Copy Setting", GUILayout.Width(100)))
                    {
                        string _json = JsonUtility.ToJson(myTarget.Data);
                        TextEditor te = new TextEditor();
                        te.text = _json;
                        te.SelectAll();
                        te.Copy();
                        EditorUtility.DisplayDialog("Copy Setting", "Successfully copied settings to clipboard!", "OK");
                    }
                    if (GUILayout.Button("Paste Setting", GUILayout.Width(100)))
                    {
                        string _json = GUIUtility.systemCopyBuffer;
                        try
                        {
                            WeaponPositionData _data = JsonUtility.FromJson<WeaponPositionData>(_json);
                            myTarget.Data.CarryParentTransform = _data.CarryParentTransform;
                            myTarget.Data.HoldParentTransform = _data.HoldParentTransform;
                            myTarget.Data.VisibleWhenCarry = _data.VisibleWhenCarry;
                            myTarget.Data.Pos = new Vector3[_data.Pos.Length];
                            myTarget.Data.Rot = new Vector3[_data.Rot.Length];
                            myTarget.Data.Scale = new Vector3[_data.Scale.Length];
                            for (int i = 0; i < _data.Pos.Length; i++) myTarget.Data.Pos[i] = _data.Pos[i];
                            for (int i = 0; i < _data.Rot.Length; i++) myTarget.Data.Rot[i] = _data.Rot[i];
                            for (int i = 0; i < _data.Scale.Length; i++) myTarget.Data.Scale[i] = _data.Scale[i];
                            EditorUtility.DisplayDialog("Paste Setting", "Successfully pasted settings from clipboard!", "OK");
                        }
                        catch
                        {
                            EditorUtility.DisplayDialog("Paste Setting", "Invalid data from clipboard!", "OK");
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    if (myTarget.PositionMode)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUI.backgroundColor = myTarget.PreviewSex == Sex.Male ? _titleColor : Color.black;
                        GUI.color = myTarget.PreviewSex == Sex.Male ? Color.white : Color.white * 0.7F;
                        if (GUILayout.Button("Male Positioning", GUILayout.Width(120)))
                        {
                            if (myTarget.PreviewSex != Sex.Male)
                            {
                                myTarget.PreviewSex = Sex.Male;
                                ResetPreview();
                                EditorGUI.FocusTextInControl(null);
                            }
                        }
                        GUI.backgroundColor = myTarget.PreviewSex == Sex.Female ? _titleColor : Color.black;
                        GUI.color = myTarget.PreviewSex == Sex.Female ? Color.white : Color.white * 0.7F;
                        if (GUILayout.Button("Female Positioning", GUILayout.Width(120)))
                        {
                            if (myTarget.PreviewSex != Sex.Female)
                            {
                                myTarget.PreviewSex = Sex.Female;
                                ResetPreview();
                                EditorGUI.FocusTextInControl(null);
                            }
                        }
                        GUI.backgroundColor = Color.white;
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();

                        if (myTarget.Data.Pos.Length < 4) myTarget.Data.Pos = new Vector3[4];
                        if (myTarget.Data.Rot.Length < 4) myTarget.Data.Rot = new Vector3[4];
                        if (myTarget.Data.Scale.Length < 4) myTarget.Data.Scale = new Vector3[4] { new Vector3(1F, 1F, 1F), new Vector3(1F, 1F, 1F), new Vector3(1F, 1F, 1F), new Vector3(1F, 1F, 1F) };

                        EditorGUILayout.Separator();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        if (GUILayout.Button(myTarget.PreviewSex == Sex.Male ? "Copy Male Positioning to Female" : "Copy Female Positioning to Male", GUILayout.Width(240)))
                        {
                            int _baseId = myTarget.PreviewSex == Sex.Male ? 0 : 2;
                            int _targetId = myTarget.PreviewSex == Sex.Male ? 2 : 0;
                            for (int i = 0; i < 2; i++)
                            {
                                myTarget.Data.Pos[_targetId + i] = myTarget.Data.Pos[_baseId + i];
                                myTarget.Data.Rot[_targetId + i] = myTarget.Data.Rot[_baseId + i];
                                myTarget.Data.Scale[_targetId + i] = myTarget.Data.Scale[_baseId + i];
                            }
                            EditorUtility.DisplayDialog("Copy Positioning", "Successfully copied!", "OK");
                        }
                        GUILayout.EndHorizontal();

                        EditorGUILayout.Separator();

                        if (myTarget.PreviewModel == null)
                        {
                            myTarget.PreviewModel = Instantiate(Resources.Load<GameObject>("CharacterCreator/Player/Character" + myTarget.PreviewSex.ToString()), myTarget.transform.position, Quaternion.identity);
                            myTarget.OriginalPos = myTarget.transform.localPosition;
                            myTarget.OriginalRot = myTarget.transform.localRotation;
                            myTarget.OriginalScale = myTarget.transform.localScale;
                            myTarget.OriginalParent = myTarget.transform.parent;
                        }
                        else
                        {

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUILayout.Label("Preview:", GUILayout.Width(60));
                            myTarget.PreviewHold = GUILayout.Toggle(myTarget.PreviewHold, "Holding", GUILayout.Width(70));
                            if (myTarget.PreviewHold)
                            {
                                Transform _targetBone = FindChildTransform(myTarget.PreviewModel.transform, myTarget.Data.HoldParentTransform);
                                if (_targetBone != null)
                                {
                                    if (myTarget.transform.parent != _targetBone)
                                    {
                                        myTarget.transform.SetParent(_targetBone);
                                        EditorGUI.FocusTextInControl(null);
                                    }
                                    myTarget.PreviewCarry = false;
                                }
                                else
                                {
                                    myTarget.PreviewHold = false;
                                    EditorUtility.DisplayDialog("Preview Oositioning", "You must input the Parent Bone name before preview the positioning.", "OK");
                                }
                            }
                            GUILayout.Space(10);
                            myTarget.PreviewCarry = GUILayout.Toggle(myTarget.PreviewCarry, "Carrying", GUILayout.Width(70));
                            if (myTarget.PreviewCarry)
                            {
                                Transform _targetBone = FindChildTransform(myTarget.PreviewModel.transform, myTarget.Data.CarryParentTransform);
                                if (_targetBone != null)
                                {
                                    if (myTarget.transform.parent != _targetBone)
                                    {
                                        myTarget.transform.SetParent(_targetBone);
                                        EditorGUI.FocusTextInControl(null);
                                    }
                                    myTarget.PreviewHold = false;
                                }
                                else
                                {
                                    myTarget.PreviewCarry = false;
                                    EditorUtility.DisplayDialog("Preview Oositioning", "You must input the Parent Bone name before preview the positioning.", "OK");
                                }
                            }
                            GUILayout.EndHorizontal();

                            if (myTarget.PreviewHold || myTarget.PreviewCarry)
                            {
                                int _id = (int)myTarget.PreviewSex * 2 + (myTarget.PreviewCarry ? 1 : 0);

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUI.color = _titleColor;
                                GUILayout.Label("Position:");
                                GUI.color = Color.white;
                                GUI.backgroundColor = _buttonColor;
                                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                                {
                                    myTarget.Data.Pos[_id] = Vector3.zero;
                                }
                                GUI.backgroundColor = Color.white;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("x:", GUILayout.Width(20));
                                myTarget.Data.Pos[_id].x = EditorGUILayout.Slider(myTarget.Data.Pos[_id].x, -1F, 1F);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("y:", GUILayout.Width(20));
                                myTarget.Data.Pos[_id].y = EditorGUILayout.Slider(myTarget.Data.Pos[_id].y, -1F, 1F);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("z:", GUILayout.Width(20));
                                myTarget.Data.Pos[_id].z = EditorGUILayout.Slider(myTarget.Data.Pos[_id].z, -1F, 1F);
                                GUILayout.EndHorizontal();

                                EditorGUILayout.Separator();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUI.color = _titleColor;
                                GUILayout.Label("Rotation:");
                                GUI.color = Color.white;
                                GUI.backgroundColor = _buttonColor;
                                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                                {
                                    myTarget.Data.Rot[_id] = Vector3.zero;
                                }
                                GUI.backgroundColor = Color.white;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("x:", GUILayout.Width(20));
                                myTarget.Data.Rot[_id].x = EditorGUILayout.Slider(myTarget.Data.Rot[_id].x, -180F, 180F);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("y:", GUILayout.Width(20));
                                myTarget.Data.Rot[_id].y = EditorGUILayout.Slider(myTarget.Data.Rot[_id].y, -180F, 180F);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(40);
                                GUILayout.Label("z:", GUILayout.Width(20));
                                myTarget.Data.Rot[_id].z = EditorGUILayout.Slider(myTarget.Data.Rot[_id].z, -180F, 180F);
                                GUILayout.EndHorizontal();

                                EditorGUILayout.Separator();

                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUI.color = _titleColor;
                                GUILayout.Label("Scale:", GUILayout.Width(70F));
                                GUI.color = Color.white;

                                SyncScale = GUILayout.Toggle(SyncScale, "Uniform Scale");

                                GUI.backgroundColor = _buttonColor;
                                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                                {
                                    myTarget.Data.Scale[_id] = Vector3.one;
                                }
                                GUI.backgroundColor = Color.white;

                                GUILayout.EndHorizontal();

                                if (SyncScale)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(40);
                                    myTarget.Data.Scale[_id].x = EditorGUILayout.Slider(myTarget.Data.Scale[_id].x, 0.5F, 1.5F);
                                    myTarget.Data.Scale[_id].y = myTarget.Data.Scale[_id].x;
                                    myTarget.Data.Scale[_id].z = myTarget.Data.Scale[_id].x;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(40);
                                    GUILayout.Label("x:", GUILayout.Width(20));
                                    myTarget.Data.Scale[_id].x = EditorGUILayout.Slider(myTarget.Data.Scale[_id].x, 0.5F, 1.5F);
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(40);
                                    GUILayout.Label("y:", GUILayout.Width(20));
                                    myTarget.Data.Scale[_id].y = EditorGUILayout.Slider(myTarget.Data.Scale[_id].y, 0.5F, 1.5F);
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(40);
                                    GUILayout.Label("z:", GUILayout.Width(20));
                                    myTarget.Data.Scale[_id].z = EditorGUILayout.Slider(myTarget.Data.Scale[_id].z, 0.5F, 1.5F);
                                    GUILayout.EndHorizontal();
                                }
                                myTarget.transform.localPosition = myTarget.Data.Pos[_id];
                                myTarget.transform.localEulerAngles = myTarget.Data.Rot[_id];
                                myTarget.transform.localScale = myTarget.Data.Scale[_id];
                            }

                        }


                    }
                    else
                    {
                        ResetPreview();
                    }




                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = Color.red;
                    GUILayout.Label("The <Weapon Model> must be a child of this transform!");
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                    ResetPreview();
                }
            }
            else
            {
                ResetPreview();
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Select parent bones before entering <Position Mode>.");

            if ((_valueChanged || GUI.changed) && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(myTarget);
        }


        private Transform FindChildTransform(Transform _trans, string _name)
        {
            if (_name == "") return null;
            foreach (var obj in _trans.GetComponentsInChildren<Transform>(true)) {
                if (obj.name == _name) return obj;
            }
            return null;
        }

        private void ResetPreview() {
            if (myTarget.PreviewModel != null)
            {
                myTarget.transform.SetParent(myTarget.OriginalParent);
                myTarget.transform.SetAsLastSibling();
                myTarget.transform.localRotation = myTarget.OriginalRot;
                myTarget.transform.localPosition= myTarget.OriginalPos;
                myTarget.transform.localScale=myTarget.OriginalScale;
                DestroyImmediate(myTarget.PreviewModel);
            }

        }

        private void OnDestroy()
        {
            ResetPreview();
        }

        private void OnDisable()
        {
            myTarget.PositionMode = false;
            ResetPreview();
        }

        private List<string> GetHoldBoneList(WeaponType _type) {
            if (_type == WeaponType.LeftHand)
            {
                return HoldBoneLNames;
            }
            else if (_type == WeaponType.RightHand)
            {
                return HoldBoneRNames;
            }
            else if (_type == WeaponType.TwoHanded)
            {
                return HoldBoneTwoHandedNames;
            }
            else
            {
                return HoldBoneNames;
            }
        }

     

    }
}
