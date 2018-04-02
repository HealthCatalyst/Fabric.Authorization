import { NgModule } from '@angular/core';

import { HttpClientTestingModule } from '@angular/common/http/testing';
import {
    FabricAuthRoleService,
    FabricExternalIdpSearchService,
    FabricAuthUserService,
    AccessControlConfigService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService
} from '../../../services';
import { FabricExternalIdpSearchServiceMock } from '../../../services/fabric-external-idp-search.service.mock';
import { FabricAuthUserServiceMock } from '../../../services/fabric-auth-user.service.mock';
import { FabricAuthGroupServiceMock } from '../../../services/fabric-auth-group.service.mock';
import { RouterTestingModule } from '@angular/router/testing';
import { FabricAuthMemberSearchServiceMock } from '../../../services/fabric-auth-member-search.service.mock';
import { FabricAuthRoleServiceMock } from '../../../services/fabric-auth-role.service.mock';

@NgModule({
    providers: [
        AccessControlConfigService,
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
        }
    ],
    imports: [RouterTestingModule]
})
export class ServicesMockModule { }
