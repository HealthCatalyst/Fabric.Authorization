import { Fabric.Identity.Samples.AngularPage } from './app.po';

describe('fabric.identity.samples.angular App', () => {
  let page: Fabric.Identity.Samples.AngularPage;

  beforeEach(() => {
    page = new Fabric.Identity.Samples.AngularPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
