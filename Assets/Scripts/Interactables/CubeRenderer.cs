using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CubeRenderer : NetworkBehaviour
{
    [SerializeField] private GameObject _timeLeftCanvas;
    [SerializeField] private Image _timeLeftImage;

    private CubeController _cubeController;
    private MeshRenderer _meshRenderer;

    public override void OnNetworkSpawn()
    {
        // 큐브의 색깔이 변하면 함수 호출하도록 지정
        _cubeController.CubeColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnCubeColorChanged(before, after);
        };

        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _cubeController.CubeColor.OnValueChanged.Invoke(_cubeController.CubeColor.Value, _cubeController.CubeColor.Value);
            });
        }
        else
        {
            _cubeController.CubeColor.OnValueChanged.Invoke(_cubeController.CubeColor.Value, _cubeController.CubeColor.Value);
        }
    }

    private void Awake()
    {
        _cubeController = GetComponent<CubeController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (_cubeController.ColorChangeTimeLeft > 0f && _cubeController.ColorChangeDuration != _cubeController.ColorChangeTimeLeft && _cubeController.CubeColor.Value != _cubeController.InitColor)             
        {
            if (!_timeLeftCanvas.activeSelf)
            {
                _timeLeftCanvas.SetActive(true);
            }

            if (MultiplayerManager.Instance.LocalPlayer != null)
            {
                _timeLeftCanvas.gameObject.transform.LookAt(MultiplayerManager.Instance.LocalPlayer.transform);
            }

            _timeLeftImage.fillAmount = _cubeController.ColorChangeTimeLeft / _cubeController.ColorChangeDuration;
        }
        else
        {
            if (_timeLeftCanvas.activeSelf)
            {
                _timeLeftCanvas.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 상자의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnCubeColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);

        _meshRenderer.material.color = newColor;
    }
}
