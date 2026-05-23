import type { TranslationDictionary } from "@/lib/i18n/en";

export const pt: TranslationDictionary = {
  app: {
    name: "Elevator Ads Admin",
    subtitle: "Operacao de playlists agendadas",
  },
  nav: {
    dashboard: "Painel",
    buildings: "Edificios",
    screens: "Telas",
    advertisers: "Anunciantes",
    creatives: "Criativos",
    campaigns: "Campanhas",
    playlists: "Playlists",
    reports: "Relatorios",
    settings: "Configuracoes",
  },
  common: {
    language: "Idioma",
    theme: "Tema",
    light: "Claro",
    dark: "Escuro",
    mobileMenu: "Menu",
    close: "Fechar",
    noData: "Nenhum dado disponivel.",
    newBuilding: "Novo edificio",
    recentActivity: "Atividade recente",
    mockedData: "Dados simulados",
    environment: "Ambiente",
    currentTheme: "Tema atual",
    currentLanguage: "Idioma atual",
    updatedForMvp: "Preparado para entrega diaria de playlists do MVP",
  },
  dashboard: {
    title: "Painel operacional",
    description:
      "Acompanhe inventario, publicacao de playlists diarias e visibilidade operacional por edificio.",
    cards: {
      buildings: "Edificios",
      screens: "Telas",
      activeScreens: "Telas ativas",
      advertisers: "Anunciantes",
      activeCampaigns: "Campanhas ativas",
      playlistsGeneratedToday: "Playlists geradas hoje",
      playlistsPendingDownload: "Playlists pendentes de download",
      playsReportedToday: "Exibicoes reportadas hoje",
    },
  },
  pages: {
    buildings: {
      title: "Edificios",
      description: "Visao do portfolio de edificios e alcance diario estimado.",
    },
    screens: {
      title: "Telas",
      description: "Saude dos dispositivos, playlist atual e status operacional.",
    },
    advertisers: {
      title: "Anunciantes",
      description: "Contas comerciais e relacao com campanhas.",
    },
    creatives: {
      title: "Criativos",
      description: "Pecas prontas para revisao e entrega nas telas dos elevadores.",
    },
    campaigns: {
      title: "Campanhas",
      description: "Janelas de veiculacao, orcamentos e restricoes de entrega.",
    },
    playlists: {
      title: "Playlists diarias",
      description: "Sequencias publicadas e distribuidas diariamente para as telas dos elevadores.",
      note: "As telas dos elevadores baixam uma playlist diaria publicada e repetem a sequencia programada ao longo do dia.",
    },
    reports: {
      title: "Relatorios",
      description: "Superficies iniciais para acompanhamento de exibicoes e proof-of-play.",
    },
    settings: {
      title: "Configuracoes",
      description: "Preferencias da interface e placeholders de ambiente para configuracoes futuras.",
    },
  },
  reports: {
    playsToday: "Exibicoes hoje",
    playsByCampaign: "Exibicoes por campanha",
    playsByScreen: "Exibicoes por tela",
    pendingProofOfPlay: "Proof-of-play pendente",
  },
  settings: {
    themePreference: "Preferencia de tema",
    languagePreference: "Preferencia de idioma",
    environmentInfo: "Informacoes do ambiente",
    environmentDescription:
      "A integracao com backend ainda nao esta conectada. Esta interface usa dados simulados por enquanto.",
  },
};
