export interface LoginInput {
  userName: string;
  password: string;
}
export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  userName: string;
  role: string;
  expiresAt: string;
  accessTokenExpiresAt: string;
}
export interface CurrentUserDto {
  userId: string;
  userName: string;
  roles: string[];
  tenantId: string;
  depotName: string;
}
export interface StoredSession {
  userName: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}
