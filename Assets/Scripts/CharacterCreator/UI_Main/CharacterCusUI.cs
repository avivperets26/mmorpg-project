using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.SceneManagement;

namespace Game.CharacterCreator
{
    public struct CharacterCusSetting
    {
        public bool AllowCustomOutfit;
        public bool AllowSexSwitch;
        public bool AllowNameChange;
        public bool AllowRaceChange;
        public bool BackCategoryVisible;
        public bool TailCategoryVisible;
        public bool RaceSettingVisible;

    }
    public class CharacterCusUI : MonoBehaviour
    {
        #region varaibles
        public GameObject DevWarning;
        public GameObject OutfitCategoryPanel;
        public GameObject OutfitSavePanel;
        public GameObject SexPanel;
        public GameObject NamePanel;
        public GameObject RacePanel;
        public GameObject BackPanel;
        public GameObject TailPanel;
        public static CharacterCusUI instance;
        public CharacterCusSliders []  MyLeftSliders;
        public Button[] MainBts;
        public GameObject[] BtSels;
        public GameObject[] SexSels;
        public cc_Mat_Setting MatScript;
        public InputField NameInput;
        public Sex mSex =0;
        public RectTransform PhotoCenter;
        public Animation PhotoResult;
        public RawImage PhotoImage;
        public Text PhotoName;
        public Text PhotoPath;
        public GameObject BlueprintList;
        public Text BlueprintListPath;
        public GameObject BluePrintItem;

        public GameObject ByteFileList;
        public Text ByteFileListPath;
        public GameObject ByteFileItem;

        public InputField PhotoNameInput;

        public static string SaveRootPath;
        public static SaveMethod SaveFormat;
        public static CharacterCusSetting Settings;
        public static CharacterAppearance InitialData;

        private int CurrentSlider = -1;
        private float CanClose = 0F;
        public CharacterAppearance AppearanceData;
        private OutfitSlots CurrentSlot;
        private List<GameObject> BlueprintItems = new List<GameObject>();
        private List<GameObject> ByteFileItems = new List<GameObject>();
        [HideInInspector]
        public bool Initialized = false;

        public delegate void OnSwitchUI();
        public static event OnSwitchUI onSwitchUI;
        
        #endregion 

        #region internal methods

        private void Awake()
        {
            instance = this;
            GetComponent<CanvasGroup>().alpha = 0F;
           
        }

        private IEnumerator Start()
        {
            while (TransitionUI.instance != null) yield return 1;
            Initialize();
            yield return new WaitForSeconds(0.3F);
            GetComponent<Animation>().Play("cc_in");
            GetComponent<CanvasGroup>().alpha = 1F;
            CameraControl.instance.Initialized = true;
        }
        public OutfitColorSetting GetOutfitSetting()
        {
            return CharacterDataSetting.instance.GetOutfitColorSetting((Sex)AppearanceData._CharacterData.Sex, CurrentSlot, AppearanceData._OutfitID[(int)CurrentSlot]);
        }

        public int GetCurrentOutfitSettingId()
        {
            switch (CurrentSlider)
            {
                default: return -1;
                case 5: return 0;
                case 4: return 1;
                case 6: return 2;
                case 7: return 3;
                case 8: return 4;
                case 9:return 5;
                case 10:return 6;
            }
        }

        public void ButtonSetSex(int _sex)
        {
            SetSex((Sex)_sex);
        }

        private void OnDestroy()
        {
            CharacterManager.instance.RemovePreviewCharacter("cc_" + mSex.ToString());
        }

