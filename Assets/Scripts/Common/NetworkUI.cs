using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public static NetworkUI Instance;

    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;
    [SerializeField] TMP_Text yourColorText;
    [SerializeField] TMP_Text WarningText;

    [SerializeField] Image[] lightCircles;

    void Start()
    {
        Instance = this;

        hostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        clientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }

    public void UpdateYourColorText(PlayerColor newColor)
    {
        if (newColor == PlayerColor.Red) yourColorText.text = "Your Color: <color=\"red\">Red</color>";
        else yourColorText.text = "Your Color: <color=\"blue\">Blue</color>";
    }

    public void UpdateLightCircles()
    {
        foreach(var circle in lightCircles)
        {
            circle.color = new Color(.3f, .3f, .3f);
        }

        if (MultiplayerManager.Instance.currentRule == Rule.Blue)
        {
            lightCircles[0].color = new Color(0, 0, 1);
        }
        else if (MultiplayerManager.Instance.currentRule == Rule.Yellow)
        {
            lightCircles[1].color = new Color(1, 1, 0);
        }
        else
        {
            lightCircles[2].color = new Color(1, 0, 0);
        }
    }

    public void ShowWarning()
    {
        WarningText.enabled = true;
    }

    public void HideWarning()
    {
        WarningText.enabled = false;
    }
}
