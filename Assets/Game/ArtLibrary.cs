// Every model the game can show, the box it must fit, and how it is fitted.
//
// ONE PLACE FOR THE SIZES. Each spec's size is the grey-box shape it replaces,
// which is the sim's hit area (SimConfig, HeroDef) - Docs/ArtPrompts.md quotes
// these and this file wins if they disagree. A model never sets its own size:
// whatever scale Tripo exported at, it is scaled uniformly into its box, so a
// briefcase can never look smaller than the thing you hit.
//
// Models live in Assets/Resources/Art/... and are found by name. A missing
// model is normal (the grey-box shape is drawn instead) and is not logged.

using System.Collections.Generic;
using PeachMe.SimCore;
using UnityEngine;

namespace PeachMe.Game
{
    public enum Pivot
    {
        Base,   // the box's bottom centre sits on the holder (heroes, obstacles, props)
        Centre  // the box's centre sits on the holder (peach, pickups)
    }

    public enum FitBy
    {
        Inside,  // largest uniform scale that keeps every axis inside Size
        Height,  // Size.y exactly; width follows the model's own proportions
        Largest  // the model's longest axis equals Size's longest axis
    }

    public readonly struct ArtSpec
    {
        public readonly string Path;     // under Resources, no extension
        public readonly Vector3 Size;    // metres: x across the track, y up, z along it
        public readonly Pivot Pivot;
        public readonly FitBy Fit;
        public readonly float Y;         // where the holder sits above the ground
        public readonly float Yaw;       // degrees; 180 if a model was made facing -Z

        public ArtSpec(string path, Vector3 size, Pivot pivot, FitBy fit, float y = 0f, float yaw = 0f)
        {
            Path = path;
            Size = size;
            Pivot = pivot;
            Fit = fit;
            Y = y;
            Yaw = yaw;
        }
    }

    public static class ArtSpecs
    {
        // The peach: a ball 2.8 m across (the grey-box ball's radius 1.4), centred.
        public const float PeachRadius = 1.4f;
        public static readonly ArtSpec PeachAngry = new ArtSpec("Art/Peach/peach_angry", Vector3.one * PeachRadius * 2f, Pivot.Centre, FitBy.Largest);
        public static readonly ArtSpec PeachSmug = new ArtSpec("Art/Peach/peach_smug", Vector3.one * PeachRadius * 2f, Pivot.Centre, FitBy.Largest);
        // Squashed sits on the ground, wider and lower than the ball. A CHOICE: 1.25x wide, 0.6x tall.
        public static readonly ArtSpec PeachSquashed = new ArtSpec("Art/Peach/peach_squashed",
            new Vector3(PeachRadius * 2.5f, PeachRadius * 1.2f, PeachRadius * 2.5f), Pivot.Base, FitBy.Inside);

        public static readonly ArtSpec StateLine = new ArtSpec("Art/Track/track_stateline", new Vector3(9f, 4f, 1.2f), Pivot.Base, FitBy.Inside);

        /// <summary>Hero height: Abe is drawn tall; everyone else 1.8 m. Width is the sim's hit width.</summary>
        public static float HeroHeight(HeroDef hero)
        {
            return hero.Id == "abe" ? 2.2f : 1.8f;
        }

        public static ArtSpec ForHero(HeroDef hero)
        {
            float w = hero.HalfWidth * 2f;
            return new ArtSpec("Art/Heroes/hero_" + hero.Id, new Vector3(w, HeroHeight(hero), w), Pivot.Base, FitBy.Height);
        }

