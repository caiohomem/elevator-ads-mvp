import { dashboardSummary, recentActivity } from "@/lib/mockData";

export {
  approveBookingRequest,
  createBookingRequest,
  generateBookingRequestForecast,
  getBookingRequest,
  getBookingRequestForecast,
  getBookingRequestsPaged,
  rejectBookingRequest,
  submitBookingRequest,
  updateBookingRequest,
} from "@/lib/api/booking-requests";
export {
  createInventoryPackage,
  deleteInventoryPackage,
  getInventoryPackageById,
  getInventoryPackageScreens,
  getInventoryPackagesPaged,
  updateInventoryPackage,
} from "@/lib/api/inventory-packages";
export {
  createAdvertiser,
  getAdvertiserById,
  getAdvertisers,
  getAdvertisersList,
  getAdvertisersPaged,
  updateAdvertiser,
} from "@/lib/api/advertisers";
export {
  createAdvertiserApiKey,
  getAdvertiserApiKeys,
  revokeAdvertiserApiKey,
} from "@/lib/api/advertiser-api-keys";
export {
  createBuilding,
  getBuildings,
  getBuildingsList,
  getBuildingsPaged,
  updateBuilding,
} from "@/lib/api/buildings";
export {
  assignCreative,
  createCampaign,
  getCampaignCreatives,
  getCampaigns,
  getCampaignsList,
  getCampaignsPaged,
  getDeliveryConstraints,
  removeCreative,
  updateCampaign,
  upsertDeliveryConstraints,
} from "@/lib/api/campaigns";
export {
  approveCreative,
  createCreative,
  getCreatives,
  getCreativesList,
  getCreativesPaged,
  rejectCreative,
  submitCreativeForReview,
  updateCreative,
} from "@/lib/api/creatives";
export {
  generatePlaylists,
  getDailyPlaylistById,
  getDailyPlaylists,
  getDailyPlaylistsList,
  getDailyPlaylistsPaged,
  publishPlaylist,
  runPlaylistSimulate,
} from "@/lib/api/playlists";
export {
  getAdvertiserCampaignReport,
} from "@/lib/api/advertiser-campaign-report";
export {
  getEstimatedProofOfPlay,
} from "@/lib/api/estimated-proof-of-play";
export {
  getProofOfPlayEvents,
  getProofOfPlayEventsPaged,
  getReportsCampaigns,
  getReportsOverview,
  getReportsScreens,
} from "@/lib/api/reports";
export { runSimulatorForecast } from "@/lib/api/simulator";
export {
  createScreen,
  getScreens,
  getScreensList,
  getScreensPaged,
  updateScreen,
} from "@/lib/api/screens";

const withLatency = async <T>(data: T): Promise<T> => Promise.resolve(data);

// TODO: No /api/dashboard-summary endpoint exists yet; keep this aggregate mocked.
export const getDashboardSummary = async () => withLatency(dashboardSummary);
export const getRecentActivity = async () => withLatency(recentActivity);
