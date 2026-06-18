#[repr(C)]
pub struct LongLiveNativeDamageSegmentRequest {
    pub current_hp: i32,
    pub incoming_damage: i32,
    pub skill_id: i32,
    pub damage_type: i32,
    pub is_player_target: i32,
    pub is_multi_hit: i32,
    pub segment_index: i32,
}

#[repr(C)]
pub struct LongLiveNativeDamageSegmentDecision {
    pub applied_damage: i32,
    pub overflow_damage: i32,
    pub predicted_hp_after_segment: i32,
    pub flags: i32,
}

const FLAG_LETHAL: i32 = 1;
const FLAG_SKIP_ORIGINAL_DAMAGE: i32 = 1 << 1;
const FLAG_SKIP_REMAINING_SEGMENTS: i32 = 1 << 2;
const FLAG_CLAMP_RESULT_HP_TO_ZERO: i32 = 1 << 3;

#[unsafe(no_mangle)]
pub extern "C" fn longlive_native_core_abi_version() -> i32 {
    1
}

#[unsafe(no_mangle)]
pub extern "C" fn longlive_native_core_add(left: i32, right: i32) -> i32 {
    left + right
}

#[unsafe(no_mangle)]
pub extern "C" fn longlive_native_core_is_ready() -> i32 {
    1
}

#[unsafe(no_mangle)]
pub extern "C" fn longlive_native_core_compute_turn_damage(
    attack: i32,
    skill_power_percent: i32,
    flat_bonus: i32,
    defense: i32,
    reduction_percent: i32,
) -> i32 {
    let scaled_attack = attack.max(0) as i64 * skill_power_percent.max(0) as i64 / 100;
    let raw_damage = scaled_attack + flat_bonus.max(0) as i64;
    let reduced_damage = raw_damage - defense.max(0) as i64;
    let mitigated_damage = reduced_damage.max(1) * (100 - reduction_percent.clamp(0, 95)) as i64 / 100;
    mitigated_damage.max(1).min(i32::MAX as i64) as i32
}

#[unsafe(no_mangle)]
pub extern "C" fn longlive_native_core_adjudicate_damage_segment(
    request: LongLiveNativeDamageSegmentRequest,
) -> LongLiveNativeDamageSegmentDecision {
    let current_hp = request.current_hp.max(0) as i64;
    let incoming_damage = request.incoming_damage.max(0) as i64;

    if current_hp <= 0 {
        return LongLiveNativeDamageSegmentDecision {
            applied_damage: 0,
            overflow_damage: incoming_damage.min(i32::MAX as i64) as i32,
            predicted_hp_after_segment: 0,
            flags: FLAG_LETHAL | FLAG_SKIP_ORIGINAL_DAMAGE | FLAG_SKIP_REMAINING_SEGMENTS | FLAG_CLAMP_RESULT_HP_TO_ZERO,
        };
    }

    let predicted_hp = current_hp - incoming_damage;
    if predicted_hp >= 0 {
        return LongLiveNativeDamageSegmentDecision {
            applied_damage: incoming_damage.min(i32::MAX as i64) as i32,
            overflow_damage: 0,
            predicted_hp_after_segment: predicted_hp.min(i32::MAX as i64) as i32,
            flags: 0,
        };
    }

    let applied_damage = current_hp.min(i32::MAX as i64) as i32;
    let overflow_damage = (incoming_damage - current_hp).min(i32::MAX as i64) as i32;

    LongLiveNativeDamageSegmentDecision {
        applied_damage,
        overflow_damage,
        predicted_hp_after_segment: 0,
        flags: FLAG_LETHAL | FLAG_SKIP_ORIGINAL_DAMAGE | FLAG_SKIP_REMAINING_SEGMENTS | FLAG_CLAMP_RESULT_HP_TO_ZERO,
    }
}
