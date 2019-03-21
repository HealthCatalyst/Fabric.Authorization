import { map, mergeMap, switchMap, tap } from 'rxjs/operators';
import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';

import { ConfigService } from './config.service';
import { OData } from './odata';
import { IAuthService } from './auth.service';

export interface IService {
    name: string;
    version?: number;
    url?: string;
    requireAuthToken: boolean;
}

interface IDiscoveryService {
    ServiceUrl: string;
    ServiceName: string;
    Version: number;
}

@Injectable(
    { providedIn: 'root' }
)
export class ServicesService {
    private baseDiscoveryServiceUrl: string;
    public services: IService[] = [
        {
            name: 'IdentityService',
            requireAuthToken: false
        },
        {
            name: 'AuthorizationService',
            requireAuthToken: true
        },
        {
            name: 'IdentityProviderSearchService',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            requireAuthToken: false
        },
        {
            name: 'DiscoveryService',
            requireAuthToken: true
        }
    ];

    constructor(
        private http: HttpClient,
        private configService: ConfigService,
        @Inject('IAuthService')private authService: IAuthService
    ) { }

    public initialize() {
        return this.isOAuthAuthenticationEnabled.subscribe(isEnabled => {
            this.services.find(s => s.name === 'DiscoveryService').requireAuthToken = isEnabled;
            return this.buildServiceMaps();
        });
    }

    get discoveryServiceEndpoint(): Observable<string> {
        if (this.baseDiscoveryServiceUrl === undefined) {
            // if discovery service url is pushed to the website by the window.location, then use that
            // if we get identity service url instead, then visit this website, grab the discovery_uri from
            // identity instead.
            return this.configService.getDiscoveryServiceRoot()
            .pipe(switchMap(url => url === '' ? this.discoveryServiceUrlFromIdentity : of(url)),
                    map(url => this.trimRightChar(url, '/')),
                    tap(url => this.baseDiscoveryServiceUrl = url));
        }

        return of(this.baseDiscoveryServiceUrl);
    }

    get isOAuthAuthenticationEnabled(): Observable<boolean> {
        return this.configService.getUseOAuthAuthentication().pipe(
            switchMap(stringBool => stringBool === 'true' ? of(true) : of(false))
        )
    }

    get discoveryServiceUrlFromIdentity(): Observable<string> {
        return this.configService.getIdentityServiceRoot()
            .pipe(map((url) => `${url}/.well-known/openid-configuration`),
                switchMap((url) => this.http.get(url)),
                map((response: any) => this.trimRightChar(response.discovery_uri, '/')));
    }


    get identityServiceEndpoint(): Observable<string> {
        return this.configService.getIdentityServiceRoot().pipe(
            map(url => this.trimRightChar(url, '/'))
        )
    }

    get authorizationServiceEndpoint(): string {
        const url = this.services.find(s => s.name === 'AuthorizationService').url;
        return this.trimRightChar(url, '/');
    }

    get identityProviderSearchServiceEndpoint(): string {
        const url = this.services.find(s => s.name === 'IdentityProviderSearchService').url;
        return this.trimRightChar(url, '/');
    }

    get accessControlEndpoint(): Observable<string> {
        return this.configService.getAccessControlServiceRoot().pipe(
            map(url => this.trimRightChar(url, '/'))
        )
    }

    public needsAuthToken(url: string) {
        // by getting just the path name, this strips out the host
        // and any query string parameters that will affect the results.
        // it was needed because discovery service urls have other services in them
        const urlLowerCase = this.parseUrl(url.toLowerCase()).pathname;
        // find the service with this url
        // if you cannot, then find the best match
        const service: IService = this.services.find(s => urlLowerCase.includes(s.name.toLowerCase()));
        const targetService: IService = service ? service : this.findUrlBestMatch(url);

        // take that service, see if it requires Authentication Token
        return targetService ? targetService.requireAuthToken : false;
    }

    private buildServiceMaps() {
        return this.authService.isUserAuthenticated().then(result => {
            if (result) {
                return this.discoveryServiceEndpoint.subscribe(discoveryUrl => {
                    const requestUrl = `${discoveryUrl}/Services?$filter=` + this.buildServiceFilter() +
                        `&$select=ServiceUrl,Version,ServiceName`;
                    return this.makeDiscoveryRequest(requestUrl);
                });
            }
        });
    }

    private makeDiscoveryRequest(requestUrl: string) {
        return this.isOAuthAuthenticationEnabled.subscribe(isEnabled => {
            let discoveryRequest;
            if (isEnabled) {
                discoveryRequest = this.http.get<OData.IArray<IDiscoveryService>>(requestUrl);
            } else {
                discoveryRequest = this.http.get<OData.IArray<IDiscoveryService>>(requestUrl, { withCredentials: true });
            }

            return discoveryRequest.subscribe(response => {
                for (const service of this.services) {
                    const targetService: IDiscoveryService = response.value.find(
                        s => s.ServiceName === service.name && (!service.version || s.Version === service.version)
                    );

                    if (service.name === 'DiscoveryService') {
                        service.url = null;
                    } else if (targetService === undefined) {
                        throw new Error(
                            `The ${service.name} was not found in discovery service. Please ensure it is set up correctly.`
                        );
                    } else {
                        service.url = targetService.ServiceUrl;
                    }
                }
            });
        });
    }

    private findUrlBestMatch(url: string) {
        let serviceUrlMatch = '';
        this.services.forEach(element => {
            if (element.url) {
                const result = url.toLowerCase().includes(element.url.toLowerCase());

                if (result) {
                    if (element.url.length > serviceUrlMatch.length) {
                        serviceUrlMatch = element.url.toLowerCase();
                    }
                }
            }
        });

        const matchedService = this.services.find(s => s.url && s.url.toLowerCase() === serviceUrlMatch);
        const serviceItem: IService = matchedService;
        return serviceItem;
    }

    private trimRightChar(characters, char) {
        if (characters === undefined || characters === null) {
            return characters;
        }

        let i = 0;
        while (characters[characters.length - 1 - i] === char) {
            i++;
        }

        return characters.substring(0, characters.length - i);
    }

    private buildServiceFilter(): string {
        return this.services.map(service => `ServiceName eq \'${service.name}\'`).join(' or ');
    }

    private parseUrl(url) {
        const location = document.createElement('a');
        location.href = url;
        // IE doesn't populate all link properties when setting .href with a relative URL,
        // however .href will return an absolute URL which then can be used on itself
        // to populate these additional fields.
        if (location.host == "") {
            location.href = location.href;
        }

        return location;
    }
}
