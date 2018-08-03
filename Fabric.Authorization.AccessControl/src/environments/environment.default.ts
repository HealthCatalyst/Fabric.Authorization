/** default values for environment settings */
export const defaultEnvironment = {
  production: false,
  fabricAuthApiUri: 'http://localhost:5004',
  fabricAuthApiVersionSegment: 'v1',
  fabricIdentityApiUri: 'http://localhost/identity',
  fabricExternalIdPSearchApiUri: 'http://localhost/IdentityProviderSearchService',
  fabricExternalIdPSearchApiVersionSegment: 'v1',
  applicationEndpoint: 'http://localhost:5004',
  isGrainVisible: false
};

export type Environment = typeof defaultEnvironment;
