using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
public sealed class WaveMetaballs : MonoBehaviour
{
    private const int MaxMetaballs = 64;
    private static readonly int CentersId = Shader.PropertyToID("_MetaballCenters");
    private static readonly int CountId = Shader.PropertyToID("_MetaballCount");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");

    [Header("Source Points")]
    [SerializeField] private Transform waveRoot;
    [SerializeField] private GameObject sourcePointPrefab;
    [SerializeField] private Material metaballMaterial;
    [SerializeField, Range(1, MaxMetaballs)] private int pointCount = 50;
    [SerializeField] private bool moveSourcePointsWithMetaballs;
    [SerializeField] private bool showSourcePointModels;
    [SerializeField] private int randomSeed = 12345;

    [Header("Wave Shape (World Units)")]
    [SerializeField] private Vector2 smallWaveAmplitudeRange = new Vector2(0.05f, 0.15f);
    [SerializeField] private Vector2 largeWaveAmplitudeRange = new Vector2(0.5f, 0.8f);
    [SerializeField, Min(0.01f)] private float wavelength = 5f;
    [SerializeField, Min(0f)] private float rightwardSpeed = 0.9f;

    [Header("Tide (World Units)")]
    [SerializeField] private SliderPush tideSlider;
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

    [SerializeField, Min(0.01f)] private float resumeBlendDuration = 0.4f;

    private float currentTideOffset;
    private float pausedTideOffset;
    private float resumeStartTideOffset;
    private float resumeBlendElapsed;
    private bool blendingToAutomatic;
    
    private readonly Vector4[] centers = new Vector4[MaxMetaballs];
    private Transform[] pointMarkers;
    private Vector3[] baseCenters;
    private float[] radii;
    private float[] waveAmplitudeVariation;
    private float[] waveSectionAmplitudes;
    private Transform[] sourcePointTransforms;
    private Vector3[] sourcePointStartPositions;
    private float elapsedTime;
    private float waveTravelDistance;
    private float sliderStartPosition;
    private float travelDistanceAtPause;
    private bool sliderControlActive;
    private Renderer[] sourceRenderers;
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

        SpriteRenderer[] sprites = waveRoot.GetComponentsInChildren<SpriteRenderer>(true);
        sourceRenderers = sprites.Length == pointCount
            ? sprites
            : waveRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (!TryReadPointCenters(out baseCenters))
        {
            Debug.LogError($"WaveMetaballs could not find {pointCount} source points under {waveRoot.name}.", this);
            enabled = false;
            return;
        }
        if (moveSourcePointsWithMetaballs && sourcePointTransforms == null)
        {
            Debug.LogError("Moving source points requires one SpriteRenderer or MeshRenderer per metaball.", this);
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
            sourceVisibility[i] = sourceRenderers[i].enabled;
        SetSourceVisible(false);

        pointMarkers = new Transform[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var marker = new GameObject($"Metaball {i + 1:00}");
            marker.transform.SetParent(transform, false);
            pointMarkers[i] = marker.transform;
        }

        CreateSurface();
        properties = new MaterialPropertyBlock();
        RefreshWave(0f);
    }

    private void LateUpdate()
    {
        if (surfaceRenderer != null)
            RefreshWave(Time.deltaTime);
    }

    private void RefreshWave(float deltaTime)
    {
        if (sliderControlActive)
        {
            float sweepDuration = 2f / Mathf.Max(0.01f, tideCyclesPerSecond);
            waveTravelDistance = travelDistanceAtPause + Mathf.Max(0f, rightwardSpeed) *
                sweepDuration * (tideSlider.NormalizedDownPosition - sliderStartPosition);
        }
        else
        {
            elapsedTime += deltaTime;
            if (blendingToAutomatic)
                resumeBlendElapsed += deltaTime;
            waveTravelDistance += Mathf.Max(0f, rightwardSpeed) * deltaTime;
        }
        UpdateWave(elapsedTime);
    }

    public void SwitchToSliderControl()
    {
        if (sliderControlActive)
            return;
        if (tideSlider == null)
        {
            Debug.LogWarning("WaveMetaballs needs a tide slider before it can switch to slider control.", this);
            return;
        }

        sliderStartPosition = tideSlider.NormalizedDownPosition;
        travelDistanceAtPause = waveTravelDistance;
        
        //record tide offset when pause
        pausedTideOffset = currentTideOffset;
        blendingToAutomatic = false;
        sliderControlActive = true;
    }

    public void ResumeAutomaticControl()
    {
        if (!sliderControlActive)
            return;

        // Start the blend from the last frame actually shown on screen.
        resumeStartTideOffset = currentTideOffset;

        resumeBlendElapsed = 0f;
        blendingToAutomatic = true;
        sliderControlActive = false;
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
        RestoreSourcePointPositions();
        SetSourceVisible(true);
    }

    private void OnDestroy()
    {
        if (surfaceMesh != null)
            Destroy(surfaceMesh);
    }

