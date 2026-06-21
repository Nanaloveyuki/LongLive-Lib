import { cpSync, existsSync, mkdirSync, readdirSync, rmSync, statSync, writeFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'

const repoRoot = resolve(process.cwd(), '..')
const sourceRoot = join(repoRoot, 'docs')
const docsRoot = resolve(process.cwd(), 'docs')
const legacyGeneratedRoot = join(docsRoot, '_generated')

const copySpecs = [
  {
    source: 'deploy-guide.md',
    target: 'guide/deploy-guide.md',
  },
  {
    source: 'host-runtime-validation-checklist.md',
    target: 'guide/host-runtime-validation-checklist.md',
  },
  {
    source: 'item-use-optimization.md',
    target: 'guide/item-use-optimization.md',
  },
  {
    source: 'map-trace.md',
    target: 'guide/map-trace.md',
  },
  {
    source: 'mod-loader-usage.md',
    target: 'guide/mod-loader-usage.md',
  },
  {
    source: 'next-runtime-usage.md',
    target: 'guide/next-runtime-usage.md',
  },
  {
    source: 'next-runtime-examples.md',
    target: 'guide/next-runtime-examples.md',
  },
  {
    source: 'workshop-upload-checklist.md',
    target: 'workshop/upload-checklist.md',
  },
  {
    source: 'workshop-release-0.1.0.md',
    target: 'workshop/release-0.1.0.md',
  },
]

const managedTargets = [
  ...copySpecs.map(spec => spec.target),
  'workshop/overview.md',
  'en/workshop/upload-checklist.md',
  'en/workshop/release-0.1.0.md',
  'design/reference',
  'en/design/reference',
  'report/findings',
  'en/report/findings',
  'samples/library',
  'en/samples/library',
  'en/guide',
]

function ensureDir(path) {
  mkdirSync(path, { recursive: true })
}

function clearManagedTargets() {
  rmSync(legacyGeneratedRoot, { force: true, recursive: true })

  for (const target of managedTargets) {
    rmSync(join(docsRoot, target), { force: true, recursive: true })
  }
}

function copyFile(sourceRelativePath, targetRelativePath) {
  const sourcePath = join(sourceRoot, sourceRelativePath)
  const targetPath = join(docsRoot, targetRelativePath)
  if (!existsSync(sourcePath)) {
    throw new Error(`Missing docs source: ${relative(repoRoot, sourcePath)}`)
  }

  ensureDir(dirname(targetPath))
  cpSync(sourcePath, targetPath)
}

function copyFileToEnglishMirror(sourceRelativePath, targetRelativePath) {
  copyFile(sourceRelativePath, targetRelativePath)
  copyFile(sourceRelativePath, join('en', targetRelativePath).replace(/\\/g, '/'))
}

function copyDirectory(sourceRelativePath, targetRelativePath) {
  const sourcePath = join(sourceRoot, sourceRelativePath)
  const targetPath = join(docsRoot, targetRelativePath)
  if (!existsSync(sourcePath)) {
    throw new Error(`Missing docs source directory: ${relative(repoRoot, sourcePath)}`)
  }

  ensureDir(dirname(targetPath))
  cpSync(sourcePath, targetPath, { recursive: true })
}

function listMarkdownFiles(rootPath) {
  const results = []

  function walk(currentPath) {
    for (const entry of readdirSync(currentPath)) {
      const fullPath = join(currentPath, entry)
      const stats = statSync(fullPath)
      if (stats.isDirectory()) {
        walk(fullPath)
        continue
      }

      if (entry.toLowerCase().endsWith('.md')) {
        results.push(relative(rootPath, fullPath).replace(/\\/g, '/'))
      }
    }
  }

  walk(rootPath)
  return results.sort((left, right) => left.localeCompare(right))
}

function writeIndexFile(targetRelativePath, title, intro, entries) {
  const targetPath = join(docsRoot, targetRelativePath)
  ensureDir(dirname(targetPath))

  const lines = [`# ${title}`, '', intro, '']

  for (const entry of entries) {
    const label = entry.replace(/\.md$/i, '')
    lines.push(`- [${label}](./${entry})`)
  }

  lines.push('')
  writeFileSync(targetPath, lines.join('\n'), 'utf8')
}

clearManagedTargets()

for (const spec of copySpecs) {
  copyFileToEnglishMirror(spec.source, spec.target)
}

copyFile('workshop.md', 'workshop/overview.md')

copyDirectory('design', 'design/reference')
copyDirectory('design', 'en/design/reference')
copyDirectory('report', 'report/findings')
copyDirectory('report', 'en/report/findings')
copyDirectory('samples', 'samples/library')
copyDirectory('samples', 'en/samples/library')

writeIndexFile(
  'design/reference/index.md',
  '设计文档索引',
  '这里收录的是当前架构草稿、注册表规划、兼容层方向，以及运行时设计相关文档。',
  listMarkdownFiles(join(docsRoot, 'design', 'reference')).filter(entry => entry !== 'index.md'),
)

writeIndexFile(
  'en/design/reference/index.md',
  'Design Notes',
  'These documents track architecture drafts, registry planning, compatibility direction, and runtime design work.',
  listMarkdownFiles(join(docsRoot, 'en', 'design', 'reference')).filter(entry => entry !== 'index.md'),
)

writeIndexFile(
  'report/findings/index.md',
  '报告索引',
  '这里收录的是问题排查、回归记录、运行时观察，以及实机测试后的结论。',
  listMarkdownFiles(join(docsRoot, 'report', 'findings')).filter(entry => entry !== 'index.md'),
)

writeIndexFile(
  'en/report/findings/index.md',
  'Reports',
  'These documents record investigations, regressions, runtime findings, and conclusions from live testing.',
  listMarkdownFiles(join(docsRoot, 'en', 'report', 'findings')).filter(entry => entry !== 'index.md'),
)

writeIndexFile(
  'samples/library/index.md',
  '示例索引',
  '这里同步了仓库里的示例包和 bridge 演示内容，方便直接在文档站中浏览。',
  listMarkdownFiles(join(docsRoot, 'samples', 'library')).filter(entry => entry !== 'index.md'),
)

writeIndexFile(
  'en/samples/library/index.md',
  'Samples',
  'These sample packages and bridge demos are mirrored from the repository docs folder for convenient browsing.',
  listMarkdownFiles(join(docsRoot, 'en', 'samples', 'library')).filter(entry => entry !== 'index.md'),
)

console.log('Synced repository docs into frontend/docs managed routes')
