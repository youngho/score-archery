using UnityEngine;

public class RubberDuckBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float driftSpeed = 1.0f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2.0f;
    public float destroyZ = 10.0f;

    private float _randomOffset;
    private float _startY;

    void Start()
    {
        _randomOffset = Random.Range(0f, 2f * Mathf.PI);
        _startY = transform.position.y;
    }

    void Update()
    {
        // Drift movement (along Z-axis)
        transform.Translate(Vector3.forward * driftSpeed * Time.deltaTime, Space.World);

        // Bobbing movement (Float)
        float newY = _startY + Mathf.Sin(Time.time * floatSpeed + _randomOffset) * floatAmplitude;
        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;

        // Auto-destruction
        if (transform.position.z > destroyZ)
        {
            Destroy(gameObject);
        }
    }
}
