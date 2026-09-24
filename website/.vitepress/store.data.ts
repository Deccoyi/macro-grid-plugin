import { defineLoader } from 'vitepress'
import { loadStore, type StorePlugin } from './store-lib'

export type { StorePlugin }
declare const data: StorePlugin[]
export { data }

export default defineLoader({
  watch: ['../store/catalog.json'],
  async load(): Promise<StorePlugin[]> {
    return loadStore()
  },
})
