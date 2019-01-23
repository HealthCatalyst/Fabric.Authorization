import { TestBed, inject } from '@angular/core/testing';

import { ServicesService, IService } from './services.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ConfigService } from './config.service';

describe('ServicesService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ServicesService, ConfigService],
      imports: [HttpClientTestingModule]
    });
  });

  it('should be created', inject([ServicesService], (service: ServicesService) => {
    expect(service).toBeTruthy();
  }));

  it('matches correct service requireAuthToken, when Idpss service out of order, and upper case',
    inject(
      [ServicesService], (service: ServicesService) => {
        var strUrl = "https://testdomain.local/IdentityProviderSearchService/v1";
        let listOfServices: IService[] = [
          {
            name: 'IdentityService',
            version: 1.1,
            url: 'https://testdomain.local/Identity',
            requireAuthToken: false
          },
          {
            name: 'IdentityProviderSearchService',
            version: 1.2,
            url: 'https://testdomain.local/IdentityProviderSearchService/v1',
            requireAuthToken: true
          },
          {
              name: 'AuthorizationService',
              version: 1.3,
              url: 'https://testdomain.local/Authorization/v1',
              requireAuthToken: true
          },
          {
              name: 'AccessControl',
              version: 1.4,
              url: 'https://testdomain.local/Authorization',
              requireAuthToken: false
          }
      ];

        service.services = listOfServices;
        var result = service.needsAuthToken(strUrl);
        expect(result).toBeTruthy();
  }));

  it('matches correct service requireAuthToken, when Idpss service out of order, and lower case',
  inject(
    [ServicesService], (service: ServicesService) => {
      var strUrl = "https://testdomain.local/identityprovidersearchservice/v1";
      let listOfServices: IService[] = [
        {
          name: 'identityservice',
          version: 1.1,
          url: 'https://testdomain.local/identity',
          requireAuthToken: false
        },
        {
          name: 'identityprovidersearchservice',
          version: 1.2,
          url: 'https://testdomain.local/identityprovidersearchservice/v1',
          requireAuthToken: true
        },
        {
            name: 'AuthorizationService',
            version: 1.3,
            url: 'https://testdomain.local/Authorization/v1',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            version: 1.4,
            url: 'https://testdomain.local/Authorization',
            requireAuthToken: false
        }
    ];

      service.services = listOfServices;
      var result = service.needsAuthToken(strUrl);
      expect(result).toBeTruthy();
  }));

  it('matches correct service requireAuthToken, when Idpss service out of order, and mixed case',
  inject(
    [ServicesService], (service: ServicesService) => {
      var strUrl = "https://testdomain.local/IdentityProviderSearchService/v1";
      let listOfServices: IService[] = [
        {
          name: 'identityservice',
          version: 1.1,
          url: 'https://testdomain.local/identity',
          requireAuthToken: false
        },
        {
          name: 'identityprovidersearchservice',
          version: 1.2,
          url: 'https://testdomain.local/identityprovidersearchservice/v1',
          requireAuthToken: true
        },
        {
            name: 'AuthorizationService',
            version: 1.3,
            url: 'https://testdomain.local/Authorization/v1',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            version: 1.4,
            url: 'https://testdomain.local/Authorization',
            requireAuthToken: false
        }
    ];

      service.services = listOfServices;
      var result = service.needsAuthToken(strUrl);
      expect(result).toBeTruthy();
  }));

  it('matches correct service requireAuthToken, when identity service not listed first, and upper case',
  inject(
    [ServicesService], (service: ServicesService) => {
      var strUrl = "https://testdomain.local/Identity";
      let listOfServices: IService[] = [
        {
          name: 'IdentityProviderSearchService',
          version: 1.2,
          url: 'https://testdomain.local/IdentityProviderSearchService/v1',
          requireAuthToken: true
        },
        {
          name: 'IdentityService',
          version: 1.1,
          url: 'https://testdomain.local/Identity',
          requireAuthToken: false
        },
        {
            name: 'AuthorizationService',
            version: 1.3,
            url: 'https://testdomain.local/Authorization/v1',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            version: 1.4,
            url: 'https://testdomain.local/Authorization',
            requireAuthToken: false
        }
    ];

      service.services = listOfServices;
      var result = service.needsAuthToken(strUrl);
      expect(result).toBeFalsy();
  }));

  it('matches correct service requireAuthToken, when identity service not listed first, and lower case',
  inject(
    [ServicesService], (service: ServicesService) => {
      var strUrl = "https://testdomain.local/identity";
      let listOfServices: IService[] = [
        {
          name: 'identityprovidersearchservice',
          version: 1.2,
          url: 'https://testdomain.local/identityprovidersearchservice/v1',
          requireAuthToken: true
        },
        {
          name: 'identityservice',
          version: 1.1,
          url: 'https://testdomain.local/identity',
          requireAuthToken: false
        },
        {
            name: 'AuthorizationService',
            version: 1.3,
            url: 'https://testdomain.local/Authorization/v1',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            version: 1.4,
            url: 'https://testdomain.local/Authorization',
            requireAuthToken: false
        }
    ];

      service.services = listOfServices;
      var result = service.needsAuthToken(strUrl);
      expect(result).toBeFalsy();
  }));

  it('matches correct service requireAuthToken, when identity service not listed first, and mixed case',
  inject(
    [ServicesService], (service: ServicesService) => {
      var strUrl = "https://testdomain.local/Identity";
      let listOfServices: IService[] = [
        {
          name: 'identityprovidersearchservice',
          version: 1.2,
          url: 'https://testdomain.local/identityprovidersearchservice/v1',
          requireAuthToken: true
        },
        {
          name: 'identityservice',
          version: 1.1,
          url: 'https://testdomain.local/identity',
          requireAuthToken: false
        },
        {
            name: 'AuthorizationService',
            version: 1.3,
            url: 'https://testdomain.local/Authorization/v1',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            version: 1.4,
            url: 'https://testdomain.local/Authorization',
            requireAuthToken: false
        }
    ];

      service.services = listOfServices;
      var result = service.needsAuthToken(strUrl);
      expect(result).toBeFalsy();
  }));
});
