# Aerial Combat System - Test Checklist

## Pre-Test Setup
- [ ] Open the main scene with the Knight character
- [ ] Add the `AerialCombatTester` component to any GameObject in the scene
- [ ] Enable `enableDebugLogs` and `enableGizmos` in the tester
- [ ] Locate the Knight's KnightData ScriptableObject in the project
- [ ] Verify `enableAerialCombat` is set to `true`

## Basic Functionality Tests

### 1. Air Combo System
- [ ] **Test 1**: Jump and press X once
  - Expected: Knight hovers briefly, performs air attack, then falls normally
  - Watch for: Hover effect, attack animation, console log "Starting air combo"

- [ ] **Test 2**: Jump and press X twice quickly
  - Expected: First attack → hover → second attack → hover → falls
  - Watch for: Two distinct hover phases, combo continuation log

- [ ] **Test 3**: Jump and press X three times quickly
  - Expected: Two normal attacks → automatic downward strike → fast descent → impact
  - Watch for: Automatic 3rd attack, increased falling speed, landing particles

- [ ] **Test 4**: Jump, press X, wait too long, press X again
  - Expected: First attack → timeout → fall normally, second X does fall attack
  - Watch for: "Combo ended" log, fall state transition

### 2. Fall Attack System  
- [ ] **Test 5**: Jump, hold Down arrow, press X
  - Expected: Fast downward attack with enhanced damage
  - Watch for: "Fall Attack detected" log, increased gravity

- [ ] **Test 6**: Jump from high platform, Down+X
  - Expected: Enhanced damage due to fall height
  - Watch for: Impact particles, damage scaling message

### 3. Edge Cases
- [ ] **Test 7**: Try air attack while grounded
  - Expected: Normal ground attack (system should not interfere)
  
- [ ] **Test 8**: Jump, air attack, land, jump again, air attack
  - Expected: Each jump allows new air combo
  - Watch for: Proper reset between jumps

- [ ] **Test 9**: Jump, air attack, wall slide, air attack
  - Expected: Can perform air attacks from wall slide
  
- [ ] **Test 10**: Double jump + air attacks
  - Expected: Compatible with double jump system

## Parameter Validation Tests

### Hover Duration
- [ ] Set `airAttackHoverDuration` to 0.1f, test if hover is very brief
- [ ] Set `airAttackHoverDuration` to 1.0f, test if hover lasts longer

### Damage Multipliers  
- [ ] Set `downwardStrikeDamageMultiplier` to 3.0f, verify final attack does more damage
- [ ] Set `fallAttackDamageMultiplier` to 2.0f, verify fall attack damage increase

### Timing Windows
- [ ] Set `airComboMaxDelay` to 0.1f, test if combo window becomes very tight
- [ ] Set `airComboMaxDelay` to 2.0f, test if combo window becomes very generous

## Visual/Audio Feedback Tests
- [ ] Verify attack animations play correctly for each air attack
- [ ] Check that landing particles appear after downward strike
- [ ] Confirm hover effect is visually apparent (character stops falling briefly)
- [ ] Test that character properly flips direction during air attacks

## Integration Tests
- [ ] **With Double Jump**: Jump → Double Jump → Air Combo
- [ ] **With Wall Slide**: Wall slide → Jump off wall → Air attacks
- [ ] **With Dash**: Ground dash → Jump → Air attacks
- [ ] **Combat Integration**: Air attacks should damage enemies if present

## Performance Tests
- [ ] Rapid air attack usage - no memory leaks or performance drops
- [ ] System disabled (`enableAerialCombat = false`) - no overhead
- [ ] Multiple characters in scene (if applicable) - no interference

## Expected Debug Output Examples
```
[AerialCombatTester] Aerial Combat System: ENABLED
[AerialCombatTester] Air Attack Input - Crouch: False, Air Attacks: 0/3, In Combo: False  
[AerialCombatTester] Starting air combo
[AerialCombatTester] Current State: KnightAirAttackState
[AerialCombatTester] Continuing air combo - Attack 2
[AerialCombatTester] Current State: KnightDownwardStrikeState
```

## Troubleshooting Guide

### If air attacks don't trigger:
1. Check KnightData has `enableAerialCombat = true`
2. Verify Knight is actually airborne (not grounded)
3. Confirm attack input is being detected

### If hover doesn't work:
1. Check `airAttackHoverDuration > 0`
2. Verify gravity is being modified (check rb.gravityScale)
3. Look for physics conflicts with other systems

### If combo doesn't chain:
1. Verify timing is within `airComboMaxDelay`
2. Check that `isInAirCombo` flag is properly set
3. Confirm attack inputs are registered consecutively

## Success Criteria
- [ ] All basic functionality tests pass
- [ ] No console errors during any test
- [ ] Smooth visual transitions between states
- [ ] Proper integration with existing movement systems
- [ ] Parameters can be adjusted and take effect immediately
- [ ] System can be completely disabled without side effects

## Post-Test Configuration
Once testing is complete:
- [ ] Remove or disable `AerialCombatTester` component for production
- [ ] Fine-tune parameters based on gameplay feel
- [ ] Document any custom parameter combinations used
- [ ] Test with final enemy configurations if applicable