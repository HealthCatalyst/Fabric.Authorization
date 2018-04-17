import { Injectable, Inject } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { environment } from '../../environments/environment';

import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { IAccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricBaseService {
  constructor(
    protected httpClient: HttpClient,
    @Inject('IAccessControlConfigService') protected accessControlConfigService: IAccessControlConfigService
  ) {}
}
