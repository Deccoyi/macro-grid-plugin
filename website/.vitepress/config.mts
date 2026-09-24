import { defineConfig } from 'vitepress'

const repo = 'https://github.com/Deccoyi/macro-grid-plugin'

export default defineConfig({
  title: 'Macro Grid Plugins',
  description: 'Documentation for writing plugins for Macro Grid, the phone-and-tablet macro deck for Windows.',
  base: '/macro-grid-plugin/',
  lang: 'en-US',
  cleanUrls: true,
  lastUpdated: false,
  appearance: true, // light/dark toggle, follows the system setting by default
  head: [['meta', { name: 'theme-color', content: '#3c8772' }]],

  themeConfig: {
    search: { provider: 'local' },
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
      {
        text: 'Macro Grid',
        items: [
          { text: 'PC server site', link: 'https://deccoyi.github.io/macro-grid/' },
          { text: 'Download Macro Grid', link: 'https://deccoyi.github.io/macro-grid/download' },
          { text: 'Phone app', link: 'https://deccoyi.github.io/macro-grid-client/' },
        ],
      },
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
    outline: { level: [2, 3] },
    footer: {
      message: 'Macro Grid: <a href="https://deccoyi.github.io/macro-grid/">PC app</a> · <a href="https://deccoyi.github.io/macro-grid-client/">Phone app</a> · <a href="https://deccoyi.github.io/macro-grid-plugin/">Plugins</a><br>Released under the MIT License. Alpha software, written entirely by an AI assistant, provided as is without warranty.',
      copyright: 'Copyright (c) 2026 Deccoyi',
    },
  },
})