        public static ArtSpec ForItem(ItemKind kind)
        {
            switch (kind)
            {
                // Obstacles: inside the hit box. Jump, slide, change lane.
                case ItemKind.TaxForms: return new ArtSpec("Art/Items/item_taxforms", new Vector3(1.5f, 0.6f, 0.6f), Pivot.Base, FitBy.Inside);
                // Red tape: posts to the ground, bar on top. The bar is the top 0.35 m, so its
                // underside is at 1.2 m - the slide clears it, standing does not.
                case ItemKind.RedTape: return new ArtSpec("Art/Items/item_redtape", new Vector3(2.1f, RedTapeTop, 0.35f), Pivot.Base, FitBy.Inside);
                case ItemKind.Briefcase: return new ArtSpec("Art/Items/item_briefcase", new Vector3(1.5f, 1.9f, 0.8f), Pivot.Base, FitBy.Inside);
                // Pickups: centred, at chest/knee height.
                case ItemKind.Star: return new ArtSpec("Art/Items/item_star", new Vector3(0.45f, 0.45f, 0.1f), Pivot.Centre, FitBy.Largest, y: 0.8f);
                case ItemKind.HotDog: return new ArtSpec("Art/Items/item_hotdog", new Vector3(1.0f, 0.35f, 0.35f), Pivot.Centre, FitBy.Largest, y: 0.6f);
                // Hazards: lying on the ground.
                case ItemKind.FakeNews: return new ArtSpec("Art/Items/item_fakenews", new Vector3(0.9f, 0.4f, 0.4f), Pivot.Centre, FitBy.Largest, y: 0.2f);
                default: return new ArtSpec("Art/Items/item_flipflop", new Vector3(0.7f, 0.12f, 1.3f), Pivot.Base, FitBy.Inside);
            }
        }

        public const float RedTapeTop = 1.55f;
        public const float RedTapeBar = 0.35f;
    }

    public static class ArtLibrary
    {
        // null is cached too: a model that isn't there is looked for once.
        private static readonly Dictionary<string, GameObject> Loaded = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static bool Has(ArtSpec spec)
        {
            return Load(spec.Path) != null;
        }

        /// <summary>
        /// A holder with the model fitted into spec's box, parented to parent; or
        /// null if there is no model (draw the grey-box shape instead).
        /// The holder is what callers move. They never touch the model's own
        /// rotation - the importer's root rotation is kept and composed with
        /// (FlightSim2026: "never write an imported model's root rotation").
        /// </summary>
        public static Transform TryMake(ArtSpec spec, Transform parent, string name)
        {
            var prefab = Load(spec.Path);
            if (prefab == null) return null; // no model yet: the caller draws the grey box

            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            var fit = new GameObject("fit").transform;
            fit.SetParent(holder, false);
            fit.localRotation = Quaternion.Euler(0f, spec.Yaw, 0f);
            Object.Instantiate(prefab, fit, false);

            Strip(holder);

            // Measure in the holder's own space, at scale 1, with the yaw applied.
            if (!LocalBounds(holder, out Bounds b) || b.size.x <= 0f || b.size.y <= 0f || b.size.z <= 0f)
            {
                if (Warned.Add(spec.Path))
                {
                    Debug.LogWarning($"[Art] {spec.Path} has no visible size - check the import. Drawing the grey box.");
                }
                Object.Destroy(holder.gameObject);
                return null;
            }

            float s = Scale(spec, b.size);
            fit.localScale = Vector3.one * s;

            // Scaling about the holder's origin scales the bounds about it too.
            Vector3 c = b.center * s;
            Vector3 min = b.min * s;
            fit.localPosition = spec.Pivot == Pivot.Base
                ? new Vector3(-c.x, -min.y, -c.z)
                : -c;
            return holder;
        }

        private static GameObject Load(string path)
        {
            if (Loaded.TryGetValue(path, out var go)) return go;
            go = Resources.Load<GameObject>(path);
            Loaded[path] = go;
            return go;
        }

        private static float Scale(ArtSpec spec, Vector3 size)
        {
            switch (spec.Fit)
            {
                case FitBy.Height:
                    return spec.Size.y / size.y;
                case FitBy.Largest:
                    return Max(spec.Size) / Max(size);
                default:
                    return Mathf.Min(spec.Size.x / size.x, Mathf.Min(spec.Size.y / size.y, spec.Size.z / size.z));
            }
        }

        private static float Max(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }

        // The sim does every collision; imported colliders would only cost time.
        // Shadows off: grey-box lighting, and cheap on phones.
        private static void Strip(Transform root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true)) Object.Destroy(col);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static bool LocalBounds(Transform holder, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            var toLocal = holder.worldToLocalMatrix;
            foreach (var r in holder.GetComponentsInChildren<Renderer>(true))
            {
                var wb = r.bounds;
                var e = wb.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = wb.center + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z);
                    var p = toLocal.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any;
        }
    }
}
