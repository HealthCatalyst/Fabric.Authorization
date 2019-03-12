import { Injectable } from '@angular/core';
import { IFabricPrincipal } from '../models/fabricPrincipal.model';

@Injectable({
  providedIn: 'root'
})
export class NameDisplayService {
  constructor() { }

  getPrincipalNameToDisplay(principal: IFabricPrincipal): string {
    if (principal.principalType.toLowerCase() === 'user') {
      return principal.identityProviderUserPrincipalName || principal.subjectId;
    }  else if (principal.principalType.toLowerCase() === 'group') {
      if (!principal.tenantAlias) {
        return principal.subjectId;
      }
      return principal.subjectId + '@' + principal.tenantAlias;
    }
  }
}
