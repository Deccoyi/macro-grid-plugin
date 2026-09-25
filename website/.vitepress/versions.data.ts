import { defineLoader } from 'vitepress'

// The current Macro Grid version (the server and the plugin SDK share one) is read from the server repository at build time, so the
// compatibility table never has to be edited when it changes. On any error the value is null and the page shows a generic text instead.
export interface CurrentVersions {
  macroGrid: string | null
}

declare const data: CurrentVersions
export { data }

const RAW = 'https://raw.githubusercontent.com/Deccoyi/macro-grid/main/'

export default defineLoader({
  async load(): Promise<CurrentVersions> {
    try {
      const res = await fetch(RAW + 'Directory.Build.props')
      if (!res.ok) return { macroGrid: null }
      const match = /<Version>(\d+\.\d+\.\d+)<\/Version>/.exec(await res.text())
      return { macroGrid: match ? match[1] : null }
    } catch {
      return { macroGrid: null }
    }
  },
})
