using Metroidvania.Entities;
using UnityEngine;

namespace Metroidvania.Characters.Knight
{
    public abstract class KnightStateBase
    {
        public readonly KnightStateMachine machine;

        public KnightCharacterController character => machine.character;

        public virtual bool isCrouchState => false;
        public virtual bool isInvincible => false;

        protected KnightStateBase(KnightStateMachine machine)
        {
            this.machine = machine;
        }

        public abstract bool CanEnter();

        public virtual void Enter(KnightStateBase previousState) { }

        public virtual void Transition() { }
        public virtual void Update() { }
        public virtual void PhysicsUpdate() { }

        public virtual void Exit() { }

        public virtual bool TryEnter()
        {
            if (CanEnter())
            {
                machine.EnterState(this);
                return true;
            }
            return false;
        }

        public virtual void HandleJump()
        {
            machine.jumpState.TryEnter();
        }

        public virtual void HandleDash()
        {
            machine.rollState.TryEnter();
        }

        public virtual void HandleAttack()
        {
            machine.TryEnterAttackState();
        }
        
        public virtual void HandleHeavyAttack()
        {
            machine.TryEnterHeavyAttackState();
        }
        
        public virtual void HandleDirectionalDash(float direction)
        {
            var directionalDashState = machine.directionalDashState;
            if (directionalDashState.CanEnterWithDirection(direction))
            {
                directionalDashState.EnterWithDirection(direction);
            }
            // Also try from other states if current state can't dash
            else if (this != machine.directionalDashState)
            {
                if (directionalDashState.CanEnterWithDirection(direction))
                {
                    directionalDashState.EnterWithDirection(direction);
                }
            }
        }
    }

    public class KnightIdleState : KnightStateBase
    {
        public KnightIdleState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.IdleAnimHash);
            
