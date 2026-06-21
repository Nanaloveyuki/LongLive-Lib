# LongLive Lib Docs Frontend

This directory contains the public VitePress site for `LongLive Lib`.

## Purpose

- `../docs/` stays as the repository-side source folder used during development.
- `./docs/` is the public-facing site tree.
- `./scripts/sync-docs.mjs` mirrors selected repository docs into the public site structure.

## Commands

```powershell
pnpm install
pnpm run dev
pnpm run build
```

## Deployment

The repository uses GitHub Pages for automatic deployment.

- pushing to `main` triggers a docs build
- the workflow installs dependencies from `frontend/`
- `pnpm run build` publishes `frontend/docs/.vitepress/dist`

The workflow file lives at:

- `../.github/workflows/docs-pages.yml`

## Language Layout

- Root pages are the default Simplified Chinese entry.
- `/en/` holds the English mirror.
- Detailed Chinese rewrites will be added gradually where wording matters for player-facing pages.

## Publishing Rule

- `../docs/` remains the internal source folder used during development.
- external readers should use the site built from `frontend/`.
