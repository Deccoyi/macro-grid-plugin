import { defineConfig } from 'vitepress'
import { sharedConfig, turkishThemeLabels } from './shared'

const repo = 'https://github.com/Deccoyi/macro-grid-plugin'
const shared = sharedConfig('/macro-grid-plugin/', '#7c3aed')

export default defineConfig({
  ...shared,
  title: 'Macro Grid Plugins',
  description: 'Documentation for writing plugins for Macro Grid, the phone-and-tablet macro deck for Windows.',
  base: '/macro-grid-plugin/',

  locales: {
    root: { label: 'English', lang: 'en-US' },
    tr: {
      label: 'Türkçe',
      lang: 'tr-TR',
      link: '/tr/',
      description: "Macro Grid için eklenti yazma dokümantasyonu. Macro Grid, Windows için telefon ve tablet makro deck'idir.",
      themeConfig: {
        ...turkishThemeLabels(),
        nav: [
          { text: 'Mağaza', link: '/tr/store/', activeMatch: '/tr/store/' },
          {
            text: 'Dokümanlar',
            activeMatch: '^/tr/(introduction|getting-started|basics|tutorials|guides|reference)/',
            items: [
              { text: 'Giriş', link: '/tr/introduction/' },
              { text: 'Başlarken', link: '/tr/getting-started/' },
              { text: 'Eklenti temelleri', link: '/tr/basics/' },
              { text: 'Öğreticiler', link: '/tr/tutorials/js-hello-world' },
              { text: 'Kılavuzlar', link: '/tr/guides/settings-pages' },
              { text: 'Başvuru', link: '/tr/reference/manifest' },
            ],
          },
          { text: 'Eklenti geliştir', link: '/tr/getting-started/', activeMatch: '^/tr/getting-started/' },
        ],
        sidebar: [
          {
            text: 'Giriş',
            items: [
              { text: 'Macro Grid nedir', link: '/tr/introduction/' },
              { text: 'Başlarken', link: '/tr/getting-started/' },
            ],
          },
          {
            text: 'Eklenti temelleri',
            items: [
              { text: 'Türler, klasörler ve kurulum', link: '/tr/basics/' },
              { text: 'Uyumluluk ve sürümleme', link: '/tr/basics/compatibility' },
            ],
          },
          {
            text: 'Öğreticiler',
            items: [
              { text: '1. JavaScript ile merhaba dünya', link: '/tr/tutorials/js-hello-world' },
              { text: '2. C# ile merhaba dünya', link: '/tr/tutorials/csharp-hello-world' },
              { text: '3. Canlı veri gösterme', link: '/tr/tutorials/live-data' },
            ],
          },
          {
            text: 'Kılavuzlar',
            items: [
              { text: 'Ayar sayfaları', link: '/tr/guides/settings-pages' },
              { text: 'Simge paketleri: PLC Icons', link: '/tr/guides/icon-packs' },
              { text: 'Gerçek dünyadan bir eklenti: OBS', link: '/tr/guides/obs-plugin' },
              { text: 'Hata ayıklama ve günlükler', link: '/tr/guides/debugging' },
              { text: 'Eklentinizi yayımlama', link: '/tr/guides/publishing' },
              { text: 'Depo kuralları', link: '/tr/guides/repo-rules' },
            ],
          },
          {
            text: 'Başvuru',
            items: [
              { text: 'Manifest (plugin.json)', link: '/tr/reference/manifest' },
              { text: 'JavaScript host API', link: '/tr/reference/js-host-api' },
              { text: 'C# SDK arayüzleri', link: '/tr/reference/csharp-sdk' },
              { text: 'İzinler', link: '/tr/reference/permissions' },
              { text: 'Değişiklik günlükleri', link: '/tr/reference/changelogs' },
              { text: 'Kaynak dizini', link: '/tr/reference/source-index' },
            ],
          },
        ],
        editLink: {
          pattern: `${repo}/edit/dev/website/:path`,
          text: "GitHub'da bir değişiklik öner",
        },
      },
    },
  },

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
          { text: 'Source index', link: '/reference/source-index' },
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
