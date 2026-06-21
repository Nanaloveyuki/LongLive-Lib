# Content Schema Draft

This document describes the first content-oriented JSON-mod direction for `LongLive`.

It builds on top of the general schema rules already defined in `mod-schema-draft.md`.

## 1. Design Principle

The content schema should describe contributed game content without hardwiring the full game runtime into the schema itself.

The goal is to model content packages such as:

- items
- skills
- buffs
- asset mappings
- future story/event resources

The schema should stay declarative.

## 2. Why This Is Separate From Commands and Queries

Commands and queries describe runtime extension points.

Items, skills, buffs, and asset mappings describe contributed content.

These are different problem domains and should not be merged into one giant JSON file.

## 3. Current Content Entrypoints

The manifest can now reserve these content entrypoints under `entrypoints.content`:

- `items`
- `skills`
- `buffs`
- `assets`

## 4. Item Example

```json
{
  "items": [
    {
      "id": 900001,
      "name": "Demo Spirit Pill",
      "description": "A simple demo item.",
      "info": "Restores a small amount of spirit.",
      "icon": 101,
      "itemType": 5,
      "quality": 1,
      "phase": 1,
      "maxStack": 99,
      "price": 120,
      "seid": [1001],
      "affix": [],
      "flags": []
    }
  ]
}
```

## 5. Skill Example

```json
{
  "skills": [
    {
      "id": 910001,
      "skillPkId": 910001,
      "name": "Demo Sword Light",
      "description": "A simple demo attack skill.",
      "guideDescription": "Demo skill guide text.",
      "icon": 201,
      "quality": 1,
      "phase": 1,
      "baseDamage": 12,
      "attackScript": "SkillAttack",
      "battle": true,
      "learnLevel": 1,
      "learnCostMonth": 1,
      "seid": [2001],
      "affix": [],
      "costTypes": [1],
      "costValues": [2]
    }
  ]
}
```

## 6. Buff Example

```json
{
  "buffs": [
    {
      "id": 920001,
      "name": "Demo Resolve",
      "description": "A simple demo buff.",
      "icon": 301,
      "buffType": 1,
      "trigger": 1,
      "removeTrigger": 0,
      "seid": [3001],
      "affix": [],
      "hidden": false
    }
  ]
}
```

## 7. Asset Mapping Example

```json
{
  "assets": [
    {
      "id": "demo-npc-portrait",
      "kind": "portrait",
      "target": "npc:900001",
      "source": "assets/portraits/demo-npc.png",
      "mode": "replace"
    }
  ]
}
```

## 8. Relationship To Next

This draft borrows high-level organization ideas from Next's item, skill, and buff data models.

It does not attempt to mirror every existing field or every editor-facing property.

That is intentional.

`LongLive` is a library boundary, not a full clone of Next's upper content editor model.

## 9. What Stays Out For Now

The following remain deferred:

- full story event content schema
- full NPC schema
- complete battle formula schema
- direct asset-bundle authoring workflow
- execution semantics for every item or skill field

The first goal is a stable content-declaration shape, not total runtime completeness.

## 10. Current Installation State

The current codebase now routes item, skill, buff, and asset declarations through a content installation registry.

However, the default registry is still intentionally non-invasive:

- command and query entries can install into the current runtime facade
- content entries are reported as deferred by default

This keeps the package format and installation pipeline stable before real runtime injection behavior is chosen.
