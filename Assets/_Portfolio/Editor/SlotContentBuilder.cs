using System.Collections.Generic;
using System.Linq;
using Portfolio.Slot;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Sets the art in Art/UI/Slot (rendered by Art/UI/tools/slot_gen.html) to Sprite import, and creates
    /// Resources/SlotMachine.asset with the six icons and their facts. Never overwrites the asset's text, so edit
    /// the facts in the Inspector. Runs by itself once if the asset is missing.
    /// </summary>
    [InitializeOnLoad]
    public static class SlotContentBuilder
    {
        const string ArtDir = "Assets/_Portfolio/Art/UI/Slot";
        const string AssetPath = "Assets/_Portfolio/Resources/SlotMachine.asset";

        static SlotContentBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && NeedsBuild()) Build();
            };
        }

        // Icon file → what its triple reveals: to someone just visiting, to HR, to a team lead, to a head of department.
        // All from the CVs.
        static readonly (string id, string fact, string hr, string lead, string head)[] Facts =
        {
            ("Lamp",
                "Since 2017 I've run my own lighting brand, Babilanur: lamps you control by touch.",
                "Running my own brand for seven years taught me to own a project end to end.",
                "Babilanur lamps combine handcrafted wood and metal, 3D printing and microcontrollers.",
                "Babilanur has been my own product brand since 2017, from concept to fabrication."),
            ("Controller",
                "I built five games at Shenkar, one of them in 48 hours at Global Game Jam.",
                "At Global Game Jam I shipped a full game in 48 hours with a team.",
                "In Unity I script player controllers, enemy behavior and animation state machines.",
                "Five games in one program, finished with highest honors."),
            ("Chip",
                "I design my own PCBs, and took a product through FCC and CE certification.",
                "I work directly with engineers and manufacturers, all the way to production.",
                "I design PCBs and custom electronics around sensors and microcontrollers.",
                "I took a product through FCC and CE certification and into production."),
            ("Camera",
                "Before design, I was a cinema camera technician on film sets.",
                "Years on film sets taught me to keep calm and work well in a big crew.",
                "As a camera technician I learned to troubleshoot complex rigs under pressure.",
                "I built and priced equipment packages for film productions and led the warehouse team."),
            ("Frame",
                "I lit 17 century-old miniature rooms for the Tel Aviv Museum of Art.",
                "A museum trusted me with 100-year-old miniatures: careful, patient work.",
                "Custom LED circuits for each of 17 rooms, installed without drilling a single hole.",
                "My lighting is part of a permanent 20-year exhibit at the Tel Aviv Museum of Art."),
            ("Wrench",
                "I finished Game Development at Shenkar with highest honors, and I still build everything by hand.",
                "I'm a maker: I learn by building things with my own hands.",
                "SolidWorks, Blender, Unity and Arduino: I go from CAD to code.",
                "Industrial designer and game developer in one: I design it, build it and ship it."),
        };

        /// <summary>Also runs when the asset predates the per-visitor lines.</summary>
        static bool NeedsBuild()
        {
            var d = AssetDatabase.LoadAssetAtPath<SlotMachineData>(AssetPath);
            return !d || d.symbols.Count == 0 || d.symbols.Any(s => string.IsNullOrEmpty(s.factHR));
        }

        [MenuItem("Portfolio/Slot/Build Slot Machine Content")]
        public static void Build()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp.textureType == TextureImporterType.Sprite) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{n}.png");

            if (!AssetDatabase.IsValidFolder("Assets/_Portfolio/Resources")) AssetDatabase.CreateFolder("Assets/_Portfolio", "Resources");
            var data = AssetDatabase.LoadAssetAtPath<SlotMachineData>(AssetPath);
            bool fresh = !data;
            if (fresh) { data = ScriptableObject.CreateInstance<SlotMachineData>(); AssetDatabase.CreateAsset(data, AssetPath); }

            data.body = S("Slot_Body"); data.window = S("Slot_Window"); data.lever = S("Slot_Lever"); data.energyIcon = S("Slot_Energy");
            var existing = data.symbols.ToDictionary(s => s.id, s => s);
            static string Keep(string edited, string seed) => string.IsNullOrEmpty(edited) ? seed : edited; // keep edited text
            data.symbols = Facts.Select(f =>
            {
                existing.TryGetValue(f.id, out var old);
                return new SlotSymbol
                {
                    id = f.id,
                    icon = S("Icon_" + f.id),
                    fact = Keep(old?.fact, f.fact),
                    factHR = Keep(old?.factHR, f.hr),
                    factTeamLead = Keep(old?.factTeamLead, f.lead),
                    factHead = Keep(old?.factHead, f.head),
                };
            }).ToList();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Slot] Fortune machine ready: {data.symbols.Count} icons ({AssetPath}).", data);
        }

        [MenuItem("Portfolio/Slot/Reset Visit (role, energy, secrets)")]
        static void ResetRun()
        {
            Energy.Reset();
            Visitor.Forget(); // the welcome screen asks again
            Debug.Log("[Slot] New visit: choose who you are again, start energy, no secrets revealed.");
        }
    }
}
