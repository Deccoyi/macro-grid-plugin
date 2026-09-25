<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'
import { useStoreText } from './store-i18n'

const props = defineProps<{ plugin?: StorePlugin }>()

const { t, prefix, category, fmtDate } = useStoreText()

const p = computed(() => props.plugin)
const iconUrl = computed(() => (p.value?.icon ? withBase(`/store/icons/${p.value.icon}.svg`) : ''))
const letter = computed(() => (p.value?.name.trim()[0] ?? '?').toUpperCase())
const RELEASES = 'https://github.com/Deccoyi/macro-grid-plugin/releases'
const isCs = computed(() => p.value?.kind === 'C#')

// Sections: overview, versions and, when the plugin has a changelog, the changelog. Reflected in the URL hash.
type TabId = 'overview' | 'versions' | 'changelog'
const tab = ref<TabId>('overview')
const tabs = computed(() => {
  const list: { id: TabId; label: string; count?: number }[] = [
    { id: 'overview', label: t.value.tabOverview },
    { id: 'versions', label: t.value.versions, count: p.value?.releases.length },
  ]
  if (p.value?.changelog.length) list.push({ id: 'changelog', label: t.value.changelog })
  return list
})
// A long changelog is shown in steps so the page stays light.
const CL_STEP = 8
const clShown = ref(CL_STEP)
const clEntries = computed(() => (p.value?.changelog ?? []).slice(0, clShown.value))
const clMore = computed(() => Math.max(0, (p.value?.changelog.length ?? 0) - clShown.value))