        private void Update()
        {
            if (!Initialized) return;
            MyCharacter.GetComponent<Animator>().SetBool("Static", CurrentSlider == 0 || PhotoCenter.gameObject.activeSelf);

            for (int i = 0; i < MainBts.Length; i++)
            {
                MainBts[i].transform.localScale = Vector3.Lerp(MainBts[i].transform.localScale, Vector3.one * (CurrentSlider == i ? 1F : 0.7F), Time.deltaTime * 8F);
                BtSels[i].SetActive(CurrentSlider == i);
            }
            CanClose += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape) && CanClose > 5F)
            {

            }
            if (EventSystem.current.currentSelectedGameObject == NameInput.gameObject)
            {
                AppearanceData._Name = NameInput.text;
            }
        }


        IEnumerator SaveCharacterWithPhotoCo(string _path, string _fileName, BlurPrintType _type)
        {
            PhotoResult.gameObject.SetActive(false);
            PhotoNameInput.text = _fileName;
            CharacterManager.SetPhotoMode(_type == BlurPrintType.Outfits ? 1 : 2);
            PhotoCenter.gameObject.SetActive(true);
            bool _done = false;
            bool _process = false;
            while (!_done)
            {
                if (EventSystem.current.currentSelectedGameObject == null && Input.GetKeyDown(KeyCode.Space))
                {
                    _process = true;
                    _done = true;
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _process = false;
                    _done = true;
                }
                yield return 1;
            }

            if (_process)
            {
                SoundManager.Play2D("Photo");
                _fileName = PhotoNameInput.text;
                yield return new WaitForEndOfFrame();
                Texture2D _tex = CaptureAvatar();
                CharacterManager.SaveCharacterDataWithPhoto(MyCharacter, _tex, _path + _fileName + ".png", _type);
                PhotoName.text = _fileName + ".png";
                PhotoPath.text = _path;
                PhotoImage.texture = _tex;
                PhotoResult.gameObject.SetActive(true);
                yield return 1;
                while (PhotoResult.isPlaying)
                {
                    yield return 1;
                }
                float _time = 0F;
                while (_time < 2F && !Input.GetKeyDown(KeyCode.Space))
                {
                    yield return 1;
                    _time += Time.deltaTime;
                }
                PhotoResult.gameObject.SetActive(false);
            }
            CharacterManager.SetPhotoMode(0);
            yield return 1;
            PhotoCenter.gameObject.SetActive(false);
        }

        public void BluePrintClick(Text _file)
        {
            CameraControl.instance.DisableForTime(0.3F);
            CharacterManager.instance.RemovePreviewCharacter("cc_" + mSex.ToString());
            CharacterManager.LoadCharacterDataWithPhoto(ref AppearanceData, BlueprintListPath.text + _file.text);
            mSex = (Sex)AppearanceData._Sex;
            MyCharacter.Initialize(AppearanceData);
            RefreshData();
            ToggleBlueprintList(false);
            SoundManager.Play2D("Load");
        }

        public void ByteFileClick(Text _file)
        {
            CameraControl.instance.DisableForTime(0.3F);
            CharacterManager.instance.RemovePreviewCharacter("cc_" + mSex.ToString());
#if UNITY_EDITOR
            AppearanceData = CharacterManager.LoadCharacterDataFromResources(ByteFileListPath.text + _file.text.Replace(".bytes", ""), AppearanceData).Copy();
#else
            AppearanceData = CharacterManager.LoadCharacterDataFromFile(AppearanceData,ByteFileListPath.text + _file.text).Copy();
#endif
            mSex = (Sex)AppearanceData._CharacterData.Sex;
            MyCharacter.Initialize(AppearanceData);
            RefreshData();
            ToggleByteFileList(false);
            SoundManager.Play2D("Load");
        }

        private void InitBlueprintList(string _path)
        {
            foreach (GameObject obj in BlueprintItems)
            {
                if (obj != null) Destroy(obj);
            }
            BlueprintItems.Clear();
            BlueprintListPath.text = _path;
            string[] _files = Directory.GetFiles(_path, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string _file in _files)
            {
                GameObject _newItem = Instantiate(BluePrintItem, BluePrintItem.transform.parent);
                byte[] _bytes = File.ReadAllBytes(_file);
                Texture2D _tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
                ImageConversion.LoadImage(_tex, _bytes);
                _newItem.SetActive(true);
                _newItem.GetComponentInChildren<RawImage>(true).texture = _tex;
                _newItem.GetComponentInChildren<Text>(true).text = Path.GetFileName(_file);
                BlueprintItems.Add(_newItem);
            }
            SoundManager.Play2D("Paper");
        }

        private void InitByteFileList(string _path, string _resourcePath)
        {
            foreach (GameObject obj in ByteFileItems)
            {
                if (obj != null) Destroy(obj);
            }
            ByteFileItems.Clear();
#if UNITY_EDITOR
            ByteFileListPath.text = _resourcePath;
#else
            ByteFileListPath.text = _path;
#endif
            string[] _files = Directory.GetFiles(_path, "*.bytes", SearchOption.TopDirectoryOnly);
            foreach (string _file in _files)
            {
                byte[] _bytes = File.ReadAllBytes(_file);
                if (_bytes[1] == AppearanceData._CharacterData.Sex)
                {
                    GameObject _newItem = Instantiate(ByteFileItem, ByteFileItem.transform.parent);
                    _newItem.SetActive(true);
                    _newItem.GetComponentInChildren<Text>(true).text = Path.GetFileName(_file);
                    ByteFileItems.Add(_newItem);
                }
            }
            SoundManager.Play2D("Paper");
        }
