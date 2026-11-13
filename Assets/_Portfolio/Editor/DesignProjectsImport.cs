using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Portfolio → Content → Import Design Projects: turns the images in Content/Projects/Images/&lt;Key&gt;/ into sprites,
    /// creates one ProjectData per design project (text from myPortfoliomk3.ai) and lists them in 03_Projects.
    /// Never overwrites a project asset that already exists, so edits in the Inspector are safe; delete an asset to rebuild it.
    /// </summary>
    public static class DesignProjectsImport
    {
        const string Root = "Assets/_Portfolio/Content/Projects";
        const string Images = Root + "/Images";
        const string StationPath = "Assets/_Portfolio/Content/Stations/03_Projects.asset";
        const string ExamplePath = Root + "/Example_Project.asset";

        class Seed
        {
            public string key, title, tagline, category, context, tools, challenge, solution, process, explodedCaption;
            public Color accent;
            public (string label, string value)[] facts = { };
            public Dictionary<string, string> captions = new();
        }

        static readonly Seed[] Seeds =
        {
            new Seed
            {
                key = "AlexaMic", title = "Arri Alexa Camera Mic",
                tagline = "A camera microphone that plugs straight into the ARRI ALEXA Mini's line-level input.",
                category = "Product design · Audio",
                accent = new Color32(107, 140, 174, 255),
                challenge = "ARRI released the ALEXA Mini camera with only a LINE level audio input, making it challenging to directly connect a microphone for in-camera sound. Additionally, an elegant physical connection was needed to integrate smoothly with the camera's accessories.",
                solution = "I developed a camera microphone with a built-in LINE level amplifier, allowing direct connection without external sound syncing. The microphone body was designed to screw directly onto the XLR plug, creating a unified, sleek appearance that blends seamlessly with the camera setup.",
                process = "To reduce production costs, I used ready-made components like the XLR plug. Since the product is intended for small production runs, 3D printing the ergonomic body saves on expensive injection moulds.\n\nTogether with an electronics engineer, we ensured uncompromising sound quality by selecting a wide-frequency-range microphone capsule and integrating a compact preamp. We also provided a set of cables for different ALEXA models, so power and audio both run through a single connector.",
                tools = "3D printing, custom PCB and preamp, ready-made XLR housing",
                facts = new[] { ("Parts", "3D-printed body, main PCB, XLR housing, camera mount"), ("Electronics", "With an electronics engineer") },
                explodedCaption = "3D-printed body, main PCB, ready-made XLR housing and camera mount",
            },
            new Seed
            {
                key = "GiantSimon", title = "Giant Simon",
                tagline = "A one-metre Simon Says game for monday.com's Purim party for employees' kids.",
                category = "Interactive installation", context = "monday.com, Purim party",
                accent = new Color32(123, 92, 198, 255),
                challenge = "MONDAY.COM held a Purim party for employees' children, aiming to create engaging and experiential games to entertain the kids throughout the event.",
                solution = "I designed a giant Simon Says game, one meter in diameter, with large touch-sensitive buttons. The buttons light up in vibrant colors to attract children and engage them in the game, challenging them to follow light patterns in an exciting and interactive way.",
                process = "The structure was crafted from laser-cut wooden layers to ensure precision and seamless assembly. The illuminated buttons were made from acrylic (Perspex), painted in the colors of the MONDAY.COM logo to keep the brand identity. The electronics were built around an Arduino microcontroller, programmed to drive high-brightness LEDs that stay visible from a distance.",
                tools = "Laser-cut MDF, painted acrylic (Perspex), Arduino, LED strip",
                facts = new[] { ("Size", "1 m diameter") },
                explodedCaption = "Touch buttons, painted acrylic, MDF layer, LED strip and the main MCU",
                captions = { ["g2"] = "Kids playing at the party", ["g3"] = "Following the light pattern" },
            },
            new Seed
            {
                key = "Umasqu", title = "Umasqu portable lamp",
                tagline = "A rechargeable mask lamp that carries light through an acrylic layer.",
                category = "Lighting · Product design",
                accent = new Color32(201, 162, 39, 255),
                solution = "Designing decorative and ambient lighting fixtures. The process includes developing a unique concept that reflects the brand's values and design language, from creating mockups and a customized electrical circuit for the product to achieving the highest precision in the prototype and its components.",
                process = "Beyond the branding, product construction and circuit, I also led the development of the technical file, including certification and regulatory approvals according to FCC and CE standards.",
                tools = "Acrylic, LEDs, rechargeable battery, charging PCB",
                facts = new[] { ("Certification", "FCC and CE") },
                explodedCaption = "Acrylic light layer, LEDs, rechargeable battery and charging PCB",
            },
            new Seed
            {
                key = "HelenaRubinstein", title = "Helena Rubinstein's Miniature Rooms",
                tagline = "Modern lighting for 17 historical miniature rooms at the Tel Aviv Museum of Art.",
                category = "Exhibition lighting", context = "Tel Aviv Museum of Art, permanent exhibit",
                accent = new Color32(142, 107, 74, 255),
                challenge = "The Tel Aviv Museum's collection of 17 miniatures from Helena Rubinstein, over 100 years old, needed a lighting solution for a permanent 20-year exhibit. The LED lighting had to bring out the miniatures' colors while minimizing harmful UV light that could damage the fragile materials. Each room had its own needs: some take natural light through windows, others need specific interior lighting. Everything had to be installed without drilling or permanently altering the original structures, and stay easy for museum staff to replace and control.",
                solution = "Together with an electronics engineer, we developed custom LED circuits tailored to each room, with about 12 LEDs per room. Each LED sits on its own PCB that doubles as a heat sink, with connectors running through the miniature ceilings for discreet power. To preserve the original wood we reused the existing lighting infrastructure in some rooms, and in others designed custom plastic holders with foam to prevent damage.",
                process = "All LEDs connect to a six-channel dimming system, so the light balance of each room can be tuned, whether for natural light reflections or enclosed spaces. The result protects the rooms, raises their visual impact and stays easy to maintain.",
                tools = "Custom LED PCBs, 3D-printed holders, 6-channel dimmer, COB LEDs",
                facts = new[] { ("Rooms", "17 miniatures, over 100 years old"), ("LEDs", "About 12 per room"), ("Control", "6-channel dimming"), ("Lifetime", "Permanent 20-year exhibit") },
                explodedCaption = "Wiring: LED feeds, replaceable LEDs, LED driver and the dimmer control box",
                captions =
                {
                    ["g1"] = "The rooms installed in the museum", ["g3"] = "Wiring through the ceiling without drilling",
                    ["g4"] = "A replaceable LED on its holder", ["g5"] = "A COB LED beam that mimics sunlight through a window",
                    ["g6"] = "An LED panel simulating soft sky light reflected in a window",
                },
            },
            new Seed
            {
                key = "Babilanur", title = "Babilanur",
                tagline = "Interactive lighting fixtures you shape through touch.",
                category = "Lighting · Interactive",
                accent = new Color32(184, 134, 43, 255),
                solution = "Interactive lighting fixtures that invite the user to experience light in a personal way, shaping it through touch. Each fixture is a unique creation, combining meticulous craftsmanship with natural materials such as wood, iron and metals, alongside advanced technology like 3D printing and microcontrollers.",
                process = "The vision behind each piece is to bring the magic of light to life, not just as a means of illumination, but as an interactive partner in the space, evoking a sense of presence, as if you are not alone, but have something you can communicate with.",
                tools = "Wood, iron, metals, 3D printing, microcontrollers",
                facts = new[] { ("Interaction", "First pet turns it on, each extra pet adds brightness, a double tap turns it off") },
                captions = { ["g2"] = "Hand-finished glass and brass", ["g4"] = "In the workshop" },
            },
        };

        [MenuItem("Portfolio/Content/Import Design Projects")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            var created = new List<ProjectData>();
            int made = 0;
            foreach (var seed in Seeds)
            {
                var dir = $"{Images}/{seed.key}";
                if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning($"[Projects] No images at {dir}"); continue; }
                var sprites = ImportSprites(dir, seed.key);

                var path = $"{Root}/{seed.key}.asset";
                var data = AssetDatabase.LoadAssetAtPath<ProjectData>(path);
                if (!data)
                {
                    data = ScriptableObject.CreateInstance<ProjectData>();
                    Fill(data, seed, sprites);
                    AssetDatabase.CreateAsset(data, path);
                    made++;
                }
                created.Add(data);
            }

            var station = AssetDatabase.LoadAssetAtPath<StationData>(StationPath);
            if (station)
            {
                station.projects = created;
                if (station.body.StartsWith("Highlights of projects"))
                    station.body = "Industrial design and lighting: products, a museum installation and interactive pieces. Open a ticket to see the story behind each one.";
                if (station.subtitle == "Selected work") station.subtitle = "Industrial design";
                EditorUtility.SetDirty(station);
            }
            if (AssetDatabase.LoadAssetAtPath<ProjectData>(ExamplePath)) AssetDatabase.DeleteAsset(ExamplePath);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Projects] {made} new, {created.Count} listed in 03_Projects.");
            Selection.activeObject = station;
        }

        static Dictionary<string, Sprite> ImportSprites(string dir, string key)
        {
            var result = new Dictionary<string, Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.mipmapEnabled)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.mipmapEnabled = false;
                    imp.maxTextureSize = 2048;
                    imp.SaveAndReimport();
                }
                var name = Path.GetFileNameWithoutExtension(path); // e.g. Babilanur_g3
                var slot = name.StartsWith(key + "_") ? name.Substring(key.Length + 1) : name;
                result[slot] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return result;
        }

        static void Fill(ProjectData d, Seed s, Dictionary<string, Sprite> sprites)
        {
            d.title = s.title; d.tagline = s.tagline; d.category = s.category; d.accent = s.accent;
            d.context = s.context ?? ""; d.tools = s.tools ?? "";
            d.challenge = s.challenge ?? ""; d.solution = s.solution ?? ""; d.process = s.process ?? "";
            d.year = ""; d.role = ""; d.duration = "";
            sprites.TryGetValue("cover", out d.cover);
            if (sprites.TryGetValue("exploded", out var ex)) { d.explodedView = ex; d.explodedCaption = s.explodedCaption ?? ""; }
            d.gallery = sprites.Keys.Where(k => k.StartsWith("g")).OrderBy(k => k)
                .Select(k => new ProjectImage { image = sprites[k], caption = s.captions.TryGetValue(k, out var c) ? c : "" }).ToList();
            d.facts = s.facts.Select(f => new ProjectFact { label = f.label, value = f.value }).ToList();
        }
    }
}