function pick(id: TabId) {
  tab.value = id
  history.replaceState(null, '', `${location.pathname}${location.search}${id === 'overview' ? '' : `#${id}`}`)
}
onMounted(() => {
  const id = location.hash.slice(1)
  if (tabs.value.some((x) => x.id === id)) tab.value = id as TabId
})

const stats = computed(() => {
  const x = p.value
  if (!x) return []
  return [
    { v: x.version, l: t.value.statVersion },
    { v: x.kind, l: t.value.statKind },
    { v: x.macroGrid ? `${x.macroGrid}+` : '-', l: t.value.statServer },
    isCs.value
      ? { v: t.value.statFull, l: t.value.statAccess }
      : { v: String(x.permissions.length), l: t.value.statPermissions },
  ]
})

const details = computed(() => {
  const x = p.value
  if (!x) return []
  const rows: { k: string; v: string; mono?: boolean }[] = [{ k: t.value.statVersion, v: x.version }]
  if (x.releaseDate) rows.push({ k: t.value.detailRelease, v: fmtDate(x.releaseDate) })
  rows.push({ k: t.value.detailId, v: x.id, mono: true }, { k: t.value.detailType, v: isCs.value ? t.value.kindCs : t.value.kindJs })
  if (x.macroGrid) rows.push({ k: t.value.detailServer, v: `${x.macroGrid} ${t.value.orNewer}` })
  return rows
})

const requirements = computed(() =>
  p.value?.macroGrid ? [`${t.value.server} ${p.value.macroGrid} ${t.value.orNewer}`] : [],
)

function permDesc(name: string): string {
  if (name === 'variables') return t.value.permVariables
  if (name === 'actions') return t.value.permActions
  if (name === 'input') return t.value.permInput
  if (name.startsWith('http:')) return `${t.value.permHttp} ${name.slice(5)}`
  return ''
}

function groupLabel(name: string): string {
  return t.value.groupNames[name.toLowerCase()] ?? name
}
function groupKind(name: string): string {
  const n = name.toLowerCase()
  if (n === 'new' || n === 'added') return 'is-new'
  if (n === 'fixed') return 'is-fix'
  if (n === 'removed' || n === 'security') return 'is-warn'
  return 'is-chg'
}
</script>

<template>
  <div v-if="p" class="ps-detail">
    <nav class="ps-crumbs" aria-label="Breadcrumb">
      <a :href="withBase(`${prefix}/store/`)">{{ t.store }}</a>
      <span class="ps-crumb-sep" aria-hidden="true">/</span>
      <span>{{ category(p.category) }}</span>
      <span class="ps-crumb-sep" aria-hidden="true">/</span>
      <span class="ps-crumb-now" aria-current="page">{{ p.name }}</span>
    </nav>

    <div class="ps-layout">
      <div class="ps-main">
        <header class="ps-detail-head">
          <span class="ps-icon ps-icon-big" aria-hidden="true">
            <span v-if="iconUrl" class="ps-icon-mask" :style="{ '--ps-icon': `url(${iconUrl})` }"></span>
            <span v-else class="ps-icon-letter">{{ letter }}</span>
          </span>
          <div class="ps-detail-title">
            <div class="ps-title-row">
              <h1>{{ p.name }}</h1>
              <span v-if="p.example" class="ps-badge ps-badge-example">{{ t.example }}</span>
              <span v-if="p.prerelease" class="ps-badge ps-badge-alpha">{{ t.alpha }}</span>
            </div>
            <p class="ps-by">
              <a :href="`https://github.com/${p.author}`">{{ p.author }}</a> &middot; {{ category(p.category) }}
            </p>
            <p class="ps-tagline">{{ p.description }}</p>
          </div>
        </header>

        <dl class="ps-stats">
          <div v-for="s in stats" :key="s.l" class="ps-stat">
            <dt>{{ s.l }}</dt>
            <dd>{{ s.v }}</dd>
          </div>
        </dl>

        <div class="ps-tabs" role="tablist" :aria-label="t.tabsLabel">
          <button
            v-for="x in tabs"
            :key="x.id"
            type="button"
            role="tab"
            class="ps-tab"
            :class="{ active: tab === x.id }"
            :aria-selected="tab === x.id"
            @click="pick(x.id)"
          >
            {{ x.label }}<span v-if="x.count" class="ps-tab-count">{{ x.count }}</span>
          </button>
        </div>

        <div v-if="tab === 'overview'" class="ps-panel" role="tabpanel">
          <section v-if="p.whatItDoes">
            <h2>{{ t.whatItDoes }}</h2>
            <div class="vp-doc ps-rich" v-html="p.whatItDoes"></div>
          </section>

          <section v-if="requirements.length">
            <h2>{{ t.requirements }}</h2>
            <ul class="ps-checks">
              <li v-for="r in requirements" :key="r">
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="20 6 9 17 4 12" /></svg>
                <span>{{ r }}</span>
              </li>
            </ul>
          </section>

          <section v-if="!isCs">
            <h2>{{ t.permissions }}</h2>
            <p class="ps-muted">{{ t.permissionsIntro }}</p>
            <ul v-if="p.permissions.length" class="ps-perms">
              <li v-for="x in p.permissions" :key="x">
                <code class="ps-perm">{{ x }}</code>
                <span>{{ permDesc(x) }}</span>
              </li>
            </ul>
          </section>

          <aside v-else class="ps-note">
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" /></svg>
            <span>{{ t.csTrust }}</span>
          </aside>
        </div>

        <div v-else-if="tab === 'versions'" class="ps-panel" role="tabpanel">
          <h2>{{ t.versions }}</h2>
          <ul v-if="p.releases.length" class="ps-releases">
            <li v-for="(r, i) in p.releases" :key="r.version">
              <strong class="ps-rel-ver">v{{ r.version }}</strong>
              <span v-if="i === 0" class="ps-badge ps-badge-brand">{{ t.latest }}</span>
              <span v-if="r.prerelease" class="ps-badge ps-badge-alpha">{{ t.alpha }}</span>
              <span class="ps-rel-date">{{ fmtDate(r.date) }}</span>
              <a class="ps-rel-link" :href="r.notesUrl">{{ t.releaseNotes }}</a>
              <a v-if="r.downloadUrl" class="ps-btn ps-btn-sm" :class="{ 'ps-btn-primary': i === 0 }" :href="r.downloadUrl">{{ t.download }}</a>
            </li>
          </ul>
          <p v-else class="ps-muted">{{ t.noReleaseInfo }}</p>
          <p class="ps-muted"><a :href="RELEASES">{{ t.olderVersions }}</a></p>
        </div>

        <div v-else class="ps-panel" role="tabpanel">
          <h2>{{ t.changelog }}</h2>
          <ol class="ps-timeline">
            <li v-for="(e, i) in clEntries" :key="e.version">
              <details class="ps-cl" :open="i === 0">
                <summary class="ps-cl-head">
                  <strong class="ps-cl-ver">{{ e.version }}</strong>
                  <span v-if="i === 0" class="ps-badge ps-badge-brand">{{ t.latest }}</span>
                  <span class="ps-cl-date">{{ fmtDate(e.date) }}</span>
                  <span class="ps-cl-tags">
                    <span v-for="g in e.groups.filter((x) => x.name)" :key="g.name" class="ps-cl-tag" :class="groupKind(g.name)">{{ groupLabel(g.name) }}</span>
                  </span>
                  <svg class="ps-cl-chev" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="6 9 12 15 18 9" /></svg>
                </summary>
                <div class="ps-cl-body">
                  <section v-for="(g, j) in e.groups" :key="j" class="ps-cl-group">
                    <h3 v-if="g.name" class="ps-cl-label" :class="groupKind(g.name)">{{ groupLabel(g.name) }}</h3>
                    <div class="vp-doc ps-rich" v-html="g.html"></div>
                  </section>
                </div>
              </details>
            </li>
          </ol>
          <button v-if="clMore" type="button" class="ps-btn ps-more" @click="clShown += CL_STEP">{{ t.showMore }} ({{ clMore }})</button>
        </div>
      </div>

      <aside class="ps-aside">
        <div class="ps-card ps-cta">
          <a class="ps-btn ps-btn-primary ps-btn-big ps-btn-block" :href="p.downloadUrl">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><polyline points="7 10 12 15 17 10" /><line x1="12" y1="15" x2="12" y2="3" /></svg>
            {{ p.hasRelease ? `${t.download} v${p.version}` : t.getOnGithub }}
          </a>
          <a class="ps-btn ps-btn-block" :href="p.sourceUrl">
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="16 18 22 12 16 6" /><polyline points="8 6 2 12 8 18" /></svg>
            {{ t.sourceCode }}
          </a>
          <p class="ps-cta-note">{{ t.zipNote }}</p>
        </div>

        <div class="ps-card ps-howto">
          <h2>{{ t.howToInstall }}</h2>
          <ol class="ps-steps">
            <li>{{ p.hasRelease ? t.step1 : t.step1NoRelease }}</li>
            <li>{{ t.step2a }}<strong>{{ t.step2b }}</strong></li>
            <li>{{ isCs ? t.step3Cs : t.step3Js }}</li>
          </ol>
          <a :href="withBase(`${prefix}/basics/#${t.installAnchor}`)">{{ t.installLink }}</a>
        </div>

        <div class="ps-card ps-facts">
          <h2>{{ t.detailsTitle }}</h2>
          <dl>
            <div v-for="d in details" :key="d.k">
              <dt>{{ d.k }}</dt>
              <dd :class="{ mono: d.mono }">{{ d.v }}</dd>
            </div>
          </dl>
        </div>
      </aside>
    </div>
  </div>
  <p v-else class="ps-empty">{{ t.notFound }} <a :href="withBase(`${prefix}/store/`)">{{ t.backToStore }}</a>.</p>
</template>
