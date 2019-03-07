import { Subject, Observable, of } from 'rxjs';
import { IAccessControlConfigService } from './access-control-config.service';
import { IDataChangedEventArgs } from '../models/changedDataEventArgs.model';
import { Exception } from '../models/exception.model';

export class MockAccessControlConfigService implements IAccessControlConfigService {
    clientId: string;
    identityProvider: string;
    grain: string;
    securableItem: string;
    fabricAuthApiUrl = 'auth';
    fabricExternalIdpSearchApiUrl = 'idpss';
    dataChanged = new Subject<IDataChangedEventArgs>();
    errorRaised: Subject<Exception>;
}
