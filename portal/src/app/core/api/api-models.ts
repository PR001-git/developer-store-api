/** Response and request shapes of the Sales API, as its controllers and DTOs define them. Enums travel as strings. */

export type UserRole = 'None' | 'Customer' | 'Manager' | 'Admin';
export type UserStatus = 'Unknown' | 'Active' | 'Inactive' | 'Suspended';

export interface ApiResponse {
  readonly success: boolean;
  readonly message: string;
}

export interface ApiResponseWithData<T> extends ApiResponse {
  readonly data: T;
}

export interface PaginatedResponse<T> extends ApiResponseWithData<T[]> {
  readonly currentPage: number;
  readonly totalPages: number;
  readonly totalItems: number;
}

export interface ApiErrorBody {
  readonly type: string;
  readonly error: string;
  readonly detail: string;
}

export interface AuthenticateRequest {
  readonly email: string;
  readonly password: string;
}

export interface AuthenticateResponse {
  readonly token: string;
  readonly email: string;
  readonly name: string;
  readonly role: string;
}

export interface CreateUserRequest {
  readonly username: string;
  readonly password: string;
  readonly phone: string;
  readonly email: string;
  readonly status: UserStatus;
  readonly role: UserRole;
}

export interface User {
  readonly id: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string;
  readonly role: UserRole;
  readonly status: UserStatus;
}

export interface SaleItemRequest {
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPrice: number;
}

export interface UpdateSaleRequest {
  readonly saleDate: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly branchId: string;
  readonly branchName: string;
  readonly items: readonly SaleItemRequest[];
}

export interface CreateSaleRequest extends UpdateSaleRequest {
  readonly saleNumber?: string;
}

export interface SaleItem {
  readonly id: string;
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly discountPercentage: number;
  readonly discountAmount: number;
  readonly totalAmount: number;
  readonly isCancelled: boolean;
}

export interface Sale {
  readonly id: string;
  readonly saleNumber: string;
  readonly saleDate: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly branchId: string;
  readonly branchName: string;
  readonly totalAmount: number;
  readonly isCancelled: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
  readonly items: readonly SaleItem[];
}

export interface SalesPage {
  readonly sales: readonly Sale[];
  readonly currentPage: number;
  readonly totalPages: number;
  readonly totalItems: number;
}
