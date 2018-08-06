import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IRole } from '../models/role.model';

@Injectable()
export class FabricAuthRoleService extends FabricBaseService {

  public static BaseRoleApiUrl = '';

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthRoleService.BaseRoleApiUrl) {
      FabricAuthRoleService.BaseRoleApiUrl = `${accessControlConfigService.fabricAuthApiUrl}/roles`;
    }
  }

  getRolesBySecurableItemAndGrain(
    grain: string,
    securableItem: string
  ): Observable<IRole[]> {
    const url = this.getRolesBySecurableItemSegment(grain, securableItem);
    return this.httpClient.get<IRole[]>(url);
  }

  private getRolesBySecurableItemSegment(
    grain: string,
    securableItem: string
  ): string {
    return `${FabricAuthRoleService.BaseRoleApiUrl}/${grain}/${securableItem}`;
  }
}
