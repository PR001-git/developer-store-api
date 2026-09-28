import { API_URL } from './api';

/** Fails fast, with the fix, when the API isn't up. */
export default async function globalSetup(): Promise<void> {
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try {
      if ((await fetch(`${API_URL}/health/ready`, { signal: AbortSignal.timeout(5_000) })).ok) {
        return;
      }
    } catch {
      // Not listening yet.
    }
    await new Promise(resolve => setTimeout(resolve, 1_000));
  }
  throw new Error(`The API isn't answering at ${API_URL}/health/ready. From the repo root, run: docker compose up -d --build`);
}
