import { dashboardSummary, recentActivity } from "@/lib/mockData";

export {
  createAdvertiser,
  getAdvertisers,
  getAdvertisersList,
  updateAdvertiser,
} from "@/lib/api/advertisers";
export { createBuilding, getBuildings, getBuildingsList, updateBuilding } from "@/lib/api/buildings";
export {
  assignCreative,
  createCampaign,
  getCampaignCreatives,
  getCampaigns,
  getCampaignsList,
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
  rejectCreative,
  submitCreativeForReview,
  updateCreative,
} from "@/lib/api/creatives";
export { getDailyPlaylists } from "@/lib/api/playlists";
export { createScreen, getScreens, getScreensList, updateScreen } from "@/lib/api/screens";

const withLatency = async <T>(data: T): Promise<T> => Promise.resolve(data);

// TODO: No /api/dashboard-summary endpoint exists yet; keep this aggregate mocked.
export const getDashboardSummary = async () => withLatency(dashboardSummary);
export const getRecentActivity = async () => withLatency(recentActivity);
