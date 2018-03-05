export class AuthMemberSearchRequest {

    public pageNumber?: number;
    public pageSize?: number;
    public filter: string;
    public sortKey: string;
    public sortDirection: string;
    public clientId: string;
}