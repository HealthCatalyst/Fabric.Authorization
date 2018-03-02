import { Injectable } from '@angular/core';
import { FabricAuthBaseService } from './fabric-auth-base.service';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { AuthSearchResult } from '../models/authSearchResult';

@Injectable()
export class FabricAuthMemberService extends FabricAuthBaseService {

  static readonly baseMemberApiUrl = FabricAuthBaseService.authUrl + "/members";

  constructor(http: Http) {
    super(http);
  }

  public searchMembers() : Observable<AuthSearchResult> {
    return null;
  }

}
