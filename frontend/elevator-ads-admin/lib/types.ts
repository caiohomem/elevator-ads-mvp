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
