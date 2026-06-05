export type BuildingType = "Residential" | "Commercial" | "MixedUse" | "Hospitality";
export type EntityStatus = "Active" | "Inactive";
export type ScreenStatus = "Active" | "Inactive" | "Offline" | "Maintenance";
export type ApprovalStatus = "Draft" | "PendingReview" | "Approved" | "Rejected";
export type CampaignStatus = "Draft" | "Scheduled" | "Active" | "Paused";
export type BookingRequestStatus =
  | "Draft"
  | "Submitted"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "ConvertedToCampaign";
export type PlaylistStatus = "Draft" | "Published" | "Downloaded" | "Expired";
export type InventoryPackageStatus = "Active" | "Inactive";
export type ApiKeyStatus = "Active" | "Revoked" | "Expired";

export interface PagedQuery {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  search?: string;
  status?: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

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
  campaignName: string;
  mediaType: string;
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

export interface ApiAdvertiserApiKey {
  id: string;
  advertiserId: string;
  name: string;
  keyPrefix: string;
  scopes: string[];
  status: ApiKeyStatus;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
  revokedAt: string | null;
}

export interface CreateApiKeyPayload {
  name: string;
  scopes: string[];
  expiresAt?: string | null;
}

export interface CreateApiKeyResponse extends ApiAdvertiserApiKey {
  plainApiKey: string;
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

export interface ApiBookingRequest {
  id: string;
  advertiserId: string;
  name: string;
  dateFrom: string;
  dateTo: string;
  cities: string[];
  buildingTypes: DeliveryBuildingType[];
  screenOrientations: DeliveryScreenOrientation[];
  creativeDurationSeconds: number;
  budget: number;
  campaignObjective: string;
  notes: string;
  status: BookingRequestStatus;
  createdAt: string;
  updatedAt: string;
}

export interface ApiCampaignForecast {
  id: string;
  bookingRequestId: string;
  eligibleScreens: number;
  eligibleBuildings: number;
  estimatedPlays: number;
  estimatedAudience: number;
  estimatedCost: number;
  availableCapacity: number;
  warnings: string[];
  conflicts: string[];
  createdAt: string;
  updatedAt: string;
}

export interface ApiInventoryPackage {
  id: string;
  name: string;
  description: string;
  cities: string[];
  buildingTypes: DeliveryBuildingType[];
  screenOrientations: DeliveryScreenOrientation[];
  screenIds: string[];
  buildingIds: string[];
  baseCpm: number;
  status: InventoryPackageStatus;
  createdAt: string;
  updatedAt: string;
}

export interface SimulatorForecastRequest {
  advertiserId?: string | null;
  dateFrom: string;
  dateTo: string;
  cities?: string[] | null;
  buildingTypes?: DeliveryBuildingType[] | null;
  screenOrientations?: DeliveryScreenOrientation[] | null;
  creativeDurationSeconds: number;
  budget?: number | null;
  campaignObjective?: string | null;
  notes?: string | null;
}

export interface SimulatorForecastResponse {
  eligibleScreens: number;
  eligibleBuildings: number;
  estimatedPlays: number;
  estimatedAudience: number;
  estimatedCost: number;
  availableCapacity: number;
  warnings: string[];
  conflicts: string[];
  suggestedNextAction: string;
}

export interface PlaylistSimulateRequest {
  bookingRequestId?: string | null;
  campaignId?: string | null;
  inventoryPackageId?: string | null;
  date: string;
  screenIds?: string[] | null;
  creativeDurationSeconds: number;
  operatingHoursPerDay: number;
  maxLoopDurationSeconds?: number | null;
}

export interface PlaylistSimulateItem {
  order: number;
  campaignId: string | null;
  creativeId: string | null;
  creativeDurationSeconds: number;
  source: string;
  notes: string | null;
}

export interface PlaylistSimulateResponse {
  date: string;
  eligibleScreens: number;
  eligibleBuildings: number;
  loopDurationSeconds: number;
  estimatedLoopsPerDay: number;
  estimatedPlaysPerCreative: number;
  estimatedTotalPlays: number;
  estimatedAudience: number;
  items: PlaylistSimulateItem[];
  warnings: string[];
  conflicts: string[];
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

export type DeliveryBuildingType = "Residential" | "Corporate" | "Commercial" | "MixedUse" | "Other";
export type DeliveryScreenOrientation = "Landscape" | "Portrait";
export type DeliveryDayOfWeek = "Sunday" | "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday";

export const DELIVERY_BUILDING_TYPES: readonly DeliveryBuildingType[] = [
  "Residential",
  "Corporate",
  "Commercial",
  "MixedUse",
  "Other",
] as const;

export const DELIVERY_SCREEN_ORIENTATIONS: readonly DeliveryScreenOrientation[] = ["Landscape", "Portrait"] as const;

export const DELIVERY_DAYS_OF_WEEK: readonly DeliveryDayOfWeek[] = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
] as const;

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

export interface GroupSummary {
  id: string;
  name: string;
  plays: number;
  playedSeconds: number;
}

export interface OverviewReport {
  date: string;
  totalPlays: number;
  totalPlayedSeconds: number;
  byCampaign: GroupSummary[];
  byScreen: GroupSummary[];
  byCreative: GroupSummary[];
}

export interface CampaignReport {
  from: string;
  to: string;
  totalPlays: number;
  totalPlayedSeconds: number;
  campaigns: GroupSummary[];
}

export interface ScreenReport {
  from: string;
  to: string;
  totalPlays: number;
  totalPlayedSeconds: number;
  screens: GroupSummary[];
}

export interface EstimatedProofOfPlayItem {
  date: string;
  screenId: string;
  screenName: string;
  buildingId: string;
  buildingName: string;
  city: string;
  creativeId: string;
  creativeName: string;
  scheduledPlays: number;
  reportedPlays: number;
  estimatedAudience: number;
  estimatedImpressions: number;
}

export interface EstimatedProofOfPlayReport {
  campaignId: string;
  campaignName: string;
  advertiserId: string;
  advertiserName: string;
  dateFrom: string;
  dateTo: string;
  totalScheduledPlays: number;
  totalReportedPlays: number;
  estimatedAudience: number;
  estimatedImpressions: number;
  screensCount: number;
  buildingsCount: number;
  cities: string[];
  items: EstimatedProofOfPlayItem[];
  assumptions: string[];
  warnings: string[];
}

export interface AdvertiserCampaignCreativeSummary {
  creativeId: string;
  creativeName: string;
  mediaType: string;
  durationSeconds: number;
  totalPlays: number;
  estimatedImpressions: number;
}

export interface AdvertiserCampaignDailyBreakdown {
  date: string;
  totalPlays: number;
  estimatedAudience: number;
  estimatedImpressions: number;
  screensCount: number;
  buildingsCount: number;
}

export interface AdvertiserCampaignReport {
  advertiserId: string;
  advertiserName: string;
  campaignId: string;
  campaignName: string;
  dateFrom: string;
  dateTo: string;
  status: string;
  totalPlays: number;
  totalScheduledPlays: number;
  totalReportedPlays: number;
  estimatedAudience: number;
  estimatedImpressions: number;
  screensCount: number;
  buildingsCount: number;
  cities: string[];
  creatives: AdvertiserCampaignCreativeSummary[];
  dailyBreakdown: AdvertiserCampaignDailyBreakdown[];
  assumptions: string[];
  warnings: string[];
}

export interface ProofOfPlayEvent {
  id: string;
  screenId: string;
  screenName: string;
  playlistId: string;
  playlistItemId: string;
  campaignId: string;
  campaignName: string;
  creativeId: string;
  creativeName: string;
  playedAt: string;
  durationSeconds: number;
  createdAt: string;
}
