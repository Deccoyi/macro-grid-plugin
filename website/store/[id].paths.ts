import { loadStore } from '../.vitepress/store-lib'

export default {
  async paths() {
    const plugins = await loadStore()
    return plugins.map((plugin) => ({ params: { id: plugin.id, plugin } }))
  },
}
