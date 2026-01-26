using UnityEngine;

public class BalloonBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float upwardSpeed = 2.0f;
    public float horizontalVelocity = 0.0f;
    public float horizontalDamping = 0.5f;
    public float destroyHeight = 10.0f;
    public float driftAmount = 0.5f;
    public float driftSpeed = 1.0f;

    private float _randomDriftOffset;

    void Start()
    {
        _randomDriftOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Upward movement
        Vector3 movement = Vector3.up * upwardSpeed * Time.deltaTime;

        // Diagonal movement (inward launch)
        movement.x += horizontalVelocity * Time.deltaTime;
        
        // Dampen horizontal velocity over time
        horizontalVelocity = Mathf.Lerp(horizontalVelocity, 0f, horizontalDamping * Time.deltaTime);

        // Subtle horizontal drift
        float drift = Mathf.Sin(Time.time * driftSpeed + _randomDriftOffset) * driftAmount * Time.deltaTime;
        movement.x += drift;

        transform.Translate(movement, Space.World);

        // Auto-destruction when reaching peak height
        if (transform.position.y > destroyHeight)
        {
            Destroy(gameObject);
        }
    }
}
