import { Injectable } from '@angular/core';
import { IFabricPrincipal} from '../models/fabricPrincipal.model';
import { USER, GROUP } from '../constants/principal-constants';

@Injectable({
  providedIn: 'root'
})
export class NameDisplayService {
  constructor() { }

  getPrincipalNameToDisplay(principal: IFabricPrincipal): string {
    if (principal.principalType.toLowerCase() === USER) {
      return principal.identityProviderUserPrincipalName || principal.subjectId;
    }  else if (principal.principalType.toLowerCase() === GROUP) {
      if (!principal.tenantAlias) {
        return principal.subjectId;
      }
      return principal.subjectId + '@' + principal.tenantAlias;
    }
  }
}
