using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WavePointSceneFix
{
    static WavePointSceneFix()
    {
        EditorApplication.delayCall += AlignOpenSceneIfNeeded;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AlignOpenSceneIfNeeded;
    }

    private static void AlignOpenSceneIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        AlignOpenScene();
    }

    [MenuItem("Tools/Wave/Align Source Sprites")]
    public static void AlignOpenScene()
    {
        AlignScene(SceneManager.GetActiveScene());
    }

    private static void AlignScene(Scene scene)
    {
        int aligned = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (WaveMetaballs wave in root.GetComponentsInChildren<WaveMetaballs>(true))
                aligned += Align(wave);
        }
        if (aligned > 0)
            Debug.Log($"Aligned {aligned} wave source Sprites.");
    }

    public static void AlignProjectScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/mouseOnly.unity", OpenSceneMode.Single);
        AlignScene(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save the aligned wave scene.");
    }

    private static int Align(WaveMetaballs wave)
    {
        var serializedWave = new SerializedObject(wave);
        Transform root = serializedWave.FindProperty("waveRoot").objectReferenceValue as Transform;
        GameObject source = serializedWave.FindProperty("sourcePointPrefab").objectReferenceValue as GameObject;
        int count = serializedWave.FindProperty("pointCount").intValue;
        if (root == null || source == null)
            throw new InvalidOperationException($"{wave.name} needs Wave Root and Source Point Prefab.");

        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        var centers = new List<Vector3>();
        foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            Vector3 localCenter = source.transform.InverseTransformPoint(
                filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
            centers.Add(root.TransformPoint(localCenter));
        }
        if (sprites.Length != count || centers.Count != count)
            throw new InvalidOperationException($"{wave.name}: expected {count} Sprites and source meshes, found {sprites.Length} and {centers.Count}.");

        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        centers.Sort((a, b) => a.x.CompareTo(b.x));
        int aligned = 0;
        for (int i = 0; i < count; i++)
        {
            Transform point = sprites[i].transform;
            Quaternion targetRotation = root.rotation * Quaternion.Euler(90f, 0f, 0f);
            if (Quaternion.Angle(point.rotation, targetRotation) < 0.01f &&
                Vector3.Distance(sprites[i].bounds.center, centers[i]) < 0.001f)
                continue;
            Undo.RecordObject(point, "Align wave source Sprite");
            point.rotation = targetRotation;
            point.position += centers[i] - sprites[i].bounds.center;
            PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            aligned++;
        }
        if (aligned > 0)
            EditorSceneManager.MarkSceneDirty(wave.gameObject.scene);
        return aligned;
    }
}
