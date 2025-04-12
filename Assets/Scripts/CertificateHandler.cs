using UnityEngine;
using UnityEngine.UI;

public class CertificateLocker : MonoBehaviour
{
    public Button categoryOneCertficate;
    public Button categoryTwoCertficate;
    public Button categoryThreeCertficate;
    public Button categoryFourCertficate;

    void Start()
    {
        // Initially lock certificate
        LockCertificate(categoryOneCertficate);
        LockCertificate(categoryTwoCertficate);
        LockCertificate(categoryThreeCertficate);
        LockCertificate(categoryFourCertficate);
    }

    // Lock a certificate
    public void LockCertificate(Button button)
    {
        button.interactable = false;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Optional: Show lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(true);
    }

    //  Unlock a certificate
    public void UnlockCertificate(Button button)
    {
        button.interactable = true;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Optional: Hide lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(false);
    }

    /* 
    * Unlocking a Lesson or Category
    * public CategoryLocker categoryLocker;

       void CheckPlayerProgress() {
           if (playerLevel >= 5) {
               categoryLocker.UnlockCategory(categoryLocker.categoryTwoButton);
           }
       }
    */
}
