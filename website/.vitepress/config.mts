import { defineConfig } from 'vitepress'
import { sharedConfig } from './shared'

const repo = 'https://github.com/Deccoyi/macro-grid-plugin'
const shared = sharedConfig('/macro-grid-plugin/', '#7c3aed')

export default defineConfig({
  ...shared,
  title: 'Macro Grid Plugins',
  description: 'Documentation for writing plugins for Macro Grid, the phone-and-tablet macro deck for Windows.',
  base: '/macro-grid-plugin/',

  themeConfig: {
    ...shared.themeConfig,
    nav: [
      { text: 'Store', link: '/store/', activeMatch: '/store/' },
      {
        text: 'Docs',
        activeMatch: '^/(introduction|getting-started|basics|tutorials|guides|reference)/',
        items: [
          { text: 'Introduction', link: '/introduction/' },
          { text: 'Getting started', link: '/getting-started/' },
          { text: 'Plugin basics', link: '/basics/' },
          { text: 'Tutorials', link: '/tutorials/js-hello-world' },
          { text: 'Guides', link: '/guides/settings-pages' },
          { text: 'Reference', link: '/reference/manifest' },
        ],
      },
      { text: 'Build a plugin', link: '/getting-started/', activeMatch: '^/getting-started/' },
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'What is Macro Grid', link: '/introduction/' },
          { text: 'Getting started', link: '/getting-started/' },
        ],
      },
      {
        text: 'Plugin basics',
        items: [
          { text: 'Kinds, folders and install', link: '/basics/' },
          { text: 'Compatibility and versioning', link: '/basics/compatibility' },
        ],
      },
      {
        text: 'Tutorials',
        items: [
          { text: '1. JavaScript hello world', link: '/tutorials/js-hello-world' },
          { text: '2. C# hello world', link: '/tutorials/csharp-hello-world' },
          { text: '3. Showing live data', link: '/tutorials/live-data' },
        ],
      },
      {
        text: 'Guides',
        items: [
          { text: 'Settings pages', link: '/guides/settings-pages' },
          { text: 'Icon packs (PLC Icons)', link: '/guides/icon-packs' },
          { text: 'A real-world plugin: OBS', link: '/guides/obs-plugin' },
          { text: 'Debugging and logs', link: '/guides/debugging' },
          { text: 'Publishing your plugin', link: '/guides/publishing' },
          { text: 'Repository rules', link: '/guides/repo-rules' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Manifest (plugin.json)', link: '/reference/manifest' },
          { text: 'JavaScript host API', link: '/reference/js-host-api' },
          { text: 'C# SDK interfaces', link: '/reference/csharp-sdk' },
          { text: 'Permissions', link: '/reference/permissions' },
          { text: 'Changelogs', link: '/reference/changelogs' },
        ],
      },
    ],
    socialLinks: [{ icon: 'github', link: repo }],
    editLink: {
      pattern: `${repo}/edit/dev/website/:path`,
      text: 'Suggest a change on GitHub',
    },
  },
})
