import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

const resources = {
  en: {
    translation: {
      appTitle: 'CozyLedger',
      languageLabel: 'Language',
      healthOnline: 'Backend status: online',
      healthOffline: 'Backend status: unavailable',
      navDashboard: 'Dashboard',
      navLedger: 'Ledger',
      navReports: 'Reports',
      navAccounts: 'Accounts',
      navMembers: 'Members',
      dashboardTitle: 'Household Snapshot',
      dashboardHint: 'Quick overview widgets will be added here.',
      ledgerTitle: 'Ledger',
      ledgerHint: 'Transactions list and filters will be added in milestone 8.',
      reportsTitle: 'Reports',
      reportsHint: 'Summary and category charts will be added in milestone 9.',
      accountsTitle: 'Accounts',
      accountsHint: 'Manage accounts and categories in upcoming milestones.',
      membersTitle: 'Members',
      membersHint: 'Invite and membership management flows land in milestone 8.'
    }
  },
  zh: {
    translation: {
      appTitle: '温馨账本',
      languageLabel: '语言',
      healthOnline: '后端状态：在线',
      healthOffline: '后端状态：不可用',
      navDashboard: '仪表盘',
      navLedger: '流水',
      navReports: '报表',
      navAccounts: '账户',
      navMembers: '成员',
      dashboardTitle: '家庭概览',
      dashboardHint: '后续会在这里补充概览组件。',
      ledgerTitle: '流水',
      ledgerHint: '交易列表和筛选将在第 8 里程碑完成。',
      reportsTitle: '报表',
      reportsHint: '汇总和分类图表将在第 9 里程碑完成。',
      accountsTitle: '账户',
      accountsHint: '账户与分类管理将在后续里程碑实现。',
      membersTitle: '成员',
      membersHint: '邀请与成员管理流程将在第 8 里程碑实现。'
    }
  }
} as const

void i18n.use(initReactI18next).init({
  resources,
  lng: 'en',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false
  }
})

export default i18n
