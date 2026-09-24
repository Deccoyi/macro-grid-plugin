import DefaultTheme from 'vitepress/theme'
import type { Theme } from 'vitepress'
import PluginStore from './PluginStore.vue'
import PluginCard from './PluginCard.vue'
import PluginDetail from './PluginDetail.vue'
import './store.css'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('PluginStore', PluginStore)
    app.component('PluginCard', PluginCard)
    app.component('PluginDetail', PluginDetail)
  },
} satisfies Theme
