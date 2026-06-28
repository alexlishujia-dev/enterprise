export const environment = {
  production: true,
  apiBaseUrl: '/api',
  /** 生产环境默认同源；若前后端分离部署，改为 API 公网地址 */
  assetBaseUrl: '',
  requestSignEnabled: true,
  requestSignSecretKey: 'EnterprisePlatform-Prod-Request-Sign-Key-Change-Me!',
  tokenStorageKey: 'ep_access_token',
  userStorageKey: 'ep_current_user'
};
