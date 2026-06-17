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
