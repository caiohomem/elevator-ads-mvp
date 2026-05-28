export type BuildingType = "Residential" | "Commercial" | "MixedUse" | "Hospitality";
export type EntityStatus = "Active" | "Inactive";
export type ScreenStatus = "Active" | "Inactive" | "Offline" | "Maintenance";
export type ApprovalStatus = "Draft" | "PendingReview" | "Approved" | "Rejected";
export type CampaignStatus = "Draft" | "Scheduled" | "Active" | "Paused";
export type PlaylistStatus = "Draft" | "Published" | "Downloaded" | "Expired";

export interface Building {
  id: string;
  name: string;
  city: string;
  country: string;
  type: BuildingType;
  estimatedDailyAudience: number;
  screens: number;
  status: EntityStatus;
}

export interface Screen {
  id: string;
  name: string;
  buildingId: string;
  buildingName: string;
  resolution: string;
  orientation: "Portrait" | "Landscape";
  status: ScreenStatus;
  lastSeen: string;
  currentPlaylist: string;
}

export interface Advertiser {
  id: string;
  name: string;
  contact: string;
  email: string;
  status: EntityStatus;
  campaigns: number;
}

export interface Creative {
  id: string;
  name: string;
  advertiserId: string;
  advertiserName: string;
  mediaType: "Image" | "Video" | "HTML5";
  durationSeconds: number;
  approvalStatus: ApprovalStatus;
}

export interface Campaign {
  id: string;
  name: string;
  advertiserId: string;
  advertiserName: string;
  status: CampaignStatus;
  startDate: string;
  endDate: string;
  dailyBudget: number;
  creatives: number;
  deliveryConstraints: string;
}

export interface DailyPlaylistItem {
  id: string;
  creativeName: string;
  position: number;
  durationSeconds: number;
}

export interface DailyPlaylist {
  id: string;
  date: string;
  screenId: string;
  screenName: string;
  buildingName: string;
  version: string;
  status: PlaylistStatus;
  items: DailyPlaylistItem[];
  generatedAt: string;
  publishedAt: string | null;
  downloadedAt: string | null;
}

export interface DashboardSummary {
  buildings: number;
  screens: number;
  activeScreens: number;
  advertisers: number;
  activeCampaigns: number;
  playlistsGeneratedToday: number;
  playlistsPendingDownload: number;
  playsReportedToday: number;
}

export interface ActivityEvent {
  id: string;
  type: "playlist" | "creative" | "campaign" | "screen";
  title: string;
  detail: string;
  time: string;
}

export interface ApiBuilding {
  id: string;
  name: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  buildingType: string;
  estimatedDailyAudience: number;
  createdAt: string;
  updatedAt: string;
}

export interface ApiScreen {
  id: string;
  buildingId: string;
  name: string;
  externalCode: string;
  resolutionWidth: number;
  resolutionHeight: number;
  orientation: string;
  status: string;
  lastSeenAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApiAdvertiser {
  id: string;
  name: string;
  legalName: string;
  taxId: string;
  contactName: string;
  contactEmail: string;
  phone: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface ApiCreative {
  id: string;
  advertiserId: string;
  name: string;
  mediaUrl: string;
  mediaType: string;
  durationSeconds: number;
  approvalStatus: string;
  createdAt: string;
  updatedAt: string;
}

export interface ApiCampaign {
  id: string;
  advertiserId: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  status: string;
  dailyBudget: number | null;
  totalBudget: number | null;
  maxCpm: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApiDailyPlaylistItem {
  id: string;
  dailyPlaylistId: string;
  campaignId: string;
  creativeId: string;
  order: number;
  durationSeconds: number;
  createdAt: string;
}

export interface ApiDailyPlaylist {
  id: string;
  screenId: string;
  date: string;
  version: number;
  status: string;
  generatedAt: string;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
  items: ApiDailyPlaylistItem[];
}
