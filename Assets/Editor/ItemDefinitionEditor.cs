#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Items; // ItemCategory, ItemSubtype, CharacterClass, etc.

[CustomEditor(typeof(ItemDefinition), true)]
public class ItemDefinitionEditor : Editor
{
    SerializedProperty visualProp; 
    // --- Cached props we draw manually ---
    SerializedProperty
        // identity
        itemIdProp, displayNameProp, descriptionProp,
        // inventory / prefabs / icon / preview
        widthProp, heightProp, worldPrefabProp, invPreviewPrefabProp, iconProp, previewProp,
        // classification
        categoryProp, subtypeProp, gripProp,
        // requirements
        requirementsProp,
        // stats (variants)
        baseDamageProp, baseDefenseProp, baseMagicResistProp, hpOnKillProp, manaOnKillProp,
        // durability & value
        baseDurabilityProp, baseValueProp,
        // sockets & blessing
        canBeBlessedProp, socketSlotTypeProp, socketsMaxProp,
        // rarity
        defaultTierProp, legacyRarityProp;

    SerializedProperty potionTypeProp, potionSizeProp;
    SerializedProperty instantAmountProp, overTimeAmountProp;
    SerializedProperty overTimeDurationSecondsProp, tickIntervalSecondsProp;
    SerializedProperty maxStackProp;

    // Foldouts (single source of truth; one section each)
    bool fIdentity     = true;
    bool fInventory    = true;
    bool fClass        = true;
    bool fRequirements = true;
    bool fStats        = true;
    bool fDurability   = true;
    bool fSockets      = true;
    bool fRarity       = true;
    bool fPreview      = false;
    bool fPotion = true;

