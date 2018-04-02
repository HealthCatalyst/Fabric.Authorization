import { Environment, defaultEnvironment } from './environment.default';

export const environment = Object.assign<Environment, Partial<Environment>>(defaultEnvironment, {
  production: true
});
