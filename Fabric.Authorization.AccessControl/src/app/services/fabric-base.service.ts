import { Injectable, Inject } from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { IAccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricBaseService {
  constructor(
    protected httpClient: HttpClient,
    @Inject('IAccessControlConfigService') protected accessControlConfigService: IAccessControlConfigService
  ) {}
}
