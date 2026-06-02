import { dashboardSummary, recentActivity } from "@/lib/mockData";

export { getAdvertisers } from "@/lib/api/advertisers";
export { getBuildings } from "@/lib/api/buildings";
export { getCampaigns } from "@/lib/api/campaigns";
export { getCreatives } from "@/lib/api/creatives";
export { getDailyPlaylists } from "@/lib/api/playlists";
export { getScreens } from "@/lib/api/screens";

const withLatency = async <T>(data: T): Promise<T> => Promise.resolve(data);

// TODO: No /api/dashboard-summary endpoint exists yet; keep this aggregate mocked.
export const getDashboardSummary = async () => withLatency(dashboardSummary);
export const getRecentActivity = async () => withLatency(recentActivity);
