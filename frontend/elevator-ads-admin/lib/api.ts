import { dashboardSummary, recentActivity } from "@/lib/mockData";

export {
  createAdvertiser,
  getAdvertisers,
  getAdvertisersList,
  getAdvertisersPaged,
  updateAdvertiser,
} from "@/lib/api/advertisers";
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
} from "@/lib/api/playlists";
export {
  getProofOfPlayEvents,
  getProofOfPlayEventsPaged,
  getReportsCampaigns,
  getReportsOverview,
  getReportsScreens,
} from "@/lib/api/reports";
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
