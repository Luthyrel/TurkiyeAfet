
using UnityEngine;

public class TableHoldIK : MonoBehaviour
{
    public PrototypePlayerController player;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        Debug.Log("TableHoldIK nesneye eklendi: " + gameObject.name);
    }
    private void OnAnimatorIK(int layerIndex)
    {

        if (animator == null)
            return;

        if (player == null || !player.IsHoldingTable)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        Vector3 table = player.TablePosition;

        Vector3 leftHand = table + new Vector3(-1.08f, 0.48f, -0.53f);
        Vector3 rightHand = table + new Vector3(-0.68f, 0.60f, -0.53f);

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);

        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);

        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand);
    }
}
