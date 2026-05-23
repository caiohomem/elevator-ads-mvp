export interface TranslationDictionary {
  app: {
    name: string;
    subtitle: string;
  };
  nav: {
    dashboard: string;
    buildings: string;
    screens: string;
    advertisers: string;
    creatives: string;
    campaigns: string;
    playlists: string;
    reports: string;
    settings: string;
  };
  common: {
    language: string;
    theme: string;
    light: string;
    dark: string;
    mobileMenu: string;
    close: string;
    noData: string;
    newBuilding: string;
    recentActivity: string;
    mockedData: string;
    environment: string;
    currentTheme: string;
    currentLanguage: string;
    updatedForMvp: string;
  };
  dashboard: {
    title: string;
    description: string;
    cards: {
      buildings: string;
      screens: string;
      activeScreens: string;
      advertisers: string;
      activeCampaigns: string;
      playlistsGeneratedToday: string;
      playlistsPendingDownload: string;
      playsReportedToday: string;
    };
  };
  pages: {
    buildings: { title: string; description: string };
    screens: { title: string; description: string };
    advertisers: { title: string; description: string };
    creatives: { title: string; description: string };
    campaigns: { title: string; description: string };
    playlists: { title: string; description: string; note: string };
    reports: { title: string; description: string };
    settings: { title: string; description: string };
  };
  reports: {
    playsToday: string;
    playsByCampaign: string;
    playsByScreen: string;
    pendingProofOfPlay: string;
  };
  settings: {
    themePreference: string;
    languagePreference: string;
    environmentInfo: string;
    environmentDescription: string;
  };
}

export const en: TranslationDictionary = {
  app: {
    name: "Elevator Ads Admin",
    subtitle: "Scheduled playlist operations",
  },
  nav: {
    dashboard: "Dashboard",
    buildings: "Buildings",
    screens: "Screens",
    advertisers: "Advertisers",
    creatives: "Creatives",
    campaigns: "Campaigns",
    playlists: "Playlists",
    reports: "Reports",
    settings: "Settings",
  },
  common: {
    language: "Language",
    theme: "Theme",
    light: "Light",
    dark: "Dark",
    mobileMenu: "Menu",
    close: "Close",
    noData: "No data available yet.",
    newBuilding: "New building",
    recentActivity: "Recent activity",
    mockedData: "Mocked data",
    environment: "Environment",
    currentTheme: "Current theme",
    currentLanguage: "Current language",
    updatedForMvp: "Prepared for MVP daily playlist delivery",
  },
  dashboard: {
    title: "Operations dashboard",
    description:
      "Monitor elevator inventory, daily playlist publishing, and operator visibility across buildings.",
    cards: {
      buildings: "Buildings",
      screens: "Screens",
      activeScreens: "Active screens",
      advertisers: "Advertisers",
      activeCampaigns: "Active campaigns",
      playlistsGeneratedToday: "Playlists generated today",
      playlistsPendingDownload: "Playlists pending download",
      playsReportedToday: "Plays reported today",
    },
  },
  pages: {
    buildings: {
      title: "Buildings",
      description: "Portfolio overview of building inventory and audience footprint.",
    },
    screens: {
      title: "Screens",
      description: "Device health, playlist assignment, and operational status.",
    },
    advertisers: {
      title: "Advertisers",
      description: "Commercial accounts and campaign relationships.",
    },
    creatives: {
      title: "Creatives",
      description: "Review-ready assets prepared for elevator screen delivery.",
    },
    campaigns: {
      title: "Campaigns",
      description: "Flight windows, budgets, and targeting constraints.",
    },
    playlists: {
      title: "Daily playlists",
      description: "Published sequences distributed to elevator screens each day.",
      note: "Elevator screens download a published daily playlist and repeat the programmed sequence throughout the day.",
    },
    reports: {
      title: "Reports",
      description: "Placeholder reporting surfaces for playback and proof-of-play review.",
    },
    settings: {
      title: "Settings",
      description: "Frontend preferences and environment placeholders for future configuration.",
    },
  },
  reports: {
    playsToday: "Plays today",
    playsByCampaign: "Plays by campaign",
    playsByScreen: "Plays by screen",
    pendingProofOfPlay: "Pending proof-of-play",
  },
  settings: {
    themePreference: "Theme preference",
    languagePreference: "Language preference",
    environmentInfo: "Environment information",
    environmentDescription:
      "Backend wiring is not connected yet. This shell currently serves mocked operational data.",
  },
};
