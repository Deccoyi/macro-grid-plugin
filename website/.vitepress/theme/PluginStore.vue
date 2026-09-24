<script setup lang="ts">
import { computed, ref } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'
import PluginCard from './PluginCard.vue'
import { useStoreText } from './store-i18n'

const props = defineProps<{ plugins: StorePlugin[] }>()

const { t, prefix, category: catLabel } = useStoreText()

const query = ref('')
const category = ref('All')

const categories = computed(() => ['All', ...new Set(props.plugins.map((p) => p.category))])

const shown = computed(() => {
  const q = query.value.trim().toLowerCase()
  return props.plugins
    .filter((p) => category.value === 'All' || p.category === category.value)
    .filter(
      (p) =>
        !q || `${p.name} ${p.description} ${p.category} ${catLabel(p.category)} ${p.kind}`.toLowerCase().includes(q),
    )
    .sort((a, b) => Number(b.featured) - Number(a.featured) || a.name.localeCompare(b.name))
})
</script>

<template>
  <div class="ps-store">
    <header class="ps-hero">
      <h1>{{ t.storeTitle }}</h1>
      <p>{{ t.storeIntro }}</p>
    </header>

    <div class="ps-controls">
      <label class="ps-search">
        <span class="ps-sr">{{ t.searchLabel }}</span>
        <input v-model="query" type="search" :placeholder="t.searchLabel" autocomplete="off" />
      </label>
      <div class="ps-chips" role="group" :aria-label="t.filterLabel">
        <button
          v-for="c in categories"
          :key="c"
          type="button"
          class="ps-chip"
          :class="{ active: category === c }"
          :aria-pressed="category === c"
          @click="category = c"
        >
          {{ c === 'All' ? t.all : catLabel(c) }}
        </button>
      </div>
    </div>

    <div class="ps-grid" v-if="shown.length">
      <PluginCard v-for="p in shown" :key="p.id" :plugin="p" />
    </div>
    <p v-else class="ps-empty" role="status">
      {{ plugins.length ? t.noMatch : t.emptyStore }}
    </p>

    <p class="ps-foot">
      {{ t.wantYours }}
      <a :href="withBase(`${prefix}/guides/publishing#${t.publishAnchor}`)">{{ t.publishLink }}</a>.
    </p>
  </div>
</template>
