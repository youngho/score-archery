using UnityEngine;

public class SnowmanBehavior : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float wobbleSpeed = 5f;
    public float wobbleAmount = 15f;
    public Vector3 moveDirection = Vector3.forward;
    public Vector3 wobbleAxis = Vector3.forward;

    private float wobbleTimer;

    void Start()
    {
        wobbleTimer = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Movement
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Wobble (Roly-poly effect)
        wobbleTimer += Time.deltaTime * wobbleSpeed;
        float angle = Mathf.Sin(wobbleTimer) * wobbleAmount;
        
        // Apply wobble around the forward axis (assuming model is upright by default)
        // Note: The base rotation (270, 0, 0) is specific to the imported GLB orientation
        transform.rotation = Quaternion.Euler(270f, 0f, 0f) * Quaternion.AngleAxis(angle, wobbleAxis);
    }
}
