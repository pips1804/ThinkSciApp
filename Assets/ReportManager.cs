using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class ReportManager : MonoBehaviour
{
    [Header("Score Scroll View")]
    public Transform scoreContentParent;
    public GameObject scoreRowPrefab;
    public Text quizTitleText;

    [Header("Sort Buttons")]
    public Button scoreSortButton;
    public Button dateSortButton;

    private DatabaseManager dbManager;
    private List<QuizScoreRecord> currentRecords = new List<QuizScoreRecord>();

    // Toggle flags
    private bool scoreAscending = true;
    private bool dateAscending = true;

    void Start()
    {
        dbManager = FindObjectOfType<DatabaseManager>();

        // Setup button listeners
        scoreSortButton.onClick.AddListener(ToggleScoreSort);
        dateSortButton.onClick.AddListener(ToggleDateSort);

        // Initial button labels
        scoreSortButton.GetComponentInChildren<Text>().text = "Low → High";
        dateSortButton.GetComponentInChildren<Text>().text = "Old → New";
    }

    public void ShowQuizScores(int quizId, string quizName)
    {
        ClearPanel(scoreContentParent);

        if (quizTitleText != null)
            quizTitleText.text = quizName;

        currentRecords = dbManager.GetScoresByQuiz(quizId);

        // Default display (unsorted initially)
        DisplayRecords(currentRecords);
    }

    private void DisplayRecords(List<QuizScoreRecord> records)
    {
        ClearPanel(scoreContentParent);

        foreach (QuizScoreRecord record in records)
        {
            GameObject newRow = Instantiate(scoreRowPrefab, scoreContentParent);

            // Parse DB time safely
            DateTime completedAtUtc;
            if (!DateTime.TryParse(record.CompletedAt, out completedAtUtc))
            {
                completedAtUtc = DateTime.UtcNow; // fallback if parsing fails
            }
            else
            {
                // Make sure it's treated as UTC
                completedAtUtc = DateTime.SpecifyKind(completedAtUtc, DateTimeKind.Utc);
            }

            // Convert UTC → Philippine Time (UTC+8) manually
            DateTime completedAt = completedAtUtc.AddHours(8);

            // Format date & time
            string dateOnly = completedAt.ToString("MMMM dd, yyyy");
            string timeOnly = completedAt.ToString("hh:mm tt");

            // Find text fields in prefab
            Text scoreText = newRow.transform.Find("ScoreText").GetComponent<Text>();
            Text dateText = newRow.transform.Find("DateText").GetComponent<Text>();
            Text timeText = newRow.transform.Find("TimeText").GetComponent<Text>();

            scoreText.text = $"Score: {record.Score}";
            dateText.text = dateOnly;
            timeText.text = timeOnly;

            // Show badge if new
            Transform newBadge = newRow.transform.Find("NewBadge");
            if (newBadge != null)
            {
                newBadge.gameObject.SetActive(record.IsNew);
            }

            // After showing, mark as seen
            if (record.IsNew)
            {
                record.IsNew = false;
                dbManager.MarkScoreAsSeen(record);
            }
        }
    }


    private void ClearPanel(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }

    // Toggle Score Sort
    private void ToggleScoreSort()
    {
        if (scoreAscending)
        {
            currentRecords = currentRecords.OrderBy(r => r.Score).ToList();
            scoreSortButton.GetComponentInChildren<Text>().text = "High → Low";
        }
        else
        {
            currentRecords = currentRecords.OrderByDescending(r => r.Score).ToList();
            scoreSortButton.GetComponentInChildren<Text>().text = "Low → High";
        }

        scoreAscending = !scoreAscending;
        DisplayRecords(currentRecords);
    }

    // Toggle Date Sort
    private void ToggleDateSort()
    {
        if (dateAscending)
        {
            currentRecords = currentRecords.OrderBy(r => System.DateTime.Parse(r.CompletedAt)).ToList();
            dateSortButton.GetComponentInChildren<Text>().text = "New → Old";
        }
        else
        {
            currentRecords = currentRecords.OrderByDescending(r => System.DateTime.Parse(r.CompletedAt)).ToList();
            dateSortButton.GetComponentInChildren<Text>().text = "Old → New";
        }

        dateAscending = !dateAscending;
        DisplayRecords(currentRecords);
    }

    public void ShowQuizOne() => ShowQuizScores(1, "What Are Forces?");
    public void ShowQuizTwo() => ShowQuizScores(2, "Balanced vs. Unbalanced Forces");
    public void ShowQuizThree() => ShowQuizScores(3, "Free-Body Diagrams");
    public void ShowQuizFour() => ShowQuizScores(4, "Effects of Unbalanced Forces");
    public void ShowQuizFive() => ShowQuizScores(5, "Distance vs. Displacement");
    public void ShowQuizSix() => ShowQuizScores(6, "Speed vs. Velocity");
    public void ShowQuizSeven() => ShowQuizScores(7, "Uniform Velocity and Distance-Time");
    public void ShowQuizEight() => ShowQuizScores(8, "Heat vs. Temperature");
    public void ShowQuizNine() => ShowQuizScores(9, "Modes of Heat Transfer");
    public void ShowQuizTen() => ShowQuizScores(10, "Heat Transfer and the Particle Model");
    public void ShowQuizEleven() => ShowQuizScores(11, "Modern Renewable Energy Sources");
    public void ShowQuizTwelve() => ShowQuizScores(12, "Technological Devices Transforming Heat Energy");
    public void ShowQuizThirteen() => ShowQuizScores(13, "Particle Model in Energy Innovations");
    public void ShowQuizFourteen() => ShowQuizScores(14, "Local and Global Solutions to the Energy Crisis");
}
