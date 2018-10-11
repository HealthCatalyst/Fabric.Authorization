import { HttpClient } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IGrain } from '../models/grain.model';
import { environment } from '../../environments/environment';

@Injectable()
export class FabricAuthGrainService extends FabricBaseService {
  private static baseUrl = '';
  private static isVisible = false;

  private grains: Array<IGrain> = [];
  private lastUpdateTimestamp = 0;

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthGrainService.baseUrl) {
      FabricAuthGrainService.baseUrl = `${accessControlConfigService.fabricAuthApiUrl}/grains`;
    }

    if (!FabricAuthGrainService.isVisible) {
      FabricAuthGrainService.isVisible = environment.isGrainVisible;
    }
  }

  public getAllGrains(): Observable<Array<IGrain>> {
    const currentTimeStamp = new Date().getTime();
    if (this.lastUpdateTimestamp === 0 || (Math.abs(currentTimeStamp - this.lastUpdateTimestamp) / 60000) > 2) {
      this.lastUpdateTimestamp = currentTimeStamp;
      return this.httpClient.get<Array<IGrain>>(FabricAuthGrainService.baseUrl).pipe(tap(g => {
        this.grains = g;
      }));
    }

    return of(this.grains);
  }

  public isGrainVisible(): boolean {
    return FabricAuthGrainService.isVisible;
  }
}
