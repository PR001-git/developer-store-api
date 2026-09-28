import { describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { parseStoredSession, sessionFromAuthResponse } from './session';

const USER_ID = '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11';
const response = (payload: Record<string, unknown>) =>
  ({ token: fakeJwt(payload), email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer' });

describe('sessionFromAuthResponse', () => {
  it('takes the user id and expiry from the token', () => {
    const session = sessionFromAuthResponse(response({ nameid: USER_ID, exp: 1_790_000_000 }));

    expect(session).toEqual({
      token: expect.any(String), userId: USER_ID, name: 'Ana Souza',
      email: 'ana@developerstore.test', role: 'Customer', expiresAt: 1_790_000_000_000,
    });
  });

  it('accepts the long name-identifier claim too', () => {
    const claim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    expect(sessionFromAuthResponse(response({ [claim]: USER_ID, exp: 1 })).userId).toBe(USER_ID);
  });

  it('rejects a token without a user id or expiry', () => {
    expect(() => sessionFromAuthResponse(response({ exp: 1 }))).toThrow();
    expect(() => sessionFromAuthResponse(response({ nameid: USER_ID }))).toThrow();
  });
});

describe('parseStoredSession', () => {
  it('reads back what it stored', () => {
    const session = sessionFromAuthResponse(response({ nameid: USER_ID, exp: 1 }));
    expect(parseStoredSession(JSON.stringify(session))).toEqual(session);
  });

  it('ignores anything that is not a session', () => {
    expect(parseStoredSession(null)).toBeNull();
    expect(parseStoredSession('{not json')).toBeNull();
    expect(parseStoredSession('{"token":1}')).toBeNull();
  });
});
