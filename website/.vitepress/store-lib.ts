import fs from 'node:fs'
import path from 'node:path'
import { createMarkdownRenderer } from 'vitepress'

export const REPO = 'Deccoyi/macro-grid-plugin'
export const RELEASES_PAGE = `https://github.com/${REPO}/releases`
const REPO_BLOB = `https://github.com/${REPO}/blob/main`

export interface StoreRelease {
  version: string
  date: string
  prerelease: boolean
  notesUrl: string
  downloadUrl: string | null
}

export interface ChangelogGroup {
  /** The "###" heading of the group as written in CHANGELOG.md ("New", "Fixed", ...), or '' for loose items. */
  name: string
  html: string
}

export interface ChangelogEntry {
  version: string
  /** ISO date (YYYY-MM-DD) from the "## 0.2.1 - 2026-09-24" heading, or ''. */
  date: string
  groups: ChangelogGroup[]
}

export interface StorePlugin {
  id: string
  author: string
  name: string
  description: string
  category: string
  icon: string
  featured: boolean
  example: boolean
  kind: 'C#' | 'JavaScript'
  version: string
  prerelease: boolean
  /** The oldest Macro Grid the plugin runs on ("1.0.0"); older manifests only have minServerVersion, which stands in for it. */
  macroGrid: string
  permissions: string[]
  downloadUrl: string
  hasRelease: boolean
  releases: StoreRelease[]
  releaseDate: string
  whatItDoes: string
  changelog: ChangelogEntry[]
  sourceUrl: string
}

interface CatalogEntry {
  id: string
  dir: string
  category?: string
  icon?: string
  featured?: boolean
}

function repoRoot(): string {
  const srcDir: string | undefined = (globalThis as any).VITEPRESS_CONFIG?.srcDir
  return path.resolve(srcDir ?? process.cwd(), '..')
}

function readText(file: string): string {
  try {
    return fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n')
  } catch {
    return ''
  }
}

/** Fetches the releases; returns [] on any failure, so the build never breaks. */
async function fetchReleases(): Promise<any[]> {
  try {
    const headers: Record<string, string> = {
      Accept: 'application/vnd.github+json',
      'User-Agent': 'macro-grid-plugin-store',
    }
    if (process.env.GITHUB_TOKEN) headers.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`
    if (process.env.STORE_FORCE_FAIL) throw new Error('forced failure')
    const res = await fetch(`https://api.github.com/repos/${REPO}/releases?per_page=100`, {
      headers,
      signal: AbortSignal.timeout(20000),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    const json = await res.json()
    return Array.isArray(json) ? json : []
  } catch (e) {
    console.warn(
      `[store] Could not load GitHub releases (${(e as Error).message}); download buttons fall back to the Releases page.`,
    )
    return []
  }
}

