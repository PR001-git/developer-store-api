import type { AuthenticateResponse } from '../api/api-models';

export const SESSION_STORAGE_KEY = 'ds.session';

export interface Session {
  readonly token: string;
  readonly userId: string;
  readonly name: string;
  readonly email: string;
  readonly role: string;
  /** Milliseconds since the epoch, from the token's `exp`. */
  readonly expiresAt: number;
}

// JwtSecurityTokenHandler writes ClaimTypes.NameIdentifier as `nameid`; the others are fallbacks.
const USER_ID_CLAIMS = ['nameid', 'sub', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

export function decodeJwtPayload(token: string): Record<string, unknown> {
  const part = token.split('.')[1];
  if (!part) {
    throw new Error('The token is not a JWT.');
  }
  const base64 = part.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(part.length / 4) * 4, '=');
  const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
  return JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>;
}

export function sessionFromAuthResponse(response: AuthenticateResponse): Session {
  const payload = decodeJwtPayload(response.token);
  const userId = USER_ID_CLAIMS.map(claim => payload[claim]).find((value): value is string => typeof value === 'string');
  const expiry = payload['exp'];
  if (!userId || typeof expiry !== 'number') {
    throw new Error('The token has no user id or expiry.');
  }
  return {
    token: response.token,
    userId,
    name: response.name,
    email: response.email,
    role: response.role,
    expiresAt: expiry * 1000,
  };
}

export function parseStoredSession(json: string | null): Session | null {
  if (json === null) {
    return null;
  }
  try {
    const value = JSON.parse(json) as Record<string, unknown>;
    const strings = ['token', 'userId', 'name', 'email', 'role'].every(key => typeof value[key] === 'string');
    return strings && typeof value['expiresAt'] === 'number' ? (value as unknown as Session) : null;
  } catch {
    return null;
  }
}
