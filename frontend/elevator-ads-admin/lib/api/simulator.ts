import { apiMutate, type ApiResult } from "@/lib/api/client";
import type { SimulatorForecastRequest, SimulatorForecastResponse } from "@/lib/types";

const simulatorForecastEndpoint = "/api/programmatic/simulator/forecast";

export async function runSimulatorForecast(
  payload: SimulatorForecastRequest,
): Promise<ApiResult<SimulatorForecastResponse>> {
  return apiMutate<SimulatorForecastRequest, SimulatorForecastResponse>(simulatorForecastEndpoint, "POST", payload);
}
