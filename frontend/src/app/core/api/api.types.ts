export interface ApiPage<T> {
  items: T[];
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
export type ApiParams = Record<string, string | number | boolean | undefined | null>;
export function queryParams(params: ApiParams): Record<string, string> {
  return Object.fromEntries(Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== '')
    .map(([key, value]) => [key, String(value)]));
}
