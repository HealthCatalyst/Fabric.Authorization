import { AuthMemberSearchResult } from './authMemberSearchResult.model';

export type SortDirection = 'asc' | 'ascending' | 'desc' | 'descending';
export type SortKey = 'name' | 'role' | 'lastlogin' | 'subjectid';

export interface AuthMemberSearchRequest {
  pageNumber?: number;
  pageSize?: number;
  filter?: string;
  sortKey?: SortKey;
  sortDirection?: SortDirection;

  /** if omitted, must provide both `grain` and `securableItem` */
  clientId?: string;
  /** if provided, must also provide `securableItem` */
  grain?: string;
  /** if provided, must also provide `grain` */
  securableItem?: string;
}
