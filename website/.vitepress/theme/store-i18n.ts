import { useData } from 'vitepress'
import { computed } from 'vue'
import { langOf } from '../i18n'

const en = {
  // Store list
  storeTitle: 'Plugin Store',
  storeIntro: 'Download plugins for Macro Grid. Each one is a zip you install from the editor.',
  searchLabel: 'Search plugins',
  filterLabel: 'Filter by category',
  all: 'All',
  noMatch: 'No plugins match your search.',
  emptyStore: 'The store is empty right now.',
  wantYours: 'Want to add yours? See',
  publishLink: 'Getting your plugin into the Store',
  publishAnchor: 'getting-your-plugin-into-the-store',
  example: 'Example',
  alpha: 'alpha',
  latest: 'Latest',
  download: 'Download',
  getOnGithub: 'Get it on GitHub',
  details: 'Details',
  categories: {} as Record<string, string>,

  // Detail page
  store: 'Store',
  notFound: 'Plugin not found.',
  backToStore: 'Back to the store',
  sourceCode: 'Source code',
  zipNote: 'A zip file you install from the editor.',
  tabsLabel: 'Sections',
  tabOverview: 'Overview',
  versions: 'Versions',
  changelog: 'Changelog',
  whatItDoes: 'What it does',
  requirements: 'Requirements',
  server: 'Macro Grid server',
  orNewer: 'or newer',
  permissions: 'Permissions',
  permissionsIntro: 'This is a JavaScript plugin. It runs in a sandbox and starts once you approve these permissions.',
  csTrust: 'This is a C# plugin and runs on your PC with full trust. Only install plugins from a source you trust. Macro Grid is alpha software.',
  permVariables: 'Read any variable and publish its own',
  permActions: 'Register its own actions',
  permInput: 'Press key combinations and type text on the PC',
  permHttp: 'Send HTTP requests to',

  statVersion: 'Version',
  statKind: 'Type',
  statServer: 'Needs server',
  statAccess: 'Access',
  statFull: 'Full',
  statPermissions: 'Permissions',
  kindCs: 'C# plugin',
  kindJs: 'JavaScript plugin',

  howToInstall: 'How to install',
  step1: 'Download the zip and unzip it into a folder.',
  step1NoRelease: 'Get the zip from the GitHub Releases page and unzip it into a folder.',
  step2a: 'In the editor choose ',
  step2b: 'Plugins → Manage Plugins… → Install from Folder…',
  step3Cs: 'The plugin loads immediately, no restart needed.',
  step3Js: 'It asks for the permissions listed here and starts once you approve them.',
  installLink: 'Install a plugin',
  installAnchor: 'install-a-plugin',
  detailsTitle: 'Details',
  detailRelease: 'Release date',
  detailId: 'ID',
  detailType: 'Type',
  detailServer: 'Needs server',
  detailSdk: 'SDK',

  releaseNotes: 'Release notes',
  noReleaseInfo: 'Release information is not available right now.',
  olderVersions: 'Older versions on GitHub Releases',
  showMore: 'Show more',
  groupNames: { new: 'New', added: 'Added', fixed: 'Fixed', changed: 'Changed', removed: 'Removed', security: 'Security' } as Record<string, string>,
}

