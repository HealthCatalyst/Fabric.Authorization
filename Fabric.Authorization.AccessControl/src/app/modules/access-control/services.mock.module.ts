import { NgModule } from '@angular/core';

import { FabricExternalIdpSearchService } from '../../services/fabric-external-idp-search.service';
import { FabricAuthRoleService } from '../../services/fabric-auth-role.service';
import { FabricAuthUserService } from '../../services/fabric-auth-user.service';
import { FabricAuthGroupService } from '../../services/fabric-auth-group.service';
import { FabricAuthMemberSearchService } from '../../services/fabric-auth-member-search.service';
import { FabricAuthGrainService } from '../../services/fabric-auth-grain.service';
import { MockAccessControlConfigService } from '../../services/access-control-config.service.mock';
import { FabricAuthEdwAdminService } from '../../services/fabric-auth-edwadmin.service';
import { FabricAuthEdwAdminServiceMock } from '../../services/fabric-auth-edwadmin.service.mock';

import { FabricExternalIdpSearchServiceMock } from '../../services/fabric-external-idp-search.service.mock';
import { FabricAuthUserServiceMock } from '../../services/fabric-auth-user.service.mock';
import { FabricAuthGroupServiceMock } from '../../services/fabric-auth-group.service.mock';
import { RouterTestingModule } from '@angular/router/testing';
import { FabricAuthMemberSearchServiceMock } from '../../services/fabric-auth-member-search.service.mock';
import { FabricAuthRoleServiceMock } from '../../services/fabric-auth-role.service.mock';
import { FabricAuthGrainServiceMock } from '../../services/fabric-auth-grain.service.mock';
import { CurrentUserServiceMock } from '../../services/current-user.service.mock';
import { CurrentUserService } from '../../services/current-user.service';

@NgModule({
    providers: [
        {
            provide: 'IAccessControlConfigService',
            useValue: MockAccessControlConfigService
        },
        {
            provide: FabricExternalIdpSearchService,
            useClass: FabricExternalIdpSearchServiceMock
        },
        {
            provide: FabricAuthRoleService,
            useClass: FabricAuthRoleServiceMock
        },
        {
            provide: FabricAuthUserService,
            useClass: FabricAuthUserServiceMock
        },
        {
            provide: FabricAuthGroupService,
            useClass: FabricAuthGroupServiceMock
        },
        {
            provide: FabricAuthMemberSearchService,
            useClass: FabricAuthMemberSearchServiceMock
        },
        {
            provide: FabricAuthGrainService,
            useClass: FabricAuthGrainServiceMock
        },
        {
          provide: FabricAuthEdwAdminService,
          useClass: FabricAuthEdwAdminServiceMock
        },
        {
            provide: CurrentUserService,
            useClass: CurrentUserServiceMock
        }
    ],
    imports: [RouterTestingModule]
})
export class ServicesMockModule { }
