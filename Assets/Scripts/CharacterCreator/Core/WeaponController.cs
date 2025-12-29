using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this script to an empty GameObject, then put your weapon model as a child of this GameObject and assign it to "Weapon Model" slot.
/// </summary>
namespace Game.CharacterCreator
{
    [System.Serializable]
    public class WeaponPositionData
    {
        public string CarryParentTransform;
        public string HoldParentTransform;
        public bool VisibleWhenCarry = true;
        public Vector3 [] Pos=new Vector3[4];
        public Vector3 [] Rot = new Vector3[4];
        public Vector3 [] Scale = new Vector3[4] { new Vector3(1F,1F,1F), new Vector3(1F, 1F, 1F) , new Vector3(1F, 1F, 1F) , new Vector3(1F, 1F, 1F) };
    }

    public class WeaponController : MonoBehaviour
    {
        public string uid;
        public WeaponType Type;
        public Transform SheathModel;
        public Transform WeaponModel;
        public WeaponPositionData Data=new WeaponPositionData();
        public bool PositionMode = false;
        public GameObject PreviewModel;
        public Vector3 OriginalPos;
        public Quaternion OriginalRot;
        public Vector3 OriginalScale;
        public Transform OriginalParent;
        public Sex PreviewSex = Sex.Male;
        public bool PreviewHold = false;
        public bool PreviewCarry = false;
        private GameObject SheathModelClone;


        void Awake()
        {
            if (PreviewModel != null) Destroy(PreviewModel);
            PositionMode = false;
        }

       

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && PositionMode && PreviewModel!=null)
            {
                if (PreviewHold)
                {
                    Transform _targetBone = FindChildTransform(PreviewModel.transform, Data.HoldParentTransform);
                    if (_targetBone != null)
                    {
                        Gizmos.color = new Color(0F,1F,0F,0.2F);
                        Gizmos.DrawSphere(_targetBone.position,0.1F);
                    }
                }

                if (PreviewCarry)
                {
                    Transform _targetBone = FindChildTransform(PreviewModel.transform, Data.CarryParentTransform);
                    if (_targetBone != null)
                    {
                        Gizmos.color = new Color(0F, 1F, 0F, 0.2F);
                        Gizmos.DrawSphere(_targetBone.position, 0.1F);
                    }
                }
            }
        }

        public void RandomUid()//Assigns a random unique ID for this character.
        {
            List<byte> _bytes = new List<byte>();
            for (int i = 0; i < Random.Range(6, 8); i++)
            {
                _bytes.Add((byte)Random.Range(63, 123));
            }
            uid = System.Text.Encoding.ASCII.GetString(_bytes.ToArray());
        }

        public void SetSheath(bool _hold, CharacterEntity _entity)
        {
            if (SheathModel != null)
            {
                SheathModel.gameObject.SetActive(!_hold);
                if (_hold && _entity.GetBoneByName(Data.CarryParentTransform)!=null)
                {
                    if (SheathModelClone == null)
                    {
                        SheathModelClone = new GameObject(gameObject.name+ "_SheathClone");
                        GameObject _newObj = Instantiate(SheathModel.gameObject, SheathModelClone.transform);
                        _newObj.transform.localPosition = SheathModel.localPosition;
                        _newObj.transform.localRotation = SheathModel.localRotation;
                        _newObj.transform.localScale = SheathModel.localScale;
                        SheathModelClone.transform.SetParent(_entity.GetBoneByName(Data.CarryParentTransform));
                        int _id = ((int)_entity.mCharacterAppearance._Sex) * 2 + 1;
                        SheathModelClone.transform.localPosition = Data.Pos[_id];
                        SheathModelClone.transform.localEulerAngles = Data.Rot[_id];
                        SheathModelClone.transform.localScale = Data.Scale[_id];
                        _newObj.SetActive(true);
                    }
                }
                if (SheathModelClone != null) SheathModelClone.SetActive(_hold);
            }
        }

        public void Unequip() {
            if (SheathModelClone != null) Destroy(SheathModelClone);
            Destroy(gameObject);
        }


        private Transform FindChildTransform(Transform _trans, string _name)
        {
            if (_name == "") return null;
            foreach (var obj in _trans.GetComponentsInChildren<Transform>(true))
            {
                if (obj.name == _name) return obj;
            }
            return null;
        }
    }
}
