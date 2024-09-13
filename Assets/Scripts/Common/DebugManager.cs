using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;

    [SerializeField] private GameObject debugViewerContent;
    [SerializeField] private RectTransform debugViewerScrollView;
    [SerializeField] private Scrollbar debugViewerScrollbar;
    [SerializeField] private GameObject debugTextPrefab;
    [SerializeField] private Button debugToggleButton;

    Queue<GameObject> debugTextQueue = new Queue<GameObject>();

    void Start()
    {
        Instance = this;

        debugToggleButton.onClick.AddListener(() => { HideDebugConsole(); });                                               
    }

    public void AddDebugText(string text)
    {
        GameObject newText = Instantiate(debugTextPrefab);
        newText.transform.parent = debugViewerContent.transform;

        TMP_Text newTMP = newText.GetComponent<TMP_Text>();
        newTMP.text = "- " + text;
        newText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, newTMP.preferredHeight);

        debugTextQueue.Enqueue(newText);

        while (debugTextQueue.Count > 20)
        {
            GameObject temp = debugTextQueue.Dequeue();
            Destroy(temp);
        }

        debugViewerScrollbar.value = 0f;
    }

    public void ShowDebugConsole()
    {
        debugViewerScrollView.sizeDelta = new Vector2(400, 200);

        debugToggleButton.gameObject.GetComponentInChildren<TMP_Text>().text = "¡å";
        debugToggleButton.onClick.RemoveAllListeners();
        debugToggleButton.onClick.AddListener(() => { HideDebugConsole(); });
    }

    public void HideDebugConsole()
    {
        debugViewerScrollView.sizeDelta = new Vector2(400, 0);

        debugToggleButton.gameObject.GetComponentInChildren<TMP_Text>().text = "¡ã";
        debugToggleButton.onClick.RemoveAllListeners();
        debugToggleButton.onClick.AddListener(() => { ShowDebugConsole(); });
    }
}
