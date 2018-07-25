export interface ISecurableItem {
  id?: string;
  name: string;
  clientOwner: string;
  grain: string;
  securableItems: Array<ISecurableItem>;
  createdBy: string;
  modifiedBy: string;
}