            // Reset jumps when touching ground
            machine.ResetJumps();
        }

        public override void Transition()
        {
            if (!(machine.fallState.TryEnter() || machine.crouchIdleState.TryEnter() || machine.crouchWalkState.TryEnter()) && character.horizontalMove != 0)
                machine.EnterState(machine.runState);
        }

        public override void PhysicsUpdate()
        {
            character.rb.Slide(Vector2.zero, Time.deltaTime, character.data.slideMovement);
        }
    }

    public class KnightRunState : KnightStateBase
    {
        public KnightRunState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return Mathf.Abs(character.horizontalMove) > 0.0f && character.collisionChecker.isGrounded && !character.collisionChecker.CollidingInWall(character.horizontalMove);
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.RunAnimHash);
            
            // Reset jumps when touching ground
            machine.ResetJumps();
        }

        public override void Transition()
        {
            if (!(machine.fallState.TryEnter() || machine.crouchIdleState.TryEnter() || machine.crouchWalkState.TryEnter()) && character.horizontalMove == 0)
                machine.EnterState(machine.idleState);
        }

        public override void PhysicsUpdate()
        {
            character.rb.Slide(new Vector2(character.data.moveSpeed * character.horizontalMove, 0.0f), Time.deltaTime, character.data.slideMovement);
            character.FlipFacingDirection(character.horizontalMove);
        }
    }

    public class KnightJumpState : KnightStateBase
    {
        private bool _jumpPressed;

        public KnightJumpState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            // First jump: grounded + can stand
            if (character.collisionChecker.isGrounded && character.canStand)
                return character.jumpAction.IsPressed() && machine.jumpsRemaining > 0;
            
            // Double jump: in air + double jump enabled
            return character.jumpAction.IsPressed() && 
                   character.data.enableDoubleJump && 
                   machine.jumpsRemaining > 0 && 
                   !character.collisionChecker.isGrounded;
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.JumpAnimHash);
            character.particles.jump.Play();

            // Consume a jump
            machine.TryConsumeJump();
            
            // Use different height for double jump
            float jumpForce = character.collisionChecker.isGrounded ? 
                             character.data.jumpHeight : 
                             character.data.doubleJumpHeight;
                             
            character.rb.linearVelocityY = jumpForce;
            _jumpPressed = true;
        }

        public override void Transition()
        {
            if (character.rb.linearVelocityY < 0.0f)
                machine.EnterDefaultState();
        }

        public override void Update()
        {
            _jumpPressed = character.jumpAction.IsPressed();
        }

        public override void PhysicsUpdate()
        {
            if (!_jumpPressed)
            {
                character.rb.linearVelocityY += (character.data.jumpLowMultiplier - 1) * Physics2D.gravity.y * Time.deltaTime;
            }
            if (character.collisionChecker.CollidingInWall(character.horizontalMove))
            {
                character.rb.linearVelocityX = 0.0f;
            }
            else
            {
                character.rb.linearVelocityX = character.data.airMoveSpeed * character.horizontalMove;
                character.FlipFacingDirection(character.horizontalMove);
            }
        }

        public override void HandleJump() { }
    }

    public class KnightFallState : KnightStateBase
    {
        private float _fallStartPositionY;

        public KnightFallState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return !character.collisionChecker.isGrounded;
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.FallAnimHash);
            _fallStartPositionY = character.rb.position.y;
        }

        public override void Transition()
        {
            if (character.collisionChecker.isGrounded)
            {
                if (_fallStartPositionY - character.rb.position.y > character.data.fallParticlesDistance)
                    character.particles.landing.Play();
                machine.EnterDefaultState();
            }
            else
            {
                machine.wallslideState.TryEnter();
            }
        }

        public override void PhysicsUpdate()
        {
            if (!character.collisionChecker.CollidingInWall(character.horizontalMove))
                character.rb.linearVelocityX = character.data.airMoveSpeed * character.horizontalMove;
            else
                character.rb.linearVelocityX = 0.0f;

            character.rb.linearVelocityY += (character.data.jumpFallMultiplier - 1) * Physics2D.gravity.y * Time.deltaTime;
            character.FlipFacingDirection(character.horizontalMove);
        }
        
        public override void HandleJump()
        {
            // Allow double jump in fall state
            if (character.data.enableDoubleJump && machine.jumpsRemaining > 0)
                machine.jumpState.TryEnter();
        }
        
        public override void HandleAttack()
        {
            // Handle aerial combat in fall state
            if (character.data.enableAerialCombat)
            {
                machine.TryEnterAerialAttackState();
            }
        }
    }

    public class KnightRollState : KnightStateBase
    {
        public override bool isInvincible => true;

        private float _elapsedTime;
        private float _lastExitTime;

        public bool isInCooldown => Time.time - _lastExitTime < character.data.rollCooldown;

        public KnightRollState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return character.collisionChecker.isGrounded && !machine.currentState.isCrouchState && character.dashAction.WasPerformedThisFrame() && !isInCooldown;
        }

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;

            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.RollAnimHash, true);
            character.FlipFacingDirection(character.facingDirection);
        }

        public override void Transition()
        {

            if (_elapsedTime > character.data.rollDuration)
                machine.EnterDefaultState();
            else
                machine.fallState.TryEnter();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

        }

        public override void PhysicsUpdate()
        {
            float curveMultiplier = character.data.rollHorizontalMoveCurve.Evaluate(_elapsedTime / character.data.rollDuration);
            character.rb.Slide(new Vector2(character.data.rollSpeed * curveMultiplier * character.facingDirection, 0.0f), Time.deltaTime, character.data.slideMovement);
        }

        public override void Exit()
        {
            _lastExitTime = Time.time;
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public abstract class KnightCrouchStateBase : KnightStateBase
    {
        public override bool isCrouchState => true;

        public KnightCrouchStateBase(KnightStateMachine machine) : base(machine) { }

        public override void HandleJump()
        {
            character.TryDropPlatform();
        }

        public override void HandleDash()
        {
            machine.slideState.TryEnter();
        }

        public override void HandleAttack()
        {
            machine.crouchAttackState.TryEnter();
        }
    }

    public class KnightCrouchIdleState : KnightCrouchStateBase
    {
        private float _elapsedTime;
        private bool _inQuittingAnim;
        private float _quittingAnimElapsedTime;
        private bool _hasSwappedAnim;

        public KnightCrouchIdleState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return (character.crouchAction.IsPressed() || !character.canStand) && character.collisionChecker.isGrounded && character.horizontalMove == 0;
        }

        public override void Enter(KnightStateBase previousState)
        {
            bool shouldMakeTransition = !previousState.isCrouchState;

            _elapsedTime = 0;
            _quittingAnimElapsedTime = 0;
            _inQuittingAnim = false;

            character.SetColliderBounds(character.data.crouchColliderBounds);
            
            // Reset jumps when touching ground
            machine.ResetJumps();

            _hasSwappedAnim = !shouldMakeTransition;
            character.SwitchAnimation(shouldMakeTransition
                ? KnightCharacterController.CrouchTransitionAnimHash
                : KnightCharacterController.CrouchIdleAnimHash);
        }

        public override void Transition()
        {
            if (!(machine.fallState.TryEnter() || machine.crouchWalkState.TryEnter()) && _quittingAnimElapsedTime >= character.data.crouchTransitionTime)
                machine.EnterState(machine.idleState);
        }

        public override void Update()
        {
            if (_inQuittingAnim)
                _quittingAnimElapsedTime += Time.deltaTime;

            _elapsedTime += Time.deltaTime;

            if (!_hasSwappedAnim && _elapsedTime > character.data.crouchTransitionTime)
            {
                _hasSwappedAnim = true;
                character.SwitchAnimation(KnightCharacterController.CrouchIdleAnimHash);
            }
            else if (!character.crouchAction.IsPressed() && character.canStand)
            {
                _inQuittingAnim = true;
            }

            if (character.jumpAction.WasPerformedThisFrame())
                character.TryDropPlatform();
        }

        public override void PhysicsUpdate()
        {
            character.rb.Slide(Vector2.zero, Time.deltaTime, character.data.slideMovement);
        }
    }

    public class KnightCrouchWalkState : KnightCrouchStateBase
    {
        private float _elapsedTime;
        private bool _inQuittingAnim;
        private float _quittingAnimElapsedTime;
        private bool _hasSwappedAnim;

        public KnightCrouchWalkState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return (character.crouchAction.IsPressed() || !character.canStand) && character.collisionChecker.isGrounded && character.horizontalMove != 0;
        }

        public override void Enter(KnightStateBase previousState)
        {
            bool shouldMakeTransition = !previousState.isCrouchState;

            _elapsedTime = 0;
            _inQuittingAnim = false;
            _quittingAnimElapsedTime = 0;

            character.SetColliderBounds(character.data.crouchColliderBounds);
            
            // Reset jumps when touching ground
            machine.ResetJumps();

            _hasSwappedAnim = !shouldMakeTransition;
            character.SwitchAnimation(shouldMakeTransition
                ? KnightCharacterController.CrouchTransitionAnimHash
                : KnightCharacterController.CrouchWalkAnimHash);
        }

        public override void Transition()
        {
            if (!(machine.fallState.TryEnter() || machine.crouchIdleState.TryEnter()) && _quittingAnimElapsedTime >= character.data.crouchTransitionTime)
                machine.EnterState(machine.idleState);
        }

        public override void Update()
        {
            if (_inQuittingAnim)
                _quittingAnimElapsedTime += Time.deltaTime;

            _elapsedTime += Time.deltaTime;

            if (!_inQuittingAnim && !_hasSwappedAnim && _elapsedTime > character.data.crouchTransitionTime)
            {
                _hasSwappedAnim = true;
                character.SwitchAnimation(KnightCharacterController.CrouchWalkAnimHash);
            }

            if (!character.crouchAction.IsPressed() && character.canStand)
                _inQuittingAnim = true;

            if (character.jumpAction.WasPerformedThisFrame())
                character.TryDropPlatform();
        }

        public override void PhysicsUpdate()
        {
            character.rb.Slide(new Vector2(character.data.crouchWalkSpeed * character.horizontalMove, 0.0f), Time.deltaTime, character.data.slideMovement);
            character.FlipFacingDirection(character.horizontalMove);
        }
    }

    public class KnightSlideState : KnightStateBase
    {
        public override bool isCrouchState => true;
        public override bool isInvincible => true;

        private float _elapsedTime;
        private float _lastExitTime = int.MinValue;

        private bool _inQuittingAnim;

        public bool isInCooldown => Time.time - _lastExitTime < character.data.slideCooldown;

        public KnightSlideState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return character.collisionChecker.isGrounded && machine.currentState.isCrouchState && character.dashAction.WasPerformedThisFrame() && !isInCooldown;
        }

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;
            _inQuittingAnim = false;

            character.SetColliderBounds(character.data.crouchColliderBounds);
            character.SwitchAnimation(KnightCharacterController.SlideAnimHash, true);
            character.particles.slide.Play();
            character.FlipFacingDirection(character.facingDirection);
        }

        public override void Transition()
        {
            if (_elapsedTime > character.data.slideDuration)
                machine.EnterState(machine.crouchIdleState);
            else
                machine.fallState.TryEnter();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

            if (!_inQuittingAnim && _elapsedTime > character.data.slideDuration - character.data.slideTransitionTime)
            {
                character.SwitchAnimation(KnightCharacterController.SlideEndAnimHash);
                _inQuittingAnim = true;
            }
        }

        public override void PhysicsUpdate()
        {
            float slideProgress = _elapsedTime / character.data.slideDuration;
            float curveMultiplier = character.data.slideMoveCurve.Evaluate(slideProgress);
            character.rb.Slide(new Vector2(character.data.slideSpeed * curveMultiplier * character.facingDirection, 0.0f), Time.deltaTime, character.data.slideMovement);
        }

        public override void Exit()
        {
            _lastExitTime = Time.time;
            character.particles.slide.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightWallslideState : KnightStateBase
    {
        public KnightWallslideState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter()
        {
            return !character.collisionChecker.isGrounded && character.collisionChecker.CollidingInWall(character.horizontalMove) && character.horizontalMove == character.facingDirection;
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.WallslideAnimHash);
            character.particles.wallslide.Play();
            character.rb.linearVelocityX = 0.0f;
        }

        public override void Transition()
        {
            if (!CanEnter())
                machine.EnterDefaultState();
        }

        public override void PhysicsUpdate()
        {
            character.rb.linearVelocityY = -character.data.wallSlideSpeed;
        }

        public override void Exit()
        {
            character.particles.wallslide.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public override void HandleJump()
        {
            machine.walljumpState.TryEnter();
        }

        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightWalljumpState : KnightStateBase
    {
        private float _elapsedTime;

        public KnightWalljumpState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;

            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.JumpAnimHash);
            character.Flip();
            {
                ParticleSystem.ShapeModule shape = character.particles.walljump.shape;
                shape.rotation = new Vector3(0, 0, 90 * -character.facingDirection);
                character.particles.walljump.Play();
            }

            // Reset jumps on wall jump (gives you another chance to double jump)
            machine.ResetJumps();
            machine.TryConsumeJump(); // Consume one jump for the wall jump itself

            character.rb.linearVelocity = new Vector2(character.data.wallJumpForce.x * character.facingDirection, character.data.wallJumpForce.y);
        }

        public override void Transition()
        {
            if (_elapsedTime > character.data.wallJumpDuration)
                machine.EnterDefaultState();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightAttackState : KnightStateBase
    {
        protected enum ExitAttackCommand { None, Roll, Slide }

        public static int lastStandAttack = 0;
        public static float lastAttackTime = 0;

        protected float _elapsedTime;
        protected bool _triggered;
        protected ExitAttackCommand _currentExitCommand;

        public readonly KnightData.Attack attackData;
        public readonly int animHash;
        public readonly KnightData.ColliderBounds colliderBounds;

        public KnightAttackState nextAttackState { get; set; }

        public KnightAttackState(KnightStateMachine machine, KnightData.Attack attackData, int animHash, KnightData.ColliderBounds colliderBounds) : base(machine)
        {
            this.attackData = attackData;
            this.animHash = animHash;
            this.colliderBounds = colliderBounds;
        }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;
            _triggered = false;
            _currentExitCommand = ExitAttackCommand.None;

            character.SetColliderBounds(colliderBounds);
            character.SwitchAnimation(animHash, true);
            character.rb.linearVelocityX = 0;
        }

        public override void Transition()
        {
            _ = machine.fallState.TryEnter();

            if (_elapsedTime < attackData.duration - attackData.attackEndOffset)
                return;

            switch (_currentExitCommand)
            {
                case ExitAttackCommand.Roll:
                    if (machine.rollState.isInCooldown)
                        break;
                    machine.EnterState(machine.rollState);
                    return;
                case ExitAttackCommand.Slide:
                    if (machine.slideState.isInCooldown)
                        break;
                    machine.EnterState(machine.slideState);
                    return;
            }

            if (character.attackAction.WasPerformedThisFrame())
            {
                if (nextAttackState.isCrouchState && !character.crouchAction.IsPressed() && character.canStand)
                    machine.EnterState(machine.firstAttackState);
                else if (!nextAttackState.isCrouchState && character.crouchAction.IsPressed())
                    machine.EnterState(machine.crouchAttackState);
                else
                    machine.EnterState(nextAttackState);
            }
            else if (_elapsedTime > attackData.duration)
                machine.EnterDefaultState();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

            if (character.dashAction.WasPerformedThisFrame())
            {
                _currentExitCommand = character.crouchAction.IsPressed() || !character.canStand ? ExitAttackCommand.Slide : ExitAttackCommand.Roll;
            }

            if (!_triggered && _elapsedTime >= attackData.triggerTime)
            {
                _triggered = true;
                character.PerformAttack(attackData);
            }
        }

        public override void Exit()
        {
            if (character.horizontalMove != 0)
                character.FlipTo((int)Mathf.Sign(character.horizontalMove));
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }

        public static int StepAttack(float attackComboMaxDelay)
        {
            lastStandAttack++;
            lastAttackTime = Time.time;

            if (lastStandAttack > 2 || Time.time - lastAttackTime >= attackComboMaxDelay)
                lastStandAttack = 1;

            return lastStandAttack;
        }
    }

    public class KnightCrouchAttackState : KnightAttackState
    {
        public override bool isCrouchState => true;

        public KnightCrouchAttackState(KnightStateMachine machine) : base(machine, machine.character.data.crouchAttack, KnightCharacterController.CrouchAttackAnimHash, machine.character.data.crouchColliderBounds) { }
    }

    public class KnightHurtState : KnightStateBase
    {
        public override bool isInvincible => true;

        private float _elapsedTime;

        public EntityHitData hitData { get; set; }

        public KnightHurtState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;

            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.HurtAnimHash);

            character.rb.linearVelocity = Vector2.zero;
            character.rb.AddForce(hitData.knockbackForce, ForceMode2D.Impulse);
        }

        public override void Transition()
        {
            if (_elapsedTime > character.data.hurtTime)
                machine.EnterDefaultState();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }

        public void EnterHurtState(EntityHitData hitData)
        {
            this.hitData = hitData;
            machine.EnterState(this);
        }
    }

    public class KnightDieState : KnightStateBase
    {
        public override bool isInvincible => true;

        public KnightDieState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            character.SwitchAnimation(KnightCharacterController.DieAnimHash, true);
            character.SetColliderBounds(character.data.crouchColliderBounds);
            character.rb.linearVelocity = Vector2.zero;
            character.data.onDieChannel.Raise(character);
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightFakeWalkState : KnightStateBase
    {
        public override bool isInvincible => true;

        private float _elapsedTime;

        public float currentWalkDuration { get; set; }

        public KnightFakeWalkState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0;
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.RunAnimHash);
        }

        public override void Transition()
        {
            if (!machine.fallState.TryEnter() && _elapsedTime > currentWalkDuration)
            {
                machine.EnterDefaultState();
            }
        }

        public override void PhysicsUpdate()
        {
            character.rb.Slide(new Vector2(character.facingDirection * character.data.moveSpeed, 0.0f), Time.deltaTime, character.data.slideMovement);
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }

        public void EnterFakeWalk(float duration)
        {
            currentWalkDuration = duration;
            machine.EnterState(this);
        }
    }

    public class KnightValidationState : KnightStateBase
    {
        public KnightValidationState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => true;
    }

    public class KnightDirectionalDashState : KnightStateBase
    {
        private float _dashDirection; // -1 for left, 1 for right
        private float _elapsedTime;
        private float _lastExitTime = -100f; // Initialize to allow first dash

        public bool isInCooldown => Time.time - _lastExitTime < character.data.dashCooldown;

        public KnightDirectionalDashState(KnightStateMachine machine) : base(machine) { }

        public override bool CanEnter() => false; // Use CanEnterWithDirection instead

        public bool CanEnterWithDirection(float direction)
        {
            return character.data.enableDirectionalDash && !isInCooldown;
        }

        public void EnterWithDirection(float direction)
        {
            _dashDirection = direction;
            machine.EnterState(this);
        }

        public override void Enter(KnightStateBase previousState)
        {
            _elapsedTime = 0f;

            character.SetColliderBounds(character.data.standColliderBounds);
            // Use Dash animation (fallback to Run if Dash doesn't exist)
            character.SwitchAnimation(KnightCharacterController.DashAnimHash, true);
            
            // Set facing direction to dash direction
            character.FlipTo((int)_dashDirection);
        }

        public override void Transition()
        {
            if (_elapsedTime > character.data.dashDuration)
                machine.EnterDefaultState();
            else
                machine.fallState.TryEnter(); // Allow transition to fall if not grounded
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        public override void PhysicsUpdate()
        {
            float speedMultiplier = 1f;
            
            // Apply curve if available, otherwise use linear falloff
            if (character.data.dashCurve != null && character.data.dashCurve.keys.Length > 0)
            {
                float normalizedTime = character.data.dashDuration > 0 ? _elapsedTime / character.data.dashDuration : 1f;
                speedMultiplier = character.data.dashCurve.Evaluate(normalizedTime);
            }
            else
            {
                // Default behavior: maintain speed for 80% of duration, then quick falloff
                float normalizedTime = character.data.dashDuration > 0 ? _elapsedTime / character.data.dashDuration : 1f;
                speedMultiplier = normalizedTime < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (normalizedTime - 0.8f) / 0.2f);
            }

            // Apply horizontal dash velocity
            character.rb.linearVelocityX = character.data.dashSpeed * speedMultiplier * _dashDirection;
            
            // Don't modify Y velocity - maintain current vertical movement
        }

        public override void Exit()
        {
            _lastExitTime = Time.time;
        }

        // During dash, disable other actions
        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
        public override void HandleDirectionalDash(float direction) { } // Can't dash while dashing
    }

    // ============ AERIAL COMBAT STATES ============
    
    public class KnightAirAttackState : KnightStateBase
    {
        public KnightStateBase nextAirAttackState;  // More flexible - can be any state
        
        private readonly KnightData.Attack attackData;
        private readonly int animationHash;
        private readonly int attackNumber; // 1 or 2
        private float _elapsedTime;
        private float _hoverStartTime;
        private bool _isHovering;
        private float _targetDistance; // Fixed distance to move forward
        private float _startPositionX; // Starting X position
        private float _moveSpeed; // Speed of forward movement

        public KnightAirAttackState(KnightStateMachine machine, KnightData.Attack attackData, int animationHash, int attackNumber) : base(machine)
        {
            this.attackData = attackData;
            this.animationHash = animationHash;
            this.attackNumber = attackNumber;
        }

        public override bool CanEnter()
        {
            return !character.collisionChecker.isGrounded && machine.IsInAirCombo();
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(animationHash);
            
            _elapsedTime = 0f;
            _hoverStartTime = Time.time;
            _isHovering = true;
            
            // Start hovering - reduce gravity and stop falling
            character.rb.gravityScale = 0.1f;
            character.rb.linearVelocityY = 0f;
            
            // ✨ FIXED FORWARD MOVEMENT: Set target distance and movement speed
            _targetDistance = character.data.airAttackForwardDistance; // Configurable distance
            _startPositionX = character.transform.position.x;
            _moveSpeed = _targetDistance / character.data.airAttackHoverDuration; // Move distance over hover duration
            
            int facingDirection = character.transform.localScale.x > 0 ? 1 : -1;
            
            // DEBUG LOGS
            Debug.Log($"[AIR ATTACK #{attackNumber}] ENTER - Fixed distance movement setup!");
            Debug.Log($"[AIR ATTACK #{attackNumber}] Target distance: {_targetDistance}, moveSpeed: {_moveSpeed}");
            Debug.Log($"[AIR ATTACK #{attackNumber}] Start position: {_startPositionX}, facing: {facingDirection}");
            Debug.Log($"[AIR ATTACK #{attackNumber}] Current velocity: {character.rb.linearVelocity}");
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

            // Trigger attack at specific time
            if (_elapsedTime >= attackData.triggerTime)
            {
                character.PerformAttack(attackData);
            }
        }

        public override void PhysicsUpdate()
        {
            // Maintain hover for specified duration
            if (_isHovering && Time.time - _hoverStartTime < character.data.airAttackHoverDuration)
            {
                character.rb.linearVelocityY = 0f;
                
                // ✨ FIXED DISTANCE MOVEMENT: Move forward at constant speed
                int facingDirection = character.transform.localScale.x > 0 ? 1 : -1;
                float currentDistance = Mathf.Abs(character.transform.position.x - _startPositionX);
                
                if (currentDistance < _targetDistance && !character.collisionChecker.CollidingInWall(facingDirection))
                {
                    float forwardVelocity = _moveSpeed * facingDirection;
                    character.rb.linearVelocityX = forwardVelocity;
                    
                    // DEBUG LOG
                    Debug.Log($"[AIR ATTACK #{attackNumber}] MOVING - distance: {currentDistance:F2}/{_targetDistance}, velocity: {forwardVelocity:F2}");
                }
                else
                {
                    character.rb.linearVelocityX = 0f;
                    Debug.Log($"[AIR ATTACK #{attackNumber}] STOPPED - distance reached or wall hit");
                }
            }
            else
            {
                _isHovering = false;
                character.rb.gravityScale = 1f; // Restore normal gravity
                
                // Allow normal air movement after hover
                if (!character.collisionChecker.CollidingInWall(character.horizontalMove))
                {
                    character.rb.linearVelocityX = character.data.airMoveSpeed * character.horizontalMove;
                }
            }
            
            Debug.Log($"[AIR ATTACK #{attackNumber}] Current velocity: {character.rb.linearVelocity}, isHovering: {_isHovering}");
            character.FlipFacingDirection(character.horizontalMove);
        }

        public override void Transition()
        {
            // Land check
            if (character.collisionChecker.isGrounded)
            {
                machine.OnGroundedLanding();
                machine.EnterDefaultState();
                return;
            }
            
            // Use hover duration as minimum attack time, regardless of attackData.duration
            float minimumAttackTime = character.data.airAttackHoverDuration;
            
            Debug.Log($"[AIR ATTACK] Elapsed: {_elapsedTime}s, Minimum time: {minimumAttackTime}s");
            
            // Attack finished after hover duration
            if (_elapsedTime > minimumAttackTime)
            {
                Debug.Log($"[AIR ATTACK] Attack #{attackNumber} finished after {_elapsedTime}s");
                
                float timeSinceHover = Time.time - (_hoverStartTime + minimumAttackTime);
                
                // For first attack, wait for player input with 2 second window
                if (attackNumber == 1)
                {
                    // Check for next input within 2 second window
                    if (character.attackAction.WasPerformedThisFrame() && timeSinceHover <= 2.0f)
                    {
                        Debug.Log("[AIR ATTACK] Continuing to second attack");
                        // Continue to second air attack
                        machine.EnterState(nextAirAttackState);
                        return;
                    }
                    
                    // If 2 seconds passed, combo ends
                    if (timeSinceHover > 2.0f)
                    {
                        Debug.Log("[AIR ATTACK] Combo timeout - falling");
                        machine.EndAirCombo();
                        machine.EnterState(machine.fallState);
                        return;
                    }
                    
                    // Still within window, keep waiting
                }
                
                // For second attack, wait for player input for third attack (MANUAL DOWNWARD STRIKE)
                if (attackNumber == 2)
                {
                    // Check for manual input for downward strike
                    if (character.attackAction.WasPerformedThisFrame())
                    {
                        Debug.Log("[AIR ATTACK] Manual input - triggering DOWNWARD STRIKE!");
                        // MANUAL downward strike as 3rd attack
                        machine.EnterState(machine.airDownwardStrikeState);
                        return;
                    }
                    
                    // No timeout for second attack - wait indefinitely for input or ground
                    // Will fall naturally if no input and will transition on ground contact
                }
            }
        }

        public override void Exit()
        {
            character.rb.gravityScale = 1f; // Ensure gravity is restored
        }

        // Disable other actions during air attack
        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightDownwardStrikeState : KnightStateBase
    {
        private float _elapsedTime;
        private bool _hasTriggeredAttack;
        private bool _isAccelerating;

        public KnightDownwardStrikeState(KnightStateMachine machine) : base(machine)
        {
        }

        public override bool CanEnter()
        {
            return !character.collisionChecker.isGrounded && machine.IsInAirCombo();
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.FirstAttackAnimHash); // Use normal attack animation
            
            _elapsedTime = 0f;
            _hasTriggeredAttack = false;
            _isAccelerating = false;
            
            // TEMPORARY DEBUG - Remove after testing
            Debug.Log($"[DOWNWARD STRIKE] Starting downward strike with acceleration {character.data.downwardStrikeAcceleration}");
            
            // Brief hover before downward acceleration
            character.rb.gravityScale = 0f;
            character.rb.linearVelocityY = 0f;
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

            // Start acceleration after brief delay
            if (!_isAccelerating && _elapsedTime >= 0.1f)
            {
                _isAccelerating = true;
                character.rb.gravityScale = 1f;
            }

            // Trigger enhanced attack
            if (!_hasTriggeredAttack && _elapsedTime >= character.data.airDownwardStrike.triggerTime)
            {
                // Create enhanced attack data with increased damage
                var enhancedAttack = character.data.airDownwardStrike;
                var enhancedDamage = Mathf.RoundToInt(enhancedAttack.damage * character.data.downwardStrikeDamageMultiplier);
                
                // Create temporary enhanced attack data
                var tempAttackData = new KnightData.Attack
                {
                    duration = enhancedAttack.duration,
                    horizontalMoveOffset = enhancedAttack.horizontalMoveOffset,
                    triggerTime = enhancedAttack.triggerTime,
                    attackEndOffset = enhancedAttack.attackEndOffset,
                    triggerCollider = enhancedAttack.triggerCollider,
                    damage = enhancedDamage,
                    force = enhancedAttack.force
                };
                
                character.PerformAttack(tempAttackData);
                _hasTriggeredAttack = true;
            }
        }

        public override void PhysicsUpdate()
        {
            if (_isAccelerating)
            {
                // Apply strong downward acceleration
                character.rb.linearVelocityY = -character.data.downwardStrikeAcceleration;
                character.rb.linearVelocityX = 0f; // Lock horizontal movement during strike
            }
        }

        public override void Transition()
        {
            if (character.collisionChecker.isGrounded)
            {
                machine.OnGroundedLanding();
                // Create landing impact effect
                character.particles.landing.Play();
                machine.EnterDefaultState();
            }
        }

        public override void Exit()
        {
            machine.EndAirCombo();
            character.rb.gravityScale = 1f;
        }

        // Disable all actions during downward strike
        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }

    public class KnightFallAttackState : KnightStateBase
    {
        private float _elapsedTime;
        private bool _hasPerformedLandingAttack;
        private bool _hasHitEnemyDuringFall;
        private float _fallStartHeight;
        private Collider2D[] _enemyCheckHits = new Collider2D[8];

        public KnightFallAttackState(KnightStateMachine machine) : base(machine)
        {
        }

        public override bool CanEnter()
        {
            return !character.collisionChecker.isGrounded;
        }

        public override void Enter(KnightStateBase previousState)
        {
            character.SetColliderBounds(character.data.standColliderBounds);
            character.SwitchAnimation(KnightCharacterController.FirstAttackAnimHash); // Reuse attack animation
            
            _elapsedTime = 0f;
            _hasPerformedLandingAttack = false;
            _hasHitEnemyDuringFall = false;
            _fallStartHeight = character.rb.position.y;
            
            // AGGRESSIVE FALL ACCELERATION
            // 1. Immediate downward velocity boost
            character.rb.linearVelocityY = Mathf.Min(character.rb.linearVelocityY, -8f);
            
            // 2. Dramatically increase gravity for fast fall
            character.rb.gravityScale = 3.5f;
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        public override void PhysicsUpdate()
        {
            // Allow limited horizontal movement
            if (!character.collisionChecker.CollidingInWall(character.horizontalMove))
                character.rb.linearVelocityX = character.data.airMoveSpeed * 0.5f * character.horizontalMove;
            
            // CONTINUOUS DOWNWARD ACCELERATION during fall attack
            // Add extra downward force for dramatic fall effect
            character.rb.linearVelocityY += -15f * Time.deltaTime;
            
            character.FlipFacingDirection(character.horizontalMove);
            
            // Check for enemies during fall (if not already hit one)
            if (!_hasHitEnemyDuringFall)
            {
                CheckForEnemiesDuringFall();
            }
        }

        private void CheckForEnemiesDuringFall()
        {
            var contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(character.data.hittableLayer);
            
            // Check small area around character for direct enemy contact
            Vector2 checkPosition = character.rb.position;
            Vector2 checkSize = new Vector2(0.8f, 1.2f); // Slightly smaller than full attack area
            
            int hitCount = Physics2D.OverlapBox(checkPosition, checkSize, 0, contactFilter, _enemyCheckHits);
            
            if (hitCount > 0)
            {
                // Hit enemy during fall - deal reduced damage and end attack
                float fallHeight = _fallStartHeight - character.rb.position.y;
                float damageMultiplier = fallHeight >= character.data.minimumFallHeight ? 
                    character.data.fallAttackDamageMultiplier * 0.7f : 0.7f; // 70% damage for mid-air hits
                
                var midAirDamage = Mathf.RoundToInt(character.data.fallAttack.damage * damageMultiplier);
                
                // Create mid-air attack data
                var midAirAttackData = new KnightData.Attack
                {
                    duration = character.data.fallAttack.duration,
                    horizontalMoveOffset = character.data.fallAttack.horizontalMoveOffset * 0.5f,
                    triggerTime = 0f,
                    attackEndOffset = character.data.fallAttack.attackEndOffset,
                    triggerCollider = new Rect(checkPosition.x - checkSize.x/2, checkPosition.y - checkSize.y/2, checkSize.x, checkSize.y),
                    damage = midAirDamage,
                    force = character.data.fallAttack.force * 0.8f
                };
                
                character.PerformAttack(midAirAttackData);
                _hasHitEnemyDuringFall = true;
                
                // Slight upward bounce after hitting enemy
                character.rb.linearVelocityY = Mathf.Max(character.rb.linearVelocityY, 3f);
            }
        }

        public override void Transition()
        {
            if (character.collisionChecker.isGrounded)
            {
                // Landing - perform ground impact attack if we haven't hit an enemy during fall
                if (!_hasHitEnemyDuringFall && !_hasPerformedLandingAttack)
                {
                    PerformLandingImpactAttack();
                }
                
                float fallHeight = _fallStartHeight - character.rb.position.y;
                machine.OnGroundedLanding();
                
                // Create impact effect based on fall height
                if (fallHeight >= character.data.minimumFallHeight)
                {
                    character.particles.landing.Play();
                }
                
                machine.EnterDefaultState();
            }
            else if (_elapsedTime > character.data.fallAttack.duration)
            {
                // Return to normal fall if attack duration is over
                machine.EnterState(machine.fallState);
            }
        }

        private void PerformLandingImpactAttack()
        {
            float fallHeight = _fallStartHeight - character.rb.position.y;
            float damageMultiplier = fallHeight >= character.data.minimumFallHeight ? 
                character.data.fallAttackDamageMultiplier : 1f;
            
            var landingDamage = Mathf.RoundToInt(character.data.fallAttack.damage * damageMultiplier);
            
            // Create ground impact attack data - larger area for landing
            var landingAttackData = new KnightData.Attack
            {
                duration = character.data.fallAttack.duration,
                horizontalMoveOffset = character.data.fallAttack.horizontalMoveOffset,
                triggerTime = 0f, // Immediate on landing
                attackEndOffset = character.data.fallAttack.attackEndOffset,
                triggerCollider = character.data.fallAttack.triggerCollider, // Use full configured area
                damage = landingDamage,
                force = character.data.fallAttack.force * damageMultiplier // Scale force with damage
            };
            
            character.PerformAttack(landingAttackData);
            _hasPerformedLandingAttack = true;
        }

        public override void Exit()
        {
            character.rb.gravityScale = 1f;
        }

        public override void HandleJump() { }
        public override void HandleDash() { }
        public override void HandleAttack() { }
    }
    
    public class KnightThrustAttackState : KnightStateBase
    {
        private int attackHash;
        private KnightData.ColliderBounds colliderBounds;
        private KnightData.Attack attackData;
        private float _elapsedTime;
        private bool _triggered;
        
        public KnightThrustAttackState(KnightStateMachine machine) : base(machine)
        {
            // Usa i dati del primo attacco ma con 130% di danno
            attackData = machine.character.data.firstAttack;
            attackHash = KnightCharacterController.FirstAttackAnimHash;
            colliderBounds = machine.character.data.standColliderBounds;
        }
        
        public override bool CanEnter() => true;
        
        public override void Enter(KnightStateBase previousState)
        {
            base.Enter(previousState);
            _elapsedTime = 0;
            _triggered = false;
            
            character.SetColliderBounds(colliderBounds);
            character.SwitchAnimation(attackHash, true);
            character.rb.linearVelocityX = 0;
        }
        
        public override void Update()
        {
            _elapsedTime += UnityEngine.Time.deltaTime;
            
            // Trigger enhanced damage hitbox during active time
            if (!_triggered && _elapsedTime >= attackData.triggerTime)
            {
                _triggered = true;
                
                // Create enhanced attack data with 130% damage
                var originalDamage = attackData.damage;
                var enhancedDamage = Mathf.RoundToInt(originalDamage * 1.3f);
                
                // Create a modified attack data with enhanced damage
                var enhancedAttackData = new KnightData.Attack
                {
                    duration = attackData.duration,
                    horizontalMoveOffset = attackData.horizontalMoveOffset,
                    triggerTime = attackData.triggerTime,
                    attackEndOffset = attackData.attackEndOffset,
                    triggerCollider = attackData.triggerCollider,
                    damage = enhancedDamage,
                    force = attackData.force * 1.1f // Also enhance knockback slightly
                };
                
                character.PerformAttack(enhancedAttackData);
            }
        }
        
        public override void Transition()
        {
            // Check if attack animation is finished
            if (_elapsedTime >= attackData.duration)
            {
                machine.EnterDefaultState();
            }
        }
        
        public override void HandleAttack()
        {
            // Cannot chain from thrust attack - it's a heavy single attack
        }
        
        public override void HandleHeavyAttack()
        {
            // Cannot chain heavy attacks
        }
        
        public override void HandleJump() { }
        public override void HandleDash() { }
    }
}
