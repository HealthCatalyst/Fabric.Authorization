import { Component, OnInit, OnDestroy } from '@angular/core';

import { Subject } from 'rxjs/Subject';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/takeUntil';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';
import 'rxjs/add/observable/zip';
import 'rxjs/add/observable/empty';
import 'rxjs/add/operator/mergeMap';

import {
  FabricExternalIdpSearchService,
  FabricAuthRoleService,
  AccessControlConfigService,
  FabricAuthUserService,
  FabricAuthGroupService
} from '../../../services';
import { IFabricPrincipal, IRole, IUser, IGroup } from '../../../models';
import { inherits } from 'util';
import { Router, ActivatedRoute } from '@angular/router';

export interface IRoleModel extends IRole {
  selected: boolean;
}

export interface IFabricPrincipalModel extends IFabricPrincipal {
  selected: boolean;
}

@Component({
  selector: 'app-member',
  templateUrl: './member.component.html',
  styleUrls: ['./member.component.scss']
})
export class MemberComponent implements OnInit, OnDestroy {
  public principals: Array<IFabricPrincipalModel> = [];
  public roles: Array<IRoleModel> = [];
  public searching = false;

  public searchTextSubject = new Subject<string>();
  public searchText: string;
  private ngUnsubscribe: any = new Subject();
  private principalSelected = false;
  public entityType = '';
  public subjectId = '';
  public editMode = false;

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService,
    private router: Router,
    private route: ActivatedRoute
  ) {
  }

  ngOnInit() {
    this.subjectId = this.route.snapshot.paramMap.get('subjectid');
    this.entityType = (this.route.snapshot.paramMap.get('type') || '').toLowerCase();
    this.editMode = !!this.subjectId;

    // Roles
    this.roleService
      .getRolesBySecurableItemAndGrain(
        this.configService.grain,
        this.configService.securableItem
      )
      .takeUntil(this.ngUnsubscribe)
      .mergeMap((roles: IRole[]) => {
        this.roles = roles.map(role => <IRoleModel>role);
        return this.bindExistingRoles(this.subjectId, this.entityType);
      })
      .subscribe();

    // Search text
    this.searchTextSubject
      .takeUntil(this.ngUnsubscribe)
      .filter(() => !this.editMode)
      .do(() => {
        this.roles.map(r => r.selected = false);
        this.principals.map(p => p.selected = false);
        this.searching = true;
      }).subscribe();

    // User / Group IDP search
    this.idpSearchService
      .searchUser(this.searchTextSubject, null)
      .takeUntil(this.ngUnsubscribe)
      .subscribe(result => {
        this.searching = false;
        this.principals = result.principals.map(principal => <IFabricPrincipalModel>principal);
      });
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  selectPrincipal(principal: IFabricPrincipalModel): Subscription {
    for (const otherPrincal of this.principals.filter((p) => p.subjectId !== principal.subjectId)) {
      otherPrincal.selected = false;
    }
    this.principalSelected = !principal.selected;
    if (this.principalSelected) {
      return this
        .bindExistingRoles(principal.subjectId, principal.principalType)
        .subscribe();
    } else {
      this.roles.map(role => role.selected = false);
      return Subscription.EMPTY;
    }
  }

  save(): Subscription {
    const selectedPrincipal: IFabricPrincipalModel = this.principals.find(p => p.selected);
    const selectedType = selectedPrincipal ? selectedPrincipal.principalType : this.entityType;
    const selectedSubjectId = selectedPrincipal ? selectedPrincipal.subjectId : this.subjectId;
    const selectedRoles = this.roles.filter(r => r.selected);

    const saveObservable = selectedType === 'user' ?
      this.saveUser(selectedSubjectId, selectedRoles) :
      this.saveGroup(selectedSubjectId, selectedRoles);

    return saveObservable.subscribe(null, (error) => {
      // TODO: Error handling
      console.error(error);
    }, () => {
      this.router.navigate(['/accesscontrol']);
    });
  }

  private saveUser(subjectId: string, selectedRoles: IRoleModel[]): Observable<any> {
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

  private saveGroup(subjectId: string, selectedRoles: IRoleModel[]): Observable<any> {
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
          this.configService.grain,
          this.configService.securableItem);
      })
      .mergeMap((groupRoles: IRole[]) => {
        const rolesToAdd = selectedRoles.filter(userRole => !groupRoles.some(selectedRole => userRole.id === selectedRole.id));
        const rolesToDelete = groupRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));
        return Observable.zip(
          this.groupService.addRolesToGroup(group.groupName, rolesToAdd),
          this.groupService.removeRolesFromGroup(group.groupName, rolesToDelete));
      });
  }

  private bindExistingRoles(subjectId: string, principalType: string): Observable<any> {
    if (!this.subjectId) {
      return Observable.empty();
    }
    const roleObservable = principalType === 'user' ?
      this.userService.getUserRoles(this.configService.identityProvider, subjectId) :
      this.groupService.getGroupRoles(subjectId, this.configService.grain, this.configService.securableItem);

    return roleObservable.do((existingRoles: IRole[]) => {
      if (!existingRoles) {
        this.roles.map(role => role.selected = false);
      } else {
        this.roles.map(role => role.selected = existingRoles.some(userRole => userRole.id === role.id));
      }
    });
  }
}
