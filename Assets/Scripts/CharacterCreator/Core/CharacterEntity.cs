using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
/// <summary>
/// This script is to manage the characters, you can inherit your character control script from it.
/// </summary>
namespace Game.CharacterCreator
{
    public class CharacterEntity : MonoBehaviour
    {
        #region variables
        [HideInInspector]
        public string uid = "DemoCharacter";
        [HideInInspector]
        public CharacterAppearance mCharacterAppearance;
        [HideInInspector]
        public string EditorRootPath;
        [HideInInspector]
        public bool LoadFromBlueprint = false;
        [HideInInspector]
        public string BlueprintPath = "";
        [HideInInspector]
        public RuntimeAnimatorController MaleController;
        [HideInInspector]
        public RuntimeAnimatorController FemaleController;
        [HideInInspector]
        public bool AutoInitializeWithDefaultLooking = false;
        [HideInInspector]
        public WeaponController[] Weapons;
        [HideInInspector]
        public int WeaponEquipType = 0;
        [HideInInspector]
        public WeaponState DefaultWeaponState = WeaponState.Carry;
        [HideInInspector]
        public CharacterBoneControl mCharacterBoneControl;

        [SerializeField] private string playerPrefabResourceRoot = "CharacterCreator/Player";

        private WeaponState CurrentWeaponState = WeaponState.Hide;

        protected Animator mAnimator;
        public Transform LookAtTarget;
        public bool TogglePlaceHolderGizmo = true;
        public Mesh PlaceHolderMesh;
        public Color PlaceHolderColor = new Color(0F, 0.7F, 1F, 0.5F);
        private Coroutine ResetCo;
        private Dictionary<WeaponType, WeaponController> EquippedWeapons = new Dictionary<WeaponType, WeaponController>();
        #endregion

        #region internal methods

        IEnumerator ResetAnimator(Avatar _avatar)
        {
            yield return 1;
            mAnimator.avatar = _avatar;
        }