const tr: typeof en = {
  storeTitle: 'Eklenti Mağazası',
  storeIntro: "Macro Grid için eklentileri indirin. Her biri, Düzenleyici'den kurduğunuz bir zip dosyasıdır.",
  searchLabel: 'Eklenti ara',
  filterLabel: 'Kategoriye göre süz',
  all: 'Tümü',
  noMatch: 'Aramanızla eşleşen eklenti yok.',
  emptyStore: 'Mağaza şu an boş.',
  wantYours: 'Sizinki de burada olsun mu? Bkz.',
  publishLink: "Eklentinizi Mağaza'ya ekleme",
  publishAnchor: 'eklentinizi-magaza-ya-ekleme',
  example: 'Örnek',
  alpha: 'alfa',
  latest: 'Son sürüm',
  download: 'İndir',
  getOnGithub: "GitHub'dan edinin",
  details: 'Ayrıntılar',
  categories: { Examples: 'Örnekler', 'Icon packs': 'Simge paketleri', Integrations: 'Entegrasyonlar', Other: 'Diğer' },

  store: 'Mağaza',
  notFound: 'Eklenti bulunamadı.',
  backToStore: 'Mağazaya dön',
  sourceCode: 'Kaynak kodu',
  zipNote: "Zip dosyası. Düzenleyici'den yüklersiniz.",
  tabsLabel: 'Bölümler',
  tabOverview: 'Genel bakış',
  versions: 'Sürümler',
  changelog: 'Değişiklik günlüğü',
  whatItDoes: 'Ne yapar',
  requirements: 'Gereksinimler',
  server: 'Macro Grid sunucusu',
  orNewer: 'veya üstü',
  permissions: 'İzinler',
  permissionsIntro: 'Bu bir JavaScript eklentisidir. Korumalı alanda çalışır ve aşağıdaki izinleri siz onaylayınca başlar.',
  csTrust: 'Bu bir C# eklentisidir ve bilgisayarınızda tam yetkiyle çalışır. Yalnızca güvendiğiniz kaynaklardan kurun. Macro Grid alfa aşamasındadır.',
  permVariables: 'Değişkenleri okumak ve kendi değişkenlerini yayınlamak',
  permActions: 'Kendi aksiyonlarını eklemek',
  permInput: 'Bilgisayarda tuş kombinasyonlarına basmak ve metin yazmak',
  permHttp: 'Şuraya HTTP isteği göndermek:',

  statVersion: 'Sürüm',
  statKind: 'Tür',
  statServer: 'Gerekli sunucu',
  statAccess: 'Yetki',
  statFull: 'Tam',
  statPermissions: 'İzin',
  kindCs: 'C# eklentisi',
  kindJs: 'JavaScript eklentisi',

  howToInstall: 'Nasıl kurulur',
  step1: "Zip'i indirin ve bir klasöre açın.",
  step1NoRelease: "Zip'i GitHub Sürümler sayfasından alın ve bir klasöre açın.",
  step2a: "Düzenleyici'de şunu seçin: ",
  step2b: 'Eklentiler → Eklentileri Yönet… → Klasörden Yükle…',
  step3Cs: 'Eklenti hemen yüklenir, yeniden başlatma gerekmez.',
  step3Js: 'Burada listelenen izinleri ister ve siz onaylayınca başlar.',
  installLink: 'Eklenti kurma',
  installAnchor: 'eklenti-kurma',
  detailsTitle: 'Ayrıntılar',
  detailRelease: 'Yayın tarihi',
  detailId: 'Kimlik',
  detailType: 'Tür',
  detailServer: 'Gerekli sunucu',
  detailSdk: 'SDK',

  releaseNotes: 'Sürüm notları',
  noReleaseInfo: 'Sürüm bilgisi şu an kullanılamıyor.',
  olderVersions: 'GitHub Sürümler sayfasında eski sürümler',
  showMore: 'Daha fazla göster',
  groupNames: { new: 'Yeni', added: 'Eklendi', fixed: 'Düzeltildi', changed: 'Değişti', removed: 'Kaldırıldı', security: 'Güvenlik' },
}

export function useStoreText() {
  const { lang } = useData()
  const isTr = computed(() => langOf(lang.value) === 'tr')
  const t = computed(() => (isTr.value ? tr : en))
  /** Prefix for links inside the site: '' or '/tr'. */
  const prefix = computed(() => (isTr.value ? '/tr' : ''))
  const category = (c: string) => t.value.categories[c] ?? c
  /** '24 Sep 2026' / '24 Eyl 2026' for an ISO date; '' when there is none. */
  const fmtDate = (iso: string) => {
    if (!iso) return ''
    const d = new Date(`${iso}T00:00:00Z`)
    return isNaN(d.getTime())
      ? iso
      : d.toLocaleDateString(isTr.value ? 'tr-TR' : 'en-US', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })
  }
  return { t, prefix, category, isTr, fmtDate }
}
