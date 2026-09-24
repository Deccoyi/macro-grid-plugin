<script setup lang="ts">
import { computed } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'
import { useStoreText } from './store-i18n'

const props = defineProps<{ plugin?: StorePlugin }>()

const { t, prefix, category } = useStoreText()

const p = computed(() => props.plugin)
const iconUrl = computed(() => (p.value?.icon ? withBase(`/store/icons/${p.value.icon}.svg`) : ''))
const letter = computed(() => (p.value?.name.trim()[0] ?? '?').toUpperCase())
const RELEASES = 'https://github.com/Deccoyi/macro-grid-plugin/releases'
</script>

<template>
  <div v-if="p" class="ps-detail">
    <p class="ps-back"><a :href="withBase(`${prefix}/store/`)">&larr; {{ t.allPlugins }}</a></p>

    <header class="ps-detail-head">
      <span class="ps-icon ps-icon-big" aria-hidden="true">
        <span v-if="iconUrl" class="ps-icon-mask" :style="{ '--ps-icon': `url(${iconUrl})` }"></span>
        <span v-else class="ps-icon-letter">{{ letter }}</span>
      </span>
      <div class="ps-detail-title">
        <h1>{{ p.name }}</h1>
        <div class="ps-badges">
          <span class="ps-badge">v{{ p.version }}</span>
          <span class="ps-badge">{{ p.kind }}</span>
          <span v-if="p.example" class="ps-badge ps-badge-example">{{ t.example }}</span>
          <span v-if="p.prerelease" class="ps-badge ps-badge-alpha">{{ t.alpha }}</span>
          <span class="ps-badge">{{ category(p.category) }}</span>
        </div>
        <p class="ps-desc">{{ p.description }}</p>
      </div>
      <div class="ps-detail-cta">
        <a class="ps-btn ps-btn-primary ps-btn-big" :href="p.downloadUrl">{{ p.hasRelease ? `${t.download} v${p.version}` : t.getOnGithub }}</a>
        <a class="ps-link-small" :href="p.sourceUrl">{{ t.sourceCode }}</a>
      </div>
    </header>

    <section v-if="p.whatItDoes">
      <h2>{{ t.whatItDoes }}</h2>
      <div class="vp-doc ps-rich" v-html="p.whatItDoes"></div>
    </section>

    <section>
      <h2>{{ t.requirements }}</h2>
      <dl class="ps-req">
        <dt>{{ t.server }}</dt>
        <dd>{{ p.minServerVersion }} {{ t.orNewer }}</dd>
        <dt>{{ t.sdk }}</dt>
        <dd>{{ p.sdkVersion }}</dd>
        <dt>{{ t.kind }}</dt>
        <dd>{{ p.kind === 'C#' ? t.kindCs : t.kindJs }}</dd>
        <dt>{{ t.permissions }}</dt>
        <dd>
          <template v-if="p.permissions.length"><code v-for="x in p.permissions" :key="x" class="ps-perm">{{ x }}</code></template>
          <template v-else>{{ t.noPermissions }}</template>
        </dd>
      </dl>
    </section>

    <section>
      <h2>{{ t.install }}</h2>
      <ol>
        <li>{{ t.install1 }}{{ p.hasRelease ? '' : t.install1b }}{{ t.install1c }}<code>plugin.json</code>.</li>
        <li>{{ t.install2a }}<strong>{{ t.install2b }}</strong></li>
        <li>{{ t.install3a }}<strong>{{ t.install3b }}</strong>{{ t.install3c }}</li>
        <li v-if="p.kind !== 'C#'">{{ t.install4 }}</li>
      </ol>
      <p>
        {{ t.moreDetail }} <a :href="withBase(`${prefix}/basics/#${t.installAnchor}`)">{{ t.installLink }}</a>.
        <template v-if="p.kind === 'C#'">{{ t.csTrust }}</template>
      </p>
    </section>

    <section>
      <h2>{{ t.versions }}</h2>
      <ul v-if="p.releases.length" class="ps-versions">
        <li v-for="(r, i) in p.releases" :key="r.version">
          <strong>v{{ r.version }}</strong>
          <span v-if="i === 0" class="ps-badge">{{ t.latest }}</span>
          <span v-if="r.prerelease" class="ps-badge ps-badge-alpha">{{ t.alpha }}</span>
          <span class="ps-date">{{ r.date }}</span>
          <a v-if="r.downloadUrl" :href="r.downloadUrl">{{ t.download }}</a>
          <a :href="r.notesUrl">{{ t.releaseNotes }}</a>
        </li>
      </ul>
      <p v-else>{{ t.noReleaseInfo }}</p>
      <p><a :href="RELEASES">{{ t.olderVersions }}</a></p>
    </section>

    <section v-if="p.changelog">
      <h2>{{ t.changelog }}</h2>
      <div class="vp-doc ps-rich ps-changelog" v-html="p.changelog"></div>
    </section>
  </div>
  <p v-else class="ps-empty">{{ t.notFound }} <a :href="withBase(`${prefix}/store/`)">{{ t.backToStore }}</a>.</p>
</template>
