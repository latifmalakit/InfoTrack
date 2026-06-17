import type { SearchRunListItem, SearchRunReport, SearchRunRequest } from '../types';

const jsonHeaders = {
  'Content-Type': 'application/json'
};

export async function getDefaultLocations(): Promise<string[]> {
  const response = await fetch('/api/locations/defaults');
  await assertSuccess(response);
  const payload = (await response.json()) as { locations: string[] };
  return payload.locations;
}

export async function runSearch(request: SearchRunRequest): Promise<SearchRunReport> {
  const response = await fetch('/api/search-runs', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request)
  });

  await assertSuccess(response);
  return (await response.json()) as SearchRunReport;
}

export async function getRecentSearchRuns(): Promise<SearchRunListItem[]> {
  const response = await fetch('/api/search-runs/recent');
  await assertSuccess(response);
  return (await response.json()) as SearchRunListItem[];
}

export async function getSearchRun(runId: string): Promise<SearchRunReport> {
  const response = await fetch(`/api/search-runs/${encodeURIComponent(runId)}`);
  await assertSuccess(response);
  return (await response.json()) as SearchRunReport;
}

async function assertSuccess(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }

  let detail = `${response.status} ${response.statusText}`;
  try {
    const problem = (await response.json()) as { detail?: string; title?: string };
    detail = problem.detail ?? problem.title ?? detail;
  } catch {
    // Keep the HTTP status as the error when the server does not return ProblemDetails
  }

  throw new Error(detail);
}
