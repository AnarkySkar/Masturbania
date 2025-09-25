# Aerial Combat System - Implementation Guide

## Overview
The Aerial Combat System adds advanced air-based combat mechanics to the Knight character, including air combos and fall attacks.

## Features Implemented

### 1. Air Attack Combo (3-Hit Chain)
- **Input**: Press X (Attack) up to 3 times while in air
- **Mechanics**:
  - First 2 attacks: Character hovers in air during attack
  - 3rd attack: Mandatory downward strike with increased damage
  - Must be chained within the combo time window
- **States**: `KnightAirAttackState`, `KnightDownwardStrikeState`

### 2. Fall Attack (Single Enhanced Attack)
- **Input**: Hold Crouch + Press X (Attack) while falling
- **Mechanics**:
  - Damage scales based on fall height
  - Enhanced damage if fallen from minimum height
  - Creates impact particles on landing
- **State**: `KnightFallAttackState`

## Configuration Parameters (KnightData.cs)

### Main Toggle
- `enableAerialCombat` (bool): Master switch for the entire system

### Air Combo Settings
- `airFirstAttack` (Attack): First air attack data
- `airSecondAttack` (Attack): Second air attack data  
- `airDownwardStrike` (Attack): Final downward strike data
- `airComboMaxDelay` (float): Time window to continue combo (default: 0.8s)
- `airAttackHoverDuration` (float): Hover time per attack (default: 0.3s)
- `downwardStrikeAcceleration` (float): Downward speed for final strike (default: 25f)
- `downwardStrikeDamageMultiplier` (float): Damage bonus for final strike (default: 1.8x)

### Fall Attack Settings
- `fallAttack` (Attack): Fall attack base data
- `minimumFallHeight` (float): Height threshold for bonus damage (default: 2f)
- `fallAttackDamageMultiplier` (float): Damage bonus for fall attack (default: 1.5x)

## How It Works

### State Machine Integration
The system integrates with the existing `KnightStateMachine` by:
1. Adding new aerial combat states
2. Extending `TryEnterAttackState()` to handle air attacks
3. Adding combo tracking variables (`currentAirAttacks`, `isInAirCombo`)

### Input Detection
- **Air Combo**: Normal attack input while airborne (not grounded)
- **Fall Attack**: `crouchAction.IsPressed() + attackAction.WasPerformedThisFrame()` while airborne

### Physics Integration
- **Hover System**: Temporarily reduces `gravityScale` and sets `linearVelocityY = 0`
- **Downward Strike**: Applies strong downward acceleration
- **Fall Attack**: Increases gravity scale for faster descent

## Testing

### Using AerialCombatTester.cs
1. Add the `AerialCombatTester` component to any GameObject in the scene
2. Enable `enableDebugLogs` for console output
3. Enable `enableGizmos` for visual indicators
4. The tester will automatically find and monitor the Knight character

### Test Scenarios
1. **Basic Air Combo**: Jump → X → X → X (3rd is automatic downward)
2. **Fall Attack**: Jump → Hold Down → X
3. **Combo Timeout**: Jump → X → (wait) → X (should not continue combo)
4. **Mixed Usage**: Jump → X → X → Land → Jump → Down+X

## Integration Points

### Existing Systems
- **Double Jump**: Compatible - air attacks consume same air time as double jumps
- **Wall Slide**: Air attacks can be initiated from wall slide
- **Collision Detection**: Uses existing `PerformAttack()` method
- **Animation System**: Reuses existing attack animations

### Input System
- Uses existing `InputReader` and `InputActions`
- Compatible with both keyboard and gamepad
- Leverages existing action mappings (Attack, Crouch)

## Troubleshooting

### Common Issues
1. **Air attacks not triggering**: Check `enableAerialCombat` is true in KnightData
2. **No hover effect**: Verify `airAttackHoverDuration` > 0
3. **Fall attack not working**: Ensure crouch input is held while pressing attack
4. **Combo not chaining**: Check `airComboMaxDelay` timing

### Debug Information
- Use `AerialCombatTester` for real-time system monitoring
- Check console logs for input detection and state transitions
- Verify attack data is properly configured in ScriptableObject

## Performance Considerations
- System only active when `enableAerialCombat` is true
- Minimal overhead when grounded (standard behavior)
- Physics modifications are temporary and restored after attacks
- No additional memory allocation during runtime

## Future Enhancements
Potential areas for expansion:
- Dedicated air attack animations
- Air dash integration
- Elemental air attacks
- Multi-directional air strikes
- Air combo multipliers