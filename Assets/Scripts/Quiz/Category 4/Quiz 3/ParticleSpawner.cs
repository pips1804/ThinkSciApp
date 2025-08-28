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

    void Start()
    {
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
