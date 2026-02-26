using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionDemolition : MonoBehaviour
{
    public static MissionDemolition S;

    [Header("Level Prefabs (optional)")]
    public GameObject[] castlePrefabs;
    public string[] levelNames;

    [Header("Procedural Fallback Prefabs")]
    public GameObject wallPrefab;
    public GameObject slabPrefab;
    public GameObject goalPrefab;
    public float groundY = -9f;

    [Header("Structure Physics")]
    public float wallStackStep = 1f;
    public float wallMass = 7f;
    public float slabMass = 9f;
    public float structureDrag = 0.01f;
    public float structureAngularDrag = 0.35f;
    public float structureMaxAngularVelocity = 10f;

    [Header("Timing")]
    public float levelAdvanceDelay = 2f;
    public float levelIntroDuration = 1.25f;

    [Header("Dynamic")]
    [SerializeField] private int level = 0;
    [SerializeField] private int shotsThisLevel = 0;
    [SerializeField] private int shotsTotal = 0;
    [SerializeField] private float transitionRemaining = 0f;
    [SerializeField] private float introRemaining = 0f;
    [SerializeField] private bool currentLevelUsesPrefab = false;

    private GameObject currentLevelRoot;
    private bool isTransitioning;
    private Slingshot slingshot;
    private GUIStyle hudStyle;
    private GUIStyle bannerStyle;

    public bool SlingshotIsEnabled
    {
        get { return !isTransitioning; }
    }

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        slingshot = FindObjectOfType<Slingshot>();
        LoadLevel(0);
    }

    void Update()
    {
        if (introRemaining > 0f)
        {
            introRemaining -= Time.deltaTime;
        }

        if (isTransitioning) return;

        if (Goal.goalMet)
        {
            StartCoroutine(AdvanceLevelAfterDelay());
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadCurrentLevel();
        }
    }

    public void RegisterShot()
    {
        shotsThisLevel++;
        shotsTotal++;
    }

    public void ReloadCurrentLevel()
    {
        if (isTransitioning) return;
        LoadLevel(level);
    }

    IEnumerator AdvanceLevelAfterDelay()
    {
        isTransitioning = true;
        transitionRemaining = Mathf.Max(0f, levelAdvanceDelay);
        while (transitionRemaining > 0f)
        {
            transitionRemaining -= Time.deltaTime;
            yield return null;
        }

        int next = level + 1;
        if (next >= LevelCount())
        {
            next = 0;
        }

        LoadLevel(next);
        transitionRemaining = 0f;
        isTransitioning = false;
    }

    void LoadLevel(int levelIndex)
    {
        CleanupBeforeLoad();

        Goal.goalMet = false;
        shotsThisLevel = 0;
        level = levelIndex;

        List<GameObject> usablePrefabs = GetUsableCastlePrefabs();
        if (levelIndex < usablePrefabs.Count)
        {
            currentLevelRoot = Instantiate(usablePrefabs[levelIndex]);
            currentLevelRoot.name = "Castle_" + levelIndex;
            currentLevelUsesPrefab = true;
        }
        else
        {
            currentLevelRoot = BuildProceduralCastle(levelIndex);
            currentLevelUsesPrefab = false;
        }

        TuneLevelRigidbodies(currentLevelRoot);
        FollowCam.POI = null;
        introRemaining = levelIntroDuration;
    }

    void CleanupBeforeLoad()
    {
        if (slingshot != null)
        {
            slingshot.CancelAiming();
        }

        if (currentLevelRoot != null)
        {
            Destroy(currentLevelRoot);
            currentLevelRoot = null;
        }

        DestroyLegacySceneCastles();

        Projectile[] projectiles = FindObjectsOfType<Projectile>();
        foreach (Projectile proj in projectiles)
        {
            if (proj != null)
            {
                Destroy(proj.gameObject);
            }
        }

        ProjectileLine[] lines = FindObjectsOfType<ProjectileLine>();
        foreach (ProjectileLine line in lines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
    }

    void DestroyLegacySceneCastles()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t == null || t.parent != null) continue;

            string objName = t.gameObject.name;
            bool looksLikeCastleRoot = objName == "Castle" || objName.StartsWith("Castle_");
            if (!looksLikeCastleRoot) continue;

            if (currentLevelRoot != null && t.gameObject == currentLevelRoot) continue;
            Destroy(t.gameObject);
        }
    }

    int LevelCount()
    {
        int prefabCount = GetUsableCastlePrefabs().Count;
        return Mathf.Max(prefabCount, 6);
    }

    List<GameObject> GetUsableCastlePrefabs()
    {
        List<GameObject> usable = new List<GameObject>();
        if (castlePrefabs == null) return usable;

        foreach (GameObject go in castlePrefabs)
        {
            if (go != null)
            {
                usable.Add(go);
            }
        }

        return usable;
    }

    GameObject BuildProceduralCastle(int levelIndex)
    {
        GameObject root = new GameObject("Castle_" + levelIndex);
        int layout = levelIndex % 6;
        float wallH = GetWallHeight();
        float slabH = GetSlabHeight();
        float wallBaseY = groundY + wallH * 0.5f;
        float slabOnTop(int stories) { return groundY + stories * wallH + slabH * 0.5f; }
        float goalUnderSlab(int stories) { return slabOnTop(stories) - (slabH * 0.5f + 1.2f); }
        Vector3 V(float x, float y) { return new Vector3(x, y, 0f); }

        if (wallPrefab == null || slabPrefab == null || goalPrefab == null)
        {
            return root;
        }

        if (layout == 0)
        {
            // Fort Gate: goal protected by a roof and front towers.
            SpawnTower(root.transform, V(20f, wallBaseY), 5);
            SpawnTower(root.transform, V(28f, wallBaseY), 5);
            SpawnTower(root.transform, V(18f, wallBaseY), 3);
            SpawnTower(root.transform, V(30f, wallBaseY), 3);
            SpawnSlab(root.transform, V(24f, slabOnTop(5)));
            SpawnSlab(root.transform, V(24f, slabOnTop(4)));
            SpawnGoal(root.transform, V(24f, goalUnderSlab(4)));
        }
        else if (layout == 1)
        {
            // Triple Keep: high side towers with suspended central roof.
            SpawnTower(root.transform, V(30f, wallBaseY), 6);
            SpawnTower(root.transform, V(36f, wallBaseY), 6);
            SpawnTower(root.transform, V(33f, wallBaseY), 4);
            SpawnSlab(root.transform, V(33f, slabOnTop(7)));
            SpawnSlab(root.transform, V(33f, slabOnTop(6)));
            SpawnGoal(root.transform, V(33f, goalUnderSlab(6)));
        }
        else if (layout == 2)
        {
            // Sky Bridge: goal under bridge with side supports.
            SpawnTower(root.transform, V(38f, wallBaseY), 7);
            SpawnTower(root.transform, V(48f, wallBaseY), 7);
            SpawnTower(root.transform, V(43f, wallBaseY), 3);
            SpawnSlab(root.transform, V(43f, slabOnTop(7)));
            SpawnSlab(root.transform, V(40.8f, slabOnTop(7)));
            SpawnSlab(root.transform, V(45.2f, slabOnTop(7)));
            SpawnGoal(root.transform, V(43f, goalUnderSlab(7)));
        }
        else if (layout == 3)
        {
            // Courtyard: front wall, internal pillars, roof pocket for goal.
            SpawnTower(root.transform, V(52f, wallBaseY), 5);
            SpawnTower(root.transform, V(62f, wallBaseY), 5);
            SpawnTower(root.transform, V(56f, wallBaseY), 6);
            SpawnTower(root.transform, V(58f, wallBaseY), 6);
            SpawnSlab(root.transform, V(57f, slabOnTop(6)));
            SpawnSlab(root.transform, V(57f, slabOnTop(5)));
            SpawnGoal(root.transform, V(57f, goalUnderSlab(5)));
        }
        else if (layout == 4)
        {
            // Staggered Bastion: uneven heights so rebounds are less predictable.
            // Keep this one closer so it is challenging, not impossible.
            SpawnTower(root.transform, V(44f, wallBaseY), 4);
            SpawnTower(root.transform, V(48f, wallBaseY), 6);
            SpawnTower(root.transform, V(52f, wallBaseY), 5);
            SpawnTower(root.transform, V(56f, wallBaseY), 7);
            SpawnSlab(root.transform, V(49.5f, slabOnTop(6)));
            SpawnSlab(root.transform, V(54f, slabOnTop(7)));
            SpawnGoal(root.transform, V(51.5f, slabOnTop(5)));
        }
        else
        {
            // Citadel: dense multi-column shell around a central goal.
            SpawnTower(root.transform, V(84f, wallBaseY), 6);
            SpawnTower(root.transform, V(88f, wallBaseY), 8);
            SpawnTower(root.transform, V(92f, wallBaseY), 8);
            SpawnTower(root.transform, V(96f, wallBaseY), 6);
            SpawnSlab(root.transform, V(90f, slabOnTop(8)));
            SpawnSlab(root.transform, V(90f, slabOnTop(7)));
            SpawnSlab(root.transform, V(90f, slabOnTop(6)));
            SpawnGoal(root.transform, V(90f, goalUnderSlab(6)));
        }

        return root;
    }

    void SpawnTower(Transform parent, Vector3 basePos, int wallCount)
    {
        float step = GetWallHeight();
        for (int i = 0; i < wallCount; i++)
        {
            Vector3 pos = basePos + new Vector3(0f, i * step, 0f);
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
            wall.transform.SetParent(parent);
            ConfigureStructurePiece(wall, false);
        }
    }

    void SpawnSlab(Transform parent, Vector3 pos)
    {
        GameObject slab = Instantiate(slabPrefab, pos, Quaternion.identity);
        slab.transform.SetParent(parent);
        ConfigureStructurePiece(slab, true);
    }

    void SpawnGoal(Transform parent, Vector3 pos)
    {
        GameObject goal = Instantiate(goalPrefab, pos, Quaternion.identity);
        goal.transform.SetParent(parent);
    }

    void OnGUI()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label);
            hudStyle.fontSize = 22;
            hudStyle.normal.textColor = Color.black;
            hudStyle.fontStyle = FontStyle.Bold;
        }
        if (bannerStyle == null)
        {
            bannerStyle = new GUIStyle(GUI.skin.box);
            bannerStyle.fontSize = 28;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.fontStyle = FontStyle.Bold;
        }

        int totalLevels = LevelCount();
        string levelDisplayName = GetLevelName(level);
        GUI.Label(new Rect(20, 20, 700, 30), "Level: " + (level + 1) + "/" + totalLevels + " - " + levelDisplayName, hudStyle);
        GUI.Label(new Rect(20, 50, 400, 30), "Layout source: " + (currentLevelUsesPrefab ? "Prefab" : "Procedural"), hudStyle);
        GUI.Label(new Rect(20, 80, 300, 30), "Shots (level): " + shotsThisLevel, hudStyle);
        GUI.Label(new Rect(20, 110, 300, 30), "Shots (total): " + shotsTotal, hudStyle);
        GUI.Label(new Rect(20, 140, 500, 30), "Press R to reload current level", hudStyle);
        if (introRemaining > 0f)
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 220, 20, 440, 50), "Now Playing: " + levelDisplayName, bannerStyle);
        }
        if (Goal.goalMet || isTransitioning)
        {
            float seconds = Mathf.Max(0f, transitionRemaining);
            GUI.Box(new Rect(Screen.width * 0.5f - 220, 80, 440, 50), "Goal hit - next level in " + seconds.ToString("0.0") + "s");
        }
    }

    string GetLevelName(int levelIndex)
    {
        if (levelNames != null && levelIndex >= 0 && levelIndex < levelNames.Length && !string.IsNullOrWhiteSpace(levelNames[levelIndex]))
        {
            return levelNames[levelIndex];
        }

        switch (levelIndex % 6)
        {
            case 0: return "Fort Gate";
            case 1: return "Triple Keep";
            case 2: return "Sky Bridge";
            case 3: return "Courtyard";
            case 4: return "Staggered Bastion";
            default: return "Citadel";
        }
    }

    void ConfigureStructurePiece(GameObject go, bool isSlab)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.mass = isSlab ? slabMass : wallMass;
        rb.linearDamping = structureDrag;
        rb.angularDamping = structureAngularDrag;
        rb.maxAngularVelocity = structureMaxAngularVelocity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void TuneLevelRigidbodies(GameObject levelRoot)
    {
        if (levelRoot == null) return;

        Rigidbody[] allBodies = levelRoot.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in allBodies)
        {
            if (rb == null) continue;
            bool isProjectile = rb.GetComponent<Projectile>() != null;
            if (isProjectile) continue;

            bool isSlab = rb.gameObject.name.ToLower().Contains("slab");
            rb.mass = isSlab ? slabMass : wallMass;
            rb.linearDamping = structureDrag;
            rb.angularDamping = structureAngularDrag;
            rb.maxAngularVelocity = structureMaxAngularVelocity;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    float GetWallHeight()
    {
        if (wallPrefab == null) return Mathf.Max(0.1f, wallStackStep);
        return Mathf.Max(0.1f, Mathf.Abs(wallPrefab.transform.localScale.y));
    }

    float GetSlabHeight()
    {
        if (slabPrefab == null) return 0.5f;
        return Mathf.Max(0.1f, Mathf.Abs(slabPrefab.transform.localScale.y));
    }
}
