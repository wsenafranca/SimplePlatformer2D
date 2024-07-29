using UnityEngine;

public class IKFoot : MonoBehaviour
{
    [Range(0f, 1f)]
    public float weightPositionRight = 1f;
    [Range(0f, 1f)]
    public float weightRotationRight = 0f;
    [Range(0f, 1f)]
    public float weightPositionLeft = 1f;
    [Range(0f, 1f)]
    public float weightRotationLeft = 0f;

    public Transform pelvis;
    public Transform rightFoot;
    public Transform leftFoot;

    public Vector3 offsetFoot;

    public float groundDistance = 1.5f;
    public LayerMask groundMask;

    private Animator _animator;
    private PlayerMovement _movement;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if(!enabled || Mathf.Abs(_movement.velocity.z) > 0.1f)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0.0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0.0f);
            return;
        }

        var footPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
        if(Physics.Raycast(footPos + Vector3.up, Vector3.down, out var hit, groundDistance, groundMask))
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weightPositionRight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weightRotationRight);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + offsetFoot);

            if (weightRotationRight > 0f) //adjust foot if is enable
            {
                //Little formula to calculate foot rotation (This can be better)
                Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, hit.normal), hit.normal);
                Quaternion pelvisRot = Quaternion.Euler(0.0f, pelvis.eulerAngles.y, 0.0f);
                _animator.SetIKRotation(AvatarIKGoal.RightFoot, footRotation * pelvisRot);
            }
        }
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0.0f);
        }

        footPos = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        if(Physics.Raycast(footPos + Vector3.up, Vector3.down, out hit, groundDistance, groundMask))
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weightPositionLeft);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, weightRotationLeft);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + offsetFoot);

            if (weightRotationLeft > 0f) //adjust foot if is enable
            {
                //Little formula to calculate foot rotation (This can be better)
                Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, hit.normal), hit.normal);
                Quaternion pelvisRot = Quaternion.Euler(0.0f, pelvis.eulerAngles.y, 0.0f);
                _animator.SetIKRotation(AvatarIKGoal.LeftFoot, footRotation * pelvisRot);
            }
        }
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0.0f);
        }
    }
}
