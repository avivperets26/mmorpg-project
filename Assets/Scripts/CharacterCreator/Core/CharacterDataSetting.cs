using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.CharacterCreator
{

    #region Enum
    public enum WeaponType
    {
        TwoHanded,
        LeftHand,
        RightHand,
        Custom1,
        Custom2,
        Custom3,
        Custom4,
        Custom5,
        Custom6,
    }
    public enum WeaponState
    {
        Carry,
        Hold,
        Hide
    }
    public enum SaveMethod
    {
        BytesFile,
        PngFile
    }
    public enum BlurPrintType
    {
        BodyShape,
        Character,
        Outfits,
        AllAppearance
    }
    public enum Sex
    {
        Male,
        Female
    }
    public enum OutfitSlots
    {
        Armor,
        Helmet,
        Gauntlet,
        Boot,
        Pants,
        Back,
        Tail
    }
    #endregion

    #region EquipmentAppearance Class
    [System.Serializable]
    public class EquipmentAppearance
    {
        public OutfitSlots Type;
        public int MaleMeshId;
        public int FemaleMeshId;
        public bool UseCustomColor = false;
        public Color CustomColor1;
        public Color CustomColor2;
        public Color CustomColor3;
        [HideInInspector]
        public bool uiFold = false;
        public EquipmentAppearance() { }
        public EquipmentAppearance(OutfitSlots _type,int _maleMeshId, int _femaleMeshId,bool _useCustomColor=false,Color _color1= default(Color), Color _color2 = default(Color), Color _color3 = default(Color))
        {
            Type = _type;
            MaleMeshId = _maleMeshId;
            FemaleMeshId = _femaleMeshId;
            UseCustomColor = _useCustomColor;
            CustomColor1 = _color1;
            CustomColor2 = _color2;
            CustomColor3 = _color3;
            uiFold = false;
        }

        public int GetId(Sex _sex)
        {
            return _sex == Sex.Male ? MaleMeshId : FemaleMeshId;
        }
    }
    #endregion

    #region Character Class

    [System.Serializable]
    public class CharacterAppearance
    {
        public string _Name = "unnamed";
        public CharacterData _CharacterData;
        public byte[] _OutfitID;
        public Uint8Color[] _CusColor1;
        public Uint8Color[] _CusColor2;
        public Uint8Color[] _CusColor3;
        public enum DataVersions
        {
            Before_v1_3,
            Before_v1_5,
            Lastest
        }

        public Sex _Sex
        {
            get
            {
                return (Sex)_CharacterData.Sex;
            }
        }
        public int _Race
        {
            get
            {
                return (int)_CharacterData.Race;
            }
        }

        public bool isSameAs(CharacterAppearance _target)
        {
            for (int i = 0; i < _OutfitID.Length; i++)
            {
                if (_OutfitID[i] != _target._OutfitID[i]) return false;
            }
            for (int i = 0; i < _CusColor1.Length; i++)
            {
                if (_CusColor1[i] != _target._CusColor1[i]) return false;
            }
            for (int i = 0; i < _CusColor2.Length; i++)
            {
                if (_CusColor2[i] != _target._CusColor2[i]) return false;
            }
            for (int i = 0; i < _CusColor3.Length; i++)
            {
                if (_CusColor3[i] != _target._CusColor3[i]) return false;
            }
            if(_target._CharacterData.Sex != _CharacterData.Sex) return false;
            if (_target._CharacterData.Race != _CharacterData.Race) return false;
            for (int i = 0; i < _CharacterData.DataFloat.Length; i++)
            {
                if (_target._CharacterData.DataFloat[i] != _CharacterData.DataFloat[i]) return false;
            }
            for (int i = 0; i < _CharacterData.DataColor.Length; i++)
            {
                if (_target._CharacterData.DataColor[i] != _CharacterData.DataColor[i]) return false;
            }
            for (int i = 0; i < _CharacterData.DataInt.Length; i++)
            {
                if (_target._CharacterData.DataInt[i] != _CharacterData.DataInt[i]) return false;
            }
            return true;
        }

        public CharacterAppearance Copy()
        {
            CharacterAppearance _copy = new CharacterAppearance();
            _copy._CharacterData= _CharacterData.Copy();
            _copy._Name = _Name;
            _copy._OutfitID = new byte[_OutfitID.Length];
            _copy._CusColor1 = new Uint8Color[_CusColor1.Length];
            _copy._CusColor2 = new Uint8Color[_CusColor2.Length];
            _copy._CusColor3 = new Uint8Color[_CusColor3.Length];
            for (int i = 0; i < _OutfitID.Length; i++)
            {
                _copy._OutfitID[i] = _OutfitID[i];
            }
            for (int i = 0; i < _CusColor1.Length; i++)
            {
                _copy._CusColor1[i] = new Uint8Color(_CusColor1[i].r, _CusColor1[i].g, _CusColor1[i].b, _CusColor1[i].a);
            }
            for (int i = 0; i < _CusColor2.Length; i++)
            {
                _copy._CusColor2[i] = new Uint8Color(_CusColor2[i].r, _CusColor2[i].g, _CusColor2[i].b, _CusColor2[i].a);
            }
            for (int i = 0; i < _CusColor3.Length; i++)
            {
                _copy._CusColor3[i] = new Uint8Color(_CusColor3[i].r, _CusColor3[i].g, _CusColor3[i].b, _CusColor3[i].a);
            }
            return _copy;
        }

        public void DefaultValue()
        {
            _Name = "unnamed";
            int _outfitSlotsCount = System.Enum.GetValues(typeof(OutfitSlots)).Length;
            _OutfitID = new byte[_outfitSlotsCount];
            for (int i = 0; i < _outfitSlotsCount; i++)
            {
                _OutfitID[i] = 0;
            }
            _CusColor1 = new Uint8Color[_outfitSlotsCount];
            _CusColor2 = new Uint8Color[_outfitSlotsCount];
            _CusColor3 = new Uint8Color[_outfitSlotsCount];
            for (int i = 0; i < _outfitSlotsCount; i++)
            {
                _CusColor1[i] = Uint8Color.Set(Color.gray);
                _CusColor2[i] = Uint8Color.Set(Color.gray);
                _CusColor3[i] = Uint8Color.Set(Color.gray);
            }
        }

        public CharacterAppearance() { }
        public CharacterAppearance(CharacterData _cData)
        {
            _CharacterData = _cData;
            DefaultValue();
        }

        public void Load(byte[] _bytes, DataVersions _dataVersion= DataVersions.Lastest)
        {
            int _outfitSlotsCount = Enum.GetValues(typeof(OutfitSlots)).Length;

            if (_dataVersion == DataVersions.Before_v1_5)
            {
                _outfitSlotsCount = 5; //Before v1.5 there were only 5 outfit slots
            }
            else if (_dataVersion == DataVersions.Before_v1_3)
            {
                _outfitSlotsCount = 4; //Before v1.3 there were only 4 outfit slots
            }

            int _index = 0;
            BlurPrintType _bluePrintType = (BlurPrintType)_bytes[_index];
            _index++;
            _CharacterData.Sex = _bytes[_index];
            _index++;

            if (_dataVersion == DataVersions.Lastest)//Before v1,5 there is no race setting.
            {
                if (_bluePrintType != BlurPrintType.BodyShape && _bluePrintType != BlurPrintType.Outfits) _CharacterData.Race = _bytes[_index];
                _index++;
            }

            if (_bluePrintType != BlurPrintType.Outfits)
            {
                if (_bluePrintType == BlurPrintType.BodyShape)
                {
                    byte[] _bodyBytes = new byte[120];
                    Array.Copy(_bytes, _index, _bodyBytes, 0, 120);
                    for (int i=0;i<120;i++) {
                        if (i == 1 || (i >= 81 && i <= 119)) _CharacterData.DataFloat[i] = _bodyBytes[i];
                    }
                    _index += 120;
                }
                else
                {
                    Array.Copy(_bytes, _index, _CharacterData.DataFloat, 0, 120);
                    _index += 120;
                }
              
                if (_bluePrintType != BlurPrintType.BodyShape)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        byte[] _color = new byte[4];
                        Array.Copy(_bytes, _index, _color, 0, 4);
                        _CharacterData.DataColor[i] = new Uint8Color(_color);
                        _index += 4;
                    }
                    Array.Copy(_bytes, _index, _CharacterData.DataInt, 0, 10);
                    _index += 10;
                }
            }
            if (_bluePrintType == BlurPrintType.Outfits || _bluePrintType == BlurPrintType.AllAppearance)
            {
                byte _tail = _OutfitID[6];
                Uint8Color _tailColor1 = _CusColor1[6].Copy();
                Uint8Color _tailColor2 = _CusColor1[6].Copy();
                Uint8Color _tailColor3 = _CusColor1[6].Copy();
                byte _back = _OutfitID[5];
                Uint8Color _backColor1 = _CusColor1[5].Copy();
                Uint8Color _backColor2 = _CusColor1[5].Copy();
                Uint8Color _backColor3 = _CusColor1[5].Copy();
               

                Array.Copy(_bytes, _index, _OutfitID, 0, _outfitSlotsCount);
                _index += _outfitSlotsCount;
                for (int i = 0; i < _outfitSlotsCount; i++)
                {
                    byte[] _color1 = new byte[4];
                    Array.Copy(_bytes, _index, _color1, 0, 4);
                    _CusColor1[i] = new Uint8Color(_color1);
                    _index += 4;

                    byte[] _color2 = new byte[4];
                    Array.Copy(_bytes, _index, _color2, 0, 4);
                    _CusColor2[i] = new Uint8Color(_color2);
                    _index += 4;

                    byte[] _color3 = new byte[4];
                    Array.Copy(_bytes, _index, _color3, 0, 4);
                    _CusColor3[i] = new Uint8Color(_color3);
                    _index += 4;
                }

                if (_bluePrintType == BlurPrintType.Outfits) {
                    if (CharacterDataSetting.instance.RaceSettings[_CharacterData.Race].DefaultBackAccessory != 0)
                    {
                        _OutfitID[5] = _back;
                        _CusColor1[5] = _backColor1;
                        _CusColor2[5] = _backColor2;
                        _CusColor3[5] = _backColor3;
                    }
                    if (CharacterDataSetting.instance.RaceSettings[_CharacterData.Race].DefaultTail != 0)
                    {
                        _OutfitID[6] = _tail;
                        _CusColor1[6] = _tailColor1;
                        _CusColor2[6] = _tailColor2;
                        _CusColor3[6] = _tailColor3;
                    }
                }

                if (_dataVersion == DataVersions.Before_v1_3)//We need to add settings of pants and cape for data from older version before v1.3 
                {
                    _OutfitID[4] = _OutfitID[0];
                    _CusColor1[4] = _CusColor1[0].Copy();
                    _CusColor2[4] = _CusColor2[0].Copy();
                    _CusColor3[4] = _CusColor3[0].Copy();

                    _OutfitID[5] = 0;
                    _CusColor1[5] = Uint8Color.Set(Color.white);
                    _CusColor2[5] = Uint8Color.Set(Color.white);
                    _CusColor3[5] = Uint8Color.Set(Color.white);

                    _OutfitID[6] = 0;
                    _CusColor1[6] = Uint8Color.Set(Color.white);
                    _CusColor2[6] = Uint8Color.Set(Color.white);
                    _CusColor3[6] = Uint8Color.Set(Color.white);
                }

                if (_dataVersion == DataVersions.Before_v1_5)//We need to add settings of cape for data from older version before v1.3 
                {
                    _OutfitID[5] = 0;
                    _CusColor1[5] = Uint8Color.Set(Color.white);
                    _CusColor2[5] = Uint8Color.Set(Color.white);
                    _CusColor3[5] = Uint8Color.Set(Color.white);

                    _OutfitID[6] = 0;
                    _CusColor1[6] = Uint8Color.Set(Color.white);
                    _CusColor2[6] = Uint8Color.Set(Color.white);
                    _CusColor3[6] = Uint8Color.Set(Color.white);
                }

            }
            if (_bluePrintType == BlurPrintType.AllAppearance)
            {
                int _length = _bytes.Length - _index;
                byte[] _nameBytes = new byte[_length];
                Array.Copy(_bytes, _index, _nameBytes, 0, _length);
                _Name = Encoding.ASCII.GetString(_nameBytes);
            }
        }

        public CharacterAppearance(byte[] _bytes)
        {
            _CharacterData = new CharacterData();
            DefaultValue();
            Load(_bytes);
        }

        public CharacterAppearance(byte[] _bytes, DataVersions dataVersions)
        {
            _CharacterData = new CharacterData();
            DefaultValue();
            Load(_bytes, dataVersions);
        }

        public byte[] ToBytes(BlurPrintType _bluePrintType = BlurPrintType.AllAppearance)
        {
            int _outfitSlotsCount = System.Enum.GetValues(typeof(OutfitSlots)).Length;
            List<byte> _bytes = new List<byte>();
            _bytes.Add((byte)_bluePrintType);
            _bytes.Add(_CharacterData.Sex);
            _bytes.Add(_CharacterData.Race);
            if (_bluePrintType!= BlurPrintType.Outfits) {
                _bytes.AddRange(_CharacterData.DataFloat);
                if (_bluePrintType != BlurPrintType.BodyShape)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        _bytes.AddRange(_CharacterData.DataColor[i].ToBytes());
                    }
                    _bytes.AddRange(_CharacterData.DataInt);
                }
            }
            if (_bluePrintType == BlurPrintType.Outfits || _bluePrintType== BlurPrintType.AllAppearance)
            {
                _bytes.AddRange(_OutfitID);
                for (int i=0;i< _outfitSlotsCount;i++) {
                    _bytes.AddRange(_CusColor1[i].ToBytes());
                    _bytes.AddRange(_CusColor2[i].ToBytes());
                    _bytes.AddRange(_CusColor3[i].ToBytes());
                }
            }
            if (_bluePrintType == BlurPrintType.AllAppearance) {
                byte[] bytes = Encoding.ASCII.GetBytes(_Name);
                _bytes.AddRange(bytes);
            }
            return _bytes.ToArray();
        }

    }

    [System.Serializable]
    public class CharacterData
    {
        public byte Sex = 0;
        public byte Race=0;
        public byte[] DataFloat = new byte[120];
        public Uint8Color[] DataColor = new Uint8Color[15];
        public byte[] DataInt = new byte[10];
       

        public CharacterData Copy()
        {
            CharacterData _copy = new CharacterData();
            _copy.Sex = Sex;
            _copy.Race = Race;
            _copy.DataFloat = new byte[120];
            _copy.DataColor = new Uint8Color[15];
            _copy.DataInt = new byte[10];
            for (int i = 0; i < DataFloat.Length; i++)
            {
                _copy.DataFloat[i] = DataFloat[i];
            }
            for (int i = 0; i < DataColor.Length; i++)
            {
                _copy.DataColor[i] = new Uint8Color(DataColor[i].r, DataColor[i].g, DataColor[i].b, DataColor[i].a);
            }
            for (int i = 0; i < DataInt.Length; i++)
            {
                _copy.DataInt[i] = DataInt[i];
            }
            return _copy;
        }

        public void Random(Color[] _skinColors, Color[] _hairColors)
        {
            Race = (byte)Mathf.Max(UnityEngine.Random.Range(-10, CharacterDataSetting.instance.RaceSettings.Length),0);
            for (int i = 0; i < 120; i++)
            {
                if (i == 115 || i == 119)
                {
                    DataFloat[i] = (byte)UnityEngine.Random.Range(0, 100);
                }
                else if (i == 1 || i >= 108)
                {
                    DataFloat[i] = (byte)UnityEngine.Random.Range(30, 70);
                }
                else if (i == 107)
                {
                    DataFloat[i] = 50;
                }
                else if (i == 81 || i == 106)
                {
                    DataFloat[i] = (byte)UnityEngine.Random.Range(35, 65);
                }
                else
                {
                    DataFloat[i] = (byte)UnityEngine.Random.Range(20, 80);
                }
            }

            DataColor[0] = Uint8Color.Random();
            DataColor[1] = Uint8Color.Set(_skinColors[UnityEngine.Random.Range(0, _skinColors.Length)]);//skin
            DataColor[2] = Uint8Color.RandomOffset(DataColor[1], 30, 20, 20, 200);//lip
            DataColor[3] = Uint8Color.RandomOffset(DataColor[1], 20, 20, 0, 100);//makeup
            DataColor[4] = Uint8Color.Set(_hairColors[UnityEngine.Random.Range(0, _hairColors.Length)]);//hair
            DataColor[5] = Uint8Color.RandomOffset(DataColor[1], 80, 0, 80, 240);//Face Tattoo
            DataColor[6] = Uint8Color.RandomOffset(DataColor[1], 80, 0, 80, 240);//Body Tattoo
            DataColor[7] = Uint8Color.Random();//Eye
            DataColor[8] = Uint8Color.RandomOffset(DataColor[7], 30, 100, 50, 255);//Eye_Highlights_Color
            DataColor[9] = DataColor[4].Copy();//Eyebrows
            DataColor[9].a = (byte)UnityEngine.Random.Range(0, 200);

            int[] IntMaxMale = new int[10]{
                0,
                CharacterDataSetting.instance.MaleMaterialSettings[9].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[10].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[11].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[12].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[13].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[14].TextureIdMax+1,
                CharacterDataSetting.instance.MaleMaterialSettings[15].TextureIdMax+1,
                CharacterDataSetting.instance.MaleAccessorySettings[0].MaxValue+1,
                CharacterDataSetting.instance.MaleAccessorySettings[1].MaxValue+1
            };
            int[] IntMaxFemale = new int[10]{
                 0,
                CharacterDataSetting.instance.FemaleMaterialSettings[9].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[10].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[11].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[12].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[13].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[14].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleMaterialSettings[15].TextureIdMax+1,
                CharacterDataSetting.instance.FemaleAccessorySettings[0].MaxValue+1,
                CharacterDataSetting.instance.FemaleAccessorySettings[1].MaxValue+1
            };
            for (int i = 0; i < 10; i++)
            {
                DataInt[i] = (byte)UnityEngine.Random.Range(0, Sex == 0 ? IntMaxMale[i] : IntMaxFemale[i]);
            }
        }

        public CharacterData()
        {
            DataFloat = new byte[120];
            DataColor = new Uint8Color[15];
            DataInt = new byte[10];
            Race = 0;
            for (int i = 0; i < DataFloat.Length; i++)
            {
                DataFloat[i] = 50;
            }
            DataFloat[119] = 0;
            DataFloat[115] = 100;
            for (int i = 0; i < DataColor.Length; i++)
            {
                DataColor[i] = Uint8Color.Set(Color.white);
            }
            for (int i = 0; i < DataInt.Length; i++)
            {
                DataInt[i] = 0;
            }
            DataColor[1] = Uint8Color.Set(new Color(1F, 0.835F, 0.75F, 1F));
            DataInt[8] = 1;
        }
        public static CharacterData Create(byte _sex)
        {
            CharacterData _data = new CharacterData();
            _data.Sex = _sex;

            if (_sex == 0)
            {
                _data.DataColor[2] = Uint8Color.Set(new Color(1F, 0.5F, 0.45F, 1F));
                _data.DataColor[4] = Uint8Color.Set(new Color(0.6F, 0.4F, 0.25F, 1F));
                _data.DataColor[7] = Uint8Color.Set(new Color(0.6F, 1F, 1F, 0.7F));
                _data.DataColor[9] = Uint8Color.Set(new Color(0.6F, 0.4F, 0.25F, 1F));
                _data.DataInt[8] = 5;
            }
            else
            {
                _data.DataColor[2] = Uint8Color.Set(new Color(1F, 0.44F, 0.35F, 1F));
                _data.DataColor[3] = Uint8Color.Set(new Color(1F, 0.34F, 0.2F, 1F));
                _data.DataColor[4] = Uint8Color.Set(new Color(1F, 0.69F, 0.5F, 1F));
                _data.DataColor[7] = Uint8Color.Set(new Color(0.6F, 1F, 1F, 0.7F));
                _data.DataColor[9] = Uint8Color.Set(new Color(0.6F, 0.4F, 0.25F, 1F));
            }
            return _data;
        }
    }

    [System.Serializable]
    public class RaceSetting
    {
        public string RaceName = "Human";
        [Header("> Base Settings")]
        public int[] RaceMorph = new int[10] { 0,0,0,0,0,0,0,0,0,0};
        public int[] TeethMorph = new int[4] {0,0,0,0 };
        public Color DefaultSkinColor=Color.white;
        public Color DefaultEyeColor= Color.blue;
        public int DefaultEyeID = 0;
        
        [Header("> Ear Settings")]
        [Range(0, 100)]
        public int DefaultEarsShape = 50;
        [Range(0, 100)]
        public int DefaultEarsScale = 50;
        [Range(0, 100)]
        public int DefaultEarsTipOffset = 50;
        [Header("> Male Texture Settings")]
        public Texture MaleBodyColorTexture;
        public Texture MaleBodyNormalTexture;
        public Texture MaleHeadColorTexture;
        public Texture MaleHeadNormalTexture;
        public Texture MaleHandColorTexture;
        public Texture MaleFeetColorTexture;
        public Texture MaleHairTexture;
        [Header("> Female Texture Settings")]
        public Texture FemaleBodyColorTexture;
        public Texture FemaleBodyNormalTexture;
        public Texture FemaleHeadColorTexture;
        public Texture FemaleHeadNormalTexture;
        public Texture FemaleHandColorTexture;
        public Texture FemaleFeetColorTexture;
        public Texture FemaleHairTexture;
        [Header("> Accessories Settings")]
        public int DefaultMaleHeadAccessory = 0;
        public int DefaultFemaleHeadAccessory = 0;
        public int DefaultBackAccessory = 0;
        public int DefaultTail = 0;
        
    }

    #endregion

    #region Outfit Class
    [System.Serializable]
    public class OutfitColorSetting
    {
        public bool ToggleCustomColor1 = true;
        public Color DefaultColor1 = Color.white;
        public bool ToggleCustomColor2 = true;
        public Color DefaultColor2 = Color.white;
        public bool ToggleCustomColor3 = true;
        public Color DefaultColor3 = Color.white;
    }


    [System.Serializable]
    public class OutfitInfo
    {
        public string DisplayName;
        public Texture Icon;
        public OutfitSlots Slot;
        public string MeshPath;
        public string MaterialPath;
        public OutfitColorSetting ColorSetting;
        public bool HideHair = false;
        public bool HideFace = false;
        public bool PossibleIntersect = false;
        public int BreastMorphID = -1;
        public bool CanBeOverride = false;
        public bool OverrideTexture = false;
        public OutfitSlots OverrideSlot;
        public Texture OverrideMask1;
        public bool[] AvailableForRaces = new bool[10] { true, true, true, true, true, true, true, true, true, true };
        public string[] BoneNames;
        public OutfitInfo copy()
        {
            OutfitInfo _copy = new OutfitInfo();
            _copy.DisplayName = this.DisplayName;
            _copy.Slot = this.Slot;
            _copy.MeshPath = this.MeshPath;
            _copy.MaterialPath = this.MaterialPath;
            _copy.BoneNames = new string[this.BoneNames.Length];
            for (int i=0;i< BoneNames.Length;i++) {
                _copy.BoneNames[i] = this.BoneNames[i];
            }
            return _copy;
        }
     }


    [System.Serializable]
    public class AccessoryInfo
    {
        public string DisplayName;
        public CharacterDataSetting.CharacterTextureNames Slot;
        public string MatSlotName = "_Color";
        public string PrefabPath;
        public string ThumbPath;
        public CharacterDataSetting.CharacterColorNames ColorType;
        public string RootBoneNames;
        public int MinValue;
        public int MaxValue;
        public bool ZeroForNull=true;
        public string SkinnedRendererName = "";

        public AccessoryInfo copy()
        {
            AccessoryInfo _copy = new AccessoryInfo();
            _copy.DisplayName = this.DisplayName;
            _copy.Slot = this.Slot;
            _copy.MatSlotName = this.MatSlotName;
            _copy.PrefabPath = this.PrefabPath;
            _copy.ThumbPath = this.ThumbPath;
            _copy.ColorType = this.ColorType;
            _copy.RootBoneNames = this.RootBoneNames;
            _copy.MinValue = this.MinValue;
            _copy.MaxValue = this.MaxValue;
            _copy.ZeroForNull = this.ZeroForNull;
            return _copy;
        }
    }


    [System.Serializable]
    public class Uint8Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Uint8Color() { }
        public Uint8Color(byte [] _bytes)
        {
            r = _bytes[0];
            g = _bytes[1];
            b = _bytes[2];
            a = _bytes[3];
        }
        public Uint8Color(byte red,byte green,byte blue,byte alpha )
        {
            r = red;
            g = green;
            b = blue;
            a = alpha;
        }

        public Uint8Color Copy()
        {
            return new Uint8Color(r,g,b,a);
        }

        public static Uint8Color RandomOffset(Uint8Color _baseColor,byte _offsetMinimal, byte _offsetMaxuium, int _alphaMinimal=0, int _alphaMaxuium=255)
        {
            return new Uint8Color()
            {
                r = (byte)Mathf.FloorToInt(Mathf.Clamp(_baseColor.r + UnityEngine.Random.Range(-_offsetMinimal, _offsetMaxuium), 0, 255)),
                g = (byte)Mathf.FloorToInt(Mathf.Clamp(_baseColor.g + UnityEngine.Random.Range(-_offsetMinimal, _offsetMaxuium), 0, 255)),
                b = (byte)Mathf.FloorToInt(Mathf.Clamp(_baseColor.b + UnityEngine.Random.Range(-_offsetMinimal, _offsetMaxuium), 0, 255)),
                a = (byte)Mathf.FloorToInt(UnityEngine.Random.Range(_alphaMinimal, _alphaMaxuium))
            };
        }

        public static Uint8Color Random()
        {
            return new Uint8Color()
            {
                r = (byte)UnityEngine.Random.Range(0, 255),
                g = (byte)UnityEngine.Random.Range(0, 255),
                b = (byte)UnityEngine.Random.Range(0, 255),
                a = 255
            };
        }

        public static Uint8Color Set(Color _color)
        {
            return new Uint8Color() {
                r = (byte)Mathf.FloorToInt(_color.r * 255),
                g = (byte)Mathf.FloorToInt(_color.g * 255),
                b = (byte)Mathf.FloorToInt(_color.b * 255),
                a = (byte)Mathf.FloorToInt(_color.a * 255)
            };
        }

        public static Color Get(Uint8Color _color)
        {
            return new Color((_color.r*1F)/255F, (_color.g * 1F) / 255F, (_color.b * 1F) / 255F, (_color.a * 1F) / 255F);
        }

        public byte[] ToBytes()
        {
            return new byte[4] {r,g,b,a };
        }
    }

    #endregion

    #region Customization Class
    [System.Serializable]
    public class CharacterBoneDeformSetting
    {
        public string BoneNmae;
        public bool SetPos = false;
        public bool SetRot = false;
        public bool SetScale = false;
        public Vector3 OriPos;
        public Vector3 OriRot;
        public Vector3 OriScale;
        public CharacterBoneSlider[] PositionSliders;
        public CharacterBoneSlider[] RotationSliders;
        public CharacterBoneSlider[] ScaleSliders;
        public CharacterBoneDeformSetting copy()
        {
            CharacterBoneDeformSetting _copy = new CharacterBoneDeformSetting();
            _copy.BoneNmae = this.BoneNmae;
            _copy.SetPos = this.SetPos;
            _copy.SetRot = this.SetRot;
            _copy.SetScale = this.SetScale;
            _copy.OriPos = this.OriPos;
            _copy.OriRot = this.OriRot;
            _copy.OriScale = this.OriScale;
            _copy.PositionSliders =new CharacterBoneSlider [this.PositionSliders.Length];
            for (int i=0;i < _copy.PositionSliders.Length;i++) {
                _copy.PositionSliders[i] = this.PositionSliders[i].copy();
            }
            _copy.RotationSliders = new CharacterBoneSlider[this.RotationSliders.Length];
            for (int i = 0; i < _copy.RotationSliders.Length; i++)
            {
                _copy.RotationSliders[i] = this.RotationSliders[i].copy();
            }
            _copy.ScaleSliders = new CharacterBoneSlider[this.ScaleSliders.Length];
            for (int i = 0; i < _copy.ScaleSliders.Length; i++)
            {
                _copy.ScaleSliders[i] = this.ScaleSliders[i].copy();
            }

            return _copy;
        }
    }

    [System.Serializable]
    public class CharacterBoneSlider
    {
        public CharacterDataSetting.CharacterBoneSliderNames SliderType;
        public float DefaultValue;
        public float MinOffset;
        public float MaxOffset;
        public float Mutiplier = 1F;
        public CharacterBoneSlider copy()
        {
            CharacterBoneSlider _copy = new CharacterBoneSlider();
            _copy.SliderType = this.SliderType;
            _copy.DefaultValue = this.DefaultValue;
            _copy.MinOffset = this.MinOffset;
            _copy.MaxOffset = this.MaxOffset;
            _copy.Mutiplier = this.Mutiplier;
            return _copy;
        }
    }

    [System.Serializable]
    public class CharacterMaterialSetting
    {
        public string SettingName;
        public string[] RendererName;
        public string SlotName = "";
        public CharacterDataSetting.CharacterColorNames ColorType;
        public CharacterDataSetting.CharacterTextureNames TextureType;
        public CharacterDataSetting.CharacterBoneSliderNames FloatType;
        public string TextureBaseName = "";
        public int TextureIdMin = 0;
        public int TextureIdMax = 0;
        public float SliderMin = 0F;
        public float SliderMax = 1F;

        public CharacterMaterialSetting copy()
        {
            CharacterMaterialSetting _copy = new CharacterMaterialSetting();
            _copy.SettingName = this.SettingName;
            _copy.RendererName = new string[ this.RendererName.Length];
            for (int i = 0; i < _copy.RendererName.Length; i++)
            {
                _copy.RendererName[i] = this.RendererName[i];
            }
            _copy.SlotName = this.SlotName;
            _copy.ColorType = this.ColorType;
            _copy.TextureType = this.TextureType;
            _copy.FloatType = this.FloatType;
            _copy.TextureBaseName = this.TextureBaseName;
            _copy.TextureIdMin = this.TextureIdMin;
            _copy.TextureIdMax = this.TextureIdMax;
            _copy.SliderMin = this.SliderMin;
            _copy.SliderMax = this.SliderMax;
            return _copy;
        }
    }

    /// <summary>
    /// The EmotionSetting class is used to combine the head/eyelash/teeth morphs to create preset emotions.
    /// </summary>
    [System.Serializable]
    public class EmotionSetting
    {
        public bool fold = false;

        /// <summary>
        /// Unique string id, will be used when calling <CharacterEntity>().PlayEmotion(string _uid)
        /// </summary>
        public string uid = "";

        /// <summary>
        /// Morph settings of this emotion.
        /// </summary>
        public List<Vector2> HeadMorphs = new  List<Vector2>();

        /// <summary>
        /// Reset all morph settings of this emotion.
        /// </summary>
        public void Reset()
        {
            HeadMorphs.Clear();
        }


        /// <summary>
        /// Get the display name of an emotion by its id.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static string GetEmotionName(int _id)
        {
            if (_id < 0 || _id >= 16) return "None";
            return MorphName[_id];
        }

        /// <summary>
        /// Get the head blendshape value by its index.
        /// </summary>
        /// <param name="_morphIndex"></param>
        /// <returns></returns>
        public int GetHeadMorph(int _morphIndex)
        {
            if (_morphIndex < 0 || _morphIndex >= 22) return 0;
            for (int i=0;i< HeadMorphs.Count;i++) {
                if (HeadMorphMapping[Mathf.FloorToInt(HeadMorphs[i].x)] == _morphIndex) return Mathf.FloorToInt(HeadMorphs[i].y);
            }
            return 0;
        }

        /// <summary>
        /// Get the eyelash blendshape value by its index.
        /// </summary>
        /// <param name="_morphIndex"></param>
        /// <returns></returns>
        public int GetEyelashMorph(int _morphIndex)
        {
            if (_morphIndex < 0 || _morphIndex > 2) return 0;
            for (int i = 0; i < HeadMorphs.Count; i++)
            {
                if (EyelashMorphMapping[Mathf.FloorToInt(HeadMorphs[i].x)] == _morphIndex) return Mathf.FloorToInt(HeadMorphs[i].y);
            }
            return 0;
        }

        /// <summary>
        /// Get the teeth open blendshape value.
        /// </summary>
        /// <returns></returns>
        public int GetTeethOpen()
        {
            int _result = 0;
            for (int i = 0; i < HeadMorphs.Count; i++)
            {
                _result =Mathf.Max(_result,TeethMorphMapping[Mathf.FloorToInt(HeadMorphs[i].x)]);
            }
            return _result;
        }

        

        /// <summary>
        /// The head morph id of each emotion
        /// </summary>
        private static int[] HeadMorphMapping = new int[16]{
            0,
            21,
            4,
            5,
            19,
            20,
            2,
            3,
            1,
            6,
            7,
            8,
            9,
            10,
            11,
            12
            };

        /// <summary>
        /// The eyelash morph id of each emotion
        /// </summary>
        private static int[] EyelashMorphMapping = new int[16]{
            0,
            2,
            -1,
            -1,
            -1,
            -1,
            -1,
            1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1
            };
        /// <summary>
        /// How much the teeth open for each emotion
        /// </summary>
        private static int[] TeethMorphMapping = new int[16]{
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            100,
            38,
            22,
            28,
            100,
            100,
            0,
            25
            };
        /// <summary>
        /// The display name of each emotion
        /// </summary>
        public static string[] MorphName=new string[16] { 
        "Eye Close",//0
        "Eye Squint",//1
        "Eyebrow Down",//2
        "Eyebrow Up",//3
        "Angry",//4
        "Sad",//5
        "Happy",//6
        "Pain",//7
        "Open Mouth",//8
        "Viseme w",//9
        "Viseme th",//10
        "Viseme t",//11
        "Viseme ow",//12
        "Viseme oo",//13
        "Viseme f",//14
        "Viseme ee",//15
        };

        


    }

    #endregion

    public class CharacterDataSetting : MonoBehaviour
    {
       
        public static CharacterDataSetting instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) return ((GameObject)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/CharacterCreator/Core/CharacterData.prefab",typeof(GameObject))).GetComponent< CharacterDataSetting>();
#endif
                    _instance = Instantiate(Resources.Load<GameObject>("CharacterCreator/Core/CharacterData")).GetComponent<CharacterDataSetting>();
                }
                return _instance;
            }
        }
        private static CharacterDataSetting _instance;
        #region Settings
        public RaceSetting[] RaceSettings;

        public CharacterBoneDeformSetting[] MaleBoneSettings;
        public CharacterBoneDeformSetting[] FemaleBoneSettings;
        public CharacterMaterialSetting[] MaleMaterialSettings;
        public CharacterMaterialSetting[] FemaleMaterialSettings;

        public AccessoryInfo[] MaleAccessorySettings;
        public AccessoryInfo[] FemaleAccessorySettings;

        public OutfitInfo[] MaleArmorSetting;
        public OutfitInfo[] MalePantsSetting;
        public OutfitInfo[] MaleBootSetting;
        public OutfitInfo[] MaleGloveSetting;
        public OutfitInfo[] MaleHelmetSetting;

        public OutfitInfo[] FemaleArmorSetting;
        public OutfitInfo[] FemalePantsSetting;
        public OutfitInfo[] FemaleBootSetting;
        public OutfitInfo[] FemaleGloveSetting;
        public OutfitInfo[] FemaleHelmetSetting;
        #endregion

        #region Enum
        public enum CharacterBoneSliderNames
        {
            None,
            Head_Size,
            Upper_Face_Width,
            Upper_Face_Length,
            Upper_Face_Depth,
            Upper_Face_Height,
            Lower_Face_Width,
            Lower_Face_Length,
            Lower_Face_Depth,
            Lower_Face_Height,
            ForeHead_Width,
            ForeHead_Height,
            ForeHead_Depth,
            Cheek_Width,
            Cheek_Length,
            Cheek_Height,
            Cheek_Depth,
            Jaw_Width,
            Jaw_Height,
            Jaw_Depth,
            Chin_Width,
            Chin_Length,
            Chin_Height,
            Chin_Depth,
            Ears_Scale,
            Ears_Shape,
            Ear_Tips_Offset,
            Eyebrows_Depth,
            Eyebrows_Space,
            Outer_Eyebrows_Width,
            Outer_Eyebrows_Height,
            Inner_Eyebrows_Angle,
            Eyes_Width,
            Eyes_Length,
            Eyes_Depth,
            Eyes_Height,
            Pupils_Width,
            Pupils_Length,
            Retinas_Width,
            Retinas_Length,
            Upper_Eyelid_Height,
            Upper_Eyelid_Shape,
            Lower_Eyelid_Height,
            Lower_Eyelid_Shape,
            Inner_Eye_Corner_Height,
            Inner_Eye_Corner_Shape,
            Outer_Eye_Corner_Height,
            Outer_Eye_Corner_Shape,
            Nose_Width,
            Nose_Length,
            Nose_Depth,
            Nose_Height,
            Nose_Bridge_Width,
            Nose_Bridge_Depth,
            Nose_Tip_Width,
            Nose_Tip_Length,
            Nose_Tip_Height,
            Nose_Tip_Depth,
            Nose_Wing_Width,
            Nose_Wing_Length,
            Nose_Wing_Height,
            Nose_Wing_Depth,
            Mouth_Width,
            Mouth_Length,
            Mouth_Depth,
            Mouth_Height,
            Upper_Lip_Width,
            Upper_Lip_Length,
            Upper_Lip_Depth,
            Upper_Lip_Height,
            Lower_Lip_Width,
            lower_Lip_Length,
            Lower_Lip_Depth,
            Lower_Lip_Height,
            Mouth_Corners_Width,
            Mouth_Corners_Height,
            Mouth_Corners_Length,
            Mouth_Corners_Depth,
            Teeth_Width,
            Teeth_Height,
            Teeth_Depth,
            Body_Height,
            Neck_Width,
            Neck_Depth,
            Chest_Width,
            Chest_Length,
            Chest_Depth,
            Breast_Size,
            Stomach_Width,
            Stomach_Depth,
            Waist_Width,
            Waist_Length,
            Waist_Depth,
            Hip_Width,
            Hip_Depth,
            Ass_Width,
            Ass_Length,
            Ass_Depth,
            Shoulder_Width,
            Shoulder_Length,
            Shoulder_Depth,
            Upperarm_Size,
            Forearm_Size,
            Hand_Size,
            Thigh_Size,
            Calf_Size,
            Feet_Size,
            Toe_Size,
            Mouth_Underbite,
            Upper_Lip_Bite,
            Upper_Lip_Flip,
            Lower_Lip_Bite,
            Lower_Lip_Flip,
            Eye_Space,
            Retinas_Z,
            Muscular,
            Eyebrows_X,
            Eyebrows_Y,
            Eyebrows_Thickness,
            Age,
            Title_Head,
            Title_Upper_Face,
            Title_Lower_Face,
            Title_ForeHead,
            Title_Cheek,
            Title_Chin,
            Title_Jaw,
            Title_Ear,
            Title_Eyebrows,
            Title_Eyes,
            Title_Pupils,
            Title_Retinas,
            Title_Upper_Eyelid,
            Title_Lower_Eyelid,
            Title_Inner_Eye_Corner,
            Title_Outer_Eye_Corner,
            Title_Nose,
            Title_Nose_Bridge,
            Title_Nose_Tip,
            Title_Nose_Wing,
            Title_Mouth,
            Title_Upper_Lip,
            Title_Lower_Lip,
            Title_Mouth_Corners,
            Title_Teeth,
            Title_Body,
            Title_Neck,
            Title_Chest,
            Title_Breast,
            Title_Stomach,
            Title_Waist,
            Title_Hip,
            Title_Buttocks,
            Title_Shoulders,
            Title_Upperarms,
            Title_Forearms,
            Title_Hands,
            Title_Thighs,
            Title_Calfs,
            Title_Feet,
            Title_Toes
        };

        public enum CharacterColorNames
        {
            None,
            Skin_Color,
            Lip_Color,
            Makeup_Color,
            Hair_Color,
            Face_Tattoo_Color,
            Body_Tattoo_Color,
            Eye_Color,
            Eye_Highlights_Color,
            Eyebrows_Color
        }

        public enum CharacterTextureNames
        {
            None,
            Lip,
            Makeup,
            Face_Tattoo,
            Body_Tattoo,
            Eyes,
            Eye_Highlights,
            Eyebrows,
            HairID,
            BeardID,
            Back,
            Tail
        }
        #endregion

        public OutfitColorSetting GetOutfitColorSetting(Sex _sex,OutfitSlots _slot, int _id)
        {
            return GetOutfitSettings(_sex, _slot)[_id].ColorSetting;
        }

        public OutfitInfo [] GetOutfitSettings(Sex _sex, OutfitSlots _slot)
        {
            switch (_slot)
            {
                default:
                    return _sex == Sex.Female ? FemaleArmorSetting : MaleArmorSetting;
                case OutfitSlots.Helmet:
                    return _sex == Sex.Female ? FemaleHelmetSetting: MaleHelmetSetting;
                case OutfitSlots.Gauntlet:
                    return _sex == Sex.Female ? FemaleGloveSetting: MaleGloveSetting;
                case OutfitSlots.Boot:
                    return _sex == Sex.Female ? FemaleBootSetting: MaleBootSetting;
                case OutfitSlots.Pants:
                    return _sex == Sex.Female ? FemalePantsSetting : MalePantsSetting;
                case OutfitSlots.Back:
                    OutfitInfo [] _infosBack = new OutfitInfo[(_sex == Sex.Female ? instance.FemaleAccessorySettings[2].MaxValue:instance.MaleAccessorySettings[2].MaxValue)+1];
                    string _iconPathBack = _sex == Sex.Female ? instance.FemaleAccessorySettings[2].ThumbPath : instance.MaleAccessorySettings[2].ThumbPath;
                    string _modelPathBack = _sex == Sex.Female ? instance.FemaleAccessorySettings[2].PrefabPath : instance.MaleAccessorySettings[2].PrefabPath;
                    for (int i = 0; i < _infosBack.Length; i++) {
                        _infosBack[i]= new OutfitInfo();
                        _infosBack[i].DisplayName = i == 0 ? "None" : _sex.ToString() + " Back " + i.ToString();
#if UNITY_EDITOR
                        if (i == 0)
                            _infosBack[i].Icon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/CharacterCreator/Icons/Empty.png", typeof(Texture));
                        else
                            _infosBack[i].Icon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/" + _iconPathBack + i.ToString() + ".png", typeof(Texture));

#else
                        if (i == 0)
                            _infosBack[i].Icon = Resources.Load<Texture>("Assets/CharacterCreator/Resources/CharacterCreator/Icons/Empty");
                        else
                            _infosBack[i].Icon = Resources.Load<Texture>(_iconPathBack + i.ToString());
#endif
                        _infosBack[i].MeshPath = _modelPathBack + i.ToString("00");
                        _infosBack[i].MaterialPath = "";
                        _infosBack[i].ColorSetting = new OutfitColorSetting();
                        _infosBack[i].ColorSetting.ToggleCustomColor1 = true;
                        _infosBack[i].ColorSetting.ToggleCustomColor2 = true;
                        _infosBack[i].ColorSetting.ToggleCustomColor3 = true;
                        _infosBack[i].ColorSetting.DefaultColor1 = Color.white;
                        _infosBack[i].ColorSetting.DefaultColor2 = Color.white;
                        _infosBack[i].ColorSetting.DefaultColor3 = Color.white;
                        _infosBack[i].Slot = OutfitSlots.Back;
                    }
                    return _infosBack;
                case OutfitSlots.Tail:
                    OutfitInfo[] _infosTail = new OutfitInfo[(_sex == Sex.Female ? instance.FemaleAccessorySettings[3].MaxValue : instance.MaleAccessorySettings[3].MaxValue)+1];
                    string _iconPathTail = _sex == Sex.Female ? instance.FemaleAccessorySettings[3].ThumbPath : instance.MaleAccessorySettings[3].ThumbPath;
                    string _modelPathTail = _sex == Sex.Female ? instance.FemaleAccessorySettings[3].PrefabPath : instance.MaleAccessorySettings[3].PrefabPath;
                    for (int i = 0; i < _infosTail.Length; i++)
                    {
                        _infosTail[i] = new OutfitInfo();
                        _infosTail[i].DisplayName =i==0?"None": _sex.ToString() + " Tail " + i.ToString() ;
#if UNITY_EDITOR
                        if (i == 0)
                            _infosTail[i].Icon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/CharacterCreator/Icons/Empty.png", typeof(Texture));
                        else
                            _infosTail[i].Icon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/CharacterCreator/Resources/" + _iconPathTail + i.ToString() + ".png", typeof(Texture));

#else
                        if (i == 0)
                            _infosTail[i].Icon = Resources.Load<Texture>("Assets/CharacterCreator/Resources/CharacterCreator/Icons/Empty");
                        else
                            _infosTail[i].Icon = Resources.Load<Texture>(_iconPathTail + i.ToString());

#endif
                        _infosTail[i].MeshPath = _modelPathTail + i.ToString("00") ;
                        _infosTail[i].MaterialPath = "";
                        _infosTail[i].ColorSetting = new OutfitColorSetting();
                        _infosTail[i].ColorSetting.ToggleCustomColor1 = true;
                        _infosTail[i].ColorSetting.ToggleCustomColor2 = true;
                        _infosTail[i].ColorSetting.ToggleCustomColor3 = true;
                        _infosTail[i].ColorSetting.DefaultColor1 = Color.white;
                        _infosTail[i].ColorSetting.DefaultColor2 = Color.white;
                        _infosTail[i].ColorSetting.DefaultColor3 = Color.white;
                        _infosTail[i].Slot = OutfitSlots.Tail;
                    }
                    return _infosTail;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

    }


}