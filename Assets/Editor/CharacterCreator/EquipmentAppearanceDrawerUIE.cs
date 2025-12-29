using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace Game.CharacterCreator
{
    [CustomPropertyDrawer(typeof(EquipmentAppearance))]
    public class EquipmentAppearanceDrawerUIE : PropertyDrawer
    {

        private CharacterDataSetting _data;
        private GUIContent[] _maleMeshList = new GUIContent[1];
        private GUIContent[] _femaleMeshList = new GUIContent[1];
        private OutfitSlots _oldSlot;
        private void GetDataScript()
        {
            GameObject _dataObj = Resources.Load<GameObject>("CharacterCreator/Core/CharacterData");
            _data = _dataObj.GetComponent<CharacterDataSetting>();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
  
            EditorGUI.BeginProperty(position, label, property);
            label.text += " :";
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var _pos = position;
            
            EditorGUI.PropertyField(new Rect(_pos.x, _pos.y, 100, 20), property.FindPropertyRelative("Type"), GUIContent.none);
            _pos.x += 130;
            property.FindPropertyRelative("uiFold").boolValue = EditorGUI.Foldout(new Rect(_pos.x, _pos.y, 50, 20), property.FindPropertyRelative("uiFold").boolValue, "Settings");
            if (property.FindPropertyRelative("uiFold").boolValue)
            {
                if (_data == null) GetDataScript();
                OutfitSlots _slot = (OutfitSlots)property.FindPropertyRelative("Type").enumValueIndex;
                OutfitInfo[] _maleInfos = _data.GetOutfitSettings(Sex.Male, _slot);
                OutfitInfo[] _femaleInfos = _data.GetOutfitSettings(Sex.Female, _slot);

                if (_maleMeshList.Length != _maleInfos.Length || _oldSlot != _slot || _femaleMeshList.Length != _femaleInfos.Length)
                {
                    _maleMeshList = new GUIContent[_maleInfos.Length];
                    for (int i = 0; i < _maleMeshList.Length; i++) _maleMeshList[i] =new GUIContent(" "+_maleInfos[i].DisplayName, _maleInfos[i].Icon, "Model ID: " + i.ToString());

                    _femaleMeshList = new GUIContent[_femaleInfos.Length];
                    for (int i = 0; i < _femaleMeshList.Length; i++) _femaleMeshList[i] = new GUIContent(" " + _femaleInfos[i].DisplayName, _femaleInfos[i].Icon, "Model ID: " + i.ToString());

                    _oldSlot = _slot;
                }

                _pos.y += 20;
                _pos.x = position.x;
                GUI.color = new Color(0F,1F,0F);
                EditorGUI.LabelField(new Rect(_pos.x, _pos.y, 70, 20), "Model ID:");
                GUI.color = Color.white;
                _pos.y += 20;
                _pos.x = position.x;

                EditorGUI.LabelField(new Rect(_pos.x, _pos.y, 35, 20), "Male");
                _pos.x += 50;
                property.FindPropertyRelative("MaleMeshId").intValue= EditorGUI.Popup(new Rect(_pos.x, _pos.y, 150, 18), property.FindPropertyRelative("MaleMeshId").intValue, _maleMeshList);
                _pos.x += 160;
                if (EditorGUI.LinkButton(new Rect(_pos.x, _pos.y, 30, 18),"View")) {
                    if (MeshPreview.instance != null) MeshPreview.instance.Close();
                    if (_data!=null)
                    {
                        int _id = property.FindPropertyRelative("MaleMeshId").intValue;
                        if (_slot == OutfitSlots.Back || _slot == OutfitSlots.Tail)
                        {
                            if (_id > 0) MeshPreview.ShowPreview("", "", "Assets/CharacterCreator/Resources/" + _maleInfos[_id].MeshPath + ".prefab");
                        }
                        else
                        {
                            string _path = AssetDatabase.GetAssetPath(_data.gameObject).Replace("CharacterData", "PreviewModel");
                            MeshPreview.ShowPreview(_maleInfos[_id].MeshPath, _maleInfos[_id].MaterialPath, _path);
                        }
                    }
                }

                _pos.y += 20;
                _pos.x = position.x;
                EditorGUI.LabelField(new Rect(_pos.x, _pos.y, 50, 20), "Female");
                _pos.x += 50;
                property.FindPropertyRelative("FemaleMeshId").intValue = EditorGUI.Popup(new Rect(_pos.x, _pos.y, 150, 18), property.FindPropertyRelative("FemaleMeshId").intValue, _femaleMeshList);
                _pos.x += 160;
                if (EditorGUI.LinkButton(new Rect(_pos.x, _pos.y, 30, 18), "View"))
                {
                    if (MeshPreview.instance != null) MeshPreview.instance.Close();
                    if (_data != null)
                    {
                        int _id = property.FindPropertyRelative("FemaleMeshId").intValue;
                        if (_slot == OutfitSlots.Back || _slot == OutfitSlots.Tail)
                        {
                            if(_id>0) MeshPreview.ShowPreview("", "", "Assets/CharacterCreator/Resources/" + _femaleInfos[_id].MeshPath + ".prefab");
                        }
                        else
                        {
                            string _path = AssetDatabase.GetAssetPath(_data.gameObject).Replace("CharacterData", "PreviewModel");
                            MeshPreview.ShowPreview(_femaleInfos[_id].MeshPath, _femaleInfos[_id].MaterialPath, _path);
                        }
                    }
                }

                _pos.x = position.x;
                _pos.y += 30;
                EditorGUI.PropertyField(new Rect(_pos.x, _pos.y, 20, 20), property.FindPropertyRelative("UseCustomColor"), GUIContent.none);
                _pos.x += 20;
                GUI.color = new Color(0F, 1F, 0F);
                EditorGUI.LabelField(new Rect(_pos.x, _pos.y, 90, 20), "Custom color:");
                GUI.color = Color.white;
                _pos.x += 90;
                if (property.FindPropertyRelative("UseCustomColor").boolValue)
                {
                    EditorGUI.PropertyField(new Rect(_pos.x, _pos.y, 40, 20), property.FindPropertyRelative("CustomColor1"), GUIContent.none);
                    _pos.x += 40;
                    EditorGUI.PropertyField(new Rect(_pos.x, _pos.y, 40, 20), property.FindPropertyRelative("CustomColor2"), GUIContent.none);
                    _pos.x += 40;
                    EditorGUI.PropertyField(new Rect(_pos.x, _pos.y, 40, 20), property.FindPropertyRelative("CustomColor3"), GUIContent.none);
                    _pos.x += 40;
                }
            }
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.FindPropertyRelative("uiFold").boolValue ? 120:20;
        }
    }
}
