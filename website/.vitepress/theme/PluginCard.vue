<script setup lang="ts">
import { computed } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'
import { useStoreText } from './store-i18n'

const props = defineProps<{ plugin: StorePlugin }>()

const { t, prefix, category } = useStoreText()

const href = computed(() => withBase(`${prefix.value}/store/${props.plugin.id}`))
const iconUrl = computed(() => (props.plugin.icon ? withBase(`/store/icons/${props.plugin.icon}.svg`) : ''))
const letter = computed(() => (props.plugin.name.trim()[0] ?? '?').toUpperCase())
</script>

<template>
  <article class="ps-card">
    <a class="ps-card-head" :href="href">
      <span class="ps-icon" aria-hidden="true">
        <span v-if="iconUrl" class="ps-icon-mask" :style="{ '--ps-icon': `url(${iconUrl})` }"></span>
        <span v-else class="ps-icon-letter">{{ letter }}</span>
      </span>
      <span class="ps-card-title">
        <span class="ps-name">{{ plugin.name }}</span>
        <span class="ps-category">{{ category(plugin.category) }}</span>
      </span>
    </a>
    <p class="ps-desc">{{ plugin.description }}</p>
    <div class="ps-badges">
      <span class="ps-badge">v{{ plugin.version }}</span>
      <span class="ps-badge">{{ plugin.kind }}</span>
      <span v-if="plugin.example" class="ps-badge ps-badge-example">{{ t.example }}</span>
      <span v-if="plugin.prerelease" class="ps-badge ps-badge-alpha">{{ t.alpha }}</span>
    </div>
    <div class="ps-actions">
      <a class="ps-btn ps-btn-primary" :href="plugin.downloadUrl">{{ plugin.hasRelease ? t.download : t.getOnGithub }}</a>
      <a class="ps-btn" :href="href">{{ t.details }}</a>
    </div>
  </article>
</template>
