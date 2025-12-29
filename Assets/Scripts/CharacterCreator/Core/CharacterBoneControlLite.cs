using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.CharacterCreator
{
    [System.Serializable]
    public class BoneData
    {
        public Transform Bone;
        public bool SetPos = false;
        public bool SetRot = false;
        public bool SetScale = false;
        public Vector3 Pos;
        public Vector3 Rot;
        public Vector3 Scale;
    }
    public class CharacterBoneControlLite : MonoBehaviour
    {
        public List<BoneData> BoneDatas = new List<BoneData>();

        public void AddData(Transform _Bone, CharacterBoneDeformSetting _data)
        {
            BoneData _newData = new BoneData();
            _newData.Bone = _Bone;
            _newData.SetPos = _data.SetPos;
            if (_data.SetPos) _newData.Pos = _Bone.localPosition;
            _newData.SetRot = _data.SetRot;
            if (_data.SetRot) _newData.Rot = _Bone.localEulerAngles;
            _newData.SetScale = _data.SetScale;
            if (_data.SetScale) _newData.Scale = _Bone.localScale;
            BoneDatas.Add(_newData);
        }

        void LateUpdate()
        {
            foreach (var obj in BoneDatas) {
                if (obj.SetPos) obj.Bone.localPosition = obj.Pos;
                if (obj.SetRot) obj.Bone.localEulerAngles = obj.Rot;
                if (obj.SetScale) obj.Bone.localScale = obj.Scale;
            }
        }
    }
}
