using Metroidvania.Events;
using System;
using UnityEngine;

namespace Metroidvania.Characters.Knight
{
    [CreateAssetMenu(fileName = "KnightData", menuName = "Scriptables/Characters/Knight Data")]
    public class KnightData : ScriptableObject
    {
        [Serializable]
        public class Attack
        {
#if UNITY_EDITOR
            public bool drawGizmos;
#endif

            [Space]
            public float duration;

            public float horizontalMoveOffset;

            [Space]
            public float triggerTime;

            public float attackEndOffset;

            public Rect triggerCollider;

            [Space]
            public int damage;
            public float force;
        }

        [Serializable]
        public class ColliderBounds
        {
#if UNITY_EDITOR
            public bool drawGizmos;
#endif
            public Rect bounds;
        }

        [Header("Properties")]
        public CharacterAttributeData<float> lifeAttributeData;

        [Header("Events")]
        public ObjectEventChannel onDieChannel;
        public CharacterHurtEventChannel onHurtChannel;

        [Header("Ground Check")]
        public LayerMask groundLayer;

        [Header("Movement")]
        public float moveSpeed;
        public Rigidbody2D.SlideMovement slideMovement;
        public float airMoveSpeed;

        [Header("Jump")]
        public float jumpHeight;
        public float jumpFallMultiplier;
        public float jumpLowMultiplier;
        public float jumpCoyoteTime;
        
        [Header("Double Jump")]
        public bool enableDoubleJump = true;
        public float doubleJumpHeight = 15f;
        public int maxJumps = 2;

        [Header("Fall")]
        public float fallParticlesDistance;

        [Header("Crouch")]
        public float crouchWalkSpeed;
        public float crouchTransitionTime;

        [Header("Slide")]
        public float slideDuration;
        public float slideSpeed;
        public float slideCooldown;
        public AnimationCurve slideMoveCurve;
        public float slideTransitionTime;

        [Header("Roll")]
        public float rollDuration;
        public float rollSpeed;
        public float rollCooldown;
        public AnimationCurve rollHorizontalMoveCurve;
        
        [Header("Directional Dash")]
        public bool enableDirectionalDash = true;
        public float dashSpeed = 20f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 2f;
        public AnimationCurve dashCurve;

        [Header("Attacks")]
        public LayerMask hittableLayer;
        public float attackComboMaxDelay;
        public Attack firstAttack;
        public Attack secondAttack;
        public Attack crouchAttack;

        [Header("Aerial Combat")]
        [Space(5)]
        [Tooltip("Enable air attack combo system")]
        public bool enableAerialCombat = true;
        
        [Space]
        [Tooltip("First air attack in combo")]
        public Attack airFirstAttack;
        
        [Tooltip("Second air attack in combo")]
        public Attack airSecondAttack;
        
        [Tooltip("Final downward strike attack (mandatory 3rd attack)")]
        public Attack airDownwardStrike;
        
        [Space]
        [Tooltip("Time window to continue air combo")]
        public float airComboMaxDelay = 0.8f;
        
        [Tooltip("Duration to hover in air during each air attack")]
        public float airAttackHoverDuration = 0.3f;
        
        [Tooltip("Downward acceleration force for final strike")]
        public float downwardStrikeAcceleration = 25f;
        
        [Tooltip("Damage multiplier for downward strike")]
        public float downwardStrikeDamageMultiplier = 1.8f;
        
        [Space]
        [Tooltip("Fall attack when pressing Down + Attack")]
        public Attack fallAttack;
        
        [Tooltip("Minimum fall height to trigger enhanced fall attack")]
        public float minimumFallHeight = 2f;
        
        [Tooltip("Fall attack damage multiplier")]
        public float fallAttackDamageMultiplier = 1.5f;

        [Header("Wall Abilities")]
        public float wallSlideSpeed;
        public Vector2 wallJumpForce;
        public float wallJumpDuration;

        [Header("Hurt")]
        public float hurtTime;

        [Header("Fake Walk")]
        public float fakeWalkOnSceneTransitionTime;

        [Header("Invincibility")]
        public float invincibilityAlphaChange;
        public float invincibilityFadeSpeed;
        public float defaultInvincibilityTime;

        [Header("Colliders")]
        public ColliderBounds standColliderBounds;
        public ColliderBounds crouchColliderBounds;
        public Rect crouchHeadRect;
    }
}
