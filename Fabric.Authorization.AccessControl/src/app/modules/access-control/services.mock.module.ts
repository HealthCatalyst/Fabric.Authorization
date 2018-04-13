import { NgModule } from '@angular/core';

import { HttpClientTestingModule } from '@angular/common/http/testing';
import { IAccessControlConfigService } from '../../services/access-control-config.service';
import { FabricExternalIdpSearchService } from '../../services/fabric-external-idp-search.service';
import { FabricAuthRoleService } from '../../services/fabric-auth-role.service';
import { FabricAuthUserService } from '../../services/fabric-auth-user.service';
import { FabricAuthGroupService } from '../../services/fabric-auth-group.service';
import { FabricAuthMemberSearchService } from '../../services/fabric-auth-member-search.service';
import { ClientAccessControlConfigService } from '../../services/global/client-access-control-config.service';
import { IDataChangedEventArgs } from '../../models/changedDataEventArgs.model';
import { MockAccessControlConfigService } from '../../services/access-control-config.service.mock';
import { FabricExternalIdpSearchServiceMock } from '../../services/fabric-external-idp-search.service.mock';
import { FabricAuthUserServiceMock } from '../../services/fabric-auth-user.service.mock';
import { FabricAuthGroupServiceMock } from '../../services/fabric-auth-group.service.mock';
import { RouterTestingModule } from '@angular/router/testing';
import { FabricAuthMemberSearchServiceMock } from '../../services/fabric-auth-member-search.service.mock';
import { FabricAuthRoleServiceMock } from '../../services/fabric-auth-role.service.mock';

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
        }
    ],
    imports: [RouterTestingModule]
})
export class ServicesMockModule { }
