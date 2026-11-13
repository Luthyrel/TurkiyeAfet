using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PrototypeImpactAudio : MonoBehaviour
{
    [SerializeField] private float minimumImpactSpeed = 1.0f;
    [SerializeField] private float repeatDelay = 0.28f;
    [SerializeField] private float volume = 0.36f;

    private float nextAllowedTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < nextAllowedTime || collision.relativeVelocity.sqrMagnitude
            < minimumImpactSpeed * minimumImpactSpeed)
            return;

        nextAllowedTime = Time.time + repeatDelay;
        PrototypeAudio.PlayImpact(volume);
    }
}
