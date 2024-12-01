using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkSyncInterpolator : MonoBehaviour
{
    private float _lerpSpeed = 20f;

    public GameObject Target
    {
        get => _target;
        set => _target = value;
    }
    private GameObject _target;

    void Update()
    {
        if (_target)
        {
            if (gameObject.name == "Elevator")
            {
                Debug.Log($"{_target.transform.position.y}");
            }
            float posDif = Vector3.Distance(transform.position, _target.transform.position);
            float rotDif = 1f - Quaternion.Dot(transform.rotation, _target.transform.rotation);

            if (posDif > 0.00001f || rotDif > 0.00001f)
            {
                transform.position = Vector3.Lerp(transform.position, _target.transform.position, Time.deltaTime * _lerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, _target.transform.rotation, Time.deltaTime * _lerpSpeed);
            }
        }
    }
}
