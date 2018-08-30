import { map, mergeMap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';

import { ConfigService } from './config.service';
import { OData } from './odata';

interface IService {
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
    private services: IService[] = [
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
        }
    ];

    constructor(private http: HttpClient, private configService: ConfigService) {}

    public initialize(): Observable<string> {
        return this.configService.getDiscoveryServiceRoot().pipe(
            mergeMap(discoveryServiceRoot => {
                const url: string =
                    `${discoveryServiceRoot}/Services?$filter=` +
                    this.services.map(service => `ServiceName eq \'${service.name}\'`).join(' or ') +
                    `&$select=ServiceUrl,Version,ServiceName`;
                return this.http.get<OData.IArray<IDiscoveryService>>(url, {withCredentials: true});
            }),
            map(response => {
                for (const service of this.services) {
                    const targetService: IDiscoveryService = response.value.find(
                        s => s.ServiceName === service.name && (!service.version || s.Version === service.version)
                    );
                    if (targetService === undefined) {
                        throw new Error(`The ${service.name} was not found in discovery service. Please ensure it is set up correctly.`);
                    }
                    service.url = targetService.ServiceUrl;
                }

                return this.services.find(s => s.name === 'IdentityService').url;
            })
        );
    }

    get identityServiceEndpoint(): string {
        return this.services.find(s => s.name === 'IdentityService').url;
    }

    get authorizationServiceEndpoint(): string {
        return this.services.find(s => s.name === 'AuthorizationService').url;
    }

    get identityProviderSearchServiceEndpoint(): string {
        return this.services.find(s => s.name === 'IdentityProviderSearchService').url;
    }

    get accessControlEndpoint(): string {
        return this.services.find(s => s.name === 'AccessControl').url;
    }

    public needsAuthToken(url: string) {
        const targetService: IService = this.services.find(s => url.startsWith(s.url));
        return targetService ? targetService.requireAuthToken : false;
    }
}
