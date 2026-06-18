local helper = CS.SkySwordKill.Next.Helper

local bridge = {}

local missing_host_reminder_setting_key = "longlive.bridge.enable_missing_host_reminder"
local required_handshake_version = 1
local required_capabilities = {
    "host-bootstrap",
    "next-runtime"
}
local bridge_i18n = nil

local function get_int(key, default_value)
    local value = helper.GetInt(key)
    if value == nil then
        return default_value
    end

    return value
end

local function get_str(key, default_value)
    local value = helper.GetStr(key)
    if value == nil or value == "" then
        return default_value
    end

    return value
end

local function normalize_locale(raw_locale)
    if raw_locale == nil or raw_locale == "" then
        return "en_us"
    end

    local normalized = string.lower(raw_locale)
    normalized = string.gsub(normalized, "-", "_")

    if normalized == "zh" or normalized == "zh_hans" or normalized == "zh_cn" then
        return "zh_cn"
    end

    return "en_us"
end

local function load_i18n_table()
    local locale = normalize_locale(get_str("longlive.current_locale", ""))
    local ok, catalog = pcall(require, "i18n/" .. locale)
    if ok and type(catalog) == "table" then
        return catalog
    end

    ok, catalog = pcall(require, "i18n/en_us")
    if ok and type(catalog) == "table" then
        return catalog
    end

    return {}
end

local function get_localized_text(key, default_value)
    if bridge_i18n == nil then
        bridge_i18n = load_i18n_table()
    end

    local localized = bridge_i18n[key]
    if localized == nil or localized == "" then
        return default_value
    end

    return localized
end

local function build_status_detail()
    local present = get_int("longlive.host.present", 0) == 1
    local version = get_str("longlive.host.version", "missing")
    local handshake_version = get_int("longlive.host.handshake_version", 0)
    local capabilities = get_str("longlive.host.capabilities", "")

    if present then
        return string.format(
            "present=true;version=%s;handshake=%d;capabilities=%s",
            version,
            handshake_version,
            capabilities)
    end

    return "present=false;version=missing;handshake=0;capabilities="
end

local function build_status_summary(compatible, reason)
    if compatible then
        return "ok"
    end

    if reason == "missing" then
        return "missing"
    end

    return "incompatible:" .. reason
end

local function split_capabilities(raw_capabilities)
    local result = {}
    if raw_capabilities == nil or raw_capabilities == "" then
        return result
    end

    for capability in string.gmatch(raw_capabilities, "([^,]+)") do
        result[capability] = true
    end

    return result
end

local function evaluate_compatibility()
    local present = get_int("longlive.host.present", 0) == 1
    if not present then
        return false, "missing", get_str("longlive.host.version", "missing"), required_handshake_version, ""
    end

    local host_version = get_str("longlive.host.version", "unknown")
    local handshake_version = get_int("longlive.host.handshake_version", 0)
    local raw_capabilities = get_str("longlive.host.capabilities", "")
    local capabilities = split_capabilities(raw_capabilities)

    if handshake_version < required_handshake_version then
        return false, "handshake", host_version, handshake_version, raw_capabilities
    end

    for _, capability in ipairs(required_capabilities) do
        if not capabilities[capability] then
            return false, "capability:" .. capability, host_version, handshake_version, raw_capabilities
        end
    end

    return true, "ok", host_version, handshake_version, raw_capabilities
end

local function is_missing_host_reminder_enabled(env)
    if env == nil then
        return true
    end

    return env:GetBoolSetting(missing_host_reminder_setting_key)
end

function bridge.enter_game(runner, env)
    local install_root = get_str("longlive.host.install_root", "unknown")
    local reminder_enabled = is_missing_host_reminder_enabled(env)
    local compatible, reason, version, handshake_version, raw_capabilities = evaluate_compatibility()
    local status_summary = build_status_summary(compatible, reason)
    local status_detail = build_status_detail()

    helper.SetStr("longlive.bridge.last_status", status_summary)
    helper.SetStr("longlive.bridge.last_status_detail", status_detail)
    helper.SetStr("longlive.bridge.last_host_version", version)
    helper.SetInt("longlive.bridge.last_host_present", get_int("longlive.host.present", 0) == 1 and 1 or 0)
    helper.SetInt("longlive.bridge.last_missing_host_reminder_enabled", reminder_enabled and 1 or 0)
    helper.SetInt("longlive.bridge.last_host_compatible", compatible and 1 or 0)
    helper.SetStr("longlive.bridge.last_host_compatibility_reason", reason)
    helper.SetInt("longlive.bridge.last_host_handshake_version", handshake_version)
    helper.SetStr("longlive.bridge.last_host_capabilities", raw_capabilities)

    if compatible then
        print(get_localized_text("bridge.log.host_compatible_prefix", "[LongLive.Bridge] host compatible: ") .. status_detail)
        return
    end

    print(
        get_localized_text("bridge.log.host_incompatible_prefix", "[LongLive.Bridge] host incompatible. ") ..
        "status=" .. status_summary ..
        ", version=" .. version ..
        ", handshake=" .. tostring(handshake_version) ..
        ", capabilities=" .. raw_capabilities ..
        ", install_root=" .. install_root)

    if reminder_enabled then
        if reason == "missing" then
            runner.ShowTip(get_localized_text("bridge.tip.host_missing", "LongLive Host is missing. Install LongLive Host into BepInEx/plugins."), 0)
        else
            runner.ShowTip(get_localized_text("bridge.tip.host_incompatible", "LongLive Host is installed, but this Bridge needs a newer compatible Host build."), 0)
        end
    end
end

function bridge.status_text(env)
    return get_str("longlive.bridge.last_status", "missing")
end

function bridge.status_detail_text(env)
    return get_str("longlive.bridge.last_status_detail", "present=false;version=missing;handshake=0;capabilities=")
end

return bridge
