import { Subject } from 'rxjs';
import { IAccessControlConfigService } from './access-control-config.service';
import { IDataChangedEventArgs } from '../models/changedDataEventArgs.model';
import { Exception } from '../models/exception.model';

export class MockAccessControlConfigService implements IAccessControlConfigService {
    clientId: string;
    identityProvider: string;
    grain: string;
    securableItem: string;
    fabricAuthApiUrl = '';
    fabricExternalIdpSearchApiUrl = '';
    dataChanged = new Subject<IDataChangedEventArgs>();
    errorRaised: Subject<Exception>;
}
