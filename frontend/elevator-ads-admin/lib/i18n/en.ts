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
    bookingRequests: string;
    programmaticSimulator: string;
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
    loading: string;
    errorLoading: string;
    retry: string;
    apiUnavailable: string;
    newBuilding: string;
    recentActivity: string;
    mockedData: string;
    environment: string;
    currentTheme: string;
    currentLanguage: string;
    updatedForMvp: string;
  };
  login: {
    title: string;
    subtitle: string;
    username: string;
    password: string;
    submit: string;
    submitting: string;
    invalidCredentials: string;
    unexpectedError: string;
    logout: string;
    loggedInAs: string;
    forbiddenTitle: string;
    forbiddenMessage: string;
    forbiddenAction: string;
  };
  pagination: {
    showing: string;
    of: string;
    prev: string;
    next: string;
    pageSize: string;
  };
  filters: {
    search: string;
    status: string;
    all: string;
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
    bookingRequests: {
      title: string;
      description: string;
      saveSuccess: string;
      submitSuccess: string;
      approveSuccess: string;
      rejectSuccess: string;
      view: string;
      detailTitle: string;
      columns: {
        name: string;
        advertiser: string;
        flight: string;
        status: string;
        budget: string;
        createdAt: string;
      };
      statuses: Record<"Draft" | "Submitted" | "UnderReview" | "Approved" | "Rejected" | "ConvertedToCampaign", string>;
      details: {
        advertiser: string;
        dateFrom: string;
        dateTo: string;
        cities: string;
        buildingTypes: string;
        screenOrientations: string;
        creativeDurationSeconds: string;
        budget: string;
        campaignObjective: string;
        notes: string;
      };
      forecast: {
        title: string;
        generate: string;
        generating: string;
        empty: string;
        loadError: string;
        eligibleScreens: string;
        eligibleBuildings: string;
        estimatedPlays: string;
        estimatedAudience: string;
        estimatedCost: string;
        availableCapacity: string;
        warnings: string;
        conflicts: string;
        disclaimer: string;
        updatedAt: string;
      };
    };
    programmaticSimulator: { title: string; description: string };
    playlists: {
      title: string;
      description: string;
      note: string;
      selectDate: string;
      generate: string;
      generating: string;
      generateSuccess: string;
      generateEmpty: string;
      generateError: string;
      viewDetails: string;
      publish: string;
      publishing: string;
      publishSuccess: string;
      publishError: string;
      publishedNote: string;
      columnDate: string;
      columnScreen: string;
      columnBuilding: string;
      columnVersion: string;
      columnStatus: string;
      columnItems: string;
      columnGeneratedAt: string;
      columnPublishedAt: string;
      columnDownloadedAt: string;
      itemOrder: string;
      itemCampaign: string;
      itemCreative: string;
      itemMediaType: string;
      itemDuration: string;
      detailScreen: string;
      detailBuilding: string;
      detailDate: string;
      detailVersion: string;
      detailStatus: string;
      detailGeneratedAt: string;
      detailPublishedAt: string;
      detailItems: string;
      status: {
        draft: string;
        published: string;
        downloaded: string;
        expired: string;
      };
    };
    reports: { title: string; description: string };
    settings: { title: string; description: string };
  };
  reports: {
    playsToday: string;
    playsByCampaign: string;
    playsByScreen: string;
    playsByCreative: string;
    pendingProofOfPlay: string;
    totalPlays: string;
    totalPlayedSeconds: string;
    dateFrom: string;
    dateTo: string;
    applyFilter: string;
    overviewSummary: string;
    overviewDate: string;
    campaignDeliverySummary: string;
    screenDeliverySummary: string;
    proofOfPlayEvents: string;
    campaignId: string;
    screenId: string;
    creativeId: string;
    playedAt: string;
    playedSeconds: string;
    plays: string;
    rangeLabel: string;
  };
  simulator: {
    note: string;
    advertiser: string;
    advertiserPlaceholder: string;
    existingAdvertisers: string;
    dateFrom: string;
    dateTo: string;
    cities: string;
    citiesHelp: string;
    buildingTypes: string;
    screenOrientations: string;
    creativeDurationSeconds: string;
    budget: string;
    campaignObjective: string;
    objectivePlaceholder: string;
    notes: string;
    notesPlaceholder: string;
    runForecast: string;
    runningForecast: string;
    emptyTitle: string;
    emptyDescription: string;
    resultsTitle: string;
    eligibleScreens: string;
    eligibleBuildings: string;
    estimatedPlays: string;
    estimatedAudience: string;
    estimatedCost: string;
    availableCapacity: string;
    warnings: string;
    conflicts: string;
    suggestedNextAction: string;
    audienceEstimate: string;
    capacityEstimate: string;
    optional: string;
  };
  settings: {
    themePreference: string;
    languagePreference: string;
    environmentInfo: string;
    environmentDescription: string;
  };
  forms: {
    newBuilding: string;
    editBuilding: string;
    newScreen: string;
    editScreen: string;
    newAdvertiser: string;
    editAdvertiser: string;
    newCreative: string;
    editCreative: string;
    newCampaign: string;
    editCampaign: string;
    newBookingRequest: string;
    editBookingRequest: string;
    manageCreatives: string;
    assignedCreatives: string;
    availableCreatives: string;
    assign: string;
    remove: string;
    startBeforeEnd: string;
    submit: string;
    submitForReview: string;
    approve: string;
    reject: string;
    save: string;
    cancel: string;
    saving: string;
    saved: string;
    saveFailed: string;
    fieldRequired: string;
    invalidEmail: string;
    mustBePositive: string;
    mustBeGreaterThanZero: string;
    actionFailed: string;
    edit: string;
    deliveryConstraints: {
      title: string;
      edit: string;
      save: string;
      cities: string;
      citiesHelp: string;
      buildingTypes: string;
      screenOrientations: string;
      daysOfWeek: string;
      startTime: string;
      endTime: string;
      timeHelp: string;
      emptyNote: string;
      loadFailed: string;
      startBeforeEnd: string;
    };
    building: {
      name: string;
      city: string;
      country: string;
      type: string;
      audience: string;
      address: string;
      postalCode: string;
    };
    screen: {
      name: string;
      buildingId: string;
      resolutionWidth: string;
      resolutionHeight: string;
      orientation: string;
      externalCode: string;
    };
    advertiser: {
      name: string;
      legalName: string;
      taxId: string;
      contactName: string;
      contactEmail: string;
      phone: string;
      status: string;
    };
    creative: {
      advertiserId: string;
      name: string;
      mediaUrl: string;
      mediaType: string;
      durationSeconds: string;
    };
    campaign: {
      advertiserId: string;
      name: string;
      status: string;
      startDate: string;
      endDate: string;
      dailyBudget: string;
      totalBudget: string;
      maxCpm: string;
    };
    bookingRequest: {
      advertiser: string;
      name: string;
      dateFrom: string;
      dateTo: string;
      cities: string;
      citiesHelp: string;
      buildingTypes: string;
      screenOrientations: string;
      creativeDurationSeconds: string;
      budget: string;
      campaignObjective: string;
      notes: string;
    };
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
    bookingRequests: "Booking Requests",
    programmaticSimulator: "Programmatic Simulator",
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
    noData: "No data found.",
    loading: "Loading data...",
    errorLoading: "Error loading data",
    retry: "Retry",
    apiUnavailable: "API unavailable. Showing fallback data where possible.",
    newBuilding: "New building",
    recentActivity: "Recent activity",
    mockedData: "Mocked data",
    environment: "Environment",
    currentTheme: "Current theme",
    currentLanguage: "Current language",
    updatedForMvp: "Prepared for MVP daily playlist delivery",
  },
  login: {
    title: "Sign in",
    subtitle: "Use your admin credentials to access scheduled playlist operations.",
    username: "Username",
    password: "Password",
    submit: "Sign in",
    submitting: "Signing in...",
    invalidCredentials: "Invalid username or password.",
    unexpectedError: "Unable to sign in right now. Please try again.",
    logout: "Sign out",
    loggedInAs: "Signed in as",
    forbiddenTitle: "Forbidden",
    forbiddenMessage: "Your account is authenticated, but it does not have permission to complete that action.",
    forbiddenAction: "Back to dashboard",
  },
  pagination: {
    showing: "Showing",
    of: "of",
    prev: "Previous",
    next: "Next",
    pageSize: "Page size",
  },
  filters: {
    search: "Search",
    status: "Status",
    all: "All",
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
    bookingRequests: {
      title: "Booking requests",
      description: "Structured commercial demand captured before forecast, approval, campaign creation, and playlist allocation.",
      saveSuccess: "Booking request saved.",
      submitSuccess: "Booking request submitted.",
      approveSuccess: "Booking request approved.",
      rejectSuccess: "Booking request rejected.",
      view: "View",
      detailTitle: "Booking request details",
      columns: {
        name: "Name",
        advertiser: "Advertiser",
        flight: "Flight",
        status: "Status",
        budget: "Budget",
        createdAt: "Created",
      },
      statuses: {
        Draft: "Draft",
        Submitted: "Submitted",
        UnderReview: "Under review",
        Approved: "Approved",
        Rejected: "Rejected",
        ConvertedToCampaign: "Converted to campaign",
      },
      details: {
        advertiser: "Advertiser",
        dateFrom: "Date from",
        dateTo: "Date to",
        cities: "Cities",
        buildingTypes: "Building types",
        screenOrientations: "Screen orientations",
        creativeDurationSeconds: "Creative duration",
        budget: "Budget",
        campaignObjective: "Campaign objective",
        notes: "Notes",
      },
      forecast: {
        title: "Campaign forecast",
        generate: "Generate forecast",
        generating: "Generating...",
        empty: "No forecast yet.",
        loadError: "Unable to load forecast.",
        eligibleScreens: "Eligible screens",
        eligibleBuildings: "Eligible buildings",
        estimatedPlays: "Estimated plays",
        estimatedAudience: "Estimated audience",
        estimatedCost: "Estimated cost",
        availableCapacity: "Available capacity",
        warnings: "Warnings",
        conflicts: "Conflicts",
        disclaimer: "Estimate only. Not proof of play.",
        updatedAt: "Updated at",
      },
    },
    programmaticSimulator: {
      title: "Programmatic simulator",
      description:
        "Simulate how an external buyer would forecast scheduled elevator media before any real external API or OpenRTB flow exists.",
    },
    playlists: {
      title: "Daily playlists",
      description: "Published sequences distributed to elevator screens each day.",
      note: "Elevator screens download a published daily playlist and repeat the programmed sequence throughout the day.",
      selectDate: "Playlist date",
      generate: "Generate playlists",
      generating: "Generating...",
      generateSuccess: "Daily playlists generated for the selected date.",
      generateEmpty: "No active campaigns produced a playlist for this date.",
      generateError: "Unable to generate playlists.",
      viewDetails: "View",
      publish: "Publish",
      publishing: "Publishing...",
      publishSuccess: "Playlist published and queued for elevator screens.",
      publishError: "Unable to publish playlist.",
      publishedNote:
        "Published playlists are downloaded by elevator screens and are not exposed to the player UI as drafts.",
      columnDate: "Date",
      columnScreen: "Screen",
      columnBuilding: "Building",
      columnVersion: "Version",
      columnStatus: "Status",
      columnItems: "Items",
      columnGeneratedAt: "Generated at",
      columnPublishedAt: "Published at",
      columnDownloadedAt: "Downloaded at",
      itemOrder: "Order",
      itemCampaign: "Campaign",
      itemCreative: "Creative",
      itemMediaType: "Media type",
      itemDuration: "Duration (s)",
      detailScreen: "Screen",
      detailBuilding: "Building",
      detailDate: "Date",
      detailVersion: "Version",
      detailStatus: "Status",
      detailGeneratedAt: "Generated at",
      detailPublishedAt: "Published at",
      detailItems: "Items",
      status: {
        draft: "Draft playlists are only visible in the admin and are not yet available to players.",
        published: "Published playlists have been made available to elevator screens for download.",
        downloaded: "At least one elevator screen has already downloaded this playlist.",
        expired: "This playlist is no longer the active program for the day.",
      },
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
    playsByCreative: "Plays by creative",
    pendingProofOfPlay: "Pending proof-of-play",
    totalPlays: "Total plays",
    totalPlayedSeconds: "Total played seconds",
    dateFrom: "From",
    dateTo: "To",
    applyFilter: "Apply",
    overviewSummary: "Daily overview",
    overviewDate: "Overview date",
    campaignDeliverySummary: "Campaign delivery summary",
    screenDeliverySummary: "Screen delivery summary",
    proofOfPlayEvents: "Proof-of-play events",
    campaignId: "Campaign",
    screenId: "Screen",
    creativeId: "Creative",
    playedAt: "Played at",
    playedSeconds: "Played seconds",
    plays: "Plays",
    rangeLabel: "Range",
  },
  simulator: {
    note: "This simulator forecasts scheduled daily playlist buying only. It is not OpenRTB, real-time bidding, or live next-ad serving.",
    advertiser: "Advertiser",
    advertiserPlaceholder: "Optional buyer or advertiser identifier",
    existingAdvertisers: "Known advertisers",
    dateFrom: "Date from",
    dateTo: "Date to",
    cities: "Cities",
    citiesHelp: "Use commas to target multiple cities. Leave blank to include all cities.",
    buildingTypes: "Building types",
    screenOrientations: "Screen orientations",
    creativeDurationSeconds: "Creative duration (seconds)",
    budget: "Budget",
    campaignObjective: "Campaign objective",
    objectivePlaceholder: "Awareness, launches, leasing, footfall...",
    notes: "Notes",
    notesPlaceholder: "Optional buyer notes for the simulated request",
    runForecast: "Run forecast",
    runningForecast: "Running forecast...",
    emptyTitle: "No forecast yet",
    emptyDescription: "Choose dates and targeting criteria to estimate reach, plays, cost, and capacity for a scheduled elevator playlist buy.",
    resultsTitle: "Forecast result",
    eligibleScreens: "Eligible screens",
    eligibleBuildings: "Eligible buildings",
    estimatedPlays: "Estimated plays",
    estimatedAudience: "Estimated audience",
    estimatedCost: "Estimated cost",
    availableCapacity: "Available capacity",
    warnings: "Warnings",
    conflicts: "Conflicts",
    suggestedNextAction: "Suggested next action",
    audienceEstimate: "Audience uses building-level daily estimates when available.",
    capacityEstimate: "Displayed as a percentage of placeholder unclaimed playlist capacity for the MVP.",
    optional: "Optional",
  },
  settings: {
    themePreference: "Theme preference",
    languagePreference: "Language preference",
    environmentInfo: "Environment information",
    environmentDescription:
      "Set NEXT_PUBLIC_API_BASE_URL to connect the admin frontend to the backend API.",
  },
  forms: {
    newBuilding: "New building",
    editBuilding: "Edit building",
    newScreen: "New screen",
    editScreen: "Edit screen",
    newAdvertiser: "New advertiser",
    editAdvertiser: "Edit advertiser",
    newCreative: "New creative",
    editCreative: "Edit creative",
    newCampaign: "New campaign",
    editCampaign: "Edit campaign",
    newBookingRequest: "New booking request",
    editBookingRequest: "Edit booking request",
    manageCreatives: "Manage creatives",
    assignedCreatives: "Assigned creatives",
    availableCreatives: "Available approved creatives",
    assign: "Assign",
    remove: "Remove",
    startBeforeEnd: "Start date must be before or equal to end date.",
    submit: "Submit",
    submitForReview: "Submit for review",
    approve: "Approve",
    reject: "Reject",
    save: "Save",
    cancel: "Cancel",
    saving: "Saving...",
    saved: "Saved",
    saveFailed: "Save failed",
    fieldRequired: "This field is required.",
    invalidEmail: "Enter a valid email address.",
    mustBePositive: "Value cannot be negative.",
    mustBeGreaterThanZero: "Value must be greater than zero.",
    actionFailed: "Action failed",
    edit: "Edit",
    deliveryConstraints: {
      title: "Delivery constraints",
      edit: "Edit constraints",
      save: "Save constraints",
      cities: "Cities",
      citiesHelp: "Comma-separated list. Leave empty to allow all cities.",
      buildingTypes: "Building types",
      screenOrientations: "Screen orientations",
      daysOfWeek: "Days of week",
      startTime: "Start time",
      endTime: "End time",
      timeHelp: "Leave both empty to allow all hours.",
      emptyNote: "Empty constraints mean all values are allowed.",
      loadFailed: "Unable to load delivery constraints.",
      startBeforeEnd: "Start time must be before end time.",
    },
    building: {
      name: "Name",
      city: "City",
      country: "Country",
      type: "Type",
      audience: "Estimated daily audience",
      address: "Address",
      postalCode: "Postal code",
    },
    screen: {
      name: "Name",
      buildingId: "Building",
      resolutionWidth: "Resolution width (px)",
      resolutionHeight: "Resolution height (px)",
      orientation: "Orientation",
      externalCode: "External code",
    },
    advertiser: {
      name: "Name",
      legalName: "Legal name",
      taxId: "Tax ID",
      contactName: "Contact name",
      contactEmail: "Contact email",
      phone: "Phone",
      status: "Status",
    },
    creative: {
      advertiserId: "Advertiser",
      name: "Name",
      mediaUrl: "Media URL",
      mediaType: "Media type",
      durationSeconds: "Duration (seconds)",
    },
    campaign: {
      advertiserId: "Advertiser",
      name: "Name",
      status: "Status",
      startDate: "Start date",
      endDate: "End date",
      dailyBudget: "Daily budget",
      totalBudget: "Total budget",
      maxCpm: "Max CPM",
    },
    bookingRequest: {
      advertiser: "Advertiser",
      name: "Name",
      dateFrom: "Date from",
      dateTo: "Date to",
      cities: "Cities",
      citiesHelp: "Lisbon, Porto, Madrid",
      buildingTypes: "Building types",
      screenOrientations: "Screen orientations",
      creativeDurationSeconds: "Creative duration (seconds)",
      budget: "Budget",
      campaignObjective: "Campaign objective",
      notes: "Notes",
    },
  },
};