/** The README intro: text between the "# " title and the first "## " heading, plus its first paragraph. */
function readmeParts(md: string): { first: string; intro: string } {
  const out: string[] = []
  let seenTitle = false
  for (const line of md.split('\n')) {
    if (!seenTitle && /^#\s/.test(line)) {
      seenTitle = true
      continue
    }
    if (/^##\s/.test(line)) break
    out.push(line)
  }
  const intro = out.join('\n').trim()
  const first = (intro.split(/\n\s*\n/)[0] ?? '').replace(/\n/g, ' ').trim()
  return { first, intro }
}

function plain(md: string): string {
  return md
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    .replace(/[`*_]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
}

function oneLine(text: string, max = 240): string {
  const p = plain(text)
  const first = p.match(/^.*?[.!?](\s|$)/)?.[0].trim() ?? p
  if (first.length <= max) return first
  return first.slice(0, max - 3).replace(/\s+\S*$/, '') + '...'
}

function cmpVersion(a: string, b: string): number {
  const pa = a.split('-')[0].split('.').map(Number)
  const pb = b.split('-')[0].split('.').map(Number)
  for (let i = 0; i < 3; i++) {
    const d = (pa[i] || 0) - (pb[i] || 0)
    if (d) return d
  }
  return 0
}

/** Drops changelog sections of versions that are newer than the latest published release (not released yet). */
function dropUnreleased(md: string, latest: string | undefined): string {
  if (!latest) return md
  const parts = md.split(/^(?=## )/m)
  return parts
    .filter((part) => {
      const m = part.match(/^## v?(\d+\.\d+\.\d+)/)
      return !m || cmpVersion(m[1], latest) <= 0
    })
    .join('')
}

let renderer: Awaited<ReturnType<typeof createMarkdownRenderer>> | undefined

async function render(src: string, dirOnGithub: string): Promise<string> {
  if (!src.trim()) return ''
  try {
    renderer ??= await createMarkdownRenderer(process.cwd(), {}, '/macro-grid-plugin/')
    const html = renderer.render(src)
    // Relative links point into the repository; make them absolute on GitHub.
    return html.replace(/href="([^"]+)"/g, (m, href: string) => {
      if (/^(https?:|mailto:|#|\/)/.test(href)) return m
      return `href="${new URL(href, `${REPO_BLOB}/${dirOnGithub}/`).href}"`
    })
  } catch {
    return ''
  }
}

/** Splits the CHANGELOG.md body ("## 0.2.1 - 2026-09-24" / "### New" / list) into versions and groups, each group rendered to HTML. */
async function parseChangelog(md: string, dirOnGithub: string): Promise<ChangelogEntry[]> {
  const entries: ChangelogEntry[] = []
  for (const part of md.split(/^(?=## )/m)) {
    const m = part.match(/^##\s+v?(\d+\.\d+\.\d+\S*)(?:\s*[-–—]\s*(\d{4}-\d{2}-\d{2}))?[^\n]*\n([\s\S]*)$/)
    if (!m) continue
    const groups: ChangelogGroup[] = []
    for (const g of m[3].split(/^(?=### )/m)) {
      const gm = g.match(/^###\s+([^\n]+)\n([\s\S]*)$/)
      const src = (gm ? gm[2] : g).trim()
      if (!src) continue
      groups.push({ name: gm ? gm[1].trim() : '', html: await render(src, dirOnGithub) })
    }
    entries.push({ version: m[1], date: m[2] ?? '', groups })
  }
  return entries
}

let cache: Promise<StorePlugin[]> | undefined

export function loadStore(): Promise<StorePlugin[]> {
  cache ??= build().catch((e) => {
    console.warn(`[store] Store data failed (${(e as Error).message}).`)
    return [] as StorePlugin[]
  })
  return cache
}

async function build(): Promise<StorePlugin[]> {
  const root = repoRoot()
  let catalog: CatalogEntry[] = []
  try {
    catalog = JSON.parse(readText(path.join(root, 'website', 'store', 'catalog.json')) || '[]')
  } catch (e) {
    console.warn(`[store] catalog.json is invalid (${(e as Error).message}).`)
  }
  // Every top-level folder with a plugin.json is listed; catalog.json only adds optional display details (category,
  // icon, featured, order). A new plugin therefore shows up in the Store without any extra step.
  const listed = new Set(catalog.map((e) => e.dir))
  for (const d of fs.readdirSync(root, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
    if (d.isDirectory() && !listed.has(d.name) && fs.existsSync(path.join(root, d.name, 'plugin.json'))) {
      catalog.push({ id: d.name.toLowerCase(), dir: d.name })
    }
  }
  const all = await fetchReleases()
  const published = all.filter((r) => r && !r.draft && typeof r.tag_name === 'string')
  const plugins: StorePlugin[] = []

  for (const entry of catalog) {
    try {
      const dir = path.join(root, entry.dir)
      let manifest: any = null
      try {
        manifest = JSON.parse(readText(path.join(dir, 'plugin.json')))
      } catch {
        /* handled below */
      }
      if (!manifest) {
        console.warn(`[store] ${entry.dir}/plugin.json is missing or invalid; skipped.`)
        continue
      }

      const id: string = manifest.id ?? entry.id
      const prefix = `plugin-${id}-v`
      const releases: StoreRelease[] = published
        .filter((r) => r.tag_name.startsWith(prefix))
        .sort((a, b) => Date.parse(b.published_at ?? b.created_at) - Date.parse(a.published_at ?? a.created_at))
        .map((r) => {
          const zip = (r.assets ?? []).find(
            (a: any) => typeof a.name === 'string' && a.name.toLowerCase().endsWith('.zip'),
          )
          return {
            version: r.tag_name.slice(prefix.length),
            date: String(r.published_at ?? r.created_at ?? '').slice(0, 10),
            prerelease: !!r.prerelease,
            notesUrl: r.html_url,
            downloadUrl: zip?.browser_download_url ?? null,
          }
        })

      const latest = releases[0]
      const readme = readmeParts(readText(path.join(dir, 'README.md')))
      const kindRaw = String(manifest.kind ?? '').toLowerCase()
      const changelogRaw = dropUnreleased(readText(path.join(dir, 'CHANGELOG.md'))
        .replace(/^#\s.*\n/, '')
        .replace(/^(?!##)[^\n]*CHANGELOG-developer[^\n]*$/gm, '')
        .replace(/^## Unreleased[ \t]*\n(?=\s*## |\s*$)/m, '')
        .trim(), latest?.version).trim()

      plugins.push({
        id,
        author: REPO.split('/')[0],
        name: manifest.name ?? id,
        description: manifest.description ? String(manifest.description) : oneLine(readme.first),
        category: entry.category ?? 'Other',
        icon: entry.icon ?? '',
        featured: !!entry.featured,
        example: entry.category === 'Examples',
        kind: kindRaw === 'csharp' ? 'C#' : 'JavaScript',
        version: latest?.version ?? String(manifest.version ?? ''),
        prerelease: latest ? latest.prerelease : true,
        macroGrid: String(manifest.macroGrid ?? manifest.minServerVersion ?? ''),
        permissions: Array.isArray(manifest.permissions) ? manifest.permissions.map(String) : [],
        downloadUrl: latest?.downloadUrl ?? RELEASES_PAGE,
        hasRelease: !!latest?.downloadUrl,
        releases: releases.slice(0, 4),
        releaseDate: latest?.date ?? '',
        whatItDoes: await render(readme.intro, entry.dir),
        changelog: await parseChangelog(changelogRaw, entry.dir),
        sourceUrl: `https://github.com/${REPO}/tree/main/${entry.dir}`,
      })
    } catch (e) {
      console.warn(`[store] ${entry.id}: ${(e as Error).message}; skipped.`)
    }
  }
  return plugins
}
