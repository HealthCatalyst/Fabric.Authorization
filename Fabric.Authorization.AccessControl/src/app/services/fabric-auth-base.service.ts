import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { environment } from '../../environments/environment';

import { Exception } from '../models';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricBaseService {

  constructor(protected httpClient: HttpClient, protected accessControlConfigService: AccessControlConfigService) { }
}