#endregion

        public CharacterBoneControl MyCharacter
        {
            get
            {
                if (CharacterManager.instance.GetPreviewCharacter("cc_" + mSex.ToString()) != null)
                {
                    return CharacterManager.instance.GetPreviewCharacter("cc_" + mSex.ToString());
                }
                else
                {
                    return CharacterManager.instance.CreatePreviewCharacter("cc_" + mSex.ToString(), mSex);
                }
            }
        }

        public void Initialize()
        {
            if (InitialData == null)
            {
                if(Application.isEditor) DevWarning.SetActive(true);
                InitialData = new CharacterAppearance(CharacterData.Create(0));
            }
            AppearanceData = InitialData.Copy();
            mSex = (Sex)InitialData._CharacterData.Sex;
            SwitchUI(1);
            InitializeCharacter();
            
            NamePanel.SetActive(Settings.AllowNameChange);
            OutfitCategoryPanel.SetActive(Settings.AllowCustomOutfit);
            OutfitSavePanel.SetActive(Settings.AllowCustomOutfit);
            SexPanel.SetActive(Settings.AllowSexSwitch);
            RacePanel.SetActive(Settings.AllowRaceChange && Settings.RaceSettingVisible);
            if(BackPanel) BackPanel.SetActive(Settings.BackCategoryVisible);
            if (TailPanel) TailPanel.SetActive(Settings.TailCategoryVisible);
            Initialized = true;
        }


        public void CloseDeveWarning()
        {
            DevWarning.SetActive(false);
        }

        public void OpenHelp()
        {
            Application.OpenURL(Application.dataPath.Replace("Assets", "") + "Assets/CharacterCreator/Documentation/UserGuide.pdf");
        }

        public void InitializeCharacter()
        {
            MyCharacter.Initialize(AppearanceData);
            MyCharacter.transform.localEulerAngles = new Vector3(0F, -5F, 0F);
            RefreshData();
            MyCharacter.GetComponent<Animator>().SetTrigger("Enter");
        }

        public void RefreshData()
        {
            foreach (CharacterCusSliders obj in MyLeftSliders)
            {
                obj.InitData(AppearanceData);
            }
            MatScript.UpdateValue();
            SexSels[0].SetActive(mSex == Sex.Male);
            SexSels[1].SetActive(mSex == Sex.Female);
            NameInput.text = AppearanceData._Name;
        }

        public void SwitchOutfit(OutfitSlots _slot, int _id)
        {
            AppearanceData._OutfitID[(int)_slot] = (byte)_id;
            if ((int)_slot < 5)
            {
                AppearanceData._CusColor1[(int)_slot] = Uint8Color.Set(CharacterDataSetting.instance.GetOutfitColorSetting((Sex)AppearanceData._CharacterData.Sex, _slot, _id).DefaultColor1);
                AppearanceData._CusColor2[(int)_slot] = Uint8Color.Set(CharacterDataSetting.instance.GetOutfitColorSetting((Sex)AppearanceData._CharacterData.Sex, _slot, _id).DefaultColor2);
                AppearanceData._CusColor3[(int)_slot] = Uint8Color.Set(CharacterDataSetting.instance.GetOutfitColorSetting((Sex)AppearanceData._CharacterData.Sex, _slot, _id).DefaultColor3);
            }
            SoundManager.Play2D("Clothes");
        }

        public void SetSex(Sex _sex)
        {
            CharacterManager.instance.RemovePreviewCharacter("cc_" + mSex.ToString());
            if (mSex != _sex)AppearanceData = new CharacterAppearance(CharacterData.Create((byte)_sex));
            mSex = _sex;
            InitializeCharacter();
            SexSels[0].SetActive(_sex ==  Sex.Male);
            SexSels[1].SetActive(_sex ==  Sex.Female);
            SoundManager.Play2D("Load");
        }

        public void SaveClothes()
        {
            string _rootPath = SaveRootPath + "/Clothes/";
            if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
            string _photoName = AppearanceData._Name;
            int _index = 2;
            while (File.Exists(_rootPath + _photoName + ".png"))
            {
                _photoName = AppearanceData._Name + "_" + _index.ToString();
                _index++;
            }
            if (SaveFormat == SaveMethod.PngFile)
            {
                SaveCharacterWithPhoto(_rootPath, _photoName, BlurPrintType.Outfits);
            }
            else
            {
                CharacterManager.SaveCharacterDataToResources(MyCharacter, _rootPath + _photoName, BlurPrintType.Outfits);
                SimpleDynamicMsg.PopMsg("Character saved in ["+SaveRootPath + "/Clothes/" + _photoName + "]");
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
        }

        public void LoadClothes()
        {
            string _rootPath = SaveRootPath + "/Clothes/";
            if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
            if (SaveFormat == SaveMethod.PngFile)
            {
                InitBlueprintList(_rootPath);
                ToggleBlueprintList(true);
            }
            else
            {
                InitByteFileList(_rootPath, "CharacterCreator/CustomBlueprints/Clothes/");
                ToggleByteFileList(true);
            }
        }

        public void SaveData()
        {
            string _rootPath = SaveRootPath + "/Characters/";
            if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
            string _photoName = AppearanceData._Name;
            int _index = 2;
            while (File.Exists(_rootPath + _photoName + ".png"))
            {
                _photoName = AppearanceData._Name + "_" + _index.ToString();
                _index++;
            }
            if (SaveFormat == SaveMethod.PngFile)
            {
                SaveCharacterWithPhoto(_rootPath, _photoName, BlurPrintType.AllAppearance);
            }
            else
            {
                CharacterManager.SaveCharacterDataToResources(MyCharacter, _rootPath+ _photoName, BlurPrintType.AllAppearance);
                SimpleDynamicMsg.PopMsg("Character saved in ["+SaveRootPath + "/Characters/" + _photoName+"]");
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
        }

        public void LoadData()
        {
            string _rootPath = SaveRootPath + "/Characters/";
            if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
            if (SaveFormat == SaveMethod.PngFile)
            {
                InitBlueprintList(_rootPath);
                ToggleBlueprintList(true);
            }
            else
            {
                InitByteFileList(_rootPath, "CharacterCreator/CustomBlueprints/Characters/");
                ToggleByteFileList(true);
            }
        }

        public void SaveCharacterWithPhoto(string _path, string _fileName, BlurPrintType _type)
        {
            StartCoroutine(SaveCharacterWithPhotoCo(_path,_fileName, _type));
        }

        public Texture2D CaptureAvatar()
        {
            Vector3[] corners = new Vector3[4];
            PhotoCenter.GetWorldCorners(corners);
            var bl = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var tl = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            var tr = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            var height = tl.y - bl.y;
            var width = tr.x - bl.x;
            //Debug.Log(bl.x+","+ bl.y+"  > "+width + ","+height);
            Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
            Rect rex = new Rect(bl.x, bl.y, width, height);
            tex.ReadPixels(rex, 0, 0);
            tex.Apply();
            return tex;
        }

        public void ToggleBlueprintList(bool _visible)
        {
            BlueprintList.SetActive(_visible);
        }

        public void ToggleByteFileList(bool _visible)
        {
            ByteFileList.SetActive(_visible);
        }

        public void RestoreData()
        {
            CharacterManager.instance.RemovePreviewCharacter("cc_" + mSex.ToString());
            Initialize();
            SimpleDynamicMsg.PopMsg("Character has been reset.");
            SoundManager.Play2D("Load");
        }

        public void Preset(int _id)
        {
            AppearanceData = CharacterManager.LoadCharacterDataFromResources( "CharacterCreator/Presets/shape_"+_id.ToString()+"_"+mSex.ToString(), AppearanceData).Copy();
            MyCharacter.Initialize(AppearanceData);
            RefreshData();
            ToggleByteFileList(false);
            SoundManager.Play2D("Load");
        }

        public void RandomLook()
        {
            Color[] _colors = ColorPicker.GetColorPalette();
            List<Color> _skinColors = new List<Color>();
            List<Color> _hairColors = new List<Color>();
            for (int i=0;i<50;i++) {
               if(i<=12 || (i>=20 && i<=24) || (i>=30 && i<=34)) _skinColors.Add(_colors[i]);
               if (i <= 12 || (i >= 15 && i <= 24) || i >= 35) _hairColors.Add(_colors[i]);
            }
            AppearanceData._CharacterData.Random(_skinColors.ToArray(),_hairColors.ToArray());
            MyCharacter.Initialize(AppearanceData);
            RefreshData();
            SoundManager.Play2D("Load");
        }

        public void RandomClothes()
        {
            Color[] _colors = ColorPicker.GetColorPalette();
            Uint8Color _outfitColor1 = Uint8Color.Set(_colors[Random.Range(0, _colors.Length)]);
            Uint8Color _outfitColor2 = Uint8Color.Set(_colors[Random.Range(0, _colors.Length)]);
            Uint8Color _outfitColor3 = Uint8Color.Set(_colors[Random.Range(0, _colors.Length)]);
            bool _default = Random.Range(0, 100) < 30;
            
            for (int i = 0; i < 5; i++)
            {
                OutfitInfo [] _infos= CharacterDataSetting.instance.GetOutfitSettings((Sex)AppearanceData._CharacterData.Sex, (OutfitSlots)i);
                AppearanceData._OutfitID[i] = (byte)Random.Range(0,_infos.Length);
                AppearanceData._CusColor1[i] = _default? Uint8Color.Set(_infos[AppearanceData._OutfitID[i]].ColorSetting.DefaultColor1): _outfitColor1;
                AppearanceData._CusColor2[i] = _default ? Uint8Color.Set(_infos[AppearanceData._OutfitID[i]].ColorSetting.DefaultColor2) : _outfitColor2;
                AppearanceData._CusColor3[i] = _default ? Uint8Color.Set(_infos[AppearanceData._OutfitID[i]].ColorSetting.DefaultColor3) : _outfitColor3;
            }
            RefreshData();
            SoundManager.Play2D("Load");
        }

        public void SwitchUI(int _id)
        {
            foreach (CharacterCusSliders obj in MyLeftSliders)
            {
                if (obj.gameObject.activeSelf)
                    obj.FadeAway();
            }
            if (CurrentSlider != _id)
            {
                MyLeftSliders[_id].gameObject.SetActive(true);
                CurrentSlider = _id;
            }
            else
            {
                CurrentSlider = -1;
            }
            CurrentSlot = MyLeftSliders[_id].OutfitSlot;
            MyCharacter.GetComponent<Animator>().SetBool("Static", CurrentSlider==0);
            if (_id != -1 && onSwitchUI!=null) onSwitchUI();
            if (ColorPicker.instance != null) ColorPicker.instance.Cancel();
            SoundManager.Play2D("MenuOff");
        }

        public void Close()
        {
            GetComponent<Animation>().Play("cc_out");
            CharacterManager.OnCharacterCustomized(AppearanceData);
        }
  


    }
}
