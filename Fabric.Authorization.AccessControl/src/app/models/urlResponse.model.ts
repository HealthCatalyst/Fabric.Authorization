export class UrlResponse {
    constructor(identityUrl, accessControlUrl) {
        this.identityUrl = identityUrl;
        this.accessControlUrl = accessControlUrl;
    }

    public identityUrl: string;
    public accessControlUrl: string;
}
