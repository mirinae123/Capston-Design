using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 멀티플레이어 테스트 UI를 지원하는 싱글톤 클래스.
/// </summary>
public class NetworkUI : MonoBehaviour
{
    public static NetworkUI Instance;

    [SerializeField] Button _hostButton;
    [SerializeField] Button _clientButton;
    [SerializeField] TMP_Text _yourColorText;
    [SerializeField] TMP_InputField _ipInputField;

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        _ipInputField.text = "127.0.0.1";

        _hostButton.onClick.AddListener(() =>
        {
            UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            transport.ConnectionData.Address = _ipInputField.text;
            NetworkManager.Singleton.StartHost();

            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(false);
            _ipInputField.gameObject.SetActive(false);
        }
        );
        _clientButton.onClick.AddListener(() =>
        {
            UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            transport.ConnectionData.Address = _ipInputField.text;
            NetworkManager.Singleton.StartClient();

            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(false);
            _ipInputField.gameObject.SetActive(false);
        }
        );
    }

    public void UpdateYourColorText(ColorType newColor)
    {
        if (newColor == ColorType.Red) _yourColorText.text = "Your Color: <color=\"red\">Red</color>";
        else _yourColorText.text = "Your Color: <color=\"blue\">Blue</color>";
    }
}
