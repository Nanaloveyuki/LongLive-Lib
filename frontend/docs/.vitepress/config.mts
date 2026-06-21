import { defineConfig, type DefaultTheme } from 'vitepress'

const repoSlug = process.env.GITHUB_REPOSITORY?.split('/')[1] ?? 'LongLive-Lib'
const docsBase = process.env.DOCS_BASE ?? (process.env.GITHUB_ACTIONS ? `/${repoSlug}/` : '/')
const repository = 'https://github.com/Nanaloveyuki/LongLive-Lib'

type LocaleMode = 'zh' | 'en'

function withPrefix(prefix: string, value: string): string {
  return `${prefix}${value}`
}

function createGuideSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/guide/') },
        { text: '部署指南', link: withPrefix(prefix, '/guide/deploy-guide') },
        { text: '运行验证清单', link: withPrefix(prefix, '/guide/host-runtime-validation-checklist') },
        { text: '批量用物优化', link: withPrefix(prefix, '/guide/item-use-optimization') },
        { text: '地图追踪', link: withPrefix(prefix, '/guide/map-trace') },
        { text: 'Mod Loader 用法', link: withPrefix(prefix, '/guide/mod-loader-usage') },
        { text: 'Next 运行时用法', link: withPrefix(prefix, '/guide/next-runtime-usage') },
        { text: 'Next 运行时示例', link: withPrefix(prefix, '/guide/next-runtime-examples') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/guide/') },
        { text: 'Deployment Guide', link: withPrefix(prefix, '/guide/deploy-guide') },
        { text: 'Host Runtime Validation', link: withPrefix(prefix, '/guide/host-runtime-validation-checklist') },
        { text: 'Item Use Optimization', link: withPrefix(prefix, '/guide/item-use-optimization') },
        { text: 'Map Trace', link: withPrefix(prefix, '/guide/map-trace') },
        { text: 'Mod Loader Usage', link: withPrefix(prefix, '/guide/mod-loader-usage') },
        { text: 'Next Runtime Usage', link: withPrefix(prefix, '/guide/next-runtime-usage') },
        { text: 'Next Runtime Examples', link: withPrefix(prefix, '/guide/next-runtime-examples') },
      ]
}

function createDesignSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/design/') },
        { text: '设计文档索引', link: withPrefix(prefix, '/design/reference/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/design/') },
        { text: 'Design Index', link: withPrefix(prefix, '/design/reference/') },
      ]
}

function createReportSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/report/') },
        { text: '报告索引', link: withPrefix(prefix, '/report/findings/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/report/') },
        { text: 'Report Index', link: withPrefix(prefix, '/report/findings/') },
      ]
}

function createWorkshopSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/workshop/') },
        { text: '创意工坊说明', link: withPrefix(prefix, '/workshop/overview') },
        { text: '上传清单', link: withPrefix(prefix, '/workshop/upload-checklist') },
        { text: '0.1.0 发布记录', link: withPrefix(prefix, '/workshop/release-0.1.0') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/workshop/') },
        { text: 'Workshop Notes', link: withPrefix(prefix, '/workshop/overview') },
        { text: 'Upload Checklist', link: withPrefix(prefix, '/workshop/upload-checklist') },
        { text: 'Release 0.1.0 Notes', link: withPrefix(prefix, '/workshop/release-0.1.0') },
      ]
}

function createSamplesSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/samples/') },
        { text: '示例索引', link: withPrefix(prefix, '/samples/library/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/samples/') },
        { text: 'Sample Index', link: withPrefix(prefix, '/samples/library/') },
      ]
}

function createPlayerSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/players/') },
        { text: '功能说明', link: withPrefix(prefix, '/players/features/') },
        { text: '安装与使用', link: withPrefix(prefix, '/players/getting-started/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/players/') },
        { text: 'Features', link: withPrefix(prefix, '/players/features/') },
        { text: 'Getting Started', link: withPrefix(prefix, '/players/getting-started/') },
      ]
}

function createModderSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/modders/') },
        { text: '当前能力', link: withPrefix(prefix, '/modders/capabilities/') },
        { text: '文档入口', link: withPrefix(prefix, '/modders/reading-path/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/modders/') },
        { text: 'Current Capabilities', link: withPrefix(prefix, '/modders/capabilities/') },
        { text: 'Reading Path', link: withPrefix(prefix, '/modders/reading-path/') },
      ]
}

