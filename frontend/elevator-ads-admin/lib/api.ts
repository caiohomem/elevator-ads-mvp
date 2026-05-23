import {
  advertisers,
  buildings,
  campaigns,
  creatives,
  dailyPlaylists,
  dashboardSummary,
  recentActivity,
  screens,
} from "@/lib/mockData";

const withLatency = async <T>(data: T): Promise<T> => Promise.resolve(data);

export const getBuildings = async () => withLatency(buildings);
export const getScreens = async () => withLatency(screens);
export const getAdvertisers = async () => withLatency(advertisers);
export const getCreatives = async () => withLatency(creatives);
export const getCampaigns = async () => withLatency(campaigns);
export const getDailyPlaylists = async () => withLatency(dailyPlaylists);
export const getDashboardSummary = async () => withLatency(dashboardSummary);
export const getRecentActivity = async () => withLatency(recentActivity);
