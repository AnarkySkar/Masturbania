using UnityEngine;
using Metroidvania.Characters.Knight;

namespace Metroidvania.Testing
{
    /// <summary>
    /// Test script for the new Aerial Combat System
    /// Attach to any GameObject to test aerial combat mechanics in the scene
    /// </summary>
    public class AerialCombatTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enableGizmos = true;
        
        [Header("Test Status")]
        [SerializeField] private bool systemEnabled = false;
        [SerializeField] private int currentAirAttacks = 0;
        [SerializeField] private bool isInAirCombo = false;
        [SerializeField] private bool isGrounded = false;
        
        private KnightCharacterController knightController;
        private KnightStateMachine stateMachine;
        
        void Start()
        {
            // Find the Knight in the scene
            knightController = FindObjectOfType<KnightCharacterController>();
            
            if (knightController == null)
            {
                Debug.LogError("[AerialCombatTester] No KnightCharacterController found in scene!");
                enabled = false;
                return;
            }
            
            stateMachine = knightController.stateMachine;
            systemEnabled = knightController.data.enableAerialCombat;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AerialCombatTester] Aerial Combat System: {(systemEnabled ? "ENABLED" : "DISABLED")}");
                LogSystemParameters();
            }
        }
        
        void Update()
        {
            if (knightController == null || stateMachine == null) return;
            
            // Update test status
            UpdateTestStatus();
            
            // Test input combinations
            TestInputs();
        }
        
        private void UpdateTestStatus()
        {
            isGrounded = knightController.collisionChecker.isGrounded;
            isInAirCombo = stateMachine.IsInAirCombo();
            currentAirAttacks = 3 - stateMachine.airAttacksRemaining;
        }
        
        private void TestInputs()
        {
            if (!enableDebugLogs) return;
            
            // Log air attack attempts
            if (knightController.attackAction.WasPerformedThisFrame() && !isGrounded)
            {
                bool crouchPressed = knightController.crouchAction.IsPressed();
                Debug.Log($"[AerialCombatTester] Air Attack Input - Crouch: {crouchPressed}, Air Attacks: {currentAirAttacks}/3, In Combo: {isInAirCombo}");
                
                if (crouchPressed)
                {
                    Debug.Log("[AerialCombatTester] Fall Attack detected (Crouch + Attack)");
                }
                else if (currentAirAttacks == 0)
                {
                    Debug.Log("[AerialCombatTester] Starting air combo");
                }
                else if (currentAirAttacks < 3 && isInAirCombo)
                {
                    Debug.Log($"[AerialCombatTester] Continuing air combo - Attack {currentAirAttacks + 1}");
                }
            }
            
            // Log state changes
            string currentStateName = stateMachine.currentState.GetType().Name;
            if (currentStateName.Contains("Air") || currentStateName.Contains("Fall") || currentStateName.Contains("Downward"))
            {
                Debug.Log($"[AerialCombatTester] Current State: {currentStateName}");
            }
        }
        
        private void LogSystemParameters()
        {
            var data = knightController.data;
            Debug.Log($"[AerialCombatTester] System Parameters:");
            Debug.Log($"  - Air Combo Max Delay: {data.airComboMaxDelay}s");
            Debug.Log($"  - Air Attack Hover Duration: {data.airAttackHoverDuration}s");
            Debug.Log($"  - Downward Strike Acceleration: {data.downwardStrikeAcceleration}");
            Debug.Log($"  - Downward Strike Damage Multiplier: {data.downwardStrikeDamageMultiplier}x");
            Debug.Log($"  - Fall Attack Damage Multiplier: {data.fallAttackDamageMultiplier}x");
            Debug.Log($"  - Minimum Fall Height: {data.minimumFallHeight}");
        }
        
        void OnGUI()
        {
            if (!enableGizmos || knightController == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            GUILayout.Label("=== AERIAL COMBAT TESTER ===");
            GUILayout.Label($"System Enabled: {systemEnabled}");
            GUILayout.Label($"Grounded: {isGrounded}");
            GUILayout.Label($"Current State: {stateMachine.currentState.GetType().Name}");
            GUILayout.Label($"Air Attacks: {currentAirAttacks}/3");
            GUILayout.Label($"In Air Combo: {isInAirCombo}");
            GUILayout.Label($"Jumps Remaining: {stateMachine.jumpsRemaining}");
            
            GUILayout.Space(10);
            GUILayout.Label("CONTROLS:");
            GUILayout.Label("X = Attack (in air for combo)");
            GUILayout.Label("Down + X = Fall Attack");
            GUILayout.Label("Z = Jump/Double Jump");
            
            GUILayout.EndArea();
        }
        
        void OnDrawGizmosSelected()
        {
            if (!enableGizmos || knightController == null) return;
            
            // Draw attack ranges for aerial attacks
            Vector3 pos = knightController.transform.position;
            
            // Air attack range (reusing existing attack data)
            Gizmos.color = Color.cyan;
            if (knightController.data.airFirstAttack.triggerCollider != default)
            {
                Vector3 size = new Vector3(knightController.data.airFirstAttack.triggerCollider.width, 
                                         knightController.data.airFirstAttack.triggerCollider.height, 1);
                Gizmos.DrawWireCube(pos, size);
            }
            
            // Fall attack detection height
            Gizmos.color = Color.red;
            Vector3 fallStart = pos + Vector3.up * knightController.data.minimumFallHeight;
            Gizmos.DrawLine(pos, fallStart);
            Gizmos.DrawWireSphere(fallStart, 0.2f);
        }
    }
}