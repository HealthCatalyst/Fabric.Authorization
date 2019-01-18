import { TestBed, inject, async } from '@angular/core/testing';

import { ServicesService, IService} from './services.service';
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

  it('matches correct service requireAuthToken, when services out of order',
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
        }
      )
    )
});
