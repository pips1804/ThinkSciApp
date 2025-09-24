using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizNotificationManager : MonoBehaviour
{
    [Header("Category Buttons")]
    public Button category1Button; // Quizzes 1-4
    public Button category2Button; // Quizzes 5-7
    public Button category3Button; // Quizzes 8-10
    public Button category4Button; // Quizzes 11-14

    [Header("Quiz Buttons (Drag in order: Quiz1 → Quiz14)")]
    public List<Button> quizButtons;

    [Header("Database")]
    public DatabaseManager db;

    private int currentUserId = 1;

    void OnEnable()
    {
        UpdateAllNotifications();
    }

    public void UpdateAllNotifications()
    {
        // ----- QUIZ BUTTONS -----
        for (int i = 0; i < quizButtons.Count; i++)
        {
            int quizId = i + 1;
            bool hasNew = HasNewScore(quizId);
            ShowRedDot(quizButtons[i], hasNew);
        }

        // ----- CATEGORY BUTTONS -----
        ShowRedDot(category1Button, HasAnyNewScoreInRange(1, 4));
        ShowRedDot(category2Button, HasAnyNewScoreInRange(5, 7));
        ShowRedDot(category3Button, HasAnyNewScoreInRange(8, 10));
        ShowRedDot(category4Button, HasAnyNewScoreInRange(11, 14));
    }

    private void ShowRedDot(Button btn, bool show)
    {
        if (btn == null) return;
        Transform redDot = btn.transform.Find("RedDot");
        if (redDot != null) redDot.gameObject.SetActive(show);
    }

    private bool HasNewScore(int quizId)
    {
        List<QuizScoreRecord> scores = db.GetScoresByQuiz(quizId, currentUserId);
        foreach (var s in scores)
        {
            if (s.IsNew) return true;
        }
        return false;
    }

    private bool HasAnyNewScoreInRange(int startQuizId, int endQuizId)
    {
        for (int q = startQuizId; q <= endQuizId; q++)
        {
            if (HasNewScore(q)) return true;
        }
        return false;
    }
}
