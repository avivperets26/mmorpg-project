using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// This script manages all aspects of a character's appearance, storing the appearance data in a Dictionary using the character's UID as the key. 
/// Typically, you don't need to manually update the character's appearance as it is automatically handled by "CharacterEntity.cs". 
/// However, if you need to modify the character's appearance independently from "CharacterEntity.cs", you can use this script.
/// Additionally, this script allows save/load a character's appearance from a byte file or a PNG file.
/// </summary>
namespace Game.CharacterCreator
{
    public class CharacterManager : MonoBehaviour
    {
        #region variables
        public static CharacterManager instance;
        public bool useRelativePathToGameInstallFolder = true;
        public string CharacterBlueprintRootPath = "CharacterBlueprints";
        public bool AllowOutfitsWhenCreate = true;
        public bool AllowChangeSexWhenCreate = true;
        public bool AllowOutfitsWhenCustomize = false;
        public bool AllowChangeNameWhenCustomize = false;
        public bool AllowChangeSexWhenCustomize = false;
        public bool AllowChangeRaceWhenCustomize = false;
        public bool BackCategoryVisible = true;
        public bool TailCategoryVisible = true;
        public bool RaceSettingVisible = true;
        public bool CacheMeshTexture = true;
        public List<EmotionSetting> EmotionSettings = new List<EmotionSetting>();
        public Dictionary<string, CharacterBoneControl> CharacterEntityDic = new Dictionary<string, CharacterBoneControl>();
        public static int PhotoMode = 0;
        public static Dictionary<string, EmotionSetting> CharacterEmotions = new Dictionary<string, EmotionSetting>();
        public static Dictionary<string, Texture> LoadedTextures = new Dictionary<string, Texture>();
        public static Dictionary<string, Mesh> LoadedMeshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, Material> LoadedMaterials = new Dictionary<string, Material>();
        public static Dictionary<string, GameObject> LoadedObjects = new Dictionary<string, GameObject>();
        private static Dictionary<string, CharacterAppearance> CharacterAppearanceDic = new Dictionary<string, CharacterAppearance>();
        private static string CustomizeUid="";
        private static string ExitScene="";
        #endregion

        #region internal methods
        private void Awake()
        {
            instance = this;
            CharacterEmotions.Clear();
            foreach (var obj in EmotionSettings) {
                if (!CharacterEmotions.ContainsKey(obj.uid))
                {
                    CharacterEmotions.Add(obj.uid,obj);
                }
            }
            SetPhotoMode(0);
        }

        public string BlueprintPath
        {
            get
            {
                if (useRelativePathToGameInstallFolder)
                {
#if UNITY_EDITOR
                    return Application.dataPath + "/" + CharacterBlueprintRootPath;
#else
                    return Application.dataPath + "/../" + CharacterBlueprintRootPath;
#endif
                }
                else
                {
                    return CharacterBlueprintRootPath;
                }
            }
        }

        public static void ClearLoadedAssets()
        {
            LoadedMeshes.Clear();
            LoadedTextures.Clear();
            LoadedMaterials.Clear();
            LoadedObjects.Clear();
        }

        public static Texture LoadTexture(string _path)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (Texture)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/" + _path+".png", typeof(Texture));
            }
#endif
            if (!instance.CacheMeshTexture) return Resources.Load<Texture>(_path);
            string _key = _path.Replace(" ", "").Replace("CharacterCreator/Player/", "");
            if (LoadedTextures.ContainsKey(_key))
            {
                return LoadedTextures[_key];
            }
            else
            {
                LoadedTextures.Add(_key, Resources.Load<Texture>(_path));
                return LoadedTextures[_key];
            }
        }

        public static Mesh LoadMesh(string _path)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (Mesh)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/"+ _path+".mesh", typeof(Mesh));
            }
#endif
            if (!instance.CacheMeshTexture) return Resources.Load<Mesh>(_path);
            string _key = _path.Replace(" ", "").Replace("CharacterCreator/Player/", "");
            if (LoadedMeshes.ContainsKey(_key))
            {
                return LoadedMeshes[_key];
            }
            else
            {
                LoadedMeshes.Add(_key, Resources.Load<Mesh>(_path));
                return LoadedMeshes[_key];
            }
        }

        public static Material LoadMaterial(string _path)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (Material)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/" + _path+".mat", typeof(Material));
            }
#endif
            if (!instance.CacheMeshTexture) return Resources.Load<Material>(_path);
            string _key = _path.Replace(" ", "").Replace("CharacterCreator/Player/", "");
            if (LoadedMaterials.ContainsKey(_key))
            {
                return LoadedMaterials[_key];
            }
            else
            {
                LoadedMaterials.Add(_key, Resources.Load<Material>(_path));
                return LoadedMaterials[_key];
            }
        }

        public static GameObject LoadObject(string _path)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (GameObject)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/" + _path+".prefab", typeof(GameObject));
            }
