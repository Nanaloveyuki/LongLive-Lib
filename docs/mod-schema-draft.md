# Mod Schema Draft

This document describes the first JSON-mod direction for `LongLive`.

## 1. Design Goal

JSON mods are designed as declarative contribution packages.

They are not intended to become a second general-purpose scripting language.

The intended layering is:

- C# for host and capability implementation
- JSON for package metadata and declarative contributions
- Next's existing Lua layer remains the scripting layer when scripting is needed

## 2. Initial Scope

The first schema version focuses on:

- manifest metadata
- state-key declarations
- command declarations
- query declarations
- locale resources

It does not yet define:

- a full event-execution DSL
- embedded Lua
- a custom bytecode or expression language
- runtime installation behavior inside the schema itself

## 3. Suggested Package Layout

```text
MyJsonMod/
  manifest.json
  state-keys.json
  commands.json
  queries.json
  locales/
    en-US.json
    zh-CN.json
```

## 4. Manifest Example

```json
{
  "schemaVersion": 1,
  "id": "longlive.demo",
  "name": "LongLive Demo",
  "version": "0.1.0",
  "authors": ["miaom"],
  "description": "Demo JSON mod for LongLive.",
  "dependencies": [
    {
      "id": "io.github.nanaloveyuki.longlive-lib",
      "version": ">=0.1.0"
    }
  ],
  "entrypoints": {
    "commands": "commands.json",
    "queries": "queries.json",
    "stateKeys": "state-keys.json"
  },
  "locales": [
    "locales/en-US.json",
    "locales/zh-CN.json"
  ]
}
```

## 5. State-Key Example

```json
{
  "keys": [
    {
      "id": "longlive.demo.counter",
      "type": "int",
      "default": 0,
      "description": "Demo counter."
    },
    {
      "id": "longlive.demo.locale",
      "type": "string",
      "default": "en-US",
      "description": "Preferred locale."
    }
  ]
}
```

Supported state-key types in the current draft:

- `int`
- `string`

## 6. Command Example

```json
{
  "commands": [
    {
      "id": "LongLiveEcho",
      "handler": "longlive.echo",
      "backend": "builtin",
      "args": [
        {
          "name": "message",
          "type": "string",
          "index": 0,
          "default": ""
        }
      ],
      "options": {
        "writeStateKey": "longlive.last_error",
        "logInvocation": true
      }
    }
  ]
}
```

Supported command backends in the current draft:

- `builtin`
- `next-script`

Current implementation note:

- `builtin` is the only backend installed by the current code skeleton
- `next-script` is reserved in the schema but not installed yet

## 7. Query Example

```json
{
  "queries": [
    {
      "id": "LongLiveDebugEnabled",
      "handler": "longlive.state.int",
      "backend": "builtin",
      "options": {
        "key": "longlive.debug_enabled",
        "default": 0
      }
    }
  ]
}
```

## 8. Locale Example

```json
{
  "Menu.Start": "Start",
  "Menu.Quit": "Quit"
}
```

Locale files are expected to contain a single JSON object of string keys.

## 9. Validation Direction

The initial JSON-mod pipeline is designed to validate in three stages:

1. parse files
2. validate structure
3. validate semantics

Semantic validation should catch issues such as:

- duplicate ids
- unsupported backends
- unsupported state-key types
- mismatched default-value types
- invalid locale JSON

## 10. What Will Not Be Added Early

The following are intentionally deferred:

- a JSON-only control-flow DSL
- loops and branching as primary schema features
- embedded host object access rules inside JSON
- a second Lua runtime inside `LongLive`

When real scripting is needed, the preferred scripting path remains Next's existing Lua layer rather than embedding a second scripting system into this schema.

## 11. Capability Mapping Direction

The intended execution model is capability-based.

That means JSON declarations should map to stable handler ids such as:

- `longlive.echo`
- `longlive.state.int`

Instead of embedding raw host implementation details directly into the schema.