    private void SetSourceVisible(bool restoreOriginal)
    {
        if (sourceRenderers == null || sourceVisibility == null)
            return;

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            if (sourceRenderers[i] != null)
                sourceRenderers[i].enabled = sourceVisibility[i] &&
                    (restoreOriginal || (moveSourcePointsWithMetaballs && showSourcePointModels));
        }
    }

    private void RestoreSourcePointPositions()
    {
        if (!moveSourcePointsWithMetaballs || sourcePointTransforms == null)
            return;
        for (int i = 0; i < sourcePointTransforms.Length; i++)
        {
            if (sourcePointTransforms[i] != null)
                sourcePointTransforms[i].position = sourcePointStartPositions[i];
        }
    }

    private bool TryReadPointCenters(out Vector3[] result)
    {
        if (sourceRenderers.Length == pointCount)
        {
            result = new Vector3[pointCount];
            sourcePointTransforms = new Transform[pointCount];
            sourcePointStartPositions = new Vector3[pointCount];
            var prefabCenters = new List<Vector3>();
            if (sourceRenderers[0] is SpriteRenderer && sourcePointPrefab != null)
            {
                foreach (MeshFilter filter in sourcePointPrefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null)
                        continue;
                    Vector3 center = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
                    prefabCenters.Add(waveRoot.TransformPoint(
                        sourcePointPrefab.transform.InverseTransformPoint(center)));
                }
            }
            bool usePrefabCenters = prefabCenters.Count == pointCount;
            if (sourceRenderers[0] is SpriteRenderer && !usePrefabCenters)
            {
                Debug.LogError($"WaveMetaballs found {prefabCenters.Count} meshes in the source model, but needs {pointCount} to position the Sprites.", this);
                result = null;
                return false;
            }
            prefabCenters.Sort((a, b) => a.x.CompareTo(b.x));
            int[] sortedIndices = new int[pointCount];
            for (int i = 0; i < pointCount; i++)
                sortedIndices[i] = i;
            Array.Sort(sortedIndices, (a, b) =>
                usePrefabCenters
                    ? string.CompareOrdinal(sourceRenderers[a].name, sourceRenderers[b].name)
                    : sourceRenderers[a].bounds.center.x.CompareTo(sourceRenderers[b].bounds.center.x));
            for (int i = 0; i < pointCount; i++)
            {
                Renderer renderer = sourceRenderers[sortedIndices[i]];
                result[i] = usePrefabCenters ? prefabCenters[i] : renderer.bounds.center;
                sourcePointTransforms[i] = renderer.transform;
                if (usePrefabCenters)
                {
                    // The source meshes lie on XZ; sprites start on their local XY plane.
                    renderer.transform.rotation = waveRoot.rotation * Quaternion.Euler(90f, 0f, 0f);
                    renderer.transform.position += result[i] - renderer.bounds.center;
                }
                sourcePointStartPositions[i] = renderer.transform.position;
            }
            return true;
        }


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
        // Slider motion is centered on the tide position captured at pause.
        float maxTideOffset = tideSlider != null ? 2f * tideDistance : tideDistance;
        minZ -= maxWaveAmplitude + maxTideOffset + margin;
        maxZ += maxWaveAmplitude + maxTideOffset + margin;
        // Place the visible point models just above the fused surface in the top-down scene.
        float surfaceY = baseCenters[0].y +
            (moveSourcePointsWithMetaballs && showSourcePointModels ? -0.01f : 0.01f);

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
        float automaticTideOffset = tideDistance * Mathf.Sin(2f * Mathf.PI * tideCyclesPerSecond * time);
        float tideOffset = automaticTideOffset;
        if (sliderControlActive)
            tideOffset = pausedTideOffset - tideDistance * Mathf.Sin(4f * Mathf.PI *
                (tideSlider.NormalizedDownPosition - sliderStartPosition));
        else if (blendingToAutomatic)
        {
            float blendProgress = Mathf.Clamp01(resumeBlendElapsed / Mathf.Max(0.01f, resumeBlendDuration));
            float easedProgress = blendProgress * blendProgress * (3f - 2f * blendProgress);
            tideOffset = Mathf.Lerp(resumeStartTideOffset, automaticTideOffset, easedProgress);
            if (blendProgress >= 1f)
                blendingToAutomatic = false;
        }
        currentTideOffset = tideOffset;
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
            if (moveSourcePointsWithMetaballs)
                // Keep each source point at the same center as its metaball.
                sourcePointTransforms[i].position = sourcePointStartPositions[i] + (position - baseCenters[i]);
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
        SpriteRenderer[] sprites = waveRoot.GetComponentsInChildren<SpriteRenderer>(true);
        Renderer[] renderers = sprites.Length == pointCount
            ? sprites
            : waveRoot.GetComponentsInChildren<MeshRenderer>(true);
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
            foreach (Renderer renderer in renderers)
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
