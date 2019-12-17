import { APP_SWITCHER_CONFIG } from '@healthcatalyst/cashmere';

export const MockAppSwitcherConfig =
{
  provide: APP_SWITCHER_CONFIG,
  useFactory: ()=>({discoveryServiceUri: "test.discoveryservice.uri"})
};
