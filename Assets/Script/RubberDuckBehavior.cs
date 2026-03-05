using UnityEngine;

public class RubberDuckBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float driftSpeed = 1.0f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2.0f;
    public float destroyZ = 10.0f;

    [Header("Natural Float Settings")]
    public float tiltAmplitude = 5.0f;
    public float tiltSpeed = 1.5f;
    public float swayAmplitude = 0.2f;
    public float swaySpeed = 1.0f;
    public float yawAmplitude = 10.0f;

    [Header("Avoidance Settings")]
    public float obstacleDetectionDist = 1.5f;
    public float turnSpeed = 2.0f;
    public LayerMask terrainLayer = -1;

    [Header("Collision Settings")]
    public float bounceForce = 1.5f;
    public float bounceCooldown = 1.0f;

    private float _randomOffset;
    private float _startY;
    private float _startX;
    private Quaternion _initialRotation;
    private Vector3 _currentDriftDirection = Vector3.forward;
    private float _currentBounceCooldown = 0f;

    void Start()
    {
        _randomOffset = Random.Range(0f, 2f * Mathf.PI);
        _startY = transform.position.y;
        _startX = transform.position.x;
        _initialRotation = transform.rotation;
    }

    void Update()
    {
        if (_currentBounceCooldown > 0)
        {
            _currentBounceCooldown -= Time.deltaTime;
        }
        else
        {
            CheckForObstacles();
        }

        float time = Time.time + _randomOffset;

        // 1. Drift movement (Dynamic direction)
        Vector3 movement = _currentDriftDirection * driftSpeed * (_currentBounceCooldown > 0 ? bounceForce : 1.0f) * Time.deltaTime;
        
        // 2. Horizontal Sway
        float sway = Mathf.Sin(time * swaySpeed) * swayAmplitude;
        Vector3 swayOffset = Vector3.Cross(_currentDriftDirection, Vector3.up).normalized * sway;

        transform.position += movement + (swayOffset * Time.deltaTime * swaySpeed);

        // 3. Bobbing movement (Vertical Float)
        Vector3 pos = transform.position;
        pos.y = _startY + Mathf.Sin(time * floatSpeed) * floatAmplitude;
        transform.position = pos;

        // 4. Natural Tilting
        float pitch = Mathf.Sin(time * tiltSpeed * 0.8f) * tiltAmplitude;
        float roll = Mathf.Cos(time * tiltSpeed * 1.1f) * tiltAmplitude;
        float yawOsc = Mathf.Sin(time * tiltSpeed * 0.5f) * yawAmplitude;

        Quaternion driftRot = Quaternion.LookRotation(_currentDriftDirection);
        transform.rotation = driftRot * Quaternion.Euler(pitch, yawOsc, roll);

        // 5. Auto-destruction
        if (transform.position.z > destroyZ)
        {
            Destroy(gameObject);
        }
    }

    void CheckForObstacles()
    {
        Vector3 forward = _currentDriftDirection;
        bool hitCenter = RaycastCheck(forward);
        bool hitLeft = RaycastCheck(Quaternion.Euler(0, -30, 0) * forward);
        bool hitRight = RaycastCheck(Quaternion.Euler(0, 30, 0) * forward);

        if (hitCenter || hitLeft || hitRight)
        {
            float steering = 0;
            if (hitLeft && !hitRight) steering = 1;
            else if (hitRight && !hitLeft) steering = -1;
            else if (hitCenter) steering = hitLeft ? 1 : -1;

            if (steering != 0)
            {
                _currentDriftDirection = Quaternion.Euler(0, steering * turnSpeed * Time.deltaTime * 50f, 0) * _currentDriftDirection;
            }
        }
        else
        {
            _currentDriftDirection = Vector3.Slerp(_currentDriftDirection, Vector3.forward, Time.deltaTime * 0.5f);
        }
    }

    bool RaycastCheck(Vector3 direction)
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, direction, out hit, obstacleDetectionDist, terrainLayer))
        {
            if (hit.collider.gameObject.name.Contains("Terrain") || hit.collider.CompareTag("Terrain"))
            {
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Bounce off the boat
        if (collision.gameObject.name.Contains("WoodBoat") || collision.gameObject.CompareTag("WoodBoat"))
        {
            Vector3 normal = collision.contacts[0].normal;
            // Ensure normal is on the horizontal plane
            normal.y = 0;
            normal.Normalize();

            // Reflect the drift direction
            _currentDriftDirection = Vector3.Reflect(_currentDriftDirection, normal);
            _currentDriftDirection.y = 0;
            _currentDriftDirection.Normalize();

            _currentBounceCooldown = bounceCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawRay(rayStart, _currentDriftDirection * obstacleDetectionDist);
        Gizmos.DrawRay(rayStart, (Quaternion.Euler(0, -30, 0) * _currentDriftDirection) * obstacleDetectionDist);
        Gizmos.DrawRay(rayStart, (Quaternion.Euler(0, 30, 0) * _currentDriftDirection) * obstacleDetectionDist);
    }
}
