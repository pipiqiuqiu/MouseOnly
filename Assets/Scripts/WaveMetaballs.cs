using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveMetaballs : MonoBehaviour
{
    private const int MaxMetaballs = 64;
    private static readonly int CentersId = Shader.PropertyToID("_MetaballCenters");
    private static readonly int CountId = Shader.PropertyToID("_MetaballCount");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");

    [Header("Source Points")]
    [SerializeField] private Transform waveRoot;
    [SerializeField] private Material metaballMaterial;
    [SerializeField, Range(1, MaxMetaballs)] private int pointCount = 50;
    [SerializeField] private int randomSeed = 12345;

    [Header("Wave Shape (World Units)")]
    [SerializeField] private Vector2 smallWaveAmplitudeRange = new Vector2(0.05f, 0.15f);
    [SerializeField] private Vector2 largeWaveAmplitudeRange = new Vector2(0.5f, 0.8f);
    [SerializeField, Min(0.01f)] private float wavelength = 5f;
    [SerializeField, Min(0f)] private float rightwardSpeed = 0.9f;

    [Header("Tide (World Units)")]
    [SerializeField, Min(0f)] private float tideDistance = 1f;
    [SerializeField, Min(0f)] private float tideCyclesPerSecond = 0.12f;

    [Header("Metaballs (World Units)")]
    [SerializeField] private Vector2 radiusRange = new Vector2(0.27f, 0.37f);
    [SerializeField, Min(0.001f)] private float blend = 0.18f;

    [Header("Shoreline Size Animation")]
    [SerializeField] private Transform shoreline;
    [SerializeField, Min(0f)] private float fullSizeDistance = 1.2f;
    [SerializeField, Min(0f)] private float minimumSizeDistance = 4.2f;
    [SerializeField] private Vector2 shoreSizeScaleRange = new Vector2(0.75f, 1.35f);

    private readonly Vector4[] centers = new Vector4[MaxMetaballs];
    private Transform[] pointMarkers;
    private Vector3[] baseCenters;
    private float[] radii;
    private float[] waveAmplitudeVariation;
    private float[] waveSectionAmplitudes;
    private float elapsedTime;
    private float waveTravelDistance;
    private MeshRenderer[] sourceRenderers;
    private bool[] sourceVisibility;
    private MeshRenderer surfaceRenderer;
    private Mesh surfaceMesh;
    private MaterialPropertyBlock properties;

    private void Awake()
    {
        if (waveRoot == null || metaballMaterial == null || shoreline == null)
        {
            Debug.LogError("WaveMetaballs needs the wave root, metaball material, and shoreline reference.", this);
            enabled = false;
            return;
        }

        sourceRenderers = waveRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (!TryReadPointCenters(out baseCenters))
        {
            Debug.LogError($"WaveMetaballs could not find {pointCount} source points under {waveRoot.name}.", this);
            enabled = false;
            return;
        }

        radii = new float[pointCount];
        waveAmplitudeVariation = new float[pointCount];
        var random = new System.Random(randomSeed);
        float waveLength = Mathf.Max(0.01f, wavelength);
        int sectionCount = Mathf.Max(3, Mathf.CeilToInt((baseCenters[pointCount - 1].x - baseCenters[0].x) / waveLength) + 1);
        waveSectionAmplitudes = new float[sectionCount];
        bool hasLargeWave = false;
        bool hasSmallWave = false;
        for (int i = 0; i < sectionCount; i++)
        {
            bool large = random.NextDouble() < 0.5;
            if (i == sectionCount - 1)
            {
                if (!hasLargeWave) large = true;
                else if (!hasSmallWave) large = false;
            }
            waveSectionAmplitudes[i] = RandomInRange(random,
                large ? largeWaveAmplitudeRange : smallWaveAmplitudeRange);
            hasLargeWave |= large;
            hasSmallWave |= !large;
        }
        for (int i = 0; i < pointCount; i++)
        {
            radii[i] = Mathf.Max(0.01f, RandomInRange(random, radiusRange));
            waveAmplitudeVariation[i] = Mathf.Lerp(0.9f, 1.1f, (float)random.NextDouble());
        }

        sourceVisibility = new bool[sourceRenderers.Length];
        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            sourceVisibility[i] = sourceRenderers[i].enabled;
            sourceRenderers[i].enabled = false;
        }

        pointMarkers = new Transform[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var marker = new GameObject($"Metaball {i + 1:00}");
            marker.transform.SetParent(transform, false);
            pointMarkers[i] = marker.transform;
        }

        CreateSurface();
        properties = new MaterialPropertyBlock();
        UpdateWave(0f);
    }

    private void Update()
    {
        if (surfaceRenderer != null)
        {
            elapsedTime += Time.deltaTime;
            waveTravelDistance += Mathf.Max(0f, rightwardSpeed) * Time.deltaTime;
            UpdateWave(elapsedTime);
        }
    }

    private static float RandomInRange(System.Random random, Vector2 range)
    {
        float low = Mathf.Min(range.x, range.y);
        float high = Mathf.Max(range.x, range.y);
        return Mathf.Lerp(low, high, (float)random.NextDouble());
    }

    private void OnEnable()
    {
        if (surfaceRenderer != null)
            surfaceRenderer.enabled = true;
        SetSourceVisible(false);
    }

    private void OnDisable()
    {
        if (surfaceRenderer != null)
            surfaceRenderer.enabled = false;
        SetSourceVisible(true);
    }

    private void OnDestroy()
    {
        if (surfaceMesh != null)
            Destroy(surfaceMesh);
    }

    private void SetSourceVisible(bool visible)
    {
        if (sourceRenderers == null || sourceVisibility == null)
            return;

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            if (sourceRenderers[i] != null)
                sourceRenderers[i].enabled = visible && sourceVisibility[i];
        }
    }

    private bool TryReadPointCenters(out Vector3[] result)
    {
        // Most OBJ imports create a renderer for each original point mesh.
        if (sourceRenderers.Length == pointCount)
        {
            result = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
                result[i] = sourceRenderers[i].bounds.center;
            Array.Sort(result, (a, b) => a.x.CompareTo(b.x));
            return true;
        }

        // Some import settings combine the OBJ groups into one renderer. In that
        // case, each disconnected piece of the mesh is one original point.
        var points = new List<Vector3>();
        foreach (MeshFilter filter in waveRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null)
                continue;
            if (!mesh.isReadable)
            {
                Debug.LogError($"Enable Read/Write on the source mesh {mesh.name} to locate its points.", this);
                result = null;
                return false;
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int[] parents = new int[vertices.Length];
            bool[] used = new bool[vertices.Length];
            for (int i = 0; i < parents.Length; i++)
                parents[i] = i;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                used[a] = used[b] = used[c] = true;
                Join(parents, a, b);
                Join(parents, a, c);
            }

            var sums = new Dictionary<int, Vector3>();
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!used[i])
                    continue;
                int root = Find(parents, i);
                sums[root] = sums.TryGetValue(root, out Vector3 sum) ? sum + vertices[i] : vertices[i];
                counts[root] = counts.TryGetValue(root, out int count) ? count + 1 : 1;
            }
            foreach (var entry in sums)
                points.Add(filter.transform.TransformPoint(entry.Value / counts[entry.Key]));
        }

        points.Sort((a, b) => a.x.CompareTo(b.x));
        result = points.ToArray();
        return result.Length == pointCount;
    }

    private static int Find(int[] parents, int i)
    {
        while (parents[i] != i)
        {
            parents[i] = parents[parents[i]];
            i = parents[i];
        }
        return i;
    }

    private static void Join(int[] parents, int a, int b)
    {
        parents[Find(parents, a)] = Find(parents, b);
    }

    private void CreateSurface()
    {
        float maxRadius = Mathf.Max(radiusRange.x, radiusRange.y, 0.01f);
        float maxWaveAmplitude = 1.1f * Mathf.Max(
            Mathf.Abs(smallWaveAmplitudeRange.x), Mathf.Abs(smallWaveAmplitudeRange.y),
            Mathf.Abs(largeWaveAmplitudeRange.x), Mathf.Abs(largeWaveAmplitudeRange.y));
        float maxSizeScale = Mathf.Max(0.01f, shoreSizeScaleRange.x, shoreSizeScaleRange.y);
        float margin = maxRadius * maxSizeScale + blend + 0.1f;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
        foreach (Vector3 point in baseCenters)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }
        minX -= margin;
        maxX += margin;
        minZ -= maxWaveAmplitude + tideDistance + margin;
        maxZ += maxWaveAmplitude + tideDistance + margin;
        float surfaceY = baseCenters[0].y + 0.01f;

        surfaceMesh = new Mesh { name = "Wave Metaball Surface" };
        surfaceMesh.vertices = new[]
        {
            transform.InverseTransformPoint(new Vector3(minX, surfaceY, minZ)),
            transform.InverseTransformPoint(new Vector3(minX, surfaceY, maxZ)),
            transform.InverseTransformPoint(new Vector3(maxX, surfaceY, maxZ)),
            transform.InverseTransformPoint(new Vector3(maxX, surfaceY, minZ))
        };
        surfaceMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        surfaceMesh.RecalculateBounds();

        var surface = new GameObject("Wave Metaball Surface");
        surface.transform.SetParent(transform, false);
        surface.AddComponent<MeshFilter>().sharedMesh = surfaceMesh;
        surfaceRenderer = surface.AddComponent<MeshRenderer>();
        surfaceRenderer.sharedMaterial = metaballMaterial;
        surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        surfaceRenderer.receiveShadows = false;
    }

    private void UpdateWave(float time)
    {
        float baseX = baseCenters[0].x;
        float tideOffset = tideDistance * Mathf.Sin(2f * Mathf.PI * tideCyclesPerSecond * time);
        float shapeGrowth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time));
        float waveLength = Mathf.Max(0.01f, wavelength);
        float shoreZ = shoreline.position.z;
        float nearDistance = Mathf.Max(0f, fullSizeDistance);
        float farDistance = Mathf.Max(nearDistance + 0.01f, minimumSizeDistance);
        float farScale = Mathf.Max(0.01f, Mathf.Min(shoreSizeScaleRange.x, shoreSizeScaleRange.y));
        float nearScale = Mathf.Max(farScale, Mathf.Max(shoreSizeScaleRange.x, shoreSizeScaleRange.y));
        for (int i = 0; i < pointMarkers.Length; i++)
        {
            Vector3 position = baseCenters[i];
            float wavePosition = (position.x - baseX - waveTravelDistance) / waveLength;
            int section = Mathf.FloorToInt(wavePosition);
            float sectionBlend = Mathf.SmoothStep(0f, 1f, wavePosition - section);
            int left = ((section % waveSectionAmplitudes.Length) + waveSectionAmplitudes.Length) % waveSectionAmplitudes.Length;
            int right = (left + 1) % waveSectionAmplitudes.Length;
            float waveAmplitude = Mathf.Lerp(waveSectionAmplitudes[left], waveSectionAmplitudes[right], sectionBlend)
                * waveAmplitudeVariation[i];
            position.z += shapeGrowth * waveAmplitude * Mathf.Sin(2f * Mathf.PI * wavePosition) + tideOffset;
            pointMarkers[i].position = position;
            float distanceToShore = Mathf.Abs(position.z - shoreZ);
            float shoreBlend = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(nearDistance, farDistance, distanceToShore));
            float currentRadius = radii[i] * Mathf.Lerp(nearScale, farScale, shoreBlend);
            pointMarkers[i].localScale = Vector3.one * (2f * currentRadius);
            centers[i] = new Vector4(position.x, position.z, currentRadius, 0f);
        }

        properties.SetVectorArray(CentersId, centers);
        properties.SetFloat(CountId, pointMarkers.Length);
        properties.SetFloat(BlendId, blend);
        surfaceRenderer.SetPropertyBlock(properties);
    }

    private void OnDrawGizmosSelected()
    {
        if (waveRoot == null)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        MeshRenderer[] renderers = waveRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length != pointCount)
            return;
        float previewRadius = Mathf.Max(0.01f, (radiusRange.x + radiusRange.y) * 0.5f);
        for (int i = 0; i < renderers.Length; i++)
        {
            Gizmos.DrawWireSphere(renderers[i].bounds.center, previewRadius);
        }

        if (shoreline != null)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            foreach (MeshRenderer renderer in renderers)
            {
                minX = Mathf.Min(minX, renderer.bounds.center.x);
                maxX = Mathf.Max(maxX, renderer.bounds.center.x);
            }
            Gizmos.color = Color.yellow;
            float y = renderers[0].bounds.center.y + 0.02f;
            Gizmos.DrawLine(new Vector3(minX - 1f, y, shoreline.position.z),
                new Vector3(maxX + 1f, y, shoreline.position.z));
        }
    }
}
