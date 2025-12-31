using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This script is to apply the appearance data and facial expressions to the character.
/// </summary>

namespace Game.CharacterCreator
{
    public class CharacterBoneControl : MonoBehaviour
    {
        
        [Header("Don't change this on the prefabs from package.")]
        public int DefaultSex = 0;
        [Header("AutoInittialize will inittialize this character with default appearance on start.")]
        public bool AutoInittialize = false;
        [Header("Ignore height setting and make all characters same height.")]
        public bool ForceHeightEven = false;
        [Header("Make the appearance sliders have wider range.")]
        public float SliderMutiplier = 0F;
        [Header("Make the character randomly blink.")]
        public bool AutoBlink = true;
        [Range(0F,100F)]
        public float EyeOpen = 100F;
       
        [Header("Disable this if you want other system to manage blend shapes, such as LipSync system.")]
        public bool ManageBlendShapes = true;


        #region ComponentsReference
        [Header("Components Reference")]
        public SkinnedMeshRenderer EyesRenderer;
        public SkinnedMeshRenderer EyeShadowsRenderer;
        public SkinnedMeshRenderer HeadRenderer;
        public SkinnedMeshRenderer TeethRenderer;
        public SkinnedMeshRenderer BodyRenderer;
        public SkinnedMeshRenderer PantsRenderer;
        public SkinnedMeshRenderer FeetRenderer;
        public SkinnedMeshRenderer HandsRenderer;
        public SkinnedMeshRenderer HelmetRenderer;
        public SkinnedMeshRenderer EyelashRenderer;
        public Transform [] Bones;
        #endregion


        #region variables
        [HideInInspector]
        public CharacterAppearance MyData;
        public Dictionary<string, Transform> BoneDictionary = new Dictionary<string, Transform>();
        [HideInInspector]
        public Animation BackAnimation;
        [HideInInspector]
        public Animation TailAnimation;
        [HideInInspector]
        public Animation HeadAccAnimation;

        [HideInInspector]
        public float mHeight = 1F;
        [HideInInspector]
        public Transform LookAtTarget;
        [HideInInspector]
        public string EditorPath = "";
        public Color RimColor
        {
            get => _rimColor;
            set
            {
                if (!Application.isPlaying) return;
                _rimColor = value;
                foreach (var obj in RendererMapping.Values)
                {
                    if (obj != null) obj.material.SetColor("_OutlineRimColor", _rimColor);
                }
            }
        }
        private Color _rimColor;
        public Dictionary<string, GameObject> AccessoryObjs = new Dictionary<string, GameObject>();
        public Dictionary<string, int> AccessoryIDs = new Dictionary<string, int>();
        private Dictionary<string, Renderer> MyRenderer = new Dictionary<string, Renderer>();
        private Dictionary<OutfitSlots, Renderer> RendererMapping = new Dictionary<OutfitSlots, Renderer>();
       
        public bool Inited = false;
        public bool isInited
        {
            get
            {
                return Inited;
            }
        }
        private bool InternalInited = false;
        private bool ShowHair = true;
        private bool ShowFace = true;
        private float BlinkValue = 0F;
        private int[] OldOutfitID;
        private int PhotoMode = 0;
        private float BlinkTimer =5F;
        private Uint8Color[] OldCusColor1;
        private Uint8Color[] OldCusColor2;
        private Uint8Color[] OldCusColor3;
        private Coroutine BlinkCo;
        private int OutfitSlotsCount;
        private KeyValuePair<string, float> ActiveEmotion = new KeyValuePair<string, float>();
        #endregion

        #region internal methods
        private void Awake()
        {
            InternalInit();
        }

        public void InternalInit()
        {
            MyRenderer.Clear();
            ActiveEmotion = new KeyValuePair<string, float>("null", 0F);
            RendererMapping.Clear();
            RendererMapping.Add(OutfitSlots.Armor, BodyRenderer);
            RendererMapping.Add(OutfitSlots.Helmet, HelmetRenderer);
            RendererMapping.Add(OutfitSlots.Gauntlet, HandsRenderer);
            RendererMapping.Add(OutfitSlots.Boot, FeetRenderer);
            RendererMapping.Add(OutfitSlots.Pants, PantsRenderer);

            BoneDictionary.Clear();
            foreach (Transform trans in Bones)
            {
                if (trans.tag == "Bone")
                {
                    BoneDictionary.Add(trans.name, trans);
                }
            }
            OutfitSlotsCount = System.Enum.GetValues(typeof(OutfitSlots)).Length;
            OldOutfitID = new int[OutfitSlotsCount];
            for (int i = 0; i < OutfitSlotsCount; i++)
            {
                OldOutfitID[i] = 0;
            }
            OldCusColor1 = new Uint8Color[OutfitSlotsCount];
            OldCusColor2 = new Uint8Color[OutfitSlotsCount];
            OldCusColor3 = new Uint8Color[OutfitSlotsCount];
            for (int i = 0; i < OutfitSlotsCount; i++)
            {
                OldCusColor1[i] = Uint8Color.Set(Color.white);
                OldCusColor2[i] = Uint8Color.Set(Color.white);
                OldCusColor3[i] = Uint8Color.Set(Color.white);
            }
            InternalInited = true;
        }

