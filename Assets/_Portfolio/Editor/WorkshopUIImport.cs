using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Portfolio → UI → Import Workshop PSD: switches UI_Workshop.psd to the 2D PSD Importer with the settings from
    /// Docs/UI_PSD_Spec.md, then fills Resources/UISkin.asset with every layer sprite. Re-run after editing the PSD.
    /// Talks to the importer by reflection so this file still compiles if com.unity.2d.psdimporter is removed.
    /// </summary>
    public static class WorkshopUIImport
    {
        const string PsdPath = "Assets/_Portfolio/Art/UI/UI_Workshop.psd";
        const string SkinFolder = "Assets/_Portfolio/Resources";
        const string SkinPath = SkinFolder + "/UISkin.asset";
        const string ImporterType = "UnityEditor.U2D.PSD.PSDImporter, Unity.2D.Psdimporter.Editor";

        /// <summary>
        /// Portfolio → UI → Set Up Workshop Fonts: TMP assets for Courier Prime (+Bold) and Work Sans, Heebo as the Hebrew
        /// fallback, Courier Prime Bold as the real bold weight, and both faces stored in the UISkin.
        /// </summary>
        [MenuItem("Portfolio/UI/Set Up Workshop Fonts")]
        public static void SetUpFonts()
        {
            PortfolioFonts.CreateFontAssets(); // makes "<name> SDF.asset" for every .ttf in Art/Fonts
            const string dir = "Assets/_Portfolio/Art/Fonts/";
            var courier = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dir + "CourierPrime SDF.asset");
            var courierBold = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dir + "CourierPrime-Bold SDF.asset");
            var work = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dir + "WorkSans SDF.asset");
            var hebrew = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dir + "Heebo SDF.asset");
            if (!courier || !work) { Debug.LogError("[WorkshopUI] Font assets missing; are the .ttf files in Art/Fonts?"); return; }

            foreach (var f in new[] { courier, courierBold, work })
            {
                if (!f) continue;
                if (hebrew && !f.fallbackFontAssetTable.Contains(hebrew)) f.fallbackFontAssetTable.Insert(0, hebrew);
                EditorUtility.SetDirty(f);
            }
            if (courierBold)
            {
                var table = courier.fontWeightTable;
                table[7].regularTypeface = courierBold; // 700 = Bold: use the real bold, not a faked one
                EditorUtility.SetDirty(courier);
            }

            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            if (skin)
            {
                skin.typewriter = courier;
                skin.label = work;
                EditorUtility.SetDirty(skin);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WorkshopUI] Fonts ready: Courier Prime (+Bold), Work Sans, Heebo fallback.", skin);
        }

        [MenuItem("Portfolio/UI/Import Workshop PSD")]
        public static void Run()
        {
            var type = Type.GetType(ImporterType);
            if (type == null)
            {
                EditorUtility.DisplayDialog("PSD Importer missing",
                    "Install com.unity.2d.psdimporter (it is in Packages/manifest.json; let the Package Manager finish resolving).", "OK");
                return;
            }
            if (AssetDatabase.LoadMainAssetAtPath(PsdPath) == null && AssetImporter.GetAtPath(PsdPath) == null)
            {
                Debug.LogError($"[WorkshopUI] {PsdPath} not found.");
                return;
            }

            // .psd is an "override" extension for the PSD Importer (only .psb is claimed by default).
            if (AssetDatabase.GetImporterOverride(PsdPath) != type)
            {
                var setOverride = typeof(AssetDatabase).GetMethods()
                    .First(m => m.Name == nameof(AssetDatabase.SetImporterOverride) && m.IsGenericMethodDefinition);
                setOverride.MakeGenericMethod(type).Invoke(null, new object[] { PsdPath });
            }

            var importer = AssetImporter.GetAtPath(PsdPath);
            if (importer == null || importer.GetType() != type)
            {
                Debug.LogError($"[WorkshopUI] {PsdPath} is not using the PSD Importer (got {importer?.GetType().Name}).");
                return;
            }

            // Hidden layers hold every _Hover / _Pressed / _Active state and the other ticket drawings.
            var so = new SerializedObject(importer);
            var hidden = so.FindProperty("m_ImportHiddenLayers");
            if (hidden != null) hidden.boolValue = true;
            else Debug.LogWarning("[WorkshopUI] Couldn't find 'Include Hidden Layers'; tick it in the Inspector.");
            so.ApplyModifiedPropertiesWithoutUndo();

            // 2× art: 200 px per unit so 1 PSD px = 0.5 UI px against the Canvas Scaler's 100.
            type.GetProperty("spritePixelsPerUnit")?.SetValue(importer, 200f);

            // Keep the packed atlas at full resolution.
            var get = type.GetMethod("GetImporterPlatformSettings");
            var set = type.GetMethod("SetImporterPlatformSettings");
            if (get != null && set != null)
            {
                foreach (var target in new[] { EditorUserBuildSettings.activeBuildTarget, BuildTarget.WebGL })
                {
                    var ps = (TextureImporterPlatformSettings)get.Invoke(importer, new object[] { target });
                    if (ps.maxTextureSize < 4096) ps.maxTextureSize = 4096;
                    set.Invoke(importer, new object[] { ps });
                }
            }

            ((ScriptedImporter)importer).SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(PsdPath).OfType<Sprite>().OrderBy(s => s.name).ToList();
            if (sprites.Count == 0)
            {
                Debug.LogError("[WorkshopUI] The PSD imported but produced no sprites. Check the importer's Layer Management settings.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SkinFolder)) AssetDatabase.CreateFolder("Assets/_Portfolio", "Resources");
            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            if (!skin)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.SetSprites(sprites);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();

            string[] required =
            {
                "BG_Paper", "Btn_Close_Normal", "Btn_Close_Hover", "Logo_Stroke", "Signature_Stroke",
                "Ticket_Shadow", "BG_Card", "Art_Area", "Perforation", "Ticket_Play_Normal", "Ticket_Play_Pressed",
                "Art_ShellBound_Stroke", "Art_Maskadu_Stroke", "Art_WhereIsMyMind_Stroke", "Art_ChatBaker_Stroke",
                "Art_KaasJourney_Stroke", "Art_Project_Stroke", "Stamp_Normal", "Stamp_Inked",
            };
            var missing = required.Where(n => !skin.Get(n)).ToArray();
            if (missing.Length > 0)
                Debug.LogWarning("[WorkshopUI] Skin is missing layers: " + string.Join(", ", missing));
            Debug.Log($"[WorkshopUI] UISkin filled with {sprites.Count} sprites from {PsdPath}.", skin);
            Selection.activeObject = skin;
        }
    }
}
