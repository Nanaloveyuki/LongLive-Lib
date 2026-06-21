<script setup lang="ts">
import { computed } from 'vue'
import { withBase } from 'vitepress'

type HubLink = {
  title: string
  summary: string
  href: string
}

type AudienceLink = {
  title: string
  summary: string
  href: string
}

const props = withDefaults(
  defineProps<{
    locale?: 'zh-CN' | 'en-US'
  }>(),
  {
    locale: 'zh-CN',
  },
)

const content = computed(() => {
  if (props.locale === 'en-US') {
    return {
      eyebrow: 'Quick Entry',
      title: 'Pick the part of LongLive Lib you need and jump straight in.',
      copy: 'Choose an audience path first, then use the shortcut links if you already know where you want to go.',
      audienceLinks: [
        {
          title: 'Modder',
          summary: 'Runtime APIs, diagnostics, scene-routing work, and design notes.',
          href: '/en/modders/',
        },
        {
          title: 'Player',
          summary: 'What this mod changes in game, how to use it, and where to look when something feels wrong.',
          href: '/en/players/',
        },
        {
          title: 'Just browsing',
          summary: 'A short project overview, current directions, and a few useful entry points.',
          href: '/en/overview/',
        },
      ] satisfies AudienceLink[],
      quickLinks: [
        {
          title: 'Guide',
          summary: 'Deployment, runtime validation, bulk item optimization, and Next runtime usage.',
          href: '/en/guide/',
        },
        {
          title: 'Design',
          summary: 'Scene routing, map runtime, compatibility direction, and host strategy drafts.',
          href: '/en/design/',
        },
        {
          title: 'Report',
          summary: 'Regression reports and conclusions gathered from live gameplay testing.',
          href: '/en/report/',
        },
        {
          title: 'Workshop',
          summary: 'Packaging notes, player-facing overview, and release-facing material.',
          href: '/en/workshop/',
        },
      ] satisfies HubLink[],
    }
  }

    return {
      eyebrow: '快速入口',
      title: '直接进入你现在需要的部分。',
      copy: '先选一个更接近你的入口，再往下找具体内容。',
      audienceLinks: [
        {
          title: 'Modder',
          summary: '看接口方向、运行时诊断、SceneRouting，以及后续地图能力相关文档。',
          href: '/modders/',
        },
        {
          title: '玩家',
          summary: '看这个 Mod 现在做了什么、该怎么用、遇到问题先查哪里。',
          href: '/players/',
        },
        {
          title: '随便看看',
          summary: '快速了解这个项目现在是什么、准备往哪里做，以及从哪开始看。',
          href: '/overview/',
        },
      ] satisfies AudienceLink[],
      quickLinks: [
        {
          title: '指南',
          summary: '部署、验证、批量用物优化，以及当前 Next 运行时的用法。',
          href: '/guide/',
        },
        {
          title: '设计',
          summary: 'SceneRouting、自定义地图、兼容层和 Host 方向的设计文档。',
          href: '/design/',
        },
        {
          title: '报告',
          summary: '实机测试后的问题排查、回归记录和最终结论。',
          href: '/report/',
        },
        {
          title: '创意工坊',
          summary: '面向玩家的说明、上传清单，以及发布相关记录。',
          href: '/workshop/',
        },
      ] satisfies HubLink[],
    }
})
</script>

<template>
  <section class="hub-grid">
    <article class="hub-panel hub-panel-accent">
      <p class="hub-eyebrow">{{ content.eyebrow }}</p>
      <h2 class="hub-title">{{ content.title }}</h2>
      <p class="hub-copy">{{ content.copy }}</p>
      <ul class="hub-audience-list">
        <li v-for="link in content.audienceLinks" :key="link.title">
          <a class="hub-audience-card" :href="withBase(link.href)">
            <strong>{{ link.title }}</strong>
            <span>{{ link.summary }}</span>
          </a>
        </li>
      </ul>
    </article>

    <article class="hub-panel">
      <p class="hub-eyebrow">{{ props.locale === 'en-US' ? 'Start Here' : '从这里开始' }}</p>
      <ul class="hub-link-list">
        <li v-for="link in content.quickLinks" :key="link.title">
          <a class="hub-link-card" :href="withBase(link.href)">
            <strong>{{ link.title }}</strong>
            <span>{{ link.summary }}</span>
          </a>
        </li>
      </ul>
    </article>
  </section>
</template>

<style scoped>
.hub-grid {
  display: grid;
  gap: 1rem;
  margin: 1.5rem 0 2rem;
}

.hub-panel {
  border: 1px solid var(--vp-c-divider);
  border-radius: 24px;
  padding: 1.25rem;
  background: var(--ll-panel-bg);
  box-shadow: 0 18px 46px rgba(50, 31, 20, 0.08);
}

.hub-panel-accent {
  background: var(--ll-panel-accent-bg),
    radial-gradient(circle at top right, rgba(185, 78, 41, 0.16), transparent 34%),
    linear-gradient(180deg, rgba(255, 248, 239, 0.98), rgba(245, 236, 223, 0.98));
}

.hub-eyebrow {
  margin: 0 0 0.5rem;
  color: #93461d;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  font-size: 0.75rem;
  font-weight: 700;
}

.hub-title {
  margin: 0;
  font-size: 1.6rem;
  line-height: 1.15;
}

.hub-copy {
  margin: 0.9rem 0 0;
  color: var(--vp-c-text-2);
}

.hub-audience-list,
.hub-link-list {
  margin: 1rem 0 0;
  padding: 0;
  list-style: none;
}

.hub-audience-list {
  display: grid;
  gap: 0.7rem;
}

.hub-audience-card {
  display: grid;
  gap: 0.35rem;
  padding: 0.95rem 1rem;
  border-radius: 18px;
  text-decoration: none;
  color: inherit;
  background: var(--ll-chip-bg);
  border: 1px solid var(--ll-chip-border);
}

.hub-audience-card strong {
  color: var(--vp-c-brand-1);
}

.hub-audience-card span {
  color: var(--vp-c-text-2);
  line-height: 1.5;
}

.hub-link-list {
  display: grid;
  gap: 0.75rem;
}

.hub-link-card {
  display: grid;
  gap: 0.35rem;
  padding: 0.95rem 1rem;
  border-radius: 18px;
  text-decoration: none;
  color: inherit;
  background: var(--ll-chip-bg);
  border: 1px solid var(--ll-chip-border);
}

.hub-link-card strong {
  color: var(--vp-c-brand-1);
}

.hub-link-card span {
  color: var(--vp-c-text-2);
  line-height: 1.5;
}

.hub-link-card:hover {
  border-color: rgba(185, 78, 41, 0.35);
  transform: translateY(-1px);
}

.hub-audience-card:hover {
  border-color: rgba(185, 78, 41, 0.35);
  transform: translateY(-1px);
}

@media (min-width: 860px) {
  .hub-grid {
    grid-template-columns: 1.2fr 1fr;
  }
}
</style>
