import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';

import { Role } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthRoleService extends FabricBaseService {

  public static BaseRoleApiUrl;
  

  constructor(httpClient: HttpClient, accessControlConfigService: AccessControlConfigService) {
    super(httpClient, accessControlConfigService);

    if(!FabricAuthRoleService.BaseRoleApiUrl){
      FabricAuthRoleService.BaseRoleApiUrl = `${accessControlConfigService.getFabricExternalIdpSearchApiUrl()}/roles`;
    }
    
  }

  getRolesBySecurableItemAndGrain(grain: string, securableItem: string) : Observable<Role[]> {

    let url = this.getRolesBySecurableItemSegment(grain, securableItem);
    return this.httpClient
      .get<Role[]>(url);
  }

  private getRolesBySecurableItemSegment(grain:string, securableItem: string) : string {
    return  `${FabricAuthRoleService.BaseRoleApiUrl}/${grain}/${securableItem}`; 
  }
}
