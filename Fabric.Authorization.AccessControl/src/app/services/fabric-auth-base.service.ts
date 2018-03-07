import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { environment } from '../../environments/environment';

import { Exception } from '../models';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';

@Injectable()
export class FabricAuthBaseService {

  protected static readonly retryCount = 3;
  protected static readonly authUrl = `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`;

  constructor(protected httpClient: HttpClient) { }
}
