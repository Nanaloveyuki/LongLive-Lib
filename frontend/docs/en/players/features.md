# Features

These are the changes players are most likely to notice right away.

## Bulk item use feels smoother

Using very large batches of consumables can freeze the game for a while.

LongLive Lib now spreads that work across frames and tries to keep the game responsive.

This path currently includes:

- quantity selection from long right-click
- quantity selection from middle click
- merged summaries instead of a wall of repeated prompts

## Pop-tip spam is reduced

When the game throws too many prompts in a short time, the right side of the screen can become unreadable.

LongLive Lib tries to merge repeated prompts and clear overloaded prompt queues more quickly, so the screen stays readable.

## Battle-side overkill is guarded

Some high-damage, multi-hit battles keep processing extra damage after the target is already effectively dead.

That can lead to frame drops, stacked audio, and other ugly behavior.

LongLive Lib now adds a guard layer to reduce those useless follow-up calculations.

## Some fade-heavy transitions are faster

Selected black-screen and scene-transition paths are accelerated so repeated travel feels less sluggish.

## TuJian search now supports pinyin

The TuJian search path now supports pinyin input.

If you do not want to type the original term directly, this makes the search much easier to use.

## The main-menu entry also helps players

The `LongLive Lib` entry in the main menu is not only for development.

It also helps you confirm whether the mod and its host layer actually loaded.

## Things worth knowing

- the project is still under active development
- slightly longer startup is normal
- using it together with `Next` is still the safer option for compatibility
- most optimization switches can be disabled from the `F1` config menu
