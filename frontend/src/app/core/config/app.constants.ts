export const APP_CONSTANTS = {
  appTitle: 'EnterprisePlatform',
  defaultPageSize: 20,
  maxPageSize: 200,
  dateTimeFormat: 'yyyy-MM-dd HH:mm:ss',
  dateFormat: 'yyyy-MM-dd'
} as const;

export const REQUEST_SIGN_HEADERS = {
  timestamp: 'X-Platform-Timestamp',
  nonce: 'X-Platform-Nonce',
  signature: 'X-Platform-Signature',
  responseTimestamp: 'X-Platform-Response-Timestamp',
  responseSignature: 'X-Platform-Response-Signature'
} as const;