    void OnEnable()
    {
        // identity
        itemIdProp       = serializedObject.FindProperty("itemId");
        displayNameProp  = serializedObject.FindProperty("displayName");
        descriptionProp  = serializedObject.FindProperty("description");

        // inventory/prefabs/icon/preview
        widthProp            = serializedObject.FindProperty("width");
        heightProp           = serializedObject.FindProperty("height");
        worldPrefabProp      = serializedObject.FindProperty("worldPrefab");
        invPreviewPrefabProp = serializedObject.FindProperty("inventoryPreviewPrefab");
        iconProp             = serializedObject.FindProperty("icon");
        previewProp          = serializedObject.FindProperty("preview");

        // classification
        categoryProp = serializedObject.FindProperty("category");
        subtypeProp  = serializedObject.FindProperty("subtype");
        gripProp     = serializedObject.FindProperty("grip");

        // requirements
        requirementsProp = serializedObject.FindProperty("requirements");

        // stats
        baseDamageProp      = serializedObject.FindProperty("baseDamage");
        baseDefenseProp     = serializedObject.FindProperty("baseDefense");
        baseMagicResistProp = serializedObject.FindProperty("baseMagicResist");
        hpOnKillProp        = serializedObject.FindProperty("hpOnKill");
        manaOnKillProp      = serializedObject.FindProperty("manaOnKill");

        // durability & value
        baseDurabilityProp = serializedObject.FindProperty("baseDurability");
        baseValueProp      = serializedObject.FindProperty("baseValue");

        // blessing & sockets
        canBeBlessedProp   = serializedObject.FindProperty("canBeBlessed");
        socketSlotTypeProp = serializedObject.FindProperty("socketSlotType");
        socketsMaxProp     = serializedObject.FindProperty("socketsMax");

        // rarity
        defaultTierProp  = serializedObject.FindProperty("defaultTier");
        legacyRarityProp = serializedObject.FindProperty("legacyRarity");

        if (target is Game.Items.EquipmentItemDefinition)
            visualProp = serializedObject.FindProperty("visual");

        if (target is PotionItemDefinition)
        {
            potionTypeProp              = serializedObject.FindProperty("potionType");
            potionSizeProp              = serializedObject.FindProperty("potionSize");
            instantAmountProp           = serializedObject.FindProperty("instantAmount");
            overTimeAmountProp          = serializedObject.FindProperty("overTimeAmount");
            overTimeDurationSecondsProp = serializedObject.FindProperty("overTimeDurationSeconds");
            tickIntervalSecondsProp     = serializedObject.FindProperty("tickIntervalSeconds");
            maxStackProp                = serializedObject.FindProperty("maxStack");
        }
        else
        {
            potionTypeProp = potionSizeProp = null;
            instantAmountProp = overTimeAmountProp =
                overTimeDurationSecondsProp = tickIntervalSecondsProp = maxStackProp = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // NOTHING is auto-drawn. We render every section once, manually.

        var cat = (ItemCategory)categoryProp.enumValueIndex;
        var st  = (ItemSubtype)subtypeProp.enumValueIndex;

        // ---------------- Identity ----------------
        fIdentity = FoldoutHeader(fIdentity, "Identity");
        if (fIdentity)
        {
            Box(() =>
            {
                EditorGUILayout.PropertyField(itemIdProp);
                EditorGUILayout.PropertyField(displayNameProp);
                EditorGUILayout.PropertyField(descriptionProp);
            });
        }

        // ---------------- Inventory & Prefabs ----------------
        fInventory = FoldoutHeader(fInventory, "Inventory & Prefabs");
        if (fInventory)
        {
            Box(() =>
            {
                EditorGUILayout.IntSlider(widthProp, 1, 8);
                EditorGUILayout.IntSlider(heightProp, 1, 12);
                EditorGUILayout.PropertyField(worldPrefabProp);
                EditorGUILayout.PropertyField(invPreviewPrefabProp, new GUIContent("Inventory Preview Prefab"));
                EditorGUILayout.PropertyField(iconProp);
            });
        }

        // ---------------- Visuals (Equipment only) ----------------
        if (target is Game.Items.EquipmentItemDefinition)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuals (optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visualProp, new GUIContent("Visual"));
        }

        // ---------------- Classification ----------------
        fClass = FoldoutHeader(fClass, "Classification");
        if (fClass)
        {
            Box(() =>
            {
                EditorGUILayout.PropertyField(categoryProp);
                EditorGUILayout.PropertyField(subtypeProp);
                using (new EditorGUI.DisabledScope(cat != ItemCategory.Weapon))
                {
                    EditorGUILayout.PropertyField(gripProp);
                }
            });
        }

        // ---------------- Potion (only for PotionItemDefinition) ------------
        bool isPotion = target is PotionItemDefinition;
        if (isPotion && potionTypeProp != null)
        {
            fPotion = FoldoutHeader(fPotion, "Potion");
            if (fPotion)
            {
                Box(() =>
                {
                    EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(potionTypeProp, new GUIContent("Potion Type"));
                    EditorGUILayout.PropertyField(potionSizeProp, new GUIContent("Potion Size"));

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Healing", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(instantAmountProp, new GUIContent("Instant Amount"));
                    EditorGUILayout.PropertyField(overTimeAmountProp, new GUIContent("Over-Time Amount"));
                    EditorGUILayout.PropertyField(overTimeDurationSecondsProp, new GUIContent("Duration (sec)"));
                    EditorGUILayout.PropertyField(tickIntervalSecondsProp, new GUIContent("Tick Interval (sec)"));

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(maxStackProp, new GUIContent("Max Stack"));
                });
            }
        }


        // ---------------- Requirements ----------------
        fRequirements = FoldoutHeader(fRequirements, "Requirements");
        if (fRequirements)
        {
            Box(() => DrawRequirements(requirementsProp));
        }

        // ---------------- Combat Stats & Bonuses (merged) ----------------
        fStats = FoldoutHeader(fStats, "Combat Stats & Bonuses");
        if (fStats)
        {
            Box(() =>
            {
                if (cat == ItemCategory.Weapon)
                {
                    DrawDamageProfile(baseDamageProp);
                    EditorGUILayout.HelpBox(
                        "Attack Speed is APS (1.00 = neutral). Crit Chance accepts 0..1 or 0..100%. Values normalize in OnValidate.",
                        MessageType.None);
                }
                else if (cat == ItemCategory.Armor || st == ItemSubtype.Shield)
                {
                    EditorGUILayout.IntSlider(baseDefenseProp, 0, 999, new GUIContent("Physical Defense"));
                    EditorGUILayout.IntSlider(baseMagicResistProp, 0, 999, new GUIContent("Magic Resist"));
                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(hpOnKillProp, new GUIContent("HP on Kill"));
                    EditorGUILayout.PropertyField(manaOnKillProp, new GUIContent("Mana on Kill"));
                }
                else
                {
                    // Accessories / misc
                    EditorGUILayout.PropertyField(hpOnKillProp, new GUIContent("HP on Kill"));
                    EditorGUILayout.PropertyField(manaOnKillProp, new GUIContent("Mana on Kill"));
                }
            });
        }

        // ---------------- Durability & Value ----------------
        fDurability = FoldoutHeader(fDurability, "Durability & Value");
        if (fDurability)
        {
            Box(() =>
            {
                EditorGUILayout.IntSlider(baseDurabilityProp, 1, 200, new GUIContent("Base Durability"));
                EditorGUILayout.IntSlider(baseValueProp, 0, 1_000_000, new GUIContent("Base Value (gold)"));
            });
        }

        // ---------------- Blessing & Sockets ----------------
        fSockets = FoldoutHeader(fSockets, "Blessing & Sockets");
        if (fSockets)
        {
            Box(() =>
            {
                EditorGUILayout.PropertyField(canBeBlessedProp, new GUIContent("Can Be Blessed"));

                // Socket type is derived in OnValidate but visible for clarity
                EditorGUILayout.PropertyField(socketSlotTypeProp, new GUIContent("Socket Slot Type"));

                int w = Mathf.Max(1, widthProp.intValue);
                int h = Mathf.Max(1, heightProp.intValue);
                int maxSockets = Mathf.Max(1, w * h);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Sockets (Dynamic Limit)", EditorStyles.miniBoldLabel);

                EditorGUI.BeginChangeCheck();
                int next = EditorGUILayout.IntSlider(new GUIContent("Max Sockets"), socketsMaxProp.intValue, 0, maxSockets);
                if (EditorGUI.EndChangeCheck())
                    socketsMaxProp.intValue = next;

                EditorGUILayout.HelpBox($"Capped by footprint: {w} × {h} → {maxSockets}", MessageType.Info);
            });
        }

        // ---------------- Tier / Rarity ----------------
        fRarity = FoldoutHeader(fRarity, "Tier / Rarity");
        if (fRarity)
        {
            Box(() =>
            {
                EditorGUILayout.PropertyField(defaultTierProp, new GUIContent("Default Tier"));
                EditorGUILayout.PropertyField(legacyRarityProp, new GUIContent("Legacy Rarity (compat)"));
            });
        }

        // ---------------- 3D Preview Tuning ----------------
        fPreview = FoldoutHeader(fPreview, "3D Preview Tuning (UI only)");
        if (fPreview)
        {
            Box(() =>
            {
                // Draw the whole preview struct (it has no inner headers)
                EditorGUILayout.PropertyField(previewProp, true);
            });
        }

        serializedObject.ApplyModifiedProperties();
    }

