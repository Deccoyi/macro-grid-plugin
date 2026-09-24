import { defineLoader } from 'vitepress'

// The current server and plugin SDK versions are read from the server repository at build time, so the
// compatibility table never has to be edited when a version changes. On any error the value is null and
// the page shows a generic text instead.
export interface CurrentVersions {
  server: string | null
  sdk: string | null
}

declare const data: CurrentVersions
export { data }

const RAW = 'https://raw.githubusercontent.com/Deccoyi/macro-grid/main/'

async function readVersion(path: string, pattern: RegExp): Promise<string | null> {
  try {
    const res = await fetch(RAW + path)
    if (!res.ok) return null
    const match = pattern.exec(await res.text())
    return match ? match[1] : null
  } catch {
    return null
  }
}

export default defineLoader({
  async load(): Promise<CurrentVersions> {
    const [server, sdk] = await Promise.all([
      readVersion('src/MacroGrid.Core/Sessions/ClientHub.cs', /ServerVersion\s*=\s*"([^"]+)"/),
      readVersion('src/MacroGrid.Plugin.Abstractions/PluginSdk.cs', /Version\s*=\s*"([^"]+)"/),
    ])
    return { server, sdk }
  },
})
