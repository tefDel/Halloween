using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SetTerrainObstacles : MonoBehaviour
{
    [Header("Selección de Terrains")]
    [Tooltip("Arrastra aquí TODOS los terrains que quieras procesar")]
    public Terrain[] specificTerrains;

    [Tooltip("Tag para buscar terrains automáticamente (déjalo vacío para no usar)")]
    public string terrainTag = "";

    [Tooltip("Layer para buscar terrains (selecciona 'Nothing' para no usar)")]
    public LayerMask terrainLayer = 0;

    [Space(10)]
    [Tooltip("Si está marcado, procesará TODOS los terrains de la escena (ignora las otras opciones)")]
    public bool processAllTerrainsInScene = false;

    [Header("Configuración de Obstáculos")]
    [Tooltip("Radio adicional para dar más espacio alrededor de los árboles")]
    public float radiusOffset = 0.5f;

    [Tooltip("Si es true, usa carving dinámico. Si es false, debes rebakear el NavMesh")]
    public bool useDynamicCarving = true;

    private int totalObstaclesCreated = 0;

    void Start()
    {
        CreateTreeObstacles();
    }

    public void CreateTreeObstacles()
    {
        List<Terrain> terrainsToProcess = new List<Terrain>();

        // Opción 1: Procesar TODOS los terrains de la escena
        if (processAllTerrainsInScene)
        {
            Terrain[] allTerrains = FindObjectsOfType<Terrain>();
            terrainsToProcess.AddRange(allTerrains);
            Debug.Log($"<color=cyan>Modo: Procesando TODOS los terrains de la escena ({allTerrains.Length} encontrados)</color>");
        }
        else
        {
            // Opción 2: Terrains asignados manualmente en el Inspector
            if (specificTerrains != null && specificTerrains.Length > 0)
            {
                foreach (Terrain t in specificTerrains)
                {
                    if (t != null && !terrainsToProcess.Contains(t))
                    {
                        terrainsToProcess.Add(t);
                    }
                }
                Debug.Log($"<color=cyan>Agregados {terrainsToProcess.Count} terrain(s) desde el Inspector</color>");
            }

            // Opción 3: Buscar por TAG
            if (!string.IsNullOrEmpty(terrainTag))
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(terrainTag);
                int tagCount = 0;
                foreach (GameObject obj in taggedObjects)
                {
                    Terrain t = obj.GetComponent<Terrain>();
                    if (t != null && !terrainsToProcess.Contains(t))
                    {
                        terrainsToProcess.Add(t);
                        tagCount++;
                    }
                }
                Debug.Log($"<color=cyan>Agregados {tagCount} terrain(s) con tag '{terrainTag}'</color>");
            }

            // Opción 4: Buscar por LAYER
            if (terrainLayer != 0)
            {
                Terrain[] allTerrains = FindObjectsOfType<Terrain>();
                int layerCount = 0;
                foreach (Terrain t in allTerrains)
                {
                    if (((1 << t.gameObject.layer) & terrainLayer) != 0 && !terrainsToProcess.Contains(t))
                    {
                        terrainsToProcess.Add(t);
                        layerCount++;
                    }
                }
                Debug.Log($"<color=cyan>Agregados {layerCount} terrain(s) desde el layer seleccionado</color>");
            }
        }

        // Validar que hay terrains para procesar
        if (terrainsToProcess.Count == 0)
        {
            Debug.LogError("ERROR: No se encontraron terrains para procesar. Asegúrate de:");
            Debug.LogError("- Marcar 'Process All Terrains In Scene', o");
            Debug.LogError("- Asignar terrains en 'Specific Terrains', o");
            Debug.LogError("- Configurar un 'Terrain Tag', o");
            Debug.LogError("- Seleccionar un 'Terrain Layer'");
            return;
        }

        Debug.Log($"<color=yellow>══════════════════════════════════════════════════</color>");
        Debug.Log($"<color=yellow>INICIANDO PROCESAMIENTO DE {terrainsToProcess.Count} TERRAIN(S)</color>");
        Debug.Log($"<color=yellow>══════════════════════════════════════════════════</color>");

        // Procesar cada terrain encontrado
        totalObstaclesCreated = 0;
        int processedCount = 0;

        foreach (Terrain terrain in terrainsToProcess)
        {
            processedCount++;
            Debug.Log($"\n<color=cyan>[{processedCount}/{terrainsToProcess.Count}] Procesando: '{terrain.name}'</color>");
            int count = ProcessTerrain(terrain);
            totalObstaclesCreated += count;
        }

        // Resumen final
        Debug.Log($"\n<color=yellow>══════════════════════════════════════════════════</color>");
        Debug.Log($"<color=green>✓✓✓ COMPLETADO ✓✓✓</color>");
        Debug.Log($"<color=green>Total de terrains procesados: {terrainsToProcess.Count}</color>");
        Debug.Log($"<color=green>Total de obstáculos creados: {totalObstaclesCreated}</color>");
        Debug.Log($"<color=yellow>══════════════════════════════════════════════════</color>");

        if (useDynamicCarving)
        {
            Debug.Log("<color=cyan>Modo: Carving Dinámico activado. Los enemigos evitarán los árboles automáticamente.</color>");
        }
        else
        {
            Debug.Log("<color=yellow>Modo: Estático. DEBES REBAKEAR EL NAVMESH ahora (Window > AI > Navigation > Bake).</color>");
        }
    }

    private int ProcessTerrain(Terrain terrain)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("  └─ Terrain o TerrainData es null, saltando...");
            return 0;
        }

        TreeInstance[] obstacles = terrain.terrainData.treeInstances;

        if (obstacles.Length == 0)
        {
            Debug.Log($"  └─ Sin árboles, saltando...");
            return 0;
        }

        float length = terrain.terrainData.size.z;
        float width = terrain.terrainData.size.x;
        float height = terrain.terrainData.size.y;

        Debug.Log($"  ├─ Tamaño: {width} x {height} x {length}");
        Debug.Log($"  ├─ Árboles detectados: {obstacles.Length}");

        // Buscar o crear el padre para los obstáculos de este terrain
        string parentName = $"Tree_Obstacles_{terrain.name}";
        GameObject parent = GameObject.Find(parentName);

        if (parent != null)
        {
            Debug.Log($"  ├─ Limpiando obstáculos anteriores...");
            DestroyImmediate(parent);
        }

        parent = new GameObject(parentName);
        parent.transform.position = terrain.GetPosition();

        int successCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < obstacles.Length; i++)
        {
            TreeInstance tree = obstacles[i];

            // Verificar que el prototipo existe
            if (tree.prototypeIndex >= terrain.terrainData.treePrototypes.Length)
            {
                skippedCount++;
                continue;
            }

            TreePrototype prototype = terrain.terrainData.treePrototypes[tree.prototypeIndex];

            if (prototype.prefab == null)
            {
                skippedCount++;
                continue;
            }

            // Calcular posición mundial del árbol
            Vector3 worldPos = new Vector3(
                tree.position.x * width,
                tree.position.y * height,
                tree.position.z * length
            ) + terrain.GetPosition();

            Quaternion worldRot = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

            // Crear GameObject para el obstáculo
            GameObject obs = new GameObject($"TreeObstacle_{i}");
            obs.transform.SetParent(parent.transform);
            obs.transform.position = worldPos;
            obs.transform.rotation = worldRot;
            obs.transform.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

            // Obtener el collider del prefab
            Collider prefabCollider = prototype.prefab.GetComponent<Collider>();

            NavMeshObstacle obsElement = obs.AddComponent<NavMeshObstacle>();
            ConfigureObstacle(obsElement, useDynamicCarving);

            if (prefabCollider == null)
            {
                // Si no hay collider, crear uno por defecto
                obsElement.shape = NavMeshObstacleShape.Capsule;
                obsElement.radius = 0.5f * tree.widthScale + radiusOffset;
                obsElement.height = 5f * tree.heightScale;
                obsElement.center = new Vector3(0, obsElement.height / 2, 0);

                successCount++;
                continue;
            }

            // Configurar según el tipo de collider
            if (prefabCollider is CapsuleCollider capsule)
            {
                obsElement.shape = NavMeshObstacleShape.Capsule;
                obsElement.center = capsule.center;
                obsElement.radius = capsule.radius * tree.widthScale + radiusOffset;
                obsElement.height = capsule.height * tree.heightScale;
            }
            else if (prefabCollider is BoxCollider box)
            {
                obsElement.shape = NavMeshObstacleShape.Box;
                obsElement.center = box.center;
                obsElement.size = new Vector3(
                    box.size.x * tree.widthScale + radiusOffset * 2,
                    box.size.y * tree.heightScale,
                    box.size.z * tree.widthScale + radiusOffset * 2
                );
            }
            else if (prefabCollider is SphereCollider sphere)
            {
                obsElement.shape = NavMeshObstacleShape.Capsule;
                obsElement.center = sphere.center;
                obsElement.radius = sphere.radius * tree.widthScale + radiusOffset;
                obsElement.height = sphere.radius * 2 * tree.heightScale;
            }
            else
            {
                // Collider no soportado, usar valores por defecto
                obsElement.shape = NavMeshObstacleShape.Capsule;
                obsElement.radius = 0.5f * tree.widthScale + radiusOffset;
                obsElement.height = 5f * tree.heightScale;
                obsElement.center = new Vector3(0, obsElement.height / 2, 0);
            }

            successCount++;
        }

        string statusIcon = successCount == obstacles.Length ? "✓" : "⚠";
        Debug.Log($"  └─ {statusIcon} Resultado: {successCount} obstáculos creados" +
                  (skippedCount > 0 ? $" ({skippedCount} saltados)" : ""));

        return successCount;
    }

    private void ConfigureObstacle(NavMeshObstacle obstacle, bool enableCarving)
    {
        obstacle.carving = enableCarving;
        obstacle.carveOnlyStationary = true;

        if (enableCarving)
        {
            obstacle.carvingMoveThreshold = 0.1f;
            obstacle.carvingTimeToStationary = 0.5f;
        }
    }

    [ContextMenu("Limpiar Todos los Obstáculos")]
    public void ClearAllObstacles()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Tree_Obstacles_"))
            {
                DestroyImmediate(obj);
                count++;
            }
        }

        Debug.Log($"<color=yellow>Se limpiaron {count} grupos de obstáculos.</color>");
    }

    [ContextMenu("Recrear Obstáculos")]
    public void RecreateObstacles()
    {
        ClearAllObstacles();
        CreateTreeObstacles();
    }

    [ContextMenu("Mostrar Información de Terrains")]
    public void ShowTerrainInfo()
    {
        Terrain[] allTerrains = FindObjectsOfType<Terrain>();
        Debug.Log($"<color=cyan>═══════ INFORMACIÓN DE TERRAINS ═══════</color>");
        Debug.Log($"Total de terrains en la escena: {allTerrains.Length}");

        foreach (Terrain t in allTerrains)
        {
            int treeCount = t.terrainData != null ? t.terrainData.treeInstances.Length : 0;
            Debug.Log($"  • '{t.name}' - Layer: {LayerMask.LayerToName(t.gameObject.layer)} - Tag: {t.tag} - Árboles: {treeCount}");
        }
        Debug.Log($"<color=cyan>═══════════════════════════════════════</color>");
    }
}