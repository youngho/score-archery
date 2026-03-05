using UnityEngine;

public class RubberDuckBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float driftSpeed = 1.0f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2.0f;
    public float destroyZ = 10.0f;

    [Header("Natural Float Settings")]
    public float tiltAmplitude = 5.0f;    // Max tilt in degrees
    public float tiltSpeed = 1.5f;
    public float swayAmplitude = 0.2f;    // Max horizontal sway
    public float swaySpeed = 1.0f;
    public float yawAmplitude = 10.0f;   // Max rotation oscillation

    private float _randomOffset;
    private float _startY;
    private float _startX;
    private Quaternion _initialRotation;

    void Start()
    {
        _randomOffset = Random.Range(0f, 2f * Mathf.PI);
        _startY = transform.position.y;
        _startX = transform.position.x;
        _initialRotation = transform.rotation;
    }

    void Update()
    {
        float time = Time.time + _randomOffset;

        // 1. Drift movement (along Z-axis)
        // We use a local offset for sway to keep it relative to the starting X
        float sway = Mathf.Sin(time * swaySpeed) * swayAmplitude;
        Vector3 currentPos = transform.position;
        currentPos.z += driftSpeed * Time.deltaTime;
        currentPos.x = _startX + sway;

        // 2. Bobbing movement (Vertical Float)
        currentPos.y = _startY + Mathf.Sin(time * floatSpeed) * floatAmplitude;
        transform.position = currentPos;

        // 3. Natural Tilting (Pitch, Roll, Yaw)
        float pitch = Mathf.Sin(time * tiltSpeed * 0.8f) * tiltAmplitude;
        float roll = Mathf.Cos(time * tiltSpeed * 1.1f) * tiltAmplitude;
        float yaw = Mathf.Sin(time * tiltSpeed * 0.5f) * yawAmplitude;

        transform.rotation = _initialRotation * Quaternion.Euler(pitch, yaw, roll);

        // 4. Auto-destruction
        if (transform.position.z > destroyZ)
        {
            Destroy(gameObject);
        }
    }
}
