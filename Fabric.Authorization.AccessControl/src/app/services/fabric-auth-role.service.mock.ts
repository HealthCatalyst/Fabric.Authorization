import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';

import { IRole } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

export const mockRoles: IRole[] = [
    { name: 'admin', grain: 'app', securableItem: 'foo' },
    { name: 'superuser', grain: 'app', securableItem: 'foo' }
];

export class FabricAuthRoleServiceMock {
    getRolesBySecurableItemAndGrain: jasmine.Spy = jasmine.createSpy('getRolesBySecurableItemAndGrain');
}
