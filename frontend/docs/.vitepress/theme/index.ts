import DefaultTheme from 'vitepress/theme'
import type { Theme } from 'vitepress'

import HomeDocHub from './components/HomeDocHub.vue'

import './custom.css'

const theme: Theme = {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('HomeDocHub', HomeDocHub)
  },
}

export default theme
