import { APIRequestContext, expect, request } from '@playwright/test';
import type {
  ApiResponseWithData, AuthenticateResponse, CreateSaleRequest, Sale,
} from '../../src/app/core/api/api-models';

export const API_URL = process.env['API_URL'] ?? 'http://localhost:8080';
export const TEST_PASSWORD = 'Portal@2026';

export interface TestUser {
  readonly username: string;
  readonly email: string;
  readonly phone: string;
  readonly password: string;
}

const unique = (): string => `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 7)}`;

export function newTestUser(tag = 'e2e'): TestUser {
  const id = unique();
  return {
    username: `${tag}-${id}`.slice(0, 50),
    email: `${tag}.${id}@developerstore.test`,
    phone: `+5511${String(Math.floor(Math.random() * 1e9)).padStart(9, '0')}`,
    password: TEST_PASSWORD,
  };
}

/** Talks to the API directly (no browser, no CORS) to set up and inspect data. */
export class ApiClient {
  private token = '';

  private constructor(private readonly context: APIRequestContext) {}

  static async create(): Promise<ApiClient> {
    return new ApiClient(await request.newContext({ baseURL: API_URL }));
  }

  async signUp(user: TestUser): Promise<number> {
    const response = await this.context.post('/api/users', {
      data: { ...user, status: 'Active', role: 'Customer' },
    });
    expect(response.status(), await response.text()).toBe(201);
    return response.status();
  }

  async signIn(user: TestUser): Promise<AuthenticateResponse> {
    const response = await this.context.post('/api/auth', { data: { email: user.email, password: user.password } });
    expect(response.status(), await response.text()).toBe(200);
    const body = (await response.json()) as ApiResponseWithData<AuthenticateResponse>;
    this.token = body.data.token;
    return body.data;
  }

  async createSale(sale: CreateSaleRequest): Promise<Sale> {
    const response = await this.context.post('/api/sales', { data: sale, headers: this.auth() });
    expect(response.status(), await response.text()).toBe(201);
    return ((await response.json()) as ApiResponseWithData<Sale>).data;
  }

  async cancelSale(id: string): Promise<void> {
    const response = await this.context.patch(`/api/sales/${id}/cancel`, { headers: this.auth() });
    expect(response.status(), await response.text()).toBe(200);
  }

  async dispose(): Promise<void> {
    await this.context.dispose();
  }

  private auth(): Record<string, string> {
    return { Authorization: `Bearer ${this.token}` };
  }
}