    // -------- Helpers: consistent section chrome --------

    private static bool FoldoutHeader(bool state, string title)
    {
        return EditorGUILayout.Foldout(state, title, true, EditorStyles.foldoutHeader);
    }

    private static void Box(System.Action body)
    {
        using (new EditorGUILayout.VerticalScope("box")) { body?.Invoke(); }
    }

    // -------- Manual child drawing to avoid nested headers --------

    private static void DrawRequirements(SerializedProperty req)
    {
        if (req == null) return;

        var level    = req.FindPropertyRelative("level");
        var usableBy = req.FindPropertyRelative("usableBy");
        var minStr   = req.FindPropertyRelative("minStrength");
        var minDex   = req.FindPropertyRelative("minDexterity");
        var minEng   = req.FindPropertyRelative("minEnergy");

        EditorGUILayout.IntSlider(level, 1, 100, new GUIContent("Level"));
        EditorGUILayout.PropertyField(usableBy, new GUIContent("Usable By"));
        EditorGUILayout.Space(2);
        minStr.intValue = EditorGUILayout.IntSlider(new GUIContent("Min Strength"),  minStr.intValue, 0, 500);
        minDex.intValue = EditorGUILayout.IntSlider(new GUIContent("Min Dexterity"), minDex.intValue, 0, 500);
        minEng.intValue = EditorGUILayout.IntSlider(new GUIContent("Min Energy"),    minEng.intValue, 0, 500);
    }

    private static void DrawDamageProfile(SerializedProperty dmg)
    {
        if (dmg == null) return;

        var min            = dmg.FindPropertyRelative("min");
        var max            = dmg.FindPropertyRelative("max");
        var wizardry       = dmg.FindPropertyRelative("wizardry");
        var critChance     = dmg.FindPropertyRelative("critChance");
        var critMultiplier = dmg.FindPropertyRelative("critMultiplier");
        var attackSpeed    = dmg.FindPropertyRelative("attackSpeed");

        EditorGUILayout.IntSlider(min, 0, 999, new GUIContent("Physical Min"));
        EditorGUILayout.IntSlider(max, Mathf.Max(0, min.intValue), 999, new GUIContent("Physical Max"));
        EditorGUILayout.IntSlider(wizardry, 0, 999, new GUIContent("Wizardry (Magic Flat)"));
        EditorGUILayout.Slider(critChance, 0f, 100f, new GUIContent("Crit Chance (0..1 or 0..100)"));
        EditorGUILayout.Slider(critMultiplier, 1.0f, 5.0f, new GUIContent("Crit Multiplier (x)"));
        attackSpeed.floatValue = EditorGUILayout.FloatField(new GUIContent("Attack Speed (APS)"), attackSpeed.floatValue);
    }
}
#endif
