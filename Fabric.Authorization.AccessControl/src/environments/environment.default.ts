/** default values for environment settings */
export const defaultEnvironment = {
  production: false,
  fabricAuthApiUri: 'http://localhost/authorization',
  fabricAuthApiVersionSegment: 'v1',
  fabricIdentityApiUri: 'http://localhost/identity',
  fabricExternalIdPSearchApiUri: 'http://localhost:5009',
  fabricExternalIdPSearchApiVersionSegment: 'v1',
  applicationEndpoint: 'http://localhost:4200'
};

export type Environment = typeof defaultEnvironment;
