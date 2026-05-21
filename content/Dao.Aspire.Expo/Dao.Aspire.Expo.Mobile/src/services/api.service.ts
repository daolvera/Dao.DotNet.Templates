const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5000';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string | null;
}

export async function getWeatherForecast(): Promise<WeatherForecast[]> {
  const response = await fetch(`${API_BASE_URL}/api/weatherforecast`);
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }
  return response.json() as Promise<WeatherForecast[]>;
}