        private void UpdateRace()
        {
            if (CharacterDataSetting.instance == null) return;
            if (Application.isEditor && !Application.isPlaying)
            {
                HeadRenderer.sharedMaterials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHeadColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHeadColorTexture);
                HeadRenderer.sharedMaterials[0].SetTexture("_MaskMap3",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHairTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHairTexture);
                HeadRenderer.sharedMaterials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHeadNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHeadNormalTexture);
                BodyRenderer.sharedMaterials[0].SetTexture("_MainTex",
                   MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyColorTexture);
                BodyRenderer.sharedMaterials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyNormalTexture);
                PantsRenderer.sharedMaterials[0].SetTexture("_MainTex",
                  MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyColorTexture);
                PantsRenderer.sharedMaterials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyNormalTexture);
                HandsRenderer.sharedMaterials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHandColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHandColorTexture);
                FeetRenderer.sharedMaterials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleFeetColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleFeetColorTexture);
            }
            else
            {
                HeadRenderer.materials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHeadColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHeadColorTexture);
                HeadRenderer.materials[0].SetTexture("_MaskMap3",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHairTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHairTexture);
                HeadRenderer.materials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHeadNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHeadNormalTexture);
                BodyRenderer.materials[0].SetTexture("_MainTex",
                   MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyColorTexture);
                BodyRenderer.materials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyNormalTexture);
                PantsRenderer.materials[0].SetTexture("_MainTex",
                  MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyColorTexture);
                PantsRenderer.materials[0].SetTexture("_BodyNormal",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleBodyNormalTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleBodyNormalTexture);
                HandsRenderer.materials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleHandColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleHandColorTexture);
                FeetRenderer.materials[0].SetTexture("_MainTex",
                    MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].MaleFeetColorTexture : CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].FemaleFeetColorTexture);
            }
            for (int i = 0; i < CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].RaceMorph.Length; i++)
            {
                HeadRenderer.SetBlendShapeWeight(i + 13, CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].RaceMorph[i]);
            }
            for (int i = 0; i < CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].TeethMorph.Length; i++)
            {
                TeethRenderer.SetBlendShapeWeight(i + 1, CharacterDataSetting.instance.RaceSettings[MyData._CharacterData.Race].TeethMorph[i]);
            }

        }

        private void SetOutfitColor(OutfitSlots _slot)
        {
            OldCusColor1[(int)_slot] = MyData._CusColor1[(int)_slot];
            OldCusColor2[(int)_slot] = MyData._CusColor2[(int)_slot];
            OldCusColor3[(int)_slot] = MyData._CusColor3[(int)_slot];
            if ((int)_slot >= 5)
            {
                if (AccessoryObjs.ContainsKey(_slot.ToString()+ "Type"))
                {
                    foreach (Renderer obj in AccessoryObjs[_slot.ToString() + "Type"].GetComponentsInChildren<Renderer>(true))
                    {
                        if (Application.isEditor && !Application.isPlaying )
                        {
                            if (obj.sharedMaterial != null)
                            {
                                obj.sharedMaterial.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)_slot]));
                                obj.sharedMaterial.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)_slot]));
                                obj.sharedMaterial.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)_slot]));
                            }
                        }
                        else 
                        {
                            if (obj.material != null)
                            {
                                obj.material.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)_slot]));
                                obj.material.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)_slot]));
                                obj.material.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)_slot]));
                            }
                        }
                    }
                }
            }
            else
            {
                if (RendererMapping.ContainsKey(_slot) && RendererMapping[_slot] != null)
                {
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        if (RendererMapping[_slot].sharedMaterial != null)
                        {
                            RendererMapping[_slot].sharedMaterial.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)_slot]));
                            RendererMapping[_slot].sharedMaterial.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)_slot]));
                            RendererMapping[_slot].sharedMaterial.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)_slot]));
                        }
                    }
                    else
                    {
                        if (RendererMapping[_slot].material != null)
                        {
                            RendererMapping[_slot].material.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)_slot]));
                            RendererMapping[_slot].material.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)_slot]));
                            RendererMapping[_slot].material.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)_slot]));
                        }
                    }
                }
            }
        }

       


        private OutfitInfo GetOutfitSetting(OutfitSlots _slot, int _id, int _sex)
        {
            switch (_slot)
            {
                case OutfitSlots.Gauntlet: return _sex == 0 ? CharacterDataSetting.instance.MaleGloveSetting[_id] : CharacterDataSetting.instance.FemaleGloveSetting[_id];
                case OutfitSlots.Boot: return _sex == 0 ? CharacterDataSetting.instance.MaleBootSetting[_id] : CharacterDataSetting.instance.FemaleBootSetting[_id];
                case OutfitSlots.Helmet: return _sex == 0 ? CharacterDataSetting.instance.MaleHelmetSetting[_id] : CharacterDataSetting.instance.FemaleHelmetSetting[_id];
                case OutfitSlots.Pants: return _sex == 0 ? CharacterDataSetting.instance.MalePantsSetting[_id] : CharacterDataSetting.instance.FemalePantsSetting[_id];
                default: return _sex == 0 ? CharacterDataSetting.instance.MaleArmorSetting[_id] : CharacterDataSetting.instance.FemaleArmorSetting[_id];
            }
        }

        private void Start()
        {
            if (AutoInittialize && !Inited) Initialize(new CharacterAppearance(CharacterData.Create((byte)DefaultSex)));
        }

        private void CheckHairVisibility()
        {
            if (AccessoryObjs.ContainsKey("HairType"))
            {
                AccessoryObjs["HairType"].SetActive(ShowHair);
            }
            if (AccessoryObjs.ContainsKey("BeardType"))
            {
                AccessoryObjs["BeardType"].SetActive(ShowHair);
            }
        }

        private void OnDestroy()
        {
           
        }
        private void LoadSkinnedMesh(SkinnedMeshRenderer _renderer, string _meshPath, string _matPath, string[] boneNames)
        {
            if (!InternalInited) InternalInit();
            if (_meshPath.Length > 0 && _matPath.Length > 0)
            {
                _renderer.enabled = false;
                _renderer.sharedMesh = CharacterManager.LoadMesh(_meshPath);
                if (Application.isEditor && !Application.isPlaying)
                {
                   if(CharacterManager.LoadMaterial(_matPath)!=null) _renderer.sharedMaterial = CharacterManager.LoadMaterial(_matPath);
                }
                else{
                    _renderer.material = CharacterManager.LoadMaterial(_matPath);
                }


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


        private void Update()
        {
            if (!Inited) return;
            CheckEmotions();
            CheckOutfits();
            CheckPhotoMode();
            CheckLookAt();
        }

      

        private void CheckLookAt()
        {
            if (!InternalInited) InternalInit();
            if (LookAtTarget != null)
            {
                Vector3 _dirL = (LookAtTarget.position - BoneDictionary["LookAtL"].position).normalized;
                if (Vector3.Angle(_dirL, BoneDictionary["Bip001 Head"].up) < (DefaultSex==0?20F:12F))
                {
                    Quaternion _lookRotL = Quaternion.LookRotation( _dirL, Vector3.up);
                    BoneDictionary["LookAtL"].rotation = Quaternion.RotateTowards(BoneDictionary["LookAtL"].rotation, _lookRotL, 300F * Time.deltaTime);
                }
                Vector3 _dirR = (LookAtTarget.position - BoneDictionary["LookAtR"].position).normalized;
                if (Vector3.Angle(_dirR, BoneDictionary["Bip001 Head"].up) < (DefaultSex == 0 ? 20F : 12F))
                {
                    Quaternion _lookRotR = Quaternion.LookRotation(_dirR, Vector3.up);
                    BoneDictionary["LookAtR"].rotation = Quaternion.RotateTowards(BoneDictionary["LookAtR"].rotation, _lookRotR, 300F * Time.deltaTime);
                }
            }
            else
            {
                BoneDictionary["LookAtL"].localRotation = Quaternion.RotateTowards(BoneDictionary["LookAtL"].localRotation, Quaternion.identity, Time.deltaTime * 900F);
                BoneDictionary["LookAtR"].localRotation = Quaternion.RotateTowards(BoneDictionary["LookAtR"].localRotation, Quaternion.identity, Time.deltaTime * 900F);
            }
        }

        private void CheckPhotoMode()
        {
            if (HeadRenderer == null || !Application.isPlaying) return;
            if (PhotoMode != CharacterManager.PhotoMode)
            {
                PhotoMode = CharacterManager.PhotoMode;
                if (PhotoMode == 0)
                {
                    UpdateMaterialData();
                }
                else
                {
                    HeadRenderer.materials[0].SetColor("_Color1", new Color(0F, 0F, 0F, 0F));
                    HeadRenderer.materials[0].SetColor("_Color2", new Color(0F, 0F, 0F, 0F));
                    HeadRenderer.materials[0].SetColor("_Color3", new Color(0F, 0F, 0F, 0F));
                    HeadRenderer.materials[1].SetColor("_BaseColor", new Color(0F, 0F, 0F, 0F));
                }
            }
        }
        private void CheckOutfits()
        {
            if (MyData == null || MyData._OutfitID == null || MyData._CusColor1 == null || MyData._CusColor2 == null || MyData._CusColor3 == null)
                return;
            var count = Mathf.Min(OldOutfitID.Length, MyData._OutfitID.Length, OldCusColor1.Length, MyData._CusColor1.Length, OldCusColor2.Length, MyData._CusColor2.Length, OldCusColor3.Length, MyData._CusColor3.Length);
            for (int i = 0; i < count; i++)
            {
                if (OldOutfitID[i] != MyData._OutfitID[i]) SwitchOutfit((OutfitSlots)i, MyData._OutfitID[i]);
                if (OldCusColor1[i] != MyData._CusColor1[i] || OldCusColor2[i] != MyData._CusColor2[i] || OldCusColor3[i] != MyData._CusColor3[i]) SetOutfitColor((OutfitSlots)i);
            }
        }

        private void CheckEmotions()
        {
            if (HeadRenderer != null)
            {
                if (HeadRenderer.enabled != ShowFace) HeadRenderer.enabled = ShowFace;
                if (TeethRenderer.enabled != ShowFace) TeethRenderer.enabled = ShowFace;
                if (EyesRenderer.enabled != ShowFace) EyesRenderer.enabled = ShowFace;
                if (EyeShadowsRenderer.enabled != ShowFace) EyeShadowsRenderer.enabled = ShowFace;


                if (ActiveEmotion.Key != "null")
                {
                    ActiveEmotion = new KeyValuePair<string, float>(ActiveEmotion.Key, Mathf.MoveTowards(ActiveEmotion.Value, 0F, Time.deltaTime));
                    if (ActiveEmotion.Value <= 0F) ActiveEmotion = new KeyValuePair<string, float>("null",0F);
                }

                for (int i = 1; i <= 21; i++)
                {
                    if ((ManageBlendShapes || i < 6 || i > 12) && (i<13 || i>18))
                    {

                        HeadRenderer.SetBlendShapeWeight(i, Mathf.MoveTowards(HeadRenderer.GetBlendShapeWeight(i),
                            CharacterManager.CharacterEmotions.ContainsKey(ActiveEmotion.Key) ? CharacterManager.CharacterEmotions[ActiveEmotion.Key].GetHeadMorph(i) : 0F, Time.deltaTime * 300F));
                    }
                }

                TeethRenderer.SetBlendShapeWeight(0, Mathf.MoveTowards(TeethRenderer.GetBlendShapeWeight(0),
                        CharacterManager.CharacterEmotions.ContainsKey(ActiveEmotion.Key) ? CharacterManager.CharacterEmotions[ActiveEmotion.Key].GetTeethOpen() : 0F, Time.deltaTime * 300F));


                float _eyeClose = Mathf.Clamp((CharacterManager.CharacterEmotions.ContainsKey(ActiveEmotion.Key) ? CharacterManager.CharacterEmotions[ActiveEmotion.Key].GetHeadMorph(0) : 0F)+ BlinkValue + (100F - EyeOpen), 0F, 100F);
                HeadRenderer.SetBlendShapeWeight(0, Mathf.MoveTowards(HeadRenderer.GetBlendShapeWeight(0), _eyeClose, Time.deltaTime * 200F));
                if (EyelashRenderer)
                {
                    EyelashRenderer.SetBlendShapeWeight(0, HeadRenderer.GetBlendShapeWeight(0));
                    EyelashRenderer.SetBlendShapeWeight(1, HeadRenderer.GetBlendShapeWeight(3));
                    EyelashRenderer.SetBlendShapeWeight(2, HeadRenderer.GetBlendShapeWeight(21));
                }
                if (AutoBlink)
                {
                    BlinkTimer = Mathf.MoveTowards(BlinkTimer, 0F, Time.deltaTime);
                    if (BlinkTimer <= 0F)
                    {
                        BlinkTimer = Random.Range(5F, 10F);
                        Blink();
                    }
                }
               
            }
        }

        private void LateUpdate()
        {
            BoneTransform();
        }

        private void BoneTransform()
        {
            if (CharacterDataSetting.instance == null) return;
            if (!InternalInited) InternalInit();
            foreach (CharacterBoneDeformSetting obj in MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleBoneSettings : CharacterDataSetting.instance.FemaleBoneSettings)
            {
                if (BoneDictionary.ContainsKey(obj.BoneNmae))
                {
                    ApplyBoneData(BoneDictionary[obj.BoneNmae], obj);
                }
                else
                {
                    BoneDictionary.Add(obj.BoneNmae, FindAllChild(transform, obj.BoneNmae));
                    ApplyBoneData(BoneDictionary[obj.BoneNmae], obj);
                }
            }
            mHeight = (ForceHeightEven ? 1F : ((MyData._CharacterData.Sex == 0 ? 0.7F : 0.8F) + MyData._CharacterData.DataFloat[81] * 0.004F));
            transform.localScale = Vector3.one * mHeight;
        }

        IEnumerator DoBlink()
        {
            while (BlinkValue < 100F)
            {
                BlinkValue += Time.deltaTime * 500F;
            }
            yield return new WaitForSeconds(0.3F);
            while (BlinkValue >0F)
            {
                BlinkValue = Mathf.MoveTowards(BlinkValue, 0F, Time.deltaTime * 200F);
            }
        }

        private void LoadAccessories()
        {
            if (!InternalInited) InternalInit();
            foreach (AccessoryInfo setting in MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleAccessorySettings : CharacterDataSetting.instance.FemaleAccessorySettings)
            {
                int _id;
                if ((int)setting.Slot > 9)
                {
                    var outfitIndex = (int)setting.Slot - 5;
                    if (MyData._OutfitID == null || outfitIndex < 0 || outfitIndex >= MyData._OutfitID.Length)
                    {
                        Debug.LogWarning($"CharacterBoneControl: OutfitID index out of range for slot {setting.Slot}.");
                        continue;
                    }
                    _id = MyData._OutfitID[outfitIndex];
                }
                else
                {
                    var dataIndex = (int)setting.Slot;
                    if (MyData._CharacterData == null || MyData._CharacterData.DataInt == null || dataIndex < 0 || dataIndex >= MyData._CharacterData.DataInt.Length)
                    {
                        Debug.LogWarning($"CharacterBoneControl: DataInt index out of range for slot {setting.Slot}.");
                        continue;
                    }
                    _id = MyData._CharacterData.DataInt[dataIndex];
                }
                bool _load = false;
                if (AccessoryIDs.ContainsKey(setting.DisplayName))
                {
                    if (AccessoryIDs[setting.DisplayName] != _id) _load = true;
                }
                else
                {
                    _load = true;
                }

                if (_load)
                {
                    if (AccessoryObjs.ContainsKey(setting.DisplayName))
                    {
                        if (AccessoryObjs[setting.DisplayName] != null)
                        {
                            if (AccessoryObjs[setting.DisplayName].name != setting.SkinnedRendererName)
                            {
                                DestroyImmediate(AccessoryObjs[setting.DisplayName]);
                            }
                        }
                        AccessoryObjs.Remove(setting.DisplayName);
                    }
                    if (AccessoryIDs.ContainsKey(setting.DisplayName)) AccessoryIDs.Remove(setting.DisplayName);
                    if (setting.SkinnedRendererName != "")
                    {
                        FindAllChild(transform, setting.SkinnedRendererName).GetComponent<SkinnedMeshRenderer>().enabled = false;
                    }
                    if (_id > 0 || !setting.ZeroForNull)
                    {
                        GameObject _newAccObj = Instantiate(CharacterManager.LoadObject(setting.PrefabPath + _id.ToString("00")), BoneDictionary[setting.RootBoneNames]);
                        if (setting.Slot == CharacterDataSetting.CharacterTextureNames.Back)
                            BackAnimation = _newAccObj.GetComponentInChildren<Animation>();
                        else if (setting.Slot == CharacterDataSetting.CharacterTextureNames.Tail)
                            TailAnimation = _newAccObj.GetComponentInChildren<Animation>();
                        else if (setting.Slot == CharacterDataSetting.CharacterTextureNames.BeardID)
                            HeadAccAnimation = _newAccObj.GetComponentInChildren<Animation>();

#if UNITY_EDITOR
                        foreach (var obj in _newAccObj.GetComponentsInChildren<Renderer>(true))
                        {
                            CreateEditorMaterialCopy(obj);
                        }
#endif

                        _newAccObj.transform.localPosition = Vector3.zero;
                        _newAccObj.transform.localEulerAngles = Vector3.zero;
                        _newAccObj.transform.localScale = Vector3.one;
                        if (_newAccObj.tag == "Skinned" || _newAccObj.GetComponentInChildren<SkinnedMeshRenderer>())
                        {
                            if (setting.SkinnedRendererName != "")
                            {
                                SkinnedMeshRenderer _proxyRenderer = FindAllChild(transform, setting.SkinnedRendererName).GetComponent<SkinnedMeshRenderer>();
                                SkinnedMeshRenderer _accRenderer = _newAccObj.GetComponentInChildren<SkinnedMeshRenderer>();
                                Transform[] _bones = new Transform[_accRenderer.bones.Length];
                                for (int i = 0; i < _bones.Length; i++)
                                {
                                    _bones[i] = BoneDictionary[_accRenderer.bones[i].name];
                                }
                                _proxyRenderer.sharedMesh = _accRenderer.sharedMesh;
                                if (Application.isEditor && !Application.isPlaying)
                                {
                                    _proxyRenderer.sharedMaterial = _accRenderer.sharedMaterial;
                                }
                                else
                                {
                                    _proxyRenderer.material = _accRenderer.sharedMaterial;
                                }
                                _proxyRenderer.rootBone = BoneDictionary[_accRenderer.rootBone.name];
                                _proxyRenderer.bones = _bones;
                                _proxyRenderer.enabled = true;
                                _proxyRenderer.ResetBounds();
                                _newAccObj.transform.SetParent(_proxyRenderer.transform);
                                _newAccObj.SetActive(false);
                                _newAccObj = _proxyRenderer.gameObject;
                            }
                            else
                            {
                                SkinnedMeshRenderer _accRenderer = _newAccObj.GetComponentInChildren<SkinnedMeshRenderer>();
                                Transform[] _bones = new Transform[_accRenderer.bones.Length];
                                for (int i = 0; i < _bones.Length; i++)
                                {
                                    if (BoneDictionary.ContainsKey(_accRenderer.bones[i].name)) {
                                        _bones[i] = BoneDictionary[_accRenderer.bones[i].name];
                                    }
                                    else {
                                        _bones[i] = _accRenderer.bones[i];
                                        if (BoneDictionary.ContainsKey(_accRenderer.bones[i].parent.name)) {
                                            Vector3 _localPos = _accRenderer.bones[i].localPosition;
                                            Quaternion _localRot= _accRenderer.bones[i].localRotation;
                                            Vector3 _localScale = _accRenderer.bones[i].localScale;
                                            _accRenderer.bones[i].SetParent(BoneDictionary[_accRenderer.bones[i].parent.name],false);
                                            _accRenderer.bones[i].localPosition= _localPos;
                                            _accRenderer.bones[i].localRotation= _localRot;
                                            _accRenderer.bones[i].localScale= _localScale;
                                        }
                                    }
                                }
                                _accRenderer.bones = _bones;
                                if (BoneDictionary.ContainsKey(_accRenderer.rootBone.name)) _accRenderer.rootBone = BoneDictionary[_accRenderer.rootBone.name];
                                _newAccObj.transform.SetParent(transform);
                                _newAccObj.transform.localPosition = Vector3.zero;
                                _newAccObj.transform.localEulerAngles = Vector3.zero;
                                _newAccObj.transform.localScale = Vector3.one;
                            }
                        }
                        AccessoryObjs.Add(setting.DisplayName, _newAccObj);
                        AccessoryIDs.Add(setting.DisplayName, _id);
                    }
                }

                if (AccessoryObjs.ContainsKey(setting.DisplayName))
                {
                    foreach (Renderer obj in AccessoryObjs[setting.DisplayName].GetComponentsInChildren<Renderer>(true))
                    {
                        if ((int)setting.Slot > 9)
                        {
                            if (Application.isEditor && !Application.isPlaying)
                            {
                                obj.sharedMaterial.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)setting.Slot - 5]));
                                obj.sharedMaterial.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)setting.Slot - 5]));
                                obj.sharedMaterial.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)setting.Slot - 5]));
                            }
                            else
                            {
                                obj.material.SetColor("_Color1", Uint8Color.Get(MyData._CusColor1[(int)setting.Slot - 5]));
                                obj.material.SetColor("_Color2", Uint8Color.Get(MyData._CusColor2[(int)setting.Slot - 5]));
                                obj.material.SetColor("_Color3", Uint8Color.Get(MyData._CusColor3[(int)setting.Slot - 5]));
                            }
                        }
                        else
                        {
                            if (Application.isEditor && !Application.isPlaying)
                            {
                                obj.sharedMaterial.SetColor(setting.MatSlotName, Uint8Color.Get(MyData._CharacterData.DataColor[(int)setting.ColorType]));
                            }
                            else
                            {
                                obj.material.SetColor(setting.MatSlotName, Uint8Color.Get(MyData._CharacterData.DataColor[(int)setting.ColorType]));
                            }
                        }
                    }
                }
            }
            CheckHairVisibility();
        }

        private void ApplyMaterialData(Renderer _renderer, CharacterMaterialSetting _data, int _id)
        {

            if (_renderer == null) return;
            Material _mat = (Application.isEditor && !Application.isPlaying) ? _renderer.sharedMaterials[_id] : _renderer.materials[_id];

            if (_data.ColorType != CharacterDataSetting.CharacterColorNames.None)
            {
                _mat.SetColor(_data.SlotName, Uint8Color.Get(MyData._CharacterData.DataColor[(int)_data.ColorType]));
            }

            if (_data.TextureType != CharacterDataSetting.CharacterTextureNames.None)
            {
                _mat.SetTexture(_data.SlotName, CharacterManager.LoadTexture(_data.TextureBaseName + ((int)MyData._CharacterData.DataInt[(int)_data.TextureType]).ToString("00")));
            }

            if (_data.FloatType != CharacterDataSetting.CharacterBoneSliderNames.None)
            {
                if (_data.TextureBaseName == "offsetx")
                {
                    _mat.SetTextureOffset(_data.SlotName, new Vector2(GetBoneValue(_data.FloatType, 0F, _data.SliderMin, _data.SliderMax), _mat.GetTextureOffset(_data.SlotName).y));
                }
                else if (_data.TextureBaseName == "offsety")
                {
                    _mat.SetTextureOffset(_data.SlotName, new Vector2(_mat.GetTextureOffset(_data.SlotName).x, GetBoneValue(_data.FloatType, 0F, _data.SliderMin, _data.SliderMax)));
                }
                else if (_data.TextureBaseName == "tilingx")
                {
                    _mat.SetTextureScale(_data.SlotName, new Vector2(GetBoneValue(_data.FloatType, 1F, _data.SliderMin, _data.SliderMax), _mat.GetTextureScale(_data.SlotName).y));
                }
                else if (_data.TextureBaseName == "tilingy")
                {
                    _mat.SetTextureScale(_data.SlotName, new Vector2(_mat.GetTextureScale(_data.SlotName).x, GetBoneValue(_data.FloatType, 1F, _data.SliderMin, _data.SliderMax)));
                }
                else
                {
                    _mat.SetFloat(_data.SlotName, MyData._CharacterData.DataFloat[(int)_data.FloatType] * 0.01F);
                }
            }

        }

        private void ApplyBoneData(Transform _bone, CharacterBoneDeformSetting _data)
        {
            if (_data.SetPos && _data.PositionSliders.Length >= 3)
            {
                _bone.localPosition = new Vector3(GetBoneValue(_data.PositionSliders[0].SliderType, _data.OriPos.x, _data.PositionSliders[0].MinOffset, _data.PositionSliders[0].MaxOffset),
                    GetBoneValue(_data.PositionSliders[1].SliderType, _data.OriPos.y, _data.PositionSliders[1].MinOffset, _data.PositionSliders[1].MaxOffset),
                    GetBoneValue(_data.PositionSliders[2].SliderType, _data.OriPos.z, _data.PositionSliders[2].MinOffset, _data.PositionSliders[2].MaxOffset)
                );
            }

            if (_data.SetRot && _data.RotationSliders.Length >= 3)
            {
                _bone.localEulerAngles = new Vector3(GetBoneValue(_data.RotationSliders[0].SliderType, _data.OriRot.x, _data.RotationSliders[0].MinOffset, _data.RotationSliders[0].MaxOffset),
                    GetBoneValue(_data.RotationSliders[1].SliderType, _data.OriRot.y, _data.RotationSliders[1].MinOffset, _data.RotationSliders[1].MaxOffset),
                    GetBoneValue(_data.RotationSliders[2].SliderType, _data.OriRot.z, _data.RotationSliders[2].MinOffset, _data.RotationSliders[2].MaxOffset)
                );
            }

            if (_data.SetScale && _data.ScaleSliders.Length >= 3)
            {
                _bone.localScale = new Vector3(GetBoneValue(_data.ScaleSliders[0].SliderType, _data.OriScale.x, _data.ScaleSliders[0].MinOffset, _data.ScaleSliders[0].MaxOffset),
                    GetBoneValue(_data.ScaleSliders[1].SliderType, _data.OriScale.y, _data.ScaleSliders[1].MinOffset, _data.ScaleSliders[1].MaxOffset),
                    GetBoneValue(_data.ScaleSliders[2].SliderType, _data.OriScale.z, _data.ScaleSliders[2].MinOffset, _data.ScaleSliders[2].MaxOffset)
                );
            }
        }

        private float GetBoneValue(CharacterDataSetting.CharacterBoneSliderNames _type, float _oriValue, float _min, float _max)
        {
            float _value = MyData._CharacterData.DataFloat[(int)_type] * 0.01F;
            if (_type != CharacterDataSetting.CharacterBoneSliderNames.None)
            {
                if (_value > 0.5F)
                {
                    return Mathf.Lerp(_oriValue, _max + _max * SliderMutiplier, (_value - 0.5F) * 2F);
                }
                else if (_value < 0.5F)
                {
                    return Mathf.Lerp(_oriValue, _min - _min * SliderMutiplier, (0.5F - _value) * 2F);
                }
                else
                {
                    return _oriValue;
                }
            }
            else
            {
                return _oriValue;
            }
        }

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

        private void CheckIntersection()
        {
            if ( BodyRenderer.sharedMesh.blendShapeCount > 0)
            {
                BodyRenderer.SetBlendShapeWeight(0, GetOutfitSetting(OutfitSlots.Gauntlet, OldOutfitID[(int)OutfitSlots.Gauntlet], MyData._CharacterData.Sex).PossibleIntersect ? 100F : 0F);
            }
            if (PantsRenderer.sharedMesh.blendShapeCount > 0)
            {
                PantsRenderer.SetBlendShapeWeight(0, GetOutfitSetting(OutfitSlots.Boot, OldOutfitID[(int)OutfitSlots.Boot], MyData._CharacterData.Sex).PossibleIntersect ? 100F : 0F);
            }
            
        }

        public void UpdateBreastSize()
        {
            if (MyData._Sex == Sex.Female && BodyRenderer!=null && BodyRenderer.sharedMesh.blendShapeCount > 0 && GetOutfitSetting(OutfitSlots.Armor, OldOutfitID[(int)OutfitSlots.Armor], MyData._CharacterData.Sex).BreastMorphID>-1)
            {
                BodyRenderer.SetBlendShapeWeight(GetOutfitSetting(OutfitSlots.Armor, OldOutfitID[(int)OutfitSlots.Armor], MyData._CharacterData.Sex).BreastMorphID, MyData._CharacterData.DataFloat[87]);
            }
        }

        private void CheckOverride()
        {
            for (int i = 0; i < OutfitSlotsCount; i++)
            {
                OutfitInfo _info = GetOutfitSetting((OutfitSlots)i, OldOutfitID[i], MyData._CharacterData.Sex);
                if (_info.OverrideMask1)
                {
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        if (RendererMapping[_info.OverrideSlot].sharedMaterial != null && GetOutfitSetting(_info.OverrideSlot, OldOutfitID[(int)_info.OverrideSlot], MyData._CharacterData.Sex).CanBeOverride)
                        {
                            RendererMapping[_info.OverrideSlot].sharedMaterial.SetTexture("_MaskMap1", _info.OverrideMask1);
                            RendererMapping[_info.OverrideSlot].sharedMaterial.SetColor("_Color1", Uint8Color.Get(OldCusColor1[i]));
                        }
                    }
                    else
                    {

                        if (RendererMapping[_info.OverrideSlot].material != null && GetOutfitSetting(_info.OverrideSlot, OldOutfitID[(int)_info.OverrideSlot], MyData._CharacterData.Sex).CanBeOverride)
                        {
                            RendererMapping[_info.OverrideSlot].material.SetTexture("_MaskMap1", _info.OverrideMask1);
                            RendererMapping[_info.OverrideSlot].material.SetColor("_Color1", Uint8Color.Get(OldCusColor1[i]));
                        }
                    }
                }
            }
        }

        private void ClearOverride()
        {
            for (int i = 0; i < OutfitSlotsCount; i++)
            {
                OutfitInfo _info = GetOutfitSetting((OutfitSlots)i, OldOutfitID[i], MyData._CharacterData.Sex);
                if (_info.OverrideMask1)
                {
                    if (RendererMapping[_info.OverrideSlot].material!=null && GetOutfitSetting(_info.OverrideSlot, OldOutfitID[(int)_info.OverrideSlot], MyData._CharacterData.Sex).CanBeOverride)
                    {
                        if (Application.isEditor && !Application.isPlaying)
                        {
                            RendererMapping[_info.OverrideSlot].sharedMaterial.SetTexture("_MaskMap1", null);
                            RendererMapping[_info.OverrideSlot].sharedMaterial.SetColor("_Color1", new Color(0F, 0F, 0F));
                        }
                        else
                        {
                            RendererMapping[_info.OverrideSlot].material.SetTexture("_MaskMap1", null);
                            RendererMapping[_info.OverrideSlot].material.SetColor("_Color1", new Color(0F, 0F, 0F));
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Initialize this character with Appearance data.
        /// </summary>
        /// <param name="_data"></param>
        public void Initialize(CharacterAppearance _data) 
        {
            MyData = _data;
            if (Application.isEditor && !Application.isPlaying)
            {
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                InternalInit();
                CheckOutfits();
                CreateEditorMaterialCopy(HeadRenderer);
                CreateEditorMaterialCopy(EyesRenderer);
                UpdateMaterialData();
                CheckOverride();
                CheckIntersection();
                UpdateBreastSize();
                BoneTransform();
#endif
            }
            else
            {
                UpdateMaterialData();
            }
            Inited = true;
        }

        /// <summary>
        /// Switch the outfit with slot type and mesh id.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="_id"></param>
        public void SwitchOutfit(OutfitSlots slot, int _id) 
        {
            if (OldOutfitID[(int)slot] == _id) return;
            ClearOverride();
            OldOutfitID[(int)slot] = _id;
            switch (slot) {
                case OutfitSlots.Armor:
                    LoadSkinnedMesh(BodyRenderer,GetOutfitSetting(OutfitSlots.Armor,_id,MyData._CharacterData.Sex).MeshPath, GetOutfitSetting(OutfitSlots.Armor, _id, MyData._CharacterData.Sex).MaterialPath, GetOutfitSetting(OutfitSlots.Armor, _id, MyData._CharacterData.Sex).BoneNames);
                    if (Application.isEditor && !Application.isPlaying) CreateEditorMaterialCopy(BodyRenderer);
                    break;
                case OutfitSlots.Pants:
                    LoadSkinnedMesh(PantsRenderer, GetOutfitSetting(OutfitSlots.Pants, _id, MyData._CharacterData.Sex).MeshPath, GetOutfitSetting(OutfitSlots.Pants, _id, MyData._CharacterData.Sex).MaterialPath, GetOutfitSetting(OutfitSlots.Pants, _id, MyData._CharacterData.Sex).BoneNames);
                    if (Application.isEditor && !Application.isPlaying) CreateEditorMaterialCopy(PantsRenderer);
                    break;
                case OutfitSlots.Boot:
                    LoadSkinnedMesh(FeetRenderer, GetOutfitSetting(OutfitSlots.Boot, _id, MyData._CharacterData.Sex).MeshPath, GetOutfitSetting(OutfitSlots.Boot, _id, MyData._CharacterData.Sex).MaterialPath, GetOutfitSetting(OutfitSlots.Boot, _id, MyData._CharacterData.Sex).BoneNames);
                    if (Application.isEditor && !Application.isPlaying) CreateEditorMaterialCopy(FeetRenderer);
                    break;
                case OutfitSlots.Gauntlet:
                    LoadSkinnedMesh(HandsRenderer, GetOutfitSetting(OutfitSlots.Gauntlet, _id, MyData._CharacterData.Sex).MeshPath, GetOutfitSetting(OutfitSlots.Gauntlet, _id, MyData._CharacterData.Sex).MaterialPath, GetOutfitSetting(OutfitSlots.Gauntlet, _id, MyData._CharacterData.Sex).BoneNames);
                    if (Application.isEditor && !Application.isPlaying) CreateEditorMaterialCopy(HandsRenderer);
                    break;
                case OutfitSlots.Helmet:
                    LoadSkinnedMesh(HelmetRenderer, GetOutfitSetting(OutfitSlots.Helmet, _id, MyData._CharacterData.Sex).MeshPath, GetOutfitSetting(OutfitSlots.Helmet, _id, MyData._CharacterData.Sex).MaterialPath, GetOutfitSetting(OutfitSlots.Helmet, _id, MyData._CharacterData.Sex).BoneNames);
                    if (Application.isEditor && !Application.isPlaying) CreateEditorMaterialCopy(HelmetRenderer);
                    ShowHair = !GetOutfitSetting(OutfitSlots.Helmet, _id, MyData._CharacterData.Sex).HideHair;
                    ShowFace = !GetOutfitSetting(OutfitSlots.Helmet, _id, MyData._CharacterData.Sex).HideFace;
                    CheckHairVisibility();
                    break;
            }
            SetOutfitColor(slot);
            if (!Application.isEditor || Application.isPlaying)
            {
                UpdateMaterialData();
                CheckOverride();
                CheckIntersection();
                UpdateBreastSize();
            }
        }


        /// <summary>
        /// Force the character to blink at once.
        /// </summary>
        public void Blink()
        {
            if (BlinkCo != null) StopCoroutine(BlinkCo);
            BlinkCo = StartCoroutine(DoBlink());
        }

        /// <summary>
        /// Set the emotion type and how long it lasts
        /// </summary>
        /// <param name="_uid"></param>
        /// <param name="_length"></param>
        public void SetEmotion(string _uid, float _length)
        {
            ActiveEmotion = new KeyValuePair<string, float>(_uid, _length);
        }

        /// <summary>
        /// Stop the current emotion and reset to the default emotion.
        /// </summary>
        public void StopEmotion()
        {
            ActiveEmotion = new KeyValuePair<string, float>("null", 0F);
        }

        /// <summary>
        /// Update the materials, call this if you have changed the appearance data.
        /// </summary>
        public void UpdateMaterialData()
        {
            if (CharacterDataSetting.instance == null) return;
            foreach (CharacterMaterialSetting obj in MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleMaterialSettings : CharacterDataSetting.instance.FemaleMaterialSettings)
            {
                
                foreach (string _rendererName in obj.RendererName) {
                    //Debug.Log("UpdateMaterialData>(Sex:" + MyData._CharacterData.Sex + ") " + _rendererName + " > " + obj.SlotName + " > " + obj.TextureBaseName + ((int)MyData._CharacterData.DataInt[(int)obj.TextureType]).ToString("00"));
                    string[] _names = new string[1];
                    if (_rendererName.Contains("$"))
                    {
                        _names = _rendererName.Split('$');
                    }
                    else
                    {
                        _names[0] = _rendererName;
                    }
                    int _id = 0;
                    if (_names.Length > 1) _id = int.Parse(_names[1]);

                    if (MyRenderer.ContainsKey(_names[0]))
                    {
                        ApplyMaterialData(MyRenderer[_names[0]], obj, _id);
                    }
                    else
                    {
                        MyRenderer.Add(_names[0], FindAllChild(transform, _names[0]).GetComponent<Renderer>());
                        ApplyMaterialData(MyRenderer[_names[0]], obj, _id);
                    }
                }
            }
            LoadAccessories();
            UpdateRace();
            UpdateBreastSize();
        }

        /// <summary>
        /// Create a copy of the materials of the specified renderer.
        /// </summary>
        /// <param name="_renderer"></param>
        public void CreateEditorMaterialCopy(Renderer _renderer)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _renderer!=null && _renderer.sharedMaterials.Length>0)
            {
                Material[] _mats = new Material[_renderer.sharedMaterials.Length];
                if (!AssetDatabase.IsValidFolder(EditorPath + "/" + _renderer.name)) AssetDatabase.CreateFolder(EditorPath, _renderer.name);
                for (int i = 0; i < _renderer.sharedMaterials.Length; i++)
                {
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(_renderer.sharedMaterials[i]), EditorPath + "/" + _renderer.name + "/" + _renderer.sharedMaterials[i].name + ".mat");
                    _mats[i] = (Material)AssetDatabase.LoadAssetAtPath(EditorPath + "/" + _renderer.name + "/" + _renderer.sharedMaterials[i].name + ".mat", typeof(Material));
                }
                _renderer.sharedMaterials = _mats;
                _renderer.sharedMaterial = _mats[0];
            }
#endif
        }

     
        

    }

}
