using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    public GameObject particlePrefab;
    public float spawnInterval = 0.5f;

    [Header("Spawn Point")]
    public Transform spawnPoint; // Drag an empty GameObject here

    [Header("Dialogue")]
    public Dialogues dialogues; // Drag this in the Inspector

    [Header("Heat Escape Reference")]
    public HeatEscape heatEscape; // Drag your HeatEscape GameObject here

    private bool spawning = true;
    private bool hasStarted = false;

    void Start()
    {
        StartSpawningProcess();
    }

    void OnEnable()
    {
        // If this isn't the first time (game has been restarted)
        if (hasStarted)
        {
            StartSpawningProcess();
        }
    }

    public void StartSpawningProcess()
    {
        // Stop any existing spawning first
        StopSpawning();

        // Reset spawning state
        spawning = true;
        hasStarted = true;

        // Start the spawning coroutine
        StartCoroutine(WaitForDialogueAndSpawn());
    }

    private System.Collections.IEnumerator WaitForDialogueAndSpawn()
    {
        // Wait until dialogue is finished
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        // Start spawning particles
        InvokeRepeating(nameof(SpawnParticle), 0f, spawnInterval);

        // Wait until HeatEscape quiz starts
        yield return new WaitUntil(() => heatEscape.quizPanel.activeSelf);

        // Stop spawning once quiz begins
        StopSpawning();
    }

    public void StopSpawning()
    {
        // Cancel any existing invoke
        CancelInvoke(nameof(SpawnParticle));
        spawning = false;

        // Destroy all existing particles
        DestroyAllParticles();
    }

    void SpawnParticle()
    {
        if (!spawning) return;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject particle = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);

        if (spawnPoint != null)
            particle.transform.SetParent(spawnPoint);
    }

    void DestroyAllParticles()
    {
        if (spawnPoint == null) return;

        foreach (Transform child in spawnPoint)
        {
            Destroy(child.gameObject);
        }
    }
}
