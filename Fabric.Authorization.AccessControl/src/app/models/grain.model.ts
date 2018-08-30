import { ISecurableItem } from './securableItem.model';

export interface IGrain {
  id?: string;
  name: string;
  securableItems: Array<ISecurableItem>;
  createdBy: string;
  modifiedBy: string;
  requiredWriteScopes: Array<string>;
  isShared: boolean;
}
