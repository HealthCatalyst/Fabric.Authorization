import { Component, OnInit, OnDestroy, Inject } from '@angular/core';

import { Subject } from 'rxjs/Subject';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/takeUntil';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';
import 'rxjs/add/observable/zip';
import 'rxjs/add/operator/mergeMap';
import 'rxjs/add/operator/distinctUntilChanged';

import { inherits } from 'util';
import { Router, ActivatedRoute } from '@angular/router';
import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { IRole } from '../../../models/role.model';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { IAccessControlConfigService } from '../../../services/access-control-config.service';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { IUser } from '../../../models/user.model';
import { IGroup } from '../../../models/group.model';

@Component({
  selector: 'app-member',
  templateUrl: './member.component.html',
  styleUrls: ['./member.component.scss', '../access-control.scss']
})
export class MemberComponent implements OnInit, OnDestroy {
  public principals: Array<IFabricPrincipal> = [];
  public roles: Array<IRole> = [];
  public searching = false;
  public searchTextSubject = new Subject<string>();
  public searchText: string;
  public selectedPrincipal?: IFabricPrincipal;
  private grain: string;
  private securableItem: string;
  public editMode = true;

  private ngUnsubscribe: any = new Subject();

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    @Inject('IAccessControlConfigService')private configService: IAccessControlConfigService,
    private groupService: FabricAuthGroupService,
    private router: Router,
    private route: ActivatedRoute
  ) {
  }

  ngOnInit() {
    const subjectId: string = this.route.snapshot.paramMap.get('subjectid');
    const principalType: string = (this.route.snapshot.paramMap.get('type') || '').toLowerCase();
    this.grain = this.route.snapshot.paramMap.get('grain');
    this.securableItem = this.route.snapshot.paramMap.get('securableItem');

    this.searchText = subjectId;
        if (subjectId && principalType) {
            this.selectedPrincipal = {
                subjectId,
                principalType
            };
        }

    // Roles
    this.roleService
      .getRolesBySecurableItemAndGrain(
        this.grain,
        this.securableItem
      )
      .takeUntil(this.ngUnsubscribe)
      .mergeMap((roles: IRole[]) => {
        this.roles = roles.map(role => <IRole>role);
        return this.bindExistingRoles(this.selectedPrincipal);
      })
      .subscribe();

    // Search text
    this.searchTextSubject
      .takeUntil(this.ngUnsubscribe)
      .distinctUntilChanged()
      .debounceTime(500)
      .do((term) => {
        if (this.selectedPrincipal && term !== this.selectedPrincipal.subjectId) {
          this.selectedPrincipal = null;
        }

        this.roles.map(r => r.selected = false);
        this.principals.map(p => p.selected = false);
        if (term && term.length > 2) {
          this.searching = true;
        } else {
          this.searching = false;
          this.principals = [];
        }
      }).subscribe();

    // User / Group IDP search
    this.idpSearchService
      .search(this.searchTextSubject, null)
      .takeUntil(this.ngUnsubscribe)
      .subscribe(result => {
        this.searching = false;
        this.principals =
          result.principals.length === 0
              ? [
                    {
                        subjectId: this.searchText,
                        principalType: 'group'
                    },
                    {
                        subjectId: this.searchText,
                        principalType: 'user'
                    }
                ]
              : result.principals;
      });
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  selectPrincipal(principal: IFabricPrincipal): Subscription {
    this.principals = [];
    this.selectedPrincipal = principal;
    this.searchText = principal.subjectId;
    return this.bindExistingRoles(principal).subscribe();
  }

  save(): Subscription {
    const selectedRoles: IRole[] = this.roles.filter(r => r.selected);

    const saveObservable: Observable<any> =
        this.selectedPrincipal.principalType === 'user'
            ? this.saveUser(this.selectedPrincipal.subjectId, selectedRoles)
            : this.saveGroup(this.selectedPrincipal.subjectId, selectedRoles);

    return saveObservable.subscribe(null, null, () => {
        this.router.navigate(['/access-control']);
    });
  }

  private saveUser(subjectId: string, selectedRoles: IRole[]): Observable<any> {
    const user: IUser = { identityProvider: this.configService.identityProvider, subjectId: subjectId };
    return this.userService
      .getUser(
        this.configService.identityProvider,
        subjectId
      )
      .mergeMap((userResult: IUser) => {
        return Observable.of(userResult);
      })
      .catch(err => {
        if (err.statusCode === 404) {
          return this
            .userService
            .createUser(user);
        }

        return Observable.throw(err.message);
      })
      .mergeMap((newUser: IUser) => {
        return this.userService.getUserRoles(user.identityProvider, user.subjectId);
      })
      .mergeMap((userRoles: IRole[]) => {
        const rolesToAdd = selectedRoles.filter(userRole => !userRoles.some(selectedRole => userRole.id === selectedRole.id));
        const rolesToDelete = userRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));
        return Observable.zip(
          this.userService
            .addRolesToUser(
              user.identityProvider,
              user.subjectId,
              rolesToAdd
            ),
          this.userService
            .removeRolesFromUser(
              user.identityProvider,
              user.subjectId,
              rolesToDelete
            ));
      });
  }

  private saveGroup(subjectId: string, selectedRoles: IRole[]): Observable<any> {
    const group: IGroup = { groupName: subjectId, groupSource: '' };
    return this.groupService
      .getGroup(group.groupName)
      .mergeMap((groupResult: IGroup) => {
        return Observable.of(groupResult);
      })
      .catch(err => {
        if (err.statusCode === 404) {
          return this
            .groupService
            .createGroup(group);
        }

        return Observable.throw(err.message);
      })
      .mergeMap((newGroup: IGroup) => {
        return this.groupService.getGroupRoles(group.groupName,
          this.grain,
          this.securableItem);
      })
      .mergeMap((groupRoles: IRole[]) => {
        const rolesToAdd = selectedRoles.filter(userRole => !groupRoles.some(selectedRole => userRole.id === selectedRole.id));
        const rolesToDelete = groupRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));
        return Observable.zip(
          this.groupService.addRolesToGroup(group.groupName, rolesToAdd),
          this.groupService.removeRolesFromGroup(group.groupName, rolesToDelete));
      });
  }

  private bindExistingRoles(principal: IFabricPrincipal): Observable<any> {
    if (!principal || !principal.subjectId) {
        return Observable.of(undefined);
    }
    const roleObservable: Observable<any> =
        principal.principalType === 'user'
            ? this.userService.getUserRoles(this.configService.identityProvider, principal.subjectId)
            : this.groupService.getGroupRoles(principal.subjectId, this.grain, this.securableItem);

    return roleObservable.do((existingRoles: IRole[]) => {
        if (!existingRoles) {
            this.roles.map(role => (role.selected = false));
        } else {
            this.roles.map(role => (role.selected = existingRoles.some(userRole => userRole.id === role.id)));
        }
    });
}
}