function createOverviewSidebar(prefix: string, locale: LocaleMode): DefaultTheme.SidebarItem[] {
  return locale === 'zh'
    ? [
        { text: '总览', link: withPrefix(prefix, '/overview/') },
        { text: '项目现在是什么', link: withPrefix(prefix, '/overview/what-is-longlive/') },
      ]
    : [
        { text: 'Overview', link: withPrefix(prefix, '/overview/') },
        { text: 'What LongLive Lib Is', link: withPrefix(prefix, '/overview/what-is-longlive/') },
      ]
}

export default defineConfig({
  title: 'LongLive Lib',
  description: 'LongLive Lib documentation site.',
  base: docsBase,
  cleanUrls: true,
  srcExclude: ['dev/**'],
  lastUpdated: true,
  locales: {
    root: {
      label: '简体中文',
      lang: 'zh-CN',
      title: 'LongLive Lib',
      description: 'LongLive Lib 文档站。',
    },
    en: {
      label: 'English',
      lang: 'en-US',
      title: 'LongLive Lib',
      description: 'Documentation site for LongLive Lib.',
    },
  },
  themeConfig: {
    siteTitle: 'LongLive Lib',
    logo: '/favicon.svg',
    search: {
      provider: 'local',
    },
    socialLinks: [{ icon: 'github', link: repository }],
    locales: {
      root: {
        nav: [
          { text: '首页', link: '/' },
          { text: '玩家', link: '/players/' },
          { text: 'Modder', link: '/modders/' },
          { text: '指南', link: '/guide/' },
          { text: '设计', link: '/design/' },
          { text: '报告', link: '/report/' },
          { text: '创意工坊', link: '/workshop/' },
          { text: '示例', link: '/samples/' },
        ],
        sidebar: {
          '/players/': createPlayerSidebar('', 'zh'),
          '/modders/': createModderSidebar('', 'zh'),
          '/overview/': createOverviewSidebar('', 'zh'),
          '/guide/': createGuideSidebar('', 'zh'),
          '/design/': createDesignSidebar('', 'zh'),
          '/report/': createReportSidebar('', 'zh'),
          '/workshop/': createWorkshopSidebar('', 'zh'),
          '/samples/': createSamplesSidebar('', 'zh'),
        },
        outlineTitle: '本页内容',
        docFooter: {
          prev: '上一页',
          next: '下一页',
        },
        darkModeSwitchLabel: '外观',
        lightModeSwitchTitle: '切换到浅色模式',
        darkModeSwitchTitle: '切换到深色模式',
        sidebarMenuLabel: '菜单',
        returnToTopLabel: '回到顶部',
        lastUpdated: {
          text: '最后更新于',
        },
      },
      en: {
        nav: [
          { text: 'Home', link: '/en/' },
          { text: 'Players', link: '/en/players/' },
          { text: 'Modders', link: '/en/modders/' },
          { text: 'Guide', link: '/en/guide/' },
          { text: 'Design', link: '/en/design/' },
          { text: 'Report', link: '/en/report/' },
          { text: 'Workshop', link: '/en/workshop/' },
          { text: 'Samples', link: '/en/samples/' },
        ],
        sidebar: {
          '/en/players/': createPlayerSidebar('/en', 'en'),
          '/en/modders/': createModderSidebar('/en', 'en'),
          '/en/overview/': createOverviewSidebar('/en', 'en'),
          '/en/guide/': createGuideSidebar('/en', 'en'),
          '/en/design/': createDesignSidebar('/en', 'en'),
          '/en/report/': createReportSidebar('/en', 'en'),
          '/en/workshop/': createWorkshopSidebar('/en', 'en'),
          '/en/samples/': createSamplesSidebar('/en', 'en'),
        },
        outlineTitle: 'On this page',
        docFooter: {
          prev: 'Previous page',
          next: 'Next page',
        },
        darkModeSwitchLabel: 'Appearance',
        lightModeSwitchTitle: 'Switch to light mode',
        darkModeSwitchTitle: 'Switch to dark mode',
        sidebarMenuLabel: 'Menu',
        returnToTopLabel: 'Return to top',
        lastUpdated: {
          text: 'Last updated',
        },
      },
    },
    footer: {
      message: 'Built with VitePress.',
      copyright: 'MIT',
    },
  },
})
