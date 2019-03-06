import { map, mergeMap, switchMap, tap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin } from 'rxjs';

import { ConfigService } from './config.service';
import { OData } from './odata';
import { UrlResponse } from '../../models/urlResponse.model';

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

@Injectable()
export class ServicesService {
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

    constructor(private http: HttpClient, private configService: ConfigService) { }

    public initialize() {

    }

    public getIdentityAndAccessControlUrl(): Observable<UrlResponse> {
        return this.isOAuthAuthenticationEnabled.pipe(
            switchMap(isEnabled => {
              this.services.find(s => s.name === 'DiscoveryService').requireAuthToken = isEnabled              
              if(isEnabled) {
                // if OAuth, we get identity and access control up front
                return forkJoin(this.identityServiceEndpoint, this.accessControlEndpoint)
              } else {
                // If Windows Auth, we have discovery up front and have to build
                // out the service urls from discovery
                return this.buildServiceMaps().toPromise().then(identityUrl => {
                        return this.accessControlEndpoint.toPromise().then(accessControlUrl => {
                            return [identityUrl, accessControlUrl]
                        })
                    })
              }
            }),
            switchMap(urlList => of(new UrlResponse(urlList[0], urlList[1])))
        )
    }

    private baseDiscoveryServiceUrl: string;
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
        const url = this.services.find(s => s.name === 'AuthorizationService').url
        return this.trimRightChar(url, '/');
    }

    get identityProviderSearchServiceEndpoint(): string {
        const url = this.services.find(s => s.name === 'IdentityProviderSearchService').url
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

    public buildServiceMaps(): Observable<string> {
        return this.discoveryServiceEndpoint.pipe(
            map(discoveryUrl => `${discoveryUrl}/Services?$filter=` + this.buildServiceFilter() + `&$select=ServiceUrl,Version,ServiceName`),
            mergeMap(discoveryUrl => this.http.get<OData.IArray<IDiscoveryService>>(discoveryUrl, { withCredentials: true })),
            map(response => {
                for (const service of this.services) {
                    const targetService: IDiscoveryService = response.value.find(
                        s => s.ServiceName === service.name && (!service.version || s.Version === service.version)
                    );

                    if (service.name === 'DiscoveryService') {
                        service.url = null;
                    } else if (targetService === undefined) {
                        throw new Error(`The ${service.name} was not found in discovery service. Please ensure it is set up correctly.`);
                    } else {
                        service.url = targetService.ServiceUrl;
                    }
                }
                
                return this.services.find(s => s.name === 'IdentityService').url;
            })
        );
    }

    private findUrlBestMatch(url: string) {
        var serviceUrlMatch = "";
        this.services.forEach(element => {
            if (element.url) {
                var result = url.toLowerCase().includes(element.url.toLowerCase());

                if (result) {
                    if (element.url.length > serviceUrlMatch.length) {
                        serviceUrlMatch = element.url.toLowerCase();
                    }
                }
            }
        });

        var matchedService = this.services.find(s => s.url && s.url.toLowerCase() === serviceUrlMatch);
        const serviceItem: IService = matchedService;
        return serviceItem;
    }

    private trimRightChar(characters, char) {
        var i = 0;
        while (characters[characters.length - 1 - i] === char)
            i++

        return characters.substring(0, characters.length - i);
    }

    private buildServiceFilter(): string {
        return this.services.map(service => `ServiceName eq \'${service.name}\'`).join(' or ');
    }

    private parseUrl(url) {
        var location = document.createElement("a");
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
