export class AuthSearchResult {

    public subjectId: string;
    public identityProvider: string;
    public roles: Array<string>;
    public groupName: string;
    public firstName: string;
    public middleName: string;
    public lastName: string;
    public lastLoginDateTimeUtc: string | Date;
    public entityType: string;

    constructor() { }
}