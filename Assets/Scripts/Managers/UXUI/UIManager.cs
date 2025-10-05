using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI XpText;
    

    void Start()
    {
        timeText.text = TimeManager.Instance.GetCurrentTime().ToString();
        XpText.text = 0.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        timeUiUpdate();
    }
    void timeUiUpdate()
    {
        float time = TimeManager.Instance.GetCurrentTime();
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        // Format as military time MM:SS
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    void XpUiUpdate(int xp)
    {
        XpText.text =xp.ToString();
    }
}
