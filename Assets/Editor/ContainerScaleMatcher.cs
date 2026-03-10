using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ContainerScaleMatcher
{
    private const string MenuPath = "Tools/Match Containers To container1 (Visible Size - Exact XYZ)";

    [MenuItem(MenuPath)]
    public static void MatchExactXyz()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("No active loaded scene.");
            return;
        }

        var targets = FindObjects(scene, new[]
        {
            "container1", "container2", "container3", "container4", "container5", "container6", "container7"
        });

        if (!targets.TryGetValue("container1", out var reference) || reference == null)
        {
            Debug.LogError("Could not find GameObject named 'container1' in the active scene.");
            return;
        }

        // Measure sizes in container1's local space so rotation doesn't explode AABB axis sizes.
        if (!TryGetBoundsInSpace(reference, reference.transform, out var refBounds))
        {
            Debug.LogError("'container1' has no Renderers to measure bounds from.");
            return;
        }

        var refSize = refBounds.size;
        if (refSize.x <= 0f || refSize.y <= 0f || refSize.z <= 0f)
        {
            Debug.LogError("'container1' bounds size is zero. Cannot match sizes.");
            return;
        }

        int changed = 0;
        foreach (var kvp in targets)
        {
            var name = kvp.Key;
            var go = kvp.Value;
            if (go == null || name == "container1")
                continue;

            if (!TryGetBoundsInSpace(go, reference.transform, out var b))
            {
                Debug.LogWarning($"'{name}' has no Renderers; skipped.");
                continue;
            }

            var size = b.size;
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            {
                Debug.LogWarning($"'{name}' bounds size is zero; skipped.");
                continue;
            }

            var multiplier = new Vector3(refSize.x / size.x, refSize.y / size.y, refSize.z / size.z);

            var t = go.transform;
            Undo.RecordObject(t, "Match container visible size (Exact XYZ)");
            t.localScale = Vector3.Scale(t.localScale, multiplier);
            changed++;

            Debug.Log(
                $"[{name}] size(ref-space) {Format(size)} -> {Format(refSize)}, " +
                $"mult {Format(multiplier)}, newScale {Format(t.localScale)}");
        }

        if (changed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"Matched {changed} container(s) to container1 visible size (Exact XYZ) in scene '{scene.name}'.");
    }

    private static Dictionary<string, GameObject> FindObjects(Scene scene, IReadOnlyList<string> names)
    {
        var result = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        var roots = scene.GetRootGameObjects();
        foreach (var n in names)
        {
            result[n] = FindByNameInRoots(roots, n);
        }
        return result;
    }

    private static GameObject FindByNameInRoots(GameObject[] roots, string name)
    {
        foreach (var r in roots)
        {
            if (r == null) continue;
            if (r.name == name) return r;

            var direct = r.transform.Find(name);
            if (direct != null) return direct.gameObject;

            var found = FindInChildren(r.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindInChildren(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child == null) continue;
            if (child.name == name) return child;
            var found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static bool TryGetBoundsInSpace(GameObject go, Transform space, out Bounds bounds)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool has = false;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            EncapsulateRendererBoundsInSpace(r, space, ref bounds, ref has);
        }
        return has;
    }

    private static void EncapsulateRendererBoundsInSpace(Renderer renderer, Transform space, ref Bounds bounds, ref bool has)
    {
        // renderer.bounds is WORLD-space AABB; convert its 8 corners into the requested space and rebuild AABB there.
        var b = renderer.bounds;
        var c = b.center;
        var e = b.extents;

        Span<Vector3> corners = stackalloc Vector3[8]
        {
            c + new Vector3( e.x,  e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y, -e.z),
        };

        for (int i = 0; i < corners.Length; i++)
            corners[i] = space.InverseTransformPoint(corners[i]);

        if (!has)
        {
            bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);
            has = true;
            return;
        }

        for (int i = 0; i < corners.Length; i++)
            bounds.Encapsulate(corners[i]);
    }

    private static string Format(Vector3 v) => $"({v.x:F4}, {v.y:F4}, {v.z:F4})";
}

