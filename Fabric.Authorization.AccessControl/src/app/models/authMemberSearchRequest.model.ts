export interface IAuthMemberSearchRequest {
  pageSize?: number;
  pageNumber?: number;
  filter?: string;
  sortKey?: string;
  sortDirection?: string;
  clientId?: string;
  grain: string;
  securableItem: string;
}
