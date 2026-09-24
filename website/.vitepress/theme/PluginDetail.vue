<script setup lang="ts">
import { computed } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'

const props = defineProps<{ plugin?: StorePlugin }>()

const p = computed(() => props.plugin)
const iconUrl = computed(() => (p.value?.icon ? withBase(`/store/icons/${p.value.icon}.svg`) : ''))
const letter = computed(() => (p.value?.name.trim()[0] ?? '?').toUpperCase())
const RELEASES = 'https://github.com/Deccoyi/macro-grid-plugin/releases'
</script>

<template>
  <div v-if="p" class="ps-detail">
    <p class="ps-back"><a :href="withBase('/store/')">&larr; All plugins</a></p>

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
          <span v-if="p.example" class="ps-badge ps-badge-example">Example</span>
          <span v-if="p.prerelease" class="ps-badge ps-badge-alpha">alpha</span>
          <span class="ps-badge">{{ p.category }}</span>
        </div>
        <p class="ps-desc">{{ p.description }}</p>
      </div>
      <div class="ps-detail-cta">
        <a class="ps-btn ps-btn-primary ps-btn-big" :href="p.downloadUrl">{{ p.hasRelease ? `Download v${p.version}` : 'Get it on GitHub' }}</a>
        <a class="ps-link-small" :href="p.sourceUrl">Source code</a>
      </div>
    </header>

    <section v-if="p.whatItDoes">
      <h2>What it does</h2>
      <div class="vp-doc ps-rich" v-html="p.whatItDoes"></div>
    </section>

    <section>
      <h2>Requirements</h2>
      <dl class="ps-req">
        <dt>Macro Grid server</dt>
        <dd>{{ p.minServerVersion }} or newer</dd>
        <dt>Plugin SDK</dt>
        <dd>{{ p.sdkVersion }}</dd>
        <dt>Kind</dt>
        <dd>{{ p.kind === 'C#' ? 'C# plugin (runs inside the server, with full trust)' : 'JavaScript plugin (sandboxed, needs approval of its permissions)' }}</dd>
        <dt>Permissions</dt>
        <dd>
          <template v-if="p.permissions.length"><code v-for="x in p.permissions" :key="x" class="ps-perm">{{ x }}</code></template>
          <template v-else>None declared</template>
        </dd>
      </dl>
    </section>

    <section>
      <h2>Install</h2>
      <ol>
        <li>Download the zip{{ p.hasRelease ? '' : ' from the GitHub Releases page' }} and unzip it into a folder. The folder must contain <code>plugin.json</code>.</li>
        <li>In the Macro Grid editor open <strong>Plugins, Manage Plugins...</strong></li>
        <li>Choose <strong>Install from Folder...</strong> and pick the unzipped folder. It is loaded immediately, no restart.</li>
        <li v-if="p.kind !== 'C#'">The plugin asks for the permissions listed above and starts once you approve them.</li>
      </ol>
      <p>
        More detail in <a :href="withBase('/basics/#install-a-plugin')">Install a plugin</a>.
        <template v-if="p.kind === 'C#'">C# plugins have full access to your PC, so only install them from a source you trust.</template>
      </p>
    </section>

    <section>
      <h2>Versions</h2>
      <ul v-if="p.releases.length" class="ps-versions">
        <li v-for="(r, i) in p.releases" :key="r.version">
          <strong>v{{ r.version }}</strong>
          <span v-if="i === 0" class="ps-badge">latest</span>
          <span v-if="r.prerelease" class="ps-badge ps-badge-alpha">alpha</span>
          <span class="ps-date">{{ r.date }}</span>
          <a v-if="r.downloadUrl" :href="r.downloadUrl">Download</a>
          <a :href="r.notesUrl">Release notes</a>
        </li>
      </ul>
      <p v-else>Release information is not available right now.</p>
      <p><a :href="RELEASES">Older versions on GitHub Releases</a></p>
    </section>

    <section v-if="p.changelog">
      <h2>Changelog</h2>
      <div class="vp-doc ps-rich ps-changelog" v-html="p.changelog"></div>
    </section>
  </div>
  <p v-else class="ps-empty">Plugin not found. <a :href="withBase('/store/')">Back to the store</a>.</p>
</template>
