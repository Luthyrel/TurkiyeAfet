using UnityEngine;

/// <summary>Reports a direct collision between a moving landslide object and the player.</summary>
public sealed class PrototypeLandslideDebrisImpact : MonoBehaviour
{
    private bool reported;

    private void OnCollisionEnter(Collision collision)
    {
        if (reported || collision == null || collision.collider == null)
            return;

        PrototypePlayerController player = collision.collider.GetComponentInParent<PrototypePlayerController>();
        if (player == null)
            return;

        PrototypeScenario scenario = PrototypeBootstrap.Instance != null
            ? PrototypeBootstrap.Instance.Scenario : null;
        if (scenario == null || !scenario.IsLandslideActive)
            return;

        reported = true;
        scenario.RegisterLandslideImpact(collision.relativeVelocity.magnitude, GetComponent<Rigidbody>());
    }
}
