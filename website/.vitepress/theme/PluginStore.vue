<script setup lang="ts">
import { computed, ref } from 'vue'
import { withBase } from 'vitepress'
import type { StorePlugin } from '../store-lib'
import PluginCard from './PluginCard.vue'

const props = defineProps<{ plugins: StorePlugin[] }>()

const query = ref('')
const category = ref('All')

const categories = computed(() => ['All', ...new Set(props.plugins.map((p) => p.category))])

const shown = computed(() => {
  const q = query.value.trim().toLowerCase()
  return props.plugins
    .filter((p) => category.value === 'All' || p.category === category.value)
    .filter((p) => !q || `${p.name} ${p.description} ${p.category} ${p.kind}`.toLowerCase().includes(q))
    .sort((a, b) => Number(b.featured) - Number(a.featured) || a.name.localeCompare(b.name))
})
</script>

<template>
  <div class="ps-store">
    <header class="ps-hero">
      <h1>Plugin Store</h1>
      <p>Download plugins for Macro Grid. Each one is a zip you install from the editor.</p>
    </header>

    <div class="ps-controls">
      <label class="ps-search">
        <span class="ps-sr">Search plugins</span>
        <input v-model="query" type="search" placeholder="Search plugins" autocomplete="off" />
      </label>
      <div class="ps-chips" role="group" aria-label="Filter by category">
        <button
          v-for="c in categories"
          :key="c"
          type="button"
          class="ps-chip"
          :class="{ active: category === c }"
          :aria-pressed="category === c"
          @click="category = c"
        >
          {{ c }}
        </button>
      </div>
    </div>

    <div class="ps-grid" v-if="shown.length">
      <PluginCard v-for="p in shown" :key="p.id" :plugin="p" />
    </div>
    <p v-else class="ps-empty" role="status">
      {{ plugins.length ? 'No plugins match your search.' : 'The store is empty right now.' }}
    </p>

    <p class="ps-foot">
      Want to add yours? See <a :href="withBase('/guides/publishing#getting-your-plugin-into-the-store')">Getting your plugin into the Store</a>.
    </p>
  </div>
</template>
