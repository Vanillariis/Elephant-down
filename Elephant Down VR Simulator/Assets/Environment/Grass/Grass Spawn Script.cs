using UnityEngine;

public class GrassSpawnScript : MonoBehaviour
{
    [Header("References")]
    public GameObject grassPrefab;
    public Transform player;

    [Header("Patch Distribution")]
    public int patchCount = 25;
    public float spawnRadiusAroundPlayer = 15f;

    [Header("Patch Variation")]
    public Vector2Int grassPerPatchRange = new Vector2Int(4, 10);
    public Vector2 patchRadiusRange = new Vector2(0.5f, 1.5f);

    [Header("Grass Variation")]
    public Vector2 randomScaleRange = new Vector2(0.8f, 1.2f);

    [Header("Grounding")]
    public bool useRaycastToGround = true;
    public float raycastStartHeight = 20f;
    public float raycastDistance = 50f;
    public LayerMask groundLayer;

    void Start()
    {
        SpawnGrassPatches();
    }

    void SpawnGrassPatches()
    {
        if (grassPrefab == null || player == null)
        {
            Debug.LogWarning("Assign both grassPrefab and player in the Inspector.");
            return;
        }

        for (int p = 0; p < patchCount; p++)
        {
            // Choose a random patch center around the player
            Vector2 patchOffset = Random.insideUnitCircle * spawnRadiusAroundPlayer;
            Vector3 patchCenter = new Vector3(
                player.position.x + patchOffset.x,
                player.position.y,
                player.position.z + patchOffset.y
            );

            // Randomize this patch
            int grassCount = Random.Range(grassPerPatchRange.x, grassPerPatchRange.y + 1);
            float patchRadius = Random.Range(patchRadiusRange.x, patchRadiusRange.y);

            for (int i = 0; i < grassCount; i++)
            {
                // Random position inside this patch
                Vector2 localOffset = Random.insideUnitCircle * patchRadius;
                Vector3 spawnPos = new Vector3(
                    patchCenter.x + localOffset.x,
                    patchCenter.y,
                    patchCenter.z + localOffset.y
                );

                // Snap to ground if enabled
                if (useRaycastToGround)
                {
                    Ray ray = new Ray(
                        new Vector3(spawnPos.x, player.position.y + raycastStartHeight, spawnPos.z),
                        Vector3.down
                    );

                    if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
                    {
                        spawnPos.y = hit.point.y;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Only rotate around Y so the grass stays upright
                Quaternion rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);

                GameObject grass = Instantiate(grassPrefab, spawnPos, rotation, transform);

                float scale = Random.Range(randomScaleRange.x, randomScaleRange.y);
                grass.transform.localScale = Vector3.one * scale;
            }
        }
    }
}