#endif
            if (!instance.CacheMeshTexture ) return Resources.Load<GameObject>(_path);
            string _key = _path.Replace(" ", "").Replace("CharacterCreator/Player/", "");
            if (LoadedObjects.ContainsKey(_key))
            {
                return LoadedObjects[_key];
            }
            else
            {
                LoadedObjects.Add(_key, Resources.Load<GameObject>(_path));
                return LoadedObjects[_key];
            }
        }

        private void OnDestroy()
        {
            SetPhotoMode(0);
        }
        private CharacterBoneControl NewPreviewCharacter(Sex _sex)
        {
            return Instantiate(Resources.Load<GameObject>("CharacterCreator/Player/" + (_sex == Sex.Female ? "CharacterFemale" : "CharacterMale"))).GetComponent<CharacterBoneControl>();
        }

        public static void SetPhotoMode(int _mode)
        {
            PhotoMode = _mode;
            Shader.SetGlobalInt("PhotoMode", PhotoMode);
        }

        public static void StartCreation(CharacterEntity _entity, string _saveRootPath, SaveMethod _saveFormat, CharacterCusSetting _setting, Scene _exitScene)
        {
            CustomizeUid = _entity.uid;
            CharacterCusUI.InitialData = _entity.mCharacterAppearance.Copy();
            CharacterCusUI.SaveRootPath = _saveRootPath;
            CharacterCusUI.SaveFormat = _saveFormat;
            CharacterCusUI.Settings = _setting;
            ExitScene = _exitScene.name;
            TransitionUI.LastScene = _exitScene;
            TransitionUI.NextScene = "CharacterCustomization";
            SceneManager.LoadScene("Transition", LoadSceneMode.Additive);
        }

        public static void OnCharacterCustomized(CharacterAppearance _data)
        {
            if (CustomizeUid != "")
            {
                UpdateCharacterAppearance(CustomizeUid, _data);
                CustomizeUid = "";
            }
            TransitionUI.LastScene = SceneManager.GetActiveScene();
            TransitionUI.NextScene = ExitScene;
            SceneManager.LoadScene("Transition", LoadSceneMode.Additive);
        }

        public static bool isCharacterAppearanceExist(CharacterEntity _entity)
        {
            return CharacterAppearanceDic.ContainsKey(_entity.uid);
        }

        public static CharacterAppearance GetAppearanceData(CharacterEntity _entity)
        {
            if (CharacterAppearanceDic.ContainsKey(_entity.uid))
            {
                CharacterAppearance _data = CharacterAppearanceDic[_entity.uid].Copy();
                return _data;
            }
            else
            {
                return null;
            }
        }

        public static void LoadCharacterFromResources(CharacterBoneControl _character, string _resourcePath)
        {
            TextAsset bindata = Resources.Load(_resourcePath) as TextAsset;
            _character.MyData.Load(bindata.bytes);
        }

        public static void LoadCharacterFromFile(CharacterBoneControl _character, string _path)
        {
            byte[] _bytes = File.ReadAllBytes(_path);
            _character.MyData.Load(_bytes);
        }
        #endregion

        /// <summary>
        /// Updates the appearance data of a character identified by the provided UID. Use this function if you need to modify a character's appearance independently of CharacterEntity.cs"
        /// </summary>
        /// <param name="_uid"></param>
        /// <param name="_data"></param>
        public static void UpdateCharacterAppearance(string _uid, CharacterAppearance _data) 
        {
            if (_uid != "")
            {
                if (CharacterAppearanceDic.ContainsKey(_uid))
                {
                    CharacterAppearanceDic[_uid] = _data.Copy();
                }
                else
                {
                    CharacterAppearanceDic.Add(_uid, _data.Copy());
                }
            }
        }

        /// <summary>
        /// Checks if the appearance data for a character with the specified UID exists in the internal Dictionary.
        /// </summary>
        /// <param name="_uid"></param>
        /// <returns></returns>
        public static bool isCharacterAppearanceExist(string _uid)
        {
            return CharacterAppearanceDic.ContainsKey(_uid);
        }

        /// <summary>
        /// Retrieves the appearance data of a character using the provided UID.
        /// </summary>
        /// <param name="_uid"></param>
        /// <returns></returns>
        public static CharacterAppearance GetAppearanceData(string _uid)
        {
            if (CharacterAppearanceDic.ContainsKey(_uid))
            {
                CharacterAppearance _data = CharacterAppearanceDic[_uid].Copy();
                return _data;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Load appearance data from a byte file located in the resources folder.
        /// </summary>
        /// <param name="_resourcePath"></param>
        /// <param name="_data"></param>
        /// <returns></returns>
        public static CharacterAppearance LoadCharacterDataFromResources(string _resourcePath, CharacterAppearance _data)
        {
            TextAsset bindata = Resources.Load(_resourcePath) as TextAsset;
            CharacterAppearance _result;
            if (_data == null)
            {
                _result = new CharacterAppearance(bindata.bytes);
            }
            else{
                _result=_data.Copy();
                if (bindata.bytes[0] != 3 && bindata.bytes[1] != _result._CharacterData.Sex)
                {
                    SimpleDynamicMsg.PopMsg("The gender of the blurprint is different from this character.");
                }
                else
                {
                    _result.Load(bindata.bytes);
                }
            }
           
            return _result;
        }

        /// <summary>
        /// Load appearance data from a byte file stored on disk.
        /// </summary>
        /// <param name="_data"></param>
        /// <param name="_path"></param>
        /// <returns></returns>
        public static CharacterAppearance LoadCharacterDataFromFile(CharacterAppearance _data, string _path)
        {
            byte[] _bytes = File.ReadAllBytes(_path);
            CharacterAppearance _result;
            if (_data!=null) 
                _result = _data.Copy();
            else
                _result= new CharacterAppearance(CharacterData.Create(_bytes[1]));
            if (_bytes[0] != 3 && _bytes[1] != _result._CharacterData.Sex)
            {
                SimpleDynamicMsg.PopMsg("The gender of the blurprint is different from this character.");
            }
            else
            {
                _result.Load(_bytes);
            }
            return _result;
        }

        /// <summary>
        /// Load appearance data from a PNG file on disk, with the appearance data hidden inside the image.
        /// </summary>
        /// <param name="_data"></param>
        /// <param name="_path"></param>
        /// <returns></returns>
        public static Texture2D LoadCharacterDataWithPhoto(ref CharacterAppearance _data, string _path)
        {
            byte[] _bytes = File.ReadAllBytes(_path);

            Texture2D _tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
            ImageConversion.LoadImage(_tex, _bytes);
            int length = _bytes[_bytes.Length - 1]+ _bytes[_bytes.Length - 2];
            byte[] _dataBytes = new byte[length];
            System.Array.Copy(_bytes, _bytes.Length - 2 - length, _dataBytes, 0, length);

            if (_data == null) _data = new CharacterAppearance(CharacterData.Create(_dataBytes[1]));
            if (_dataBytes[0] != 3 && _dataBytes[1] != _data._CharacterData.Sex)
            {
                SimpleDynamicMsg.PopMsg("The gender of the blurprint is different from this character.");
            }
            else
            {
                _data.Load(_dataBytes);
            }
            return _tex;
        }

        /// <summary>
        /// Save appearance data to a byte file. Use BlurPrintType to specify if you want to save the entire appearance data or just specific parts.
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_path"></param>
        /// <param name="_type"></param>
        public static void SaveCharacterData(CharacterBoneControl _character, string _path, BlurPrintType _type= BlurPrintType.AllAppearance)
        {
            string filePath = Path.ChangeExtension(_path, ".bytes");
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(filePath))File.Delete(filePath);
            File.WriteAllBytes(filePath, _character.MyData.ToBytes(_type));
        }

        /// <summary>
        /// Save appearance data to a PNG file. The data is embedded within the image file, with the option to limit saved data to certain parts using BlurPrintType.
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_photo"></param>
        /// <param name="_path"></param>
        /// <param name="_type"></param>
        public static void SaveCharacterDataWithPhoto(CharacterBoneControl _character, Texture2D _photo, string _path, BlurPrintType _type = BlurPrintType.AllAppearance)
        {
            
            string filePath = Path.ChangeExtension(_path, ".png");
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(filePath)) File.Delete(filePath);
            byte[] _bytes = _character.MyData.ToBytes(_type);

            List<byte> _finalBytes = new List<byte>();
            byte[] photoBytes = _photo.EncodeToPNG();
            _finalBytes.AddRange(photoBytes);
            _finalBytes.AddRange(_bytes);
            int _length1 = Mathf.Min(_bytes.Length,200);
            int _length2 = Mathf.Max(_bytes.Length - _length1,0);
            _finalBytes.Add((byte)_length1);
            _finalBytes.Add((byte)_length2);
            File.WriteAllBytes(filePath, _finalBytes.ToArray());
        }

        /// <summary>
        /// [For developers] Save appearance data to a byte file in the resources folder. Use BlurPrintType if you intend to save only specific sections of the data.
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_resourcePath"></param>
        /// <param name="_type"></param>
        public static void SaveCharacterDataToResources(CharacterBoneControl _character, string _resourcePath, BlurPrintType _type = BlurPrintType.AllAppearance)
        {
            string filePath = _resourcePath+ ".bytes";
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(filePath)) File.Delete(filePath);
            File.WriteAllBytes(filePath, _character.MyData.ToBytes(_type));
        }



        /// These functions allow you to manage preview characters, which is useful for displaying a character¡¯s current appearance within various UI interfaces 
        /// (e.g., rendering the player¡¯s appearance to a RenderTexture for display in an equipment or customization interface)

        /// <summary>
        /// Creates a preview character at a specified position and angle. The key is used to identify this character for further operations
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_sex"></param>
        /// <param name="_position"></param>
        /// <param name="_angle"></param>
        /// <returns></returns>
        public CharacterBoneControl CreatePreviewCharacter(string _key, Sex _sex, Vector3 _position, float _angle)
        {
            if (GetPreviewCharacter(_key)!=null)return GetPreviewCharacter(_key);
            CharacterBoneControl _newChar = NewPreviewCharacter(_sex);
            _newChar.transform.position = _position;
            _newChar.transform.eulerAngles = new Vector3(0F, _angle,0F);
            _newChar.transform.localScale = Vector3.one;
            CharacterEntityDic.Add(_key, _newChar);
            return _newChar;
        }

        /// <summary>
        /// Create a preview character
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_sex"></param>
        /// <param name="_position"></param>
        /// <param name="_rotation"></param>
        /// <returns></returns>
        public CharacterBoneControl CreatePreviewCharacter(string _key, Sex _sex, Vector3 _position, Quaternion _rotation)
        {
            if (GetPreviewCharacter(_key) != null) return GetPreviewCharacter(_key);
            CharacterBoneControl _newChar = NewPreviewCharacter(_sex);
            _newChar.transform.position = _position;
            _newChar.transform.rotation = _rotation;
            _newChar.transform.localScale = Vector3.one;
            CharacterEntityDic.Add(_key, _newChar);
            return _newChar;
        }

        /// <summary>
        /// Create a preview character
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_sex"></param>
        /// <param name="_position"></param>
        /// <returns></returns>
        public CharacterBoneControl CreatePreviewCharacter(string _key, Sex _sex, Vector3 _position)
        {
            if (GetPreviewCharacter(_key) != null) return GetPreviewCharacter(_key);
            CharacterBoneControl _newChar = NewPreviewCharacter(_sex);
            _newChar.transform.position = _position;
            _newChar.transform.eulerAngles = Vector3.zero;
            _newChar.transform.localScale = Vector3.one;
            CharacterEntityDic.Add(_key, _newChar);
            return _newChar;
        }

        /// <summary>
        /// Create a preview character
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_sex"></param>
        /// <returns></returns>
        public CharacterBoneControl CreatePreviewCharacter(string _key, Sex _sex)
        {
            if (GetPreviewCharacter(_key) != null) return GetPreviewCharacter(_key);
            CharacterBoneControl _newChar = NewPreviewCharacter(_sex);
            _newChar.transform.position = Vector3.zero;
            _newChar.transform.eulerAngles = Vector3.zero;
            _newChar.transform.localScale = Vector3.one;
            CharacterEntityDic.Add(_key, _newChar);
            return _newChar;
        }

        /// <summary>
        /// Create a preview character
        /// </summary>
        /// <param name="_sex"></param>
        /// <returns></returns>
        public CharacterBoneControl CreatePreviewCharacter(Sex _sex)
        {
            CharacterBoneControl _newChar = NewPreviewCharacter(_sex);
            _newChar.transform.localScale = Vector3.one;
            return _newChar;
        }

        /// <summary>
        /// Retrieves a preview character using the provided key. Once retrieved, you can call CharacterBoneControl().Initialize(CharacterAppearance _data) to apply appearance data to the preview character.
        /// </summary>
        /// <param name="_key"></param>
        /// <returns></returns>
        public CharacterBoneControl GetPreviewCharacter(string _key)
        {
            if (CharacterEntityDic.ContainsKey(_key))
            {
                if (CharacterEntityDic[_key] != null)
                {
                    return CharacterEntityDic[_key];
                }
                else
                {
                    CharacterEntityDic.Remove(_key);
                }
            }
            return null;
        }

        /// <summary>
        /// Removes the preview character identified by the provided key
        /// </summary>
        /// <param name="_key"></param>
        public void RemovePreviewCharacter(string _key)
        {
            if (GetPreviewCharacter(_key) != null) Destroy(GetPreviewCharacter(_key).gameObject);
            if (CharacterEntityDic.ContainsKey(_key)) CharacterEntityDic.Remove(_key);
        }

    }
}
