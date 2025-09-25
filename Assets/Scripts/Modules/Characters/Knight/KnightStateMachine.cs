namespace Metroidvania.Characters.Knight
{
    public class KnightStateMachine
    {
        public readonly KnightCharacterController character;

        public KnightStateBase currentState { get; private set; }
        
        // Double Jump System
        private int currentJumps = 0;
        public int jumpsRemaining => character.data.maxJumps - currentJumps;
        
        // Aerial Combat System
        private int currentAirAttacks = 0;
        public int airAttacksRemaining => 3 - currentAirAttacks;
        private bool isInAirCombo = false;

        public KnightIdleState idleState;
        public KnightRunState runState;
        public KnightJumpState jumpState;
        public KnightFallState fallState;
        public KnightRollState rollState;
        public KnightCrouchIdleState crouchIdleState;
        public KnightCrouchWalkState crouchWalkState;
        public KnightSlideState slideState;
        public KnightWallslideState wallslideState;
        public KnightWalljumpState walljumpState;
        public KnightAttackState firstAttackState, secondAttackState, crouchAttackState;
        public KnightThrustAttackState thrustAttackState; // Heavy attack a terra (V)
        public KnightAirAttackState airFirstAttackState, airSecondAttackState;
        public KnightDownwardStrikeState airDownwardStrikeState;
        public KnightFallAttackState fallAttackState;
        public KnightHurtState hurtState;
        public KnightDieState dieState;
        public KnightFakeWalkState fakeWalkState;
        public KnightDirectionalDashState directionalDashState;

        public KnightStateMachine(KnightCharacterController character)
        {
            this.character = character;
            EnterState(new KnightValidationState(this));

            idleState = new KnightIdleState(this);
            runState = new KnightRunState(this);
            jumpState = new KnightJumpState(this);
            fallState = new KnightFallState(this);
            rollState = new KnightRollState(this);
            crouchIdleState = new KnightCrouchIdleState(this);
            crouchWalkState = new KnightCrouchWalkState(this);
            slideState = new KnightSlideState(this);
            wallslideState = new KnightWallslideState(this);
            walljumpState = new KnightWalljumpState(this);
            firstAttackState = new KnightAttackState(this, character.data.firstAttack, KnightCharacterController.FirstAttackAnimHash, character.data.standColliderBounds);
            secondAttackState = new KnightAttackState(this, character.data.secondAttack, KnightCharacterController.SecondAttackAnimHash, character.data.standColliderBounds);
            thrustAttackState = new KnightThrustAttackState(this);
            crouchAttackState = new KnightCrouchAttackState(this);
            
            // Initialize aerial combat states
            if (character.data.enableAerialCombat)
            {
                airFirstAttackState = new KnightAirAttackState(this, character.data.airFirstAttack, KnightCharacterController.FirstAttackAnimHash, 1);
                airSecondAttackState = new KnightAirAttackState(this, character.data.airSecondAttack, KnightCharacterController.SecondAttackAnimHash, 2);
                airDownwardStrikeState = new KnightDownwardStrikeState(this);
                fallAttackState = new KnightFallAttackState(this);
                
                // Setup air combo chain with flexible next state references
                airFirstAttackState.nextAirAttackState = airSecondAttackState;
                airSecondAttackState.nextAirAttackState = airDownwardStrikeState;
            }
            
            hurtState = new KnightHurtState(this);
            dieState = new KnightDieState(this);
            fakeWalkState = new KnightFakeWalkState(this);
            directionalDashState = new KnightDirectionalDashState(this);

            firstAttackState.nextAttackState = secondAttackState;
            secondAttackState.nextAttackState = firstAttackState;

            crouchAttackState.nextAttackState = crouchAttackState;

            EnterState(idleState);
        }

        public void Update()
        {
            currentState.Update();
            currentState.Transition();
        }

        public void PhysicsUpdate()
        {
            currentState.PhysicsUpdate();
        }

        public void EnterState(KnightStateBase state)
        {
            KnightStateBase previousState = currentState;
            currentState = state;
            previousState?.Exit();
            state.Enter(previousState);
        }

        public void EnterDefaultState()
        {
            if (!character.collisionChecker.isGrounded)
            {
                EnterState(fallState);
            }
            else if (character.horizontalMove == 0)
            {
                EnterState(idleState);
            }
            else
            {
                EnterState(runState);
            }
        }

        public bool TryEnterAttackState()
        {
            if (!character.attackAction.WasPerformedThisFrame())
                return false;
                
            // Ground attacks
            if (character.collisionChecker.isGrounded)
            {
                EnterState(KnightAttackState.StepAttack(character.data.attackComboMaxDelay) == 1 ? firstAttackState : secondAttackState);
                return true;
            }
            
            // Aerial attacks (if enabled)
            if (character.data.enableAerialCombat && !character.collisionChecker.isGrounded)
            {
                return TryEnterAerialAttackState();
            }
            
            return false;
        }
        
        public bool TryEnterAerialAttackState()
        {
            if (!character.data.enableAerialCombat)
                return false;
                
            // Regular air combo (solo X, senza crouch)
            if (currentAirAttacks == 0)
            {
                UnityEngine.Debug.Log("[AERIAL] Starting new air combo");
                StartAirCombo();
                EnterState(airFirstAttackState);
                return true;
            }
            else if (currentAirAttacks == 1 && isInAirCombo)
            {
                UnityEngine.Debug.Log("[AERIAL] Continuing to second air attack");
                currentAirAttacks++;
                EnterState(airSecondAttackState);
                return true;
            }
            else if (currentAirAttacks == 2 && isInAirCombo)
            {
                UnityEngine.Debug.Log("[AERIAL] Third attack - downward strike");
                currentAirAttacks++;
                EnterState(airDownwardStrikeState);
                return true;
            }
            
            UnityEngine.Debug.Log($"[AERIAL] No valid air attack state - currentAirAttacks: {currentAirAttacks}, isInAirCombo: {isInAirCombo}");
            return false;
        }
        
        public bool TryEnterHeavyAttackState()
        {
            if (!character.heavyAttackAction.WasPerformedThisFrame())
                return false;
                
            // Ground attack - Stoccata con 130% danno
            if (character.collisionChecker.isGrounded)
            {
                EnterState(thrustAttackState);
                return true;
            }
            
            // Air attack - Fall Attack (completamente separato dall'air combo)
            if (character.data.enableAerialCombat && !character.collisionChecker.isGrounded)
            {
                EnterState(fallAttackState);
                return true;
            }
            
            return false;
        }
        
        // Double Jump System Methods
        public void ResetJumps()
        {
            currentJumps = 0;
        }
        
        public bool TryConsumeJump()
        {
            if (currentJumps < character.data.maxJumps)
            {
                currentJumps++;
                return true;
            }
            return false;
        }
        
        // Aerial Combat System Methods
        public void StartAirCombo()
        {
            currentAirAttacks = 1;
            isInAirCombo = true;
        }
        
        public void ResetAirCombo()
        {
            currentAirAttacks = 0;
            isInAirCombo = false;
        }
        
        public void EndAirCombo()
        {
            isInAirCombo = false;
            // Don't reset currentAirAttacks immediately to prevent spam
        }
        
        public bool IsInAirCombo() => isInAirCombo;
        
        public void OnGroundedLanding()
        {
            // Reset both jump and air attack systems when landing
            ResetJumps();
            ResetAirCombo();
        }
    }
}
