using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3;
    public float groundAcceleration = 10;
    public float airAcceleration = 7;
    public float rotateSpeed = 720;
    public float sprintModifier = 2;

    [Header("Jump")]
    public float jumpHeight = 3;
    public float speedJumpBoost = 1.7f;
    public float jumpInputBuffer = 0.5f;
    public float coyoteTime = 0.2f;
    public float earlyJumpModifier = 0.5f;
    public float fallGravityModifier = 1.5f;
    public float jumpGravityModifier = 1.0f;
    public float clampVerticalVelocity = 10.0f;
    public bool doubleJumpEnabled = true;

    [Header("Wall Sliding")]
    public Vector3 wallCheckPosition;
    public float wallCheckRadius = 0.1f;
    public Vector3 wallClimb;
    public Vector3 wallJumpOff;
    public Vector3 wallLeap;
    public float wallSlidingMaxFallingSpeed = 3.0f;
    public float wallStickTime = 0.3f;
    public float wallSlidingSnapDistance = 1.0f;

    [Header("Dash")]
    public float dashDistance = 5.0f;
    public float dashTime = 0.5f;

    [Header("Ground Slide")]
    public float groundSlideDistance = 3.0f;
    public float groundSlideMoveBoost = 1.3f;
    public float groundSlideTime = 0.5f;
    public float groundSlideColliderHeight  = 0.75f;

    [Header("FallAttack")]
    public float fallAttackVelocity = 20.0f;
    public float fallAttackAcceleration = 50.0f;

    [Header("Collision")]
    public float minFallDistance = 0.5f;
    public float skinWidth = 0.05f;
    public LayerMask groundMask;

    private Animator _animator;
    private InputController _input;
    private CapsuleCollider _collider;

    [Header("States")]
    public bool isGrounded = true;
    public bool isCeilling;
    public bool isHangingWall;
    public bool isWallSliding;
    public float fallDistance;
    public bool isDoubleJumping;
    public bool isDashing;
    public bool isGroundSliding;
    public bool isFallAttacking;
    
    public Vector3 velocity;
    public float rotation;
    public float direction = 1;
    public float _coyoteCounter;
    public float _jumpBufferCounter;
    public bool _canDoubleJump;
    public float _hangingDirection;
    public bool _isSnapping;
    public float _wallStickCounter;
    public float _dashCounter;
    public float _groundSlideCounter;
    public float _moveInput;

    private float _targetHorizontalVelocity;
    private float _targetRotation;

    public Vector3 topPosition => transform.position + _collider.center + Vector3.up * (_collider.height * 0.5f - _collider.radius + 2.0f * skinWidth);
    public Vector3 topPositionGroundSliding => transform.position + _collider.center + Vector3.up * (_collider.height * 0.5f - (_collider.height - groundSlideColliderHeight) - _collider.radius + 2.0f * skinWidth);
    public Vector3 bottomPosition => transform.position + _collider.center + Vector3.down * (_collider.height * 0.5f - _collider.radius + 2.0f * skinWidth);
    public Vector3 frontPosition => transform.position + _collider.center + Vector3.forward * (_collider.radius - 2.0f * skinWidth) * Mathf.Sign(velocity.z);
    public float radius => _collider.radius - skinWidth * 0.0f;

    public float maxMoveSpeed => walkSpeed * (_input.sprint ? sprintModifier : 1.0f);
    public float acceleration => isGrounded ? groundAcceleration : airAcceleration;
    public float jumpHeightBoosted => jumpHeight * Mathf.Lerp(velocity.z / (walkSpeed * sprintModifier), 1.0f, speedJumpBoost);
    public float gravityModifier => (velocity.y < 0 ? fallGravityModifier : velocity.y > 0 ? jumpGravityModifier : 1.0f);

    private static int SPEED_ID = Animator.StringToHash("Speed");
    private static int VELOCITY_Y_ID = Animator.StringToHash("VelocityY");
    private static int GROUNDED_ID = Animator.StringToHash("Grounded");
    private static int WALL_SLIDING_ID = Animator.StringToHash("WallSliding");
    private static int DASHING_ID = Animator.StringToHash("Dashing");
    private static int GROUND_SLIDING_ID = Animator.StringToHash("GroundSliding");
    private static int FALL_ATTACKING_ID = Animator.StringToHash("FallAttacking");

    private static int ACTION_JUMP_ID = Animator.StringToHash("Base Layer.Jump");
    private static int ACTION_DOUBLE_JUMP_ID = Animator.StringToHash("Base Layer.DoubleJump");
    private static int ACTION_WALL_SLIDE_ID = Animator.StringToHash("Base Layer.WallSlide");
    private static int ACTION_DASH_ID = Animator.StringToHash("Base Layer.Dash");
    private static int ACTION_GROUND_SLIDE_ID = Animator.StringToHash("Base Layer.GroundSlide");
    private static int ACTION_FALL_ATTACK_ID = Animator.StringToHash("Base Layer.FallAttack");
    private static int ACTION_BASIC_ATTACK_ID = Animator.StringToHash("Base Layer.BasicAttack");

    public bool debugMode = true;

    private void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponent<Animator>();
        _input = GetComponent<InputController>();
    }

    private void Update()
    {
        UpdateInputMovement();

        UpdateRotation();

        Move(velocity * Time.deltaTime);

        UpdateAnimator();
    }

    private void ApplyJump()
    {
        velocity.y = Mathf.Sqrt(-2.0f * Physics.gravity.y * jumpHeightBoosted);

        _jumpBufferCounter = 0.0f;
        _coyoteCounter = 0.0f;
    }

    private void ApplyWallSlideJump()
    {
        if (_input.moveInput == 0)
        {
            velocity.z = wallJumpOff.z * -_hangingDirection;
            direction = -_hangingDirection;
            velocity.y = wallJumpOff.y;
        }
        else if (Mathf.Sign(_input.moveInput) == _hangingDirection)
        {
            velocity.z = wallClimb.z * -_hangingDirection;
            velocity.y = wallClimb.y;
        }
        else
        {
            velocity.z = wallLeap.z * -_hangingDirection;
            direction = -_hangingDirection;
            velocity.y = wallLeap.y;
        }
    }

    private void UpdateInputMovement()
    {
        if (_input.fallAttack)
        {
            if(!isFallAttacking && !isGrounded && !isWallSliding)
            {
                isFallAttacking = true;
                velocity.y = 0.0f;
                velocity.z = 0.0f;
                _animator.CrossFade(ACTION_FALL_ATTACK_ID, 0.1f);
            }
        }

        if(isFallAttacking)
        {
            velocity.y = Mathf.MoveTowards(velocity.y, -fallAttackVelocity, fallAttackAcceleration * Time.deltaTime);

            if(!isGrounded)
            {
                return;
            }

            isFallAttacking = false;
            velocity.y = 0;
        }

        if(_input.groundSlide)
        {
            if(!isGroundSliding && isGrounded && !isWallSliding && !isDashing)
            {
                var distance = groundSlideDistance * Mathf.Lerp(1, groundSlideMoveBoost, Mathf.Abs(velocity.z)/(walkSpeed * sprintModifier));
                var alpha = GetSlideDistanceAlpha(distance);
                if(alpha > 0)
                {
                    isGroundSliding = true;
                    _animator.CrossFade(ACTION_GROUND_SLIDE_ID, 0.1f);
                    velocity.z = (distance / groundSlideTime) * direction * alpha;
                    _groundSlideCounter = groundSlideTime;
                }
            }
        }

        if(isGroundSliding)
        {
            _groundSlideCounter -= Time.deltaTime;
            if(_groundSlideCounter > 0)
            {
                return;
            }

            velocity.z = 0.0f;
            isGroundSliding = false;
        }

        if(_input.dash)
        {
            if(!isDashing && (!isHangingWall || (isWallSliding && _input.moveInput != direction)))
            {
                direction = _input.moveInput > 0.0f ? 1 : _input.moveInput < 0.0f ? -1 : direction;
                isDashing = true;
                _animator.CrossFade(ACTION_DASH_ID, 0.1f);
                velocity.z = (dashDistance / dashTime) * direction;
                velocity.y = 0.0f;
                _dashCounter = dashTime;
                isWallSliding = false;
                isDoubleJumping = false;
                isHangingWall = false;
                rotation = direction < 0 ? 180 : 0;
                _canDoubleJump = false;
                fallDistance = 0;
            }
        }

        if (isDashing)
        {
            _dashCounter -= Time.deltaTime;
            if (_dashCounter > 0 && !isHangingWall)
            {
                return;
            }

            velocity.z = 0;
            isDashing = false;
        }

        CheckWallSlideSnap();

        _moveInput = _input.moveInput;

        if (velocity.y != 0 && ((_moveInput > 0 && velocity.z < 0) || (_moveInput < 0 && velocity.z > 0))) velocity.z = 0;

        _targetHorizontalVelocity = _moveInput * maxMoveSpeed;
        velocity.z = Mathf.MoveTowards(velocity.z, _targetHorizontalVelocity, acceleration * Time.deltaTime);

        if(isHangingWall && !isGrounded && _moveInput != 0)
        {
            if(!isWallSliding)
            {
                _animator.CrossFade(ACTION_WALL_SLIDE_ID, 0.1f);
            }

            isWallSliding = true;
            isDoubleJumping = false;
            _canDoubleJump = false;
            _isSnapping = false;
            fallDistance = 0;
            velocity.y = Mathf.Max(velocity.y, -wallSlidingMaxFallingSpeed);

            if(_wallStickCounter > 0)
            {
                velocity.z = 0;
                _targetHorizontalVelocity = 0.0f;

                if (_moveInput != _hangingDirection && _moveInput != _hangingDirection)
                {
                    _wallStickCounter -= Time.deltaTime;
                }
                else
                {
                    _wallStickCounter = wallStickTime;
                }
            }
            else
            {
                _wallStickCounter = wallStickTime;
            }
        }
        else
        {
            isWallSliding = false;
        }

        if (isCeilling || isGrounded)
        {
            velocity.y = 0.0f;
            isDoubleJumping = false;
            _canDoubleJump = false;
            fallDistance = 0;
        }

        if (_input.jumpPressed)
        {
            if (doubleJumpEnabled && _canDoubleJump && !isDoubleJumping && !isWallSliding)
            {
                isDoubleJumping = true;
                ApplyJump();
                _animator.CrossFade(ACTION_DOUBLE_JUMP_ID, 0.1f);
                _canDoubleJump = false;
            }
            else
            {
                _jumpBufferCounter = jumpInputBuffer;
                _canDoubleJump = false;
            }
        }

        if (isGrounded)
        {
            _coyoteCounter = coyoteTime;
        }
        else
        {
            _coyoteCounter -= Time.deltaTime;
            _jumpBufferCounter -= Time.deltaTime;
        }

        if (_jumpBufferCounter > 0.0f && !isDoubleJumping)
        {
            if (isWallSliding)
            {
                ApplyWallSlideJump();
                _canDoubleJump = true;
                _animator.CrossFade(ACTION_JUMP_ID, 0.1f);
            }
            else if(_coyoteCounter > 0.0f)
            {
                ApplyJump();
                _animator.CrossFade(ACTION_JUMP_ID, 0.1f);
                _canDoubleJump = true;
            }
        }

        if (velocity.y > 0.0f && _input.jumpReleased)
        {
            velocity.y *= earlyJumpModifier;
        }

        velocity.y += Physics.gravity.y * gravityModifier * Time.deltaTime;
        velocity.y = Mathf.Clamp(velocity.y, -clampVerticalVelocity, clampVerticalVelocity);
    }

    private void CheckWallSlideSnap()
    {
        var checkPosition1 = transform.position + new Vector3(wallCheckPosition.x, wallCheckPosition.y, wallCheckPosition.z * direction);
        var checkPosition2 = transform.position + new Vector3(wallCheckPosition.x, wallCheckPosition.y, (wallCheckPosition.z + wallSlidingSnapDistance) * direction);
        _isSnapping = velocity.z != 0 && !isGrounded && !isWallSliding  && Physics.CheckCapsule(checkPosition1, checkPosition2, wallCheckRadius, groundMask);
    }

    private float GetSlideDistanceAlpha(float distance)
    {
        float standDist;
        if(Physics.CapsuleCast(topPosition, bottomPosition, radius, Vector3.forward * direction, out var hit, distance + skinWidth, groundMask))
        {
            standDist = hit.distance - skinWidth;
        }
        else
        {
            return 1.0f;
        }

        float crouchDist;
        if(!Physics.CapsuleCast(topPositionGroundSliding, bottomPosition, radius, Vector3.forward * direction, distance + skinWidth, groundMask)) // can pass through the path?
        {
            crouchDist = distance;
            var step = Vector3.forward * direction * crouchDist;
            if (!Physics.CheckCapsule(topPosition + step, bottomPosition + step, radius, groundMask)) // can get up after move?
            {
                return crouchDist / distance;
            }
        }

        return standDist / distance;
    }

    private void MoveHorizontal(ref Vector3 velocity)
    {
        if (isGroundSliding) return;

        var dir = direction;
        var distance = Mathf.Abs(velocity.z) + skinWidth;
        if(Mathf.Abs(velocity.z) < skinWidth)
        {
            distance = skinWidth * 2.0f;
        }

        if (Physics.CapsuleCast(topPosition, bottomPosition, radius, Vector3.forward * dir, out var hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            velocity.z = (hit.distance - skinWidth) * dir;
        }

        if (Physics.CheckSphere(transform.position + new Vector3(wallCheckPosition.x, wallCheckPosition.y, wallCheckPosition.z * dir), wallCheckRadius, groundMask))
        {
            isHangingWall = true;
            _hangingDirection = dir;
        }
    }

    private void MoveVertical(ref Vector3 velocity)
    {
        var dir = Mathf.Sign(velocity.y);
        var distance = Mathf.Abs(velocity.y) + skinWidth;
        var moveZ = Vector3.forward * velocity.z;

        if (Physics.CapsuleCast((isGroundSliding ? topPositionGroundSliding : topPosition) + moveZ, bottomPosition + moveZ, radius, Vector3.up * dir, out var hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore))
        {
            if(hit.distance <= distance)
            {
                velocity.y = (hit.distance - skinWidth) * dir;

                isGrounded = dir < 0;
                isCeilling = dir > 0;
            }

            if(!isGrounded && velocity.y <= 0.0f)
            {
                fallDistance = Mathf.Max(hit.distance, fallDistance);
            }
        }
    }

    private void Move(Vector3 velocity)
    {
        direction = velocity.z > 0 ? 1 : velocity.z < 0 ? -1 : direction;

        isGrounded = false;
        isCeilling = false;
        isHangingWall = false;

        MoveHorizontal(ref velocity);

        if (velocity.y != 0)
        {
            MoveVertical(ref velocity);
        }

        if (Mathf.Abs(velocity.z) < 0.0001f) velocity.z = 0.0f;
        if (Mathf.Abs(velocity.y) < 0.0001f) velocity.y = 0.0f;

        transform.position = transform.position + velocity;
    }

    private void UpdateRotation()
    {
        _targetRotation = direction < 0 ? 180 : 0;
        rotation = Mathf.MoveTowardsAngle(rotation, _targetRotation, rotateSpeed * Time.deltaTime);
        transform.localEulerAngles = Vector3.up * rotation;
    }

    private void UpdateAnimator()
    {
        _animator.SetFloat(SPEED_ID, Mathf.Abs(velocity.z) / walkSpeed);
        _animator.SetFloat(VELOCITY_Y_ID, velocity.y);
        _animator.SetBool(GROUNDED_ID, isGrounded);
        _animator.SetBool(WALL_SLIDING_ID, isWallSliding);
        _animator.SetBool(DASHING_ID, isDashing);
        _animator.SetBool(GROUND_SLIDING_ID, isGroundSliding);
        _animator.SetBool(FALL_ATTACKING_ID, isFallAttacking);
    }

    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position + new Vector3(wallCheckPosition.x, wallCheckPosition.y, wallCheckPosition.z * direction), wallCheckRadius);
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(transform.position + new Vector3(wallCheckPosition.x, wallCheckPosition.y, (wallCheckPosition.z + wallSlidingSnapDistance) * direction), wallCheckRadius);

        if (_collider == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(topPosition, radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottomPosition, radius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(frontPosition, radius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(bottomPosition, radius);
        Gizmos.DrawSphere(topPositionGroundSliding, radius);
    }
}