        public virtual void Start()
        {
            if (CharacterManager.isCharacterAppearanceExist(this))
            {
                if (mCharacterBoneControl != null && CharacterManager.GetAppearanceData(this).isSameAs(mCharacterAppearance))
                {
                    mCharacterBoneControl.MyData = mCharacterAppearance;
                    LoadDefaultWeapon();
                }
                else
                {
                    Initialize(CharacterManager.GetAppearanceData(this));
                }
            }
            else
            {
                if (mCharacterBoneControl != null)
                {
                    CharacterManager.UpdateCharacterAppearance(uid, mCharacterAppearance);
                    mCharacterBoneControl.MyData = mCharacterAppearance;
                    LoadDefaultWeapon();
                }
                else if (LoadFromBlueprint && BlueprintPath.Trim() != "")
                {
                    Initialize(CharacterManager.LoadCharacterDataFromResources(BlueprintPath, null));
                }
                else if (AutoInitializeWithDefaultLooking)
                {
                    ResetCharacter();
                }
                else
                {
                    mCharacterAppearance = null;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && TogglePlaceHolderGizmo && PlaceHolderMesh!=null && mCharacterBoneControl==null)
            {
                Color _color = Gizmos.color;
                Gizmos.color = PlaceHolderColor;
                Gizmos.DrawMesh(PlaceHolderMesh,0,transform.position,transform.rotation,Vector3.one);
                Gizmos.color = _color;
            }
        }
#endif

        #endregion

        /// <summary>
        /// Returns the gender of this character
        /// </summary>
        public Sex sex
        {
            get
            {
                if (mCharacterAppearance == null) return Sex.Male; // safe default
                return mCharacterAppearance._Sex;
            }
        }


        /// <summary>
        /// Assigns a random unique ID for this character.
        /// </summary>
        public void RandomUid()
        {
            List<byte> _bytes = new List<byte>();
            for (int i = 0; i < Random.Range(10, 15); i++)
            {
                _bytes.Add((byte)Random.Range(63, 123));
            }
            uid = System.Text.Encoding.ASCII.GetString(_bytes.ToArray()).Replace(@"\", "").Replace("/", "").Replace(".", "").Replace("^", "").Replace("`", "").Replace("@", "").Replace("(", "").Replace(")", "").Replace("?", "").Replace("[", "").Replace("]", "").Replace("-", "_");
        }


        #region Save/Load/Initialize
        /// <summary>
        /// Initializes the character with a "*.bytes" file in your editor folder, please use absolute full path and with extension. Example:"E:/NpcPreset_1.bytes"
        /// </summary>
        /// <param name="_editorPath"></param>
        public void LoadFromEditor(string _editorPath)
        {
            if (!Application.isEditor || Application.isPlaying) return;
            Initialize(CharacterManager.LoadCharacterDataFromFile(null, Application.dataPath + "/CharacterCreator/Resources/" + _editorPath + ".bytes"));
        }

        /// <summary>
        /// Initializes the character with a "*.bytes" file in your resources folder, please use relative path without extension from resources folder. Example: "CharacterCreator/CustomBlueprints/Characters/NpcPreset_1"
        /// </summary>
        /// <param name="_resourcePath"></param>
        public void LoadFromResourceFile(string _resourcePath)
        {
            Initialize(CharacterManager.LoadCharacterDataFromResources(_resourcePath, null));
        }

        /// <summary>
        /// Initializes the character with a "*.bytes" file from disk, please use absolute full path and with extension. Example:"E:/NpcPreset_1.bytes"
        /// </summary>
        /// <param name="_absolutePath"></param>
        public void LoadFromByteFileFromDisk(string _absolutePath)
        {
            Initialize(CharacterManager.LoadCharacterDataFromFile(mCharacterAppearance, _absolutePath));
        }

        /// <summary>
        /// Initializes the character with a "*.png" file from disk, please use absolute full path and with extension.Example:"E:/NpcPreset_1.png"
        /// </summary>
        /// <param name="_absolutePath"></param>
        public void LoadFromPngFileFromDisk(string _absolutePath)
        {
            CharacterManager.LoadCharacterDataWithPhoto(ref mCharacterAppearance, _absolutePath);
            Initialize(mCharacterAppearance);
        }

        /// <summary>
        /// Initializes the character with the bytes array loaded from your own save system.
        /// </summary>
        /// <param name="_bytes"></param>
        public void LoadFromBytes(byte[] _bytes)
        {
            if (mCharacterAppearance == null)
                mCharacterAppearance = new CharacterAppearance(CharacterData.Create(_bytes[1]));
            mCharacterAppearance.Load(_bytes);
            Initialize(mCharacterAppearance);
        }

        /// <summary>
        /// Save the character to a "*.bytes" file on disk,  please use absolute full path and with extension. Example:"E:/Player.bytes"
        /// </summary>
        /// <param name="_absolutePath"></param>
        /// <param name="_filter"></param>
        public void SaveByteFileToDisk(string _absolutePath, BlurPrintType _filter)
        {
            CharacterManager.SaveCharacterData(mCharacterBoneControl, _absolutePath, _filter);
        }

        /// <summary>
        /// Save the character to a "*.png" file on disk,  please use absolute full path and with extension. Example:"E:/Player.png"
        /// </summary>
        /// <param name="_absolutePath"></param>
        /// <param name="_photo"></param>
        /// <param name="_filter"></param>
        public void SavePngFileToDisk(string _absolutePath, Texture2D _photo, BlurPrintType _filter)
        {
            CharacterManager.SaveCharacterDataWithPhoto(mCharacterBoneControl, _photo, _absolutePath, _filter);
        }

        /// <summary>
        /// Get the bytes array of the character appearance save data. You can save this with your own save system. 
        /// </summary>
        /// <param name="_filter"></param>
        /// <returns></returns>
        public byte[] GetSaveBytes(BlurPrintType _filter = BlurPrintType.AllAppearance)
        {
            return mCharacterAppearance.ToBytes(_filter);
        }

        /// <summary>
        /// Initializes the character with a specified appearance data.
        /// </summary>
        /// <param name="_data"></param>
        public void Initialize(CharacterAppearance _data)
        {
            if (Application.isPlaying && ResetCo != null) StopCoroutine(ResetCo);
            mAnimator = GetComponent<Animator>();
            mAnimator.avatar = null;
            if (mCharacterBoneControl != null) DestroyImmediate(mCharacterBoneControl.gameObject);
            mCharacterAppearance = _data;
            var prefabPath = $"{playerPrefabResourceRoot}/" + (sex == Sex.Female ? "CharacterFemale" : "CharacterMale");
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError($"CharacterEntity: Missing character prefab at Resources/{prefabPath}. " +
                               $"Make sure the prefab exists under a Resources folder.");
                return;
            }

            mCharacterBoneControl = Instantiate(prefab, transform).GetComponent<CharacterBoneControl>();
            if (!mCharacterBoneControl)
            {
                Debug.LogError($"CharacterEntity: Prefab at Resources/{prefabPath} has no CharacterBoneControl component.");
                return;
            }
            mAnimator.runtimeAnimatorController = sex == Sex.Male ? MaleController : FemaleController;
            Avatar _avatar = mCharacterBoneControl.GetComponent<Animator>().avatar;
            DestroyImmediate(mCharacterBoneControl.GetComponent<Animator>());
            mCharacterBoneControl.transform.localPosition = Vector3.zero;
            mCharacterBoneControl.transform.localEulerAngles = Vector3.zero;
            mCharacterBoneControl.transform.localScale = Vector3.one;
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
            {
                if (!Directory.Exists(Application.dataPath + "/CharacterCreator/UniqueCharacterAssets"))
                    Directory.CreateDirectory(Application.dataPath + "/CharacterCreator/UniqueCharacterAssets");
                if (!Directory.Exists(Application.dataPath + "/CharacterCreator/UniqueCharacterAssets/" + uid))
                    Directory.CreateDirectory(Application.dataPath + "/CharacterCreator/UniqueCharacterAssets/" + uid);
                mCharacterBoneControl.EditorPath = "Assets/CharacterCreator/UniqueCharacterAssets/" + uid;
                mAnimator.avatar = _avatar;
            }
#endif
            mCharacterBoneControl.Initialize(mCharacterAppearance);
            if (Application.isPlaying)
            {
                ResetCo = StartCoroutine(ResetAnimator(_avatar));
                CharacterManager.UpdateCharacterAppearance(uid, mCharacterAppearance);
                LoadDefaultWeapon();
            }
            mCharacterBoneControl.LookAtTarget = LookAtTarget;
        }

        /// <summary>
        /// Initializes the character with the default appearance.
        /// </summary>
        /// <param name="_sex"></param>
        public void Initialize(Sex _sex)
        {
            mAnimator = GetComponent<Animator>();
            mAnimator.avatar = null;
            if (mCharacterBoneControl != null) Destroy(mCharacterBoneControl.gameObject);
            mCharacterAppearance = new CharacterAppearance(CharacterData.Create((byte)_sex));
            var prefabPath = $"{playerPrefabResourceRoot}/" + (sex == Sex.Female ? "CharacterFemale" : "CharacterMale");
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError($"CharacterEntity: Missing character prefab at Resources/{prefabPath}. " +
                               $"Make sure the prefab exists under a Resources folder.");
                return;
            }

            mCharacterBoneControl = Instantiate(prefab, transform).GetComponent<CharacterBoneControl>();
            if (!mCharacterBoneControl)
            {
                Debug.LogError($"CharacterEntity: Prefab at Resources/{prefabPath} has no CharacterBoneControl component.");
                return;
            }
            mAnimator.runtimeAnimatorController = sex == Sex.Male ? MaleController : FemaleController;
            Avatar _avatar = mCharacterBoneControl.GetComponent<Animator>().avatar;
            DestroyImmediate(mCharacterBoneControl.GetComponent<Animator>());
            mCharacterBoneControl.transform.localPosition = Vector3.zero;
            mCharacterBoneControl.transform.localEulerAngles = Vector3.zero;
            mCharacterBoneControl.transform.localScale = Vector3.one;
            mCharacterBoneControl.Initialize(mCharacterAppearance);
            StartCoroutine(ResetAnimator(_avatar));
            CharacterManager.UpdateCharacterAppearance(uid, mCharacterAppearance);
            mCharacterBoneControl.LookAtTarget = LookAtTarget;
            LoadDefaultWeapon();
        }

        #endregion


        #region Weapons

        /// <summary>
        /// Load the weapons set in the inspector.
        /// </summary>
        public void LoadDefaultWeapon()
        {
            UnequipAllWeapons();
            for (int i = 0; i < Weapons.Length; i++)
            {
                if (Weapons[i] != null) EquipWeapon(Weapons[i], DefaultWeaponState);
            }
        }

        /// <summary>
        /// Equip a weapon and set its default state.
        /// </summary>
        /// <param name="_weapon"></param>
        /// <param name="_state"></param>
        public void EquipWeapon(WeaponController _weapon, WeaponState _state = WeaponState.Carry)
        {
            if (mCharacterBoneControl == null)
            {
                Debug.LogError("Trying to load the weapon when the character is not ready.");
                return;
            }
            if (WeaponEquipType == 3)
            {
                if (EquippedWeapons.ContainsKey(_weapon.Type)) UnequipWeapon(_weapon.Type);
            }
            else
            {
                if (_weapon.Type == WeaponType.TwoHanded)
                {
                    if (EquippedWeapons.ContainsKey(WeaponType.LeftHand)) UnequipWeapon(WeaponType.LeftHand);
                    if (EquippedWeapons.ContainsKey(WeaponType.RightHand)) UnequipWeapon(WeaponType.RightHand);
                }
                if ((_weapon.Type == WeaponType.LeftHand || _weapon.Type == WeaponType.RightHand || _weapon.Type == WeaponType.TwoHanded) && EquippedWeapons.ContainsKey(WeaponType.TwoHanded)) UnequipWeapon(WeaponType.TwoHanded);
                if (EquippedWeapons.ContainsKey(_weapon.Type)) UnequipWeapon(_weapon.Type);
            }
            Transform _parent = null;
            string _parentBoneName = "";
            int _stateId = 0;
            int _sexId = (int)mCharacterAppearance._Sex;
            if (_weapon.Data.VisibleWhenCarry && _state == WeaponState.Carry)
            {
                _parentBoneName = _weapon.Data.CarryParentTransform;
                _stateId = 1;
            }
            else
            {
                _parentBoneName = _weapon.Data.HoldParentTransform;
                _stateId = 0;
            }
            if (_parentBoneName != "" && mCharacterBoneControl.BoneDictionary.ContainsKey(_parentBoneName))
            {
                _parent = mCharacterBoneControl.BoneDictionary[_parentBoneName];
            }
            if (_parent != null)
            {
                GameObject _newWeapon = Instantiate(_weapon.gameObject, _parent);
                _newWeapon.transform.localPosition = _weapon.Data.Pos[_sexId * 2 + _stateId];
                _newWeapon.transform.localEulerAngles = _weapon.Data.Rot[_sexId * 2 + _stateId];
                _newWeapon.transform.localScale = _weapon.Data.Scale[_sexId * 2 + _stateId];
                _newWeapon.gameObject.SetActive(_state != WeaponState.Hide);
                _newWeapon.GetComponent<WeaponController>().SetSheath(_state == WeaponState.Hold, this);
                EquippedWeapons.Add(_weapon.Type, _newWeapon.GetComponent<WeaponController>());
                CurrentWeaponState = _state;
            }
            else
            {
                Debug.LogError("Can not find parent bone [" + _parentBoneName + "] for " + _weapon.gameObject.name);
            }
        }

        /// <summary>
        /// Unequip a weapon with specified slot.
        /// </summary>
        /// <param name="_slot"></param>
        public void UnequipWeapon(WeaponType _slot)
        {
            if (EquippedWeapons.ContainsKey(_slot))
            {
                if (EquippedWeapons[_slot] != null) EquippedWeapons[_slot].Unequip();
                EquippedWeapons.Remove(_slot);
            }
        }

        /// <summary>
        /// Unequip all weapons.
        /// </summary>
        public void UnequipAllWeapons()
        {
            foreach (var key in EquippedWeapons.Keys)
            {
                if (EquippedWeapons[key] != null) EquippedWeapons[key].Unequip();
            }
            EquippedWeapons.Clear();
        }

        /// <summary>
        /// Switch the state of the weapon with specified slot.
        /// </summary>
        /// <param name="_state"></param>
        /// <param name="_slot"></param>
        public void SwitchWeaponState(WeaponState _state, WeaponType _slot)
        {
            if (EquippedWeapons.ContainsKey(_slot))
            {
                if (_state == WeaponState.Hide)
                {
                    EquippedWeapons[_slot].gameObject.SetActive(false);
                    EquippedWeapons[_slot].SetSheath(false, this);
                }
                else
                {
                    Transform _parent = null;
                    string _parentBoneName = "";
                    int _stateId = 0;
                    int _sexId = (int)mCharacterAppearance._Sex;
                    if (_state == WeaponState.Carry)
                    {
                        if (!EquippedWeapons[_slot].Data.VisibleWhenCarry)
                        {
                            EquippedWeapons[_slot].gameObject.SetActive(false);
                        }
                        else
                        {
                            _parentBoneName = EquippedWeapons[_slot].Data.CarryParentTransform;
                            _stateId = 1;

                        }
                    }
                    else if (_state == WeaponState.Hold)
                    {
                        _parentBoneName = EquippedWeapons[_slot].Data.HoldParentTransform;
                        _stateId = 0;
                    }

                    if (_parentBoneName != "" && mCharacterBoneControl.BoneDictionary.ContainsKey(_parentBoneName))
                    {
                        _parent = mCharacterBoneControl.BoneDictionary[_parentBoneName];
                    }

                    if (_parent != null)
                    {
                        EquippedWeapons[_slot].transform.SetParent(_parent);
                        EquippedWeapons[_slot].transform.localPosition = EquippedWeapons[_slot].Data.Pos[_sexId * 2 + _stateId];
                        EquippedWeapons[_slot].transform.localEulerAngles = EquippedWeapons[_slot].Data.Rot[_sexId * 2 + _stateId];
                        EquippedWeapons[_slot].transform.localScale = EquippedWeapons[_slot].Data.Scale[_sexId * 2 + _stateId];
                        EquippedWeapons[_slot].gameObject.SetActive(true);
                        EquippedWeapons[_slot].GetComponent<WeaponController>().SetSheath(_state == WeaponState.Hold, this);
                    }
                    else
                    {
                        Debug.LogError("Can not find parent bone [" + _parentBoneName + "] for " + EquippedWeapons[_slot].gameObject.name);
                    }
                }
            }
        }

        /// <summary>
        /// Switch the state of all weapons
        /// </summary>
        /// <param name="_state"></param>
        public void SwitchWeaponState(WeaponState _state)
        {
            foreach (var key in EquippedWeapons.Keys)
            {
                if (EquippedWeapons[key] != null) SwitchWeaponState(_state, key);
            }
            CurrentWeaponState = _state;
        }

        /// <summary>
        /// Get the current weapon state.
        /// </summary>
        /// <returns></returns>
        public WeaponState GetWeaponState()
        {
            return CurrentWeaponState;
        }

        /// <summary>
        /// Return bool value for whether a weapon with specified uid is equipped.
        /// </summary>
        /// <param name="_uid"></param>
        /// <returns></returns>
        public bool isEquippedWeapon(string _uid)
        {
            return GetEquippedWeaponByUid(_uid) != null;
        }

        /// <summary>
        /// Get a equipped weapon with  specified slot.
        /// </summary>
        /// <param name="_slot"></param>
        /// <returns></returns>
        public WeaponController GetEquippedWeaponByType(WeaponType _slot)
        {
            if (EquippedWeapons.ContainsKey(_slot))
            {
                return EquippedWeapons[_slot];
            }
            return null;
        }

        /// <summary>
        /// Get a equipped weapon with specified uid, return null if no match found.
        /// </summary>
        /// <param name="_uid"></param>
        /// <returns></returns>
        public WeaponController GetEquippedWeaponByUid(string _uid)
        {
            foreach (var key in EquippedWeapons.Keys)
            {
                if (EquippedWeapons[key] != null && EquippedWeapons[key].uid == _uid) return EquippedWeapons[key];
            }
            return null;
        }

        /// <summary>
        /// Get a list of all equipped weapon
        /// </summary>
        /// <returns></returns>
        public List<WeaponController> GetAllEquippedWeapon()
        {
            List<WeaponController> _weapons = new List<WeaponController>();
            foreach (var key in EquippedWeapons.Keys)
            {
                _weapons.Add(EquippedWeapons[key]);
            }
            return _weapons;
        }

        #endregion

        ///Bind an <EquipmentAppearance> class to your equipment data, and call this function when the character equips new gear to reflect the change in appearance.
        #region Equipment

        /// <summary>
        /// Equips a gear item and updates the character's appearance accordingly.
        /// </summary>
        /// <param name="_equipment"></param>
        public void Equip(EquipmentAppearance _equipment)
        {
            mCharacterAppearance._OutfitID[(int)_equipment.Type] = (byte)_equipment.GetId(sex);
            if (_equipment.UseCustomColor)
            {
                mCharacterAppearance._CusColor1[(int)_equipment.Type] = Uint8Color.Set(_equipment.CustomColor1);
                mCharacterAppearance._CusColor2[(int)_equipment.Type] = Uint8Color.Set(_equipment.CustomColor2);
                mCharacterAppearance._CusColor3[(int)_equipment.Type] = Uint8Color.Set(_equipment.CustomColor3);
            }
            else if ((int)_equipment.Type < 5)
            {
                OutfitColorSetting _setting = CharacterDataSetting.instance.GetOutfitColorSetting(sex, _equipment.Type, mCharacterAppearance._OutfitID[(int)_equipment.Type]);
                mCharacterAppearance._CusColor1[(int)_equipment.Type] = Uint8Color.Set(_setting.DefaultColor1);
                mCharacterAppearance._CusColor2[(int)_equipment.Type] = Uint8Color.Set(_setting.DefaultColor2);
                mCharacterAppearance._CusColor3[(int)_equipment.Type] = Uint8Color.Set(_setting.DefaultColor3);
            }
            else
            {
                mCharacterAppearance._CusColor1[(int)_equipment.Type] = Uint8Color.Set(Color.white);
                mCharacterAppearance._CusColor2[(int)_equipment.Type] = Uint8Color.Set(Color.white);
                mCharacterAppearance._CusColor3[(int)_equipment.Type] = Uint8Color.Set(Color.white);
            }
        }

        /// <summary>
        /// Equips an item with its slot and id and updates the character's appearance accordingly.
        /// </summary>
        /// <param name="_slot"></param>
        /// <param name="_id"></param>
        public void Equip(OutfitSlots _slot, int _id)
        {
            mCharacterAppearance._OutfitID[(int)_slot] = (byte)_id;
            if ((int)_slot < 5)
            {
                OutfitColorSetting _setting = CharacterDataSetting.instance.GetOutfitColorSetting(sex, _slot, mCharacterAppearance._OutfitID[(int)_slot]);
                mCharacterAppearance._CusColor1[(int)_slot] = Uint8Color.Set(_setting.DefaultColor1);
                mCharacterAppearance._CusColor2[(int)_slot] = Uint8Color.Set(_setting.DefaultColor2);
                mCharacterAppearance._CusColor3[(int)_slot] = Uint8Color.Set(_setting.DefaultColor3);
            }
            else
            {
                mCharacterAppearance._CusColor1[(int)_slot] = Uint8Color.Set(Color.white);
                mCharacterAppearance._CusColor2[(int)_slot] = Uint8Color.Set(Color.white);
                mCharacterAppearance._CusColor3[(int)_slot] = Uint8Color.Set(Color.white);
            }
        }

        /// <summary>
        /// Equips an item with its slot, id and custom colors, then updates the character's appearance accordingly.
        /// </summary>
        /// <param name="_slot"></param>
        /// <param name="_id"></param>
        /// <param name="_color1"></param>
        /// <param name="_color2"></param>
        /// <param name="_color3"></param>
        public void Equip(OutfitSlots _slot, int _id, Color _color1, Color _color2, Color _color3)
        {
            mCharacterAppearance._OutfitID[(int)_slot] = (byte)_id;
            mCharacterAppearance._CusColor1[(int)_slot] = Uint8Color.Set(_color1);
            mCharacterAppearance._CusColor2[(int)_slot] = Uint8Color.Set(_color2);
            mCharacterAppearance._CusColor3[(int)_slot] = Uint8Color.Set(_color3);
        }


        /// <summary>
        /// Unequips a specific outfit slot (e.g., helmet, armor).
        /// </summary>
        /// <param name="_slot"></param>
        public void Unequip(OutfitSlots _slot)
        {
            mCharacterAppearance._OutfitID[(int)_slot] = 0;
            if ((int)_slot < 5)
            {
                OutfitColorSetting _setting = CharacterDataSetting.instance.GetOutfitColorSetting(sex, _slot, 0);
                mCharacterAppearance._CusColor1[(int)_slot] = Uint8Color.Set(_setting.DefaultColor1);
                mCharacterAppearance._CusColor2[(int)_slot] = Uint8Color.Set(_setting.DefaultColor2);
                mCharacterAppearance._CusColor3[(int)_slot] = Uint8Color.Set(_setting.DefaultColor3);
            }
        }

        /// <summary>
        /// Returns whether a specific piece of equipment is equipped.
        /// </summary>
        /// <param name="_equipment"></param>
        /// <returns></returns>
        public bool isEquipped(EquipmentAppearance _equipment)
        {
            return mCharacterAppearance._OutfitID[(int)_equipment.Type] == _equipment.GetId(sex);
        }

        /// <summary>
        /// Returns whether a specific equipment with provided slot and id is equipped.
        /// </summary>
        /// <param name="_slot"></param>
        /// <param name="_id"></param>
        /// <returns></returns>
        public bool isEquipped(OutfitSlots _slot, int _id)
        {
            return mCharacterAppearance._OutfitID[(int)_slot] == _id;
        }

        /// <summary>
        /// Retrieves the mesh ID of the currently equipped item in the specified slot.
        /// </summary>
        /// <param name="_slot"></param>
        /// <returns></returns>
        public int GetEquippedId(OutfitSlots _slot)
        {
            return mCharacterAppearance._OutfitID[(int)_slot];
        }

        #endregion


        #region Character

        /// <summary>
        /// Take a photo of the character and return the photo as Texture2D.
        /// </summary>
        /// <param name="_imageSize"></param>
        /// <param name="_bgColor"></param>
        /// <param name="_cameraAngle"></param>
        /// <param name="_cameraLight"></param>
        /// <returns></returns>
        public Texture2D GetCharacterPhoto(Vector2 _imageSize, Color _bgColor, float _cameraAngle = 0F, bool _cameraLight = true)
        {
            return PhotoHouse.TakePhoto(_imageSize, GetBoneByName("Bip001 Head"), _bgColor, _cameraAngle, _cameraLight);
        }

        /// <summary>
        /// Get the animation compnent of the back accessory.
        /// </summary>
        /// <returns></returns>
        public Animation GetBackAnimationComponent()
        {
            if (mCharacterBoneControl == null) return null;
            return mCharacterBoneControl.BackAnimation;
        }

        /// <summary>
        /// Get the animation compnent of the tail accessory.
        /// </summary>
        /// <returns></returns>
        public Animation GetTailAnimationComponent()
        {
            if (mCharacterBoneControl == null) return null;
            return mCharacterBoneControl.TailAnimation;
        }

        /// <summary>
        /// Get the animation compnent of the head accessory.
        /// </summary>
        /// <returns></returns>
        public Animation GetHeadAccessoryAnimationComponent()
        {
            if (mCharacterBoneControl == null) return null;
            return mCharacterBoneControl.HeadAccAnimation;
        }

        /// <summary>
        /// Get the bone transform of the character by the name of the bone.
        /// </summary>
        /// <param name="_name"></param>
        /// <returns></returns>
        public Transform GetBoneByName(string _name)
        {
            if (mCharacterBoneControl != null)
            {
                if (mCharacterBoneControl.BoneDictionary.ContainsKey(_name)) return mCharacterBoneControl.BoneDictionary[_name];
            }
            return null;
        }

        /// <summary>
        /// Sets the character's emotion and specifies how long the emotion will last
        /// </summary>
        /// <param name="_uid"></param>
        /// <param name="_length"></param>
        public void SetEmotion(string _uid, float _length = 3F)
        {
            if (mCharacterBoneControl == null) return;
            mCharacterBoneControl.SetEmotion(_uid, _length);
        }

        /// <summary>
        /// Stop the current emotion and reset it to default emotion.
        /// </summary>
        public void StopEmotion()
        {
            if (mCharacterBoneControl == null) return;
            mCharacterBoneControl.StopEmotion();
        }

        /// <summary>
        /// Sets the percentage of the character��s eye openness. 1~100
        /// </summary>
        /// <param name="_openPercentage"></param>
        public void SetEyeOpen(float _openPercentage)
        {
            if (mCharacterBoneControl == null) return;
            mCharacterBoneControl.EyeOpen = _openPercentage;
        }

        /// <summary>
        /// Set the rim effect color, this could be useful for highlight the character or when the character gets hit.
        /// </summary>
        /// <param name="_color"></param>
        /// <param name="_intensity"></param>
        public void SetRimColor(Color _color, float _intensity)//
        {
            mCharacterBoneControl.RimColor = _color * _intensity;
        }

        /// <summary>
        /// Get the rim effect color, this could be useful for highlight the character or when the character gets hit.
        /// </summary>
        /// <returns></returns>
        public Color GetRimColor()
        {
            return mCharacterBoneControl.RimColor;
        }

        /// <summary>
        /// Forces the character to blink immediately.
        /// </summary>
        public void Blink()
        {
            if (mCharacterBoneControl == null) return;
            mCharacterBoneControl.Blink();
        }

        /// <summary>
        /// Makes the character look at a specified transform target.
        /// </summary>
        /// <param name="_target"></param>
        public void SetLookAt(Transform _target)
        {
            if (mCharacterBoneControl == null) return;
            LookAtTarget = _target;
            mCharacterBoneControl.LookAtTarget = LookAtTarget;
        }

        #endregion


        #region Customization
        /// <summary>
        /// Resets the character��s appearance back to its default state.
        /// </summary>
        public void ResetCharacter()
        {
            Debug.Log("Reset Character");
            mCharacterAppearance = new CharacterAppearance(CharacterData.Create((byte)Sex.Male));
            Initialize(mCharacterAppearance);
        }

        /// <summary>
        /// Switches to the Character Customization UI with this character (for player use)
        /// </summary>
        public void CustomizeCharacter()
        {
            if (CharacterManager.instance == null)
                _ = CharacterDataSetting.instance;
            if (CharacterManager.instance == null)
            {
                Debug.LogError("CharacterEntity: Missing CharacterManager; cannot open customizer.");
                return;
            }
            if (mCharacterAppearance == null) ResetCharacter();
            CharacterCusSetting _setting = new CharacterCusSetting()
            {
                AllowCustomOutfit = CharacterManager.instance.AllowOutfitsWhenCustomize,
                AllowNameChange = CharacterManager.instance.AllowChangeNameWhenCustomize,
                AllowSexSwitch = CharacterManager.instance.AllowChangeSexWhenCustomize,
                AllowRaceChange = CharacterManager.instance.AllowChangeRaceWhenCustomize,
                RaceSettingVisible = CharacterManager.instance.RaceSettingVisible,
                BackCategoryVisible = CharacterManager.instance.BackCategoryVisible,
                TailCategoryVisible = CharacterManager.instance.TailCategoryVisible
            };
            CharacterManager.StartCreation(this, CharacterManager.instance.BlueprintPath, SaveMethod.PngFile, _setting, SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Switches to the Character Creation UI (for player use)
        /// </summary>
        public void CreateCharacter()
        {
            if (CharacterManager.instance == null)
                _ = CharacterDataSetting.instance;
            if (CharacterManager.instance == null)
            {
                Debug.LogError("CharacterEntity: Missing CharacterManager; cannot create character.");
                return;
            }
            if (mCharacterAppearance == null) ResetCharacter();
            CharacterCusSetting _setting = new CharacterCusSetting()
            {
                AllowCustomOutfit = CharacterManager.instance.AllowOutfitsWhenCreate,
                AllowNameChange = true,
                AllowSexSwitch = CharacterManager.instance.AllowChangeSexWhenCreate,
                AllowRaceChange = true,
                RaceSettingVisible = CharacterManager.instance.RaceSettingVisible,
                BackCategoryVisible = CharacterManager.instance.BackCategoryVisible,
                TailCategoryVisible = CharacterManager.instance.TailCategoryVisible
            };
            CharacterManager.StartCreation(this, CharacterManager.instance.BlueprintPath, SaveMethod.PngFile, _setting, SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Switches to the Character Creation UI (for developer use, with more options)
        /// </summary>
        public void CreateCharacterByDeveloper()
        {
            if (CharacterManager.instance == null)
                _ = CharacterDataSetting.instance;
            if (CharacterManager.instance == null)
            {
                Debug.LogError("CharacterEntity: Missing CharacterManager; cannot create character.");
                return;
            }
            if (mCharacterAppearance == null) ResetCharacter();
            CharacterCusSetting _setting = new CharacterCusSetting()
            {
                AllowCustomOutfit = true,
                AllowNameChange = true,
                AllowSexSwitch = true,
                AllowRaceChange = true,
                RaceSettingVisible = true,
                BackCategoryVisible = true,
                TailCategoryVisible = true
            };
#if UNITY_EDITOR
            CharacterManager.StartCreation(this, Application.dataPath + EditorRootPath, SaveMethod.BytesFile, _setting, SceneManager.GetActiveScene());
#else
            string _path = Application.dataPath + "/../Blueprints";
            CharacterManager.StartCreation(this, CharacterManager.instance.BlueprintPath, SaveMethod.BytesFile, _setting, SceneManager.GetActiveScene());
#endif
        }

        #endregion
    }
}
